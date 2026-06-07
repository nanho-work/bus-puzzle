using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class GarageView : MonoBehaviour
    {
        private readonly Queue<BusDefinition> queuedVehicles = new Queue<BusDefinition>();

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
            var directionMaterial = PuzzlePalette.CreateSolidMaterial("Garage Portal Direction Mark", new Color(0.68f, 0.79f, 1.00f));

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
            CreateBox("Garage Exit Arrow Shaft", directionRoot.transform, new Vector3(0f, cellSize * 0.145f, cellSize * 0.18f), new Vector3(cellSize * 0.070f, cellSize * 0.018f, cellSize * 0.34f), directionMaterial);
            CreateBox("Garage Exit Arrow Left Head", directionRoot.transform, new Vector3(-cellSize * 0.075f, cellSize * 0.147f, cellSize * 0.36f), new Vector3(cellSize * 0.070f, cellSize * 0.018f, cellSize * 0.22f), directionMaterial, Quaternion.Euler(0f, -36f, 0f));
            CreateBox("Garage Exit Arrow Right Head", directionRoot.transform, new Vector3(cellSize * 0.075f, cellSize * 0.147f, cellSize * 0.36f), new Vector3(cellSize * 0.070f, cellSize * 0.018f, cellSize * 0.22f), directionMaterial, Quaternion.Euler(0f, 36f, 0f));
        }

        private static void CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            CreateBox(name, parent, localPosition, localScale, material, Quaternion.identity);
        }

        private static void CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = localRotation;
            box.transform.localScale = localScale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            ConfigureRenderer(box);
        }

        private void CreateCounter()
        {
            var badgeMaterial = PuzzlePalette.CreateSolidMaterial("Garage Counter Badge", new Color(0.96f, 0.76f, 0.16f));
            var badge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            badge.name = "Garage Counter Badge";
            badge.transform.SetParent(transform, false);
            badge.transform.localPosition = new Vector3(0f, cellSize * 0.48f, 0f);
            badge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            badge.transform.localScale = new Vector3(cellSize * 0.18f, cellSize * 0.018f, cellSize * 0.18f);
            badge.GetComponent<Renderer>().sharedMaterial = badgeMaterial;
            ConfigureRenderer(badge);

            shadowText = CreateCounterText("Garage Counter Shadow", new Color(0.03f, 0.03f, 0.035f), new Vector3(cellSize * 0.006f, cellSize * 0.006f, -cellSize * 0.015f));
            counterText = CreateCounterText("Garage Counter Text", Color.white, new Vector3(0f, 0f, -cellSize * 0.024f));
        }

        private TextMesh CreateCounterText(string name, Color color, Vector3 localPosition)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, cellSize * 0.50f, 0f) + localPosition;

            var text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = cellSize * 0.20f;
            text.fontSize = 48;
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

            if (counterText != null)
            {
                counterText.transform.rotation = camera.transform.rotation;
            }

            if (shadowText != null)
            {
                shadowText.transform.rotation = camera.transform.rotation;
            }
        }

        private static void ConfigureRenderer(GameObject gameObject)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
