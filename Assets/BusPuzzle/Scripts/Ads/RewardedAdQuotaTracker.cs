using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal enum RewardedAdQuotaBlockReason
    {
        None = 0,
        ClockRollback = 1,
        ReservationInProgress = 2,
        GlobalLimitReached = 3,
        PlacementLimitReached = 4,
        StageTotalLimitReached = 5,
        PlacementStageLimitReached = 6,
        CooldownActive = 7,
        PersistenceUnavailable = 8
    }

    /// <summary>
    /// Limits used to evaluate one rewarded-ad placement. A limit of -1 is unlimited and a
    /// limit of 0 blocks that scope completely. PlacementLimit is the limit for the placement
    /// passed to RewardedAdQuotaTracker, so callers can supply a different value per placement.
    /// </summary>
    internal readonly struct RewardedAdQuotaPolicy
    {
        public const int Unlimited = -1;

        public RewardedAdQuotaPolicy(
            int globalLimit,
            int placementLimit,
            int stageTotalLimit,
            int placementStageLimit,
            int cooldownSeconds)
        {
            GlobalLimit = NormalizeLimit(globalLimit);
            PlacementLimit = NormalizeLimit(placementLimit);
            StageTotalLimit = NormalizeLimit(stageTotalLimit);
            PlacementStageLimit = NormalizeLimit(placementStageLimit);
            CooldownSeconds = Math.Max(0, cooldownSeconds);
        }

        public int GlobalLimit { get; }
        public int PlacementLimit { get; }
        public int StageTotalLimit { get; }
        public int PlacementStageLimit { get; }
        public int CooldownSeconds { get; }

        private static int NormalizeLimit(int value)
        {
            return value < Unlimited ? Unlimited : value;
        }
    }

    internal readonly struct RewardedAdQuotaDecision
    {
        internal RewardedAdQuotaDecision(
            bool isAllowed,
            RewardedAdQuotaBlockReason blockReason,
            TimeSpan remainingWait,
            bool hasFiniteWait,
            int globalUsed,
            int placementUsed,
            int stageTotalUsed,
            int placementStageUsed)
        {
            IsAllowed = isAllowed;
            BlockReason = blockReason;
            RemainingWait = remainingWait < TimeSpan.Zero ? TimeSpan.Zero : remainingWait;
            HasFiniteWait = hasFiniteWait;
            GlobalUsed = Math.Max(0, globalUsed);
            PlacementUsed = Math.Max(0, placementUsed);
            StageTotalUsed = Math.Max(0, stageTotalUsed);
            PlacementStageUsed = Math.Max(0, placementStageUsed);
        }

        public bool IsAllowed { get; }
        public RewardedAdQuotaBlockReason BlockReason { get; }

        /// <summary>
        /// Time until the block can clear according to the current device UTC clock. This is
        /// zero when HasFiniteWait is false (for example, when a configured limit is zero).
        /// </summary>
        public TimeSpan RemainingWait { get; }

        public bool HasFiniteWait { get; }
        public int GlobalUsed { get; }
        public int PlacementUsed { get; }
        public int StageTotalUsed { get; }
        public int PlacementStageUsed { get; }
    }

    /// <summary>
    /// Opaque, single-use token returned by RewardedAdQuotaTracker.TryBegin.
    /// </summary>
    internal readonly struct RewardedAdQuotaReservation
    {
        private readonly RewardedAdQuotaTracker owner;
        private readonly long id;

        internal RewardedAdQuotaReservation(RewardedAdQuotaTracker owner, long id)
        {
            this.owner = owner;
            this.id = id;
        }

        public bool IsValid
        {
            get { return owner != null && id > 0; }
        }

        internal long Id
        {
            get { return id; }
        }

        internal bool BelongsTo(RewardedAdQuotaTracker tracker)
        {
            return ReferenceEquals(owner, tracker);
        }
    }

    /// <summary>
    /// Persistent rolling-window quota accounting for rewarded-ad show attempts. Call TryBegin
    /// immediately before RewardedAd.Show. Commit the reservation when Show returns normally and
    /// Cancel it when Show is not attempted or throws. Commit is deliberately conservative: it
    /// consumes quota even if a later SDK callback reports that the ad failed to open.
    /// </summary>
    internal sealed class RewardedAdQuotaTracker
    {
        public const string DefaultPlayerPrefsKey = "bus_puzzle_rewarded_ad_quota_v1";
        public const string NoStageContext = "";

        private const int SchemaVersion = 1;
        private const int MaxStageContextLength = 160;
        private const long RollingWindowTicks = TimeSpan.TicksPerHour * 24L;
        private const long ClockRollbackToleranceTicks = TimeSpan.TicksPerMinute * 5L;
        private const long ReservationTimeoutTicks = TimeSpan.TicksPerMinute * 2L;

        private readonly object syncRoot = new object();
        private readonly string playerPrefsKey;
        private readonly Func<DateTime> utcNowProvider;
        private readonly Dictionary<long, PendingReservation> pendingReservations =
            new Dictionary<long, PendingReservation>();

        private PersistedState state;
        private long nextReservationId;
        private bool isPersistenceHealthy = true;

        public RewardedAdQuotaTracker()
            : this(DefaultPlayerPrefsKey, null)
        {
        }

        public RewardedAdQuotaTracker(string playerPrefsKey)
            : this(playerPrefsKey, null)
        {
        }

        /// <summary>
        /// The custom clock overload exists so rollback and rolling-window behavior can be tested
        /// deterministically. Runtime callers should normally use one of the other constructors.
        /// </summary>
        public RewardedAdQuotaTracker(string playerPrefsKey, Func<DateTime> utcNowProvider)
        {
            if (string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                throw new ArgumentException("A PlayerPrefs key is required.", "playerPrefsKey");
            }

            this.playerPrefsKey = playerPrefsKey;
            this.utcNowProvider = utcNowProvider ?? GetSystemUtcNow;
            state = LoadState();
        }

        public static TimeSpan RollingWindow
        {
            get { return TimeSpan.FromTicks(RollingWindowTicks); }
        }

        public RewardedAdQuotaDecision Evaluate(
            RewardedAdPlacement placement,
            string stageContext,
            RewardedAdQuotaPolicy policy)
        {
            lock (syncRoot)
            {
                var clock = ObserveClock();
                var stateChanged = PruneExpiredRecords(clock.EffectiveUtcTicks);
                RemoveExpiredReservations(clock.EffectiveUtcTicks);

                var decision = EvaluateInternal(
                    placement,
                    NormalizeStageContext(stageContext),
                    policy,
                    clock);

                if (stateChanged && !SaveState())
                {
                    return CreateIndefiniteBlock(
                        RewardedAdQuotaBlockReason.PersistenceUnavailable,
                        BuildUsage(placement, NormalizeStageContext(stageContext)));
                }

                return decision;
            }
        }

        public bool TryBegin(
            RewardedAdPlacement placement,
            string stageContext,
            RewardedAdQuotaPolicy policy,
            out RewardedAdQuotaReservation reservation,
            out RewardedAdQuotaDecision decision)
        {
            lock (syncRoot)
            {
                var normalizedStageContext = NormalizeStageContext(stageContext);
                var clock = ObserveClock();
                PruneExpiredRecords(clock.EffectiveUtcTicks);
                RemoveExpiredReservations(clock.EffectiveUtcTicks);

                decision = EvaluateInternal(placement, normalizedStageContext, policy, clock);
                if (!decision.IsAllowed || !SaveState())
                {
                    if (decision.IsAllowed)
                    {
                        decision = CreateIndefiniteBlock(
                            RewardedAdQuotaBlockReason.PersistenceUnavailable,
                            BuildUsage(placement, normalizedStageContext));
                    }

                    reservation = default(RewardedAdQuotaReservation);
                    return false;
                }

                var reservationId = GetNextReservationId();
                pendingReservations.Add(
                    reservationId,
                    new PendingReservation(
                        reservationId,
                        placement,
                        normalizedStageContext,
                        clock.EffectiveUtcTicks));

                reservation = new RewardedAdQuotaReservation(this, reservationId);
                return true;
            }
        }

        /// <summary>
        /// Commits one conservative quota record. Call this as soon as the SDK Show call returns
        /// normally; do not wait for the reward, impression, or close callback.
        /// </summary>
        public bool Commit(RewardedAdQuotaReservation reservation)
        {
            lock (syncRoot)
            {
                PendingReservation pending;
                if (!TryTakeReservation(reservation, out pending))
                {
                    return false;
                }

                var clock = ObserveClock();
                PruneExpiredRecords(clock.EffectiveUtcTicks);

                var committedUtcTicks = Math.Max(
                    pending.StartedUtcTicks,
                    clock.EffectiveUtcTicks);
                state.records.Add(
                    new PersistedQuotaRecord
                    {
                        utcTicks = committedUtcTicks,
                        placement = (int)pending.Placement,
                        stageContext = pending.StageContext
                    });

                state.records.Sort(CompareRecordsByUtc);
                return SaveState();
            }
        }

        /// <summary>
        /// Releases a reservation without consuming quota. This is the path for a Show method
        /// that returns false or throws before an ad is accepted for presentation.
        /// </summary>
        public bool Cancel(RewardedAdQuotaReservation reservation)
        {
            lock (syncRoot)
            {
                PendingReservation ignored;
                return TryTakeReservation(reservation, out ignored);
            }
        }

        private RewardedAdQuotaDecision EvaluateInternal(
            RewardedAdPlacement placement,
            string stageContext,
            RewardedAdQuotaPolicy policy,
            ClockObservation clock)
        {
            var usage = BuildUsage(placement, stageContext);

            if (!isPersistenceHealthy)
            {
                return CreateIndefiniteBlock(
                    RewardedAdQuotaBlockReason.PersistenceUnavailable,
                    usage);
            }

            if (clock.RollbackTicks > ClockRollbackToleranceTicks)
            {
                return CreateTimedBlock(
                    RewardedAdQuotaBlockReason.ClockRollback,
                    state.lastObservedUtcTicks,
                    clock.RawUtcTicks,
                    usage);
            }

            if (pendingReservations.Count > 0)
            {
                var retryAtUtcTicks = GetPendingReservationRetryUtcTicks();
                return CreateTimedBlock(
                    RewardedAdQuotaBlockReason.ReservationInProgress,
                    retryAtUtcTicks,
                    clock.RawUtcTicks,
                    usage);
            }

            RewardedAdQuotaDecision blocked;
            if (TryCreateLimitBlock(
                    RewardedAdQuotaBlockReason.GlobalLimitReached,
                    policy.GlobalLimit,
                    usage.GlobalTimes,
                    clock.RawUtcTicks,
                    usage,
                    out blocked))
            {
                return blocked;
            }

            if (TryCreateLimitBlock(
                    RewardedAdQuotaBlockReason.PlacementLimitReached,
                    policy.PlacementLimit,
                    usage.PlacementTimes,
                    clock.RawUtcTicks,
                    usage,
                    out blocked))
            {
                return blocked;
            }

            if (!string.IsNullOrEmpty(stageContext) &&
                TryCreateLimitBlock(
                    RewardedAdQuotaBlockReason.StageTotalLimitReached,
                    policy.StageTotalLimit,
                    usage.StageTimes,
                    clock.RawUtcTicks,
                    usage,
                    out blocked))
            {
                return blocked;
            }

            if (!string.IsNullOrEmpty(stageContext) &&
                TryCreateLimitBlock(
                    RewardedAdQuotaBlockReason.PlacementStageLimitReached,
                    policy.PlacementStageLimit,
                    usage.PlacementStageTimes,
                    clock.RawUtcTicks,
                    usage,
                    out blocked))
            {
                return blocked;
            }

            if (policy.CooldownSeconds > 0 && usage.GlobalTimes.Count > 0)
            {
                var latestUtcTicks = usage.GlobalTimes[usage.GlobalTimes.Count - 1];
                var retryAtUtcTicks = SafeAddTicks(
                    latestUtcTicks,
                    (long)policy.CooldownSeconds * TimeSpan.TicksPerSecond);

                if (clock.EffectiveUtcTicks < retryAtUtcTicks)
                {
                    return CreateTimedBlock(
                        RewardedAdQuotaBlockReason.CooldownActive,
                        retryAtUtcTicks,
                        clock.RawUtcTicks,
                        usage);
                }
            }

            return new RewardedAdQuotaDecision(
                true,
                RewardedAdQuotaBlockReason.None,
                TimeSpan.Zero,
                true,
                usage.GlobalTimes.Count,
                usage.PlacementTimes.Count,
                usage.StageTimes.Count,
                usage.PlacementStageTimes.Count);
        }

        private static bool TryCreateLimitBlock(
            RewardedAdQuotaBlockReason reason,
            int limit,
            List<long> matchingTimes,
            long rawUtcTicks,
            UsageSnapshot usage,
            out RewardedAdQuotaDecision decision)
        {
            if (limit == RewardedAdQuotaPolicy.Unlimited || matchingTimes.Count < limit)
            {
                decision = default(RewardedAdQuotaDecision);
                return false;
            }

            if (limit == 0)
            {
                decision = CreateIndefiniteBlock(reason, usage);
                return true;
            }

            // If a policy was lowered below the already-recorded count, enough of the oldest
            // records must expire to bring the count below the new limit.
            var expiryIndex = matchingTimes.Count - limit;
            var retryAtUtcTicks = SafeAddTicks(
                matchingTimes[expiryIndex],
                RollingWindowTicks);
            decision = CreateTimedBlock(reason, retryAtUtcTicks, rawUtcTicks, usage);
            return true;
        }

        private UsageSnapshot BuildUsage(
            RewardedAdPlacement placement,
            string stageContext)
        {
            var usage = new UsageSnapshot();
            var placementValue = (int)placement;
            var hasStageContext = !string.IsNullOrEmpty(stageContext);

            for (var index = 0; index < state.records.Count; index++)
            {
                var record = state.records[index];
                usage.GlobalTimes.Add(record.utcTicks);

                var placementMatches = record.placement == placementValue;
                var stageMatches = hasStageContext &&
                    string.Equals(
                        record.stageContext,
                        stageContext,
                        StringComparison.Ordinal);

                if (placementMatches)
                {
                    usage.PlacementTimes.Add(record.utcTicks);
                }

                if (stageMatches)
                {
                    usage.StageTimes.Add(record.utcTicks);
                }

                if (placementMatches && stageMatches)
                {
                    usage.PlacementStageTimes.Add(record.utcTicks);
                }
            }

            return usage;
        }

        private bool PruneExpiredRecords(long effectiveUtcTicks)
        {
            if (state.records.Count == 0)
            {
                return false;
            }

            var cutoffUtcTicks = effectiveUtcTicks > RollingWindowTicks
                ? effectiveUtcTicks - RollingWindowTicks
                : DateTime.MinValue.Ticks;
            var removeCount = 0;

            while (removeCount < state.records.Count &&
                   state.records[removeCount].utcTicks <= cutoffUtcTicks)
            {
                removeCount++;
            }

            if (removeCount == 0)
            {
                return false;
            }

            state.records.RemoveRange(0, removeCount);
            return true;
        }

        private void RemoveExpiredReservations(long effectiveUtcTicks)
        {
            if (pendingReservations.Count == 0)
            {
                return;
            }

            var expiredIds = new List<long>();
            foreach (var pair in pendingReservations)
            {
                var expiresUtcTicks = SafeAddTicks(
                    pair.Value.StartedUtcTicks,
                    ReservationTimeoutTicks);
                if (effectiveUtcTicks >= expiresUtcTicks)
                {
                    expiredIds.Add(pair.Key);
                }
            }

            for (var index = 0; index < expiredIds.Count; index++)
            {
                pendingReservations.Remove(expiredIds[index]);
            }
        }

        private long GetPendingReservationRetryUtcTicks()
        {
            var retryAtUtcTicks = long.MaxValue;
            foreach (var pair in pendingReservations)
            {
                var expiresUtcTicks = SafeAddTicks(
                    pair.Value.StartedUtcTicks,
                    ReservationTimeoutTicks);
                retryAtUtcTicks = Math.Min(retryAtUtcTicks, expiresUtcTicks);
            }

            return retryAtUtcTicks == long.MaxValue
                ? state.lastObservedUtcTicks
                : retryAtUtcTicks;
        }

        private bool TryTakeReservation(
            RewardedAdQuotaReservation reservation,
            out PendingReservation pending)
        {
            if (!reservation.IsValid || !reservation.BelongsTo(this))
            {
                pending = default(PendingReservation);
                return false;
            }

            if (!pendingReservations.TryGetValue(reservation.Id, out pending))
            {
                return false;
            }

            pendingReservations.Remove(reservation.Id);
            return true;
        }

        private long GetNextReservationId()
        {
            do
            {
                nextReservationId++;
                if (nextReservationId <= 0)
                {
                    nextReservationId = 1;
                }
            }
            while (pendingReservations.ContainsKey(nextReservationId));

            return nextReservationId;
        }

        private ClockObservation ObserveClock()
        {
            var rawUtcTicks = GetUtcNow().Ticks;
            var previousObservedUtcTicks = state.lastObservedUtcTicks;
            var rollbackTicks = previousObservedUtcTicks > rawUtcTicks
                ? previousObservedUtcTicks - rawUtcTicks
                : 0L;

            if (rawUtcTicks > state.lastObservedUtcTicks)
            {
                state.lastObservedUtcTicks = rawUtcTicks;
            }

            var effectiveUtcTicks = Math.Max(rawUtcTicks, state.lastObservedUtcTicks);
            return new ClockObservation(rawUtcTicks, effectiveUtcTicks, rollbackTicks);
        }

        private DateTime GetUtcNow()
        {
            var value = utcNowProvider();
            if (value.Kind == DateTimeKind.Local)
            {
                return value.ToUniversalTime();
            }

            return value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private PersistedState LoadState()
        {
            var json = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateEmptyState();
            }

            try
            {
                var loaded = JsonUtility.FromJson<PersistedState>(json);
                if (loaded == null || loaded.version != SchemaVersion)
                {
                    isPersistenceHealthy = false;
                    Debug.LogWarning("Rewarded-ad quota state has an unsupported schema. Rewarded ads are blocked for this session.");
                    return CreateEmptyState();
                }

                if (loaded.records == null)
                {
                    loaded.records = new List<PersistedQuotaRecord>();
                }

                if (loaded.lastObservedUtcTicks < DateTime.MinValue.Ticks ||
                    loaded.lastObservedUtcTicks > DateTime.MaxValue.Ticks)
                {
                    loaded.lastObservedUtcTicks = 0L;
                }

                for (var index = loaded.records.Count - 1; index >= 0; index--)
                {
                    var record = loaded.records[index];
                    if (record == null ||
                        record.utcTicks <= DateTime.MinValue.Ticks ||
                        record.utcTicks > DateTime.MaxValue.Ticks)
                    {
                        loaded.records.RemoveAt(index);
                        continue;
                    }

                    record.stageContext = NormalizeStageContext(record.stageContext);
                    loaded.lastObservedUtcTicks = Math.Max(
                        loaded.lastObservedUtcTicks,
                        record.utcTicks);
                }

                loaded.records.Sort(CompareRecordsByUtc);
                return loaded;
            }
            catch (Exception exception)
            {
                isPersistenceHealthy = false;
                Debug.LogWarning(
                    "Failed to read rewarded-ad quota state. Rewarded ads are blocked for this session: " +
                    exception.Message);
                return CreateEmptyState();
            }
        }

        private bool SaveState()
        {
            if (!isPersistenceHealthy)
            {
                return false;
            }

            state.version = SchemaVersion;
            try
            {
                PlayerPrefs.SetString(playerPrefsKey, JsonUtility.ToJson(state));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                isPersistenceHealthy = false;
                Debug.LogWarning(
                    "Failed to persist rewarded-ad quota state. Rewarded ads are blocked for this session: " +
                    exception.Message);
                return false;
            }
        }

        private static PersistedState CreateEmptyState()
        {
            return new PersistedState
            {
                version = SchemaVersion,
                lastObservedUtcTicks = 0L,
                records = new List<PersistedQuotaRecord>()
            };
        }

        private static RewardedAdQuotaDecision CreateTimedBlock(
            RewardedAdQuotaBlockReason reason,
            long retryAtUtcTicks,
            long rawUtcTicks,
            UsageSnapshot usage)
        {
            var remainingTicks = retryAtUtcTicks > rawUtcTicks
                ? retryAtUtcTicks - rawUtcTicks
                : 0L;

            return new RewardedAdQuotaDecision(
                false,
                reason,
                TimeSpan.FromTicks(remainingTicks),
                true,
                usage.GlobalTimes.Count,
                usage.PlacementTimes.Count,
                usage.StageTimes.Count,
                usage.PlacementStageTimes.Count);
        }

        private static RewardedAdQuotaDecision CreateIndefiniteBlock(
            RewardedAdQuotaBlockReason reason,
            UsageSnapshot usage)
        {
            return new RewardedAdQuotaDecision(
                false,
                reason,
                TimeSpan.Zero,
                false,
                usage.GlobalTimes.Count,
                usage.PlacementTimes.Count,
                usage.StageTimes.Count,
                usage.PlacementStageTimes.Count);
        }

        private static long SafeAddTicks(long value, long amount)
        {
            if (amount <= 0L)
            {
                return value;
            }

            return value > DateTime.MaxValue.Ticks - amount
                ? DateTime.MaxValue.Ticks
                : value + amount;
        }

        private static string NormalizeStageContext(string stageContext)
        {
            if (string.IsNullOrWhiteSpace(stageContext))
            {
                return NoStageContext;
            }

            var normalized = stageContext.Trim();
            return normalized.Length <= MaxStageContextLength
                ? normalized
                : normalized.Substring(0, MaxStageContextLength);
        }

        private static int CompareRecordsByUtc(
            PersistedQuotaRecord left,
            PersistedQuotaRecord right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.utcTicks.CompareTo(right.utcTicks);
        }

        private static DateTime GetSystemUtcNow()
        {
            return DateTime.UtcNow;
        }

        [Serializable]
        private sealed class PersistedState
        {
            public int version;
            public long lastObservedUtcTicks;
            public List<PersistedQuotaRecord> records;
        }

        [Serializable]
        private sealed class PersistedQuotaRecord
        {
            public long utcTicks;
            public int placement;
            public string stageContext;
        }

        private readonly struct PendingReservation
        {
            public PendingReservation(
                long id,
                RewardedAdPlacement placement,
                string stageContext,
                long startedUtcTicks)
            {
                Id = id;
                Placement = placement;
                StageContext = stageContext;
                StartedUtcTicks = startedUtcTicks;
            }

            public long Id { get; }
            public RewardedAdPlacement Placement { get; }
            public string StageContext { get; }
            public long StartedUtcTicks { get; }
        }

        private readonly struct ClockObservation
        {
            public ClockObservation(
                long rawUtcTicks,
                long effectiveUtcTicks,
                long rollbackTicks)
            {
                RawUtcTicks = rawUtcTicks;
                EffectiveUtcTicks = effectiveUtcTicks;
                RollbackTicks = rollbackTicks;
            }

            public long RawUtcTicks { get; }
            public long EffectiveUtcTicks { get; }
            public long RollbackTicks { get; }
        }

        private sealed class UsageSnapshot
        {
            public readonly List<long> GlobalTimes = new List<long>();
            public readonly List<long> PlacementTimes = new List<long>();
            public readonly List<long> StageTimes = new List<long>();
            public readonly List<long> PlacementStageTimes = new List<long>();
        }
    }
}
