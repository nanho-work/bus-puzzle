using UnityEngine;

namespace BusPuzzle
{
    internal static class MobilePerformanceProfile
    {
        private const int TargetFrameRate = 60;
        private const int MobileQualityLevel = 1;
        private const float StandardRenderScale = 0.92f;
        private const float HighResolutionRenderScale = 0.84f;
        private const float UltraResolutionRenderScale = 0.78f;

        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

#if UNITY_ANDROID || UNITY_IOS
            if (QualitySettings.names.Length > MobileQualityLevel && QualitySettings.GetQualityLevel() != MobileQualityLevel)
            {
                QualitySettings.SetQualityLevel(MobileQualityLevel, true);
            }

            QualitySettings.vSyncCount = 0;
            QualitySettings.pixelLightCount = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            ApplyRenderScaleForCurrentScreen();
#endif
        }

        public static void ApplyCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.allowHDR = false;
            camera.allowMSAA = false;
#if UNITY_ANDROID || UNITY_IOS
            camera.allowDynamicResolution = true;
#endif
        }

        public static void ApplyRenderScaleForCurrentScreen()
        {
#if UNITY_ANDROID || UNITY_IOS
            var scale = GetRenderScaleForCurrentScreen();
            ScalableBufferManager.ResizeBuffers(scale, scale);
#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        private static float GetRenderScaleForCurrentScreen()
        {
            var maxSide = Mathf.Max(Screen.width, Screen.height);
            if (maxSide >= 2600)
            {
                return UltraResolutionRenderScale;
            }

            if (maxSide >= 2200)
            {
                return HighResolutionRenderScale;
            }

            return StandardRenderScale;
        }
#endif
    }
}
