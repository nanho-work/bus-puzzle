using UnityEngine;

namespace BusPuzzle
{
    public sealed class AdMobSettings : ScriptableObject
    {
        public const string ResourcePath = "Ads/AdMobSettings";
        public const string TestPublisherId = "ca-app-pub-3940256099942544";
        public const string AndroidTestAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string IosTestAppId = "ca-app-pub-3940256099942544~1458002511";
        public const string AndroidBannerTestAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        public const string IosBannerTestAdUnitId = "ca-app-pub-3940256099942544/2934735716";
        public const string AndroidRewardedTestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        public const string IosRewardedTestAdUnitId = "ca-app-pub-3940256099942544/1712485313";
        public const string StationSlotUnlockRewardType = "station_slot_unlock";
        public const string VipBusTeleportRewardType = "vip_bus_teleport";
        public const string BusColorShuffleRewardType = "bus_color_shuffle";
        public const string DepartBoostRewardType = "depart_boost";
        public const string StageClearDoubleRewardType = "stage_clear_double";

        [SerializeField] private bool useProductionAdsInRelease = true;
        [SerializeField] private string androidAppId = "ca-app-pub-5773331970563455~5379288524";
        [SerializeField] private string iosAppId = "ca-app-pub-5773331970563455~7947262028";
        [SerializeField] private string androidBannerProductionAdUnitId = "ca-app-pub-5773331970563455/5432398451";
        [SerializeField] private string iosBannerProductionAdUnitId = "ca-app-pub-5773331970563455/6945919971";
        [SerializeField] private string androidRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/3872285640";
        [SerializeField] private string iosRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/7771471978";
        [SerializeField] private string androidVipRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/3872285640";
        [SerializeField] private string iosVipRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/7771471978";
        [SerializeField] private string androidShuffleRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/3872285640";
        [SerializeField] private string iosShuffleRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/7771471978";
        [SerializeField] private string androidDepartRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/3872285640";
        [SerializeField] private string iosDepartRewardedProductionAdUnitId = "ca-app-pub-5773331970563455/7771471978";

        public bool UseProductionAdsInRelease => useProductionAdsInRelease;
        public string AndroidAppId => androidAppId;
        public string IosAppId => iosAppId;
        public string AndroidBannerProductionAdUnitId => androidBannerProductionAdUnitId;
        public string IosBannerProductionAdUnitId => iosBannerProductionAdUnitId;
        public string AndroidRewardedProductionAdUnitId => androidRewardedProductionAdUnitId;
        public string IosRewardedProductionAdUnitId => iosRewardedProductionAdUnitId;
        public string AndroidVipRewardedProductionAdUnitId => androidVipRewardedProductionAdUnitId;
        public string IosVipRewardedProductionAdUnitId => iosVipRewardedProductionAdUnitId;
        public string AndroidShuffleRewardedProductionAdUnitId => androidShuffleRewardedProductionAdUnitId;
        public string IosShuffleRewardedProductionAdUnitId => iosShuffleRewardedProductionAdUnitId;
        public string AndroidDepartRewardedProductionAdUnitId => androidDepartRewardedProductionAdUnitId;
        public string IosDepartRewardedProductionAdUnitId => iosDepartRewardedProductionAdUnitId;

        public static AdMobSettings Load()
        {
            var settings = Resources.Load<AdMobSettings>(ResourcePath);
            if (settings != null)
            {
                return settings;
            }

            settings = CreateInstance<AdMobSettings>();
            Debug.LogWarning("AdMobSettings asset is missing. Runtime will use Google rewarded test ad unit IDs.");
            return settings;
        }

        public string GetRewardedAdUnitId()
        {
            return GetRewardedAdUnitId(RewardedAdPlacement.StationSlotUnlock);
        }

        public string GetBannerAdUnitId()
        {
            return ShouldUseProductionAds() ? GetProductionBannerAdUnitId() : GetTestBannerAdUnitId();
        }

        public string GetRewardedAdUnitId(RewardedAdPlacement placement)
        {
            if (ShouldUseProductionAds())
            {
                return GetProductionRewardedAdUnitId(placement);
            }

            return GetTestRewardedAdUnitId(placement);
        }

        public string GetPlatformAppId()
        {
#if UNITY_ANDROID
            return androidAppId;
#elif UNITY_IOS
            return iosAppId;
#else
            return string.Empty;
#endif
        }

        public string GetProductionRewardedAdUnitId()
        {
            return GetProductionRewardedAdUnitId(RewardedAdPlacement.StationSlotUnlock);
        }

        public string GetProductionBannerAdUnitId()
        {
#if UNITY_ANDROID
            return androidBannerProductionAdUnitId;
#elif UNITY_IOS
            return iosBannerProductionAdUnitId;
#else
            return string.Empty;
#endif
        }

        public string GetProductionRewardedAdUnitId(RewardedAdPlacement placement)
        {
#if UNITY_ANDROID
            return GetAndroidProductionRewardedAdUnitId(placement);
#elif UNITY_IOS
            return GetIosProductionRewardedAdUnitId(placement);
#else
            return string.Empty;
#endif
        }

        private string GetAndroidProductionRewardedAdUnitId(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.VipBusTeleport:
                    return androidVipRewardedProductionAdUnitId;
                case RewardedAdPlacement.BusColorShuffle:
                    return androidShuffleRewardedProductionAdUnitId;
                case RewardedAdPlacement.DepartBoost:
                    return androidDepartRewardedProductionAdUnitId;
                case RewardedAdPlacement.StageClearDouble:
                    return androidRewardedProductionAdUnitId;
                default:
                    return androidRewardedProductionAdUnitId;
            }
        }

        private string GetIosProductionRewardedAdUnitId(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.VipBusTeleport:
                    return iosVipRewardedProductionAdUnitId;
                case RewardedAdPlacement.BusColorShuffle:
                    return iosShuffleRewardedProductionAdUnitId;
                case RewardedAdPlacement.DepartBoost:
                    return iosDepartRewardedProductionAdUnitId;
                case RewardedAdPlacement.StageClearDouble:
                    return iosRewardedProductionAdUnitId;
                default:
                    return iosRewardedProductionAdUnitId;
            }
        }

        public string GetTestRewardedAdUnitId()
        {
            return GetTestRewardedAdUnitId(RewardedAdPlacement.StationSlotUnlock);
        }

        public string GetTestBannerAdUnitId()
        {
#if UNITY_IOS
            return IosBannerTestAdUnitId;
#else
            return AndroidBannerTestAdUnitId;
#endif
        }

        public string GetTestRewardedAdUnitId(RewardedAdPlacement placement)
        {
#if UNITY_IOS
            return IosRewardedTestAdUnitId;
#else
            return AndroidRewardedTestAdUnitId;
#endif
        }

        public bool ShouldUseProductionAds()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return false;
#else
            return useProductionAdsInRelease;
#endif
        }

        public static bool LooksLikeAppId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.StartsWith("ca-app-pub-", System.StringComparison.Ordinal) &&
                value.Contains("~");
        }

        public static bool LooksLikeAdUnitId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.StartsWith("ca-app-pub-", System.StringComparison.Ordinal) &&
                value.Contains("/");
        }

        public static bool IsGoogleTestAdUnitId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.StartsWith(TestPublisherId, System.StringComparison.Ordinal);
        }
    }
}
