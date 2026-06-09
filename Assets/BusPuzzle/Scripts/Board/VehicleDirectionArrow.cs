using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleDirectionArrow
    {
        private const float ArrowWidthScale = 0.34f;
        private const float ArrowLengthScale = 0.30f;
        private const float ArrowShaftWidthScale = 0.090f;
        private const float ArrowHeadLengthRatio = 0.42f;
        private const float OutlineScale = 1.14f;
        private const float MarkerFrontOffsetScale = 0f;
        private const float MarkerLiftScale = 0.045f;

        public static GameObject Create(Transform parent, float visualWidth, float visualLength, float visualHeight, float visualCenterZ, float cellSize)
        {
            var arrowMaterial = PuzzlePalette.CreateTransparentMaterial("White Direction Arrow", new UnityEngine.Color(1f, 1f, 1f, 0.64f));
            var outlineMaterial = PuzzlePalette.CreateTransparentMaterial("Direction Arrow Outline", new UnityEngine.Color(0.04f, 0.06f, 0.08f, 0.32f));
            ConfigureMarkerMaterial(arrowMaterial);
            ConfigureMarkerMaterial(outlineMaterial);

            var arrow = new GameObject("Direction Arrow Icon");
            arrow.transform.SetParent(parent, false);
            arrow.transform.localPosition = new Vector3(0f, visualHeight + cellSize * MarkerLiftScale, visualCenterZ + visualLength * MarkerFrontOffsetScale);
            arrow.transform.localRotation = Quaternion.identity;

            CreateArrowLayer(
                "Direction Arrow Outline",
                arrow.transform,
                visualWidth * ArrowWidthScale * OutlineScale,
                visualLength * ArrowLengthScale * OutlineScale,
                visualWidth * ArrowShaftWidthScale * OutlineScale,
                -cellSize * 0.003f,
                outlineMaterial);

            CreateArrowLayer(
                "Direction Arrow Fill",
                arrow.transform,
                visualWidth * ArrowWidthScale,
                visualLength * ArrowLengthScale,
                visualWidth * ArrowShaftWidthScale,
                0f,
                arrowMaterial);

            return arrow;
        }

        private static void CreateArrowLayer(string name, Transform parent, float width, float length, float shaftWidth, float localHeight, Material material)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = new Vector3(0f, localHeight, 0f);

            var meshFilter = layer.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateArrowMesh(width, length, shaftWidth);

            var meshRenderer = layer.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static Mesh CreateArrowMesh(float width, float length, float shaftWidth)
        {
            var halfWidth = width * 0.5f;
            var halfShaftWidth = shaftWidth * 0.5f;
            var halfLength = length * 0.5f;
            var headLength = length * ArrowHeadLengthRatio;
            var headBaseZ = halfLength - headLength;
            var vertices = new[]
            {
                new Vector3(0f, 0f, halfLength),
                new Vector3(halfWidth, 0f, headBaseZ),
                new Vector3(halfShaftWidth, 0f, headBaseZ),
                new Vector3(halfShaftWidth, 0f, -halfLength),
                new Vector3(-halfShaftWidth, 0f, -halfLength),
                new Vector3(-halfShaftWidth, 0f, headBaseZ),
                new Vector3(-halfWidth, 0f, headBaseZ)
            };

            var triangles = new[]
            {
                0, 1, 6,
                1, 2, 6,
                2, 5, 6,
                2, 3, 5,
                3, 4, 5
            };

            var mesh = new Mesh { name = "Direction Arrow Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void ConfigureMarkerMaterial(Material material)
        {
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }
        }
    }
}
