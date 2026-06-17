using UnityEngine;

namespace BusPuzzle
{
    internal sealed class MockBannerAdService : IBannerAdService
    {
        private readonly AdMobSettings settings;
        private int currentStageNumber;
        private bool isShutdown;

        public MockBannerAdService(AdMobSettings settings)
        {
            this.settings = settings != null ? settings : AdMobSettings.Load();
        }

        public bool ShouldReserveSpace(int stageNumber)
        {
            return !isShutdown &&
                RemoteConfigService.AreBannerAdsEnabled &&
                stageNumber >= RemoteConfigService.BannerStartStage;
        }

        public void Initialize()
        {
            if (!isShutdown)
            {
                Debug.Log($"Banner ads are running in mock mode. Active unit: {settings.GetBannerAdUnitId()}");
            }
        }

        public void Shutdown()
        {
            isShutdown = true;
        }

        public void SetStage(int stageNumber)
        {
            currentStageNumber = Mathf.Max(1, stageNumber);
            if (ShouldReserveSpace(currentStageNumber))
            {
                Debug.Log($"Mock banner ad visible from stage {RemoteConfigService.BannerStartStage}: {settings.GetBannerAdUnitId()}");
            }
        }
    }
}
