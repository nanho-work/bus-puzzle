using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BusPuzzle
{
    internal static class DenseShowcaseLayoutEngine
    {
        private const int SubcellDivisions = 4;
        private const float SubcellStepCells = 1f / SubcellDivisions;
        private const float FirstSubcellOffsetCells = -0.5f + SubcellStepCells * 0.5f;
        private const float MaxDenseOffsetComponentCells = 0.44f;
        private const float MaxDenseAngleOffsetDegrees = 45f;
        private const float VisualPreviewCollisionScale = 1.0f;
        private const float DensePlacementPaddingCells = 0.0f;
        private const float VisualPreviewPlacementPaddingCells = 0.035f;
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
            out List<BusDefinition> vehicles,
            bool useVisualPreviewQuality = false,
            int placementProbeIndex = 0,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            vehicles = new List<BusDefinition>();
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            targetVehicleCount = Mathf.Clamp(targetVehicleCount, 1, 80);
            if (!TryResolveDenseLibraryId(
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                    out var libraryId))
            {
                return false;
            }

            random = random ?? new System.Random(0);
            var useTemplateVisualPlacementQuality =
                useVisualPreviewQuality ||
                libraryId == VehicleShapeLibraryId.Heart ||
                libraryId == VehicleShapeLibraryId.HeartArrow;
            var placementLayoutVariantIndex =
                libraryId == VehicleShapeLibraryId.Heart
                    ? VehicleLayoutPatternEngine.GetShapeLibraryVariantIndex(
                        (int)VehicleShapeLibraryId.Heart,
                        0)
                    : layoutVariantIndex;
            var poseLayoutVariantIndex =
                libraryId == VehicleShapeLibraryId.Heart
                    ? VehicleLayoutPatternEngine.GetShapeLibraryVariantIndex(
                        (int)VehicleShapeLibraryId.Heart,
                        Mathf.Max(0, placementProbeIndex))
                    : layoutVariantIndex;

            var slots = VehicleLayoutPatternEngine.CreateSlots(
                profile,
                random,
                targetVehicleCount,
                placementLayoutVariantIndex);
            if (slots.Count == 0)
            {
                return false;
            }

            VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                profile,
                targetVehicleCount,
                placementLayoutVariantIndex,
                out var placementShapeDefinition);
            VehicleShapeTemplateCatalog.TryGetQualityTemplate(
                placementShapeDefinition,
                out var placementQualityTemplate);

            var minimumDenseVehicleCount = ShapeLibraryVehicleCoverage.GetMinimumVehicleCount(profile, layoutVariantIndex);
            var denseTargetVehicleCount =
                libraryId == VehicleShapeLibraryId.Heart ||
                libraryId == VehicleShapeLibraryId.HeartArrow
                    ? Mathf.Min(
                        targetVehicleCount,
                        ShapeLibraryVehicleCoverage.HeartSilhouetteVehicleCapacity)
                    : targetVehicleCount;
            vehicles = BuildVehiclePass(
                slots,
                libraryId,
                profile,
                poseLayoutVariantIndex,
                colors,
                denseTargetVehicleCount,
                minimumDenseVehicleCount,
                placementShapeDefinition,
                placementQualityTemplate,
                useTemplateVisualPlacementQuality,
                false,
                false,
                cancellationToken);
            if (IsAcceptableDenseSet(profile, vehicles, targetVehicleCount, layoutVariantIndex, libraryId, useVisualPreviewQuality))
            {
                return true;
            }

            vehicles = BuildVehiclePass(
                slots,
                libraryId,
                profile,
                poseLayoutVariantIndex,
                colors,
                denseTargetVehicleCount,
                minimumDenseVehicleCount,
                placementShapeDefinition,
                placementQualityTemplate,
                useTemplateVisualPlacementQuality,
                true,
                false,
                cancellationToken);
            if (IsAcceptableDenseSet(profile, vehicles, targetVehicleCount, layoutVariantIndex, libraryId, useVisualPreviewQuality))
            {
                return true;
            }

            vehicles = BuildVehiclePass(
                slots,
                libraryId,
                profile,
                poseLayoutVariantIndex,
                colors,
                denseTargetVehicleCount,
                minimumDenseVehicleCount,
                placementShapeDefinition,
                placementQualityTemplate,
                useTemplateVisualPlacementQuality,
                true,
                true,
                cancellationToken);
            if (IsAcceptableDenseSet(profile, vehicles, targetVehicleCount, layoutVariantIndex, libraryId, useVisualPreviewQuality))
            {
                return true;
            }

            vehicles.Clear();
            return false;
        }

        private static bool TryResolveDenseLibraryId(
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int layoutVariantIndex,
            out VehicleShapeLibraryId libraryId)
        {
            if (VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                libraryId = (VehicleShapeLibraryId)libraryIndex;
                return true;
            }

            if (VehicleLayoutPatternEngine.TryCreateTemplateQualityShapeDefinition(
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                    out var definition) &&
                definition.LibraryId != VehicleShapeLibraryId.None)
            {
                libraryId = definition.LibraryId;
                return true;
            }

            libraryId = VehicleShapeLibraryId.None;
            return false;
        }

        private static bool IsAcceptableDenseSet(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> vehicles,
            int targetVehicleCount,
            int layoutVariantIndex,
            VehicleShapeLibraryId libraryId,
            bool useVisualPreviewQuality)
        {
            if (useVisualPreviewQuality ||
                libraryId == VehicleShapeLibraryId.Heart ||
                libraryId == VehicleShapeLibraryId.HeartArrow)
            {
                // Heart candidates are intentionally dense at this stage. LevelGenerator
                // applies the mirror-pair opening pass before enforcing the final
                // silhouette and full exit-order gates. Running those gates here would
                // judge the unfinished candidate and force a generic-layout fallback.
                return HasMinimumDenseVehicleCount(profile, vehicles, layoutVariantIndex);
            }

            if (!IsQualityDenseSet(
                    profile,
                    vehicles,
                    targetVehicleCount,
                    layoutVariantIndex,
                    libraryId))
            {
                return false;
            }

            return !RequiresBuildTimeGreedyExitProof(libraryId) ||
                HasGreedyExitOrder(vehicles);
        }

        private static bool HasMinimumDenseVehicleCount(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> vehicles,
            int layoutVariantIndex)
        {
            if (vehicles == null)
            {
                return false;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            return vehicles.Count >= ShapeLibraryVehicleCoverage.GetMinimumVehicleCount(profile, layoutVariantIndex);
        }

        private static bool RequiresBuildTimeGreedyExitProof(VehicleShapeLibraryId libraryId)
        {
            // Star and Heart layouts need a geometry-preserving opening pass after the
            // dense silhouette exists. Heart openings are applied atomically to mirror
            // pairs by LevelGenerator before the normal playable-order check.
            return libraryId != VehicleShapeLibraryId.Star &&
                libraryId != VehicleShapeLibraryId.Heart &&
                libraryId != VehicleShapeLibraryId.HeartArrow;
        }

        private static List<BusDefinition> BuildVehiclePass(
            IReadOnlyList<VehicleLayoutSlot> slots,
            VehicleShapeLibraryId libraryId,
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<PuzzleColor> colors,
            int targetVehicleCount,
            int minimumDenseVehicleCount,
            VehicleShapeLayoutDefinition shapeDefinition,
            IVehicleShapeTemplate qualityTemplate,
            bool useVisualPreviewQuality,
            bool forceEscapeLanes,
            bool forceRadialEscapes,
            CancellationToken cancellationToken)
        {
            var vehicles = new List<BusDefinition>(targetVehicleCount);
            var poseCandidates = new List<DensePoseCandidate>(256);
            var sizeCandidates = new List<BusSize>(3);
            for (var slotIndex = 0; slotIndex < slots.Count && vehicles.Count < targetVehicleCount; slotIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var slot = slots[slotIndex];
                var preservesHeartMirrorPairs =
                    libraryId == VehicleShapeLibraryId.Heart ||
                    libraryId == VehicleShapeLibraryId.HeartArrow;
                if (preservesHeartMirrorPairs)
                {
                    if (slotIndex + 1 < slots.Count &&
                        AreMirrorXPairSlots(slot, slots[slotIndex + 1]))
                    {
                        if (vehicles.Count + 1 < targetVehicleCount)
                        {
                            TryPlaceMirroredHeartPair(
                                slot,
                                slots[slotIndex + 1],
                                libraryId,
                                profile,
                                layoutVariantIndex,
                                colors,
                                targetVehicleCount,
                                minimumDenseVehicleCount,
                                shapeDefinition,
                                qualityTemplate,
                                useVisualPreviewQuality,
                                forceEscapeLanes,
                                forceRadialEscapes,
                                slotIndex,
                                vehicles,
                                poseCandidates,
                                sizeCandidates,
                                cancellationToken);
                        }

                        slotIndex++;
                    }

                    // Heart silhouettes are authored and opened as mirror pairs. Never
                    // use an unpaired filler slot merely to satisfy an odd vehicle budget.
                    continue;
                }

                var color = PickDenseColor(colors, slot, vehicles.Count, layoutVariantIndex);
                BuildPoseCandidates(
                    slot,
                    libraryId,
                    slotIndex,
                    layoutVariantIndex,
                    forceEscapeLanes,
                    forceRadialEscapes,
                    poseCandidates);
                BuildSizeCandidates(
                    slot,
                    libraryId,
                    profile,
                    slotIndex,
                    vehicles.Count,
                    targetVehicleCount,
                    minimumDenseVehicleCount,
                    layoutVariantIndex,
                    useVisualPreviewQuality,
                    sizeCandidates);

                var placed = false;
                for (var sizeIndex = 0; sizeIndex < sizeCandidates.Count && !placed; sizeIndex++)
                {
                    for (var poseIndex = 0; poseIndex < poseCandidates.Count; poseIndex++)
                    {
                        if ((poseIndex & 15) == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        var pose = poseCandidates[poseIndex];
                        var candidate = new BusDefinition(
                            color,
                            sizeCandidates[sizeIndex],
                            pose.Direction,
                            slot.GridPosition,
                            pose.AngleOffsetDegrees,
                            pose.PositionOffsetCells);
                        if (!IsDensePlaceable(
                                candidate,
                                vehicles,
                                shapeDefinition,
                                qualityTemplate,
                                useVisualPreviewQuality))
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

        private static bool TryPlaceMirroredHeartPair(
            VehicleLayoutSlot leftSlot,
            VehicleLayoutSlot rightSlot,
            VehicleShapeLibraryId libraryId,
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<PuzzleColor> colors,
            int targetVehicleCount,
            int minimumDenseVehicleCount,
            VehicleShapeLayoutDefinition shapeDefinition,
            IVehicleShapeTemplate qualityTemplate,
            bool useVisualPreviewQuality,
            bool forceEscapeLanes,
            bool forceRadialEscapes,
            int slotIndex,
            List<BusDefinition> vehicles,
            List<DensePoseCandidate> poseCandidates,
            List<BusSize> sizeCandidates,
            CancellationToken cancellationToken)
        {
            BuildPoseCandidates(
                leftSlot,
                libraryId,
                slotIndex,
                layoutVariantIndex,
                forceEscapeLanes,
                forceRadialEscapes,
                poseCandidates);
            BuildSizeCandidates(
                leftSlot,
                libraryId,
                profile,
                slotIndex,
                vehicles.Count,
                // A high stage budget must add more vehicles, not make the early
                // silhouette vehicles longer. Letting the normal >=34/42 size
                // pressure run here causes a 42-car Heart to collapse to roughly
                // twenty oversized buses and spill well outside the contour.
                // Heart pairs stay small until the requested silhouette density is
                // secured; later pairs may still use the normal visual size mix.
                vehicles.Count < minimumDenseVehicleCount
                    ? Mathf.Min(targetVehicleCount, 33)
                    : targetVehicleCount,
                minimumDenseVehicleCount,
                layoutVariantIndex,
                useVisualPreviewQuality,
                sizeCandidates);

            var leftColor = PickDenseColor(colors, leftSlot, vehicles.Count, layoutVariantIndex);
            var rightColor = PickDenseColor(colors, rightSlot, vehicles.Count + 1, layoutVariantIndex);
            for (var sizeIndex = 0; sizeIndex < sizeCandidates.Count; sizeIndex++)
            {
                var size = sizeCandidates[sizeIndex];
                for (var poseIndex = 0; poseIndex < poseCandidates.Count; poseIndex++)
                {
                    if ((poseIndex & 15) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var leftPose = poseCandidates[poseIndex];
                    var left = new BusDefinition(
                        leftColor,
                        size,
                        leftPose.Direction,
                        leftSlot.GridPosition,
                        leftPose.AngleOffsetDegrees,
                        leftPose.PositionOffsetCells);
                    if (!IsDensePlaceable(
                            left,
                            vehicles,
                            shapeDefinition,
                            qualityTemplate,
                            useVisualPreviewQuality))
                    {
                        continue;
                    }

                    var leftYaw = DirectionToYaw(leftPose.Direction) + leftPose.AngleOffsetDegrees;
                    var rightYaw = NormalizeYaw(-leftYaw);
                    var rightDirection = DirectionFromYaw(rightYaw);
                    var rightAngleOffset = Mathf.DeltaAngle(DirectionToYaw(rightDirection), rightYaw);
                    var right = new BusDefinition(
                        rightColor,
                        size,
                        rightDirection,
                        rightSlot.GridPosition,
                        rightAngleOffset,
                        new Vector2(-leftPose.PositionOffsetCells.x, leftPose.PositionOffsetCells.y));

                    vehicles.Add(left);
                    if (IsDensePlaceable(
                            right,
                            vehicles,
                            shapeDefinition,
                            qualityTemplate,
                            useVisualPreviewQuality))
                    {
                        vehicles.Add(right);
                        return true;
                    }

                    vehicles.RemoveAt(vehicles.Count - 1);
                }
            }

            return false;
        }

        private static bool AreMirrorXPairSlots(VehicleLayoutSlot left, VehicleLayoutSlot right)
        {
            return left.GridPosition.y == right.GridPosition.y &&
                left.GridPosition.x + right.GridPosition.x == BoardLayoutConfig.GridColumns - 1 &&
                left.ShapeKind == right.ShapeKind &&
                left.ShapeRole == right.ShapeRole;
        }

        private static bool IsQualityDenseSet(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> vehicles,
            int targetVehicleCount,
            int layoutVariantIndex,
            VehicleShapeLibraryId libraryId)
        {
            profile = profile ?? LevelDifficultyProfile.CreateCustom(
                LevelDifficulty.Normal,
                targetVehicleCount,
                6,
                0.5f,
                0.5f,
                false);
            var minimumUsefulCount = ShapeLibraryVehicleCoverage.GetMinimumVehicleCount(
                profile,
                layoutVariantIndex);
            if (vehicles.Count < minimumUsefulCount)
            {
                return false;
            }

            var usesPairAwareOpeningPass =
                libraryId == VehicleShapeLibraryId.Heart ||
                libraryId == VehicleShapeLibraryId.HeartArrow;
            if (!ShapeLibraryLayoutQuality.IsSatisfied(
                    profile,
                    layoutVariantIndex,
                    vehicles,
                    !usesPairAwareOpeningPass))
            {
                return false;
            }

            return true;
        }

        private static bool HasGreedyExitOrder(IReadOnlyList<BusDefinition> vehicles)
        {
            return LevelVehicleExitPlanner.TryFindExitOrder(vehicles, out var exitOrder, out _) &&
                exitOrder.Count == vehicles.Count;
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
            bool forceRadialEscapes,
            List<DensePoseCandidate> candidates)
        {
            candidates.Clear();

            var yawCandidates = new List<float>(16);
            var offsetCandidates = new List<Vector2>(48);
            var outwardDirection = GetOutwardDirection(slot.GridPosition);
            var outwardYaw = DirectionToYaw(outwardDirection);
            var slotYaw = NormalizeYaw(DirectionToYaw(slot.Direction) + slot.AngleOffsetDegrees);
            if (IsHeartLowerSpineSlot(libraryId, slot))
            {
                // The tip and its bridge are a short vertical chain. Authoring both
                // rows with vertical poses and opposite subcell separation keeps the
                // pointed bottom while leaving a collision-safe connection to the body.
                AddYaw(yawCandidates, DirectionToYaw(GridDirection.Down));
                AddYaw(yawCandidates, DirectionToYaw(GridDirection.Up));
                BuildOffsetCandidates(
                    slot,
                    libraryId,
                    slotIndex,
                    layoutVariantIndex,
                    forceEscapeLanes,
                    offsetCandidates);
                for (var yawIndex = 0; yawIndex < yawCandidates.Count; yawIndex++)
                {
                    for (var offsetIndex = 0; offsetIndex < offsetCandidates.Count; offsetIndex++)
                    {
                        AddPoseFromYaw(candidates, yawCandidates[yawIndex], offsetCandidates[offsetIndex]);
                    }
                }

                return;
            }

            if (IsHeartNotchGuardSlot(libraryId, slot))
            {
                // Keep the two inner shoulders vertical and shifted away from center.
                // A diagonal vehicle here visually bridges the authored background notch.
                AddYaw(yawCandidates, DirectionToYaw(GridDirection.Up));
                AddYaw(yawCandidates, DirectionToYaw(GridDirection.Down));
                BuildOffsetCandidates(
                    slot,
                    libraryId,
                    slotIndex,
                    layoutVariantIndex,
                    forceEscapeLanes,
                    offsetCandidates);
                for (var yawIndex = 0; yawIndex < yawCandidates.Count; yawIndex++)
                {
                    for (var offsetIndex = 0; offsetIndex < offsetCandidates.Count; offsetIndex++)
                    {
                        AddPoseFromYaw(candidates, yawCandidates[yawIndex], offsetCandidates[offsetIndex]);
                    }
                }

                return;
            }

            if (ShouldPreserveStarLinePose(libraryId, slot))
            {
                AddYaw(yawCandidates, slotYaw);
                AddYaw(yawCandidates, slotYaw + 2f);
                AddYaw(yawCandidates, slotYaw - 2f);
                BuildOffsetCandidates(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes, offsetCandidates);
                for (var yawIndex = 0; yawIndex < yawCandidates.Count; yawIndex++)
                {
                    for (var offsetIndex = 0; offsetIndex < offsetCandidates.Count; offsetIndex++)
                    {
                        AddPoseFromYaw(candidates, yawCandidates[yawIndex], offsetCandidates[offsetIndex]);
                    }
                }

                return;
            }

            if (forceRadialEscapes &&
                (!ShouldLockShapeYaw(libraryId, slot) ||
                    IsRadialPetalLibrary(libraryId) ||
                    libraryId == VehicleShapeLibraryId.Clover))
            {
                AddYaw(yawCandidates, outwardYaw);
                AddYaw(yawCandidates, outwardYaw + 12f);
                AddYaw(yawCandidates, outwardYaw - 12f);
                AddYaw(yawCandidates, slotYaw);
                BuildOffsetCandidates(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes, offsetCandidates);
                for (var yawIndex = 0; yawIndex < yawCandidates.Count; yawIndex++)
                {
                    for (var offsetIndex = 0; offsetIndex < offsetCandidates.Count; offsetIndex++)
                    {
                        AddPoseFromYaw(candidates, yawCandidates[yawIndex], offsetCandidates[offsetIndex]);
                    }
                }

                return;
            }

            var prefersEscapePose = ShouldPreferEscapePose(
                slot,
                libraryId,
                slotIndex,
                layoutVariantIndex,
                forceEscapeLanes);
            if (ShouldLockShapeYaw(libraryId, slot))
            {
                AddLockedShapeYawCandidates(
                    slot,
                    libraryId,
                    slotIndex,
                    layoutVariantIndex,
                    forceEscapeLanes,
                    prefersEscapePose,
                    slotYaw,
                    outwardYaw,
                    yawCandidates);
            }
            else
            {
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
            }

            BuildOffsetCandidates(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes, offsetCandidates);
            for (var yawIndex = 0; yawIndex < yawCandidates.Count; yawIndex++)
            {
                for (var offsetIndex = 0; offsetIndex < offsetCandidates.Count; offsetIndex++)
                {
                    AddPoseFromYaw(candidates, yawCandidates[yawIndex], offsetCandidates[offsetIndex]);
                }
            }
        }

        private static void AddLockedShapeYawCandidates(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            bool forceEscapeLanes,
            bool prefersEscapePose,
            float slotYaw,
            float outwardYaw,
            List<float> yawCandidates)
        {
            var allowEscapeYaw = ShouldAllowShapeEscapeYaw(
                slot,
                libraryId,
                slotIndex,
                layoutVariantIndex,
                forceEscapeLanes);
            if (prefersEscapePose && allowEscapeYaw)
            {
                AddYaw(yawCandidates, outwardYaw);
                AddYaw(yawCandidates, outwardYaw + 12f);
                AddYaw(yawCandidates, outwardYaw - 12f);
            }

            var reverseYaw = slotYaw + 180f;
            if (ShouldPreferPathBlockingYaw(slot, libraryId, slotIndex, layoutVariantIndex))
            {
                AddYaw(yawCandidates, reverseYaw);
                AddYaw(yawCandidates, reverseYaw + 10f);
                AddYaw(yawCandidates, reverseYaw - 10f);
            }

            AddYaw(yawCandidates, slotYaw);
            var jitter = GetLockedShapeYawJitter(slot, libraryId);
            if (jitter > 0.01f)
            {
                AddYaw(yawCandidates, slotYaw + jitter);
                AddYaw(yawCandidates, slotYaw - jitter);
                if (forceEscapeLanes)
                {
                    AddYaw(yawCandidates, slotYaw + 16f);
                    AddYaw(yawCandidates, slotYaw - 16f);
                    AddYaw(yawCandidates, slotYaw + 24f);
                    AddYaw(yawCandidates, slotYaw - 24f);
                }
            }

            if (ShouldAllowReverseShapeYaw(slot, libraryId))
            {
                AddYaw(yawCandidates, reverseYaw);
                if (jitter > 0.01f)
                {
                    AddYaw(yawCandidates, reverseYaw + jitter);
                    AddYaw(yawCandidates, reverseYaw - jitter);
                    if (forceEscapeLanes)
                    {
                        AddYaw(yawCandidates, reverseYaw + 16f);
                        AddYaw(yawCandidates, reverseYaw - 16f);
                        AddYaw(yawCandidates, reverseYaw + 24f);
                        AddYaw(yawCandidates, reverseYaw - 24f);
                    }
                }
            }

            if (!prefersEscapePose && allowEscapeYaw)
            {
                AddYaw(yawCandidates, outwardYaw);
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
            if (IsHeartBottomTipSlot(libraryId, slot))
            {
                // Author the final downward opening into the dense pose itself. This
                // leaves enough room for the first central bridge pair one row above;
                // otherwise that pair collides during dense placement and the later
                // opening pass cannot recover the disconnected tip.
                AddOffset(offsets, new Vector2(0f, -0.22f), MaxDenseOffsetComponentCells);
                AddOffset(offsets, new Vector2(0f, -0.34f), MaxDenseOffsetComponentCells);
                AddOffset(offsets, Vector2.zero, MaxDenseOffsetComponentCells);
                return;
            }

            if (IsHeartLowerBridgeSlot(libraryId, slot))
            {
                AddOffset(offsets, new Vector2(0f, 0.34f), MaxDenseOffsetComponentCells);
                AddOffset(offsets, new Vector2(0f, 0.22f), MaxDenseOffsetComponentCells);
                AddOffset(offsets, Vector2.zero, MaxDenseOffsetComponentCells);
                return;
            }

            if (IsHeartNotchGuardSlot(libraryId, slot))
            {
                var outwardX = slot.GridPosition.x < CenterX ? -1f : 1f;
                AddOffset(offsets, new Vector2(outwardX * 0.34f, 0f), MaxDenseOffsetComponentCells);
                AddOffset(offsets, new Vector2(outwardX * 0.22f, 0f), MaxDenseOffsetComponentCells);
                AddOffset(offsets, Vector2.zero, MaxDenseOffsetComponentCells);
                return;
            }

            var maxOffsetComponentCells = GetMaxOffsetComponentCells(slot, libraryId);
            var locksShapeLine = ShouldLockShapeYaw(libraryId, slot);
            if (ShouldPreserveStarLinePose(libraryId, slot))
            {
                AppendStarLineOffsets(slot, maxOffsetComponentCells, offsets);
                return;
            }

            if (locksShapeLine)
            {
                AddOffset(offsets, slot.PositionOffsetCells, maxOffsetComponentCells);
                AddOffset(offsets, Vector2.zero, maxOffsetComponentCells);
            }

            AppendRotatedLayerOffsets(slot, libraryId, slotIndex, layoutVariantIndex, maxOffsetComponentCells, offsets);
            AppendSubcellOffsets(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes, maxOffsetComponentCells, offsets);
            if (!locksShapeLine)
            {
                AddOffset(offsets, slot.PositionOffsetCells, maxOffsetComponentCells);
                AddOffset(offsets, Vector2.zero, maxOffsetComponentCells);
            }
        }

        private static void AppendStarLineOffsets(
            VehicleLayoutSlot slot,
            float maxOffsetComponentCells,
            List<Vector2> offsets)
        {
            AddOffset(offsets, slot.PositionOffsetCells, maxOffsetComponentCells);
            AddOffset(offsets, Vector2.zero, maxOffsetComponentCells);

            var yawRadians = (DirectionToYaw(slot.Direction) + slot.AngleOffsetDegrees) * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(yawRadians), Mathf.Cos(yawRadians));
            var right = new Vector2(forward.y, -forward.x);
            var baseOffset = slot.PositionOffsetCells;
            AddOffset(offsets, baseOffset + forward * 0.08f, maxOffsetComponentCells);
            AddOffset(offsets, baseOffset - forward * 0.08f, maxOffsetComponentCells);
            AddOffset(offsets, baseOffset + right * 0.04f, maxOffsetComponentCells);
            AddOffset(offsets, baseOffset - right * 0.04f, maxOffsetComponentCells);
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
            if (ShouldPreserveStarLinePose(libraryId, slot))
            {
                return 0.12f;
            }

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
            LevelDifficultyProfile profile,
            int slotIndex,
            int vehicleIndex,
            int targetVehicleCount,
            int minimumDenseVehicleCount,
            int layoutVariantIndex,
            bool useVisualPreviewQuality,
            List<BusSize> sizes)
        {
            sizes.Clear();
            if (useVisualPreviewQuality &&
                (libraryId == VehicleShapeLibraryId.Heart ||
                 libraryId == VehicleShapeLibraryId.HeartArrow) &&
                (slot.ShapeRole != VehicleShapeCellRole.Fill ||
                 IsFeatureSlot(libraryId, slot)))
            {
                // Keep the notch, tip, and lobe contour crisp. Medium/large vehicles are
                // reserved for the deep fill where their longer OBB cannot blur features.
                AddSizeCandidate(sizes, BusSize.Small);
                return;
            }

            var preferred = PickDenseSize(slot, libraryId, profile, slotIndex, vehicleIndex, targetVehicleCount);
            if (useVisualPreviewQuality)
            {
                var hash = Mathf.Abs(layoutVariantIndex + slotIndex * 17 + vehicleIndex * 23 + slot.GridPosition.x * 5 + slot.GridPosition.y * 7);
                if (UsesStarSizeMixPreview(libraryId, layoutVariantIndex))
                {
                    if (AllowsStarSizeMixLargeVehicle(slot, hash) &&
                        targetVehicleCount >= 34 &&
                        hash % 4 != 1)
                    {
                        AddSizeCandidate(sizes, BusSize.Large);
                    }

                    if (slot.ShapeRole != VehicleShapeCellRole.Accent &&
                        AllowsMediumVehicle(libraryId, slot) &&
                        targetVehicleCount >= 34)
                    {
                        AddSizeCandidate(sizes, BusSize.Medium);
                    }

                    if (preferred != BusSize.Small)
                    {
                        AddSizeCandidate(sizes, preferred);
                    }

                    AddSizeCandidate(sizes, BusSize.Small);
                    if (AllowsStarSizeMixLargeVehicle(slot, hash))
                    {
                        AddSizeCandidate(sizes, BusSize.Large);
                    }

                    return;
                }

                if (AllowsMediumVehicle(libraryId, slot) &&
                    targetVehicleCount >= 34 &&
                    hash % 3 == 0)
                {
                    AddSizeCandidate(sizes, BusSize.Medium);
                }

                if (ShouldPrioritizeVisualPreviewLargeVehicle(libraryId, slot, targetVehicleCount, hash))
                {
                    AddSizeCandidate(sizes, BusSize.Large);
                }

                if (preferred != BusSize.Small)
                {
                    AddSizeCandidate(sizes, preferred);
                }

                AddSizeCandidate(sizes, BusSize.Small);
                if (AllowsMediumVehicle(libraryId, slot) && targetVehicleCount >= 34)
                {
                    AddSizeCandidate(sizes, BusSize.Medium);
                }

                if (libraryId == VehicleShapeLibraryId.Star &&
                    slot.ShapeRole == VehicleShapeCellRole.Fill &&
                    targetVehicleCount >= 34)
                {
                    AddSizeCandidate(sizes, BusSize.Large);
                }

                return;
            }

            if (vehicleIndex < minimumDenseVehicleCount && ShouldProtectSilhouetteDensity(libraryId, slot))
            {
                AddSizeCandidate(sizes, BusSize.Small);
                if (targetVehicleCount >= 42 &&
                    vehicleIndex >= Mathf.RoundToInt(minimumDenseVehicleCount * 0.84f) &&
                    AllowsMediumVehicle(libraryId, slot))
                {
                    AddSizeCandidate(sizes, BusSize.Medium);
                }

                return;
            }

            if (vehicleIndex < minimumDenseVehicleCount && !AllowsMediumVehicle(libraryId, slot))
            {
                AddSizeCandidate(sizes, BusSize.Small);
                if (vehicleIndex >= Mathf.RoundToInt(minimumDenseVehicleCount * 0.72f))
                {
                    if (preferred == BusSize.Large)
                    {
                        AddSizeCandidate(sizes, BusSize.Medium);
                    }

                    AddSizeCandidate(sizes, preferred);
                }

                return;
            }

            AddSizeCandidate(sizes, preferred);
            if (preferred == BusSize.Small && AllowsMediumVehicle(libraryId, slot) && targetVehicleCount >= 34)
            {
                AddSizeCandidate(sizes, BusSize.Medium);
            }

            if (preferred == BusSize.Large)
            {
                AddSizeCandidate(sizes, BusSize.Medium);
            }
            else if (preferred == BusSize.Medium && AllowsLargeVehicle(libraryId, slot) && targetVehicleCount >= 46)
            {
                AddSizeCandidate(sizes, BusSize.Large);
            }

            if (preferred != BusSize.Small)
            {
                AddSizeCandidate(sizes, BusSize.Small);
            }
        }

        private static BusSize PickDenseSize(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            LevelDifficultyProfile profile,
            int slotIndex,
            int vehicleIndex,
            int targetVehicleCount)
        {
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var hash = Mathf.Abs((slot.GridPosition.x + 3) * 73856093 ^
                (slot.GridPosition.y + 7) * 19349663 ^
                (slotIndex + 11) * 83492791 ^
                (vehicleIndex + 5) * 297121507);
            if (slot.ShapeRole == VehicleShapeCellRole.Accent)
            {
                if (AllowsMediumFeatureVehicle(libraryId, slot) &&
                    targetVehicleCount >= 46 &&
                    profile.Difficulty == LevelDifficulty.SuperHard &&
                    hash % 11 == 0)
                {
                    return BusSize.Medium;
                }

                return BusSize.Small;
            }

            if (slot.ShapeRole != VehicleShapeCellRole.Fill)
            {
                if (AllowsLargeOutlineVehicle(libraryId, slot) &&
                    targetVehicleCount >= 42 &&
                    GetDenseSizePressure(profile, targetVehicleCount) >= 0.48f &&
                    hash % GetLargeOutlineDivisor(profile) == 0)
                {
                    return BusSize.Large;
                }

                return AllowsMediumOutlineVehicle(libraryId, slot) &&
                    targetVehicleCount >= 34 &&
                    hash % GetMediumOutlineDivisor(profile) == 0
                    ? BusSize.Medium
                    : BusSize.Small;
            }

            if (targetVehicleCount >= 42 && hash % GetFillLargeDivisor(profile) == 0)
            {
                return BusSize.Large;
            }

            return targetVehicleCount >= 34 && hash % GetFillMediumDivisor(profile) == 0
                ? BusSize.Medium
                : BusSize.Small;
        }

        private static bool UsesStarSizeMixPreview(VehicleShapeLibraryId libraryId, int layoutVariantIndex)
        {
            return libraryId == VehicleShapeLibraryId.Star &&
                VehicleLayoutPatternEngine.TryGetShapeLibraryVariantSeed(layoutVariantIndex, out var variantSeed) &&
                variantSeed == StageGenerationPlanner.StarSizeMixVariantSeed;
        }

        private static bool AllowsStarSizeMixLargeVehicle(VehicleLayoutSlot slot, int hash)
        {
            if (slot.ShapeRole == VehicleShapeCellRole.Fill)
            {
                return true;
            }

            return slot.ShapeRole == VehicleShapeCellRole.Outline &&
                !IsFeatureSlot(VehicleShapeLibraryId.Star, slot) &&
                hash % 5 == 0;
        }

        private static bool ShouldPrioritizeVisualPreviewLargeVehicle(
            VehicleShapeLibraryId libraryId,
            VehicleLayoutSlot slot,
            int targetVehicleCount,
            int hash)
        {
            if (!AllowsLargeVehicle(libraryId, slot))
            {
                return false;
            }

            if (libraryId == VehicleShapeLibraryId.Star &&
                slot.ShapeRole == VehicleShapeCellRole.Fill &&
                targetVehicleCount >= 34)
            {
                return hash % 2 == 0;
            }

            return targetVehicleCount >= 46 && hash % 11 == 0;
        }

        private static float GetDenseSizePressure(LevelDifficultyProfile profile, int targetVehicleCount)
        {
            var countPressure = Mathf.InverseLerp(34f, 50f, targetVehicleCount);
            var parkingPressure = profile != null ? Mathf.Clamp01(profile.ParkingTension) : 0.5f;
            return Mathf.Clamp01(countPressure * 0.62f + parkingPressure * 0.38f);
        }

        private static int GetLargeOutlineDivisor(LevelDifficultyProfile profile)
        {
            switch (profile.Difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 5;
                case LevelDifficulty.Hard:
                    return 6;
                default:
                    return 9;
            }
        }

        private static int GetMediumOutlineDivisor(LevelDifficultyProfile profile)
        {
            switch (profile.Difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 2;
                case LevelDifficulty.Hard:
                    return 3;
                default:
                    return 4;
            }
        }

        private static int GetFillLargeDivisor(LevelDifficultyProfile profile)
        {
            switch (profile.Difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 4;
                case LevelDifficulty.Hard:
                    return 5;
                default:
                    return 7;
            }
        }

        private static int GetFillMediumDivisor(LevelDifficultyProfile profile)
        {
            return profile.Difficulty == LevelDifficulty.Normal ? 3 : 2;
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

        private static bool AllowsMediumVehicle(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            return slot.ShapeRole == VehicleShapeCellRole.Fill ||
                AllowsMediumOutlineVehicle(libraryId, slot) ||
                AllowsMediumFeatureVehicle(libraryId, slot);
        }

        private static bool AllowsLargeVehicle(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            if (!ShapeLibraryLayoutQuality.SupportsLargeVehicle(libraryId))
            {
                return false;
            }

            return slot.ShapeRole == VehicleShapeCellRole.Fill ||
                AllowsLargeOutlineVehicle(libraryId, slot);
        }

        private static bool AllowsMediumOutlineVehicle(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            if (slot.ShapeRole == VehicleShapeCellRole.Accent)
            {
                return false;
            }

            if (UsesStrictLaneOffsets(libraryId))
            {
                return false;
            }

            if ((libraryId == VehicleShapeLibraryId.Heart ||
                libraryId == VehicleShapeLibraryId.HeartArrow) &&
                !IsFeatureSlot(libraryId, slot))
            {
                return true;
            }

            switch (libraryId)
            {
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Star:
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
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
                case VehicleShapeLibraryId.Smile:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static bool AllowsLargeOutlineVehicle(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            if (slot.ShapeRole == VehicleShapeCellRole.Accent)
            {
                return false;
            }

            if (ShouldProtectSilhouetteDensity(libraryId, slot))
            {
                return false;
            }

            switch (libraryId)
            {
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                case VehicleShapeLibraryId.Star:
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
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
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static bool ShouldProtectSilhouetteDensity(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            if (slot.ShapeRole == VehicleShapeCellRole.Fill)
            {
                return false;
            }

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
                    return true;
                default:
                    return false;
            }
        }

        private static bool AllowsMediumFeatureVehicle(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            if (slot.ShapeRole == VehicleShapeCellRole.Accent)
            {
                return false;
            }

            switch (libraryId)
            {
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
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
                    return (y <= 3 && absX <= 1.7f) ||
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
                    return dx * dx + dy * dy >= 22.0f;
                case VehicleShapeLibraryId.Flower:
                    return dx * dx + dy * dy >= 18.5f;
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Fan:
                    return dx * dx + dy * dy >= 20.0f;
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

        private static bool IsHeartNotchGuardSlot(
            VehicleShapeLibraryId libraryId,
            VehicleLayoutSlot slot)
        {
            if (libraryId != VehicleShapeLibraryId.Heart &&
                libraryId != VehicleShapeLibraryId.HeartArrow)
            {
                return false;
            }

            return slot.GridPosition.y >= 9 &&
                Mathf.Abs(slot.GridPosition.x - CenterX) <= 1.7f;
        }

        private static bool IsHeartBottomTipSlot(
            VehicleShapeLibraryId libraryId,
            VehicleLayoutSlot slot)
        {
            return (libraryId == VehicleShapeLibraryId.Heart ||
                    libraryId == VehicleShapeLibraryId.HeartArrow) &&
                slot.GridPosition.y <= 2 &&
                Mathf.Abs(slot.GridPosition.x - CenterX) <= 1.7f;
        }

        private static bool IsHeartLowerBridgeSlot(
            VehicleShapeLibraryId libraryId,
            VehicleLayoutSlot slot)
        {
            return (libraryId == VehicleShapeLibraryId.Heart ||
                    libraryId == VehicleShapeLibraryId.HeartArrow) &&
                slot.GridPosition.y == 3 &&
                Mathf.Abs(slot.GridPosition.x - CenterX) <= 1.7f;
        }

        private static bool IsHeartLowerSpineSlot(
            VehicleShapeLibraryId libraryId,
            VehicleLayoutSlot slot)
        {
            return IsHeartBottomTipSlot(libraryId, slot) ||
                IsHeartLowerBridgeSlot(libraryId, slot);
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

        private static bool ShouldPreferPathBlockingYaw(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex)
        {
            if (libraryId != VehicleShapeLibraryId.Stairs ||
                slot.ShapeRole == VehicleShapeCellRole.Accent ||
                IsNearOuterBand(slot.GridPosition))
            {
                return false;
            }

            var hash = Mathf.Abs(layoutVariantIndex) +
                slotIndex * 19 +
                slot.GridPosition.x * 11 +
                slot.GridPosition.y * 7;
            if (libraryId == VehicleShapeLibraryId.Arrow ||
                libraryId == VehicleShapeLibraryId.DoubleArrow)
            {
                return hash % 3 != 0;
            }

            return hash % 4 != 0;
        }

        private static bool UsesStrictLaneOffsets(VehicleShapeLibraryId libraryId)
        {
            return libraryId == VehicleShapeLibraryId.Cross ||
                libraryId == VehicleShapeLibraryId.X;
        }

        private static bool ShouldPreferEscapePose(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            bool forceEscapeLanes)
        {
            if (ShouldLockShapeYaw(libraryId, slot))
            {
                return forceEscapeLanes &&
                    ShouldAllowShapeEscapeYaw(slot, libraryId, slotIndex, layoutVariantIndex, forceEscapeLanes);
            }

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

        private static bool ShouldLockShapeYaw(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            return libraryId != VehicleShapeLibraryId.None &&
                slot.ShapeKind != VehicleShapeLayoutKind.None &&
                slot.ShapeRole != VehicleShapeCellRole.Fill;
        }

        private static float GetLockedShapeYawJitter(VehicleLayoutSlot slot, VehicleShapeLibraryId libraryId)
        {
            if (ShouldPreserveStarLinePose(libraryId, slot))
            {
                return 0f;
            }

            if (libraryId == VehicleShapeLibraryId.Star)
            {
                return 6f;
            }

            if (IsFeatureSlot(libraryId, slot))
            {
                return 6f;
            }

            return IsLinearLibrary(libraryId) || IsClosedGeometricLibrary(libraryId) ? 10f : 8f;
        }

        private static bool ShouldAllowReverseShapeYaw(VehicleLayoutSlot slot, VehicleShapeLibraryId libraryId)
        {
            if (ShouldPreserveStarLinePose(libraryId, slot))
            {
                return false;
            }

            if (!ShouldLockShapeYaw(libraryId, slot))
            {
                return false;
            }

            if (slot.ShapeRole == VehicleShapeCellRole.Accent &&
                (libraryId == VehicleShapeLibraryId.Arrow || libraryId == VehicleShapeLibraryId.DoubleArrow))
            {
                return false;
            }

            return true;
        }

        private static bool ShouldAllowShapeEscapeYaw(
            VehicleLayoutSlot slot,
            VehicleShapeLibraryId libraryId,
            int slotIndex,
            int layoutVariantIndex,
            bool forceEscapeLanes)
        {
            if (ShouldPreserveStarLinePose(libraryId, slot))
            {
                return false;
            }

            if (!ShouldLockShapeYaw(libraryId, slot))
            {
                return true;
            }

            if (!forceEscapeLanes)
            {
                return false;
            }

            if (!IsNearOuterBand(slot.GridPosition))
            {
                return false;
            }

            var hash = Mathf.Abs(layoutVariantIndex) +
                slotIndex * 13 +
                slot.GridPosition.x * 7 +
                slot.GridPosition.y * 5;
            if (IsRadialPetalLibrary(libraryId))
            {
                return IsFeatureSlot(libraryId, slot) || hash % 4 == 0;
            }

            if (libraryId == VehicleShapeLibraryId.HollowSquare)
            {
                return IsFeatureSlot(libraryId, slot) || hash % 3 == 0;
            }

            if (libraryId == VehicleShapeLibraryId.Heart || libraryId == VehicleShapeLibraryId.HeartArrow)
            {
                return IsFeatureSlot(libraryId, slot) ? hash % 2 == 0 : hash % 3 == 0;
            }

            if (IsLinearLibrary(libraryId))
            {
                return slot.ShapeRole == VehicleShapeCellRole.Accent ? hash % 2 == 0 : hash % 5 == 0;
            }

            if (IsCurvedLibrary(libraryId))
            {
                return IsFeatureSlot(libraryId, slot) ? hash % 3 == 0 : hash % 4 == 0;
            }

            if (IsClosedGeometricLibrary(libraryId))
            {
                return hash % 5 == 0;
            }

            return IsFeatureSlot(libraryId, slot) ? hash % 3 == 0 : hash % 5 == 0;
        }

        private static bool ShouldPreserveStarLinePose(VehicleShapeLibraryId libraryId, VehicleLayoutSlot slot)
        {
            return libraryId == VehicleShapeLibraryId.Star &&
                slot.ShapeKind != VehicleShapeLayoutKind.None &&
                slot.ShapeRole != VehicleShapeCellRole.Fill;
        }

        private static bool IsNearOuterBand(Vector2Int cell)
        {
            return cell.x <= 2 ||
                cell.x >= BoardLayoutConfig.GridColumns - 3 ||
                cell.y <= 2 ||
                cell.y >= BoardLayoutConfig.GridRows - 3;
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

        private static bool IsDensePlaceable(
            BusDefinition candidate,
            IReadOnlyList<BusDefinition> placedVehicles,
            VehicleShapeLayoutDefinition shapeDefinition,
            IVehicleShapeTemplate qualityTemplate,
            bool useVisualPreviewQuality)
        {
            if (!BoardLayoutConfig.IsInsideGrid(candidate.GridPosition) ||
                IsOutsideDenseBounds(candidate) ||
                !PreservesTemplateBackgroundFeatures(
                    candidate,
                    shapeDefinition,
                    qualityTemplate))
            {
                return false;
            }

            var candidateFootprint = GetDensePlacementFootprint(candidate, useVisualPreviewQuality);
            var candidateVisualFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(candidate);
            for (var index = 0; index < placedVehicles.Count; index++)
            {
                var placed = placedVehicles[index];
                if (candidateFootprint.Overlaps(GetDensePlacementFootprint(placed, useVisualPreviewQuality)))
                {
                    return false;
                }

                var placedVisualFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(placed);
                var visualPadding = useVisualPreviewQuality ? VisualPreviewPlacementPaddingCells : DensePlacementPaddingCells;
                if (candidateVisualFootprint.Overlaps(placedVisualFootprint) ||
                    candidateVisualFootprint.IsWithinPadding(placedVisualFootprint, visualPadding))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PreservesTemplateBackgroundFeatures(
            BusDefinition candidate,
            VehicleShapeLayoutDefinition shapeDefinition,
            IVehicleShapeTemplate template)
        {
            if (template == null || template.KeyFeatures == null)
            {
                return true;
            }

            var footprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(candidate);
            var halfExtents = template.GetProjectionHalfExtentsCells(shapeDefinition.Scale);
            for (var featureIndex = 0; featureIndex < template.KeyFeatures.Count; featureIndex++)
            {
                var feature = template.KeyFeatures[featureIndex];
                if (feature == null ||
                    feature.Expectation != VehicleShapeFeatureExpectation.Background)
                {
                    continue;
                }

                var featureCenter = template.NormalizedToBoard(
                    feature.NormalizedPosition,
                    shapeDefinition.Scale);
                var featureRadius = Mathf.Max(
                    0.05f,
                    feature.RadiusNormalized * Mathf.Min(halfExtents.x, halfExtents.y) * 2f);
                var requiredClearance =
                    template.Constraints.PerceptionPaddingCells +
                    featureRadius * feature.RequiredCoverage;
                if (DistanceToFootprint(featureCenter, footprint) < requiredClearance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float DistanceToFootprint(Vector2 point, VehicleFootprint footprint)
        {
            var delta = point - footprint.Center;
            var outsideRight = Mathf.Max(
                0f,
                Mathf.Abs(Vector2.Dot(delta, footprint.Right)) - footprint.HalfWidth);
            var outsideForward = Mathf.Max(
                0f,
                Mathf.Abs(Vector2.Dot(delta, footprint.Forward)) - footprint.HalfLength);
            return Mathf.Sqrt(outsideRight * outsideRight + outsideForward * outsideForward);
        }

        private static VehicleFootprint GetDensePlacementFootprint(BusDefinition bus, bool useVisualPreviewQuality)
        {
            if (!useVisualPreviewQuality)
            {
                return BoardLayoutConfig.GetVehicleFootprintCells(bus);
            }

            var rootPosition = new Vector3(
                bus.GridPosition.x + bus.PositionOffsetCells.x,
                0f,
                bus.GridPosition.y + bus.PositionOffsetCells.y);
            return BoardLayoutConfig.GetVehicleFootprint(
                rootPosition,
                bus.Rotation,
                bus.Size,
                VisualPreviewCollisionScale);
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

        private static bool IsRadialPetalLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
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
