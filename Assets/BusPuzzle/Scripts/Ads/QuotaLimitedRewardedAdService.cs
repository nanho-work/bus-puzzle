using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BusPuzzle
{
    internal interface IRewardedAdQuotaStatusProvider
    {
        RewardedAdQuotaDecision GetQuotaDecision(RewardedAdPlacement placement);
    }

    internal sealed class QuotaLimitedRewardedAdService : IRewardedAdService, IRewardedAdQuotaStatusProvider
    {
        private static readonly RewardedAdPlacement[] Placements =
        {
            RewardedAdPlacement.StationSlotUnlock,
            RewardedAdPlacement.VipBusTeleport,
            RewardedAdPlacement.BusColorShuffle,
            RewardedAdPlacement.DepartBoost,
            RewardedAdPlacement.StageClearDouble
        };

        private readonly IRewardedAdService inner;
        private readonly Func<string> stageContextProvider;
        private readonly Func<RewardedAdQuotaPolicy> policyProvider;
        private readonly RewardedAdQuotaTracker quotaTracker;

        private SynchronizationContext mainThreadContext;
        private bool isInitialized;
        private bool isShutdown;
        private int refreshGeneration;
        private double refreshDueRealtime = double.PositiveInfinity;

        public QuotaLimitedRewardedAdService(
            IRewardedAdService inner,
            Func<string> stageContextProvider,
            Func<RewardedAdQuotaPolicy> policyProvider)
            : this(inner, stageContextProvider, policyProvider, new RewardedAdQuotaTracker())
        {
        }

        internal QuotaLimitedRewardedAdService(
            IRewardedAdService inner,
            Func<string> stageContextProvider,
            Func<RewardedAdQuotaPolicy> policyProvider,
            RewardedAdQuotaTracker quotaTracker)
        {
            this.inner = inner ?? throw new ArgumentNullException("inner");
            this.stageContextProvider = stageContextProvider;
            this.policyProvider = policyProvider;
            this.quotaTracker = quotaTracker ?? throw new ArgumentNullException("quotaTracker");
        }

        public event Action AvailabilityChanged;

        public bool IsReady => IsReadyFor(RewardedAdPlacement.StationSlotUnlock);
        public string CurrentAdUnitId => inner.CurrentAdUnitId;

        public bool IsReadyFor(RewardedAdPlacement placement)
        {
            return !isShutdown &&
                GetQuotaDecision(placement).IsAllowed &&
                inner.IsReadyFor(placement);
        }

        public string GetAdUnitId(RewardedAdPlacement placement)
        {
            return inner.GetAdUnitId(placement);
        }

        public RewardedAdQuotaDecision GetQuotaDecision(RewardedAdPlacement placement)
        {
            var decision = EvaluateQuotaDecision(placement);
            ScheduleQuotaRefresh(decision);
            return decision;
        }

        private RewardedAdQuotaDecision EvaluateQuotaDecision(RewardedAdPlacement placement)
        {
            if (isShutdown)
            {
                return CreateBlockedDecision();
            }

            try
            {
                return quotaTracker.Evaluate(
                    placement,
                    GetStageContext(),
                    GetPolicy());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded-ad quota evaluation failed closed: {exception.Message}");
                return CreateBlockedDecision();
            }
        }

        public void Initialize()
        {
            if (isInitialized || isShutdown)
            {
                return;
            }

            isInitialized = true;
            mainThreadContext = SynchronizationContext.Current;
            inner.AvailabilityChanged += HandleInnerAvailabilityChanged;
            inner.Initialize();
            ScheduleNextQuotaRefresh();
        }

        public void Shutdown()
        {
            if (isShutdown)
            {
                return;
            }

            isShutdown = true;
            refreshGeneration++;
            refreshDueRealtime = double.PositiveInfinity;
            if (isInitialized)
            {
                inner.AvailabilityChanged -= HandleInnerAvailabilityChanged;
            }

            inner.Shutdown();
            AvailabilityChanged = null;
            isInitialized = false;
            mainThreadContext = null;
        }

        public void Preload()
        {
            if (isShutdown)
            {
                return;
            }

            for (var index = 0; index < Placements.Length; index++)
            {
                Preload(Placements[index]);
            }
        }

        public void Preload(RewardedAdPlacement placement)
        {
            if (isShutdown)
            {
                return;
            }

            var decision = GetQuotaDecision(placement);
            if (decision.IsAllowed)
            {
                inner.Preload(placement);
                return;
            }

            ScheduleQuotaRefresh(decision);
        }

        public bool ShowStationSlotUnlockAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(
                RewardedAdPlacement.StationSlotUnlock,
                onCompleted,
                callback => inner.ShowStationSlotUnlockAd(callback));
        }

        public bool ShowVipBusTeleportAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(
                RewardedAdPlacement.VipBusTeleport,
                onCompleted,
                callback => inner.ShowVipBusTeleportAd(callback));
        }

        public bool ShowBusColorShuffleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(
                RewardedAdPlacement.BusColorShuffle,
                onCompleted,
                callback => inner.ShowBusColorShuffleAd(callback));
        }

        public bool ShowDepartBoostAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(
                RewardedAdPlacement.DepartBoost,
                onCompleted,
                callback => inner.ShowDepartBoostAd(callback));
        }

        public bool ShowStageClearDoubleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(
                RewardedAdPlacement.StageClearDouble,
                onCompleted,
                callback => inner.ShowStageClearDoubleAd(callback));
        }

        private bool ShowRewardedAd(
            RewardedAdPlacement placement,
            Action<RewardedAdResult> onCompleted,
            Func<Action<RewardedAdResult>, bool> show)
        {
            if (isShutdown)
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                return false;
            }

            RewardedAdQuotaReservation reservation;
            RewardedAdQuotaDecision decision;
            try
            {
                if (!quotaTracker.TryBegin(
                        placement,
                        GetStageContext(),
                        GetPolicy(),
                        out reservation,
                        out decision))
                {
                    ScheduleQuotaRefresh(decision);
                    onCompleted?.Invoke(RewardedAdResult.NotReady);
                    AvailabilityChanged?.Invoke();
                    return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded-ad quota reservation failed closed: {exception.Message}");
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                AvailabilityChanged?.Invoke();
                return false;
            }

            var callbackGate = new object();
            var showReturned = false;
            var showAccepted = false;
            var hasBufferedResult = false;
            var completionDelivered = false;
            var bufferedResult = RewardedAdResult.NotReady;
            Action<RewardedAdResult> guardedCompletion = result =>
            {
                var invokeImmediately = false;
                var resultToInvoke = result;
                lock (callbackGate)
                {
                    if (completionDelivered)
                    {
                        return;
                    }

                    if (!showReturned)
                    {
                        if (!hasBufferedResult)
                        {
                            bufferedResult = result;
                            hasBufferedResult = true;
                        }

                        return;
                    }

                    completionDelivered = true;
                    resultToInvoke = showAccepted
                        ? result
                        : RewardedAdResult.NotReady;
                    invokeImmediately = true;
                }

                if (invokeImmediately)
                {
                    onCompleted?.Invoke(resultToInvoke);
                }
            };

            var shown = false;
            try
            {
                shown = show(guardedCompletion);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded-ad show threw before acceptance: {exception.Message}");
            }

            if (shown)
            {
                if (!quotaTracker.Commit(reservation))
                {
                    Debug.LogError("A shown rewarded ad could not be committed to its quota ledger.");
                }
            }
            else
            {
                quotaTracker.Cancel(reservation);
            }

            RewardedAdResult resultToDeliver = RewardedAdResult.NotReady;
            var shouldDeliverBufferedResult = false;
            lock (callbackGate)
            {
                showAccepted = shown;
                showReturned = true;
                if (hasBufferedResult)
                {
                    // A false/throwing Show contract means no ad was accepted. Never allow a
                    // malformed synchronous callback to grant a reward on that path.
                    resultToDeliver = shown
                        ? bufferedResult
                        : RewardedAdResult.NotReady;
                    shouldDeliverBufferedResult = true;
                    completionDelivered = true;
                }
            }

            ScheduleNextQuotaRefresh();
            AvailabilityChanged?.Invoke();

            if (shouldDeliverBufferedResult)
            {
                onCompleted?.Invoke(resultToDeliver);
            }

            return shown;
        }

        private void HandleInnerAvailabilityChanged()
        {
            if (!isShutdown)
            {
                AvailabilityChanged?.Invoke();
            }
        }

        private string GetStageContext()
        {
            try
            {
                var stageContext = stageContextProvider != null
                    ? stageContextProvider()
                    : RewardedAdQuotaTracker.NoStageContext;
                return string.IsNullOrWhiteSpace(stageContext)
                    ? "stage:invalid"
                    : stageContext;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded-ad stage context failed closed: {exception.Message}");
                return "stage:invalid";
            }
        }

        private RewardedAdQuotaPolicy GetPolicy()
        {
            if (policyProvider == null)
            {
                return new RewardedAdQuotaPolicy(0, 0, 0, 0, 0);
            }

            try
            {
                return policyProvider();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded-ad quota policy failed closed: {exception.Message}");
                return new RewardedAdQuotaPolicy(0, 0, 0, 0, 0);
            }
        }

        private void ScheduleQuotaRefresh(RewardedAdQuotaDecision decision)
        {
            if (isShutdown || decision.IsAllowed || !decision.HasFiniteWait)
            {
                return;
            }

            var delay = decision.RemainingWait + TimeSpan.FromMilliseconds(250);
            // Task.Delay rejects very large TimeSpan values. Re-evaluate long clock-rollback
            // blocks in bounded daily chunks instead of letting an async-void timer throw.
            var maximumDelay = TimeSpan.FromHours(24);
            if (delay > maximumDelay)
            {
                delay = maximumDelay;
            }
            var dueRealtime = Time.realtimeSinceStartupAsDouble + Math.Max(0d, delay.TotalSeconds);
            if (dueRealtime >= refreshDueRealtime)
            {
                return;
            }

            refreshDueRealtime = dueRealtime;
            var generation = ++refreshGeneration;
            RefreshAfterDelay(delay, generation);
        }

        private void ScheduleNextQuotaRefresh()
        {
            if (isShutdown)
            {
                return;
            }

            for (var index = 0; index < Placements.Length; index++)
            {
                ScheduleQuotaRefresh(EvaluateQuotaDecision(Placements[index]));
            }
        }

        private async void RefreshAfterDelay(TimeSpan delay, int generation)
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }

            var context = mainThreadContext;
            if (context == null)
            {
                return;
            }

            context.Post(_ =>
            {
                if (isShutdown || generation != refreshGeneration)
                {
                    return;
                }

                refreshDueRealtime = double.PositiveInfinity;
                Preload();
                AvailabilityChanged?.Invoke();
            }, null);
        }

        private static RewardedAdQuotaDecision CreateBlockedDecision()
        {
            return new RewardedAdQuotaDecision(
                false,
                RewardedAdQuotaBlockReason.GlobalLimitReached,
                TimeSpan.Zero,
                false,
                0,
                0,
                0,
                0);
        }
    }
}
