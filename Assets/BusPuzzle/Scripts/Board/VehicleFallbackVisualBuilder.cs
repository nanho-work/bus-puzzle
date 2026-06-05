using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleFallbackVisualBuilder
    {
        public static void Create(
            PuzzleColor color,
            Transform parent,
            float visualWidth,
            float visualHeight,
            float visualLength,
            float visualCharacterLength,
            float visualCenterZ,
            float visualFrontZ,
            float visualRearZ,
            float cellSize)
        {
            CreateBody(color, parent, visualWidth, visualHeight, visualLength, visualCharacterLength, visualCenterZ, visualFrontZ, cellSize);
            CreateWheels(parent, visualWidth, visualCharacterLength, visualFrontZ, visualRearZ, cellSize);
        }

        private static void CreateBody(
            PuzzleColor colorId,
            Transform parent,
            float visualWidth,
            float visualHeight,
            float visualLength,
            float visualCharacterLength,
            float visualCenterZ,
            float visualFrontZ,
            float cellSize)
        {
            var color = PuzzlePalette.ToColor(colorId);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, visualHeight * 0.5f, visualCenterZ);
            body.transform.localScale = new Vector3(visualWidth, visualHeight, visualLength);
            body.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial($"{PuzzlePalette.DisplayName(colorId)} Bus Body", color);

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Front Cabin";
            cabin.transform.SetParent(parent, false);
            cabin.transform.localPosition = new Vector3(0f, visualHeight + cellSize * 0.14f, visualFrontZ - visualCharacterLength * 0.18f);
            cabin.transform.localScale = new Vector3(visualWidth * 0.86f, cellSize * 0.22f, visualCharacterLength * 0.58f);
            cabin.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial($"{PuzzlePalette.DisplayName(colorId)} Bus Cabin", PuzzlePalette.Darken(color, 0.14f));
        }

        private static void CreateWheels(
            Transform parent,
            float visualWidth,
            float visualCharacterLength,
            float visualFrontZ,
            float visualRearZ,
            float cellSize)
        {
            var wheelMaterial = PuzzlePalette.CreateSolidMaterial("Wheel", new Color(0.08f, 0.09f, 0.11f));
            var rearZ = visualRearZ + visualCharacterLength * 0.20f;
            var frontZ = visualFrontZ - visualCharacterLength * 0.20f;
            var xOffset = visualWidth * 0.58f;
            var wheelY = cellSize * 0.12f;
            var wheelScale = new Vector3(cellSize * 0.24f, cellSize * 0.13f, cellSize * 0.24f);
            var wheelPositions = new[]
            {
                new Vector3(-xOffset, wheelY, rearZ),
                new Vector3(xOffset, wheelY, rearZ),
                new Vector3(-xOffset, wheelY, frontZ),
                new Vector3(xOffset, wheelY, frontZ)
            };

            foreach (var localPosition in wheelPositions)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(parent, false);
                wheel.transform.localPosition = localPosition;
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = wheelScale;
                wheel.GetComponent<Renderer>().sharedMaterial = wheelMaterial;
            }
        }
    }
}
