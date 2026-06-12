using UnityEngine;

namespace BusPuzzle
{
    internal static class ParkingGridBuilder
    {
        private const float SurfaceY = -0.033f;
        private const float PanelY = -0.029f;
        private const float MarkingY = -0.018f;
        private const float GuideY = -0.016f;

        public static void Create(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ)
        {
            var surfaceMaterial = PuzzlePalette.CreateSolidMaterial("Bus Yard Surface", new Color(0.55f, 0.68f, 0.74f));
            var panelMaterialA = PuzzlePalette.CreateTransparentMaterial("Bus Yard Concrete Panel A", new Color(0.72f, 0.82f, 0.86f, 0.12f));
            var panelMaterialB = PuzzlePalette.CreateTransparentMaterial("Bus Yard Concrete Panel B", new Color(0.44f, 0.55f, 0.62f, 0.10f));
            var laneMaterial = PuzzlePalette.CreateTransparentMaterial("Bus Yard Faded Lane Mark", new Color(0.92f, 0.97f, 1.00f, 0.34f));
            var guideMaterial = PuzzlePalette.CreateTransparentMaterial("Bus Yard Guide Dot", new Color(0.84f, 0.93f, 0.97f, 0.24f));
            var scuffMaterial = PuzzlePalette.CreateTransparentMaterial("Bus Yard Tire Scuff", new Color(0.26f, 0.35f, 0.40f, 0.14f));
            var safetyMaterial = PuzzlePalette.CreateTransparentMaterial("Bus Yard Safety Stripe", new Color(0.96f, 0.72f, 0.15f, 0.50f));
            var gridWidth = columns * cellSize;
            var gridDepth = rows * cellSize;
            var centerZ = gridBottomZ + (rows - 1) * cellSize * 0.5f;
            var center = new Vector3(0f, SurfaceY, centerZ);

            BoardGeometry.CreateFlatRoundedRect(
                "Bus Yard Surface",
                parent,
                center,
                new Vector2(gridWidth + cellSize * 0.14f, gridDepth + cellSize * 0.14f),
                cellSize * 0.10f,
                surfaceMaterial);

            CreateConcretePanels(parent, columns, rows, cellSize, gridBottomZ, panelMaterialA, panelMaterialB);
            CreateFadedLaneDividers(parent, columns, rows, cellSize, gridBottomZ, laneMaterial);
            CreateGuideDots(parent, columns, rows, cellSize, gridBottomZ, guideMaterial);
            CreateTireScuffs(parent, cellSize, gridBottomZ, scuffMaterial);
            CreateSafetyStripes(parent, columns, rows, cellSize, gridBottomZ, safetyMaterial);
        }

        private static void CreateConcretePanels(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            Material first,
            Material second)
        {
            var panelCount = 4;
            var gridWidth = columns * cellSize;
            var panelDepth = rows * cellSize / panelCount;
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
            CreateDashedHorizontalLine(parent, "Bus Yard Lane Top", gridBottomZ + cellSize * 9.5f, gridWidth, cellSize, material);
            CreateDashedHorizontalLine(parent, "Bus Yard Lane Middle", gridBottomZ + cellSize * 5.5f, gridWidth, cellSize, material);
            CreateDashedHorizontalLine(parent, "Bus Yard Lane Bottom", gridBottomZ + cellSize * 1.5f, gridWidth, cellSize, material);
            CreateDashedVerticalLine(parent, "Bus Yard Service Aisle", 0f, gridBottomZ, rows, cellSize, material);
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

        private static void CreateDashedVerticalLine(
            Transform parent,
            string name,
            float x,
            float gridBottomZ,
            int rows,
            float cellSize,
            Material material)
        {
            const int dashCount = 5;
            for (var index = 0; index < dashCount; index++)
            {
                var z = gridBottomZ + Mathf.Lerp(cellSize * 1.1f, (rows - 2.1f) * cellSize, index / (dashCount - 1f));
                BoardGeometry.CreateFlatRect(
                    $"{name} Dash {index + 1}",
                    parent,
                    new Vector3(x, MarkingY + 0.001f, z),
                    new Vector2(cellSize * 0.035f, cellSize * 0.58f),
                    material);
            }
        }

        private static void CreateGuideDots(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            Material material)
        {
            for (var y = 1; y < rows; y += 2)
            {
                for (var x = 1; x < columns; x += 2)
                {
                    BoardGeometry.CreateFlatRoundedRect(
                        $"Bus Yard Guide Dot {x},{y}",
                        parent,
                        GridToWorld(x, y, columns, cellSize, gridBottomZ) + Vector3.up * GuideY,
                        new Vector2(cellSize * 0.075f, cellSize * 0.075f),
                        cellSize * 0.035f,
                        material);
                }
            }
        }

        private static void CreateTireScuffs(Transform parent, float cellSize, float gridBottomZ, Material material)
        {
            CreateTireScuff(parent, "Bus Yard Scuff 1", new Vector2(-1.42f, gridBottomZ + cellSize * 3.1f), 0.56f, 16f, material);
            CreateTireScuff(parent, "Bus Yard Scuff 2", new Vector2(1.12f, gridBottomZ + cellSize * 4.8f), 0.48f, -11f, material);
            CreateTireScuff(parent, "Bus Yard Scuff 3", new Vector2(-0.18f, gridBottomZ + cellSize * 7.2f), 0.68f, 4f, material);
            CreateTireScuff(parent, "Bus Yard Scuff 4", new Vector2(1.54f, gridBottomZ + cellSize * 9.7f), 0.44f, 19f, material);
        }

        private static void CreateTireScuff(
            Transform parent,
            string name,
            Vector2 position,
            float length,
            float yawDegrees,
            Material material)
        {
            BoardGeometry.CreateFlatRoundedRect(
                name,
                parent,
                new Vector3(position.x, GuideY - 0.001f, position.y),
                new Vector2(0.030f, length),
                0.014f,
                material,
                Quaternion.Euler(0f, yawDegrees, 0f));
        }

        private static void CreateSafetyStripes(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            Material material)
        {
            var leftX = -(columns - 1) * cellSize * 0.5f + cellSize * 0.42f;
            var rightX = (columns - 1) * cellSize * 0.5f - cellSize * 0.42f;
            var lowerZ = gridBottomZ + cellSize * 0.45f;
            var upperZ = gridBottomZ + (rows - 1.45f) * cellSize;

            for (var index = 0; index < 4; index++)
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

        private static Vector3 GridToWorld(int x, int y, int columns, float cellSize, float gridBottomZ)
        {
            var worldX = (x - (columns - 1) * 0.5f) * cellSize;
            var worldZ = gridBottomZ + y * cellSize;
            return new Vector3(worldX, 0f, worldZ);
        }
    }
}
