using System;

namespace BusPuzzle
{
    internal sealed class RemoteConfigRewardedAdService : IRewardedAdService
    {
        private readonly IRewardedAdService inner;

        public RemoteConfigRewardedAdService(IRewardedAdService inner)
        {
            this.inner = inner;
        }

        public event Action AvailabilityChanged;

        public bool IsReady => RemoteConfigService.AreRewardedAdsEnabled && inner != null && inner.IsReady;
        public string CurrentAdUnitId => inner != null ? inner.CurrentAdUnitId : string.Empty;

        public bool IsReadyFor(RewardedAdPlacement placement)
        {
            return RemoteConfigService.AreRewardedAdsEnabled && inner != null && inner.IsReadyFor(placement);
        }

        public string GetAdUnitId(RewardedAdPlacement placement)
        {
            return inner != null ? inner.GetAdUnitId(placement) : string.Empty;
        }

        public void Initialize()
        {
            RemoteConfigService.ValuesUpdated += HandleRemoteConfigUpdated;
            if (inner != null)
            {
                inner.AvailabilityChanged += HandleInnerAvailabilityChanged;
                inner.Initialize();
            }
        }

        public void Preload()
        {
            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                inner?.Preload();
            }
        }

        public void Preload(RewardedAdPlacement placement)
        {
            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                inner?.Preload(placement);
            }
        }

        public bool ShowStationSlotUnlockAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowIfEnabled(onCompleted, () => inner != null && inner.ShowStationSlotUnlockAd(onCompleted));
        }

        public bool ShowVipBusTeleportAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowIfEnabled(onCompleted, () => inner != null && inner.ShowVipBusTeleportAd(onCompleted));
        }

        public bool ShowBusColorShuffleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowIfEnabled(onCompleted, () => inner != null && inner.ShowBusColorShuffleAd(onCompleted));
        }

        public bool ShowDepartBoostAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowIfEnabled(onCompleted, () => inner != null && inner.ShowDepartBoostAd(onCompleted));
        }

        public bool ShowStageClearDoubleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowIfEnabled(onCompleted, () => inner != null && inner.ShowStageClearDoubleAd(onCompleted));
        }

        private bool ShowIfEnabled(Action<RewardedAdResult> onCompleted, Func<bool> show)
        {
            if (!RemoteConfigService.AreRewardedAdsEnabled)
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                AvailabilityChanged?.Invoke();
                return false;
            }

            return show();
        }

        private void HandleRemoteConfigUpdated()
        {
            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                inner?.Preload();
            }

            AvailabilityChanged?.Invoke();
        }

        private void HandleInnerAvailabilityChanged()
        {
            AvailabilityChanged?.Invoke();
        }
    }
}
