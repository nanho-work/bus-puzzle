using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class VehicleBoardingCounter
    {
        private const float CounterCharacterScale = 0.135f;

        private readonly Transform root;
        private readonly TextMesh text;
        private readonly TextMesh shadowText;
        private Vector3 worldPosition;

        private VehicleBoardingCounter(Transform root, TextMesh text, TextMesh shadowText)
        {
            this.root = root;
            this.text = text;
            this.shadowText = shadowText;
            worldPosition = root.position;
            root.gameObject.SetActive(false);
        }

        public bool IsVisible => root != null && root.gameObject.activeSelf;

        public static VehicleBoardingCounter Create(
            Transform parent,
            PuzzleColor color,
            float cellSize,
            float visualRearZ)
        {
            var counterRoot = new GameObject("Boarding Counter").transform;
            counterRoot.SetParent(parent, false);
            counterRoot.localPosition = new Vector3(0f, cellSize * 0.18f, visualRearZ - cellSize * 0.18f);

            CreateCounterBadge(counterRoot, "Counter Background Shadow", new Color(0.06f, 0.07f, 0.09f), new Vector3(cellSize * 0.009f, -cellSize * 0.009f, 0.010f), cellSize);
            CreateCounterBadge(counterRoot, "Counter Badge", PuzzlePalette.ToColor(color), Vector3.zero, cellSize);
            var shadowText = CreateCounterText(counterRoot, "Counter Shadow", new Color(0.02f, 0.025f, 0.03f), new Vector3(cellSize * 0.004f, -cellSize * 0.004f, -0.018f), cellSize);
            var text = CreateCounterText(counterRoot, "Counter Text", UnityEngine.Color.white, new Vector3(0f, 0f, -0.028f), cellSize);

            return new VehicleBoardingCounter(counterRoot, text, shadowText);
        }

        public void SetWorldPosition(Vector3 position)
        {
            worldPosition = position;
            if (root != null)
            {
                root.position = worldPosition;
            }
        }

        public void Show(int remainingPeople)
        {
            UpdateText(remainingPeople);
            if (root == null)
            {
                return;
            }

            root.position = worldPosition;
            root.gameObject.SetActive(true);
            FaceCamera();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        public void UpdateText(int remainingPeople)
        {
            var value = remainingPeople.ToString();
            if (text != null)
            {
                text.text = value;
            }

            if (shadowText != null)
            {
                shadowText.text = value;
            }
        }

        public void LateUpdate()
        {
            if (!IsVisible)
            {
                return;
            }

            root.position = worldPosition;
            FaceCamera();
        }

        private void FaceCamera()
        {
            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera != null && root != null)
            {
                root.rotation = camera.transform.rotation;
            }
        }

        private static void CreateCounterBadge(Transform parent, string name, UnityEngine.Color color, Vector3 localPosition, float cellSize)
        {
            var badge = new GameObject(name);
            badge.transform.SetParent(parent, false);
            badge.transform.localPosition = localPosition;

            var meshFilter = badge.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateRoundedBadgeMesh(cellSize * 0.90f, cellSize * 0.43f, cellSize * 0.152f);

            var renderer = badge.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = PuzzlePalette.CreateSolidMaterial(name, color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static TextMesh CreateCounterText(Transform parent, string name, UnityEngine.Color color, Vector3 localPosition, float cellSize)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;

            var counterText = textObject.AddComponent<TextMesh>();
            counterText.anchor = TextAnchor.MiddleCenter;
            counterText.alignment = TextAlignment.Center;
            counterText.characterSize = cellSize * CounterCharacterScale;
            counterText.fontSize = 36;
            counterText.color = color;

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return counterText;
        }

        private static Mesh CreateRoundedBadgeMesh(float width, float height, float radius)
        {
            const int cornerSegments = 5;
            radius = Mathf.Clamp(radius, 0.01f, Mathf.Min(width, height) * 0.5f);

            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var centers = new[]
            {
                new Vector2(halfWidth - radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, -halfHeight + radius),
                new Vector2(halfWidth - radius, -halfHeight + radius)
            };
            var startAngles = new[] { 0f, 90f, 180f, 270f };
            var points = new List<Vector2>();

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
                vertices[index + 1] = new Vector3(points[index].x, points[index].y, 0f);
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

            var mesh = new Mesh { name = "Counter Badge Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
