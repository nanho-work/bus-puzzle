using UnityEngine;

namespace BusPuzzle
{
    internal static class ParkingGridBuilder
    {
        public static void Create(
            Transform parent,
            int columns,
            int rows,
            float cellSize,
            float gridBottomZ)
        {
            var cellMaterial = PuzzlePalette.CreateSolidMaterial("Parking Cell", new Color(0.68f, 0.83f, 0.89f));

            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    BoardGeometry.CreateFlatRect(
                        $"Cell {x},{y}",
                        parent,
                        GridToWorld(x, y, columns, cellSize, gridBottomZ) + Vector3.down * 0.025f,
                        new Vector2(cellSize * 0.92f, cellSize * 0.92f),
                        cellMaterial);
                }
            }
        }

        private static Vector3 GridToWorld(int x, int y, int columns, float cellSize, float gridBottomZ)
        {
            var worldX = (x - (columns - 1) * 0.5f) * cellSize;
            var worldZ = gridBottomZ + y * cellSize;
            return new Vector3(worldX, 0f, worldZ);
        }
    }
}
