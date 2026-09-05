#if BUS_PUZZLE_LEVELPLAY
using System;
using System.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class LevelPlayRewardedAdService : IRewardedAdService
    {
        private const int ShowCallbackTimeoutSeconds = 120;
        private const int RewardCallbackGraceMilliseconds = 500;
        private const int MaximumAutomaticLoadRetries = 4;

        private readonly LevelPlaySettings settings;
        private LevelPlayRewardedAd rewardedAd;
        private Action<RewardedAdResult> pendingCompletion;
        private bool isSdkInitialized;
        private bool isInitializing;
        private bool isLoading;
        private bool isShowing;
        private bool rewardEarned;
        private bool closeReceived;
        private bool isShutdown;
        private int consecutiveLoadFailures;
        private int loadGeneration;
        private int showAttemptId;

        public LevelPlayRewardedAdService(LevelPlaySettings settings)
        {
            this.settings = settings != null ? settings : LevelPlaySettings.Load();
        }

        public event Action AvailabilityChanged;

        public bool IsReady => IsReadyFor(RewardedAdPlacement.StationSlotUnlock);
        public string CurrentAdUnitId => settings.GetRewardedAdUnitId();

        public bool IsReadyFor(RewardedAdPlacement placement)
        {
            if (isShutdown || isShowing || rewardedAd == null || !rewardedAd.IsAdReady())
            {
                return false;
            }

            var placementName = settings.GetRewardedPlacementName(placement);
            return string.IsNullOrWhiteSpace(placementName) ||
                !LevelPlayRewardedAd.IsPlacementCapped(placementName);
        }

        public string GetAdUnitId(RewardedAdPlacement placement)
        {
            return CurrentAdUnitId;
        }

        public void Initialize()
        {
            EnsureSdkInitialized();
        }

        public void Shutdown()
        {
            if (isShutdown)
            {
                return;
            }

            isShutdown = true;
            isInitializing = false;
            isSdkInitialized = false;
            isLoading = false;
            isShowing = false;
            pendingCompletion = null;
            rewardEarned = false;
            closeReceived = false;
            loadGeneration++;
            showAttemptId++;
            DestroyRewardedAd();
            AvailabilityChanged = null;
        }

        public void Preload()
        {
            if (isShutdown)
            {
                return;
            }

            if (!isSdkInitialized)
            {
                EnsureSdkInitialized();
                return;
            }

            EnsureRewardedAdCreated();
            LoadAdIfNeeded();
        }

        public void Preload(RewardedAdPlacement placement)
        {
            Preload();
        }

        public bool ShowStationSlotUnlockAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.StationSlotUnlock, onCompleted);
        }

        public bool ShowVipBusTeleportAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.VipBusTeleport, onCompleted);
        }

        public bool ShowBusColorShuffleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.BusColorShuffle, onCompleted);
        }

        public bool ShowDepartBoostAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.DepartBoost, onCompleted);
        }

        public bool ShowStageClearDoubleAd(Action<RewardedAdResult> onCompleted)
        {
            return ShowRewardedAd(RewardedAdPlacement.StageClearDouble, onCompleted);
        }

        private void EnsureSdkInitialized()
        {
            if (isShutdown || isSdkInitialized || isInitializing)
            {
                return;
            }

            isInitializing = true;
            LevelPlaySdkInitializer.Initialize(settings, succeeded =>
            {
                if (isShutdown)
                {
                    return;
                }

                isInitializing = false;
                isSdkInitialized = succeeded;
                if (succeeded)
                {
                    EnsureRewardedAdCreated();
                    LoadAdIfNeeded();
                }
                else
                {
                    ScheduleInitializationRetry();
                }

                AvailabilityChanged?.Invoke();
            });
        }

        private void EnsureRewardedAdCreated()
        {
            if (rewardedAd != null || isShutdown)
            {
                return;
            }

            var adUnitId = settings.GetRewardedAdUnitId();
            if (!LevelPlaySettings.LooksLikeLevelPlayAdUnitId(adUnitId))
            {
                Debug.LogError($"LevelPlay rewarded ad unit ID is invalid: {adUnitId}");
                return;
            }

            rewardedAd = new LevelPlayRewardedAd(adUnitId);
            rewardedAd.OnAdLoaded += HandleAdLoaded;
            rewardedAd.OnAdLoadFailed += HandleAdLoadFailed;
            rewardedAd.OnAdDisplayFailed += HandleAdDisplayFailed;
            rewardedAd.OnAdRewarded += HandleAdRewarded;
            rewardedAd.OnAdClosed += HandleAdClosed;
        }

        private void DestroyRewardedAd()
        {
            if (rewardedAd == null)
            {
                return;
            }

            rewardedAd.OnAdLoaded -= HandleAdLoaded;
            rewardedAd.OnAdLoadFailed -= HandleAdLoadFailed;
            rewardedAd.OnAdDisplayFailed -= HandleAdDisplayFailed;
            rewardedAd.OnAdRewarded -= HandleAdRewarded;
            rewardedAd.OnAdClosed -= HandleAdClosed;
            rewardedAd.DestroyAd();
            rewardedAd = null;
        }

        private void LoadAdIfNeeded()
        {
            if (isShutdown || !isSdkInitialized || rewardedAd == null || isLoading || isShowing || rewardedAd.IsAdReady())
            {
                return;
            }

            isLoading = true;
            try
            {
                rewardedAd.LoadAd();
            }
            catch (Exception exception)
            {
                isLoading = false;
                Debug.LogWarning($"LevelPlay rewarded ad failed to start loading: {exception}");
                RegisterLoadFailure();
            }
        }

        private bool ShowRewardedAd(RewardedAdPlacement placement, Action<RewardedAdResult> onCompleted)
        {
            if (!IsReadyFor(placement))
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                Preload();
                return false;
            }

            pendingCompletion = onCompleted;
            isShowing = true;
            rewardEarned = false;
            closeReceived = false;
            AvailabilityChanged?.Invoke();

            var attemptId = ++showAttemptId;
            try
            {
                var placementName = settings.GetRewardedPlacementName(placement);
                rewardedAd.ShowAd(string.IsNullOrWhiteSpace(placementName) ? null : placementName);
                CompleteAfterShowTimeout(attemptId);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"LevelPlay rewarded ad failed to show for {placement}: {exception}");
                CompletePendingReward(RewardedAdResult.Failed);
                return false;
            }
        }

        private void HandleAdLoaded(LevelPlayAdInfo adInfo)
        {
            if (isShutdown)
            {
                return;
            }

            isLoading = false;
            consecutiveLoadFailures = 0;
            loadGeneration++;
            AvailabilityChanged?.Invoke();
        }

        private void HandleAdLoadFailed(LevelPlayAdError error)
        {
            if (isShutdown)
            {
                return;
            }

            isLoading = false;
            Debug.LogWarning($"LevelPlay rewarded ad failed to load: {error}");
            RegisterLoadFailure();
            AvailabilityChanged?.Invoke();
        }

        private void HandleAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            if (!isShowing || isShutdown)
            {
                return;
            }

            Debug.LogWarning($"LevelPlay rewarded ad failed to display: {error}");
            CompletePendingReward(RewardedAdResult.Failed);
        }

        private void HandleAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            if (!isShowing || isShutdown)
            {
                return;
            }

            rewardEarned = true;
            if (closeReceived)
            {
                CompletePendingReward(RewardedAdResult.RewardEarned);
            }
        }

        private void HandleAdClosed(LevelPlayAdInfo adInfo)
        {
            if (!isShowing || isShutdown)
            {
                return;
            }

            closeReceived = true;
            if (rewardEarned)
            {
                CompletePendingReward(RewardedAdResult.RewardEarned);
                return;
            }

            CompleteAfterRewardCallbackGrace(showAttemptId);
        }

        private async void CompleteAfterRewardCallbackGrace(int attemptId)
        {
            await Task.Delay(RewardCallbackGraceMilliseconds);
            if (isShutdown || !isShowing || attemptId != showAttemptId || pendingCompletion == null)
            {
                return;
            }

            CompletePendingReward(rewardEarned
                ? RewardedAdResult.RewardEarned
                : RewardedAdResult.ClosedWithoutReward);
        }

        private async void CompleteAfterShowTimeout(int attemptId)
        {
            await Task.Delay(TimeSpan.FromSeconds(ShowCallbackTimeoutSeconds));
            if (isShutdown || !isShowing || attemptId != showAttemptId || pendingCompletion == null)
            {
                return;
            }

            Debug.LogWarning("LevelPlay rewarded ad timed out while waiting for completion callbacks.");
            CompletePendingReward(rewardEarned ? RewardedAdResult.RewardEarned : RewardedAdResult.Failed);
        }

        private void CompletePendingReward(RewardedAdResult result)
        {
            var callback = pendingCompletion;
            pendingCompletion = null;
            isShowing = false;
            rewardEarned = false;
            closeReceived = false;
            showAttemptId++;
            callback?.Invoke(result);
            AvailabilityChanged?.Invoke();
            Preload();
        }

        private void RegisterLoadFailure()
        {
            consecutiveLoadFailures++;
            loadGeneration++;
            if (consecutiveLoadFailures <= MaximumAutomaticLoadRetries)
            {
                RetryLoadAfterDelay(loadGeneration, GetRetryDelaySeconds(consecutiveLoadFailures));
            }
        }

        private async void RetryLoadAfterDelay(int generation, int delaySeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            if (isShutdown || generation != loadGeneration || !RemoteConfigService.AreRewardedAdsEnabled)
            {
                return;
            }

            LoadAdIfNeeded();
        }

        private async void ScheduleInitializationRetry()
        {
            var generation = ++loadGeneration;
            await Task.Delay(TimeSpan.FromSeconds(RemoteConfigService.RewardedRetryBaseSeconds));
            if (isShutdown || generation != loadGeneration || !RemoteConfigService.AreRewardedAdsEnabled)
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
    }
}
#endif
