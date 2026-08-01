using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public static class PuzzlePalette
    {
        private const string LitShaderResourcePath = "Shaders/BusPuzzleLitColor";
        private const string FlatShaderResourcePath = "Shaders/BusPuzzleFlatColor";
        private const string TransparentShaderResourcePath = "Shaders/BusPuzzleTransparentColor";
        private static readonly Dictionary<MaterialCacheKey, Material>
            RuntimeMaterials =
                new Dictionary<MaterialCacheKey, Material>();

        public static int RuntimeMaterialCount => RuntimeMaterials.Count;

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
                case PuzzleColor.Lime:
                    return new Color(0.64f, 0.96f, 0.12f);
                case PuzzleColor.Brown:
                    return new Color(0.43f, 0.22f, 0.10f);
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
                case PuzzleColor.Lime:
                    return "Lime";
                case PuzzleColor.Brown:
                    return "Brown";
                default:
                    return "Unknown";
            }
        }

        public static Material CreateMaterial(PuzzleColor color, string nameSuffix)
        {
            var materialName = $"{DisplayName(color)} {nameSuffix}";
            var materialColor = ToColor(color);
            return GetOrCreateRuntimeMaterial(
                new MaterialCacheKey(
                    RuntimeMaterialKind.Palette,
                    materialName,
                    materialColor,
                    0f),
                () =>
                {
                    var material = CreateMaterialFromShader(
                        FindDefaultShader(),
                        materialName);
                    SetMaterialColor(material, materialColor);
                    return material;
                });
        }

        public static Material CreateSolidMaterial(string materialName, Color color)
        {
            return GetOrCreateRuntimeMaterial(
                new MaterialCacheKey(
                    RuntimeMaterialKind.Solid,
                    materialName,
                    color,
                    0f),
                () =>
                {
                    var material = CreateMaterialFromShader(
                        FindFlatShader(),
                        materialName);
                    SetMaterialColor(material, color);
                    return material;
                });
        }

        public static Material CreateLitMaterial(string materialName, Color color, float smoothness = 0.28f)
        {
            return GetOrCreateRuntimeMaterial(
                new MaterialCacheKey(
                    RuntimeMaterialKind.Lit,
                    materialName,
                    color,
                    smoothness),
                () =>
                {
                    var material = CreateMaterialFromShader(
                        FindDefaultShader(),
                        materialName);
                    SetMaterialColor(material, color);

                    if (material != null &&
                        material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", smoothness);
                    }

                    return material;
                });
        }

        public static Material CreateMaterialFromShader(Shader shader, string materialName)
        {
            Material material = null;
            if (shader != null)
            {
                material = new Material(shader) { name = materialName };
                ConfigureCommonRenderState(material);
                return material;
            }

            var fallbackMaterial = Resources.GetBuiltinResource<Material>("Default-Material.mat");
            if (fallbackMaterial != null)
            {
                material = new Material(fallbackMaterial) { name = materialName };
                ConfigureCommonRenderState(material);
                Debug.LogWarning($"Using Unity built-in default material for {materialName}; no requested shader was found.");
                return material;
            }

            Debug.LogError($"No shader or built-in default material was found for {materialName}.");
            return null;
        }

        public static Material CreateTransparentMaterial(string materialName, Color color)
        {
            return GetOrCreateRuntimeMaterial(
                new MaterialCacheKey(
                    RuntimeMaterialKind.Transparent,
                    materialName,
                    color,
                    0f),
                () =>
                {
                    var material = CreateMaterialFromShader(
                        FindTransparentShader(),
                        materialName);
                    if (material == null)
                    {
                        return null;
                    }

                    SetMaterialColor(material, color);
                    material.SetOverrideTag(
                        "RenderType",
                        "Transparent");
                    material.renderQueue =
                        (int)UnityEngine.Rendering.RenderQueue.Transparent;

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
                        material.SetFloat(
                            "_SrcBlend",
                            (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    }

                    if (material.HasProperty("_DstBlend"))
                    {
                        material.SetFloat(
                            "_DstBlend",
                            (float)UnityEngine.Rendering.BlendMode
                                .OneMinusSrcAlpha);
                    }

                    if (material.HasProperty("_ZWrite"))
                    {
                        material.SetFloat("_ZWrite", 0f);
                    }

                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHATEST_ON");
                    return material;
                });
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeMaterialCache()
        {
            foreach (var material in RuntimeMaterials.Values)
            {
                if (material == null)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(material);
                    continue;
                }
#endif
                UnityEngine.Object.Destroy(material);
            }

            RuntimeMaterials.Clear();
        }

        private static Material GetOrCreateRuntimeMaterial(
            MaterialCacheKey key,
            Func<Material> factory)
        {
            if (RuntimeMaterials.TryGetValue(
                    key,
                    out var cachedMaterial) &&
                cachedMaterial != null)
            {
                return cachedMaterial;
            }

            var material = factory != null ? factory() : null;
            if (material == null)
            {
                RuntimeMaterials.Remove(key);
                return null;
            }

            material.hideFlags = HideFlags.DontSave;
            RuntimeMaterials[key] = material;
            return material;
        }

        private enum RuntimeMaterialKind
        {
            Palette,
            Solid,
            Lit,
            Transparent
        }

        private readonly struct MaterialCacheKey :
            IEquatable<MaterialCacheKey>
        {
            public MaterialCacheKey(
                RuntimeMaterialKind kind,
                string name,
                Color color,
                float smoothness)
            {
                Kind = kind;
                Name = name ?? string.Empty;
                Color = color;
                Smoothness = smoothness;
            }

            private RuntimeMaterialKind Kind { get; }
            private string Name { get; }
            private Color Color { get; }
            private float Smoothness { get; }

            public bool Equals(MaterialCacheKey other)
            {
                return Kind == other.Kind &&
                    string.Equals(
                        Name,
                        other.Name,
                        StringComparison.Ordinal) &&
                    Color.Equals(other.Color) &&
                    Smoothness.Equals(other.Smoothness);
            }

            public override bool Equals(object obj)
            {
                return obj is MaterialCacheKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int)Kind;
                    hashCode =
                        (hashCode * 397) ^
                        StringComparer.Ordinal.GetHashCode(Name);
                    hashCode =
                        (hashCode * 397) ^
                        Color.GetHashCode();
                    hashCode =
                        (hashCode * 397) ^
                        Smoothness.GetHashCode();
                    return hashCode;
                }
            }
        }

        public static Color Darken(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r * (1f - amount)),
                Mathf.Clamp01(color.g * (1f - amount)),
                Mathf.Clamp01(color.b * (1f - amount)),
                color.a);
        }

        public static Shader FindDefaultShader()
        {
            return LoadResourceShader(LitShaderResourcePath)
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Mobile/Diffuse")
                ?? FindAlwaysAvailableFallbackShader();
        }

        public static Shader FindFlatShader()
        {
            return LoadResourceShader(FlatShaderResourcePath)
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Mobile/Diffuse")
                ?? FindAlwaysAvailableFallbackShader();
        }

        public static Shader FindTransparentShader()
        {
            return LoadResourceShader(TransparentShaderResourcePath)
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default")
                ?? FindAlwaysAvailableFallbackShader();
        }

        private static Shader LoadResourceShader(string resourcePath)
        {
            var shader = Resources.Load<Shader>(resourcePath);
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find(resourcePath.Replace("Shaders/", "BusPuzzle/"));
        }

        private static Shader FindAlwaysAvailableFallbackShader()
        {
            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("UI/Default")
                ?? Shader.Find("Hidden/Internal-Colored")
                ?? Shader.Find("Hidden/InternalErrorShader");

            if (shader == null)
            {
                Debug.LogError("No usable fallback shader was found. Add a built-in shader to Always Included Shaders.");
            }

            return shader;
        }

        private static void ConfigureCommonRenderState(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

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
