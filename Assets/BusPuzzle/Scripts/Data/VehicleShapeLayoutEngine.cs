using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal enum VehicleShapeLayoutKind
    {
        None,
        Heart,
        Circle,
        Ring,
        Cross,
        X,
        Square,
        Diamond
    }

    internal enum VehicleShapeLibraryId
    {
        None = -1,
        Circle,
        Ring,
        SemiCircle,
        DoubleRing,
        Spiral,
        Heart,
        HeartArrow,
        Star,
        Flower,
        Sunburst,
        Square,
        HollowSquare,
        Diamond,
        Triangle,
        Cross,
        X,
        Arrow,
        DoubleArrow,
        Lightning,
        S,
        Wave,
        Stairs,
        Grid,
        MazeBox,
        Crown,
        Shield,
        Smile,
        Clover,
        Eight,
        Fan
    }

    internal enum VehicleShapeCellRole
    {
        Outline,
        Fill,
        Accent
    }

    internal readonly struct VehicleShapeLayoutDefinition
    {
        public readonly VehicleShapeLayoutKind Kind;
        public readonly VehicleShapeLibraryId LibraryId;
        public readonly int Thickness;
        public readonly bool FillInterior;
        public readonly float Scale;
        public readonly bool Clockwise;
        public readonly int VariantSeed;

        public VehicleShapeLayoutDefinition(
            VehicleShapeLayoutKind kind,
            int thickness,
            bool fillInterior,
            float scale,
            bool clockwise,
            int variantSeed)
            : this(
                kind,
                GetDefaultLibraryId(kind),
                thickness,
                fillInterior,
                scale,
                clockwise,
                variantSeed)
        {
        }

        public VehicleShapeLayoutDefinition(
            VehicleShapeLayoutKind kind,
            VehicleShapeLibraryId libraryId,
            int thickness,
            bool fillInterior,
            float scale,
            bool clockwise,
            int variantSeed)
        {
            Kind = kind;
            LibraryId = libraryId;
            Thickness = Mathf.Clamp(thickness, 1, 3);
            FillInterior = fillInterior;
            Scale = Mathf.Clamp(scale, 0.82f, 1.06f);
            Clockwise = clockwise;
            VariantSeed = variantSeed;
        }

        private static VehicleShapeLibraryId GetDefaultLibraryId(VehicleShapeLayoutKind kind)
        {
            switch (kind)
            {
                case VehicleShapeLayoutKind.Heart:
                    return VehicleShapeLibraryId.Heart;
                case VehicleShapeLayoutKind.Circle:
                    return VehicleShapeLibraryId.Circle;
                case VehicleShapeLayoutKind.Ring:
                    return VehicleShapeLibraryId.Ring;
                case VehicleShapeLayoutKind.Cross:
                    return VehicleShapeLibraryId.Cross;
                case VehicleShapeLayoutKind.X:
                    return VehicleShapeLibraryId.X;
                case VehicleShapeLayoutKind.Square:
                    return VehicleShapeLibraryId.Square;
                case VehicleShapeLayoutKind.Diamond:
                    return VehicleShapeLibraryId.Diamond;
                default:
                    return VehicleShapeLibraryId.None;
            }
        }
    }

    internal readonly struct VehicleShapeCell
    {
        public readonly Vector2Int Cell;
        public readonly VehicleShapeCellRole Role;

        public VehicleShapeCell(Vector2Int cell, VehicleShapeCellRole role)
        {
            Cell = cell;
            Role = role;
        }
    }

    internal static class VehicleShapeLayoutEngine
    {
        public const int ShapeLibraryCount = 30;
        private const int MinCell = 1;
        private const int MaxCellX = BoardLayoutConfig.GridColumns - 2;
        private const int MaxCellY = BoardLayoutConfig.GridRows - 2;
        private const float CenterX = (BoardLayoutConfig.GridColumns - 1) * 0.5f;
        private const float CenterY = (BoardLayoutConfig.GridRows - 1) * 0.5f;
        private const float MaxShapeAngleOffsetDegrees = 24f;
        private const float MaxLibraryShapeAngleOffsetDegrees = 45f;

        public static bool TryCreateDefinition(
            VehicleLayoutPatternId pattern,
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int layoutVariantIndex,
            out VehicleShapeLayoutDefinition definition)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var kind = ToShapeKind(pattern);
            if (kind == VehicleShapeLayoutKind.None)
            {
                definition = default;
                return false;
            }

            var variant = Mathf.Abs(layoutVariantIndex);
            var pressure = Mathf.Clamp01(profile.ParkingTension * 0.65f + profile.StationPressure * 0.35f);
            var thickness = 1 + ((variant / 5) % 3);
            if (pressure >= 0.62f || targetVehicleCount >= 38)
            {
                thickness = Mathf.Max(thickness, 2);
            }

            if (pressure >= 0.78f || targetVehicleCount >= 50)
            {
                thickness = 3;
            }

            var allowInteriorFill = profile.Difficulty == LevelDifficulty.Normal ||
                (profile.Difficulty == LevelDifficulty.Hard && targetVehicleCount >= 52 && pressure < 0.68f);
            var fillInterior = kind != VehicleShapeLayoutKind.Ring &&
                allowInteriorFill &&
                (pressure >= 0.52f || targetVehicleCount >= 34 || variant % 3 == 0);
            var scaleStep = variant % 5;
            var scale = Mathf.Lerp(0.90f, 1.02f, scaleStep / 4f);
            if (kind == VehicleShapeLayoutKind.X || kind == VehicleShapeLayoutKind.Cross)
            {
                scale = Mathf.Lerp(0.96f, 1.04f, scaleStep / 4f);
            }

            definition = new VehicleShapeLayoutDefinition(
                kind,
                GetDefaultLibraryId(kind),
                thickness,
                fillInterior,
                scale,
                variant % 2 == 0,
                variant);
            return true;
        }

        public static bool TryCreateLibraryDefinition(
            int libraryIndex,
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int layoutVariantIndex,
            out VehicleShapeLayoutDefinition definition)
        {
            if (libraryIndex < 0 || libraryIndex >= ShapeLibraryCount)
            {
                definition = default;
                return false;
            }

            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var libraryId = (VehicleShapeLibraryId)libraryIndex;
            var variant = Mathf.Abs(layoutVariantIndex) % 1000;
            var pressure = Mathf.Clamp01(profile.ParkingTension * 0.65f + profile.StationPressure * 0.35f);
            var thickness = 1 + ((variant / 4) % 3);
            if (pressure >= 0.60f || targetVehicleCount >= 36)
            {
                thickness = Mathf.Max(thickness, 2);
            }

            var fillInterior = ShouldFillLibraryInterior(libraryId, profile, targetVehicleCount, variant);
            var scale = Mathf.Lerp(0.92f, 1.05f, (variant % 5) / 4f);
            if (libraryId == VehicleShapeLibraryId.Heart || libraryId == VehicleShapeLibraryId.HeartArrow)
            {
                var countScale = Mathf.Lerp(0.88f, 0.96f, Mathf.InverseLerp(30f, 44f, targetVehicleCount));
                scale = Mathf.Min(scale, countScale);
            }

            definition = new VehicleShapeLayoutDefinition(
                ToShapeKind(libraryId),
                libraryId,
                thickness,
                fillInterior,
                scale,
                variant % 2 == 0,
                variant);
            return true;
        }

        public static string GetLibraryDisplayName(int libraryIndex)
        {
            if (libraryIndex < 0 || libraryIndex >= ShapeLibraryCount)
            {
                return "None";
            }

            switch ((VehicleShapeLibraryId)libraryIndex)
            {
                case VehicleShapeLibraryId.Circle:
                    return "Circle";
                case VehicleShapeLibraryId.Ring:
                    return "Ring";
                case VehicleShapeLibraryId.SemiCircle:
                    return "Semi Circle";
                case VehicleShapeLibraryId.DoubleRing:
                    return "Double Ring";
                case VehicleShapeLibraryId.Spiral:
                    return "Spiral";
                case VehicleShapeLibraryId.Heart:
                    return "Heart";
                case VehicleShapeLibraryId.HeartArrow:
                    return "Heart Arrow";
                case VehicleShapeLibraryId.Star:
                    return "Star";
                case VehicleShapeLibraryId.Flower:
                    return "Flower";
                case VehicleShapeLibraryId.Sunburst:
                    return "Sunburst";
                case VehicleShapeLibraryId.Square:
                    return "Square";
                case VehicleShapeLibraryId.HollowSquare:
                    return "Hollow Square";
                case VehicleShapeLibraryId.Diamond:
                    return "Diamond";
                case VehicleShapeLibraryId.Triangle:
                    return "Triangle";
                case VehicleShapeLibraryId.Cross:
                    return "Cross";
                case VehicleShapeLibraryId.X:
                    return "X";
                case VehicleShapeLibraryId.Arrow:
                    return "Arrow";
                case VehicleShapeLibraryId.DoubleArrow:
                    return "Double Arrow";
                case VehicleShapeLibraryId.Lightning:
                    return "Lightning";
                case VehicleShapeLibraryId.S:
                    return "S Curve";
                case VehicleShapeLibraryId.Wave:
                    return "Wave";
                case VehicleShapeLibraryId.Stairs:
                    return "Stairs";
                case VehicleShapeLibraryId.Grid:
                    return "Grid";
                case VehicleShapeLibraryId.MazeBox:
                    return "Maze Box";
                case VehicleShapeLibraryId.Crown:
                    return "Crown";
                case VehicleShapeLibraryId.Shield:
                    return "Shield";
                case VehicleShapeLibraryId.Smile:
                    return "Smile";
                case VehicleShapeLibraryId.Clover:
                    return "Clover";
                case VehicleShapeLibraryId.Eight:
                    return "Eight";
                case VehicleShapeLibraryId.Fan:
                    return "Fan";
                default:
                    return "None";
            }
        }

        public static void AddShapeSlots(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            VehicleShapeLayoutDefinition definition)
        {
            AddShapeSlots(slots, occupiedCells, profile, random, definition, 0);
        }

        public static void AddShapeSlots(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            VehicleShapeLayoutDefinition definition,
            int targetVehicleCount)
        {
            var cells = BuildVehicleBudgetedCells(definition, targetVehicleCount);
            for (var index = 0; index < cells.Count; index++)
            {
                AddShapeSlot(slots, occupiedCells, random, definition, cells[index]);
            }
        }

        public static int ScoreShapeFidelity(
            VehicleShapeLayoutDefinition definition,
            IReadOnlyList<BusDefinition> vehicles)
        {
            if (definition.Kind == VehicleShapeLayoutKind.None || vehicles == null || vehicles.Count == 0)
            {
                return 0;
            }

            var cells = BuildOrderedCells(definition);
            if (cells.Count == 0)
            {
                return 0;
            }

            var roleByCell = new Dictionary<int, VehicleShapeCellRole>();
            var outlineCells = 0;
            for (var index = 0; index < cells.Count; index++)
            {
                var key = GetCellKey(cells[index].Cell);
                if (!roleByCell.ContainsKey(key))
                {
                    roleByCell.Add(key, cells[index].Role);
                    if (cells[index].Role == VehicleShapeCellRole.Outline)
                    {
                        outlineCells++;
                    }
                }
            }

            var featureCells = CollectFeatureCells(definition, cells);
            var occupiedShapeCells = new HashSet<int>();
            var occupiedOutlineCells = new HashSet<int>();
            var occupiedFeatureCells = new HashSet<int>();
            var score = 0;
            for (var index = 0; index < vehicles.Count; index++)
            {
                var vehicle = vehicles[index];
                var position = new Vector2(
                    vehicle.GridPosition.x + vehicle.PositionOffsetCells.x,
                    vehicle.GridPosition.y + vehicle.PositionOffsetCells.y);
                if (TryFindNearestShapeCell(cells, position, out var nearestCell, out var distanceCells) &&
                    distanceCells <= 0.72f)
                {
                    var key = GetCellKey(nearestCell.Cell);
                    var role = nearestCell.Role;
                    occupiedShapeCells.Add(key);
                    if (role == VehicleShapeCellRole.Outline)
                    {
                        occupiedOutlineCells.Add(key);
                    }

                    if (IsFeatureCell(definition, nearestCell))
                    {
                        occupiedFeatureCells.Add(key);
                    }

                    if (role != VehicleShapeCellRole.Fill &&
                        IsFeatureCell(definition, nearestCell) &&
                        vehicle.Color != GetRoleColor(definition.Kind, role))
                    {
                        score += 8;
                    }

                    score += Mathf.RoundToInt(distanceCells * 9f);
                    score += ScoreShapeDirectionMismatch(definition, nearestCell, vehicle);
                    continue;
                }

                var distance = GetNearestShapeDistanceCells(position, cells);
                if (distance <= 0.85f)
                {
                    score += 10;
                }
                else if (distance <= 1.70f)
                {
                    score += 24;
                }
                else
                {
                    score += 55;
                }
            }

            var expectedCoverage = Mathf.Min(vehicles.Count, roleByCell.Count);
            score += Mathf.Max(0, expectedCoverage - occupiedShapeCells.Count) * 14;

            if (outlineCells > 0)
            {
                var expectedOutlineCoverage = Mathf.Min(
                    outlineCells,
                    Mathf.CeilToInt(vehicles.Count * 0.58f));
                score += Mathf.Max(0, expectedOutlineCoverage - occupiedOutlineCells.Count) * 18;
            }

            if (featureCells.Count > 0)
            {
                var expectedFeatureCoverage = Mathf.Min(
                    featureCells.Count,
                    Mathf.Max(3, Mathf.CeilToInt(vehicles.Count * 0.18f)));
                score += Mathf.Max(0, expectedFeatureCoverage - occupiedFeatureCells.Count) * 34;
            }

            return score;
        }

        private static int ScoreShapeDirectionMismatch(
            VehicleShapeLayoutDefinition definition,
            VehicleShapeCell cell,
            BusDefinition vehicle)
        {
            if (definition.LibraryId == VehicleShapeLibraryId.None ||
                cell.Role == VehicleShapeCellRole.Fill)
            {
                return 0;
            }

            GetShapeDirection(definition, cell, out var expectedDirection, out var expectedAngleOffset);
            var expectedYaw = GridDirectionUtility.ToYawDegrees(expectedDirection) + expectedAngleOffset;
            var delta = Mathf.Abs(Mathf.DeltaAngle(expectedYaw, vehicle.YawDegrees));
            var axisDelta = Mathf.Min(delta, Mathf.Abs(180f - delta));
            if (axisDelta <= 10f)
            {
                return 0;
            }

            var weight = IsFeatureCell(definition, cell) ? 0.65f : 0.45f;
            return Mathf.RoundToInt(Mathf.Min(42f, axisDelta - 10f) * weight);
        }

        public static List<VehicleShapeCell> CreateGuideCells(VehicleShapeLayoutDefinition definition)
        {
            return BuildOrderedCells(definition);
        }

        public static bool TryFindNearestShapeCell(
            VehicleShapeLayoutDefinition definition,
            Vector2 position,
            out VehicleShapeCell nearestCell,
            out float distanceCells)
        {
            var cells = BuildOrderedCells(definition);
            return TryFindNearestShapeCell(cells, position, out nearestCell, out distanceCells);
        }

        private static bool TryFindNearestShapeCell(
            IReadOnlyList<VehicleShapeCell> cells,
            Vector2 position,
            out VehicleShapeCell nearestCell,
            out float distanceCells)
        {
            nearestCell = default;
            distanceCells = float.MaxValue;
            if (cells == null || cells.Count == 0)
            {
                return false;
            }

            var bestDistanceSquared = float.MaxValue;
            for (var index = 0; index < cells.Count; index++)
            {
                var cellCenter = new Vector2(cells[index].Cell.x, cells[index].Cell.y);
                var distanceSquared = (cellCenter - position).sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                nearestCell = cells[index];
            }

            distanceCells = Mathf.Sqrt(bestDistanceSquared);
            return true;
        }

        private static List<VehicleShapeCell> BuildVehicleBudgetedCells(
            VehicleShapeLayoutDefinition definition,
            int targetVehicleCount)
        {
            var cells = BuildOrderedCells(definition);
            if (definition.LibraryId == VehicleShapeLibraryId.None ||
                targetVehicleCount <= 0 ||
                targetVehicleCount >= cells.Count)
            {
                return cells;
            }

            if (ShouldUseContinuousLibraryOrder(definition.LibraryId))
            {
                return BuildContinuousBudgetedCells(definition, cells, targetVehicleCount);
            }

            if (definition.LibraryId == VehicleShapeLibraryId.Star)
            {
                return BuildStarBudgetedCells(definition, cells, targetVehicleCount);
            }

            return BuildMixedRoleBudgetedCells(definition, cells, targetVehicleCount);
        }

        private static List<VehicleShapeCell> BuildStarBudgetedCells(
            VehicleShapeLayoutDefinition definition,
            List<VehicleShapeCell> cells,
            int targetVehicleCount)
        {
            var feature = CollectFeatureCells(definition, cells);
            var featureKeys = BuildCellKeySet(feature);
            var outline = CollectRoleCells(cells, VehicleShapeCellRole.Outline, featureKeys);
            var accent = CollectRoleCells(cells, VehicleShapeCellRole.Accent, featureKeys);
            var fill = CollectRoleCells(cells, VehicleShapeCellRole.Fill, featureKeys);
            SortStarPathCells(definition, outline);
            SortStarPathCells(definition, accent);
            SortStarFillCells(fill);

            var selectedFeature = TakeFirst(
                feature,
                Mathf.Min(feature.Count, Mathf.Max(10, Mathf.RoundToInt(targetVehicleCount * 0.22f))));
            var remaining = Mathf.Max(0, targetVehicleCount - selectedFeature.Count);
            var selectedOutline = outline.Count <= remaining
                ? TakeFirst(outline, outline.Count)
                : SelectEvenly(outline, remaining);
            remaining = Mathf.Max(0, remaining - selectedOutline.Count);
            var selectedAccent = TakeFirst(accent, Mathf.Min(accent.Count, remaining));
            remaining = Mathf.Max(0, remaining - selectedAccent.Count);
            var selectedFill = TakeFirst(fill, remaining);

            var ordered = new List<VehicleShapeCell>(cells.Count);
            ordered.AddRange(selectedFeature);
            ordered.AddRange(selectedOutline);
            ordered.AddRange(selectedAccent);
            ordered.AddRange(selectedFill);
            AppendRemainingCells(ordered, cells);
            return ordered;
        }

        private static List<VehicleShapeCell> BuildMixedRoleBudgetedCells(
            VehicleShapeLayoutDefinition definition,
            List<VehicleShapeCell> cells,
            int targetVehicleCount)
        {
            var feature = CollectFeatureCells(definition, cells);
            var featureKeys = BuildCellKeySet(feature);
            var outline = CollectRoleCells(cells, VehicleShapeCellRole.Outline, featureKeys);
            var accent = CollectRoleCells(cells, VehicleShapeCellRole.Accent, featureKeys);
            var fill = CollectRoleCells(cells, VehicleShapeCellRole.Fill, featureKeys);
            if (!definition.FillInterior || fill.Count == 0)
            {
                return cells;
            }

            var featureQuota = Mathf.Min(
                feature.Count,
                Mathf.Min(
                    targetVehicleCount,
                    Mathf.Max(4, Mathf.RoundToInt(targetVehicleCount * GetFeatureBudgetShare(definition.LibraryId)))));
            var remainingTarget = Mathf.Max(0, targetVehicleCount - featureQuota);
            var outlineQuota = Mathf.Min(
                outline.Count,
                Mathf.Max(8, Mathf.RoundToInt(remainingTarget * GetOutlineBudgetShare(definition.LibraryId))));
            var accentQuota = Mathf.Min(accent.Count, Mathf.RoundToInt(remainingTarget * 0.10f));
            var fillQuota = Mathf.Max(0, remainingTarget - outlineQuota - accentQuota);
            if (fillQuota > fill.Count)
            {
                var overflow = fillQuota - fill.Count;
                fillQuota = fill.Count;
                outlineQuota = Mathf.Min(outline.Count, outlineQuota + overflow);
            }

            while (featureQuota + outlineQuota + accentQuota + fillQuota > targetVehicleCount && outlineQuota > 0)
            {
                outlineQuota--;
            }

            while (featureQuota + outlineQuota + accentQuota + fillQuota > targetVehicleCount && accentQuota > 0)
            {
                accentQuota--;
            }

            var selectedOutline = TakeFirst(outline, outlineQuota);
            var selectedAccent = TakeFirst(accent, accentQuota);
            var selectedFill = TakeFirst(fill, fillQuota);
            var selectedFeature = TakeFirst(feature, featureQuota);
            var ordered = new List<VehicleShapeCell>(cells.Count);
            if (ShouldPrioritizeSilhouetteOutline(definition.LibraryId))
            {
                AppendSilhouetteFirstCells(ordered, selectedFeature, selectedOutline, selectedAccent, selectedFill);
            }
            else
            {
                AppendMixedCells(ordered, selectedFeature, selectedOutline, selectedAccent, selectedFill);
            }

            AppendRemainingCells(ordered, cells);
            return ordered;
        }

        private static List<VehicleShapeCell> BuildContinuousBudgetedCells(
            VehicleShapeLayoutDefinition definition,
            List<VehicleShapeCell> cells,
            int targetVehicleCount)
        {
            var pathCells = new List<VehicleShapeCell>(cells);
            pathCells.Sort((left, right) =>
                GetLibraryPathProgress(definition.LibraryId, left.Cell)
                    .CompareTo(GetLibraryPathProgress(definition.LibraryId, right.Cell)));

            var selected = SelectEvenly(pathCells, targetVehicleCount);
            var ordered = new List<VehicleShapeCell>(cells.Count);
            ordered.AddRange(selected);
            AppendRemainingCells(ordered, pathCells);
            return ordered;
        }

        private static bool ShouldUseContinuousLibraryOrder(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static float GetOutlineBudgetShare(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                    return 0.78f;
                case VehicleShapeLibraryId.Star:
                    return 0.76f;
                case VehicleShapeLibraryId.Shield:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                    return 0.70f;
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Triangle:
                    return 0.64f;
                default:
                    return 0.56f;
            }
        }

        private static float GetFeatureBudgetShare(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                    return 0.14f;
                case VehicleShapeLibraryId.Star:
                    return 0.24f;
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                    return 0.18f;
                case VehicleShapeLibraryId.Crown:
                case VehicleShapeLibraryId.Shield:
                case VehicleShapeLibraryId.Clover:
                    return 0.18f;
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                    return 0.22f;
                default:
                    return 0.20f;
            }
        }

        private static bool ShouldPrioritizeSilhouetteOutline(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                case VehicleShapeLibraryId.Star:
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Shield:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static List<VehicleShapeCell> CollectRoleCells(
            List<VehicleShapeCell> cells,
            VehicleShapeCellRole role)
        {
            return CollectRoleCells(cells, role, null);
        }

        private static List<VehicleShapeCell> CollectRoleCells(
            List<VehicleShapeCell> cells,
            VehicleShapeCellRole role,
            HashSet<int> excludedKeys)
        {
            var roleCells = new List<VehicleShapeCell>();
            for (var index = 0; index < cells.Count; index++)
            {
                if (cells[index].Role == role &&
                    (excludedKeys == null || !excludedKeys.Contains(GetCellKey(cells[index].Cell))))
                {
                    roleCells.Add(cells[index]);
                }
            }

            return roleCells;
        }

        private static List<VehicleShapeCell> CollectFeatureCells(
            VehicleShapeLayoutDefinition definition,
            List<VehicleShapeCell> cells)
        {
            var featureCells = new List<VehicleShapeCell>();
            var used = new HashSet<int>();
            for (var index = 0; index < cells.Count; index++)
            {
                if (!IsFeatureCell(definition, cells[index]))
                {
                    continue;
                }

                if (used.Add(GetCellKey(cells[index].Cell)))
                {
                    featureCells.Add(cells[index]);
                }
            }

            featureCells.Sort((left, right) =>
            {
                var priorityCompare = GetFeaturePriority(definition, left).CompareTo(GetFeaturePriority(definition, right));
                if (priorityCompare != 0)
                {
                    return priorityCompare;
                }

                return GetCellAngle(left.Cell).CompareTo(GetCellAngle(right.Cell));
            });
            return featureCells;
        }

        private static HashSet<int> BuildCellKeySet(List<VehicleShapeCell> cells)
        {
            var keys = new HashSet<int>();
            for (var index = 0; index < cells.Count; index++)
            {
                keys.Add(GetCellKey(cells[index].Cell));
            }

            return keys;
        }

        private static List<VehicleShapeCell> TakeFirst(List<VehicleShapeCell> source, int count)
        {
            count = Mathf.Clamp(count, 0, source.Count);
            var result = new List<VehicleShapeCell>(count);
            for (var index = 0; index < count; index++)
            {
                result.Add(source[index]);
            }

            return result;
        }

        private static List<VehicleShapeCell> SelectEvenly(List<VehicleShapeCell> source, int count)
        {
            count = Mathf.Clamp(count, 0, source.Count);
            var result = new List<VehicleShapeCell>(count);
            if (count == 0)
            {
                return result;
            }

            if (count == 1)
            {
                result.Add(source[0]);
                return result;
            }

            for (var index = 0; index < count; index++)
            {
                var sourceIndex = Mathf.RoundToInt(index * (source.Count - 1f) / (count - 1f));
                result.Add(source[Mathf.Clamp(sourceIndex, 0, source.Count - 1)]);
            }

            return result;
        }

        private static void AppendMixedCells(
            List<VehicleShapeCell> ordered,
            List<VehicleShapeCell> feature,
            List<VehicleShapeCell> outline,
            List<VehicleShapeCell> accent,
            List<VehicleShapeCell> fill)
        {
            for (var index = 0; index < feature.Count; index++)
            {
                ordered.Add(feature[index]);
            }

            var outlineIndex = 0;
            var accentIndex = 0;
            var fillIndex = 0;
            while (outlineIndex < outline.Count || accentIndex < accent.Count || fillIndex < fill.Count)
            {
                AppendIfAvailable(ordered, outline, ref outlineIndex);
                AppendIfAvailable(ordered, fill, ref fillIndex);
                AppendIfAvailable(ordered, outline, ref outlineIndex);
                AppendIfAvailable(ordered, accent, ref accentIndex);
                AppendIfAvailable(ordered, fill, ref fillIndex);
            }
        }

        private static void AppendSilhouetteFirstCells(
            List<VehicleShapeCell> ordered,
            List<VehicleShapeCell> feature,
            List<VehicleShapeCell> outline,
            List<VehicleShapeCell> accent,
            List<VehicleShapeCell> fill)
        {
            var outlineIndex = 0;
            while (outlineIndex < outline.Count)
            {
                AppendIfAvailable(ordered, outline, ref outlineIndex);
            }

            var featureIndex = 0;
            var accentIndex = 0;
            var fillIndex = 0;
            while (featureIndex < feature.Count || accentIndex < accent.Count || fillIndex < fill.Count)
            {
                AppendIfAvailable(ordered, feature, ref featureIndex);
                AppendIfAvailable(ordered, fill, ref fillIndex);
                AppendIfAvailable(ordered, accent, ref accentIndex);
                AppendIfAvailable(ordered, fill, ref fillIndex);
            }
        }

        private static void AppendIfAvailable(
            List<VehicleShapeCell> ordered,
            List<VehicleShapeCell> source,
            ref int index)
        {
            if (index >= source.Count)
            {
                return;
            }

            ordered.Add(source[index]);
            index++;
        }

        private static void AppendRemainingCells(
            List<VehicleShapeCell> ordered,
            List<VehicleShapeCell> source)
        {
            var used = new HashSet<int>();
            for (var index = 0; index < ordered.Count; index++)
            {
                used.Add(GetCellKey(ordered[index].Cell));
            }

            for (var index = 0; index < source.Count; index++)
            {
                if (used.Add(GetCellKey(source[index].Cell)))
                {
                    ordered.Add(source[index]);
                }
            }
        }

        private static void SortStarPathCells(
            VehicleShapeLayoutDefinition definition,
            List<VehicleShapeCell> cells)
        {
            cells.Sort((left, right) =>
            {
                var progressCompare = GetStarPathProgress(definition, left.Cell)
                    .CompareTo(GetStarPathProgress(definition, right.Cell));
                if (progressCompare != 0)
                {
                    return progressCompare;
                }

                return GetCenterDistanceSquared(right.Cell).CompareTo(GetCenterDistanceSquared(left.Cell));
            });
        }

        private static void SortStarFillCells(List<VehicleShapeCell> cells)
        {
            cells.Sort((left, right) =>
                GetCenterDistanceSquared(right.Cell).CompareTo(GetCenterDistanceSquared(left.Cell)));
        }

        private static VehicleShapeLayoutKind ToShapeKind(VehicleLayoutPatternId pattern)
        {
            switch (pattern)
            {
                case VehicleLayoutPatternId.ShapeHeart:
                case VehicleLayoutPatternId.ShowcaseHeart:
                    return VehicleShapeLayoutKind.Heart;
                case VehicleLayoutPatternId.ShapeCircle:
                    return VehicleShapeLayoutKind.Circle;
                case VehicleLayoutPatternId.ShapeRing:
                case VehicleLayoutPatternId.ShowcaseRing:
                    return VehicleShapeLayoutKind.Ring;
                case VehicleLayoutPatternId.ShapeCross:
                case VehicleLayoutPatternId.ShowcaseQuads:
                    return VehicleShapeLayoutKind.Cross;
                case VehicleLayoutPatternId.ShapeX:
                    return VehicleShapeLayoutKind.X;
                case VehicleLayoutPatternId.ShapeSquare:
                case VehicleLayoutPatternId.ShowcaseFrame:
                    return VehicleShapeLayoutKind.Square;
                case VehicleLayoutPatternId.ShapeDiamond:
                case VehicleLayoutPatternId.DiamondCross:
                    return VehicleShapeLayoutKind.Diamond;
                default:
                    return VehicleShapeLayoutKind.None;
            }
        }

        private static VehicleShapeLayoutKind ToShapeKind(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                    return VehicleShapeLayoutKind.Heart;
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Star:
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Smile:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return VehicleShapeLayoutKind.Circle;
                case VehicleShapeLibraryId.Ring:
                    return VehicleShapeLayoutKind.Ring;
                case VehicleShapeLibraryId.Cross:
                    return VehicleShapeLayoutKind.Cross;
                case VehicleShapeLibraryId.X:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                    return VehicleShapeLayoutKind.X;
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                    return VehicleShapeLayoutKind.Square;
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Triangle:
                case VehicleShapeLibraryId.Shield:
                    return VehicleShapeLayoutKind.Diamond;
                default:
                    return VehicleShapeLayoutKind.None;
            }
        }

        private static VehicleShapeLibraryId GetDefaultLibraryId(VehicleShapeLayoutKind kind)
        {
            switch (kind)
            {
                case VehicleShapeLayoutKind.Heart:
                    return VehicleShapeLibraryId.Heart;
                case VehicleShapeLayoutKind.Circle:
                    return VehicleShapeLibraryId.Circle;
                case VehicleShapeLayoutKind.Ring:
                    return VehicleShapeLibraryId.Ring;
                case VehicleShapeLayoutKind.Cross:
                    return VehicleShapeLibraryId.Cross;
                case VehicleShapeLayoutKind.X:
                    return VehicleShapeLibraryId.X;
                case VehicleShapeLayoutKind.Square:
                    return VehicleShapeLibraryId.Square;
                case VehicleShapeLayoutKind.Diamond:
                    return VehicleShapeLibraryId.Diamond;
                default:
                    return VehicleShapeLibraryId.None;
            }
        }

        private static bool ShouldFillLibraryInterior(
            VehicleShapeLibraryId libraryId,
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int variant)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                case VehicleShapeLibraryId.Smile:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return false;
                default:
                    var pressure = profile != null
                        ? Mathf.Clamp01(profile.ParkingTension * 0.65f + profile.StationPressure * 0.35f)
                        : 0.45f;
                    return profile == null ||
                        profile.Difficulty == LevelDifficulty.Normal ||
                        targetVehicleCount >= 38 ||
                        pressure >= 0.58f ||
                        variant % 3 == 0;
            }
        }

        private static List<VehicleShapeCell> BuildOrderedCells(VehicleShapeLayoutDefinition definition)
        {
            var cells = new List<VehicleShapeCell>();
            for (var y = MinCell; y <= MaxCellY; y++)
            {
                for (var x = MinCell; x <= MaxCellX; x++)
                {
                    if (TryClassifyCell(definition, x, y, out var role))
                    {
                        cells.Add(new VehicleShapeCell(new Vector2Int(x, y), role));
                    }
                }
            }

            return OrderCells(definition, cells);
        }

        private static bool TryClassifyCell(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            if (definition.LibraryId != VehicleShapeLibraryId.None &&
                TryClassifyLibraryCell(definition, x, y, out role))
            {
                return true;
            }

            if (definition.LibraryId != VehicleShapeLibraryId.None)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            switch (definition.Kind)
            {
                case VehicleShapeLayoutKind.Heart:
                    return TryClassifyHeart(definition, x, y, out role);
                case VehicleShapeLayoutKind.Circle:
                    return TryClassifyCircle(definition, x, y, false, out role);
                case VehicleShapeLayoutKind.Ring:
                    return TryClassifyCircle(definition, x, y, true, out role);
                case VehicleShapeLayoutKind.Cross:
                    return TryClassifyCross(definition, x, y, out role);
                case VehicleShapeLayoutKind.X:
                    return TryClassifyX(definition, x, y, out role);
                case VehicleShapeLayoutKind.Square:
                    return TryClassifySquare(definition, x, y, out role);
                case VehicleShapeLayoutKind.Diamond:
                    return TryClassifyDiamond(definition, x, y, out role);
                default:
                    role = VehicleShapeCellRole.Fill;
                    return false;
            }
        }

        private static bool TryClassifyLibraryCell(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            switch (definition.LibraryId)
            {
                case VehicleShapeLibraryId.Circle:
                    return TryClassifyCircle(definition, x, y, false, out role);
                case VehicleShapeLibraryId.Ring:
                    return TryClassifyCircle(definition, x, y, true, out role);
                case VehicleShapeLibraryId.SemiCircle:
                    return TryClassifySemiCircle(definition, x, y, out role);
                case VehicleShapeLibraryId.DoubleRing:
                    return TryClassifyDoubleRing(definition, x, y, out role);
                case VehicleShapeLibraryId.Spiral:
                    return TryClassifySpiral(definition, x, y, out role);
                case VehicleShapeLibraryId.Heart:
                    return TryClassifyHeart(definition, x, y, out role);
                case VehicleShapeLibraryId.HeartArrow:
                    return TryClassifyHeartArrow(definition, x, y, out role);
                case VehicleShapeLibraryId.Star:
                    return TryClassifyStar(definition, x, y, out role);
                case VehicleShapeLibraryId.Flower:
                    return TryClassifyRadialPetal(definition, x, y, 6, 3.30f, 1.15f, out role);
                case VehicleShapeLibraryId.Sunburst:
                    return TryClassifySunburst(definition, x, y, out role);
                case VehicleShapeLibraryId.Square:
                    return TryClassifySquare(definition, x, y, out role);
                case VehicleShapeLibraryId.HollowSquare:
                    return TryClassifyHollowSquare(definition, x, y, out role);
                case VehicleShapeLibraryId.Diamond:
                    return TryClassifyDiamond(definition, x, y, out role);
                case VehicleShapeLibraryId.Triangle:
                    return TryClassifyTriangle(definition, x, y, out role);
                case VehicleShapeLibraryId.Cross:
                    return TryClassifyCross(definition, x, y, out role);
                case VehicleShapeLibraryId.X:
                    return TryClassifyX(definition, x, y, out role);
                case VehicleShapeLibraryId.Arrow:
                    return TryClassifyArrow(definition, x, y, false, out role);
                case VehicleShapeLibraryId.DoubleArrow:
                    return TryClassifyArrow(definition, x, y, true, out role);
                case VehicleShapeLibraryId.Lightning:
                    return TryClassifyLightning(definition, x, y, out role);
                case VehicleShapeLibraryId.S:
                    return TryClassifySCurve(definition, x, y, out role);
                case VehicleShapeLibraryId.Wave:
                    return TryClassifyWave(definition, x, y, out role);
                case VehicleShapeLibraryId.Stairs:
                    return TryClassifyStairs(definition, x, y, out role);
                case VehicleShapeLibraryId.Grid:
                    return TryClassifyGrid(definition, x, y, out role);
                case VehicleShapeLibraryId.MazeBox:
                    return TryClassifyMazeBox(definition, x, y, out role);
                case VehicleShapeLibraryId.Crown:
                    return TryClassifyCrown(definition, x, y, out role);
                case VehicleShapeLibraryId.Shield:
                    return TryClassifyShield(definition, x, y, out role);
                case VehicleShapeLibraryId.Smile:
                    return TryClassifySmile(definition, x, y, out role);
                case VehicleShapeLibraryId.Clover:
                    return TryClassifyClover(definition, x, y, out role);
                case VehicleShapeLibraryId.Eight:
                    return TryClassifyEight(definition, x, y, out role);
                case VehicleShapeLibraryId.Fan:
                    return TryClassifyFan(definition, x, y, out role);
                default:
                    role = VehicleShapeCellRole.Fill;
                    return false;
            }
        }

        private static bool TryClassifyHeart(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            role = VehicleShapeCellRole.Fill;
            if (!IsInsideHeart(definition, x, y))
            {
                return false;
            }

            var boundaryDistance = GetBoundaryDistance(definition, x, y, IsInsideHeart);
            if (boundaryDistance <= definition.Thickness)
            {
                role = VehicleShapeCellRole.Outline;
                return true;
            }

            if (Mathf.Abs(x - CenterX) <= 1.1f && y >= 8)
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            return definition.FillInterior;
        }

        private static bool TryClassifyCircle(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            bool ringOnly,
            out VehicleShapeCellRole role)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var distance = Mathf.Sqrt(dx * dx + dy * dy);
            var outerRadius = 5.35f * definition.Scale;
            var thickness = Mathf.Lerp(0.78f, 2.25f, (definition.Thickness - 1) / 2f);
            var innerRadius = Mathf.Max(1.1f, outerRadius - thickness - (ringOnly ? 0.95f : 0f));
            role = VehicleShapeCellRole.Fill;

            if (ringOnly)
            {
                if (distance < innerRadius || distance > outerRadius)
                {
                    return false;
                }

                role = distance > outerRadius - 0.80f || distance < innerRadius + 0.80f
                    ? VehicleShapeCellRole.Outline
                    : VehicleShapeCellRole.Fill;
                return true;
            }

            if (distance > outerRadius)
            {
                return false;
            }

            if (outerRadius - distance <= thickness)
            {
                role = VehicleShapeCellRole.Outline;
                return true;
            }

            if (distance <= 1.35f)
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            return definition.FillInterior;
        }

        private static bool TryClassifyCross(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var dx = Mathf.Abs(x - CenterX);
            var dy = Mathf.Abs(y - CenterY);
            var halfWidth = 0.55f + definition.Thickness * 0.48f;
            var extent = 5.55f * definition.Scale;
            var inVertical = dx <= halfWidth && dy <= extent;
            var inHorizontal = dy <= halfWidth && dx <= extent;
            if (!inVertical && !inHorizontal)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = dx <= halfWidth && dy <= halfWidth
                ? VehicleShapeCellRole.Accent
                : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyX(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var maxAxis = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            var extent = 5.55f * definition.Scale;
            var diagonalDistance = Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy));
            var width = 0.25f + definition.Thickness * 0.55f;
            if (maxAxis > extent || diagonalDistance > width)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = maxAxis <= 1.20f ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifySquare(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var halfSize = 5.25f * definition.Scale;
            var dx = Mathf.Abs(x - CenterX);
            var dy = Mathf.Abs(y - CenterY);
            role = VehicleShapeCellRole.Fill;
            if (dx > halfSize || dy > halfSize)
            {
                return false;
            }

            var distanceToEdge = Mathf.Min(halfSize - dx, halfSize - dy);
            if (distanceToEdge <= definition.Thickness * 0.82f)
            {
                role = VehicleShapeCellRole.Outline;
                return true;
            }

            if (dx <= 1.0f && dy <= 1.0f)
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            return definition.FillInterior;
        }

        private static bool TryClassifyDiamond(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var distance = Mathf.Abs(x - CenterX) + Mathf.Abs(y - CenterY);
            var radius = 6.65f * definition.Scale;
            role = VehicleShapeCellRole.Fill;
            if (distance > radius)
            {
                return false;
            }

            if (radius - distance <= definition.Thickness * 0.95f)
            {
                role = VehicleShapeCellRole.Outline;
                return true;
            }

            if (distance <= 1.6f)
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            return definition.FillInterior;
        }

        private static bool TryClassifySemiCircle(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var dx = x - CenterX;
            var dy = y - (CenterY - 1.4f);
            var distance = Mathf.Sqrt(dx * dx + dy * dy);
            var radius = 5.10f * definition.Scale;
            var band = 0.85f + definition.Thickness * 0.22f;
            role = VehicleShapeCellRole.Fill;
            if (dy < -1.15f || Mathf.Abs(distance - radius) > band)
            {
                return false;
            }

            role = Mathf.Abs(dx) > radius - 1.4f || dy < -0.25f
                ? VehicleShapeCellRole.Accent
                : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyDoubleRing(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var distance = Mathf.Sqrt(dx * dx + dy * dy);
            var outer = 5.20f * definition.Scale;
            var inner = 2.85f * definition.Scale;
            var band = 0.62f + definition.Thickness * 0.16f;
            role = VehicleShapeCellRole.Fill;
            if (Mathf.Abs(distance - outer) <= band)
            {
                role = VehicleShapeCellRole.Outline;
                return true;
            }

            if (Mathf.Abs(distance - inner) <= band)
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            return false;
        }

        private static bool TryClassifySpiral(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var radius = Mathf.Sqrt(dx * dx + dy * dy);
            if (radius < 0.85f || radius > 5.65f * definition.Scale)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            var angle = Mathf.Atan2(dy, dx);
            if (angle < 0f)
            {
                angle += Mathf.PI * 2f;
            }

            var target = 0.95f + angle / (Mathf.PI * 2f) * 4.65f;
            var alternate = target - 2.2f;
            var band = 0.62f + definition.Thickness * 0.08f;
            if (Mathf.Abs(radius - target) > band && Mathf.Abs(radius - alternate) > band)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = radius < 1.45f ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyHeartArrow(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var point = new Vector2(x, y);
            var arrowDistance = DistanceToSegment(point, new Vector2(2.1f, 3.0f), new Vector2(11.1f, 9.8f));
            var headA = DistanceToSegment(point, new Vector2(11.1f, 9.8f), new Vector2(9.2f, 9.8f));
            var headB = DistanceToSegment(point, new Vector2(11.1f, 9.8f), new Vector2(10.3f, 7.9f));
            if (arrowDistance <= 0.58f || headA <= 0.58f || headB <= 0.58f)
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            return TryClassifyHeart(definition, x, y, out role);
        }

        private static bool TryClassifyRadialPetal(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            int petals,
            float baseRadius,
            float amplitude,
            out VehicleShapeCellRole role)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var radius = Mathf.Sqrt(dx * dx + dy * dy);
            var angle = Mathf.Atan2(dy, dx);
            var target = (baseRadius + amplitude * Mathf.Cos(petals * angle)) * definition.Scale;
            var band = 0.68f + definition.Thickness * 0.12f;
            role = VehicleShapeCellRole.Fill;
            if (radius > target + band || radius < 1.05f)
            {
                return false;
            }

            if (Mathf.Abs(radius - target) <= band || !definition.FillInterior)
            {
                role = radius < 1.45f ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
                return true;
            }

            role = VehicleShapeCellRole.Fill;
            return true;
        }

        private static bool TryClassifyStar(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var point = new Vector2(x, y);
            var inside = IsInsideStarPolygon(definition, point);
            var distanceToEdge = GetNearestStarEdgeDistance(definition, point, out _, out _);
            var outlineBand = 0.54f + definition.Thickness * 0.16f;
            role = VehicleShapeCellRole.Fill;

            if (!inside && distanceToEdge > outlineBand)
            {
                return false;
            }

            if (distanceToEdge <= outlineBand)
            {
                role = IsNearStarOuterTip(definition, point, 0.92f)
                    ? VehicleShapeCellRole.Accent
                    : VehicleShapeCellRole.Outline;
                return true;
            }

            if (IsNearStarInnerValley(definition, point, 0.72f))
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            return definition.FillInterior;
        }

        private static bool TryClassifySunburst(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var fromCenter = new Vector2(x - CenterX, y - CenterY);
            var radius = fromCenter.magnitude;
            role = VehicleShapeCellRole.Fill;
            var ringRadius = 4.75f * definition.Scale;

            var angle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
            for (var spoke = 0; spoke < 8; spoke++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(angle, spoke * 45f)) <= 8.5f &&
                    radius >= 2.45f &&
                    radius <= 5.55f * definition.Scale)
                {
                    role = radius >= ringRadius - 0.55f
                        ? VehicleShapeCellRole.Accent
                        : VehicleShapeCellRole.Outline;
                    return true;
                }
            }

            if (Mathf.Abs(radius - ringRadius) <= 0.42f &&
                (x + y + definition.VariantSeed) % 3 == 0)
            {
                role = VehicleShapeCellRole.Outline;
                return true;
            }

            return false;
        }

        private static bool TryClassifyHollowSquare(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var half = 5.15f * definition.Scale;
            var dx = Mathf.Abs(x - CenterX);
            var dy = Mathf.Abs(y - CenterY);
            var distanceToEdge = Mathf.Min(half - dx, half - dy);
            role = VehicleShapeCellRole.Fill;
            if (dx > half || dy > half || distanceToEdge > 0.82f + definition.Thickness * 0.18f)
            {
                return false;
            }

            role = dx > half - 0.95f && dy > half - 0.95f
                ? VehicleShapeCellRole.Accent
                : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyTriangle(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var top = 11.5f;
            var bottom = 1.9f;
            if (y < bottom || y > top)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            var t = (top - y) / Mathf.Max(0.01f, top - bottom);
            var halfWidth = Mathf.Lerp(0.35f, 5.45f * definition.Scale, t);
            var dx = Mathf.Abs(x - CenterX);
            if (dx > halfWidth)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            var edgeDistance = Mathf.Min(halfWidth - dx, Mathf.Min(y - bottom, top - y));
            if (edgeDistance <= 0.76f + definition.Thickness * 0.13f || !definition.FillInterior)
            {
                role = y > top - 1.2f ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
                return true;
            }

            role = VehicleShapeCellRole.Fill;
            return true;
        }

        private static bool TryClassifyArrow(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            bool doubleArrow,
            out VehicleShapeCellRole role)
        {
            var point = new Vector2(x, y);
            var shaft = y >= CenterY - 0.85f && y <= CenterY + 0.85f && x >= 2 && x <= 10;
            var rightHead = DistanceToSegment(point, new Vector2(9.2f, CenterY + 2.7f), new Vector2(12.0f, CenterY)) <= 0.72f ||
                DistanceToSegment(point, new Vector2(9.2f, CenterY - 2.7f), new Vector2(12.0f, CenterY)) <= 0.72f;
            var leftHead = doubleArrow && (
                DistanceToSegment(point, new Vector2(3.8f, CenterY + 2.7f), new Vector2(1.0f, CenterY)) <= 0.72f ||
                DistanceToSegment(point, new Vector2(3.8f, CenterY - 2.7f), new Vector2(1.0f, CenterY)) <= 0.72f);
            if (!shaft && !rightHead && !leftHead)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = rightHead || leftHead ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyLightning(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var point = new Vector2(x, y);
            var distance = Mathf.Min(
                DistanceToSegment(point, new Vector2(8.6f, 12.0f), new Vector2(4.7f, 7.2f)),
                Mathf.Min(
                    DistanceToSegment(point, new Vector2(4.7f, 7.2f), new Vector2(7.4f, 7.2f)),
                    Mathf.Min(
                        DistanceToSegment(point, new Vector2(7.4f, 7.2f), new Vector2(4.9f, 1.7f)),
                        DistanceToSegment(point, new Vector2(7.4f, 7.2f), new Vector2(10.2f, 12.0f)))));
            if (distance > 0.78f + definition.Thickness * 0.07f)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = y <= 3 || y >= 11 ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifySCurve(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var t = Mathf.InverseLerp(1.2f, 11.8f, y);
            var targetX = CenterX + Mathf.Sin(t * Mathf.PI * 2f + Mathf.PI * 0.5f) * 3.25f * definition.Scale;
            if (Mathf.Abs(x - targetX) > 0.72f + definition.Thickness * 0.10f)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = t < 0.12f || t > 0.88f ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyWave(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var t = Mathf.InverseLerp(1.2f, 11.8f, x);
            var targetY = CenterY + Mathf.Sin(t * Mathf.PI * 2f) * 2.65f * definition.Scale;
            if (Mathf.Abs(y - targetY) > 0.72f + definition.Thickness * 0.08f)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = x <= 2 || x >= 11 ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyStairs(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var step = Mathf.Clamp((x - 1) / 2, 0, 5);
            var baseY = 2 + step * 2;
            var horizontal = y == baseY && x >= 1 + step * 2 && x <= 3 + step * 2;
            var vertical = x == 3 + step * 2 && y >= baseY && y <= baseY + 2;
            if (!horizontal && !vertical)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = step == 0 || step >= 5 ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyGrid(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var inBox = x >= 2 && x <= 11 && y >= 2 && y <= 11;
            var line = x == 2 || x == 5 || x == 8 || x == 11 || y == 2 || y == 5 || y == 8 || y == 11;
            if (!inBox || !line)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = (x == 2 || x == 11) && (y == 2 || y == 11)
                ? VehicleShapeCellRole.Accent
                : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyMazeBox(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var border = (x == 2 || x == 11 || y == 2 || y == 11) &&
                !(x == 6 && y == 2) &&
                !(x == 11 && y == 7);
            var corridors = (x >= 4 && x <= 9 && y == 4) ||
                (x == 4 && y >= 4 && y <= 9) ||
                (x >= 4 && x <= 9 && y == 9) ||
                (x == 9 && y >= 6 && y <= 9) ||
                (x >= 6 && x <= 9 && y == 6);
            if (!border && !corridors)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = !border && (x == 9 || y == 6) ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyCrown(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var point = new Vector2(x, y);
            var baseLine = y >= 3 && y <= 4 && x >= 2 && x <= 11;
            var left = DistanceToSegment(point, new Vector2(2.2f, 4.0f), new Vector2(4.0f, 10.4f)) <= 0.72f;
            var center = DistanceToSegment(point, new Vector2(6.5f, 4.0f), new Vector2(6.5f, 11.6f)) <= 0.72f;
            var right = DistanceToSegment(point, new Vector2(10.8f, 4.0f), new Vector2(9.0f, 10.4f)) <= 0.72f;
            var rim = DistanceToSegment(point, new Vector2(4.0f, 10.4f), new Vector2(6.5f, 6.2f)) <= 0.72f ||
                DistanceToSegment(point, new Vector2(6.5f, 6.2f), new Vector2(9.0f, 10.4f)) <= 0.72f;
            if (!baseLine && !left && !center && !right && !rim)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = y >= 10 ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyShield(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var top = y >= 8 && y <= 11 && x >= 2 && x <= 11;
            var taper = y >= 2 && y < 8 && Mathf.Abs(x - CenterX) <= Mathf.Lerp(0.8f, 5.0f, (y - 2f) / 6f);
            if (!top && !taper)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            var dx = Mathf.Abs(x - CenterX);
            var edge = x <= 2 || x >= 11 || y >= 11 || y <= 2 || dx >= Mathf.Lerp(0.8f, 5.0f, Mathf.Clamp01((y - 2f) / 6f)) - 0.65f;
            role = edge || !definition.FillInterior ? VehicleShapeCellRole.Outline : VehicleShapeCellRole.Fill;
            if (dx <= 0.8f && y >= 5 && y <= 9)
            {
                role = VehicleShapeCellRole.Accent;
            }

            return true;
        }

        private static bool TryClassifySmile(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var distance = Mathf.Sqrt(dx * dx + dy * dy);
            var ring = Mathf.Abs(distance - 5.0f * definition.Scale) <= 0.62f;
            var leftEye = Mathf.Abs(x - 4) <= 0 && Mathf.Abs(y - 8) <= 0;
            var rightEye = Mathf.Abs(x - 9) <= 0 && Mathf.Abs(y - 8) <= 0;
            var smileArc = y <= CenterY &&
                Mathf.Abs(Mathf.Sqrt(dx * dx + (y - 7.0f) * (y - 7.0f)) - 3.0f) <= 0.62f &&
                x >= 4 && x <= 9;
            if (!ring && !leftEye && !rightEye && !smileArc)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = leftEye || rightEye || smileArc ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyClover(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var centers = new[]
            {
                new Vector2(CenterX - 1.6f, CenterY + 1.6f),
                new Vector2(CenterX + 1.6f, CenterY + 1.6f),
                new Vector2(CenterX - 1.6f, CenterY - 1.6f),
                new Vector2(CenterX + 1.6f, CenterY - 1.6f)
            };
            var point = new Vector2(x, y);
            var inside = false;
            var outline = false;
            for (var index = 0; index < centers.Length; index++)
            {
                var distance = Vector2.Distance(point, centers[index]);
                inside |= distance <= 2.45f * definition.Scale;
                outline |= Mathf.Abs(distance - 2.45f * definition.Scale) <= 0.62f;
            }

            if (!inside)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            if (Mathf.Abs(x - CenterX) <= 0.6f && Mathf.Abs(y - CenterY) <= 0.6f)
            {
                role = VehicleShapeCellRole.Accent;
                return true;
            }

            if (!definition.FillInterior && !outline)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = outline ? VehicleShapeCellRole.Outline : VehicleShapeCellRole.Fill;
            return true;
        }

        private static bool TryClassifyEight(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var point = new Vector2(x, y);
            var upper = Mathf.Abs(Vector2.Distance(point, new Vector2(CenterX, 8.7f)) - 2.75f * definition.Scale) <= 0.66f;
            var lower = Mathf.Abs(Vector2.Distance(point, new Vector2(CenterX, 4.3f)) - 2.75f * definition.Scale) <= 0.66f;
            if (!upper && !lower)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = Mathf.Abs(y - CenterY) <= 0.8f ? VehicleShapeCellRole.Accent : VehicleShapeCellRole.Outline;
            return true;
        }

        private static bool TryClassifyFan(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            out VehicleShapeCellRole role)
        {
            var origin = new Vector2(CenterX, 1.8f);
            var point = new Vector2(x, y);
            var delta = point - origin;
            var radius = delta.magnitude;
            if (radius < 2.4f || radius > 8.9f)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 28f || angle > 152f)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            var outerArc = Mathf.Abs(radius - 7.4f * definition.Scale) <= 0.78f;
            var innerArc = Mathf.Abs(radius - 4.6f * definition.Scale) <= 0.52f &&
                angle >= 38f &&
                angle <= 142f;
            var spoke = false;
            for (var index = 0; index < 5; index++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(angle, 40f + index * 25f)) <= 6.8f)
                {
                    spoke = true;
                    break;
                }
            }

            if (!outerArc && !innerArc && !spoke)
            {
                role = VehicleShapeCellRole.Fill;
                return false;
            }

            role = outerArc ? VehicleShapeCellRole.Outline : VehicleShapeCellRole.Accent;
            return true;
        }

        private static bool IsInsideHeart(VehicleShapeLayoutDefinition definition, int x, int y)
        {
            var scale = definition.Scale;
            var point = new Vector2(x, y);
            var leftLobeCenter = new Vector2(CenterX - 2.30f, 9.40f);
            var rightLobeCenter = new Vector2(CenterX + 2.30f, 9.40f);
            var lobeWidth = 2.35f * scale;
            var lobeHeight = 2.05f * scale;
            var inLeftLobe = IsInsideEllipse(point, leftLobeCenter, lobeWidth, lobeHeight);
            var inRightLobe = IsInsideEllipse(point, rightLobeCenter, lobeWidth, lobeHeight);
            var bodyT = Mathf.Clamp01((y - 0.80f) / 6.20f);
            var bodyHalfWidth = Mathf.Lerp(0.65f, 5.15f * scale, bodyT);
            if (y > 7.0f)
            {
                bodyHalfWidth = 5.15f * scale;
            }

            var inBody = y >= 0.80f &&
                y <= 9.50f &&
                Mathf.Abs(x - CenterX) <= bodyHalfWidth;
            var centerNotch = y >= 10.10f && Mathf.Abs(x - CenterX) <= 0.85f;
            return (inLeftLobe || inRightLobe || inBody) && !centerNotch;
        }

        private static bool IsInsideEllipse(Vector2 point, Vector2 center, float radiusX, float radiusY)
        {
            var normalizedX = (point.x - center.x) / Mathf.Max(0.01f, radiusX);
            var normalizedY = (point.y - center.y) / Mathf.Max(0.01f, radiusY);
            return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
        }

        private static Vector2 GetStarVertex(VehicleShapeLayoutDefinition definition, int index)
        {
            index = Mathf.Abs(index) % 10;
            var outerRadius = 5.86f * definition.Scale;
            var innerRadius = 1.92f * definition.Scale;
            var radius = index % 2 == 0 ? outerRadius : innerRadius;
            var angle = Mathf.PI * 0.5f + index * Mathf.PI / 5f;
            return new Vector2(
                CenterX + Mathf.Cos(angle) * radius,
                CenterY + Mathf.Sin(angle) * radius);
        }

        private static bool IsInsideStarPolygon(VehicleShapeLayoutDefinition definition, Vector2 point)
        {
            var inside = false;
            var previous = GetStarVertex(definition, 9);
            for (var index = 0; index < 10; index++)
            {
                var current = GetStarVertex(definition, index);
                if ((current.y > point.y) != (previous.y > point.y))
                {
                    var denominator = previous.y - current.y;
                    if (Mathf.Abs(denominator) <= 0.0001f)
                    {
                        previous = current;
                        continue;
                    }

                    var intersectionX = (previous.x - current.x) * (point.y - current.y) /
                        denominator + current.x;
                    if (point.x < intersectionX)
                    {
                        inside = !inside;
                    }
                }

                previous = current;
            }

            return inside;
        }

        private static float GetNearestStarEdgeDistance(
            VehicleShapeLayoutDefinition definition,
            Vector2 point,
            out Vector2 start,
            out Vector2 end)
        {
            var bestDistance = float.MaxValue;
            start = GetStarVertex(definition, 0);
            end = GetStarVertex(definition, 1);
            for (var index = 0; index < 10; index++)
            {
                var segmentStart = GetStarVertex(definition, index);
                var segmentEnd = GetStarVertex(definition, index + 1);
                var distance = DistanceToSegment(point, segmentStart, segmentEnd);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                start = segmentStart;
                end = segmentEnd;
            }

            return bestDistance;
        }

        private static float GetStarPathProgress(VehicleShapeLayoutDefinition definition, Vector2Int cell)
        {
            var point = new Vector2(cell.x, cell.y);
            var bestDistanceSquared = float.MaxValue;
            var bestProgress = 0f;
            for (var index = 0; index < 10; index++)
            {
                var start = GetStarVertex(definition, index);
                var end = GetStarVertex(definition, index + 1);
                var segment = end - start;
                var lengthSquared = Mathf.Max(0.0001f, segment.sqrMagnitude);
                var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
                var closest = start + segment * t;
                var distanceSquared = (point - closest).sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                bestProgress = index + t;
            }

            return bestProgress;
        }

        private static bool IsNearStarOuterTip(
            VehicleShapeLayoutDefinition definition,
            Vector2 point,
            float threshold)
        {
            for (var index = 0; index < 10; index += 2)
            {
                if (Vector2.Distance(point, GetStarVertex(definition, index)) <= threshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNearStarInnerValley(
            VehicleShapeLayoutDefinition definition,
            Vector2 point,
            float threshold)
        {
            for (var index = 1; index < 10; index += 2)
            {
                if (Vector2.Distance(point, GetStarVertex(definition, index)) <= threshold)
                {
                    return true;
                }
            }

            return false;
        }

        private delegate bool ShapeInsidePredicate(VehicleShapeLayoutDefinition definition, int x, int y);

        private static int GetBoundaryDistance(
            VehicleShapeLayoutDefinition definition,
            int x,
            int y,
            ShapeInsidePredicate predicate)
        {
            for (var radius = 1; radius <= 3; radius++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius)
                        {
                            continue;
                        }

                        var nx = x + dx;
                        var ny = y + dy;
                        if (nx < MinCell || nx > MaxCellX || ny < MinCell || ny > MaxCellY ||
                            !predicate(definition, nx, ny))
                        {
                            return radius;
                        }
                    }
                }
            }

            return 4;
        }

        private static List<VehicleShapeCell> OrderCells(
            VehicleShapeLayoutDefinition definition,
            List<VehicleShapeCell> cells)
        {
            var ordered = new List<VehicleShapeCell>(cells.Count);
            AppendRoleCells(ordered, cells, definition, VehicleShapeCellRole.Outline);
            AppendRoleCells(ordered, cells, definition, VehicleShapeCellRole.Accent);
            AppendRoleCells(ordered, cells, definition, VehicleShapeCellRole.Fill);
            return ordered;
        }

        private static void AppendRoleCells(
            List<VehicleShapeCell> ordered,
            List<VehicleShapeCell> source,
            VehicleShapeLayoutDefinition definition,
            VehicleShapeCellRole role)
        {
            var roleCells = new List<VehicleShapeCell>();
            for (var index = 0; index < source.Count; index++)
            {
                if (source[index].Role == role)
                {
                    roleCells.Add(source[index]);
                }
            }

            roleCells.Sort((left, right) =>
            {
                var angleCompare = GetCellAngle(left.Cell).CompareTo(GetCellAngle(right.Cell));
                if (angleCompare != 0)
                {
                    return angleCompare;
                }

                return GetCenterDistanceSquared(left.Cell).CompareTo(GetCenterDistanceSquared(right.Cell));
            });

            AppendDistributed(ordered, roleCells, definition.VariantSeed + (int)role * 37);
        }

        private static void AppendDistributed(
            List<VehicleShapeCell> ordered,
            List<VehicleShapeCell> source,
            int seed)
        {
            if (source.Count == 0)
            {
                return;
            }

            var used = new bool[source.Count];
            var step = PickCoprimeStep(source.Count, seed);
            var cursor = Mathf.Abs(seed) % source.Count;
            for (var count = 0; count < source.Count; count++)
            {
                while (used[cursor])
                {
                    cursor = (cursor + 1) % source.Count;
                }

                ordered.Add(source[cursor]);
                used[cursor] = true;
                cursor = (cursor + step) % source.Count;
            }
        }

        private static int PickCoprimeStep(int count, int seed)
        {
            if (count <= 2)
            {
                return 1;
            }

            var step = Mathf.Max(1, Mathf.RoundToInt(count * 0.382f) + Mathf.Abs(seed % 5));
            while (GreatestCommonDivisor(step, count) != 1)
            {
                step++;
            }

            return step % count;
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            a = Mathf.Abs(a);
            b = Mathf.Abs(b);
            while (b != 0)
            {
                var temp = a % b;
                a = b;
                b = temp;
            }

            return Mathf.Max(1, a);
        }

        private static void AddShapeSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            System.Random random,
            VehicleShapeLayoutDefinition definition,
            VehicleShapeCell cell)
        {
            if (cell.Cell.x < MinCell || cell.Cell.x > MaxCellX || cell.Cell.y < MinCell || cell.Cell.y > MaxCellY)
            {
                return;
            }

            var key = GetCellKey(cell.Cell);
            if (!occupiedCells.Add(key))
            {
                return;
            }

            GetShapeDirection(definition, cell, out var direction, out var angleOffset);
            var offset = GetShapeOffset(definition, cell);
            var hasPreferredColor = cell.Role != VehicleShapeCellRole.Fill;
            slots.Add(new VehicleLayoutSlot(
                cell.Cell,
                direction,
                angleOffset,
                offset,
                hasPreferredColor,
                GetRoleColor(definition.Kind, cell.Role),
                definition.Kind,
                cell.Role));
        }

        private static void GetShapeDirection(
            VehicleShapeLayoutDefinition definition,
            VehicleShapeCell cell,
            out GridDirection direction,
            out float angleOffset)
        {
            var fromCenter = new Vector2(cell.Cell.x - CenterX, cell.Cell.y - CenterY);
            if (fromCenter.sqrMagnitude < 0.001f)
            {
                fromCenter = Vector2.up;
            }

            Vector2 vector;
            if (definition.LibraryId != VehicleShapeLibraryId.None &&
                TryGetLibraryDirection(definition, cell, fromCenter, out vector))
            {
                // Library-specific direction selected below.
            }
            else
            {
                switch (definition.Kind)
                {
                    case VehicleShapeLayoutKind.Cross:
                        vector = Mathf.Abs(fromCenter.x) >= Mathf.Abs(fromCenter.y)
                            ? new Vector2(Mathf.Sign(fromCenter.x), 0f)
                            : new Vector2(0f, Mathf.Sign(fromCenter.y));
                        break;
                    case VehicleShapeLayoutKind.X:
                        vector = new Vector2(
                            Mathf.Sign(fromCenter.x == 0f ? 1f : fromCenter.x),
                            Mathf.Sign(fromCenter.y == 0f ? 1f : fromCenter.y));
                        break;
                    case VehicleShapeLayoutKind.Square:
                        vector = GetSquareOutward(fromCenter);
                        break;
                    default:
                        vector = fromCenter;
                        break;
                }
            }

            if (vector.sqrMagnitude < 0.001f)
            {
                vector = Vector2.up;
            }

            vector.Normalize();
            var yaw = Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
            direction = DirectionFromYaw(yaw);
            var baseYaw = GridDirectionUtility.ToYawDegrees(direction);
            var maxAngleOffset = definition.LibraryId != VehicleShapeLibraryId.None
                ? MaxLibraryShapeAngleOffsetDegrees
                : MaxShapeAngleOffsetDegrees;
            angleOffset = Mathf.Clamp(Mathf.DeltaAngle(baseYaw, yaw), -maxAngleOffset, maxAngleOffset);
        }

        private static bool TryGetLibraryDirection(
            VehicleShapeLayoutDefinition definition,
            VehicleShapeCell cell,
            Vector2 fromCenter,
            out Vector2 vector)
        {
            switch (definition.LibraryId)
            {
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Smile:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                    vector = cell.Role == VehicleShapeCellRole.Accent
                        ? fromCenter
                        : GetTangentialVector(fromCenter, definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                    vector = GetRadialPetalDirection(definition.LibraryId, cell, fromCenter, definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.Star:
                    vector = GetStarEdgeDirection(definition, cell);
                    return true;
                case VehicleShapeLibraryId.Heart:
                    vector = cell.Role == VehicleShapeCellRole.Accent
                        ? fromCenter
                        : GetTangentialVector(new Vector2(fromCenter.x, fromCenter.y * 0.82f), definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.HeartArrow:
                    vector = cell.Role == VehicleShapeCellRole.Accent
                        ? new Vector2(1f, 0.75f)
                        : GetTangentialVector(new Vector2(fromCenter.x, fromCenter.y * 0.82f), definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.Square:
                    vector = GetSquareTangent(fromCenter, definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.HollowSquare:
                    vector = GetHollowSquareDirection(cell.Cell, fromCenter, definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.Diamond:
                    vector = GetTangentialVector(fromCenter, definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.Triangle:
                    vector = cell.Cell.y >= 10
                        ? Vector2.down
                        : cell.Cell.x < CenterX
                            ? new Vector2(1f, -1f)
                            : new Vector2(-1f, -1f);
                    return true;
                case VehicleShapeLibraryId.Cross:
                    vector = Mathf.Abs(fromCenter.x) >= Mathf.Abs(fromCenter.y)
                        ? new Vector2(Mathf.Sign(fromCenter.x == 0f ? 1f : fromCenter.x), 0f)
                        : new Vector2(0f, Mathf.Sign(fromCenter.y == 0f ? 1f : fromCenter.y));
                    return true;
                case VehicleShapeLibraryId.X:
                    vector = new Vector2(
                        Mathf.Sign(fromCenter.x == 0f ? 1f : fromCenter.x),
                        Mathf.Sign(fromCenter.y == 0f ? 1f : fromCenter.y));
                    return true;
                case VehicleShapeLibraryId.Arrow:
                    vector = Vector2.right;
                    return true;
                case VehicleShapeLibraryId.DoubleArrow:
                    vector = cell.Cell.x < CenterX ? Vector2.left : Vector2.right;
                    return true;
                case VehicleShapeLibraryId.Lightning:
                    vector = cell.Cell.y > CenterY ? new Vector2(-1f, -1f) : new Vector2(1f, -1f);
                    return true;
                case VehicleShapeLibraryId.S:
                    vector = GetSCurveTangent(cell.Cell);
                    return true;
                case VehicleShapeLibraryId.Wave:
                    vector = GetWaveTangent(cell.Cell);
                    return true;
                case VehicleShapeLibraryId.Stairs:
                    vector = GetStairsTangent(cell.Cell);
                    return true;
                case VehicleShapeLibraryId.Grid:
                    vector = Mathf.Abs(cell.Cell.x - CenterX) > Mathf.Abs(cell.Cell.y - CenterY)
                        ? Vector2.up
                        : Vector2.right;
                    return true;
                case VehicleShapeLibraryId.MazeBox:
                    vector = GetMazeDirection(cell.Cell);
                    return true;
                case VehicleShapeLibraryId.Crown:
                    vector = cell.Cell.y <= 4 ? Vector2.right : new Vector2(Mathf.Sign(CenterX - cell.Cell.x), 1f);
                    return true;
                case VehicleShapeLibraryId.Shield:
                    vector = cell.Role == VehicleShapeCellRole.Accent
                        ? Vector2.down
                        : GetTangentialVector(fromCenter, definition.Clockwise);
                    return true;
                case VehicleShapeLibraryId.Fan:
                    vector = cell.Role == VehicleShapeCellRole.Outline
                        ? GetTangentialVector(new Vector2(cell.Cell.x - CenterX, cell.Cell.y - 1.6f), definition.Clockwise)
                        : new Vector2(cell.Cell.x - CenterX, cell.Cell.y - 1.6f);
                    return true;
                default:
                    vector = Vector2.zero;
                    return false;
            }
        }

        private static Vector2 GetTangentialVector(Vector2 fromCenter, bool clockwise)
        {
            return clockwise
                ? new Vector2(fromCenter.y, -fromCenter.x)
                : new Vector2(-fromCenter.y, fromCenter.x);
        }

        private static Vector2 GetRadialPetalDirection(
            VehicleShapeLibraryId libraryId,
            VehicleShapeCell cell,
            Vector2 fromCenter,
            bool clockwise)
        {
            if (cell.Role == VehicleShapeCellRole.Accent)
            {
                return fromCenter;
            }

            if (libraryId == VehicleShapeLibraryId.Sunburst)
            {
                return fromCenter;
            }

            var tipRadius = libraryId == VehicleShapeLibraryId.Flower
                ? 3.75f
                : libraryId == VehicleShapeLibraryId.Sunburst
                    ? 3.60f
                    : 4.10f;
            return fromCenter.magnitude >= tipRadius
                ? fromCenter
                : GetTangentialVector(fromCenter, clockwise);
        }

        private static Vector2 GetStarEdgeDirection(
            VehicleShapeLayoutDefinition definition,
            VehicleShapeCell cell)
        {
            var point = new Vector2(cell.Cell.x, cell.Cell.y);
            GetNearestStarEdgeDistance(definition, point, out var start, out var end);
            var tangent = end - start;
            if (tangent.sqrMagnitude < 0.001f)
            {
                return point - new Vector2(CenterX, CenterY);
            }

            tangent.Normalize();
            return definition.Clockwise ? -tangent : tangent;
        }

        private static Vector2 GetSquareTangent(Vector2 fromCenter, bool clockwise)
        {
            var horizontalEdge = Mathf.Abs(fromCenter.y) >= Mathf.Abs(fromCenter.x);
            if (horizontalEdge)
            {
                var sign = Mathf.Sign(fromCenter.y == 0f ? 1f : fromCenter.y);
                return clockwise ? new Vector2(sign, 0f) : new Vector2(-sign, 0f);
            }

            var verticalSign = Mathf.Sign(fromCenter.x == 0f ? 1f : fromCenter.x);
            return clockwise ? new Vector2(0f, -verticalSign) : new Vector2(0f, verticalSign);
        }

        private static Vector2 GetSCurveTangent(Vector2Int cell)
        {
            var t = Mathf.InverseLerp(1.2f, 11.8f, cell.y);
            var slope = Mathf.Cos(t * Mathf.PI * 2f + Mathf.PI * 0.5f) * 3.25f;
            return new Vector2(slope, 1f);
        }

        private static Vector2 GetWaveTangent(Vector2Int cell)
        {
            var t = Mathf.InverseLerp(1.2f, 11.8f, cell.x);
            var slope = Mathf.Cos(t * Mathf.PI * 2f) * 2.65f;
            return new Vector2(1f, slope);
        }

        private static Vector2 GetStairsTangent(Vector2Int cell)
        {
            var step = Mathf.Clamp((cell.x - 1) / 2, 0, 5);
            var baseY = 2 + step * 2;
            var horizontal = cell.y == baseY;
            var vertical = cell.x == 3 + step * 2;
            var hash = Mathf.Abs(cell.x * 37 + cell.y * 19);

            if (horizontal)
            {
                return hash % 4 == 0 ? Vector2.left : Vector2.right;
            }

            if (vertical)
            {
                return hash % 4 == 0 ? Vector2.down : Vector2.up;
            }

            return cell.x < CenterX ? Vector2.right : Vector2.up;
        }

        private static Vector2 GetMazeDirection(Vector2Int cell)
        {
            if (cell.y == 4 || cell.y == 6 || cell.y == 9)
            {
                return Vector2.right;
            }

            if (cell.x == 4 || cell.x == 9)
            {
                return Vector2.up;
            }

            return GetSquareTangent(new Vector2(cell.x - CenterX, cell.y - CenterY), true);
        }

        private static Vector2 GetSquareOutward(Vector2 fromCenter)
        {
            var horizontalEdge = Mathf.Abs(fromCenter.y) >= Mathf.Abs(fromCenter.x);
            if (horizontalEdge)
            {
                return new Vector2(0f, Mathf.Sign(fromCenter.y));
            }

            return new Vector2(Mathf.Sign(fromCenter.x), 0f);
        }

        private static Vector2 GetHollowSquareDirection(Vector2Int cell, Vector2 fromCenter, bool clockwise)
        {
            var outward = GetSquareOutward(fromCenter);
            var absX = Mathf.Abs(fromCenter.x);
            var absY = Mathf.Abs(fromCenter.y);
            var isCorner = absX >= 4.2f && absY >= 4.2f;
            if (isCorner)
            {
                return outward;
            }

            var hash = Mathf.Abs(cell.x * 31 + cell.y * 17);
            if (hash % 4 == 0)
            {
                return outward;
            }

            if (hash % 7 == 0)
            {
                return -outward;
            }

            return GetSquareTangent(fromCenter, clockwise);
        }

        private static Vector2 GetShapeOffset(VehicleShapeLayoutDefinition definition, VehicleShapeCell cell)
        {
            if (cell.Role != VehicleShapeCellRole.Outline)
            {
                return Vector2.zero;
            }

            var fromCenter = new Vector2(cell.Cell.x - CenterX, cell.Cell.y - CenterY);
            if (fromCenter.sqrMagnitude < 0.001f)
            {
                return Vector2.zero;
            }

            var amount = definition.Kind == VehicleShapeLayoutKind.X ? 0.04f : 0.025f;
            return fromCenter.normalized * amount;
        }

        public static bool IsFeatureCell(VehicleShapeLayoutDefinition definition, VehicleShapeCell cell)
        {
            if (cell.Role == VehicleShapeCellRole.Accent)
            {
                return true;
            }

            var x = cell.Cell.x;
            var y = cell.Cell.y;
            var dx = x - CenterX;
            var dy = y - CenterY;
            var absX = Mathf.Abs(dx);
            var absY = Mathf.Abs(dy);
            switch (definition.LibraryId)
            {
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                    return (y <= 2 && absX <= 1.7f) ||
                        (y >= 9 && absX <= 1.4f) ||
                        (y >= 8 && Mathf.Abs(absX - 2.5f) <= 0.9f) ||
                        (absX >= 4.2f && y >= 5 && y <= 8);
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.Eight:
                    return absX <= 0.7f || absY <= 0.7f || Mathf.Abs(absX - absY) <= 0.35f;
                case VehicleShapeLibraryId.Star:
                    return IsNearStarOuterTip(definition, new Vector2(x, y), 1.30f) ||
                        IsNearStarInnerValley(definition, new Vector2(x, y), 0.95f);
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Fan:
                    return GetCenterDistanceSquared(cell.Cell) >= 17.5f;
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                    return absX >= 4.4f && absY >= 4.4f;
                case VehicleShapeLibraryId.Diamond:
                    return absX <= 0.7f || absY <= 0.7f;
                case VehicleShapeLibraryId.Triangle:
                    return y >= 10 || (y <= 3 && absX >= 3.5f);
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                    return Mathf.Max(absX, absY) >= 4.5f || Mathf.Max(absX, absY) <= 1.2f;
                case VehicleShapeLibraryId.Arrow:
                    return x >= 9 || x <= 2;
                case VehicleShapeLibraryId.DoubleArrow:
                    return x <= 3 || x >= 10;
                case VehicleShapeLibraryId.Lightning:
                    return y <= 3 || y >= 10 || (Mathf.Abs(y - 7) <= 1 && x >= 5 && x <= 8);
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                    return x <= 2 || x >= 11 || y <= 2 || y >= 11;
                case VehicleShapeLibraryId.Crown:
                    return y >= 9 || (y <= 4 && (x <= 3 || x >= 10));
                case VehicleShapeLibraryId.Shield:
                    return y <= 3 || y >= 10 || absX >= 4.0f;
                case VehicleShapeLibraryId.Smile:
                    return (Mathf.Abs(x - 4) <= 1 && Mathf.Abs(y - 8) <= 1) ||
                        (Mathf.Abs(x - 9) <= 1 && Mathf.Abs(y - 8) <= 1) ||
                        (y <= 5 && x >= 4 && x <= 9);
                case VehicleShapeLibraryId.Clover:
                    return (absX >= 2.0f && absY >= 2.0f) || (absX <= 0.7f && absY <= 0.7f);
                default:
                    return false;
            }
        }

        private static int GetFeaturePriority(VehicleShapeLayoutDefinition definition, VehicleShapeCell cell)
        {
            var x = cell.Cell.x;
            var y = cell.Cell.y;
            var dx = x - CenterX;
            var dy = y - CenterY;
            var absX = Mathf.Abs(dx);
            switch (definition.LibraryId)
            {
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                    if (y <= 2 && absX <= 1.7f)
                    {
                        return 0;
                    }

                    if (y >= 9 && absX <= 1.4f)
                    {
                        return 1;
                    }

                    if (y >= 8)
                    {
                        return 2;
                    }

                    return 3;
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Crown:
                    return -Mathf.RoundToInt(GetCenterDistanceSquared(cell.Cell) * 10f);
                case VehicleShapeLibraryId.Star:
                    if (IsNearStarOuterTip(definition, new Vector2(x, y), 1.30f))
                    {
                        return 0;
                    }

                    return IsNearStarInnerValley(definition, new Vector2(x, y), 0.95f) ? 1 : 2;
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                    return cell.Role == VehicleShapeCellRole.Accent ? 0 : 1;
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                    return Mathf.Max(Mathf.Abs(Mathf.RoundToInt(dx)), Mathf.Abs(Mathf.RoundToInt(dy))) <= 1 ? 0 : 1;
                default:
                    return cell.Role == VehicleShapeCellRole.Accent ? 0 : 1;
            }
        }

        private static GridDirection DirectionFromYaw(float yaw)
        {
            yaw = Mathf.Repeat(yaw + 360f, 360f);
            if (yaw >= 45f && yaw < 135f)
            {
                return GridDirection.Right;
            }

            if (yaw >= 135f && yaw < 225f)
            {
                return GridDirection.Down;
            }

            return yaw >= 225f && yaw < 315f ? GridDirection.Left : GridDirection.Up;
        }

        private static PuzzleColor GetRoleColor(VehicleShapeLayoutKind kind, VehicleShapeCellRole role)
        {
            switch (kind)
            {
                case VehicleShapeLayoutKind.Heart:
                    return role == VehicleShapeCellRole.Outline
                        ? PuzzleColor.Pink
                        : role == VehicleShapeCellRole.Accent
                            ? PuzzleColor.White
                            : PuzzleColor.Red;
                case VehicleShapeLayoutKind.Circle:
                case VehicleShapeLayoutKind.Ring:
                    return role == VehicleShapeCellRole.Outline
                        ? PuzzleColor.SkyBlue
                        : role == VehicleShapeCellRole.Accent
                            ? PuzzleColor.White
                            : PuzzleColor.Blue;
                case VehicleShapeLayoutKind.Cross:
                    return role == VehicleShapeCellRole.Outline
                        ? PuzzleColor.White
                        : role == VehicleShapeCellRole.Accent
                            ? PuzzleColor.Blue
                            : PuzzleColor.Red;
                case VehicleShapeLayoutKind.X:
                    return role == VehicleShapeCellRole.Outline
                        ? PuzzleColor.Yellow
                        : role == VehicleShapeCellRole.Accent
                            ? PuzzleColor.Black
                            : PuzzleColor.Orange;
                case VehicleShapeLayoutKind.Square:
                    return role == VehicleShapeCellRole.Outline
                        ? PuzzleColor.Lime
                        : role == VehicleShapeCellRole.Accent
                            ? PuzzleColor.White
                            : PuzzleColor.Green;
                case VehicleShapeLayoutKind.Diamond:
                    return role == VehicleShapeCellRole.Outline
                        ? PuzzleColor.Purple
                        : role == VehicleShapeCellRole.Accent
                            ? PuzzleColor.White
                            : PuzzleColor.Pink;
                default:
                    return PuzzleColor.Red;
            }
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return Vector2.Distance(point, start);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * t);
        }

        private static float GetNearestShapeDistanceCells(Vector2 position, IReadOnlyList<VehicleShapeCell> cells)
        {
            var best = float.MaxValue;
            for (var index = 0; index < cells.Count; index++)
            {
                var shapeCell = new Vector2(cells[index].Cell.x, cells[index].Cell.y);
                best = Mathf.Min(best, Vector2.Distance(position, shapeCell));
            }

            return best;
        }

        private static int GetNearestShapeDistanceSquared(Vector2Int cell, Dictionary<int, VehicleShapeCellRole> roleByCell)
        {
            var best = int.MaxValue;
            foreach (var pair in roleByCell)
            {
                var shapeCell = DecodeCellKey(pair.Key);
                var dx = shapeCell.x - cell.x;
                var dy = shapeCell.y - cell.y;
                best = Mathf.Min(best, dx * dx + dy * dy);
            }

            return best;
        }

        private static float GetLibraryPathProgress(VehicleShapeLibraryId libraryId, Vector2Int cell)
        {
            var x = cell.x - CenterX;
            var y = cell.y - CenterY;
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Spiral:
                {
                    var angle = Mathf.Atan2(y, x);
                    if (angle < 0f)
                    {
                        angle += Mathf.PI * 2f;
                    }

                    var radius = Mathf.Sqrt(x * x + y * y);
                    return angle + radius * 0.34f;
                }
                case VehicleShapeLibraryId.Lightning:
                    return -cell.y * 10f + cell.x;
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Stairs:
                    return cell.y * 10f + cell.x;
                case VehicleShapeLibraryId.Wave:
                    return cell.x * 10f + cell.y;
                case VehicleShapeLibraryId.Fan:
                {
                    var origin = new Vector2(CenterX, 1.8f);
                    var delta = new Vector2(cell.x, cell.y) - origin;
                    var angle = Mathf.Atan2(delta.y, delta.x);
                    return angle * 10f + delta.magnitude * 0.12f;
                }
                default:
                    return GetCellAngle(cell);
            }
        }

        private static float GetCellAngle(Vector2Int cell)
        {
            return Mathf.Atan2(cell.y - CenterY, cell.x - CenterX);
        }

        private static float GetCenterDistanceSquared(Vector2Int cell)
        {
            var dx = cell.x - CenterX;
            var dy = cell.y - CenterY;
            return dx * dx + dy * dy;
        }

        private static int GetCellKey(Vector2Int cell)
        {
            return cell.x * BoardLayoutConfig.GridRows + cell.y;
        }

        private static Vector2Int DecodeCellKey(int key)
        {
            return new Vector2Int(key / BoardLayoutConfig.GridRows, key % BoardLayoutConfig.GridRows);
        }
    }
}
