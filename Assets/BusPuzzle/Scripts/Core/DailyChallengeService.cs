using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BusPuzzle
{
    internal enum DailyChallengeStepState
    {
        Locked = 0,
        Available = 1,
        Cleared = 2,
        RewardClaimed = 3
    }

    internal readonly struct DailyChallengeReward
    {
        public readonly int Gold;
        public readonly int AdSkipTickets;

        public DailyChallengeReward(int gold, int adSkipTickets)
        {
            Gold = Mathf.Max(0, gold);
            AdSkipTickets = Mathf.Max(0, adSkipTickets);
        }

        public bool HasReward => Gold > 0 || AdSkipTickets > 0;
    }

    internal readonly struct DailyChallengeStepSnapshot
    {
        public readonly int StepIndex;
        public readonly int VehicleCount;
        public readonly int ColorCount;
        public readonly int PassengerBatchSize;
        public readonly bool HasMysteryVehicles;
        public readonly DailyChallengeReward Reward;
        public readonly DailyChallengeStepState State;

        public DailyChallengeStepSnapshot(
            int stepIndex,
            int vehicleCount,
            int colorCount,
            int passengerBatchSize,
            bool hasMysteryVehicles,
            DailyChallengeReward reward,
            DailyChallengeStepState state)
        {
            StepIndex = Mathf.Clamp(stepIndex, 1, 3);
            VehicleCount = Mathf.Max(1, vehicleCount);
            ColorCount = Mathf.Max(1, colorCount);
            PassengerBatchSize = Mathf.Max(1, passengerBatchSize);
            HasMysteryVehicles = hasMysteryVehicles;
            Reward = reward;
            State = state;
        }
    }

    internal static class DailyChallengeService
    {
        private const int StepCount = 3;
        private const string ActiveDateKey = "bus_puzzle_daily_challenge_active_date_v1";
        private const string StepStatePrefix = "bus_puzzle_daily_challenge_step_state_v1_";
        private const string AvailableNotificationSeenDateKey = "bus_puzzle_daily_challenge_available_seen_date_v1";

        private static readonly LevelData[] RuntimeLevelCache = new LevelData[StepCount];
        private static string runtimeLevelCacheDateKey;

        private static readonly DailyChallengeStepSnapshot[] StepConfigs =
        {
            new DailyChallengeStepSnapshot(
                1,
                45,
                4,
                2,
                false,
                new DailyChallengeReward(50, 0),
                DailyChallengeStepState.Locked),
            new DailyChallengeStepSnapshot(
                2,
                65,
                5,
                1,
                true,
                new DailyChallengeReward(0, 1),
                DailyChallengeStepState.Locked),
            new DailyChallengeStepSnapshot(
                3,
                80,
                6,
                1,
                true,
                new DailyChallengeReward(50, 1),
                DailyChallengeStepState.Locked)
        };

        public static string CurrentDateKey
        {
            get
            {
                EnsureToday();
                return GetTodayKey();
            }
        }

        public static bool HasClaimableReward
        {
            get
            {
                EnsureToday();
                return HasClaimableRewardForToday();
            }
        }

        public static bool HasAvailableAction
        {
            get
            {
                EnsureToday();
                for (var step = 1; step <= StepCount; step++)
                {
                    var state = GetStepState(step);
                    if (state == DailyChallengeStepState.Available ||
                        state == DailyChallengeStepState.Cleared)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static bool HasPendingNotification
        {
            get
            {
                EnsureToday();
                if (HasClaimableRewardForToday())
                {
                    return true;
                }

                return HasAvailableStartForToday() &&
                       !HasSeenAvailableNotificationToday();
            }
        }

        public static DailyChallengeStepSnapshot[] GetTodaySteps()
        {
            EnsureToday();

            var snapshots = new DailyChallengeStepSnapshot[StepCount];
            for (var index = 0; index < StepCount; index++)
            {
                var config = StepConfigs[index];
                snapshots[index] = new DailyChallengeStepSnapshot(
                    config.StepIndex,
                    config.VehicleCount,
                    config.ColorCount,
                    config.PassengerBatchSize,
                    config.HasMysteryVehicles,
                    config.Reward,
                    GetStepState(config.StepIndex));
            }

            return snapshots;
        }

        public static bool CanStartStep(int stepIndex)
        {
            EnsureToday();
            return IsValidStep(stepIndex) &&
                   GetStepState(stepIndex) == DailyChallengeStepState.Available;
        }

        public static bool MarkStepCleared(int stepIndex)
        {
            EnsureToday();
            if (!CanStartStep(stepIndex))
            {
                return false;
            }

            SetStepState(stepIndex, DailyChallengeStepState.Cleared);

            var nextStep = stepIndex + 1;
            if (IsValidStep(nextStep) &&
                GetStepState(nextStep) == DailyChallengeStepState.Locked)
            {
                SetStepState(nextStep, DailyChallengeStepState.Available);
            }

            PlayerPrefs.Save();
            return true;
        }

        public static LevelData CreateRuntimeLevel(int stepIndex)
        {
            EnsureToday();
            if (!IsValidStep(stepIndex))
            {
                return null;
            }

            var todayKey = GetTodayKey();
            if (runtimeLevelCacheDateKey == todayKey &&
                RuntimeLevelCache[stepIndex - 1] != null)
            {
                return RuntimeLevelCache[stepIndex - 1];
            }

            if (runtimeLevelCacheDateKey != todayKey)
            {
                ClearRuntimeLevelCache();
                runtimeLevelCacheDateKey = todayKey;
            }

            var config = StepConfigs[stepIndex - 1];
            var difficulty = GetDifficulty(stepIndex);
            var passengerRule = CreatePassengerFlowRule(difficulty, config.PassengerBatchSize);
            var profile = LevelDifficultyProfile.CreateCustom(
                difficulty,
                passengerRule,
                config.VehicleCount,
                config.ColorCount,
                GetParkingTension(stepIndex),
                GetStationPressure(stepIndex),
                true);
            var seed = GetDailySeed(stepIndex);
            var vehicles = LevelGenerator.BuildVehicles(
                profile,
                seed + 313,
                config.VehicleCount,
                null,
                80,
                true,
                GetLayoutVariantIndex(stepIndex));
            if (vehicles == null || vehicles.Count == 0)
            {
                return null;
            }

            if (config.HasMysteryVehicles)
            {
                vehicles = ApplyChallengeMysteryVehicles(vehicles, seed + 1699, stepIndex);
            }

            var flowPlan = LevelGenerator.BuildPassengerFlowPlan(profile, vehicles, seed + 911);
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = HideFlags.DontSave;
            level.ConfigureWithPassengerFlowPlan(
                $"Daily Challenge {stepIndex}",
                profile,
                flowPlan,
                vehicles,
                LevelGenerator.GetRotaryCapacity(difficulty),
                LevelGenerator.GetRoadPreset(difficulty),
                null,
                null,
                LevelPresentationMode.DailyChallengeEvent);
            RuntimeLevelCache[stepIndex - 1] = level;
            return level;
        }

        public static void PreloadRuntimeLevel(int stepIndex)
        {
            var level = CreateRuntimeLevel(stepIndex);
            if (level == null)
            {
                return;
            }

            GC.KeepAlive(level.PassengerUnits);
            GC.KeepAlive(level.AllVehicles);
        }

        public static bool IsCurrentDateKey(string dateKey)
        {
            return !string.IsNullOrEmpty(dateKey) &&
                   string.Equals(dateKey, GetTodayKey(), StringComparison.Ordinal);
        }

        public static void MarkAvailableNotificationSeen()
        {
            EnsureToday();
            PlayerPrefs.SetString(AvailableNotificationSeenDateKey, GetTodayKey());
            PlayerPrefs.Save();
        }

        public static bool TryClaimReward(int stepIndex, out DailyChallengeStepSnapshot claimedStep)
        {
            EnsureToday();
            claimedStep = default;
            if (!IsValidStep(stepIndex) ||
                GetStepState(stepIndex) != DailyChallengeStepState.Cleared)
            {
                return false;
            }

            var config = StepConfigs[stepIndex - 1];
            if (config.Reward.Gold > 0)
            {
                UserEconomy.AddGold(config.Reward.Gold);
            }

            if (config.Reward.AdSkipTickets > 0)
            {
                UserEconomy.AddAdSkipTickets(config.Reward.AdSkipTickets);
            }

            SetStepState(stepIndex, DailyChallengeStepState.RewardClaimed);
            PlayerPrefs.Save();

            claimedStep = new DailyChallengeStepSnapshot(
                config.StepIndex,
                config.VehicleCount,
                config.ColorCount,
                config.PassengerBatchSize,
                config.HasMysteryVehicles,
                config.Reward,
                DailyChallengeStepState.RewardClaimed);
            return true;
        }

        private static void EnsureToday()
        {
            var todayKey = GetTodayKey();
            if (PlayerPrefs.GetString(ActiveDateKey, string.Empty) == todayKey)
            {
                return;
            }

            PlayerPrefs.SetString(ActiveDateKey, todayKey);
            ClearRuntimeLevelCache();
            SetStepState(1, DailyChallengeStepState.Available);
            SetStepState(2, DailyChallengeStepState.Locked);
            SetStepState(3, DailyChallengeStepState.Locked);
            PlayerPrefs.Save();
        }

        private static void ClearRuntimeLevelCache()
        {
            for (var index = 0; index < RuntimeLevelCache.Length; index++)
            {
                var cachedLevel = RuntimeLevelCache[index];
                if (cachedLevel != null)
                {
                    UnityEngine.Object.Destroy(cachedLevel);
                    RuntimeLevelCache[index] = null;
                }
            }
        }

        private static bool HasClaimableRewardForToday()
        {
            for (var step = 1; step <= StepCount; step++)
            {
                if (GetStepState(step) == DailyChallengeStepState.Cleared)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAvailableStartForToday()
        {
            for (var step = 1; step <= StepCount; step++)
            {
                if (GetStepState(step) == DailyChallengeStepState.Available)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSeenAvailableNotificationToday()
        {
            return string.Equals(
                PlayerPrefs.GetString(AvailableNotificationSeenDateKey, string.Empty),
                GetTodayKey(),
                StringComparison.Ordinal);
        }

        private static DailyChallengeStepState GetStepState(int stepIndex)
        {
            if (!IsValidStep(stepIndex))
            {
                return DailyChallengeStepState.Locked;
            }

            var value = PlayerPrefs.GetInt(GetStepStateKey(stepIndex), stepIndex == 1
                ? (int)DailyChallengeStepState.Available
                : (int)DailyChallengeStepState.Locked);
            return IsValidState(value)
                ? (DailyChallengeStepState)value
                : DailyChallengeStepState.Locked;
        }

        private static void SetStepState(int stepIndex, DailyChallengeStepState state)
        {
            if (!IsValidStep(stepIndex))
            {
                return;
            }

            PlayerPrefs.SetInt(GetStepStateKey(stepIndex), (int)state);
        }

        private static string GetStepStateKey(int stepIndex)
        {
            return $"{StepStatePrefix}{stepIndex}";
        }

        private static bool IsValidStep(int stepIndex)
        {
            return stepIndex >= 1 && stepIndex <= StepCount;
        }

        private static bool IsValidState(int value)
        {
            return value >= (int)DailyChallengeStepState.Locked &&
                   value <= (int)DailyChallengeStepState.RewardClaimed;
        }

        private static LevelDifficulty GetDifficulty(int stepIndex)
        {
            switch (stepIndex)
            {
                case 2:
                    return LevelDifficulty.Hard;
                case 3:
                    return LevelDifficulty.SuperHard;
                default:
                    return LevelDifficulty.Normal;
            }
        }

        private static PassengerFlowDifficultyRule CreatePassengerFlowRule(
            LevelDifficulty difficulty,
            int passengerBatchSize)
        {
            var defaultRule = PassengerFlowDifficultyRule.DefaultFor(difficulty);
            var groupUnits = Mathf.Clamp(passengerBatchSize, 1, 4);
            return new PassengerFlowDifficultyRule(
                defaultRule.MinMainGroupRatio,
                defaultRule.MaxMainGroupRatio,
                groupUnits,
                groupUnits,
                defaultRule.InterferenceRatio,
                true);
        }

        private static float GetParkingTension(int stepIndex)
        {
            switch (stepIndex)
            {
                case 2:
                    return 0.60f;
                case 3:
                    return 0.78f;
                default:
                    return 0.42f;
            }
        }

        private static float GetStationPressure(int stepIndex)
        {
            switch (stepIndex)
            {
                case 2:
                    return 0.58f;
                case 3:
                    return 0.76f;
                default:
                    return 0.38f;
            }
        }

        private static int GetLayoutVariantIndex(int stepIndex)
        {
            switch (stepIndex)
            {
                case 3:
                    return 2;
                default:
                    return 0;
            }
        }

        private static int GetDailySeed(int stepIndex)
        {
            var todayKey = GetTodayKey();
            int.TryParse(todayKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dateSeed);
            unchecked
            {
                return dateSeed * 31 + stepIndex * 7919;
            }
        }

        private static string GetTodayKey()
        {
            return DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        private static List<BusDefinition> ApplyChallengeMysteryVehicles(
            IReadOnlyList<BusDefinition> vehicles,
            int seed,
            int stepIndex)
        {
            var result = vehicles != null
                ? new List<BusDefinition>(vehicles)
                : new List<BusDefinition>();
            if (result.Count == 0)
            {
                return result;
            }

            for (var index = 0; index < result.Count; index++)
            {
                result[index] = result[index].WithStartsConcealed(false);
            }

            var candidates = new List<int>();
            for (var index = 3; index < result.Count; index++)
            {
                candidates.Add(index);
            }

            if (candidates.Count == 0)
            {
                return result;
            }

            var targetRatio = stepIndex >= 3 ? 0.18f : 0.12f;
            var targetCount = Mathf.Clamp(
                Mathf.RoundToInt(result.Count * targetRatio),
                2,
                Mathf.Min(stepIndex >= 3 ? 10 : 7, candidates.Count));
            Shuffle(candidates, new System.Random(seed));
            for (var index = 0; index < targetCount; index++)
            {
                var vehicleIndex = candidates[index];
                result[vehicleIndex] = result[vehicleIndex].WithStartsConcealed(true);
            }

            return result;
        }

        private static void Shuffle<T>(IList<T> values, System.Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(0, index + 1);
                var temp = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }
    }
}
