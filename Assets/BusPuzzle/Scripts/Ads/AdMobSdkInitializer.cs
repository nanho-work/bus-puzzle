#if BUS_PUZZLE_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace BusPuzzle
{
    internal static class AdMobSdkInitializer
    {
        private static Action pendingCallbacks;
        private static bool isInitialized;
        private static bool isInitializing;

        public static void Initialize(Action onInitialized)
        {
            if (isInitialized)
            {
                onInitialized?.Invoke();
                return;
            }

            pendingCallbacks += onInitialized;
            if (isInitializing)
            {
                return;
            }

            isInitializing = true;
            IosTrackingAuthorization.RequestIfNeeded(() => MobileAds.Initialize(_ =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    isInitialized = true;
                    isInitializing = false;
                    var callbacks = pendingCallbacks;
                    pendingCallbacks = null;
                    callbacks?.Invoke();
                });
            }));
        }
    }
}
#endif
