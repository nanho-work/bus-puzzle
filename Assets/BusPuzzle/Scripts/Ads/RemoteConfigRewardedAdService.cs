using System;

namespace BusPuzzle
{
    internal sealed class RemoteConfigRewardedAdService : IRewardedAdService, IRewardedAdQuotaStatusProvider
    {
        private readonly IRewardedAdService inner;
        private bool isInitialized;
        private bool isInnerInitialized;
        private bool isShutdown;

        public RemoteConfigRewardedAdService(IRewardedAdService inner)
        {
            this.inner = inner;
        }

        public event Action AvailabilityChanged;

        public bool IsReady => !isShutdown && RemoteConfigService.AreRewardedAdsEnabled && inner != null && inner.IsReady;
        public string CurrentAdUnitId => inner != null ? inner.CurrentAdUnitId : string.Empty;

        public bool IsReadyFor(RewardedAdPlacement placement)
        {
            return !isShutdown && RemoteConfigService.AreRewardedAdsEnabled && inner != null && inner.IsReadyFor(placement);
        }

        public string GetAdUnitId(RewardedAdPlacement placement)
        {
            return inner != null ? inner.GetAdUnitId(placement) : string.Empty;
        }

        public RewardedAdQuotaDecision GetQuotaDecision(RewardedAdPlacement placement)
        {
            if (inner is IRewardedAdQuotaStatusProvider quotaStatusProvider)
            {
                return quotaStatusProvider.GetQuotaDecision(placement);
            }

            return new RewardedAdQuotaDecision(
                true,
                RewardedAdQuotaBlockReason.None,
                TimeSpan.Zero,
                true,
                0,
                0,
                0,
                0);
        }

        public void Initialize()
        {
            if (isInitialized || isShutdown)
            {
                return;
            }

            isInitialized = true;
            RemoteConfigService.ValuesUpdated += HandleRemoteConfigUpdated;
        }

        public void Shutdown()
        {
            if (isShutdown)
            {
                return;
            }

            isShutdown = true;
            if (isInitialized)
            {
                RemoteConfigService.ValuesUpdated -= HandleRemoteConfigUpdated;
                if (isInnerInitialized && inner != null)
                {
                    inner.AvailabilityChanged -= HandleInnerAvailabilityChanged;
                }
            }

            inner?.Shutdown();
            AvailabilityChanged = null;
            isInnerInitialized = false;
            isInitialized = false;
        }

        public void Preload()
        {
            if (!isShutdown && RemoteConfigService.AreRewardedAdsEnabled)
            {
                TryInitializeInner();
                inner?.Preload();
            }
        }

        public void Preload(RewardedAdPlacement placement)
        {
            if (!isShutdown && RemoteConfigService.AreRewardedAdsEnabled)
            {
                TryInitializeInner();
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
            if (isShutdown || !RemoteConfigService.AreRewardedAdsEnabled)
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                if (!isShutdown)
                {
                    AvailabilityChanged?.Invoke();
                }

                return false;
            }

            return show();
        }

        private void HandleRemoteConfigUpdated()
        {
            if (isShutdown)
            {
                return;
            }

            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                TryInitializeInner();
                inner?.Preload();
            }

            AvailabilityChanged?.Invoke();
        }

        private void TryInitializeInner()
        {
            if (isShutdown || isInnerInitialized || inner == null || !RemoteConfigService.AreRewardedAdsEnabled)
            {
                return;
            }

            inner.AvailabilityChanged += HandleInnerAvailabilityChanged;
            inner.Initialize();
            isInnerInitialized = true;
        }

        private void HandleInnerAvailabilityChanged()
        {
            if (isShutdown)
            {
                return;
            }

            AvailabilityChanged?.Invoke();
        }
    }
}
