using UnityEngine;

namespace BusPuzzle
{
    public static class PuzzlePalette
    {
        public static Color ToColor(PuzzleColor color)
        {
            switch (color)
            {
                case PuzzleColor.Red:
                    return new Color(0.93f, 0.20f, 0.18f);
                case PuzzleColor.Blue:
                    return new Color(0.12f, 0.45f, 0.94f);
                case PuzzleColor.Yellow:
                    return new Color(1.00f, 0.78f, 0.18f);
                case PuzzleColor.Green:
                    return new Color(0.14f, 0.72f, 0.38f);
                case PuzzleColor.Purple:
                    return new Color(0.56f, 0.28f, 0.86f);
                case PuzzleColor.Orange:
                    return new Color(1.00f, 0.47f, 0.16f);
                case PuzzleColor.White:
                    return new Color(0.94f, 0.95f, 0.92f);
                case PuzzleColor.Black:
                    return new Color(0.08f, 0.09f, 0.11f);
                case PuzzleColor.Pink:
                    return new Color(1.00f, 0.25f, 0.66f);
                case PuzzleColor.SkyBlue:
                    return new Color(0.12f, 0.78f, 0.96f);
                default:
                    return Color.white;
            }
        }

        public static string DisplayName(PuzzleColor color)
        {
            switch (color)
            {
                case PuzzleColor.Red:
                    return "Red";
                case PuzzleColor.Blue:
                    return "Blue";
                case PuzzleColor.Yellow:
                    return "Yellow";
                case PuzzleColor.Green:
                    return "Green";
                case PuzzleColor.Purple:
                    return "Purple";
                case PuzzleColor.Orange:
                    return "Orange";
                case PuzzleColor.White:
                    return "White";
                case PuzzleColor.Black:
                    return "Black";
                case PuzzleColor.Pink:
                    return "Pink";
                case PuzzleColor.SkyBlue:
                    return "Sky Blue";
                default:
                    return "Unknown";
            }
        }

        public static Material CreateMaterial(PuzzleColor color, string nameSuffix)
        {
            var material = new Material(FindDefaultShader());
            material.name = $"{DisplayName(color)} {nameSuffix}";
            SetMaterialColor(material, ToColor(color));
            return material;
        }

        public static Material CreateSolidMaterial(string materialName, Color color)
        {
            var material = new Material(FindFlatShader());
            material.name = materialName;
            SetMaterialColor(material, color);
            return material;
        }

        public static Material CreateTransparentMaterial(string materialName, Color color)
        {
            var material = CreateSolidMaterial(materialName, color);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            return material;
        }

        public static Color Darken(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r * (1f - amount)),
                Mathf.Clamp01(color.g * (1f - amount)),
                Mathf.Clamp01(color.b * (1f - amount)),
                color.a);
        }

        private static Shader FindDefaultShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Unlit/Color");
        }

        private static Shader FindFlatShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
