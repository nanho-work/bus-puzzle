using UnityEngine;

namespace BusPuzzle
{
    internal static class BannerAdLayout
    {
        private const float BannerHeightDp = 50f;
        private const float GameplayExtraBottomClearanceDp = 36f;
        private const float DefaultReservedHeightPixels = 64f;
        private const float DefaultGameplayExtraClearancePixels = 44f;

        public static float GetReservedHeightPixels()
        {
            if (Screen.height <= 0)
            {
                return 0f;
            }

            var dpi = Screen.dpi;
            var height = dpi > 0f
                ? Mathf.Ceil(BannerHeightDp * dpi / 160f)
                : DefaultReservedHeightPixels;
            var maxHeight = Mathf.Max(50f, Mathf.Min(180f, Screen.height * 0.12f));
            return Mathf.Clamp(height, 50f, maxHeight);
        }

        public static float GetGameplayReservedHeightPixels()
        {
            var reservedHeight = GetReservedHeightPixels();
            if (reservedHeight <= 0f || Screen.height <= 0)
            {
                return reservedHeight;
            }

            var dpi = Screen.dpi;
            var extraClearance = dpi > 0f
                ? Mathf.Ceil(GameplayExtraBottomClearanceDp * dpi / 160f)
                : DefaultGameplayExtraClearancePixels;
            var maxHeight = Mathf.Max(reservedHeight, Mathf.Min(260f, Screen.height * 0.18f));
            return Mathf.Clamp(reservedHeight + extraClearance, reservedHeight, maxHeight);
        }
    }
}
