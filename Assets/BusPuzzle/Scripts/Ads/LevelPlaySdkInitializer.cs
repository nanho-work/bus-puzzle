#if BUS_PUZZLE_LEVELPLAY
using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace BusPuzzle
{
    internal static class LevelPlaySdkInitializer
    {
        private static Action<bool> pendingCallbacks;
        private static bool isInitialized;
        private static bool isInitializing;
        private static string initializedAppKey = string.Empty;

        public static void Initialize(LevelPlaySettings settings, Action<bool> onCompleted)
        {
            settings = settings != null ? settings : LevelPlaySettings.Load();
            var appKey = settings.GetAppKey();
            if (!LevelPlaySettings.LooksLikeLevelPlayAppKey(appKey))
            {
                Debug.LogError($"LevelPlay app key is invalid for {Application.platform}: {appKey}");
                onCompleted?.Invoke(false);
                return;
            }

            if (isInitialized)
            {
                var matchesInitializedApp = string.Equals(initializedAppKey, appKey, StringComparison.Ordinal);
                if (!matchesInitializedApp)
                {
                    Debug.LogError("LevelPlay was already initialized with a different app key.");
                }

                onCompleted?.Invoke(matchesInitializedApp);
                return;
            }

            pendingCallbacks += onCompleted;
            if (isInitializing)
            {
                return;
            }

            isInitializing = true;
            initializedAppKey = appKey;
            try
            {
                ApplyPrivacySettings(settings);
                ApplyDevelopmentSettings(settings);
                LevelPlay.SetPauseGame(true);
                LevelPlay.OnInitSuccess += HandleInitializationSucceeded;
                LevelPlay.OnInitFailed += HandleInitializationFailed;

                void StartInitialization()
                {
                    try
                    {
                        var userId = PlayerIdentityService.IsReady ? PlayerIdentityService.UserId : null;
                        LevelPlay.Init(appKey, string.IsNullOrWhiteSpace(userId) ? null : userId);
                    }
                    catch (Exception exception)
                    {
                        CompleteInitializationFailure($"LevelPlay initialization could not start: {exception}");
                    }
                }

                if (settings.RequestTrackingAuthorizationOnIos)
                {
                    IosTrackingAuthorization.RequestIfNeeded(StartInitialization);
                }
                else
                {
                    StartInitialization();
                }
            }
            catch (Exception exception)
            {
                CompleteInitializationFailure($"LevelPlay initialization setup failed: {exception}");
            }
        }

        private static void ApplyPrivacySettings(LevelPlaySettings settings)
        {
            if (settings.ServeContextualAdsUntilConsentFlow)
            {
                LevelPlayPrivacySettings.SetGDPRConsent(false);
                LevelPlayPrivacySettings.SetCCPA(true);
            }

            if (settings.ApplyCoppaSetting)
            {
                LevelPlayPrivacySettings.SetCOPPA(settings.IsChildDirected);
            }
        }

        private static void ApplyDevelopmentSettings(LevelPlaySettings settings)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (settings.EnableAdapterDebugInDevelopmentBuild)
            {
                LevelPlay.SetAdaptersDebug(true);
            }

            if (settings.EnableTestSuiteMetadataInDevelopmentBuild)
            {
                LevelPlay.SetMetaData("is_test_suite", "enable");
            }
#endif
        }

        private static void HandleInitializationSucceeded(LevelPlayConfiguration configuration)
        {
            UnsubscribeInitializationEvents();
            isInitialized = true;
            isInitializing = false;
            Debug.Log($"LevelPlay initialized successfully: {configuration}");
            CompletePendingCallbacks(true);
        }

        private static void HandleInitializationFailed(LevelPlayInitError error)
        {
            CompleteInitializationFailure($"LevelPlay initialization failed: {error}");
        }

        private static void CompleteInitializationFailure(string message)
        {
            UnsubscribeInitializationEvents();
            isInitialized = false;
            isInitializing = false;
            initializedAppKey = string.Empty;
            Debug.LogWarning(message);
            CompletePendingCallbacks(false);
        }

        private static void CompletePendingCallbacks(bool succeeded)
        {
            var callbacks = pendingCallbacks;
            pendingCallbacks = null;
            if (callbacks == null)
            {
                return;
            }

            foreach (Action<bool> callback in callbacks.GetInvocationList())
            {
                try
                {
                    callback(succeeded);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static void UnsubscribeInitializationEvents()
        {
            LevelPlay.OnInitSuccess -= HandleInitializationSucceeded;
            LevelPlay.OnInitFailed -= HandleInitializationFailed;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            UnsubscribeInitializationEvents();
            pendingCallbacks = null;
            isInitialized = false;
            isInitializing = false;
            initializedAppKey = string.Empty;
        }
    }
}
#endif
