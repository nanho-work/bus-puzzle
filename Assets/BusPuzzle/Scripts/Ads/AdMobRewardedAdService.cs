#if BUS_PUZZLE_ADMOB
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class AdMobRewardedAdService : IRewardedAdService
    {
        private const int ShowCallbackTimeoutSeconds = 120;
        private const int MaximumAutomaticLoadRetries = 4;

        private sealed class LoadRetryState
        {
            public int ConsecutiveFailures;
            public int Generation;
            public double NextAllowedRealtime;
        }

        private static readonly RewardedAdPlacement[] Placements =
        {
            RewardedAdPlacement.StationSlotUnlock,
            RewardedAdPlacement.VipBusTeleport,
            RewardedAdPlacement.BusColorShuffle,
            RewardedAdPlacement.DepartBoost,
            RewardedAdPlacement.StageClearDouble
        };

        private readonly AdMobSettings settings;
        private readonly Dictionary<string, RewardedAd> rewardedAdsByUnitId =
            new Dictionary<string, RewardedAd>(StringComparer.Ordinal);
        private readonly HashSet<string> loadingAdUnitIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, LoadRetryState> loadRetryStates =
            new Dictionary<string, LoadRetryState>(StringComparer.Ordinal);

        private RewardedAd showingAd;
        private Action<RewardedAdResult> pendingCompletion;
        private bool isInitialized;
        private bool isShutdown;
        private bool isLifecycleSubscribed;
        private bool showWasInterruptedByApplicationPause;
        private volatile bool rewardEarned;
        private int showAttemptId;

        public AdMobRewardedAdService(AdMobSettings settings)
        {
            this.settings = settings != null ? settings : AdMobSettings.Load();
        }

        public event Action AvailabilityChanged;

        public bool IsReady => !isShutdown && IsReadyFor(RewardedAdPlacement.StationSlotUnlock);
        public string CurrentAdUnitId => GetAdUnitId(RewardedAdPlacement.StationSlotUnlock);

        public bool IsReadyFor(RewardedAdPlacement placement)
        {
            if (isShutdown)
            {
                return false;
            }

            var adUnitId = GetAdUnitId(placement);
            return AdMobSettings.LooksLikeAdUnitId(adUnitId) &&
                rewardedAdsByUnitId.TryGetValue(adUnitId, out var ad) &&
                ad != null &&
                ad.CanShowAd();
        }

        public string GetAdUnitId(RewardedAdPlacement placement)
        {
            return settings.GetRewardedAdUnitId(placement);
        }

        public void Initialize()
        {
            if (isInitialized || isShutdown)
            {
                return;
            }

            SubscribeToApplicationLifecycle();

            AdMobSdkInitializer.Initialize(() =>
            {
                if (isShutdown)
                {
                    return;
                }

                isInitialized = true;
                Preload();
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
            pendingCompletion = null;
            rewardEarned = false;
            showAttemptId++;
            loadingAdUnitIds.Clear();

            foreach (var retryState in loadRetryStates.Values)
            {
                retryState.Generation++;
            }

            loadRetryStates.Clear();
            UnsubscribeFromApplicationLifecycle();

            foreach (var ad in rewardedAdsByUnitId.Values)
            {
                ad?.Destroy();
            }

            rewardedAdsByUnitId.Clear();
            DestroyShowingAd();
            AvailabilityChanged = null;
        }

        public void Preload()
        {
            if (isShutdown)
            {
                return;
            }

            var uniqueAdUnitIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Placements.Length; index++)
            {
                var adUnitId = GetAdUnitId(Placements[index]);
                if (uniqueAdUnitIds.Add(adUnitId))
                {
                    PreloadAdUnit(adUnitId);
                }
            }
        }

        public void Preload(RewardedAdPlacement placement)
        {
            PreloadAdUnit(GetAdUnitId(placement));
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

        private void PreloadAdUnit(string adUnitId)
        {
            if (isShutdown || !isInitialized || loadingAdUnitIds.Contains(adUnitId))
            {
                return;
            }

            if (!AdMobSettings.LooksLikeAdUnitId(adUnitId))
            {
                Debug.LogError($"Rewarded ad unit ID is invalid: {adUnitId}");
                AvailabilityChanged?.Invoke();
                return;
            }

            if (rewardedAdsByUnitId.TryGetValue(adUnitId, out var cachedAd))
            {
                if (cachedAd != null && cachedAd.CanShowAd())
                {
                    return;
                }

                cachedAd?.Destroy();
                rewardedAdsByUnitId.Remove(adUnitId);
            }

            var retryState = GetLoadRetryState(adUnitId);
            if (Time.realtimeSinceStartupAsDouble < retryState.NextAllowedRealtime)
            {
                return;
            }

            loadingAdUnitIds.Add(adUnitId);
            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (isShutdown)
                    {
                        ad?.Destroy();
                        return;
                    }

                    loadingAdUnitIds.Remove(adUnitId);

                    if (error != null || ad == null)
                    {
                        ad?.Destroy();
                        Debug.LogWarning($"Rewarded ad failed to load: {error}");
                        RegisterLoadFailure(adUnitId);
                        AvailabilityChanged?.Invoke();
                        return;
                    }

                    ResetLoadRetryState(adUnitId);
                    if (rewardedAdsByUnitId.TryGetValue(adUnitId, out var previousAd))
                    {
                        previousAd?.Destroy();
                    }

                    rewardedAdsByUnitId[adUnitId] = ad;
                    RegisterCallbacks(ad);
                    AvailabilityChanged?.Invoke();
                });
            });
        }

        private bool ShowRewardedAd(RewardedAdPlacement placement, Action<RewardedAdResult> onCompleted)
        {
            if (isShutdown)
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                return false;
            }

            var adUnitId = GetAdUnitId(placement);
            if (showingAd != null ||
                !rewardedAdsByUnitId.TryGetValue(adUnitId, out var adToShow) ||
                adToShow == null ||
                !adToShow.CanShowAd())
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                PreloadAdUnit(adUnitId);
                return false;
            }

            pendingCompletion = onCompleted;
            rewardEarned = false;
            showWasInterruptedByApplicationPause = false;
            rewardedAdsByUnitId.Remove(adUnitId);
            showingAd = adToShow;
            AvailabilityChanged?.Invoke();

            var attemptId = ++showAttemptId;
            try
            {
                adToShow.Show(_ =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (ReferenceEquals(showingAd, adToShow) && attemptId == showAttemptId)
                        {
                            rewardEarned = true;
                        }
                    });
                });
                CompleteAfterShowTimeout(attemptId, adToShow);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded ad failed to show for {placement}: {exception}");
                CompletePendingReward(RewardedAdResult.Failed);
                return false;
            }

            return true;
        }

        private void RegisterCallbacks(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!ReferenceEquals(showingAd, ad))
                    {
                        return;
                    }

                    CompleteAfterRewardCallbackGrace(showAttemptId, ad);
                });
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!ReferenceEquals(showingAd, ad))
                    {
                        return;
                    }

                    Debug.LogWarning($"Rewarded ad failed during fullscreen content: {error}");
                    CompletePendingReward(RewardedAdResult.Failed);
                });
            };
        }

        private async void CompleteAfterRewardCallbackGrace(int attemptId, RewardedAd ad)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300));

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (isShutdown ||
                    !ReferenceEquals(showingAd, ad) ||
                    pendingCompletion == null ||
                    attemptId != showAttemptId)
                {
                    return;
                }

                CompletePendingReward(rewardEarned ? RewardedAdResult.RewardEarned : RewardedAdResult.ClosedWithoutReward);
            });
        }

        private async void CompleteAfterShowTimeout(int attemptId, RewardedAd ad)
        {
            await Task.Delay(TimeSpan.FromSeconds(ShowCallbackTimeoutSeconds));

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (isShutdown ||
                    !ReferenceEquals(showingAd, ad) ||
                    pendingCompletion == null ||
                    attemptId != showAttemptId)
                {
                    return;
                }

                if (showWasInterruptedByApplicationPause)
                {
                    showWasInterruptedByApplicationPause = false;
                    CompleteAfterShowTimeout(attemptId, ad);
                    return;
                }

                Debug.LogWarning("Rewarded ad timed out while waiting for a fullscreen completion callback.");
                CompletePendingReward(rewardEarned ? RewardedAdResult.RewardEarned : RewardedAdResult.Failed);
            });
        }

        private void CompletePendingReward(RewardedAdResult result)
        {
            if (isShutdown)
            {
                pendingCompletion = null;
                rewardEarned = false;
                DestroyShowingAd();
                return;
            }

            var callback = pendingCompletion;
            pendingCompletion = null;
            rewardEarned = false;
            showWasInterruptedByApplicationPause = false;
            showAttemptId++;

            DestroyShowingAd();
            callback?.Invoke(result);
        }

        private LoadRetryState GetLoadRetryState(string adUnitId)
        {
            if (!loadRetryStates.TryGetValue(adUnitId, out var state))
            {
                state = new LoadRetryState();
                loadRetryStates[adUnitId] = state;
            }

            return state;
        }

        private void RegisterLoadFailure(string adUnitId)
        {
            var retryState = GetLoadRetryState(adUnitId);
            retryState.ConsecutiveFailures++;
            retryState.Generation++;

            var delaySeconds = GetRetryDelaySeconds(retryState.ConsecutiveFailures);
            retryState.NextAllowedRealtime = Time.realtimeSinceStartupAsDouble + delaySeconds;
            if (retryState.ConsecutiveFailures <= MaximumAutomaticLoadRetries)
            {
                RetryPreloadAfterDelay(adUnitId, retryState.Generation, delaySeconds);
            }
        }

        private void ResetLoadRetryState(string adUnitId)
        {
            var retryState = GetLoadRetryState(adUnitId);
            retryState.ConsecutiveFailures = 0;
            retryState.NextAllowedRealtime = 0d;
            retryState.Generation++;
        }

        private async void RetryPreloadAfterDelay(string adUnitId, int generation, int delaySeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (isShutdown || !RemoteConfigService.AreRewardedAdsEnabled)
                {
                    return;
                }

                var retryState = GetLoadRetryState(adUnitId);
                if (retryState.Generation != generation)
                {
                    return;
                }

                PreloadAdUnit(adUnitId);
            });
        }

        private static int GetRetryDelaySeconds(int consecutiveFailures)
        {
            var baseSeconds = RemoteConfigService.RewardedRetryBaseSeconds;
            var maxSeconds = RemoteConfigService.RewardedRetryMaxSeconds;
            int multiplier;
            switch (Mathf.Clamp(consecutiveFailures, 1, 5))
            {
                case 1:
                    multiplier = 1;
                    break;
                case 2:
                    multiplier = 2;
                    break;
                case 3:
                    multiplier = 4;
                    break;
                case 4:
                    multiplier = 10;
                    break;
                default:
                    multiplier = 30;
                    break;
            }

            return Mathf.Clamp(baseSeconds * multiplier, baseSeconds, maxSeconds);
        }

        private void SubscribeToApplicationLifecycle()
        {
            if (isLifecycleSubscribed)
            {
                return;
            }

            Application.focusChanged += HandleApplicationFocusChanged;
            isLifecycleSubscribed = true;
        }

        private void UnsubscribeFromApplicationLifecycle()
        {
            if (!isLifecycleSubscribed)
            {
                return;
            }

            Application.focusChanged -= HandleApplicationFocusChanged;
            isLifecycleSubscribed = false;
        }

        private void HandleApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus && showingAd != null)
            {
                showWasInterruptedByApplicationPause = true;
            }
        }

        private void DestroyShowingAd()
        {
            if (showingAd == null)
            {
                return;
            }

            showingAd.Destroy();
            showingAd = null;
            showWasInterruptedByApplicationPause = false;
        }
    }
}
#endif
