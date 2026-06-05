using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleDirectionArrow
    {
        public static GameObject Create(Transform parent, float visualWidth, float visualLength, float visualHeight, float visualCenterZ, float cellSize)
        {
            var arrowMaterial = PuzzlePalette.CreateSolidMaterial("White Direction Arrow", UnityEngine.Color.white);
            var arrow = new GameObject("Direction Arrow Icon");
            arrow.transform.SetParent(parent, false);
            arrow.transform.localPosition = new Vector3(0f, visualHeight + cellSize * 0.12f, visualCenterZ);
            arrow.transform.localRotation = Quaternion.identity;

            var meshFilter = arrow.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateArrowMesh(visualWidth * 0.82f, visualLength * 0.58f);

            var meshRenderer = arrow.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = arrowMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return arrow;
        }

        private static Mesh CreateArrowMesh(float width, float length)
        {
            var halfWidth = width * 0.5f;
            var tailHalfWidth = width * 0.16f;
            var halfLength = length * 0.5f;
            var headStart = halfLength - length * 0.34f;

            var mesh = new Mesh { name = "Direction Arrow Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-tailHalfWidth, 0f, -halfLength),
                new Vector3(tailHalfWidth, 0f, -halfLength),
                new Vector3(tailHalfWidth, 0f, headStart),
                new Vector3(halfWidth, 0f, headStart),
                new Vector3(0f, 0f, halfLength),
                new Vector3(-halfWidth, 0f, headStart),
                new Vector3(-tailHalfWidth, 0f, headStart)
            };
            mesh.triangles = new[]
            {
                0, 2, 1,
                0, 6, 2,
                6, 5, 2,
                5, 3, 2,
                5, 4, 3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
