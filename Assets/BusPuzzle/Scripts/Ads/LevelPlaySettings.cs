using UnityEngine;

namespace BusPuzzle
{
    public sealed class LevelPlaySettings : ScriptableObject
    {
        public const string ResourcePath = "Ads/LevelPlaySettings";

        [Header("LevelPlay app keys")]
        [SerializeField] private string androidAppKey = "";
        [SerializeField] private string iosAppKey = "";

        [Header("LevelPlay ad unit IDs")]
        [SerializeField] private string androidBannerAdUnitId = "";
        [SerializeField] private string iosBannerAdUnitId = "";
        [SerializeField] private string androidRewardedAdUnitId = "";
        [SerializeField] private string iosRewardedAdUnitId = "";

        [Header("Optional LevelPlay placement names")]
        [SerializeField] private string bannerPlacementName = "";
        [SerializeField] private string stationSlotUnlockPlacementName = "";
        [SerializeField] private string vipBusTeleportPlacementName = "";
        [SerializeField] private string busColorShufflePlacementName = "";
        [SerializeField] private string departBoostPlacementName = "";
        [SerializeField] private string stageClearDoublePlacementName = "";

        [Header("Privacy")]
        [Tooltip("Serve contextual ads until a consent flow is implemented.")]
        [SerializeField] private bool serveContextualAdsUntilConsentFlow = true;
        [Tooltip("Only enable after the app explains tracking and needs personalized advertising on iOS.")]
        [SerializeField] private bool requestTrackingAuthorizationOnIos;
        [SerializeField] private bool applyCoppaSetting;
        [SerializeField] private bool isChildDirected;

        [Header("Development")]
        [Tooltip("Enable LevelPlay test-suite metadata in development builds. The suite still needs to be launched explicitly when testing.")]
        [SerializeField] private bool enableTestSuiteMetadataInDevelopmentBuild = true;
        [SerializeField] private bool enableAdapterDebugInDevelopmentBuild = true;

        public string AndroidAppKey => androidAppKey;
        public string IosAppKey => iosAppKey;
        public string AndroidBannerAdUnitId => androidBannerAdUnitId;
        public string IosBannerAdUnitId => iosBannerAdUnitId;
        public string AndroidRewardedAdUnitId => androidRewardedAdUnitId;
        public string IosRewardedAdUnitId => iosRewardedAdUnitId;
        public string BannerPlacementName => bannerPlacementName;
        public bool ServeContextualAdsUntilConsentFlow => serveContextualAdsUntilConsentFlow;
        public bool RequestTrackingAuthorizationOnIos => requestTrackingAuthorizationOnIos;
        public bool ApplyCoppaSetting => applyCoppaSetting;
        public bool IsChildDirected => isChildDirected;
        public bool EnableTestSuiteMetadataInDevelopmentBuild => enableTestSuiteMetadataInDevelopmentBuild;
        public bool EnableAdapterDebugInDevelopmentBuild => enableAdapterDebugInDevelopmentBuild;

        public static LevelPlaySettings Load()
        {
            var settings = Resources.Load<LevelPlaySettings>(ResourcePath);
            if (settings != null)
            {
                return settings;
            }

            Debug.LogError("LevelPlaySettings asset is missing. Ads will remain disabled.");
            return CreateInstance<LevelPlaySettings>();
        }

        public string GetAppKey()
        {
#if UNITY_IOS
            return iosAppKey;
#else
            return androidAppKey;
#endif
        }

        public string GetBannerAdUnitId()
        {
#if UNITY_IOS
            return iosBannerAdUnitId;
#else
            return androidBannerAdUnitId;
#endif
        }

        public string GetRewardedAdUnitId()
        {
#if UNITY_IOS
            return iosRewardedAdUnitId;
#else
            return androidRewardedAdUnitId;
#endif
        }

        public string GetRewardedPlacementName(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.VipBusTeleport:
                    return vipBusTeleportPlacementName;
                case RewardedAdPlacement.BusColorShuffle:
                    return busColorShufflePlacementName;
                case RewardedAdPlacement.DepartBoost:
                    return departBoostPlacementName;
                case RewardedAdPlacement.StageClearDouble:
                    return stageClearDoublePlacementName;
                default:
                    return stationSlotUnlockPlacementName;
            }
        }

        public static bool LooksLikeLevelPlayAppKey(string value)
        {
            return IsAlphaNumeric(value, 9, 64);
        }

        public static bool LooksLikeLevelPlayAdUnitId(string value)
        {
            return IsAlphaNumeric(value, 16, 64);
        }

        private static bool IsAlphaNumeric(string value, int minimumLength, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < minimumLength || value.Length > maximumLength)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index++)
            {
                if (!char.IsLetterOrDigit(value[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
