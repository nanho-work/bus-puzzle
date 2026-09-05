#if BUS_PUZZLE_LEVELPLAY
using System;
using System.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class LevelPlayBannerAdService : IBannerAdService
    {
        private const int MaximumAutomaticLoadRetries = 4;

        private readonly LevelPlaySettings settings;
        private LevelPlayBannerAd bannerAd;
        private int currentStageNumber = 1;
        private bool isInitialized;
        private bool isSdkInitialized;
        private bool isSdkInitializing;
        private bool isLoading;
        private bool isLoaded;
        private bool isVisible;
        private bool isShutdown;
        private int consecutiveLoadFailures;
        private int retryGeneration;

        public LevelPlayBannerAdService(LevelPlaySettings settings)
        {
            this.settings = settings != null ? settings : LevelPlaySettings.Load();
        }

        public bool ShouldReserveSpace(int stageNumber)
        {
            // Reserve the slot before the asynchronous banner load completes so the
            // bottom booster row cannot be covered when the native view appears.
            return !isShutdown && IsEligible(stageNumber);
        }

        public void Initialize()
        {
            if (isInitialized || isShutdown)
            {
                return;
            }

            isInitialized = true;
            RefreshBanner();
        }

        public void Shutdown()
        {
            if (isShutdown)
            {
                return;
            }

            isShutdown = true;
            isInitialized = false;
            isSdkInitialized = false;
            isSdkInitializing = false;
            isLoading = false;
            isLoaded = false;
            isVisible = false;
            retryGeneration++;
            DestroyBanner();
        }

        public void SetStage(int stageNumber)
        {
            currentStageNumber = Mathf.Max(1, stageNumber);
            RefreshBanner();
        }

        private bool IsEligible(int stageNumber)
        {
            return RemoteConfigService.AreBannerAdsEnabled &&
                stageNumber >= RemoteConfigService.BannerStartStage;
        }

        private void RefreshBanner()
        {
            if (isShutdown || !isInitialized)
            {
                return;
            }

            if (!IsEligible(currentStageNumber))
            {
                HideBanner();
                return;
            }

            if (!isSdkInitialized)
            {
                EnsureSdkInitialized();
                return;
            }

            EnsureBannerCreated();
            if (bannerAd == null)
            {
                return;
            }

            if (isLoaded)
            {
                ShowBanner();
            }
            else if (!isLoading)
            {
                LoadBanner();
            }
        }

        private void EnsureSdkInitialized()
        {
            if (isShutdown || isSdkInitialized || isSdkInitializing)
            {
                return;
            }

            isSdkInitializing = true;
            LevelPlaySdkInitializer.Initialize(settings, succeeded =>
            {
                if (isShutdown)
                {
                    return;
                }

                isSdkInitializing = false;
                isSdkInitialized = succeeded;
                if (succeeded)
                {
                    RefreshBanner();
                }
                else
                {
                    ScheduleInitializationRetry();
                }
            });
        }

        private void EnsureBannerCreated()
        {
            if (bannerAd != null || isShutdown)
            {
                return;
            }

            var adUnitId = settings.GetBannerAdUnitId();
            if (!LevelPlaySettings.LooksLikeLevelPlayAdUnitId(adUnitId))
            {
                Debug.LogError($"LevelPlay banner ad unit ID is invalid: {adUnitId}");
                return;
            }

            var builder = new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.BANNER)
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(false)
                // GameUiController already owns the safe-area and banner-slot layout.
                // Letting the Android native banner also react to WindowInsets can move
                // it between two Y positions when the ad creative auto-refreshes.
                .SetRespectSafeArea(false);
            if (!string.IsNullOrWhiteSpace(settings.BannerPlacementName))
            {
                builder.SetPlacementName(settings.BannerPlacementName);
            }

            bannerAd = new LevelPlayBannerAd(adUnitId, builder.Build());
            bannerAd.OnAdLoaded += HandleBannerLoaded;
            bannerAd.OnAdLoadFailed += HandleBannerLoadFailed;
            bannerAd.OnAdDisplayFailed += HandleBannerDisplayFailed;
        }

        private void LoadBanner()
        {
            if (bannerAd == null || isLoading || isShutdown)
            {
                return;
            }

            isLoading = true;
            try
            {
                bannerAd.LoadAd();
            }
            catch (Exception exception)
            {
                isLoading = false;
                Debug.LogWarning($"LevelPlay banner failed to start loading: {exception}");
                RegisterLoadFailure();
            }
        }

        private void ShowBanner()
        {
            if (bannerAd == null || isVisible || !isLoaded || isShutdown)
            {
                return;
            }

            try
            {
                bannerAd.ResumeAutoRefresh();
                bannerAd.ShowAd();
                isVisible = true;
            }
            catch (Exception exception)
            {
                isVisible = false;
                isLoaded = false;
                Debug.LogWarning($"LevelPlay banner failed to show: {exception}");
                RegisterLoadFailure();
            }
        }

        private void HideBanner()
        {
            if (bannerAd == null)
            {
                isVisible = false;
                return;
            }

            try
            {
                bannerAd.HideAd();
                bannerAd.PauseAutoRefresh();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"LevelPlay banner failed to hide cleanly: {exception.Message}");
            }

            isVisible = false;
        }

        private void HandleBannerLoaded(LevelPlayAdInfo adInfo)
        {
            if (isShutdown)
            {
                return;
            }

            isLoading = false;
            isLoaded = true;
            consecutiveLoadFailures = 0;
            retryGeneration++;
            if (IsEligible(currentStageNumber))
            {
                ShowBanner();
            }
            else
            {
                HideBanner();
            }
        }

        private void HandleBannerLoadFailed(LevelPlayAdError error)
        {
            if (isShutdown)
            {
                return;
            }

            isLoading = false;
            isLoaded = false;
            isVisible = false;
            Debug.LogWarning($"LevelPlay banner failed to load: {error}");
            RegisterLoadFailure();
        }

        private void HandleBannerDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            if (isShutdown)
            {
                return;
            }

            isLoaded = false;
            isVisible = false;
            Debug.LogWarning($"LevelPlay banner failed to display: {error}");
            RegisterLoadFailure();
        }

        private void RegisterLoadFailure()
        {
            consecutiveLoadFailures++;
            retryGeneration++;
            if (consecutiveLoadFailures <= MaximumAutomaticLoadRetries)
            {
                RetryLoadAfterDelay(retryGeneration, GetRetryDelaySeconds(consecutiveLoadFailures));
            }
        }

        private async void RetryLoadAfterDelay(int generation, int delaySeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            if (isShutdown || generation != retryGeneration || !IsEligible(currentStageNumber))
            {
                return;
            }

            RefreshBanner();
        }

        private async void ScheduleInitializationRetry()
        {
            var generation = ++retryGeneration;
            await Task.Delay(TimeSpan.FromSeconds(RemoteConfigService.RewardedRetryBaseSeconds));
            if (isShutdown || generation != retryGeneration || !IsEligible(currentStageNumber))
            {
                return;
            }

            EnsureSdkInitialized();
        }

        private static int GetRetryDelaySeconds(int consecutiveFailures)
        {
            var baseSeconds = RemoteConfigService.RewardedRetryBaseSeconds;
            var maxSeconds = RemoteConfigService.RewardedRetryMaxSeconds;
            var multiplier = 1 << Mathf.Clamp(consecutiveFailures - 1, 0, 4);
            return Mathf.Clamp(baseSeconds * multiplier, baseSeconds, maxSeconds);
        }

        private void DestroyBanner()
        {
            if (bannerAd == null)
            {
                return;
            }

            bannerAd.OnAdLoaded -= HandleBannerLoaded;
            bannerAd.OnAdLoadFailed -= HandleBannerLoadFailed;
            bannerAd.OnAdDisplayFailed -= HandleBannerDisplayFailed;
            bannerAd.DestroyAd();
            bannerAd = null;
        }
    }
}
#endif
