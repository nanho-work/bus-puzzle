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
        PackedClusters
    }

    internal readonly struct VehicleLayoutSlot
    {
        public readonly Vector2Int GridPosition;
        public readonly GridDirection Direction;
        public readonly float AngleOffsetDegrees;
        public readonly Vector2 PositionOffsetCells;

        public VehicleLayoutSlot(
            Vector2Int gridPosition,
            GridDirection direction,
            float angleOffsetDegrees,
            Vector2 positionOffsetCells)
        {
            GridPosition = gridPosition;
            Direction = direction;
            AngleOffsetDegrees = angleOffsetDegrees;
            PositionOffsetCells = positionOffsetCells;
        }
    }

    internal static class VehicleLayoutPatternEngine
    {
        public const int AutoLayoutVariant = -1;
        private const int VariantsPerPattern = 20;
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
            VehicleLayoutPatternId.PackedClusters
        };

        private static readonly VehicleLayoutPatternId[] NormalPatterns =
        {
            VehicleLayoutPatternId.TerminalRows,
            VehicleLayoutPatternId.DiagonalBands,
            VehicleLayoutPatternId.Ring
        };

        private static readonly VehicleLayoutPatternId[] HardPatterns =
        {
            VehicleLayoutPatternId.TerminalRows,
            VehicleLayoutPatternId.DiagonalBands,
            VehicleLayoutPatternId.Spiral
        };

        private static readonly VehicleLayoutPatternId[] SuperHardPatterns =
        {
            VehicleLayoutPatternId.DiagonalBands,
            VehicleLayoutPatternId.Spiral,
            VehicleLayoutPatternId.TerminalRows
        };

        private static readonly VehicleLayoutPatternId[] MidPressurePatterns =
        {
            VehicleLayoutPatternId.TerminalRows,
            VehicleLayoutPatternId.DiagonalBands,
            VehicleLayoutPatternId.Spiral,
            VehicleLayoutPatternId.SplitClusters,
            VehicleLayoutPatternId.Chevron,
            VehicleLayoutPatternId.DenseBlock
        };

        private static readonly VehicleLayoutPatternId[] LatePressurePatterns =
        {
            VehicleLayoutPatternId.DiagonalBands,
            VehicleLayoutPatternId.Spiral,
            VehicleLayoutPatternId.SplitClusters,
            VehicleLayoutPatternId.Chevron,
            VehicleLayoutPatternId.DenseBlock,
            VehicleLayoutPatternId.DiamondCross,
            VehicleLayoutPatternId.MazeRows,
            VehicleLayoutPatternId.PackedClusters
        };

        public static int UniqueLayoutVariantCount => StagePatternPool.Length * VariantsPerPattern;

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
            targetVehicleCount = Mathf.Clamp(targetVehicleCount, 1, 50);

            var slots = new List<VehicleLayoutSlot>(targetVehicleCount * 3);
            var occupiedCells = new HashSet<int>();
            var pattern = PickPattern(profile, random, layoutVariantIndex);
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
            var pressure = Mathf.Clamp01(profile.ParkingTension * 0.65f + profile.StationPressure * 0.35f);
            if (profile.Difficulty == LevelDifficulty.SuperHard && pressure >= 0.78f)
            {
                return LatePressurePatterns;
            }

            if (profile.Difficulty != LevelDifficulty.Normal && pressure >= 0.64f)
            {
                return MidPressurePatterns;
            }

            if (profile.Difficulty == LevelDifficulty.SuperHard)
            {
                return SuperHardPatterns;
            }

            return profile.Difficulty == LevelDifficulty.Hard ? HardPatterns : NormalPatterns;
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
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, y, (y + directionOffset) % 2 == 0 ? GridDirection.Right : GridDirection.Up);
                    }
                }
                else
                {
                    for (var x = MaxCellX; x >= MinCell; x--)
                    {
                        AddCardinalSlot(slots, occupiedCells, profile, random, x, y, (y + directionOffset) % 2 == 0 ? GridDirection.Left : GridDirection.Down);
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
            var jitterAngle = Mathf.Lerp(-cleanAngleLimit, cleanAngleLimit, (float)random.NextDouble()) * 0.18f;
            var cleanOffsetLimit = Mathf.Lerp(0.005f, 0.035f, profile.ParkingTension);
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
            const float limit = 0.22f;
            return new Vector2(
                Mathf.Clamp(offset.x, -limit, limit),
                Mathf.Clamp(offset.y, -limit, limit));
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
