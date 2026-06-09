using UnityEngine;
using UnityEngine.Rendering;

namespace BusPuzzle
{
    internal static class GroundShadowBuilder
    {
        private const int EllipseSegments = 28;

        private static Material vehicleShadowMaterial;
        private static Material vehicleCastShadowMaterial;
        private static Material passengerShadowMaterial;

        public static GameObject CreateVehicleShadow(
            Transform parent,
            float visualWidth,
            float visualLength,
            float visualCenterZ,
            float cellSize)
        {
            var root = new GameObject("Vehicle Ground Shadow");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            CreateSkewedVehicleShadow(
                "Vehicle Cast Shadow",
                root.transform,
                new Vector3(cellSize * 0.075f, cellSize * 0.007f, visualCenterZ - visualLength * 0.075f),
                new Vector2(visualWidth * 1.18f, visualLength * 0.98f),
                new Vector2(cellSize * 0.18f, -cellSize * 0.16f),
                GetVehicleCastShadowMaterial());

            CreateEllipse(
                "Vehicle Contact Shadow",
                root.transform,
                new Vector3(0f, cellSize * 0.012f, visualCenterZ - visualLength * 0.020f),
                new Vector2(visualWidth * 1.04f, visualLength * 0.84f),
                GetVehicleShadowMaterial());

            return root;
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

        private static GameObject CreateSkewedVehicleShadow(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector2 size,
            Vector2 skew,
            Material material)
        {
            var shadow = new GameObject(name);
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = localPosition;
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = Vector3.one;

            var halfWidth = Mathf.Max(0.001f, size.x * 0.5f);
            var halfDepth = Mathf.Max(0.001f, size.y * 0.5f);
            var rearOffset = new Vector3(skew.x, 0f, skew.y);
            var vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth) + rearOffset,
                new Vector3(halfWidth, 0f, -halfDepth) + rearOffset,
                new Vector3(halfWidth * 0.88f, 0f, halfDepth),
                new Vector3(-halfWidth * 0.88f, 0f, halfDepth),
                new Vector3(-halfWidth * 0.72f, 0f, -halfDepth * 0.72f) + rearOffset * 0.65f,
                new Vector3(halfWidth * 0.72f, 0f, -halfDepth * 0.72f) + rearOffset * 0.65f,
                new Vector3(halfWidth * 0.62f, 0f, halfDepth * 0.62f),
                new Vector3(-halfWidth * 0.62f, 0f, halfDepth * 0.62f),
            };

            var triangles = new[]
            {
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7,
                4, 5, 6, 4, 6, 7,
            };

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
                vehicleShadowMaterial = CreateShadowMaterial("Vehicle Contact Shadow", new Color(0.34f, 0.38f, 0.42f, 0.26f));
            }

            return vehicleShadowMaterial;
        }

        private static Material GetVehicleCastShadowMaterial()
        {
            if (vehicleCastShadowMaterial == null)
            {
                vehicleCastShadowMaterial = CreateShadowMaterial("Vehicle Cast Shadow", new Color(0.42f, 0.47f, 0.52f, 0.16f));
            }

            return vehicleCastShadowMaterial;
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
            var material = PuzzlePalette.CreateTransparentMaterial(name, color);
            ConfigureTransparent(material);
            return material;
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material == null)
            {
                return;
            }

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
