#if BUS_PUZZLE_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class AdMobBannerAdService : IBannerAdService
    {
        private readonly AdMobSettings settings;
        private BannerView bannerView;
        private int currentStageNumber = 1;
        private bool isInitialized;
        private bool isLoading;
        private bool isLoaded;
        private bool isShutdown;

        public AdMobBannerAdService(AdMobSettings settings)
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
            if (isInitialized || isShutdown)
            {
                return;
            }

            AdMobSdkInitializer.Initialize(() =>
            {
                if (isShutdown)
                {
                    return;
                }

                isInitialized = true;
                RefreshBanner();
            });
        }

        public void Shutdown()
        {
            if (isShutdown)
            {
                return;
            }

            isShutdown = true;
            isInitialized = false;
            isLoading = false;
            DestroyBanner();
        }

        public void SetStage(int stageNumber)
        {
            currentStageNumber = Mathf.Max(1, stageNumber);
            RefreshBanner();
        }

        private void RefreshBanner()
        {
            if (isShutdown)
            {
                return;
            }

            if (!ShouldReserveSpace(currentStageNumber))
            {
                HideBanner();
                return;
            }

            if (!isInitialized)
            {
                return;
            }

            if (bannerView == null)
            {
                CreateBanner();
                if (bannerView == null)
                {
                    return;
                }
            }

            if (isLoaded)
            {
                bannerView.Show();
                return;
            }

            if (!isLoading)
            {
                isLoading = true;
                bannerView.LoadAd(new AdRequest());
            }
        }

        private void CreateBanner()
        {
            DestroyBanner();

            var adUnitId = settings.GetBannerAdUnitId();
            if (!AdMobSettings.LooksLikeAdUnitId(adUnitId))
            {
                Debug.LogError($"Banner ad unit ID is invalid: {adUnitId}");
                return;
            }

            bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
            isLoaded = false;
            bannerView.Hide();
            bannerView.OnBannerAdLoaded += HandleBannerLoaded;
            bannerView.OnBannerAdLoadFailed += HandleBannerLoadFailed;
        }

        private void HandleBannerLoaded()
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoading = false;
                if (isShutdown || bannerView == null)
                {
                    return;
                }

                isLoaded = true;
                if (ShouldReserveSpace(currentStageNumber))
                {
                    bannerView.Show();
                }
                else
                {
                    bannerView.Hide();
                }
            });
        }

        private void HandleBannerLoadFailed(LoadAdError error)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoading = false;
                isLoaded = false;
                Debug.LogWarning($"Banner ad failed to load: {error}");
                HideBanner();
            });
        }

        private void HideBanner()
        {
            if (bannerView != null)
            {
                bannerView.Hide();
            }
        }

        private void DestroyBanner()
        {
            if (bannerView == null)
            {
                return;
            }

            bannerView.Destroy();
            bannerView = null;
            isLoading = false;
            isLoaded = false;
        }
    }
}
#endif
