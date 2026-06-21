using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;

namespace BusPuzzle
{
    internal static class RemoteConfigService
    {
        private const int CurrentAndroidVersionCode = 12;
        private const int CurrentIosBuildNumber = 12;
        private const string AndroidUpdateUrlFallback = "https://play.google.com/store/apps/details?id=com.koofylab.buspop";
        private const string MaintenanceMessageKoFallback = "잠시 후 다시 이용해 주세요.";
        private const string MaintenanceMessageEnFallback = "Please try again soon.";
        private const string UpdateMessageKoFallback = "새 버전으로 업데이트한 후 이용해 주세요.";
        private const string UpdateMessageEnFallback = "Please update to the latest version.";
        private const string AdsEnabledKey = "ads_enabled";
        private const string RewardedAdsEnabledKey = "rewarded_ads_enabled";
        private const string AndroidAdsEnabledKey = "android_ads_enabled";
        private const string AndroidRewardedAdsEnabledKey = "android_rewarded_ads_enabled";
        private const string IosAdsEnabledKey = "ios_ads_enabled";
        private const string IosRewardedAdsEnabledKey = "ios_rewarded_ads_enabled";
        private const string BannerAdsEnabledKey = "banner_ads_enabled";
        private const string AndroidBannerAdsEnabledKey = "android_banner_ads_enabled";
        private const string IosBannerAdsEnabledKey = "ios_banner_ads_enabled";
        private const string BannerStartStageKey = "banner_start_stage";
        private const int DefaultBannerStartStage = 10;

        private static bool isInitialized;
        private static bool isFetching;
        private static string fetchStatus = "Not initialized";

        public static event Action ValuesUpdated;

        public static bool MaintenanceEnabled { get; private set; }
        public static bool ForceUpdateEnabled { get; private set; }
        public static int AndroidMinSupportedVersionCode { get; private set; } = CurrentAndroidVersionCode;
        public static int IosMinSupportedBuildNumber { get; private set; } = CurrentIosBuildNumber;
        public static bool AdsEnabled { get; private set; } = DefaultPlatformAdsEnabled;
        public static bool RewardedAdsEnabled { get; private set; } = DefaultPlatformRewardedAdsEnabled;
        public static bool BannerAdsEnabled { get; private set; } = DefaultPlatformBannerAdsEnabled;
        public static int BannerStartStage { get; private set; } = DefaultBannerStartStage;
        public static string MaintenanceMessageKo { get; private set; } = MaintenanceMessageKoFallback;
        public static string MaintenanceMessageEn { get; private set; } = MaintenanceMessageEnFallback;
        public static string UpdateMessageKo { get; private set; } = UpdateMessageKoFallback;
        public static string UpdateMessageEn { get; private set; } = UpdateMessageEnFallback;
        public static string AndroidUpdateUrl { get; private set; } = AndroidUpdateUrlFallback;
        public static string IosUpdateUrl { get; private set; } = string.Empty;

        public static bool IsReady => isInitialized && !isFetching;
        public static string FetchStatus => fetchStatus;
        public static bool AreRewardedAdsEnabled => AdsEnabled && RewardedAdsEnabled;
        public static bool AreBannerAdsEnabled => AdsEnabled && BannerAdsEnabled;

        public static bool IsCurrentBuildUnsupported
        {
            get
            {
                if (!ForceUpdateEnabled)
                {
                    return false;
                }

#if UNITY_ANDROID
                return GetCurrentAndroidVersionCode() < AndroidMinSupportedVersionCode;
#elif UNITY_IOS
                return CurrentIosBuildNumber < IosMinSupportedBuildNumber;
#else
                return false;
#endif
            }
        }

        public static void Initialize()
        {
            if (isInitialized || isFetching)
            {
                return;
            }

            isFetching = true;
            fetchStatus = "Checking dependencies";

            FirebaseDependencyService.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
                {
                    var reason = task.Exception?.GetBaseException().Message;
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        reason = task.IsCanceled ? "Canceled" : task.Result.ToString();
                    }

                    fetchStatus = $"Firebase dependency check failed: {reason}";
                    isFetching = false;
                    Debug.LogWarning(fetchStatus);
                    ValuesUpdated?.Invoke();
                    return;
                }

                SetDefaultsAndFetch();
            });
        }

        private static void SetDefaultsAndFetch()
        {
            var defaults = new Dictionary<string, object>
            {
                { "maintenance_enabled", false },
                { "force_update_enabled", false },
                { "android_min_supported_version_code", CurrentAndroidVersionCode },
                { "ios_min_supported_build_number", CurrentIosBuildNumber },
                { AdsEnabledKey, true },
                { RewardedAdsEnabledKey, true },
                { AndroidAdsEnabledKey, true },
                { AndroidRewardedAdsEnabledKey, true },
                { IosAdsEnabledKey, false },
                { IosRewardedAdsEnabledKey, false },
                { BannerAdsEnabledKey, true },
                { AndroidBannerAdsEnabledKey, true },
                { IosBannerAdsEnabledKey, false },
                { BannerStartStageKey, DefaultBannerStartStage },
                { "maintenance_message_ko", MaintenanceMessageKoFallback },
                { "maintenance_message_en", MaintenanceMessageEnFallback },
                { "force_update_message_ko", UpdateMessageKoFallback },
                { "force_update_message_en", UpdateMessageEnFallback },
                { "android_update_url", AndroidUpdateUrlFallback },
                { "ios_update_url", string.Empty }
            };

            fetchStatus = "Setting defaults";
            FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(_ =>
            {
                fetchStatus = "Fetching";
                FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetchTask =>
                {
                    if (fetchTask.IsFaulted || fetchTask.IsCanceled)
                    {
                        fetchStatus = $"Remote Config fetch failed: {fetchTask.Exception?.GetBaseException().Message ?? "Canceled"}";
                        ApplyCachedValues();
                        isInitialized = true;
                        isFetching = false;
                        Debug.LogWarning(fetchStatus);
                        ValuesUpdated?.Invoke();
                        return;
                    }

                    FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(activateTask =>
                    {
                        if (activateTask.IsFaulted || activateTask.IsCanceled)
                        {
                            fetchStatus = $"Remote Config activate failed: {activateTask.Exception?.GetBaseException().Message ?? "Canceled"}";
                            Debug.LogWarning(fetchStatus);
                        }
                        else
                        {
                            fetchStatus = "Ready";
                        }

                        ApplyCachedValues();
                        isInitialized = true;
                        isFetching = false;
                        ValuesUpdated?.Invoke();
                    });
                });
            });
        }

        private static void ApplyCachedValues()
        {
            var config = FirebaseRemoteConfig.DefaultInstance;
            MaintenanceEnabled = config.GetValue("maintenance_enabled").BooleanValue;
            ForceUpdateEnabled = config.GetValue("force_update_enabled").BooleanValue;
            AndroidMinSupportedVersionCode = GetIntValue(config, "android_min_supported_version_code", CurrentAndroidVersionCode);
            IosMinSupportedBuildNumber = GetIntValue(config, "ios_min_supported_build_number", CurrentIosBuildNumber);
            AdsEnabled = config.GetValue(AdsEnabledKey).BooleanValue &&
                GetPlatformBoolValue(config, AndroidAdsEnabledKey, IosAdsEnabledKey, DefaultPlatformAdsEnabled);
            RewardedAdsEnabled = config.GetValue(RewardedAdsEnabledKey).BooleanValue &&
                GetPlatformBoolValue(config, AndroidRewardedAdsEnabledKey, IosRewardedAdsEnabledKey, DefaultPlatformRewardedAdsEnabled);
            BannerAdsEnabled = config.GetValue(BannerAdsEnabledKey).BooleanValue &&
                GetPlatformBoolValue(config, AndroidBannerAdsEnabledKey, IosBannerAdsEnabledKey, DefaultPlatformBannerAdsEnabled);
            BannerStartStage = Mathf.Max(1, GetIntValue(config, BannerStartStageKey, DefaultBannerStartStage));
            MaintenanceMessageKo = GetStringValue(config, "maintenance_message_ko", MaintenanceMessageKoFallback);
            MaintenanceMessageEn = GetStringValue(config, "maintenance_message_en", MaintenanceMessageEnFallback);
            UpdateMessageKo = GetStringValue(config, "force_update_message_ko", UpdateMessageKoFallback);
            UpdateMessageEn = GetStringValue(config, "force_update_message_en", UpdateMessageEnFallback);
            AndroidUpdateUrl = GetStringValue(config, "android_update_url", AndroidUpdateUrlFallback);
            IosUpdateUrl = GetStringValue(config, "ios_update_url", string.Empty);
        }

        private static int GetIntValue(FirebaseRemoteConfig config, string key, int fallback)
        {
            try
            {
                return Convert.ToInt32(config.GetValue(key).LongValue);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static string GetStringValue(FirebaseRemoteConfig config, string key, string fallback)
        {
            var value = config.GetValue(key).StringValue;
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        public static string GetMaintenanceMessage()
        {
            return IsKoreanLanguage() ? MaintenanceMessageKo : MaintenanceMessageEn;
        }

        public static string GetUpdateMessage()
        {
            return IsKoreanLanguage() ? UpdateMessageKo : UpdateMessageEn;
        }

        private static bool DefaultPlatformAdsEnabled
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        private static bool DefaultPlatformRewardedAdsEnabled
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        private static bool DefaultPlatformBannerAdsEnabled
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        private static bool GetPlatformBoolValue(
            FirebaseRemoteConfig config,
            string androidKey,
            string iosKey,
            bool fallback)
        {
#if UNITY_ANDROID
            return config.GetValue(androidKey).BooleanValue;
#elif UNITY_IOS
            return config.GetValue(iosKey).BooleanValue;
#else
            return fallback;
#endif
        }

        public static string GetUpdateUrl()
        {
#if UNITY_IOS
            return IosUpdateUrl;
#else
            return AndroidUpdateUrl;
#endif
        }

        private static bool IsKoreanLanguage()
        {
            return Localization.CurrentLanguageCode.StartsWith("ko", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetCurrentAndroidVersionCode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var packageManager = context.Call<AndroidJavaObject>("getPackageManager"))
                using (var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", context.Call<string>("getPackageName"), 0))
                {
                    return AndroidVersionSupportsLongVersionCode()
                        ? unchecked((int)packageInfo.Call<long>("getLongVersionCode"))
                        : packageInfo.Get<int>("versionCode");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read Android version code: {exception.Message}");
            }
#endif
            return CurrentAndroidVersionCode;
        }

        private static bool AndroidVersionSupportsLongVersionCode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT") >= 28;
            }
#else
            return false;
#endif
        }
    }
}
