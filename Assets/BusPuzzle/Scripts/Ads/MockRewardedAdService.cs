using System;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class MockRewardedAdService : IRewardedAdService
    {
        private readonly AdMobSettings settings;
        private bool isShutdown;

        public MockRewardedAdService(AdMobSettings settings)
        {
            this.settings = settings != null ? settings : AdMobSettings.Load();
        }

        public event Action AvailabilityChanged;

        public bool IsReady
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return !isShutdown;
#else
                return false;
#endif
            }
        }

        public string CurrentAdUnitId => settings.GetRewardedAdUnitId();

        public bool IsReadyFor(RewardedAdPlacement placement)
        {
            return IsReady;
        }

        public string GetAdUnitId(RewardedAdPlacement placement)
        {
            return settings.GetRewardedAdUnitId(placement);
        }

        public void Initialize()
        {
            if (isShutdown)
            {
                return;
            }

            Debug.Log($"Rewarded ads are running in mock mode. Active unit: {CurrentAdUnitId}");
            AvailabilityChanged?.Invoke();
        }

        public void Shutdown()
        {
            isShutdown = true;
            AvailabilityChanged = null;
        }

        public void Preload()
        {
            if (!isShutdown)
            {
                AvailabilityChanged?.Invoke();
            }
        }

        public void Preload(RewardedAdPlacement placement)
        {
            if (!isShutdown)
            {
                AvailabilityChanged?.Invoke();
            }
        }

        public bool ShowStationSlotUnlockAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.StationSlotUnlock, AdMobSettings.StationSlotUnlockRewardType, onCompleted);
        }

        public bool ShowVipBusTeleportAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.VipBusTeleport, AdMobSettings.VipBusTeleportRewardType, onCompleted);
        }

        public bool ShowBusColorShuffleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.BusColorShuffle, AdMobSettings.BusColorShuffleRewardType, onCompleted);
        }

        public bool ShowDepartBoostAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.DepartBoost, AdMobSettings.DepartBoostRewardType, onCompleted);
        }

        public bool ShowStageClearDoubleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.StageClearDouble, AdMobSettings.StageClearDoubleRewardType, onCompleted);
        }

        private bool ShowRewardedAd(RewardedAdPlacement placement, string rewardType, Action<RewardedAdResult> onCompleted)
        {
            if (!IsReady)
            {
                if (!isShutdown)
                {
                    Debug.LogError("Rewarded ad SDK is not enabled in this build.");
                }

                onCompleted?.Invoke(isShutdown ? RewardedAdResult.NotReady : RewardedAdResult.Failed);
                return false;
            }

            Debug.Log($"Mock rewarded ad completed: {rewardType} x1 ({GetAdUnitId(placement)})");
            onCompleted?.Invoke(RewardedAdResult.RewardEarned);
            return true;
        }
    }
}
