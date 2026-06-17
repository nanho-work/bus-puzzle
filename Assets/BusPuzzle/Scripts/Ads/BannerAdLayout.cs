using UnityEngine;

namespace BusPuzzle
{
    internal static class BannerAdLayout
    {
        private const float BannerHeightDp = 50f;
        private const float DefaultReservedHeightPixels = 64f;

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
    }
}
