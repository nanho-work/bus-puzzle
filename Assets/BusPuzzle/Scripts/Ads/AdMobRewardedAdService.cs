#if BUS_PUZZLE_ADMOB
using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class AdMobRewardedAdService : IRewardedAdService
    {
        private readonly AdMobSettings settings;
        private readonly Dictionary<RewardedAdPlacement, RewardedAd> rewardedAds = new Dictionary<RewardedAdPlacement, RewardedAd>();
        private readonly HashSet<RewardedAdPlacement> loadingPlacements = new HashSet<RewardedAdPlacement>();
        private RewardedAd showingAd;
        private Action<RewardedAdResult> pendingCompletion;
        private bool isInitialized;
        private bool isShutdown;
        private volatile bool rewardEarned;

        public AdMobRewardedAdService(AdMobSettings settings)
        {
            this.settings = settings != null ? settings : AdMobSettings.Load();
        }

        public event Action AvailabilityChanged;

        public bool IsReady => !isShutdown && IsReadyFor(RewardedAdPlacement.StationSlotUnlock);
        public string CurrentAdUnitId => GetAdUnitId(RewardedAdPlacement.StationSlotUnlock);

        public bool IsReadyFor(RewardedAdPlacement placement)
        {
            return !isShutdown && rewardedAds.TryGetValue(placement, out var ad) && ad != null && ad.CanShowAd();
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

            IosTrackingAuthorization.RequestIfNeeded(() => MobileAds.Initialize(_ =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (isShutdown)
                    {
                        return;
                    }

                    isInitialized = true;
                    Preload();
                });
            }));
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
            loadingPlacements.Clear();

            foreach (var ad in rewardedAds.Values)
            {
                ad?.Destroy();
            }

            rewardedAds.Clear();
            DestroyShowingAd();
            AvailabilityChanged = null;
        }

        public void Preload()
        {
            if (isShutdown)
            {
                return;
            }

            Preload(RewardedAdPlacement.StationSlotUnlock);
            Preload(RewardedAdPlacement.VipBusTeleport);
            Preload(RewardedAdPlacement.BusColorShuffle);
            Preload(RewardedAdPlacement.DepartBoost);
            Preload(RewardedAdPlacement.StageClearDouble);
        }

        public void Preload(RewardedAdPlacement placement)
        {
            if (isShutdown || !isInitialized || loadingPlacements.Contains(placement) || IsReadyFor(placement))
            {
                return;
            }

            DestroyRewardedAd(placement);

            var adUnitId = GetAdUnitId(placement);
            if (!AdMobSettings.LooksLikeAdUnitId(adUnitId))
            {
                Debug.LogError($"Rewarded ad unit ID is invalid for {placement}: {adUnitId}");
                AvailabilityChanged?.Invoke();
                return;
            }

            loadingPlacements.Add(placement);
            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (isShutdown)
                    {
                        ad?.Destroy();
                        return;
                    }

                    loadingPlacements.Remove(placement);

                    if (error != null || ad == null)
                    {
                        Debug.LogWarning($"Rewarded ad failed to load for {placement}: {error}");
                        AvailabilityChanged?.Invoke();
                        return;
                    }

                    rewardedAds[placement] = ad;
                    RegisterCallbacks(ad);
                    AvailabilityChanged?.Invoke();
                });
            });
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

        private bool ShowRewardedAd(RewardedAdPlacement placement, Action<RewardedAdResult> onCompleted)
        {
            if (isShutdown)
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                return false;
            }

            if (showingAd != null || !IsReadyFor(placement))
            {
                onCompleted?.Invoke(RewardedAdResult.NotReady);
                DestroyRewardedAd(placement);
                Preload(placement);
                return false;
            }

            pendingCompletion = onCompleted;
            rewardEarned = false;

            var adToShow = rewardedAds[placement];
            rewardedAds.Remove(placement);
            showingAd = adToShow;
            AvailabilityChanged?.Invoke();

            adToShow.Show(_ => rewardEarned = true);
            return true;
        }

        private void RegisterCallbacks(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    CompletePendingReward(rewardEarned ? RewardedAdResult.RewardEarned : RewardedAdResult.ClosedWithoutReward));
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.LogWarning($"Rewarded ad failed during fullscreen content: {error}");
                    CompletePendingReward(RewardedAdResult.Failed);
                });
            };
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

            DestroyShowingAd();
            Preload();
            callback?.Invoke(result);
        }

        private void DestroyRewardedAd(RewardedAdPlacement placement)
        {
            if (!rewardedAds.TryGetValue(placement, out var ad) || ad == null)
            {
                return;
            }

            ad.Destroy();
            rewardedAds.Remove(placement);
        }

        private void DestroyShowingAd()
        {
            if (showingAd == null)
            {
                return;
            }

            showingAd.Destroy();
            showingAd = null;
        }
    }
}
#endif
