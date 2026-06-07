using UnityEngine;

namespace BusPuzzle
{
    internal static class GameFontProvider
    {
        private const string LightFontResource = "Fonts/GmarketSansTTFLight";
        private const string BoldFontResource = "Fonts/GmarketSansTTFBold";

        private static Font lightFont;
        private static Font boldFont;
        private static Font fallbackFont;

        public static Font GetFont(FontStyle fontStyle)
        {
            var requestedFont = IsBoldStyle(fontStyle) ? GetBoldFont() : GetLightFont();
            if (requestedFont != null)
            {
                return requestedFont;
            }

            return GetFallbackFont();
        }

        public static void ApplyToTextMesh(TextMesh text, FontStyle fontStyle)
        {
            if (text == null)
            {
                return;
            }

            text.fontStyle = fontStyle;
            var font = GetFont(fontStyle);
            if (font == null)
            {
                return;
            }

            text.font = font;
            var renderer = text.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = font.material;
            }
        }

        private static bool IsBoldStyle(FontStyle fontStyle)
        {
            return fontStyle == FontStyle.Bold || fontStyle == FontStyle.BoldAndItalic;
        }

        private static Font GetLightFont()
        {
            if (lightFont == null)
            {
                lightFont = Resources.Load<Font>(LightFontResource);
            }

            return lightFont;
        }

        private static Font GetBoldFont()
        {
            if (boldFont == null)
            {
                boldFont = Resources.Load<Font>(BoldFontResource);
            }

            return boldFont != null ? boldFont : GetLightFont();
        }

        private static Font GetFallbackFont()
        {
            if (fallbackFont == null)
            {
                fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (fallbackFont == null)
                {
                    fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            return fallbackFont;
        }
    }
}
