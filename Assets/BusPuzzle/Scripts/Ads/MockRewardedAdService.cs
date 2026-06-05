using System;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class MockRewardedAdService : IRewardedAdService
    {
        private readonly AdMobSettings settings;

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
                return true;
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
            Debug.Log($"Rewarded ads are running in mock mode. Active unit: {CurrentAdUnitId}");
            AvailabilityChanged?.Invoke();
        }

        public void Preload()
        {
            AvailabilityChanged?.Invoke();
        }

        public void Preload(RewardedAdPlacement placement)
        {
            AvailabilityChanged?.Invoke();
        }

        public bool ShowStationSlotUnlockAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.StationSlotUnlock, AdMobSettings.StationSlotUnlockRewardType, onCompleted);
        }

        public bool ShowVipBusTeleportAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.VipBusTeleport, AdMobSettings.VipBusTeleportRewardType, onCompleted);
        }

        private bool ShowRewardedAd(RewardedAdPlacement placement, string rewardType, Action<RewardedAdResult> onCompleted)
        {
            if (!IsReady)
            {
                Debug.LogError("Rewarded ad SDK is not enabled in this build.");
                onCompleted?.Invoke(RewardedAdResult.Failed);
                return false;
            }

            Debug.Log($"Mock rewarded ad completed: {rewardType} x1 ({GetAdUnitId(placement)})");
            onCompleted?.Invoke(RewardedAdResult.RewardEarned);
            return true;
        }
    }
}
