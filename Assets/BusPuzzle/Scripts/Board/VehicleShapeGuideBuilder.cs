using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleShapeGuideBuilder
    {
        private const int DefaultProbeCount = 24;
        private const int MaxProbeCount = 96;
        private const float GuideY = 0.52f;
        private const float FillMarkerScale = 0.54f;
        private const float OutlineMarkerScale = 0.82f;
        private const float AccentMarkerScale = 0.68f;

        public static void Create(LevelData levelData, Transform parent)
        {
            if (!TryCreateGuide(levelData, out var libraryIndex, out var cells))
            {
                return;
            }

            var fillMaterial = CreateGuideMaterial("Shape Guide Fill", new Color(0.08f, 0.75f, 1.00f, 0.18f), 20);
            var outlineMaterial = CreateGuideMaterial("Shape Guide Outline", new Color(1.00f, 1.00f, 0.92f, 0.42f), 22);
            var accentMaterial = CreateGuideMaterial("Shape Guide Accent", new Color(1.00f, 0.78f, 0.16f, 0.50f), 24);
            if (fillMaterial == null || outlineMaterial == null || accentMaterial == null)
            {
                return;
            }

            var root = new GameObject($"Shape Guide - {VehicleShapeLayoutEngine.GetLibraryDisplayName(libraryIndex)}").transform;
            root.SetParent(parent, false);

            CreateRoleMarkers(root, cells, VehicleShapeCellRole.Fill, fillMaterial, FillMarkerScale, 0.000f);
            CreateRoleMarkers(root, cells, VehicleShapeCellRole.Outline, outlineMaterial, OutlineMarkerScale, 0.004f);
            CreateRoleMarkers(root, cells, VehicleShapeCellRole.Accent, accentMaterial, AccentMarkerScale, 0.008f);
        }

        private static bool TryCreateGuide(
            LevelData levelData,
            out int libraryIndex,
            out List<VehicleShapeCell> cells)
        {
            libraryIndex = -1;
            cells = null;
            if (levelData == null ||
                !StageGenerationSignature.TryGetInt(levelData.GenerationSignature, "layoutVariant", out var layoutVariantIndex) ||
                !VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out libraryIndex))
            {
                return false;
            }

            var profile = levelData.DifficultyProfile;
            var visibleVehicles = levelData.Buses;
            var targetVehicleCount = Mathf.Max(1, visibleVehicles != null ? visibleVehicles.Count : 0);
            var probeCount = GetProbeCount(levelData.GenerationSignature);
            if (!TryFindBestDefinition(
                profile,
                targetVehicleCount,
                layoutVariantIndex,
                probeCount,
                visibleVehicles,
                out var definition))
            {
                return false;
            }

            cells = VehicleShapeLayoutEngine.CreateGuideCells(definition);
            return cells.Count > 0;
        }

        private static bool TryFindBestDefinition(
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int layoutVariantIndex,
            int probeCount,
            IReadOnlyList<BusDefinition> vehicles,
            out VehicleShapeLayoutDefinition definition)
        {
            definition = default;
            var bestScore = int.MaxValue;
            var found = false;
            for (var probeIndex = 0; probeIndex < probeCount; probeIndex++)
            {
                var effectiveLayoutVariantIndex = VehicleLayoutPatternEngine.GetProbeLayoutVariantIndex(
                    profile,
                    layoutVariantIndex,
                    probeIndex);
                if (!VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                    profile,
                    targetVehicleCount,
                    effectiveLayoutVariantIndex,
                    out var candidate))
                {
                    continue;
                }

                var score = VehicleShapeLayoutEngine.ScoreShapeFidelity(candidate, vehicles);
                if (found && score >= bestScore)
                {
                    continue;
                }

                definition = candidate;
                bestScore = score;
                found = true;
            }

            return found;
        }

        private static int GetProbeCount(string generationSignature)
        {
            if (StageGenerationSignature.TryGetInt(generationSignature, "releaseVehicleAttempts", out var attempts))
            {
                return Mathf.Clamp(attempts, 1, MaxProbeCount);
            }

            return DefaultProbeCount;
        }

        private static Material CreateGuideMaterial(string name, Color color, int renderQueueOffset)
        {
            var material = PuzzlePalette.CreateTransparentMaterial(name, color);
            if (material != null)
            {
                material.renderQueue += renderQueueOffset;
            }

            return material;
        }

        private static void CreateRoleMarkers(
            Transform root,
            IReadOnlyList<VehicleShapeCell> cells,
            VehicleShapeCellRole role,
            Material material,
            float markerScale,
            float yOffset)
        {
            var size = Vector2.one * BoardLayoutConfig.CellSize * markerScale;
            var radius = BoardLayoutConfig.CellSize * 0.09f;
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (cell.Role != role)
                {
                    continue;
                }

                var position = BoardLayoutConfig.GridToWorld(cell.Cell);
                position.y = GuideY + yOffset;
                BoardGeometry.CreateFlatRoundedRect(
                    $"Guide {role} {cell.Cell.x:00}-{cell.Cell.y:00}",
                    root,
                    position,
                    size,
                    radius,
                    material);
            }
        }
    }
}
