#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BusPuzzle
{
    public static class RewardedAdQuotaSelfTest
    {
        private const string TestKey = "bus_puzzle_rewarded_ad_quota_self_test";
        private const string CancelTestKey = "bus_puzzle_rewarded_ad_quota_cancel_self_test";

        [MenuItem("Bus Puzzle/Validate Rewarded Ad Quota")]
        public static void RunFromMenu()
        {
            Run();
            Debug.Log("Rewarded-ad quota self-test passed.");
        }

        public static void RunBatch()
        {
            Run();
            Debug.Log("Rewarded-ad quota batch self-test passed.");
        }

        private static void Run()
        {
            PlayerPrefs.DeleteKey(TestKey);
            PlayerPrefs.DeleteKey(CancelTestKey);
            PlayerPrefs.Save();

            try
            {
                var now = new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc);
                var start = now;
                var policy = new RewardedAdQuotaPolicy(6, 2, 2, 1, 120);
                var tracker = new RewardedAdQuotaTracker(TestKey, () => now);

                Require(tracker.Evaluate(
                    RewardedAdPlacement.StationSlotUnlock,
                    "main:1",
                    policy).IsAllowed, "Initial request should be allowed.");

                Commit(tracker, RewardedAdPlacement.StationSlotUnlock, "main:1", policy);
                var cooldownDecision = tracker.Evaluate(
                    RewardedAdPlacement.DepartBoost,
                    "main:1",
                    policy);
                Require(
                    cooldownDecision.BlockReason == RewardedAdQuotaBlockReason.CooldownActive,
                    "Cooldown must apply across placements.");

                now = now.AddSeconds(121);
                var samePlacementStageDecision = tracker.Evaluate(
                    RewardedAdPlacement.StationSlotUnlock,
                    "main:1",
                    policy);
                Require(
                    samePlacementStageDecision.BlockReason == RewardedAdQuotaBlockReason.PlacementStageLimitReached,
                    "The same placement must be limited to one accepted show per stage.");

                Commit(tracker, RewardedAdPlacement.DepartBoost, "main:1", policy);
                now = now.AddSeconds(121);
                var stageDecision = tracker.Evaluate(
                    RewardedAdPlacement.BusColorShuffle,
                    "main:1",
                    policy);
                Require(
                    stageDecision.BlockReason == RewardedAdQuotaBlockReason.StageTotalLimitReached,
                    "A stage must stop after two accepted rewarded ads.");

                Commit(tracker, RewardedAdPlacement.BusColorShuffle, "main:2", policy);
                now = now.AddSeconds(121);
                Commit(tracker, RewardedAdPlacement.VipBusTeleport, "main:2", policy);
                now = now.AddSeconds(121);
                Commit(tracker, RewardedAdPlacement.StageClearDouble, "main:3", policy);
                now = now.AddSeconds(121);
                Commit(tracker, RewardedAdPlacement.StationSlotUnlock, "main:3", policy);
                now = now.AddSeconds(121);

                var globalDecision = tracker.Evaluate(
                    RewardedAdPlacement.DepartBoost,
                    "main:4",
                    policy);
                Require(
                    globalDecision.BlockReason == RewardedAdQuotaBlockReason.GlobalLimitReached,
                    "The rolling 24-hour global cap must combine all placements.");

                var reloadedTracker = new RewardedAdQuotaTracker(TestKey, () => now);
                Require(
                    reloadedTracker.Evaluate(
                        RewardedAdPlacement.DepartBoost,
                        "main:4",
                        policy).BlockReason == RewardedAdQuotaBlockReason.GlobalLimitReached,
                    "Committed quota must survive tracker recreation.");

                now = start.AddHours(24);
                Require(
                    reloadedTracker.Evaluate(
                        RewardedAdPlacement.BusColorShuffle,
                        "main:99",
                        policy).IsAllowed,
                    "The oldest accepted show must expire at the rolling 24-hour boundary.");

                now = now.AddMinutes(-10);
                Require(
                    reloadedTracker.Evaluate(
                        RewardedAdPlacement.BusColorShuffle,
                        "main:99",
                        policy).BlockReason == RewardedAdQuotaBlockReason.ClockRollback,
                    "Moving the device clock backwards must not release quota.");

                var cancelTracker = new RewardedAdQuotaTracker(CancelTestKey, () => start);
                RewardedAdQuotaReservation reservation;
                RewardedAdQuotaDecision decision;
                Require(
                    cancelTracker.TryBegin(
                        RewardedAdPlacement.DepartBoost,
                        "daily:20260823:1",
                        policy,
                        out reservation,
                        out decision),
                    "A fresh reservation should be created.");
                Require(cancelTracker.Cancel(reservation), "Cancel should release a valid reservation.");
                Require(!cancelTracker.Cancel(reservation), "A reservation must be single-use.");
                Require(
                    cancelTracker.Evaluate(
                        RewardedAdPlacement.DepartBoost,
                        "daily:20260823:1",
                        policy).IsAllowed,
                    "A canceled show must not consume quota.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(TestKey);
                PlayerPrefs.DeleteKey(CancelTestKey);
                PlayerPrefs.Save();
            }
        }

        private static void Commit(
            RewardedAdQuotaTracker tracker,
            RewardedAdPlacement placement,
            string stageContext,
            RewardedAdQuotaPolicy policy)
        {
            RewardedAdQuotaReservation reservation;
            RewardedAdQuotaDecision decision;
            Require(
                tracker.TryBegin(
                    placement,
                    stageContext,
                    policy,
                    out reservation,
                    out decision),
                $"Reservation should be allowed for {placement} at {stageContext}; block={decision.BlockReason}.");
            Require(tracker.Commit(reservation), "A valid reservation should commit once.");
            Require(!tracker.Commit(reservation), "A committed reservation must be single-use.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
#endif
