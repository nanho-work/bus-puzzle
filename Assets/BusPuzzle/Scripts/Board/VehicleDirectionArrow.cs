using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleDirectionArrow
    {
        private const float ChevronWidthScale = 0.42f;
        private const float ChevronLengthScale = 0.32f;
        private const float StrokeWidthScale = 0.12f;
        private const float OutlineScale = 1.22f;
        private const float MarkerFrontOffsetScale = 0.24f;

        public static GameObject Create(Transform parent, float visualWidth, float visualLength, float visualHeight, float visualCenterZ, float cellSize)
        {
            var arrowMaterial = PuzzlePalette.CreateTransparentMaterial("White Direction Chevron", new UnityEngine.Color(1f, 1f, 1f, 0.70f));
            var outlineMaterial = PuzzlePalette.CreateTransparentMaterial("Direction Chevron Outline", new UnityEngine.Color(0.08f, 0.11f, 0.14f, 0.38f));
            ConfigureMarkerMaterial(arrowMaterial);
            ConfigureMarkerMaterial(outlineMaterial);

            var arrow = new GameObject("Direction Chevron Icon");
            arrow.transform.SetParent(parent, false);
            arrow.transform.localPosition = new Vector3(0f, visualHeight + cellSize * 0.08f, visualCenterZ + visualLength * MarkerFrontOffsetScale);
            arrow.transform.localRotation = Quaternion.identity;

            CreateChevronLayer(
                "Direction Chevron Outline",
                arrow.transform,
                visualWidth * ChevronWidthScale * OutlineScale,
                visualLength * ChevronLengthScale * OutlineScale,
                visualWidth * StrokeWidthScale * OutlineScale,
                -cellSize * 0.003f,
                outlineMaterial);

            CreateChevronLayer(
                "Direction Chevron Fill",
                arrow.transform,
                visualWidth * ChevronWidthScale,
                visualLength * ChevronLengthScale,
                visualWidth * StrokeWidthScale,
                0f,
                arrowMaterial);

            return arrow;
        }

        private static void CreateChevronLayer(string name, Transform parent, float width, float length, float strokeWidth, float localHeight, Material material)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = new Vector3(0f, localHeight, 0f);

            var meshFilter = layer.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateChevronMesh(width, length, strokeWidth);

            var meshRenderer = layer.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static Mesh CreateChevronMesh(float width, float length, float strokeWidth)
        {
            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;
            var tip = new Vector2(0f, halfLength);
            var leftRear = new Vector2(-halfWidth, -halfLength);
            var rightRear = new Vector2(halfWidth, -halfLength);
            var vertices = new List<Vector3>(8);
            var triangles = new List<int>(12);

            AddStroke(vertices, triangles, leftRear, tip, strokeWidth);
            AddStroke(vertices, triangles, rightRear, tip, strokeWidth);

            var mesh = new Mesh { name = "Direction Chevron Mesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddStroke(List<Vector3> vertices, List<int> triangles, Vector2 start, Vector2 end, float strokeWidth)
        {
            var direction = (end - start).normalized;
            var normal = new Vector2(-direction.y, direction.x) * (strokeWidth * 0.5f);
            var index = vertices.Count;

            vertices.Add(new Vector3(start.x + normal.x, 0f, start.y + normal.y));
            vertices.Add(new Vector3(start.x - normal.x, 0f, start.y - normal.y));
            vertices.Add(new Vector3(end.x + normal.x, 0f, end.y + normal.y));
            vertices.Add(new Vector3(end.x - normal.x, 0f, end.y - normal.y));

            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
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
