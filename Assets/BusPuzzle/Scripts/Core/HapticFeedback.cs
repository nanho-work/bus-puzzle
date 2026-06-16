using UnityEngine;

namespace BusPuzzle
{
    internal static class HapticFeedback
    {
        private const float MinIntervalSeconds = 0.06f;
        private static float lastHapticTime = -10f;

        public static void PlayVehicleLaunch()
        {
            PlayOneShot(18, 36);
        }

        public static void PlayBusFull()
        {
            PlayOneShot(34, 64);
        }

        public static void PlayUiConfirm()
        {
            PlayOneShot(20, 48);
        }

        private static void PlayOneShot(long durationMs, int amplitude)
        {
            if (!UserPreferences.VibrationEnabled)
            {
                return;
            }

            if (Time.unscaledTime - lastHapticTime < MinIntervalSeconds)
            {
                return;
            }

            lastHapticTime = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(durationMs, amplitude);
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void PlayAndroid(long durationMs, int amplitude)
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null)
                    {
                        Handheld.Vibrate();
                        return;
                    }

                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        var sdkInt = version.GetStatic<int>("SDK_INT");
                        if (sdkInt >= 26)
                        {
                            using (var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                            using (var effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                                       "createOneShot",
                                       durationMs,
                                       Mathf.Clamp(amplitude, 1, 255)))
                            {
                                vibrator.Call("vibrate", effect);
                            }
                        }
                        else
                        {
                            vibrator.Call("vibrate", durationMs);
                        }
                    }
                }
            }
            catch
            {
                Handheld.Vibrate();
            }
        }
#endif
    }
}
