using UnityEngine;
using UnityEngine.Rendering;

namespace BusPuzzle
{
    internal static class GroundShadowBuilder
    {
        private const int EllipseSegments = 28;

        private static Material vehicleShadowMaterial;
        private static Material passengerShadowMaterial;

        public static GameObject CreateVehicleShadow(
            Transform parent,
            float visualWidth,
            float visualLength,
            float visualCenterZ,
            float cellSize)
        {
            return CreateEllipse(
                "Vehicle Ground Shadow",
                parent,
                new Vector3(0f, cellSize * 0.012f, visualCenterZ - visualLength * 0.025f),
                new Vector2(visualWidth * 1.08f, visualLength * 0.88f),
                GetVehicleShadowMaterial());
        }

        public static GameObject CreatePassengerShadow(Transform parent, Vector3 localPosition, float width, float depth)
        {
            return CreateEllipse(
                "Passenger Ground Shadow",
                parent,
                localPosition,
                new Vector2(width, depth),
                GetPassengerShadowMaterial());
        }

        private static GameObject CreateEllipse(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector2 size,
            Material material)
        {
            var shadow = new GameObject(name);
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = localPosition;
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = Vector3.one;

            var vertices = new Vector3[EllipseSegments + 1];
            var triangles = new int[EllipseSegments * 6];
            vertices[0] = Vector3.zero;

            var halfWidth = Mathf.Max(0.001f, size.x * 0.5f);
            var halfDepth = Mathf.Max(0.001f, size.y * 0.5f);
            for (var index = 0; index < EllipseSegments; index++)
            {
                var angle = index * Mathf.PI * 2f / EllipseSegments;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * halfWidth, 0f, Mathf.Sin(angle) * halfDepth);
            }

            for (var index = 0; index < EllipseSegments; index++)
            {
                var current = index + 1;
                var next = index + 1 == EllipseSegments ? 1 : index + 2;
                var triangleIndex = index * 6;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = next;
                triangles[triangleIndex + 2] = current;
                triangles[triangleIndex + 3] = 0;
                triangles[triangleIndex + 4] = current;
                triangles[triangleIndex + 5] = next;
            }

            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = shadow.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = shadow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return shadow;
        }

        private static Material GetVehicleShadowMaterial()
        {
            if (vehicleShadowMaterial == null)
            {
                vehicleShadowMaterial = CreateShadowMaterial("Vehicle Contact Shadow", new Color(0.58f, 0.62f, 0.66f, 0.30f));
            }

            return vehicleShadowMaterial;
        }

        private static Material GetPassengerShadowMaterial()
        {
            if (passengerShadowMaterial == null)
            {
                passengerShadowMaterial = CreateShadowMaterial("Passenger Contact Shadow", new Color(0.62f, 0.66f, 0.70f, 0.24f));
            }

            return passengerShadowMaterial;
        }

        private static Material CreateShadowMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            ConfigureTransparent(material);
            return material;
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
