using System;
using UnityEngine;

namespace BusPuzzle
{
    internal static class BoardGeometry
    {
        public static GameObject CreateFlatRect(string name, Transform parent, Vector3 position, Vector2 size, Material material)
        {
            return CreateFlatRect(name, parent, position, size, material, Quaternion.identity);
        }

        public static GameObject CreateFlatRect(string name, Transform parent, Vector3 position, Vector2 size, Material material, Quaternion rotation)
        {
            var flatObject = new GameObject(name);
            flatObject.transform.SetParent(parent, false);
            flatObject.transform.SetPositionAndRotation(position, rotation);

            var halfWidth = size.x * 0.5f;
            var halfDepth = size.y * 0.5f;
            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 3, 2, 0, 2, 1, 0, 2, 3, 0, 1, 2 };
            AssignFlatNormals(mesh);
            mesh.RecalculateBounds();

            var meshFilter = flatObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = flatObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return flatObject;
        }

        public static GameObject CreateFlatSegment(string name, Transform parent, Vector3 start, Vector3 end, float y, float width, Material material)
        {
            start.y = y;
            end.y = y;
            var delta = end - start;
            var length = Mathf.Max(0.01f, delta.magnitude);
            var rotation = delta.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(delta.normalized, Vector3.up)
                : Quaternion.identity;

            return CreateFlatRect(name, parent, Vector3.Lerp(start, end, 0.5f), new Vector2(width, length), material, rotation);
        }

        public static GameObject CreateFlatRoundedRect(string name, Transform parent, Vector3 position, Vector2 size, float radius, Material material)
        {
            return CreateFlatRoundedRect(name, parent, position, size, radius, material, Quaternion.identity);
        }

        public static GameObject CreateFlatRoundedRect(string name, Transform parent, Vector3 position, Vector2 size, float radius, Material material, Quaternion rotation)
        {
            const int cornerSegments = 5;
            var roundedObject = new GameObject(name);
            roundedObject.transform.SetParent(parent, false);
            roundedObject.transform.SetPositionAndRotation(position, rotation);

            radius = Mathf.Clamp(radius, 0.01f, Mathf.Min(size.x, size.y) * 0.5f);
            var halfWidth = size.x * 0.5f;
            var halfDepth = size.y * 0.5f;
            var centers = new[]
            {
                new Vector2(halfWidth - radius, halfDepth - radius),
                new Vector2(-halfWidth + radius, halfDepth - radius),
                new Vector2(-halfWidth + radius, -halfDepth + radius),
                new Vector2(halfWidth - radius, -halfDepth + radius)
            };
            var startAngles = new[] { 0f, 90f, 180f, 270f };
            var points = new System.Collections.Generic.List<Vector2>();

            for (var corner = 0; corner < centers.Length; corner++)
            {
                for (var segment = 0; segment <= cornerSegments; segment++)
                {
                    var angle = (startAngles[corner] + segment * 90f / cornerSegments) * Mathf.Deg2Rad;
                    points.Add(centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }

            var vertices = new Vector3[points.Count + 1];
            vertices[0] = Vector3.zero;
            for (var index = 0; index < points.Count; index++)
            {
                vertices[index + 1] = new Vector3(points[index].x, 0f, points[index].y);
            }

            var triangles = new int[points.Count * 6];
            for (var index = 0; index < points.Count; index++)
            {
                var current = index + 1;
                var next = index + 1 == points.Count ? 1 : index + 2;
                var triangleIndex = index * 6;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = current;
                triangles[triangleIndex + 2] = next;
                triangles[triangleIndex + 3] = 0;
                triangles[triangleIndex + 4] = next;
                triangles[triangleIndex + 5] = current;
            }

            return CreateMeshObject(name, roundedObject.transform, vertices, triangles, material, true);
        }

        public static GameObject CreatePathBand(
            string name,
            Transform parent,
            RotaryLayout layout,
            float centerZ,
            float y,
            float innerOffset,
            float outerOffset,
            Material material)
        {
            var sampleCount = Mathf.Max(8, layout.MeshSampleCount);
            var vertices = new Vector3[sampleCount * 2];
            var triangles = new int[sampleCount * 12];

            for (var index = 0; index < sampleCount; index++)
            {
                var sample = layout.Path.SampleByDistance(layout.Path.Length * index / sampleCount);
                vertices[index * 2] = layout.ToWorldPoint(sample.Point + sample.Outward * innerOffset, centerZ, y);
                vertices[index * 2 + 1] = layout.ToWorldPoint(sample.Point + sample.Outward * outerOffset, centerZ, y);
            }

            for (var index = 0; index < sampleCount; index++)
            {
                var next = (index + 1) % sampleCount;
                var triangleIndex = index * 12;
                var innerCurrent = index * 2;
                var outerCurrent = innerCurrent + 1;
                var innerNext = next * 2;
                var outerNext = innerNext + 1;

                triangles[triangleIndex] = innerCurrent;
                triangles[triangleIndex + 1] = outerCurrent;
                triangles[triangleIndex + 2] = outerNext;
                triangles[triangleIndex + 3] = innerCurrent;
                triangles[triangleIndex + 4] = outerNext;
                triangles[triangleIndex + 5] = innerNext;
                triangles[triangleIndex + 6] = innerCurrent;
                triangles[triangleIndex + 7] = outerNext;
                triangles[triangleIndex + 8] = outerCurrent;
                triangles[triangleIndex + 9] = innerCurrent;
                triangles[triangleIndex + 10] = innerNext;
                triangles[triangleIndex + 11] = outerNext;
            }

            return CreateMeshObject(name, parent, vertices, triangles, material);
        }

        public static GameObject CreateOpenPathBand(
            string name,
            Transform parent,
            RotaryLayout layout,
            FeederRoadPath path,
            float centerZ,
            float y,
            float innerOffset,
            float outerOffset,
            Material material)
        {
            return CreateOpenPathBand(
                name,
                parent,
                layout,
                path,
                centerZ,
                y,
                innerOffset,
                outerOffset,
                material,
                path.Sample);
        }

        public static GameObject CreateOpenPathBand(
            string name,
            Transform parent,
            RotaryLayout layout,
            FeederRoadPath path,
            float centerZ,
            float y,
            float innerOffset,
            float outerOffset,
            Material material,
            Func<float, RotaryPathSample> sampleAtProgress)
        {
            var sampleCount = Mathf.Max(8, Mathf.CeilToInt(path.Length / 0.045f));
            var vertices = new Vector3[sampleCount * 2];
            var triangles = new int[(sampleCount - 1) * 12];

            for (var index = 0; index < sampleCount; index++)
            {
                var t = index / (sampleCount - 1f);
                var sample = sampleAtProgress(t);
                vertices[index * 2] = layout.ToWorldPoint(sample.Point + sample.Outward * innerOffset, centerZ, y);
                vertices[index * 2 + 1] = layout.ToWorldPoint(sample.Point + sample.Outward * outerOffset, centerZ, y);
            }

            for (var index = 0; index < sampleCount - 1; index++)
            {
                var next = index + 1;
                var triangleIndex = index * 12;
                var innerCurrent = index * 2;
                var outerCurrent = innerCurrent + 1;
                var innerNext = next * 2;
                var outerNext = innerNext + 1;

                triangles[triangleIndex] = innerCurrent;
                triangles[triangleIndex + 1] = outerCurrent;
                triangles[triangleIndex + 2] = outerNext;
                triangles[triangleIndex + 3] = innerCurrent;
                triangles[triangleIndex + 4] = outerNext;
                triangles[triangleIndex + 5] = innerNext;
                triangles[triangleIndex + 6] = innerCurrent;
                triangles[triangleIndex + 7] = outerNext;
                triangles[triangleIndex + 8] = outerCurrent;
                triangles[triangleIndex + 9] = innerCurrent;
                triangles[triangleIndex + 10] = innerNext;
                triangles[triangleIndex + 11] = outerNext;
            }

            return CreateMeshObject(name, parent, vertices, triangles, material);
        }

        public static GameObject CreatePathFill(
            string name,
            Transform parent,
            RotaryLayout layout,
            float centerZ,
            float y,
            float edgeOffset,
            Material material)
        {
            var sampleCount = Mathf.Max(8, layout.MeshSampleCount);
            var vertices = new Vector3[sampleCount + 1];
            var triangles = new int[sampleCount * 6];
            vertices[0] = layout.ToWorldPoint(Vector2.zero, centerZ, y);

            for (var index = 0; index < sampleCount; index++)
            {
                var sample = layout.Path.SampleByDistance(layout.Path.Length * index / sampleCount);
                vertices[index + 1] = layout.ToWorldPoint(sample.Point + sample.Outward * edgeOffset, centerZ, y);
            }

            for (var index = 0; index < sampleCount; index++)
            {
                var current = index + 1;
                var next = index + 1 == sampleCount ? 1 : index + 2;
                var triangleIndex = index * 6;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = next;
                triangles[triangleIndex + 2] = current;
                triangles[triangleIndex + 3] = 0;
                triangles[triangleIndex + 4] = current;
                triangles[triangleIndex + 5] = next;
            }

            return CreateMeshObject(name, parent, vertices, triangles, material);
        }

        public static GameObject CreateMeshObject(string name, Transform parent, Vector3[] vertices, int[] triangles, Material material, bool useExistingParent = false)
        {
            var meshObject = useExistingParent ? parent.gameObject : new GameObject(name);
            if (!useExistingParent)
            {
                meshObject.transform.SetParent(parent, false);
            }

            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            AssignFlatNormals(mesh);
            mesh.RecalculateBounds();

            var meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return meshObject;
        }

        private static void AssignFlatNormals(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount <= 0)
            {
                return;
            }

            var normals = new Vector3[mesh.vertexCount];
            for (var index = 0; index < normals.Length; index++)
            {
                normals[index] = Vector3.up;
            }

            mesh.normals = normals;
        }
    }
}
