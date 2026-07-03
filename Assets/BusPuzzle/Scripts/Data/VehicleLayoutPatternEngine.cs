using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal enum VehicleLayoutPatternId
    {
        Ring,
        TerminalRows,
        DiagonalBands,
        Spiral,
        SplitClusters,
        Chevron,
        DenseBlock,
        DiamondCross,
        MazeRows,
        PackedClusters,
        DenseJam,
        ShapeHeart,
        ShapeCircle,
        ShapeRing,
        ShapeCross,
        ShapeX,
        ShapeSquare,
        ShapeDiamond,
        ShowcaseRows,
        ShowcaseRing,
        ShowcaseFrame,
        ShowcaseQuads,
        ShowcaseHeart
    }

    internal readonly struct VehicleLayoutSlot
    {
        public readonly Vector2Int GridPosition;
        public readonly GridDirection Direction;
        public readonly float AngleOffsetDegrees;
        public readonly Vector2 PositionOffsetCells;
        public readonly bool HasPreferredColor;
        public readonly PuzzleColor PreferredColor;
        public readonly VehicleShapeLayoutKind ShapeKind;
        public readonly VehicleShapeCellRole ShapeRole;

        public VehicleLayoutSlot(
            Vector2Int gridPosition,
            GridDirection direction,
            float angleOffsetDegrees,
            Vector2 positionOffsetCells)
            : this(
                gridPosition,
                direction,
                angleOffsetDegrees,
                positionOffsetCells,
                false,
                default,
                VehicleShapeLayoutKind.None,
                VehicleShapeCellRole.Fill)
        {
        }

        public VehicleLayoutSlot(
            Vector2Int gridPosition,
            GridDirection direction,
            float angleOffsetDegrees,
            Vector2 positionOffsetCells,
            bool hasPreferredColor,
            PuzzleColor preferredColor,
            VehicleShapeLayoutKind shapeKind,
            VehicleShapeCellRole shapeRole)
        {
            GridPosition = gridPosition;
            Direction = direction;
            AngleOffsetDegrees = angleOffsetDegrees;
            PositionOffsetCells = positionOffsetCells;
            HasPreferredColor = hasPreferredColor;
            PreferredColor = preferredColor;
            ShapeKind = shapeKind;
            ShapeRole = shapeRole;
        }
    }

    internal static class VehicleLayoutPatternEngine
    {
        public const int AutoLayoutVariant = -1;
        private const int VariantsPerPattern = 20;
        private const int ShapeLibraryVariantBase = -10000;
        private const int ShapeLibraryVariantStride = 1000;
        private const int MinCell = 1;
        private const int MaxCellX = BoardLayoutConfig.GridColumns - 2;
        private const int MaxCellY = BoardLayoutConfig.GridRows - 2;
        private const float CenterX = (BoardLayoutConfig.GridColumns - 1) * 0.5f;
        private const float CenterY = (BoardLayoutConfig.GridRows - 1) * 0.5f;

        private static readonly VehicleLayoutPatternId[] StagePatternPool =
        {
            VehicleLayoutPatternId.TerminalRows,
            VehicleLayoutPatternId.DiagonalBands,
            VehicleLayoutPatternId.Ring,
            VehicleLayoutPatternId.Spiral,
            VehicleLayoutPatternId.SplitClusters,
            VehicleLayoutPatternId.Chevron,
            VehicleLayoutPatternId.DenseBlock,
            VehicleLayoutPatternId.DiamondCross,
            VehicleLayoutPatternId.MazeRows,
            VehicleLayoutPatternId.PackedClusters,
            VehicleLayoutPatternId.DenseJam,
            VehicleLayoutPatternId.ShapeHeart,
            VehicleLayoutPatternId.ShapeCircle,
            VehicleLayoutPatternId.ShapeRing,
            VehicleLayoutPatternId.ShapeCross,
            VehicleLayoutPatternId.ShapeX,
            VehicleLayoutPatternId.ShapeSquare,
            VehicleLayoutPatternId.ShapeDiamond,
            VehicleLayoutPatternId.ShowcaseRows,
            VehicleLayoutPatternId.ShowcaseRing,
            VehicleLayoutPatternId.ShowcaseFrame,
            VehicleLayoutPatternId.ShowcaseQuads,
            VehicleLayoutPatternId.ShowcaseHeart
        };

        private static readonly VehicleLayoutPatternId[] NormalPatterns =
        {
            VehicleLayoutPatternId.ShapeHeart,
            VehicleLayoutPatternId.ShapeCircle,
            VehicleLayoutPatternId.ShapeSquare,
            VehicleLayoutPatternId.ShapeX,
            VehicleLayoutPatternId.ShapeDiamond,
            VehicleLayoutPatternId.ShowcaseRows,
            VehicleLayoutPatternId.ShowcaseRing,
            VehicleLayoutPatternId.TerminalRows
        };

        private static readonly VehicleLayoutPatternId[] NormalPressurePatterns =
        {
            VehicleLayoutPatternId.ShapeHeart,
            VehicleLayoutPatternId.ShapeCircle,
            VehicleLayoutPatternId.ShapeRing,
            VehicleLayoutPatternId.ShapeCross,
            VehicleLayoutPatternId.ShapeX,
            VehicleLayoutPatternId.ShapeSquare,
            VehicleLayoutPatternId.ShowcaseRows,
            VehicleLayoutPatternId.TerminalRows,
            VehicleLayoutPatternId.DenseBlock
        };

        private static readonly VehicleLayoutPatternId[] HardPatterns =
        {
            VehicleLayoutPatternId.ShapeCross,
            VehicleLayoutPatternId.ShapeX,
            VehicleLayoutPatternId.ShapeRing,
            VehicleLayoutPatternId.ShapeDiamond,
            VehicleLayoutPatternId.ShapeSquare,
            VehicleLayoutPatternId.ShowcaseFrame,
            VehicleLayoutPatternId.ShowcaseQuads,
            VehicleLayoutPatternId.PackedClusters,
            VehicleLayoutPatternId.DenseBlock
        };

        private static readonly VehicleLayoutPatternId[] SuperHardPatterns =
        {
            VehicleLayoutPatternId.ShapeCircle,
            VehicleLayoutPatternId.ShapeCross,
            VehicleLayoutPatternId.ShapeX,
            VehicleLayoutPatternId.ShapeDiamond,
            VehicleLayoutPatternId.ShapeSquare,
            VehicleLayoutPatternId.ShowcaseFrame,
            VehicleLayoutPatternId.PackedClusters,
            VehicleLayoutPatternId.DenseBlock,
            VehicleLayoutPatternId.DenseJam
        };

        private static readonly VehicleLayoutPatternId[] MidPressurePatterns =
        {
            VehicleLayoutPatternId.ShapeHeart,
            VehicleLayoutPatternId.ShapeCircle,
            VehicleLayoutPatternId.ShapeRing,
            VehicleLayoutPatternId.ShapeCross,
            VehicleLayoutPatternId.ShapeX,
            VehicleLayoutPatternId.ShapeSquare,
            VehicleLayoutPatternId.ShapeDiamond,
            VehicleLayoutPatternId.ShowcaseFrame,
            VehicleLayoutPatternId.ShowcaseRows,
            VehicleLayoutPatternId.DenseBlock,
            VehicleLayoutPatternId.PackedClusters,
            VehicleLayoutPatternId.DenseJam
        };

        private static readonly VehicleLayoutPatternId[] LatePressurePatterns =
        {
            VehicleLayoutPatternId.ShapeHeart,
            VehicleLayoutPatternId.ShapeCircle,
            VehicleLayoutPatternId.ShapeRing,
            VehicleLayoutPatternId.ShapeCross,
            VehicleLayoutPatternId.ShapeX,
            VehicleLayoutPatternId.ShapeSquare,
            VehicleLayoutPatternId.ShapeDiamond,
            VehicleLayoutPatternId.ShowcaseFrame,
            VehicleLayoutPatternId.ShowcaseRows,
            VehicleLayoutPatternId.DenseBlock,
            VehicleLayoutPatternId.PackedClusters,
            VehicleLayoutPatternId.DenseJam,
            VehicleLayoutPatternId.MazeRows
        };

        public static int UniqueLayoutVariantCount => StagePatternPool.Length * VariantsPerPattern;
        public static int ShapeLibraryVariantCount => VehicleShapeLayoutEngine.ShapeLibraryCount;

        public static int GetShapeLibraryVariantIndex(int libraryIndex, int variantSeed = 0)
        {
            libraryIndex = Mathf.Clamp(libraryIndex, 0, VehicleShapeLayoutEngine.ShapeLibraryCount - 1);
            variantSeed = Mathf.Clamp(variantSeed, 0, ShapeLibraryVariantStride - 1);
            return ShapeLibraryVariantBase - libraryIndex * ShapeLibraryVariantStride - variantSeed;
        }

        public static bool TryGetShapeLibraryIndex(int layoutVariantIndex, out int libraryIndex)
        {
            libraryIndex = -1;
            if (layoutVariantIndex > ShapeLibraryVariantBase)
            {
                return false;
            }

            var encoded = ShapeLibraryVariantBase - layoutVariantIndex;
            libraryIndex = encoded / ShapeLibraryVariantStride;
            return libraryIndex >= 0 && libraryIndex < VehicleShapeLayoutEngine.ShapeLibraryCount;
        }

        public static bool TryGetShapeLibraryVariantSeed(int layoutVariantIndex, out int variantSeed)
        {
            variantSeed = 0;
            if (!TryGetShapeLibraryIndex(layoutVariantIndex, out _))
            {
                return false;
            }

            var encoded = ShapeLibraryVariantBase - layoutVariantIndex;
            variantSeed = encoded % ShapeLibraryVariantStride;
            return true;
        }

        public static int GetProbeLayoutVariantIndex(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            int probeIndex)
        {
            if (TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                TryGetShapeLibraryVariantSeed(layoutVariantIndex, out var variantSeed);
                return GetShapeLibraryVariantIndex(libraryIndex, variantSeed + probeIndex);
            }

            if (layoutVariantIndex < 0 || probeIndex <= 0)
            {
                return layoutVariantIndex;
            }

            return layoutVariantIndex + GetLayoutVariantStride(profile) * probeIndex;
        }

        public static int GetLayoutVariantStride(LevelDifficultyProfile profile)
        {
            var patterns = GetPatternPool(profile);
            return Mathf.Max(1, patterns.Length);
        }

        public static List<VehicleLayoutSlot> CreateSlots(
            LevelDifficultyProfile profile,
            System.Random random,
            int targetVehicleCount)
        {
            return CreateSlots(profile, random, targetVehicleCount, AutoLayoutVariant);
        }

        public static List<VehicleLayoutSlot> CreateSlots(
            LevelDifficultyProfile profile,
            System.Random random,
            int targetVehicleCount,
            int layoutVariantIndex)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            random = random ?? new System.Random(0);
            targetVehicleCount = Mathf.Clamp(targetVehicleCount, 1, 80);

            var slots = new List<VehicleLayoutSlot>(targetVehicleCount * 3);
            var occupiedCells = new HashSet<int>();
            if (TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                var librarySlotRandom = CreateSlotRandom(profile.Difficulty, random, targetVehicleCount, layoutVariantIndex);
                AddShapeLibraryLayout(slots, occupiedCells, profile, librarySlotRandom, libraryIndex, targetVehicleCount, layoutVariantIndex);
                return slots;
            }

            var pattern = PickPattern(profile, random, layoutVariantIndex);
            if (ShouldForceDenseJam(profile, targetVehicleCount))
            {
                pattern = VehicleLayoutPatternId.DenseJam;
            }

            var slotRandom = CreateSlotRandom(profile.Difficulty, random, targetVehicleCount, layoutVariantIndex);

            switch (pattern)
            {
                case VehicleLayoutPatternId.DenseBlock:
                    AddDenseBlock(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.DiamondCross:
                    AddDiamondCross(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.MazeRows:
                    AddMazeRows(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.PackedClusters:
                    AddPackedClusters(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.DenseJam:
                    AddDenseJam(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.ShapeHeart:
                case VehicleLayoutPatternId.ShapeCircle:
                case VehicleLayoutPatternId.ShapeRing:
                case VehicleLayoutPatternId.ShapeCross:
                case VehicleLayoutPatternId.ShapeX:
                case VehicleLayoutPatternId.ShapeSquare:
                case VehicleLayoutPatternId.ShapeDiamond:
                    AddShapeLayout(slots, occupiedCells, profile, slotRandom, pattern, targetVehicleCount, layoutVariantIndex);
                    break;
                case VehicleLayoutPatternId.ShowcaseRows:
                    AddShowcaseRows(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.ShowcaseRing:
                    AddShapeLayout(slots, occupiedCells, profile, slotRandom, pattern, targetVehicleCount, layoutVariantIndex);
                    break;
                case VehicleLayoutPatternId.ShowcaseFrame:
                    AddShapeLayout(slots, occupiedCells, profile, slotRandom, pattern, targetVehicleCount, layoutVariantIndex);
                    break;
                case VehicleLayoutPatternId.ShowcaseQuads:
                    AddShapeLayout(slots, occupiedCells, profile, slotRandom, pattern, targetVehicleCount, layoutVariantIndex);
                    break;
                case VehicleLayoutPatternId.ShowcaseHeart:
                    AddShapeLayout(slots, occupiedCells, profile, slotRandom, pattern, targetVehicleCount, layoutVariantIndex);
                    break;
                case VehicleLayoutPatternId.TerminalRows:
                    AddTerminalRows(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.DiagonalBands:
                    AddDiagonalBands(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.Spiral:
                    AddSpiral(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.SplitClusters:
                    AddSplitClusters(slots, occupiedCells, profile, slotRandom);
                    break;
                case VehicleLayoutPatternId.Chevron:
                    AddChevron(slots, occupiedCells, profile, slotRandom);
                    break;
                default:
                    AddRing(slots, occupiedCells, profile, slotRandom);
                    break;
            }

            AddFillerSlots(slots, occupiedCells, profile, slotRandom);
            return slots;
        }

        public static int ScoreShapeFidelity(
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int layoutVariantIndex,
            IReadOnlyList<BusDefinition> vehicles)
        {
            return TryCreateShapeDefinition(profile, targetVehicleCount, layoutVariantIndex, out var definition)
                ? VehicleShapeLayoutEngine.ScoreShapeFidelity(definition, vehicles)
                : 0;
        }

        public static bool TryCreateShapeDefinition(
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int layoutVariantIndex,
            out VehicleShapeLayoutDefinition definition)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            targetVehicleCount = Mathf.Clamp(targetVehicleCount, 1, 80);
            if (TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                return VehicleShapeLayoutEngine.TryCreateLibraryDefinition(
                    libraryIndex,
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                    out definition);
            }

            var pattern = PickPattern(profile, new System.Random(0), layoutVariantIndex);
            if (ShouldForceDenseJam(profile, targetVehicleCount))
            {
                definition = default;
                return false;
            }

            return VehicleShapeLayoutEngine.TryCreateDefinition(
                pattern,
                profile,
                targetVehicleCount,
                layoutVariantIndex,
                out definition);
        }

        private static VehicleLayoutPatternId PickPattern(
            LevelDifficultyProfile profile,
            System.Random random,
            int layoutVariantIndex)
        {
            var patterns = GetPatternPool(profile);
            if (layoutVariantIndex >= 0 && patterns.Length > 0)
            {
                return patterns[Mathf.Abs(layoutVariantIndex) % patterns.Length];
            }

            return patterns[random.Next(0, patterns.Length)];
        }

        private static VehicleLayoutPatternId[] GetPatternPool(LevelDifficultyProfile profile)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var pressure = GetLayoutPressure(profile);
            if (profile.Difficulty == LevelDifficulty.SuperHard && pressure >= 0.78f)
            {
                return LatePressurePatterns;
            }

            if (profile.Difficulty != LevelDifficulty.Normal && pressure >= 0.64f)
            {
                return MidPressurePatterns;
            }

            if (profile.Difficulty == LevelDifficulty.Normal && pressure >= 0.52f)
            {
                return NormalPressurePatterns;
            }

            if (profile.Difficulty == LevelDifficulty.SuperHard)
            {
                return SuperHardPatterns;
            }

            return profile.Difficulty == LevelDifficulty.Hard ? HardPatterns : NormalPatterns;
        }

        private static bool ShouldForceDenseJam(LevelDifficultyProfile profile, int targetVehicleCount)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            targetVehicleCount = Mathf.Max(1, targetVehicleCount);
            var pressure = GetLayoutPressure(profile);
            switch (profile.Difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return targetVehicleCount >= 58 && pressure >= 0.78f;
                case LevelDifficulty.Hard:
                    return targetVehicleCount >= 54 && pressure >= 0.70f;
                default:
                    return targetVehicleCount >= 60 && pressure >= 0.58f;
            }
        }

        private static void AddShapeLayout(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            VehicleLayoutPatternId pattern,
            int targetVehicleCount,
            int layoutVariantIndex)
        {
            if (VehicleShapeLayoutEngine.TryCreateDefinition(
                    pattern,
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                out var definition))
            {
                VehicleShapeLayoutEngine.AddShapeSlots(slots, occupiedCells, profile, random, definition, targetVehicleCount);
            }
        }

        private static void AddShapeLibraryLayout(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int libraryIndex,
            int targetVehicleCount,
            int layoutVariantIndex)
        {
            if (VehicleShapeLayoutEngine.TryCreateLibraryDefinition(
                    libraryIndex,
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                    out var definition))
            {
                VehicleShapeLayoutEngine.AddShapeSlots(slots, occupiedCells, profile, random, definition, targetVehicleCount);
            }
        }

        private static float GetLayoutPressure(LevelDifficultyProfile profile)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            return Mathf.Clamp01(profile.ParkingTension * 0.65f + profile.StationPressure * 0.35f);
        }

        private static System.Random CreateSlotRandom(
            LevelDifficulty difficulty,
            System.Random fallbackRandom,
            int targetVehicleCount,
            int layoutVariantIndex)
        {
            if (layoutVariantIndex < 0)
            {
                return fallbackRandom;
            }

            unchecked
            {
                var seed = 1597463007;
                seed = seed * 31 + layoutVariantIndex;
                seed = seed * 31 + targetVehicleCount;
                seed = seed * 31 + (int)difficulty;
                return new System.Random(seed);
            }
        }

        private static void AddRing(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var startAngle = (float)random.NextDouble() * Mathf.PI * 2f;
            for (var ring = 0; ring < 4; ring++)
            {
                var radius = 2.0f + ring * 1.42f;
                var samples = 10 + ring * 8;
                for (var index = 0; index < samples; index++)
                {
                    var angle = startAngle + index * Mathf.PI * 2f / samples;
                    var x = CenterX + Mathf.Cos(angle) * radius;
                    var y = CenterY + Mathf.Sin(angle) * radius;
                    AddVectorSlot(slots, occupiedCells, profile, random, x, y, angle, true);
                }
            }
        }

        private static void AddTerminalRows(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var startX = random.NextDouble() >= 0.5d ? 1 : 2;
            var startY = random.NextDouble() >= 0.5d ? 1 : 2;
            var rowIndex = 0;
            for (var y = startY; y <= MaxCellY; y += 2)
            {
                var leftToRight = rowIndex % 2 == 0;
                if (leftToRight)
                {
                    for (var x = startX; x <= MaxCellX; x += 2)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, y, GridDirection.Right);
                    }
                }
                else
                {
                    for (var x = MaxCellX - ((MaxCellX - startX) % 2); x >= MinCell; x -= 2)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, y, GridDirection.Left);
                    }
                }

                rowIndex++;
            }
        }

        private static void AddDiagonalBands(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var directionOffset = random.Next(0, 2);
            for (var diagonal = 2; diagonal <= MaxCellX + MaxCellY; diagonal += 2)
            {
                var bandDirection = (diagonal / 2 + directionOffset) % 2 == 0
                    ? GridDirection.Right
                    : GridDirection.Up;
                for (var x = MinCell; x <= MaxCellX; x++)
                {
                    var y = diagonal - x;
                    if (y < MinCell || y > MaxCellY)
                    {
                        continue;
                    }

                    AddCardinalSlot(slots, occupiedCells, profile, random, x, y, bandDirection);
                }
            }
        }

        private static void AddSpiral(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var clockwise = random.NextDouble() >= 0.5d;
            for (var layer = 1; layer <= 5; layer++)
            {
                var left = layer;
                var right = BoardLayoutConfig.GridColumns - 1 - layer;
                var bottom = layer;
                var top = BoardLayoutConfig.GridRows - 1 - layer;
                if (left > right || bottom > top)
                {
                    break;
                }

                if (clockwise)
                {
                    for (var x = left; x <= right; x++)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, bottom, GridDirection.Right);
                    }

                    for (var y = bottom + 1; y <= top; y++)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, right, y, GridDirection.Up);
                    }

                    for (var x = right - 1; x >= left; x--)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, top, GridDirection.Left);
                    }

                    for (var y = top - 1; y > bottom; y--)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, left, y, GridDirection.Down);
                    }
                }
                else
                {
                    for (var y = bottom; y <= top; y++)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, left, y, GridDirection.Up);
                    }

                    for (var x = left + 1; x <= right; x++)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, top, GridDirection.Right);
                    }

                    for (var y = top - 1; y >= bottom; y--)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, right, y, GridDirection.Down);
                    }

                    for (var x = right - 1; x > left; x--)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, bottom, GridDirection.Left);
                    }
                }
            }
        }

        private static void AddSplitClusters(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var centers = random.NextDouble() >= 0.5d
                ? new[] { new Vector2(4.0f, 4.0f), new Vector2(9.0f, 9.0f), new Vector2(4.0f, 10.0f) }
                : new[] { new Vector2(9.0f, 4.0f), new Vector2(4.0f, 9.0f), new Vector2(9.0f, 10.0f) };

            for (var centerIndex = 0; centerIndex < centers.Length; centerIndex++)
            {
                var center = centers[centerIndex];
                for (var ring = 0; ring < 3; ring++)
                {
                    var radius = 1.0f + ring * 0.95f;
                    var samples = 7 + ring * 5;
                    for (var index = 0; index < samples; index++)
                    {
                        var angle = index * Mathf.PI * 2f / samples + centerIndex * 0.35f;
                        var x = center.x + Mathf.Cos(angle) * radius;
                        var y = center.y + Mathf.Sin(angle) * radius;
                        AddVectorSlot(slots, occupiedCells, profile, random, x, y, angle, true);
                    }
                }
            }
        }

        private static void AddChevron(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var apexY = random.NextDouble() >= 0.5d ? 3f : 10f;
            var opensUp = apexY < CenterY;
            for (var step = 0; step < 10; step++)
            {
                var y = opensUp ? apexY + step : apexY - step;
                var spread = 0.65f + step * 0.55f;
                AddVectorSlot(
                    slots,
                    occupiedCells,
                    profile,
                    random,
                    CenterX - spread,
                    y,
                    opensUp ? -Mathf.PI * 0.22f : Mathf.PI * 1.22f,
                    true);
                AddVectorSlot(
                    slots,
                    occupiedCells,
                    profile,
                    random,
                    CenterX + spread,
                    y,
                    opensUp ? Mathf.PI * 0.22f : Mathf.PI * 0.78f,
                    true);

                if (step % 2 == 0)
                {
                    AddCardinalSlot(
                        slots,
                        occupiedCells,
                        profile,
                        random,
                        Mathf.RoundToInt(CenterX),
                        Mathf.RoundToInt(y),
                        opensUp ? GridDirection.Up : GridDirection.Down);
                }
            }
        }

        private static void AddDenseBlock(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var left = random.NextDouble() >= 0.5d ? 1 : 2;
            var right = MaxCellX - (random.NextDouble() >= 0.5d ? 0 : 1);
            var bottom = random.NextDouble() >= 0.5d ? 1 : 2;
            var top = MaxCellY - (random.NextDouble() >= 0.5d ? 0 : 1);
            for (var y = bottom; y <= top; y++)
            {
                var leftToRight = (y + random.Next(0, 2)) % 2 == 0;
                if (leftToRight)
                {
                    for (var x = left; x <= right; x++)
                    {
                        AddDenseBlockSlot(slots, occupiedCells, profile, random, x, y);
                    }
                }
                else
                {
                    for (var x = right; x >= left; x--)
                    {
                        AddDenseBlockSlot(slots, occupiedCells, profile, random, x, y);
                    }
                }
            }
        }

        private static void AddDenseBlockSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int x,
            int y)
        {
            var mode = Mathf.Abs(x * 17 + y * 31 + random.Next(0, 3)) % 4;
            var direction = mode == 0
                ? GridDirection.Right
                : mode == 1
                    ? GridDirection.Left
                    : mode == 2
                        ? GridDirection.Up
                        : GridDirection.Down;
            AddCardinalSlot(slots, occupiedCells, profile, random, x, y, direction);
        }

        private static void AddDiamondCross(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var center = new Vector2Int(Mathf.RoundToInt(CenterX), Mathf.RoundToInt(CenterY));
            for (var radius = 0; radius <= 6; radius++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var dy = radius - Mathf.Abs(dx);
                    AddDiamondSlot(slots, occupiedCells, profile, random, center.x + dx, center.y + dy, dx, dy);
                    if (dy != 0)
                    {
                        AddDiamondSlot(slots, occupiedCells, profile, random, center.x + dx, center.y - dy, dx, -dy);
                    }
                }
            }

            for (var offset = -5; offset <= 5; offset++)
            {
                AddCardinalSlot(slots, occupiedCells, profile, random, center.x + offset, center.y, offset < 0 ? GridDirection.Right : GridDirection.Left);
                AddCardinalSlot(slots, occupiedCells, profile, random, center.x, center.y + offset, offset < 0 ? GridDirection.Up : GridDirection.Down);
            }
        }

        private static void AddDiamondSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int x,
            int y,
            int dx,
            int dy)
        {
            AddCardinalSlot(slots, occupiedCells, profile, random, x, y, DirectionFromDelta(dx, dy));
        }

        private static void AddMazeRows(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var connectorA = random.Next(3, 6);
            var connectorB = random.Next(8, 11);
            for (var y = MinCell; y <= MaxCellY; y++)
            {
                var leftToRight = y % 2 == 0;
                var start = leftToRight ? MinCell : MaxCellX;
                var end = leftToRight ? MaxCellX : MinCell;
                var step = leftToRight ? 1 : -1;
                for (var x = start; (leftToRight ? x <= end : x >= end); x += step)
                {
                    if ((y % 3 == 1) && x > MinCell + 1 && x < MaxCellX - 1 && x % 4 == 0)
                    {
                        continue;
                    }

                    var direction = leftToRight ? GridDirection.Right : GridDirection.Left;
                    if (x == connectorA || x == connectorB)
                    {
                        direction = y < CenterY ? GridDirection.Up : GridDirection.Down;
                    }

                    AddCardinalSlot(slots, occupiedCells, profile, random, x, y, direction);
                }
            }
        }

        private static void AddPackedClusters(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var centers = new[]
            {
                new Vector2Int(3, 3),
                new Vector2Int(10, 3),
                new Vector2Int(3, 10),
                new Vector2Int(10, 10),
                new Vector2Int(6, 6),
                new Vector2Int(7, 7)
            };

            for (var centerIndex = 0; centerIndex < centers.Length; centerIndex++)
            {
                var center = centers[centerIndex];
                var radius = centerIndex < 4 ? 2 : 3;
                for (var dx = -radius; dx <= radius; dx++)
                {
                    for (var dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius + 1)
                        {
                            continue;
                        }

                        AddCardinalSlot(
                            slots,
                            occupiedCells,
                            profile,
                            random,
                            center.x + dx,
                            center.y + dy,
                            DirectionFromDelta(dx, dy));
                    }
                }
            }
        }

        private static void AddDenseJam(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var clockwise = random.NextDouble() >= 0.5d;
            var centerOffsetX = Mathf.Lerp(-0.30f, 0.30f, (float)random.NextDouble());
            var centerOffsetY = Mathf.Lerp(-0.20f, 0.20f, (float)random.NextDouble());
            var center = new Vector2(CenterX + centerOffsetX, CenterY + centerOffsetY);

            AddDenseJamRing(slots, occupiedCells, profile, random, center, 5.30f, 4.70f, 36, clockwise);
            AddDenseJamRing(slots, occupiedCells, profile, random, center, 4.10f, 3.45f, 28, !clockwise);
            AddDenseJamRing(slots, occupiedCells, profile, random, center, 2.75f, 2.20f, 18, clockwise);
            AddDenseJamCrossLocks(slots, occupiedCells, profile, random, center, clockwise);
            AddDenseJamCornerPacks(slots, occupiedCells, profile, random);
        }

        private static void AddDenseJamRing(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            Vector2 center,
            float radiusX,
            float radiusY,
            int samples,
            bool clockwise)
        {
            var startAngle = (float)random.NextDouble() * Mathf.PI * 2f;
            var tangentSign = clockwise ? -1f : 1f;
            for (var index = 0; index < samples; index++)
            {
                var angle = startAngle + index * Mathf.PI * 2f / samples;
                var x = center.x + Mathf.Cos(angle) * radiusX;
                var y = center.y + Mathf.Sin(angle) * radiusY;
                var tangentAngle = angle + tangentSign * Mathf.PI * 0.5f;
                AddVectorSlot(slots, occupiedCells, profile, random, x, y, tangentAngle, true);
            }
        }

        private static void AddDenseJamCrossLocks(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            Vector2 center,
            bool clockwise)
        {
            var centerCell = new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
            var horizontalY = centerCell.y + (clockwise ? -1 : 1);
            var verticalX = centerCell.x + (clockwise ? 1 : -1);

            for (var x = MinCell + 1; x <= MaxCellX - 1; x++)
            {
                if (Mathf.Abs(x - centerCell.x) <= 1)
                {
                    continue;
                }

                AddCardinalSlot(
                    slots,
                    occupiedCells,
                    profile,
                    random,
                    x,
                    horizontalY,
                    x < centerCell.x ? GridDirection.Right : GridDirection.Left);
            }

            for (var y = MinCell + 1; y <= MaxCellY - 1; y++)
            {
                if (Mathf.Abs(y - centerCell.y) <= 1)
                {
                    continue;
                }

                AddCardinalSlot(
                    slots,
                    occupiedCells,
                    profile,
                    random,
                    verticalX,
                    y,
                    y < centerCell.y ? GridDirection.Up : GridDirection.Down);
            }
        }

        private static void AddDenseJamCornerPacks(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            AddDenseJamCornerPack(slots, occupiedCells, profile, random, 2, 2, 1, 1);
            AddDenseJamCornerPack(slots, occupiedCells, profile, random, MaxCellX - 1, 2, -1, 1);
            AddDenseJamCornerPack(slots, occupiedCells, profile, random, 2, MaxCellY - 1, 1, -1);
            AddDenseJamCornerPack(slots, occupiedCells, profile, random, MaxCellX - 1, MaxCellY - 1, -1, -1);
        }

        private static void AddDenseJamCornerPack(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int anchorX,
            int anchorY,
            int stepX,
            int stepY)
        {
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    if (row == 2 && column == 2)
                    {
                        continue;
                    }

                    var x = anchorX + column * stepX;
                    var y = anchorY + row * stepY;
                    var direction = row % 2 == 0
                        ? (stepX > 0 ? GridDirection.Right : GridDirection.Left)
                        : (stepY > 0 ? GridDirection.Up : GridDirection.Down);
                    AddCardinalSlot(slots, occupiedCells, profile, random, x, y, direction);
                }
            }
        }

        private static void AddShowcaseRows(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var startsLeft = random.NextDouble() >= 0.5d;
            for (var band = 0; band < 2; band++)
            {
                var yStart = band == 0 ? 2 : 1;
                var xStart = band == 0 ? 1 : 2;
                for (var y = yStart; y <= MaxCellY; y += 2)
                {
                    var rowDirection = ((y + (startsLeft ? 0 : 1)) % 4) < 2 ? GridDirection.Right : GridDirection.Left;
                    if (rowDirection == GridDirection.Right)
                    {
                        for (var x = xStart; x <= MaxCellX; x += 3)
                        {
                            AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, y, rowDirection);
                        }
                    }
                    else
                    {
                        for (var x = MaxCellX - ((MaxCellX - xStart) % 3); x >= MinCell; x -= 3)
                        {
                            AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, y, rowDirection);
                        }
                    }
                }
            }

            for (var x = 3; x <= MaxCellX - 2; x += 4)
            {
                var direction = x < CenterX ? GridDirection.Up : GridDirection.Down;
                for (var y = 3; y <= MaxCellY - 2; y += 4)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, y, direction);
                }
            }
        }

        private static void AddShowcaseRing(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var clockwise = random.NextDouble() >= 0.5d;
            AddCleanPerimeter(slots, occupiedCells, profile, random, 2, 2, MaxCellX - 1, MaxCellY - 1, clockwise);
            AddCleanPerimeter(slots, occupiedCells, profile, random, 4, 4, MaxCellX - 3, MaxCellY - 3, !clockwise);

            var centerX = Mathf.RoundToInt(CenterX);
            var centerY = Mathf.RoundToInt(CenterY);
            AddCleanCardinalSlot(slots, occupiedCells, profile, random, centerX - 1, centerY, GridDirection.Right);
            AddCleanCardinalSlot(slots, occupiedCells, profile, random, centerX + 1, centerY, GridDirection.Left);
            AddCleanCardinalSlot(slots, occupiedCells, profile, random, centerX, centerY - 1, GridDirection.Up);
            AddCleanCardinalSlot(slots, occupiedCells, profile, random, centerX, centerY + 1, GridDirection.Down);

            for (var y = 3; y <= MaxCellY - 2; y += 3)
            {
                AddCleanCardinalSlot(slots, occupiedCells, profile, random, 1, y, GridDirection.Up);
                AddCleanCardinalSlot(slots, occupiedCells, profile, random, MaxCellX, y, GridDirection.Down);
            }
        }

        private static void AddShowcaseFrame(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var clockwise = random.NextDouble() >= 0.5d;
            AddCleanPerimeter(slots, occupiedCells, profile, random, 1, 1, MaxCellX, MaxCellY, clockwise);
            AddCleanPerimeter(slots, occupiedCells, profile, random, 3, 3, MaxCellX - 2, MaxCellY - 2, !clockwise);

            AddShowcaseCluster(slots, occupiedCells, profile, random, 4, 4, false);
            AddShowcaseCluster(slots, occupiedCells, profile, random, 9, 4, true);
            AddShowcaseCluster(slots, occupiedCells, profile, random, 4, 9, true);
            AddShowcaseCluster(slots, occupiedCells, profile, random, 9, 9, false);
        }

        private static void AddShowcaseQuads(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            AddShowcaseCluster(slots, occupiedCells, profile, random, 3, 3, false);
            AddShowcaseCluster(slots, occupiedCells, profile, random, 10, 3, true);
            AddShowcaseCluster(slots, occupiedCells, profile, random, 3, 10, true);
            AddShowcaseCluster(slots, occupiedCells, profile, random, 10, 10, false);

            for (var offset = -2; offset <= 2; offset++)
            {
                if (offset == 0)
                {
                    continue;
                }

                AddCleanCardinalSlot(
                    slots,
                    occupiedCells,
                    profile,
                    random,
                    Mathf.RoundToInt(CenterX) + offset,
                    Mathf.RoundToInt(CenterY),
                    offset < 0 ? GridDirection.Right : GridDirection.Left);
                AddCleanCardinalSlot(
                    slots,
                    occupiedCells,
                    profile,
                    random,
                    Mathf.RoundToInt(CenterX),
                    Mathf.RoundToInt(CenterY) + offset,
                    offset < 0 ? GridDirection.Up : GridDirection.Down);
            }

            AddCleanPerimeter(slots, occupiedCells, profile, random, 1, 1, MaxCellX, MaxCellY, random.NextDouble() >= 0.5d);
        }

        private static void AddShowcaseHeart(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var mirror = random.NextDouble() >= 0.5d;
            for (var y = MaxCellY; y >= MinCell; y--)
            {
                if (mirror)
                {
                    for (var x = MinCell; x <= MaxCellX; x++)
                    {
                        AddHeartSlot(slots, occupiedCells, profile, random, x, y);
                    }
                }
                else
                {
                    for (var x = MaxCellX; x >= MinCell; x--)
                    {
                        AddHeartSlot(slots, occupiedCells, profile, random, x, y);
                    }
                }
            }

            AddCleanPerimeter(slots, occupiedCells, profile, random, 1, 1, MaxCellX, MaxCellY, random.NextDouble() >= 0.5d);
        }

        private static void AddHeartSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int x,
            int y)
        {
            var normalizedX = (x - CenterX) / 5.7f;
            var normalizedY = (y - 5.5f) / 5.7f;
            var value = Mathf.Pow(normalizedX * normalizedX + normalizedY * normalizedY - 1f, 3f) -
                normalizedX * normalizedX * normalizedY * normalizedY * normalizedY;
            if (value > 0.06f)
            {
                return;
            }

            AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, y, DirectionAwayFromCenter(x, y));
        }

        private static void AddShowcaseCluster(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int centerX,
            int centerY,
            bool alternate)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > 3)
                    {
                        continue;
                    }

                    var horizontal = (Mathf.Abs(dx) >= Mathf.Abs(dy)) ^ alternate;
                    var direction = horizontal
                        ? (dx <= 0 ? GridDirection.Right : GridDirection.Left)
                        : (dy <= 0 ? GridDirection.Up : GridDirection.Down);
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, centerX + dx, centerY + dy, direction);
                }
            }
        }

        private static void AddCleanPerimeter(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int left,
            int bottom,
            int right,
            int top,
            bool clockwise)
        {
            left = Mathf.Clamp(left, MinCell, MaxCellX);
            right = Mathf.Clamp(right, left, MaxCellX);
            bottom = Mathf.Clamp(bottom, MinCell, MaxCellY);
            top = Mathf.Clamp(top, bottom, MaxCellY);

            if (clockwise)
            {
                for (var x = left; x <= right; x += 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, bottom, GridDirection.Right);
                }

                for (var y = bottom + 1; y <= top; y += 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, right, y, GridDirection.Up);
                }

                for (var x = right - 1; x >= left; x -= 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, top, GridDirection.Left);
                }

                for (var y = top - 1; y > bottom; y -= 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, left, y, GridDirection.Down);
                }
            }
            else
            {
                for (var y = bottom; y <= top; y += 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, left, y, GridDirection.Up);
                }

                for (var x = left + 1; x <= right; x += 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, top, GridDirection.Right);
                }

                for (var y = top - 1; y >= bottom; y -= 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, right, y, GridDirection.Down);
                }

                for (var x = right - 1; x > left; x -= 2)
                {
                    AddCleanCardinalSlot(slots, occupiedCells, profile, random, x, bottom, GridDirection.Left);
                }
            }
        }

        private static void AddFillerSlots(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random)
        {
            var directionOffset = random.Next(0, 2);
            for (var y = MinCell; y <= MaxCellY; y++)
            {
                var leftToRight = y % 2 == 0;
                if (leftToRight)
                {
                    for (var x = MinCell; x <= MaxCellX; x++)
                    {
                        AddCleanCardinalSlot(
                            slots,
                            occupiedCells,
                            profile,
                            random,
                            x,
                            y,
                            (y + directionOffset) % 2 == 0 ? GridDirection.Right : GridDirection.Up);
                    }
                }
                else
                {
                    for (var x = MaxCellX; x >= MinCell; x--)
                    {
                        AddCleanCardinalSlot(
                            slots,
                            occupiedCells,
                            profile,
                            random,
                            x,
                            y,
                            (y + directionOffset) % 2 == 0 ? GridDirection.Left : GridDirection.Down);
                    }
                }
            }
        }

        private static void AddVectorSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            float x,
            float y,
            float directionAngle,
            bool preserveSubCellOffset)
        {
            var cell = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            var offset = preserveSubCellOffset
                ? new Vector2(x - cell.x, y - cell.y)
                : Vector2.zero;
            var yaw = Mathf.Atan2(Mathf.Cos(directionAngle), Mathf.Sin(directionAngle)) * Mathf.Rad2Deg;
            var direction = DirectionFromYaw(yaw);
            var baseYaw = GridDirectionUtility.ToYawDegrees(direction);
            var angleOffset = Mathf.DeltaAngle(baseYaw, yaw);
            AddSlot(slots, occupiedCells, profile, random, cell, direction, angleOffset, offset);
        }

        private static void AddCardinalSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int x,
            int y,
            GridDirection direction)
        {
            AddSlot(slots, occupiedCells, profile, random, new Vector2Int(x, y), direction, 0f, Vector2.zero);
        }

        private static void AddCleanCardinalSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            int x,
            int y,
            GridDirection direction)
        {
            AddSlot(slots, occupiedCells, profile, random, new Vector2Int(x, y), direction, 0f, Vector2.zero, false);
        }

        private static void AddSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            Vector2Int cell,
            GridDirection direction,
            float angleOffset,
            Vector2 positionOffset)
        {
            AddSlot(slots, occupiedCells, profile, random, cell, direction, angleOffset, positionOffset, true);
        }

        private static void AddSlot(
            List<VehicleLayoutSlot> slots,
            HashSet<int> occupiedCells,
            LevelDifficultyProfile profile,
            System.Random random,
            Vector2Int cell,
            GridDirection direction,
            float angleOffset,
            Vector2 positionOffset,
            bool allowJitter)
        {
            if (cell.x < MinCell || cell.x > MaxCellX || cell.y < MinCell || cell.y > MaxCellY)
            {
                return;
            }

            var key = cell.x * BoardLayoutConfig.GridRows + cell.y;
            if (!occupiedCells.Add(key))
            {
                return;
            }

            var cleanAngleLimit = Mathf.Lerp(2f, 7f, profile.ParkingTension);
            var jitterAngle = allowJitter
                ? Mathf.Lerp(-cleanAngleLimit, cleanAngleLimit, (float)random.NextDouble()) * 0.12f
                : 0f;
            var cleanOffsetLimit = allowJitter ? Mathf.Lerp(0.003f, 0.018f, profile.ParkingTension) : 0f;
            var jitterOffset = new Vector2(
                Mathf.Lerp(-cleanOffsetLimit, cleanOffsetLimit, (float)random.NextDouble()),
                Mathf.Lerp(-cleanOffsetLimit, cleanOffsetLimit, (float)random.NextDouble()));

            slots.Add(new VehicleLayoutSlot(
                cell,
                direction,
                Mathf.Clamp(angleOffset, -cleanAngleLimit, cleanAngleLimit) + jitterAngle,
                ClampOffset(positionOffset + jitterOffset)));
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

        private static Vector2 ClampOffset(Vector2 offset)
        {
            const float limit = 0.14f;
            return new Vector2(
                Mathf.Clamp(offset.x, -limit, limit),
                Mathf.Clamp(offset.y, -limit, limit));
        }

        private static GridDirection DirectionAwayFromCenter(int x, int y)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                return dx < 0f ? GridDirection.Left : GridDirection.Right;
            }

            return dy < 0f ? GridDirection.Down : GridDirection.Up;
        }

        private static GridDirection DirectionFromDelta(int dx, int dy)
        {
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                return dx < 0 ? GridDirection.Right : GridDirection.Left;
            }

            if (dy != 0)
            {
                return dy < 0 ? GridDirection.Up : GridDirection.Down;
            }

            return GridDirection.Up;
        }
    }
}
