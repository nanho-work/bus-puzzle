using UnityEngine;

namespace BusPuzzle
{
    internal static class ParkingGridBuilder
    {
        private const float SurfaceY = -0.033f;
        private const float PanelY = -0.029f;
        private const float MarkingY = -0.018f;

        public static void Create(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            float topExtensionZ = 0f)
        {
            topExtensionZ = Mathf.Max(0f, topExtensionZ);
            var surfaceMaterial = PuzzlePalette.CreateSolidMaterial("Bus Yard Surface", new Color(0.61f, 0.72f, 0.78f));
            var panelMaterialA = PuzzlePalette.CreateTransparentMaterial("Bus Yard Concrete Panel A", new Color(0.78f, 0.88f, 0.92f, 0.08f));
            var panelMaterialB = PuzzlePalette.CreateTransparentMaterial("Bus Yard Concrete Panel B", new Color(0.48f, 0.60f, 0.66f, 0.055f));
            var laneMaterial = PuzzlePalette.CreateTransparentMaterial("Bus Yard Faded Lane Mark", new Color(0.94f, 0.98f, 1.00f, 0.16f));
            var safetyMaterial = PuzzlePalette.CreateTransparentMaterial("Bus Yard Safety Stripe", new Color(0.96f, 0.72f, 0.15f, 0.30f));
            var gridWidth = columns * cellSize;
            var gridDepth = rows * cellSize + topExtensionZ;
            var centerZ = gridBottomZ + (rows - 1) * cellSize * 0.5f + topExtensionZ * 0.5f;
            var contentBottomZ = gridBottomZ + topExtensionZ * 0.5f;
            var center = new Vector3(0f, SurfaceY, centerZ);

            BoardGeometry.CreateFlatRoundedRect(
                "Bus Yard Surface",
                parent,
                center,
                new Vector2(gridWidth + cellSize * 0.14f, gridDepth + cellSize * 0.14f),
                cellSize * 0.10f,
                surfaceMaterial);

            CreateConcretePanels(parent, columns, rows, cellSize, gridBottomZ, topExtensionZ, panelMaterialA, panelMaterialB);
            CreateFadedLaneDividers(parent, columns, rows, cellSize, contentBottomZ, laneMaterial);
            CreateSafetyStripes(parent, columns, rows, cellSize, gridBottomZ, topExtensionZ, safetyMaterial);
        }

        private static void CreateConcretePanels(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            float topExtensionZ,
            Material first,
            Material second)
        {
            var panelCount = 4;
            var gridWidth = columns * cellSize;
            var panelDepth = (rows * cellSize + topExtensionZ) / panelCount;
            for (var index = 0; index < panelCount; index++)
            {
                var centerZ = gridBottomZ - cellSize * 0.5f + panelDepth * (index + 0.5f);
                BoardGeometry.CreateFlatRect(
                    $"Bus Yard Concrete Panel {index + 1}",
                    parent,
                    new Vector3(0f, PanelY, centerZ),
                    new Vector2(gridWidth * 0.98f, panelDepth - cellSize * 0.10f),
                    index % 2 == 0 ? first : second);
            }
        }

        private static void CreateFadedLaneDividers(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            Material material)
        {
            var gridWidth = columns * cellSize;
            CreateDashedHorizontalLine(parent, "Bus Yard Lane Upper", gridBottomZ + cellSize * 8.5f, gridWidth, cellSize, material);
            CreateDashedHorizontalLine(parent, "Bus Yard Lane Lower", gridBottomZ + cellSize * 4.5f, gridWidth, cellSize, material);
        }

        private static void CreateDashedHorizontalLine(
            Transform parent,
            string name,
            float z,
            float width,
            float cellSize,
            Material material)
        {
            const int dashCount = 6;
            var dashWidth = width / (dashCount * 1.9f);
            for (var index = 0; index < dashCount; index++)
            {
                var x = Mathf.Lerp(-width * 0.40f, width * 0.40f, index / (dashCount - 1f));
                BoardGeometry.CreateFlatRect(
                    $"{name} Dash {index + 1}",
                    parent,
                    new Vector3(x, MarkingY, z),
                    new Vector2(dashWidth, cellSize * 0.030f),
                    material);
            }
        }

        private static void CreateSafetyStripes(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            float topExtensionZ,
            Material material)
        {
            var leftX = -(columns - 1) * cellSize * 0.5f + cellSize * 0.42f;
            var rightX = (columns - 1) * cellSize * 0.5f - cellSize * 0.42f;
            var lowerZ = gridBottomZ + cellSize * 0.45f;
            var upperZ = gridBottomZ + (rows - 1.45f) * cellSize + topExtensionZ;

            for (var index = 0; index < 3; index++)
            {
                var offset = index * cellSize * 0.22f;
                CreateSafetyStripe(parent, $"Bus Yard Lower Safety Stripe {index + 1}", leftX + offset, lowerZ, material);
                CreateSafetyStripe(parent, $"Bus Yard Upper Safety Stripe {index + 1}", rightX - offset, upperZ, material);
            }
        }

        private static void CreateSafetyStripe(Transform parent, string name, float x, float z, Material material)
        {
            BoardGeometry.CreateFlatRect(
                name,
                parent,
                new Vector3(x, MarkingY + 0.002f, z),
                new Vector2(0.040f, 0.36f),
                material,
                Quaternion.Euler(0f, -28f, 0f));
        }
    }
}
