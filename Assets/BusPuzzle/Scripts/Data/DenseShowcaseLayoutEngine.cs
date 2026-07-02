using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal static class DenseShowcaseLayoutEngine
    {
        private const int SubcellDivisions = 4;
        private const float SubcellStepCells = 1f / SubcellDivisions;
        private const float FirstSubcellOffsetCells = -0.5f + SubcellStepCells * 0.5f;
        private const float MaxDenseOffsetComponentCells = 0.44f;
        private const float MaxDenseAngleOffsetDegrees = 35f;
        private const float DensePlacementPaddingCells = 0.0f;
        private const float DenseBoundaryPaddingCells = 0.05f;
        private const float CenterX = (BoardLayoutConfig.GridColumns - 1) * 0.5f;
        private const float CenterY = (BoardLayoutConfig.GridRows - 1) * 0.5f;

        private static readonly float[] CurvedLatticeAngles = { 30f, 60f, 120f, 150f, -30f, -60f };
        private static readonly float[] LinearLatticeAngles = { 12f, -12f, 30f, -30f, 60f, 120f };
        private static readonly float[] GeometricLatticeAngles = { 30f, 60f, 120f, 150f };

        private static readonly PuzzleColor[] FallbackColors =
        {
            PuzzleColor.Red,
            PuzzleColor.Orange,
            PuzzleColor.Yellow,
            PuzzleColor.Green,
            PuzzleColor.Blue,
            PuzzleColor.Purple,
            PuzzleColor.Pink,
            PuzzleColor.SkyBlue,
            PuzzleColor.Lime
        };

        public static bool TryBuildVehicles(
            LevelDifficultyProfile profile,
            System.Random random,
            int targetVehicleCount,
            int layoutVariantIndex,
            IReadOnlyList<PuzzleColor> colors,
            out List<BusDefinition> vehicles)
        {
            vehicles = new List<BusDefinition>();
            if (!VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                return false;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            random = random ?? new System.Random(0);
            targetVehicleCount = Mathf.Clamp(targetVehicleCount, 1, 80);
            var libraryId = (VehicleShapeLibraryId)libraryIndex;

            var slots = VehicleLayoutPatternEngine.CreateSlots(profile, random, targetVehicleCount, layoutVariantIndex);
            if (slots.Count == 0)
            {
                return false;
            }

            vehicles = BuildVehiclePass(slots, libraryId, layoutVariantIndex, colors, targetVehicleCount, false);
            if (IsUsableDenseSet(vehicles, targetVehicleCount, libraryId))
            {
                return true;
            }

            vehicles = BuildVehiclePass(slots, libraryId, layoutVariantIndex, colors, targetVehicleCount, true);
            if (IsUsableDenseSet(vehicles, targetVehicleCount, libraryId))
            {
                return true;
            }

            vehicles.Clear();
            return false;
        }

        private static List<BusDefinition> BuildVehiclePass(
            IReadOnlyList<VehicleLayoutSlot> slots,
            VehicleShapeLibraryId libraryId,
            int layoutVariantIndex,
            IReadOnlyList<PuzzleColor> colors,
            int targetVehicleCount,
            bool forceEscapeLanes)
        {
            var vehicles = new List<BusDefinition>(targetVehicleCount);
            var poseCandidates = new List<DensePoseCandidate>(256);
            var sizeCandidates = new List<BusSize>(3);
            for (var slotIndex = 0; slotIndex < slots.Count && vehicles.Count < targetVehicleCount; slotIndex++)
            {
                var slot = slots[slotIndex];
                var color = PickDenseColor(colors, slot, vehicles.Count, layoutVariantIndex);
                BuildPoseCandidates(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes, poseCandidates);
                BuildSizeCandidates(slot, libraryId, slotIndex, vehicles.Count, targetVehicleCount, sizeCandidates);

                var placed = false;
                for (var sizeIndex = 0; sizeIndex < sizeCandidates.Count && !placed; sizeIndex++)
                {
                    for (var poseIndex = 0; poseIndex < poseCandidates.Count; poseIndex++)
                    {
                        var pose = poseCandidates[poseIndex];
                        var candidate = new BusDefinition(
                            color,
                            sizeCandidates[sizeIndex],
                            pose.Direction,
                            slot.GridPosition,
                            pose.AngleOffsetDegrees,
                            pose.PositionOffsetCells);
                        if (!IsDensePlaceable(candidate, vehicles))
                        {
                            continue;
                        }

                        vehicles.Add(candidate);
                        placed = true;
                        break;
                    }
                }
            }

            return vehicles;
        }

        private static bool IsUsableDenseSet(
            IReadOnlyList<BusDefinition> vehicles,
            int targetVehicleCount,
            VehicleShapeLibraryId libraryId)
        {
            var minimumUsefulCount = Mathf.Min(
                targetVehicleCount,
                Mathf.Max(8, Mathf.RoundToInt(targetVehicleCount * GetMinimumUsefulRatio(libraryId))));
            if (vehicles.Count < minimumUsefulCount)
            {
                return false;
            }

            return StageSolutionAnalyzer.Analyze(vehicles, null, 1, 4096).IsSolvable;
        }

        private static float GetMinimumUsefulRatio(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                    return 0.58f;
                case VehicleShapeLibraryId.Star:
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                    return 0.64f;
                default:
                    return 0.70f;
            }
        }

        private static PuzzleColor PickDenseColor(
            IReadOnlyList<PuzzleColor> colors,
            VehicleLayoutSlot slot,
            int vehicleIndex,
            int layoutVariantIndex)
        {
            var palette = colors != null && colors.Count > 0 ? colors : FallbackColors;
            if (slot.HasPreferredColor)
            {
                return slot.PreferredColor;
            }

            var seed = Mathf.Abs(layoutVariantIndex) +
                slot.GridPosition.x * 17 +
                slot.GridPosition.y * 29 +
                vehicleIndex * 7;
            return palette[seed % palette.Count];
        }

        private static void BuildPoseCandidates(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            bool forceEscapeLanes,
            List<DensePoseCandidate> candidates)
        {
            candidates.Clear();

            var yawCandidates = new List<float>(16);
            var offsetCandidates = new List<Vector2>(48);
            var outwardDirection = GetOutwardDirection(slot.GridPosition);
            var outwardYaw = DirectionToYaw(outwardDirection);
            var slotYaw = NormalizeYaw(DirectionToYaw(slot.Direction) + slot.AngleOffsetDegrees);
            var prefersEscapePose = ShouldPreferEscapePose(
                slot,
                libraryId,
                slotIndex,
                layoutVariantIndex,
                forceEscapeLanes);
            if (prefersEscapePose)
            {
                AddYaw(yawCandidates, outwardYaw);
            }

            AddYaw(yawCandidates, slotYaw);
            if (!prefersEscapePose)
            {
                AddYaw(yawCandidates, outwardYaw);
            }

            var secondaryDirection = Mathf.Abs(slot.GridPosition.x - CenterX) > Mathf.Abs(slot.GridPosition.y - CenterY)
                ? GridDirection.Up
                : GridDirection.Right;
            AddYaw(yawCandidates, DirectionToYaw(secondaryDirection));
            AddYaw(yawCandidates, DirectionToYaw(Opposite(secondaryDirection)));
            AddYaw(yawCandidates, DirectionToYaw(RotateClockwise(outwardDirection)));
            AddYaw(yawCandidates, DirectionToYaw(RotateCounterClockwise(outwardDirection)));
            AddLibraryYawCandidates(libraryId, slotYaw, outwardYaw, yawCandidates);

            BuildOffsetCandidates(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes, offsetCandidates);
            for (var yawIndex = 0; yawIndex < yawCandidates.Count; yawIndex++)
            {
                for (var offsetIndex = 0; offsetIndex < offsetCandidates.Count; offsetIndex++)
                {
                    AddPoseFromYaw(candidates, yawCandidates[yawIndex], offsetCandidates[offsetIndex]);
                }
            }
        }

        private static void AddLibraryYawCandidates(
            VehicleShapeLibraryId libraryId,
            float slotYaw,
            float outwardYaw,
            List<float> yawCandidates)
        {
            if (IsCurvedLibrary(libraryId))
            {
                AddYaw(yawCandidates, slotYaw + 12f);
                AddYaw(yawCandidates, slotYaw - 12f);
                AddYaw(yawCandidates, slotYaw + 24f);
                AddYaw(yawCandidates, slotYaw - 24f);
                AddYawArray(yawCandidates, CurvedLatticeAngles);
                return;
            }

            if (IsLinearLibrary(libraryId))
            {
                AddYaw(yawCandidates, slotYaw + 12f);
                AddYaw(yawCandidates, slotYaw - 12f);
                AddYaw(yawCandidates, outwardYaw + 30f);
                AddYaw(yawCandidates, outwardYaw - 30f);
                AddYawArray(yawCandidates, LinearLatticeAngles);
                return;
            }

            if (IsClosedGeometricLibrary(libraryId))
            {
                AddYaw(yawCandidates, outwardYaw + 30f);
                AddYaw(yawCandidates, outwardYaw - 30f);
                AddYawArray(yawCandidates, GeometricLatticeAngles);
            }
        }

        private static void BuildOffsetCandidates(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            bool forceEscapeLanes,
            List<Vector2> offsets)
        {
            offsets.Clear();
            var maxOffsetComponentCells = GetMaxOffsetComponentCells(slot, libraryId);
            AppendRotatedLayerOffsets(slot, libraryId, slotIndex, layoutVariantIndex, maxOffsetComponentCells, offsets);
            AppendSubcellOffsets(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes, maxOffsetComponentCells, offsets);
            AddOffset(offsets, slot.PositionOffsetCells, maxOffsetComponentCells);
            AddOffset(offsets, Vector2.zero, maxOffsetComponentCells);
        }

        private static void AppendRotatedLayerOffsets(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            float maxOffsetComponentCells,
            List<Vector2> offsets)
        {
            var angles = IsCurvedLibrary(libraryId)
                ? CurvedLatticeAngles
                : IsLinearLibrary(libraryId)
                    ? LinearLatticeAngles
                    : GeometricLatticeAngles;
            for (var angleIndex = 0; angleIndex < angles.Length; angleIndex++)
            {
                var phaseX = PickLayerPhase(layoutVariantIndex, slotIndex, angleIndex, 0);
                var phaseY = PickLayerPhase(layoutVariantIndex, slotIndex, angleIndex, 1);
                if (TryCreateRotatedLayerOffset(
                    slot.GridPosition,
                    angles[angleIndex],
                    phaseX,
                    phaseY,
                    maxOffsetComponentCells,
                    out var offset))
                {
                    AddOffset(offsets, offset + slot.PositionOffsetCells, maxOffsetComponentCells);
                }
            }
        }

        private static void AppendSubcellOffsets(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            bool forceEscapeLanes,
            float maxOffsetComponentCells,
            List<Vector2> offsets)
        {
            var outward = GetOutwardVector(slot.GridPosition);
            var start = Mathf.Abs(layoutVariantIndex + slotIndex * 13 + slot.GridPosition.x * 7 + slot.GridPosition.y * 5) %
                (SubcellDivisions * SubcellDivisions);
            AppendSubcellOffsets(
                offsets,
                slot,
                outward,
                start,
                true,
                forceEscapeLanes || IsClosedGeometricLibrary(libraryId),
                maxOffsetComponentCells);
            AppendSubcellOffsets(offsets, slot, outward, start, false, false, maxOffsetComponentCells);
        }

        private static void AppendSubcellOffsets(
            List<Vector2> offsets,
            VehicleLayoutSlot slot,
            Vector2 outward,
            int start,
            bool outwardBiasedPass,
            bool requireOutwardBias,
            float maxOffsetComponentCells)
        {
            var count = SubcellDivisions * SubcellDivisions;
            for (var index = 0; index < count; index++)
            {
                var subcellIndex = (start + index * 5) % count;
                var subcellX = subcellIndex % SubcellDivisions;
                var subcellY = subcellIndex / SubcellDivisions;
                var offset = new Vector2(
                    FirstSubcellOffsetCells + subcellX * SubcellStepCells,
                    FirstSubcellOffsetCells + subcellY * SubcellStepCells);
                var outwardDot = Vector2.Dot(offset, outward);
                if (outwardBiasedPass && outwardDot < 0.03f)
                {
                    continue;
                }

                if (!outwardBiasedPass && requireOutwardBias && outwardDot >= 0.03f)
                {
                    continue;
                }

                AddOffset(offsets, offset + slot.PositionOffsetCells, maxOffsetComponentCells);
            }
        }

        private static bool TryCreateRotatedLayerOffset(
            Vector2Int cell,
            float angleDegrees,
            float phaseX,
            float phaseY,
            float maxOffsetComponentCells,
            out Vector2 offset)
        {
            var centered = new Vector2(cell.x - CenterX, cell.y - CenterY);
            var local = Rotate(centered, -angleDegrees);
            var snappedLocal = new Vector2(
                SnapToSubcell(local.x, phaseX),
                SnapToSubcell(local.y, phaseY));
            var snappedWorld = Rotate(snappedLocal, angleDegrees) + new Vector2(CenterX, CenterY);
            offset = snappedWorld - new Vector2(cell.x, cell.y);
            return Mathf.Abs(offset.x) <= maxOffsetComponentCells &&
                Mathf.Abs(offset.y) <= maxOffsetComponentCells;
        }

        private static float PickLayerPhase(int layoutVariantIndex, int slotIndex, int angleIndex, int axisIndex)
        {
            var hash = Mathf.Abs(layoutVariantIndex) + slotIndex * 17 + angleIndex * 23 + axisIndex * 31;
            return hash % 2 == 0 ? SubcellStepCells * 0.5f : -SubcellStepCells * 0.5f;
        }

        private static float SnapToSubcell(float value, float phase)
        {
            return Mathf.Round((value - phase) / SubcellStepCells) * SubcellStepCells + phase;
        }

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(
                value.x * cos - value.y * sin,
                value.x * sin + value.y * cos);
        }

        private static void AddOffset(List<Vector2> offsets, Vector2 offset, float maxOffsetComponentCells)
        {
            maxOffsetComponentCells = Mathf.Min(MaxDenseOffsetComponentCells, Mathf.Max(0f, maxOffsetComponentCells));
            if (Mathf.Abs(offset.x) > maxOffsetComponentCells ||
                Mathf.Abs(offset.y) > maxOffsetComponentCells)
            {
                return;
            }

            for (var index = 0; index < offsets.Count; index++)
            {
                if ((offsets[index] - offset).sqrMagnitude <= 0.0001f)
                {
                    return;
                }
            }

            offsets.Add(offset);
        }

        private static float GetMaxOffsetComponentCells(VehicleLayoutSlot slot, VehicleShapeLibraryId libraryId)
        {
            if (IsFeatureSlot(libraryId, slot))
            {
                return 0.16f;
            }

            if (slot.ShapeRole != VehicleShapeCellRole.Fill)
            {
                return 0.24f;
            }

            return MaxDenseOffsetComponentCells;
        }

        private static void BuildSizeCandidates(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int vehicleIndex,
            int targetVehicleCount,
            List<BusSize> sizes)
        {
            sizes.Clear();
            var preferred = PickDenseSize(slot, libraryId, slotIndex, vehicleIndex, targetVehicleCount);
            AddSizeCandidate(sizes, preferred);
            if (preferred == BusSize.Large)
            {
                AddSizeCandidate(sizes, BusSize.Medium);
            }

            if (preferred != BusSize.Small)
            {
                AddSizeCandidate(sizes, BusSize.Small);
            }
        }

        private static BusSize PickDenseSize(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int vehicleIndex,
            int targetVehicleCount)
        {
            if (IsFeatureSlot(libraryId, slot))
            {
                return BusSize.Small;
            }

            var hash = Mathf.Abs((slot.GridPosition.x + 3) * 73856093 ^
                (slot.GridPosition.y + 7) * 19349663 ^
                (slotIndex + 11) * 83492791 ^
                (vehicleIndex + 5) * 297121507);
            if (slot.ShapeRole != VehicleShapeCellRole.Fill)
            {
                return AllowsMediumOutlineVehicle(libraryId, slot) && targetVehicleCount >= 30 && hash % 5 == 0
                    ? BusSize.Medium
                    : BusSize.Small;
            }

            if (targetVehicleCount >= 38 && hash % 9 == 0)
            {
                return BusSize.Large;
            }

            return targetVehicleCount >= 30 && hash % 3 == 0
                ? BusSize.Medium
                : BusSize.Small;
        }

        private static void AddSizeCandidate(List<BusSize> sizes, BusSize size)
        {
            for (var index = 0; index < sizes.Count; index++)
            {
                if (sizes[index] == size)
                {
                    return;
                }
            }

            sizes.Add(size);
        }

        private static bool AllowsMediumOutlineVehicle(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            if (IsFeatureSlot(libraryId, slot))
            {
                return false;
            }

            switch (libraryId)
            {
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Triangle:
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                case VehicleShapeLibraryId.Shield:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFeatureSlot(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            if (slot.ShapeRole == VehicleShapeCellRole.Accent)
            {
                return true;
            }

            var x = slot.GridPosition.x;
            var y = slot.GridPosition.y;
            var dx = x - CenterX;
            var dy = y - CenterY;
            var absX = Mathf.Abs(dx);
            var absY = Mathf.Abs(dy);
            switch (libraryId)
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
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Fan:
                    return dx * dx + dy * dy >= 17.5f;
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

        private static void AddYawArray(List<float> yawCandidates, IReadOnlyList<float> yaws)
        {
            for (var index = 0; index < yaws.Count; index++)
            {
                AddYaw(yawCandidates, yaws[index]);
            }
        }

        private static void AddYaw(List<float> yawCandidates, float yaw)
        {
            yaw = NormalizeYaw(yaw);
            for (var index = 0; index < yawCandidates.Count; index++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(yawCandidates[index], yaw)) <= 0.01f)
                {
                    return;
                }
            }

            yawCandidates.Add(yaw);
        }

        private static void AddPoseFromYaw(
            List<DensePoseCandidate> candidates,
            float yawDegrees,
            Vector2 positionOffsetCells)
        {
            var direction = DirectionFromYaw(yawDegrees);
            var angleOffset = Mathf.DeltaAngle(DirectionToYaw(direction), yawDegrees);
            AddPose(candidates, direction, angleOffset, positionOffsetCells);
        }

        private static bool ShouldPreferEscapePose(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            bool forceEscapeLanes)
        {
            var hash = Mathf.Abs(layoutVariantIndex) + slotIndex * 11 + slot.GridPosition.x * 5 + slot.GridPosition.y * 3;
            var nearOuterBand = slot.GridPosition.x <= 2 ||
                slot.GridPosition.x >= BoardLayoutConfig.GridColumns - 3 ||
                slot.GridPosition.y <= 2 ||
                slot.GridPosition.y >= BoardLayoutConfig.GridRows - 3;
            if (forceEscapeLanes)
            {
                return nearOuterBand || hash % 2 == 0;
            }

            if (IsClosedGeometricLibrary(libraryId) && nearOuterBand && slot.ShapeRole != VehicleShapeCellRole.Fill)
            {
                return true;
            }

            if (slot.ShapeRole == VehicleShapeCellRole.Fill)
            {
                return hash % 3 == 0;
            }

            return nearOuterBand ? hash % 4 == 0 : hash % 7 == 0;
        }

        private static bool IsClosedGeometricLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Triangle:
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                case VehicleShapeLibraryId.Shield:
                    return true;
                default:
                    return false;
            }
        }

        private static void AddPose(
            List<DensePoseCandidate> candidates,
            GridDirection direction,
            float angleOffsetDegrees,
            Vector2 positionOffsetCells)
        {
            angleOffsetDegrees = Mathf.Clamp(angleOffsetDegrees, -MaxDenseAngleOffsetDegrees, MaxDenseAngleOffsetDegrees);
            for (var index = 0; index < candidates.Count; index++)
            {
                if (candidates[index].Direction == direction &&
                    Mathf.Abs(candidates[index].AngleOffsetDegrees - angleOffsetDegrees) <= 0.01f &&
                    (candidates[index].PositionOffsetCells - positionOffsetCells).sqrMagnitude <= 0.0001f)
                {
                    return;
                }
            }

            candidates.Add(new DensePoseCandidate(direction, angleOffsetDegrees, positionOffsetCells));
        }

        private static bool IsDensePlaceable(BusDefinition candidate, IReadOnlyList<BusDefinition> placedVehicles)
        {
            if (!BoardLayoutConfig.IsInsideGrid(candidate.GridPosition) || IsOutsideDenseBounds(candidate))
            {
                return false;
            }

            var candidateFootprint = BoardLayoutConfig.GetVehicleFootprintCells(candidate);
            var candidateVisualFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(candidate);
            for (var index = 0; index < placedVehicles.Count; index++)
            {
                var placed = placedVehicles[index];
                if (candidateFootprint.Overlaps(BoardLayoutConfig.GetVehicleFootprintCells(placed)))
                {
                    return false;
                }

                var placedVisualFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(placed);
                if (candidateVisualFootprint.Overlaps(placedVisualFootprint) ||
                    candidateVisualFootprint.IsWithinPadding(placedVisualFootprint, DensePlacementPaddingCells))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsCurvedLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                case VehicleShapeLibraryId.Star:
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Smile:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsLinearLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsOutsideDenseBounds(BusDefinition vehicle)
        {
            var footprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(vehicle);
            return footprint.ProjectMin(Vector2.right) < -DenseBoundaryPaddingCells ||
                footprint.ProjectMax(Vector2.right) > BoardLayoutConfig.GridColumns - 1f + DenseBoundaryPaddingCells ||
                footprint.ProjectMin(Vector2.up) < -DenseBoundaryPaddingCells ||
                footprint.ProjectMax(Vector2.up) > BoardLayoutConfig.GridRows - 1f + DenseBoundaryPaddingCells;
        }

        private static GridDirection GetOutwardDirection(Vector2Int cell)
        {
            var dx = cell.x - CenterX;
            var dy = cell.y - CenterY;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                return dx >= 0f ? GridDirection.Right : GridDirection.Left;
            }

            return dy >= 0f ? GridDirection.Up : GridDirection.Down;
        }

        private static Vector2 GetOutwardVector(Vector2Int cell)
        {
            var vector = new Vector2(cell.x - CenterX, cell.y - CenterY);
            return vector.sqrMagnitude > 0.0001f ? vector.normalized : Vector2.up;
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

        private static float DirectionToYaw(GridDirection direction)
        {
            return GridDirectionUtility.ToYawDegrees(direction);
        }

        private static float NormalizeYaw(float yaw)
        {
            return Mathf.Repeat(yaw + 360f, 360f);
        }

        private static GridDirection RotateClockwise(GridDirection direction)
        {
            return (GridDirection)(((int)direction + 1) % 4);
        }

        private static GridDirection RotateCounterClockwise(GridDirection direction)
        {
            return (GridDirection)(((int)direction + 3) % 4);
        }

        private static GridDirection Opposite(GridDirection direction)
        {
            return (GridDirection)(((int)direction + 2) % 4);
        }

        private readonly struct DensePoseCandidate
        {
            public readonly GridDirection Direction;
            public readonly float AngleOffsetDegrees;
            public readonly Vector2 PositionOffsetCells;

            public DensePoseCandidate(
                GridDirection direction,
                float angleOffsetDegrees,
                Vector2 positionOffsetCells)
            {
                Direction = direction;
                AngleOffsetDegrees = angleOffsetDegrees;
                PositionOffsetCells = positionOffsetCells;
            }
        }
    }
}
