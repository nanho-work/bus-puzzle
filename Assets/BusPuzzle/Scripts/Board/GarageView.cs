using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class GarageView : MonoBehaviour
    {
        private const float CounterCharacterScale = 0.145f;
        private const float CounterBadgeWidthScale = 0.54f;
        private const float CounterBadgeHeightScale = 0.36f;

        private readonly Queue<BusDefinition> queuedVehicles = new Queue<BusDefinition>();

        private Transform counterRoot;
        private TextMesh counterText;
        private TextMesh shadowText;
        private float cellSize;

        public Vector2Int GridPosition { get; private set; }
        public GridDirection ExitDirection { get; private set; }
        public int QueuedVehicleCount => queuedVehicles.Count;
        public bool IsActiveObstacle => gameObject.activeSelf;
        public VehicleFootprint CurrentFootprint => new VehicleFootprint(
            transform.position,
            Vector3.right,
            Vector3.forward,
            cellSize * 0.45f,
            cellSize * 0.45f);

        public Vector3 GetVehicleExitStartPosition(BusDefinition vehicle)
        {
            var finalPosition = BoardLayoutConfig.GridToWorld(vehicle.GridPosition, vehicle.PositionOffsetCells);
            return finalPosition - GridDirectionUtility.ToWorldVector(ExitDirection) * (cellSize * 0.92f);
        }

        public static GarageView Create(GarageDefinition definition, Transform parent, float cellSize)
        {
            var garageObject = new GameObject($"Garage {definition.GridPosition.x},{definition.GridPosition.y}");
            garageObject.transform.SetParent(parent, false);

            var view = garageObject.AddComponent<GarageView>();
            view.Initialize(definition, cellSize);
            return view;
        }

        public bool TryTakeNextVehicle(out BusDefinition vehicle)
        {
            if (queuedVehicles.Count == 0)
            {
                vehicle = default;
                HideObstacle();
                return false;
            }

            vehicle = queuedVehicles.Dequeue();
            UpdateCounter();
            return true;
        }

        public void HideIfEmpty()
        {
            if (queuedVehicles.Count == 0)
            {
                HideObstacle();
            }
        }

        private void Initialize(GarageDefinition definition, float boardCellSize)
        {
            cellSize = boardCellSize;
            GridPosition = definition.GridPosition;
            ExitDirection = definition.ExitDirection;
            foreach (var vehicle in definition.QueuedVehicles)
            {
                queuedVehicles.Enqueue(vehicle);
            }

            transform.position = BoardLayoutConfig.GridToWorld(GridPosition) + Vector3.up * (cellSize * 0.08f);
            CreateBody();
            CreateCounter();
            UpdateCounter();

            if (queuedVehicles.Count == 0)
            {
                HideObstacle();
            }
        }

        private void LateUpdate()
        {
            FaceCamera();
        }

        private void HideObstacle()
        {
            gameObject.SetActive(false);
        }

        private void CreateBody()
        {
            var shadowMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Ground Shadow", new Color(0.18f, 0.24f, 0.32f));
            var floorMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Floor", new Color(0.45f, 0.56f, 0.67f));
            var floorHighlightMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Floor Highlight", new Color(0.62f, 0.73f, 0.84f));
            var sideMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Side Wall", new Color(0.42f, 0.52f, 0.68f));
            var sideHighlightMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Side Highlight", new Color(0.57f, 0.68f, 0.82f));
            var interiorMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Interior", new Color(0.16f, 0.21f, 0.43f));
            var interiorGlowMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Interior Glow", new Color(0.25f, 0.33f, 0.62f));
            var roofMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Roof", new Color(0.34f, 0.46f, 0.66f));
            var roofHighlightMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Roof Highlight", new Color(0.56f, 0.68f, 0.84f));
            var frameMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Silver Frame", new Color(0.72f, 0.79f, 0.86f));
            var frameShadowMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Frame Shadow", new Color(0.31f, 0.40f, 0.57f));
            var directionShadowMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Direction Mark Shadow", new Color(0.08f, 0.11f, 0.14f));
            var directionMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Direction Mark", new Color(0.92f, 0.97f, 1.00f));

            var directionRoot = new GameObject("Garage Tunnel Direction Root");
            directionRoot.transform.SetParent(transform, false);
            directionRoot.transform.localRotation = GridDirectionUtility.ToRotation(ExitDirection);
            const float taperDegrees = 7f;

            CreateBox("Garage Portal Ground Shadow", directionRoot.transform, new Vector3(0f, cellSize * 0.020f, -cellSize * 0.025f), new Vector3(cellSize * 1.02f, cellSize * 0.040f, cellSize * 1.02f), shadowMaterial);
            CreateBox("Garage Portal Floor", directionRoot.transform, new Vector3(0f, cellSize * 0.070f, 0f), new Vector3(cellSize * 0.92f, cellSize * 0.105f, cellSize * 0.94f), floorMaterial);
            CreateBox("Garage Portal Floor Center Highlight", directionRoot.transform, new Vector3(0f, cellSize * 0.132f, cellSize * 0.10f), new Vector3(cellSize * 0.52f, cellSize * 0.018f, cellSize * 0.42f), floorHighlightMaterial);
            CreateBox("Garage Portal Rear Floor Shade", directionRoot.transform, new Vector3(0f, cellSize * 0.134f, -cellSize * 0.30f), new Vector3(cellSize * 0.30f, cellSize * 0.020f, cellSize * 0.24f), interiorGlowMaterial);
            CreateBox("Garage Portal Interior Back", directionRoot.transform, new Vector3(0f, cellSize * 0.22f, -cellSize * 0.43f), new Vector3(cellSize * 0.48f, cellSize * 0.31f, cellSize * 0.075f), interiorMaterial);
            CreateBox("Garage Portal Interior Glow", directionRoot.transform, new Vector3(0f, cellSize * 0.21f, -cellSize * 0.34f), new Vector3(cellSize * 0.30f, cellSize * 0.22f, cellSize * 0.045f), interiorGlowMaterial);
            CreateBox("Garage Portal Left Outer Wall", directionRoot.transform, new Vector3(-cellSize * 0.405f, cellSize * 0.22f, -cellSize * 0.04f), new Vector3(cellSize * 0.12f, cellSize * 0.36f, cellSize * 0.84f), sideMaterial, Quaternion.Euler(0f, -taperDegrees, 0f));
            CreateBox("Garage Portal Right Outer Wall", directionRoot.transform, new Vector3(cellSize * 0.405f, cellSize * 0.22f, -cellSize * 0.04f), new Vector3(cellSize * 0.12f, cellSize * 0.36f, cellSize * 0.84f), sideMaterial, Quaternion.Euler(0f, taperDegrees, 0f));
            CreateBox("Garage Portal Left Inner Highlight", directionRoot.transform, new Vector3(-cellSize * 0.305f, cellSize * 0.225f, cellSize * 0.03f), new Vector3(cellSize * 0.035f, cellSize * 0.28f, cellSize * 0.62f), sideHighlightMaterial, Quaternion.Euler(0f, -taperDegrees, 0f));
            CreateBox("Garage Portal Right Inner Highlight", directionRoot.transform, new Vector3(cellSize * 0.305f, cellSize * 0.225f, cellSize * 0.03f), new Vector3(cellSize * 0.035f, cellSize * 0.28f, cellSize * 0.62f), sideHighlightMaterial, Quaternion.Euler(0f, taperDegrees, 0f));
            CreateBox("Garage Portal Roof", directionRoot.transform, new Vector3(0f, cellSize * 0.43f, -cellSize * 0.04f), new Vector3(cellSize * 0.90f, cellSize * 0.105f, cellSize * 0.90f), roofMaterial);
            CreateBox("Garage Portal Roof Highlight", directionRoot.transform, new Vector3(0f, cellSize * 0.492f, cellSize * 0.09f), new Vector3(cellSize * 0.54f, cellSize * 0.020f, cellSize * 0.34f), roofHighlightMaterial);
            CreateBox("Garage Portal Rear Roof Shade", directionRoot.transform, new Vector3(0f, cellSize * 0.494f, -cellSize * 0.30f), new Vector3(cellSize * 0.30f, cellSize * 0.018f, cellSize * 0.22f), interiorGlowMaterial);
            CreateBox("Garage Portal Mouth Shadow", directionRoot.transform, new Vector3(0f, cellSize * 0.23f, cellSize * 0.475f), new Vector3(cellSize * 0.58f, cellSize * 0.26f, cellSize * 0.050f), interiorMaterial);
            CreateBox("Garage Portal Mouth Glow", directionRoot.transform, new Vector3(0f, cellSize * 0.245f, cellSize * 0.502f), new Vector3(cellSize * 0.42f, cellSize * 0.18f, cellSize * 0.024f), interiorGlowMaterial);
            CreateBox("Garage Portal Left Frame Shadow", directionRoot.transform, new Vector3(-cellSize * 0.39f, cellSize * 0.23f, cellSize * 0.505f), new Vector3(cellSize * 0.055f, cellSize * 0.40f, cellSize * 0.080f), frameShadowMaterial);
            CreateBox("Garage Portal Right Frame Shadow", directionRoot.transform, new Vector3(cellSize * 0.39f, cellSize * 0.23f, cellSize * 0.505f), new Vector3(cellSize * 0.055f, cellSize * 0.40f, cellSize * 0.080f), frameShadowMaterial);
            CreateBox("Garage Portal Left Silver Frame", directionRoot.transform, new Vector3(-cellSize * 0.34f, cellSize * 0.245f, cellSize * 0.525f), new Vector3(cellSize * 0.075f, cellSize * 0.38f, cellSize * 0.065f), frameMaterial);
            CreateBox("Garage Portal Right Silver Frame", directionRoot.transform, new Vector3(cellSize * 0.34f, cellSize * 0.245f, cellSize * 0.525f), new Vector3(cellSize * 0.075f, cellSize * 0.38f, cellSize * 0.065f), frameMaterial);
            CreateBox("Garage Portal Top Silver Frame", directionRoot.transform, new Vector3(0f, cellSize * 0.425f, cellSize * 0.525f), new Vector3(cellSize * 0.74f, cellSize * 0.075f, cellSize * 0.065f), frameMaterial);
            CreateBox("Garage Exit Arrow Shadow Shaft", directionRoot.transform, new Vector3(0f, cellSize * 0.142f, cellSize * 0.18f), new Vector3(cellSize * 0.088f, cellSize * 0.016f, cellSize * 0.35f), directionShadowMaterial);
            CreateBox("Garage Exit Arrow Shadow Left Head", directionRoot.transform, new Vector3(-cellSize * 0.076f, cellSize * 0.143f, cellSize * 0.36f), new Vector3(cellSize * 0.084f, cellSize * 0.016f, cellSize * 0.23f), directionShadowMaterial, Quaternion.Euler(0f, -36f, 0f));
            CreateBox("Garage Exit Arrow Shadow Right Head", directionRoot.transform, new Vector3(cellSize * 0.076f, cellSize * 0.143f, cellSize * 0.36f), new Vector3(cellSize * 0.084f, cellSize * 0.016f, cellSize * 0.23f), directionShadowMaterial, Quaternion.Euler(0f, 36f, 0f));
            CreateBox("Garage Exit Arrow Shaft", directionRoot.transform, new Vector3(0f, cellSize * 0.148f, cellSize * 0.18f), new Vector3(cellSize * 0.060f, cellSize * 0.017f, cellSize * 0.32f), directionMaterial);
            CreateBox("Garage Exit Arrow Left Head", directionRoot.transform, new Vector3(-cellSize * 0.070f, cellSize * 0.150f, cellSize * 0.35f), new Vector3(cellSize * 0.058f, cellSize * 0.017f, cellSize * 0.20f), directionMaterial, Quaternion.Euler(0f, -36f, 0f));
            CreateBox("Garage Exit Arrow Right Head", directionRoot.transform, new Vector3(cellSize * 0.070f, cellSize * 0.150f, cellSize * 0.35f), new Vector3(cellSize * 0.058f, cellSize * 0.017f, cellSize * 0.20f), directionMaterial, Quaternion.Euler(0f, 36f, 0f));
        }

        private static void CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            CreateBox(name, parent, localPosition, localScale, material, Quaternion.identity);
        }

        private static void CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
        {
            var box = VisualPrimitiveFactory.Create(PrimitiveType.Cube, name);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = localRotation;
            box.transform.localScale = localScale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            ConfigureRenderer(box);
        }

        private void CreateCounter()
        {
            counterRoot = new GameObject("Garage Counter").transform;
            counterRoot.SetParent(transform, false);
            var exit = GridDirectionUtility.ToWorldVector(ExitDirection);
            counterRoot.localPosition = exit * (cellSize * 0.42f) + Vector3.up * (cellSize * 0.63f);

            CreateCounterBadge(
                counterRoot,
                "Garage Counter Background Shadow",
                new Color(0.04f, 0.05f, 0.06f),
                new Vector3(cellSize * 0.008f, -cellSize * 0.008f, cellSize * 0.010f));
            CreateCounterBadge(
                counterRoot,
                "Garage Counter Badge",
                new Color(0.96f, 0.66f, 0.12f),
                Vector3.zero);

            shadowText = CreateCounterText("Garage Counter Shadow", new Color(0.03f, 0.025f, 0.02f), new Vector3(cellSize * 0.004f, -cellSize * 0.004f, -cellSize * 0.018f));
            counterText = CreateCounterText("Garage Counter Text", Color.white, new Vector3(0f, 0f, -cellSize * 0.028f));
        }

        private void CreateCounterBadge(Transform parent, string name, Color color, Vector3 localPosition)
        {
            var badge = new GameObject(name);
            badge.transform.SetParent(parent, false);
            badge.transform.localPosition = localPosition;

            var meshFilter = badge.AddComponent<MeshFilter>();
            var mesh = CreateRoundedBadgeMesh(
                cellSize * CounterBadgeWidthScale,
                cellSize * CounterBadgeHeightScale,
                cellSize * 0.105f);
            meshFilter.sharedMesh = mesh;
            RuntimeOwnedMesh.Attach(badge, mesh);

            var renderer = badge.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = PuzzlePalette.CreateSolidMaterial(name, color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private TextMesh CreateCounterText(string name, Color color, Vector3 localPosition)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(counterRoot, false);
            textObject.transform.localPosition = localPosition;

            var text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = cellSize * CounterCharacterScale;
            text.fontSize = 40;
            text.color = color;
            GameFontProvider.ApplyToTextMesh(text, FontStyle.Bold);

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return text;
        }

        private void UpdateCounter()
        {
            var value = queuedVehicles.Count.ToString();
            if (counterText != null)
            {
                counterText.text = value;
            }

            if (shadowText != null)
            {
                shadowText.text = value;
            }
        }

        private void FaceCamera()
        {
            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            if (counterRoot != null)
            {
                counterRoot.rotation = camera.transform.rotation;
            }
        }

        private static void ConfigureRenderer(GameObject gameObject)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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

            var mesh = new Mesh { name = "Garage Counter Badge Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
