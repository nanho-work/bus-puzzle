using UnityEngine;

namespace BusPuzzle
{
    internal static class ParkingGridBuilder
    {
        private const float SurfaceY = -0.033f;
        private const float PanelY = -0.029f;

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
            var gridWidth = columns * cellSize;
            var gridDepth = rows * cellSize + topExtensionZ;
            var centerZ = gridBottomZ + (rows - 1) * cellSize * 0.5f + topExtensionZ * 0.5f;
            var center = new Vector3(0f, SurfaceY, centerZ);

            BoardGeometry.CreateFlatRoundedRect(
                "Bus Yard Surface",
                parent,
                center,
                new Vector2(gridWidth + cellSize * 0.14f, gridDepth + cellSize * 0.14f),
                cellSize * 0.10f,
                surfaceMaterial);

            CreatePanelBands(parent, columns, rows, cellSize, gridBottomZ, topExtensionZ, panelMaterialA, panelMaterialB);
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

    }
}
