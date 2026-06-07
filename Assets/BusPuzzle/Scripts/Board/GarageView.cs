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
            if (queuedVehicles.Count == 0)
            {
                HideObstacle();
            }

            return true;
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
            var baseMaterial = PuzzlePalette.CreateSolidMaterial("Garage Base", new Color(0.19f, 0.21f, 0.25f));
            var doorMaterial = PuzzlePalette.CreateSolidMaterial("Garage Door", new Color(0.36f, 0.39f, 0.45f));
            var topMaterial = PuzzlePalette.CreateSolidMaterial("Garage Top", new Color(0.09f, 0.10f, 0.12f));

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Garage Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, cellSize * 0.17f, 0f);
            body.transform.localScale = new Vector3(cellSize * 0.88f, cellSize * 0.34f, cellSize * 0.88f);
            body.GetComponent<Renderer>().sharedMaterial = baseMaterial;
            ConfigureRenderer(body);

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Garage Door";
            door.transform.SetParent(transform, false);
            door.transform.localPosition = GridDirectionUtility.ToWorldVector(ExitDirection) * (cellSize * 0.455f) + Vector3.up * (cellSize * 0.17f);
            door.transform.localRotation = GridDirectionUtility.ToRotation(ExitDirection);
            door.transform.localScale = new Vector3(cellSize * 0.56f, cellSize * 0.24f, cellSize * 0.035f);
            door.GetComponent<Renderer>().sharedMaterial = doorMaterial;
            ConfigureRenderer(door);

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Garage Roof";
            roof.transform.SetParent(transform, false);
            roof.transform.localPosition = new Vector3(0f, cellSize * 0.37f, 0f);
            roof.transform.localScale = new Vector3(cellSize * 0.98f, cellSize * 0.08f, cellSize * 0.98f);
            roof.GetComponent<Renderer>().sharedMaterial = topMaterial;
            ConfigureRenderer(roof);
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
