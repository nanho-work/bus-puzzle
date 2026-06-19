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
            float topExtensionZ = 0f,
            BoardThemeId theme = BoardThemeId.Field)
        {
            topExtensionZ = Mathf.Max(0f, topExtensionZ);
            var style = BoardThemePalette.GetStyle(theme);
            var surfaceMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Yard Surface", style.YardSurface);
            var panelMaterialA = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Yard Slab A", style.YardPanelA);
            var panelMaterialB = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Yard Slab B", style.YardPanelB);
            var laneMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Yard Lane Paint", style.YardLine);
            var routeMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Yard Route", style.YardRoute);
            var checkpointMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Yard Checkpoint", style.YardCheckpoint);
            var trackMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Yard Track", style.YardTrack);
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

            CreatePanelBands(parent, columns, rows, cellSize, gridBottomZ, topExtensionZ, panelMaterialA, panelMaterialB);
            CreateThemeMarkings(parent, columns, rows, cellSize, contentBottomZ, laneMaterial, routeMaterial, checkpointMaterial, surfaceMaterial, style.Name);
            CreatePanelTracks(parent, columns, rows, cellSize, gridBottomZ, topExtensionZ, trackMaterial);
        }

        private static void CreatePanelBands(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            float topExtensionZ,
            Material first,
            Material second)
        {
            var panelCount = 6;
            var gridWidth = columns * cellSize;
            var panelDepth = (rows * cellSize + topExtensionZ) / panelCount;
            for (var index = 0; index < panelCount; index++)
            {
                var centerZ = gridBottomZ - cellSize * 0.5f + panelDepth * (index + 0.5f);
                BoardGeometry.CreateFlatRect(
                    $"Yard Slab {index + 1}",
                    parent,
                    new Vector3(0f, PanelY, centerZ),
                    new Vector2(gridWidth * 0.98f, panelDepth - cellSize * 0.08f),
                    index % 2 == 0 ? first : second,
                    Quaternion.Euler(0f, index % 2 == 0 ? -5f : 6f, 0f));
            }
        }

        private static void CreateThemeMarkings(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ,
            Material whiteLine,
            Material routeLine,
            Material checkpointLine,
            Material surfaceMaterial,
            string themeName)
        {
            var gridWidth = columns * cellSize;
            var gridDepth = rows * cellSize;
            var centerZ = gridBottomZ + cellSize * 6.5f;
            var leftX = -gridWidth * 0.5f + cellSize * 0.15f;
            var rightX = gridWidth * 0.5f - cellSize * 0.15f;
            var bottomZ = gridBottomZ - cellSize * 0.38f;
            var topZ = gridBottomZ + gridDepth - cellSize * 0.62f;

            CreateCircuitStroke(parent, $"{themeName} Outer Yard Route", new Vector3(0f, MarkingY, centerZ), new Vector2(gridWidth * 0.86f, gridDepth * 0.80f), cellSize * 0.065f, routeLine, surfaceMaterial);
            CreateCircuitStroke(parent, $"{themeName} Inner Yard Route", new Vector3(0f, MarkingY + 0.004f, centerZ), new Vector2(gridWidth * 0.62f, gridDepth * 0.50f), cellSize * 0.046f, routeLine, surfaceMaterial);

            BoardGeometry.CreateFlatRect($"{themeName} Bottom Yard Rail", parent, new Vector3(0f, MarkingY + 0.008f, bottomZ), new Vector2(gridWidth * 0.86f, cellSize * 0.030f), whiteLine);
            BoardGeometry.CreateFlatRect($"{themeName} Top Yard Rail", parent, new Vector3(0f, MarkingY + 0.008f, topZ), new Vector2(gridWidth * 0.86f, cellSize * 0.030f), whiteLine);
            BoardGeometry.CreateFlatRect($"{themeName} Left Yard Rail", parent, new Vector3(leftX, MarkingY + 0.008f, centerZ), new Vector2(cellSize * 0.030f, gridDepth * 0.78f), whiteLine);
            BoardGeometry.CreateFlatRect($"{themeName} Right Yard Rail", parent, new Vector3(rightX, MarkingY + 0.008f, centerZ), new Vector2(cellSize * 0.030f, gridDepth * 0.78f), whiteLine);
            BoardGeometry.CreateFlatRect($"{themeName} Yard Start Line", parent, new Vector3(0f, MarkingY + 0.010f, centerZ), new Vector2(gridWidth * 0.64f, cellSize * 0.042f), checkpointLine);
            BoardGeometry.CreateFlatRoundedRect($"{themeName} Yard Pad", parent, new Vector3(0f, MarkingY + 0.012f, centerZ), new Vector2(cellSize * 0.42f, cellSize * 0.22f), cellSize * 0.11f, whiteLine);

            CreateDashedHorizontalLine(parent, $"{themeName} Upper Lane Dashes", gridBottomZ + cellSize * 8.5f, gridWidth, cellSize, whiteLine);
            CreateDashedHorizontalLine(parent, $"{themeName} Lower Lane Dashes", gridBottomZ + cellSize * 4.5f, gridWidth, cellSize, whiteLine);
        }

        private static void CreateCircuitStroke(
            Transform parent,
            string name,
            Vector3 center,
            Vector2 size,
            float thickness,
            Material lineMaterial,
            Material fillMaterial)
        {
            var radius = Mathf.Min(size.x, size.y) * 0.5f;
            BoardGeometry.CreateFlatRoundedRect(name, parent, center, size, radius, lineMaterial);
            BoardGeometry.CreateFlatRoundedRect(
                $"{name} Inner Deck Panel",
                parent,
                center + Vector3.up * 0.002f,
                new Vector2(size.x - thickness * 2f, size.y - thickness * 2f),
                Mathf.Max(0.01f, radius - thickness),
                fillMaterial);
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

        private static void CreatePanelTracks(
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
                CreatePanelTrack(parent, $"Lower Yard Track {index + 1}", leftX + offset, lowerZ, material);
                CreatePanelTrack(parent, $"Upper Yard Track {index + 1}", rightX - offset, upperZ, material);
            }
        }

        private static void CreatePanelTrack(Transform parent, string name, float x, float z, Material material)
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
