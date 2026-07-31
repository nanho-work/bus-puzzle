using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public static class LevelGenerator
    {
        private const int DefaultMaxGenerationAttempts = 80;
        private const int VehicleBuildSolutionNodeVisitLimit = 4096;
        private const int MaxPlacementAttemptsPerVehicle = 420;
        private const int MysteryMinVehicles = 5;
        private const int MysteryMaxVehicles = 12;
        private const float MysteryEarlyRatio = 0.18f;
        private const float MysteryLateRatio = 0.30f;
        private const int LightMysteryMinVehicles = 2;
        private const int LightMysteryMaxVehicles = 7;
        private const float LightMysteryEarlyRatio = 0.08f;
        private const float LightMysteryLateRatio = 0.16f;

        private static readonly PuzzleColor[] ColorPool =
        {
            PuzzleColor.Red,
            PuzzleColor.Orange,
            PuzzleColor.Yellow,
            PuzzleColor.Green,
            PuzzleColor.Blue,
            PuzzleColor.Purple,
            PuzzleColor.White,
            PuzzleColor.Black,
            PuzzleColor.Pink,
            PuzzleColor.SkyBlue,
            PuzzleColor.Lime,
            PuzzleColor.Brown
        };

        public static LevelData CreateRuntimeLevel(
            string levelName,
            LevelDifficulty difficulty,
            int seed,
            RotaryRoadPresetId roadPresetId)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = HideFlags.DontSave;

            var profile = LevelDifficultyProfile.DefaultFor(difficulty);
            var buses = BuildVehicles(profile, seed);
            var modifiers = difficulty == LevelDifficulty.Hard
                ? StageModifierFlags.MysteryVehicles
                : StageModifierFlags.None;
            buses = ApplyMysteryVehicleModifiers(
                buses,
                CreateDefaultMysteryVehicleProfile(modifiers, profile),
                seed + 1699);
            var flowPlan = BuildPassengerFlowPlan(profile, buses, seed);
            level.ConfigureWithPassengerFlowPlan(
                levelName,
                profile,
                flowPlan,
                buses,
                GetRotaryCapacity(difficulty),
                roadPresetId);

            return level;
        }

        public static LevelData CreateRuntimeStage(
            StageGenerationRequest request,
            GarageGenerationRule garageRule,
            int candidateOffset = 0,
            int vehicleGenerationAttempts = DefaultMaxGenerationAttempts,
            bool useSolutionAnalyzer = true,
            bool useVisualPreviewFlow = false)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = HideFlags.DontSave;

            var seed = request.Seed + candidateOffset * 7919;
            var random = new System.Random(seed);
            var colors = PickColorSet(request.Profile.TargetColorCount);
            var garages = BuildGarages(request, garageRule, random, colors);
            var garageVehicleCount = CountGarageVehicles(garages);
            var regularVehicleTarget = Mathf.Max(4, request.Profile.TargetVehicleCount - garageVehicleCount);
            var buses = BuildVehicles(
                request.Profile,
                seed + 313,
                regularVehicleTarget,
                garages,
                vehicleGenerationAttempts,
                useSolutionAnalyzer,
                request.VehicleLayoutVariantIndex,
                useVisualPreviewFlow);
            buses = ApplyMysteryVehicleModifiers(buses, request.MysteryVehicleProfile, seed + 1699);
            if (useVisualPreviewFlow &&
                ShapeLibraryVehicleCoverage.RequiresCoverage(
                    request.Profile,
                    request.VehicleLayoutVariantIndex) &&
                (garages == null || garages.Count == 0) &&
                !IsTemplateBackedHeartLayout(
                    request.Profile,
                    request.VehicleLayoutVariantIndex,
                    buses.Count))
            {
                buses = PrepareVisualPreviewOpeningVehicles(
                    buses,
                    request.Profile,
                    request.VehicleLayoutVariantIndex);
            }

            var flowPlan = useVisualPreviewFlow &&
                ShapeLibraryVehicleCoverage.RequiresCoverage(
                    request.Profile,
                    request.VehicleLayoutVariantIndex) &&
                (garages == null || garages.Count == 0)
                ? BuildPassengerFlowPlanFromVehicleOrder(request.Profile, buses, seed)
                : BuildPassengerFlowPlan(request.Profile, buses, garages, seed);

            level.ConfigureWithPassengerFlowPlan(
                $"Stage {request.StageNumber:000} {request.Difficulty}",
                request.Profile,
                flowPlan,
                buses,
                request.RotaryCapacity,
                request.RoadPresetId,
                null,
                garages);

            return level;
        }

        public static List<BusDefinition> BuildVehicles(LevelDifficultyProfile profile, int seed)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            return BuildVehicles(profile, seed, profile.TargetVehicleCount, null);
        }

        public static List<BusDefinition> BuildVehicles(
            LevelDifficultyProfile profile,
            int seed,
            int targetVehicleCount,
            IReadOnlyList<GarageDefinition> garages)
        {
            return BuildVehicles(profile, seed, targetVehicleCount, garages, DefaultMaxGenerationAttempts);
        }

        public static List<BusDefinition> BuildVehicles(
            LevelDifficultyProfile profile,
            int seed,
            int targetVehicleCount,
            IReadOnlyList<GarageDefinition> garages,
            int maxGenerationAttempts)
        {
            return BuildVehicles(profile, seed, targetVehicleCount, garages, maxGenerationAttempts, true);
        }

        public static List<BusDefinition> BuildVehicles(
            LevelDifficultyProfile profile,
            int seed,
            int targetVehicleCount,
            IReadOnlyList<GarageDefinition> garages,
            int maxGenerationAttempts,
            bool useSolutionAnalyzer,
            int layoutVariantIndex = -1,
            bool useVisualPreviewQuality = false)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);

            targetVehicleCount = Mathf.Clamp(targetVehicleCount, 1, 80);
            maxGenerationAttempts = Mathf.Clamp(maxGenerationAttempts, 1, DefaultMaxGenerationAttempts);
            var bestVehicles = new List<BusDefinition>();
            var bestExitCount = -1;
            var bestVehicleCount = -1;

            for (var attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                var random = new System.Random(seed + attempt * 9973);
                var effectiveLayoutVariantIndex = VehicleLayoutPatternEngine.GetProbeLayoutVariantIndex(
                    profile,
                    layoutVariantIndex,
                    attempt);
                var vehicles = TryBuildVehicleSet(
                    profile,
                    random,
                    targetVehicleCount,
                    garages,
                    effectiveLayoutVariantIndex,
                    useVisualPreviewQuality,
                    attempt);
                if (CanAcceptShapeLibraryVisualProbe(
                        vehicles,
                        profile,
                        effectiveLayoutVariantIndex,
                        useSolutionAnalyzer,
                        useVisualPreviewQuality))
                {
                    return vehicles;
                }

                if (HasPlayableExitOrder(
                    vehicles,
                    garages,
                    profile,
                    effectiveLayoutVariantIndex,
                    useSolutionAnalyzer,
                    out var exitOrder))
                {
                    if (ShapeLibraryVehicleCoverage.IsSatisfied(profile, effectiveLayoutVariantIndex, vehicles.Count))
                    {
                        return vehicles;
                    }

                    if (vehicles.Count > bestVehicleCount)
                    {
                        bestVehicleCount = vehicles.Count;
                        bestExitCount = exitOrder.Count;
                        bestVehicles = vehicles;
                    }

                    continue;
                }

                if (exitOrder.Count > bestExitCount ||
                    (exitOrder.Count == bestExitCount && vehicles.Count > bestVehicleCount))
                {
                    bestExitCount = exitOrder.Count;
                    bestVehicleCount = vehicles.Count;
                    bestVehicles = vehicles;
                }
            }

            return bestVehicles;
        }

        private static bool CanAcceptShapeLibraryVisualProbe(
            IReadOnlyList<BusDefinition> vehicles,
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            bool useSolutionAnalyzer,
            bool useVisualPreviewQuality)
        {
            if (useSolutionAnalyzer ||
                vehicles == null ||
                !ShapeLibraryVehicleCoverage.RequiresCoverage(profile, layoutVariantIndex) ||
                !ShapeLibraryVehicleCoverage.IsSatisfied(profile, layoutVariantIndex, vehicles.Count))
            {
                return false;
            }

            // Visual previews deliberately defer exit ordering to the pair-aware opening
            // pass. Production candidates continue into HasPlayableExitOrder below so a
            // pretty but stuck shape can never be accepted early.
            return useVisualPreviewQuality;
        }

        private static bool HasPlayableExitOrder(
            IReadOnlyList<BusDefinition> vehicles,
            IReadOnlyList<GarageDefinition> garages,
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            bool useSolutionAnalyzer,
            out List<int> exitOrder)
        {
            exitOrder = new List<int>();
            if (!useSolutionAnalyzer)
            {
                if (vehicles == null || vehicles.Count == 0)
                {
                    return false;
                }

                if (ShapeLibraryVehicleCoverage.RequiresCoverage(profile, layoutVariantIndex))
                {
                    if (!ShapeLibraryVehicleCoverage.IsSatisfied(profile, layoutVariantIndex, vehicles.Count) ||
                        !ShapeLibraryLayoutQuality.IsSatisfied(profile, layoutVariantIndex, vehicles))
                    {
                        return false;
                    }

                    return LevelVehicleExitPlanner.TryFindExitOrder(vehicles, out exitOrder, out _) &&
                        exitOrder.Count == vehicles.Count;
                }

                LevelVehicleExitPlanner.TryFindExitOrder(vehicles, out exitOrder, out _);
                return exitOrder.Count == vehicles.Count;
            }

            return StageSolutionAnalyzer.Analyze(vehicles, garages, 2, VehicleBuildSolutionNodeVisitLimit).IsSolvable;
        }

        private static List<BusDefinition> ApplyMysteryVehicleModifiers(
            IReadOnlyList<BusDefinition> buses,
            MysteryVehicleGenerationProfile mysteryProfile,
            int seed)
        {
            var result = buses != null ? new List<BusDefinition>(buses) : new List<BusDefinition>();
            if (!mysteryProfile.Enabled || result.Count == 0)
            {
                return result;
            }

            var active = new bool[result.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
                result[index] = result[index].WithStartsConcealed(false);
            }

            var candidates = new List<int>();
            for (var index = 0; index < result.Count; index++)
            {
                if (!LevelVehicleExitPlanner.IsPathClear(index, result, active, out var blockingIndex) &&
                    blockingIndex >= 0)
                {
                    candidates.Add(index);
                }
            }

            if (candidates.Count == 0)
            {
                return result;
            }

            var target = Mathf.RoundToInt(result.Count * mysteryProfile.Ratio);
            target = Mathf.Clamp(
                target,
                Mathf.Min(mysteryProfile.MinVehicles, candidates.Count),
                Mathf.Min(mysteryProfile.MaxVehicles, candidates.Count));

            ShuffleIndices(candidates, new System.Random(seed ^ 0x5f3759df));
            var selected = new HashSet<int>();
            for (var index = 0; index < target; index++)
            {
                selected.Add(candidates[index]);
            }

            for (var index = 0; index < result.Count; index++)
            {
                result[index] = result[index].WithStartsConcealed(selected.Contains(index));
            }

            return result;
        }

        private static MysteryVehicleGenerationProfile CreateDefaultMysteryVehicleProfile(
            StageModifierFlags modifiers,
            LevelDifficultyProfile profile)
        {
            var hasMystery = (modifiers & StageModifierFlags.MysteryVehicles) != 0;
            var hasLightMystery = (modifiers & StageModifierFlags.LightMysteryVehicles) != 0;
            if (!hasMystery && !hasLightMystery)
            {
                return MysteryVehicleGenerationProfile.Disabled;
            }

            var tension = profile != null
                ? Mathf.Clamp01(profile.ParkingTension * 0.70f + profile.StationPressure * 0.30f)
                : 0.50f;
            var minVehicles = hasMystery ? MysteryMinVehicles : LightMysteryMinVehicles;
            var maxVehicles = hasMystery ? MysteryMaxVehicles : LightMysteryMaxVehicles;
            var earlyRatio = hasMystery ? MysteryEarlyRatio : LightMysteryEarlyRatio;
            var lateRatio = hasMystery ? MysteryLateRatio : LightMysteryLateRatio;
            return new MysteryVehicleGenerationProfile(
                true,
                minVehicles,
                maxVehicles,
                Mathf.Lerp(earlyRatio, lateRatio, tension));
        }

        private static void ShuffleIndices(List<int> indices, System.Random random)
        {
            for (var index = indices.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(0, index + 1);
                var temp = indices[index];
                indices[index] = indices[swapIndex];
                indices[swapIndex] = temp;
            }
        }

        public static PassengerFlowPlan BuildPassengerFlowPlan(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses,
            int seed)
        {
            var flowPlan = new PassengerFlowPlan();
            var solutionRoute = BuildSolutionRoute(profile, buses);
            var rule = GetPassengerFlowRule(profile);
            flowPlan.ConfigureSolutionRoute(solutionRoute, rule.MinGroupUnits, rule.MaxGroupUnits, true, seed);
            return flowPlan;
        }

        public static PassengerFlowPlan BuildPassengerFlowPlanFromVehicleOrder(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses,
            int seed)
        {
            var flowPlan = new PassengerFlowPlan();
            var solutionRoute = BuildVehicleOrderRoute(profile, buses);
            var rule = GetPassengerFlowRule(profile);
            flowPlan.ConfigureSolutionRoute(solutionRoute, rule.MinGroupUnits, rule.MaxGroupUnits, true, seed);
            return flowPlan;
        }

        public static PassengerFlowPlan BuildPassengerFlowPlan(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int seed)
        {
            var flowPlan = new PassengerFlowPlan();
            var solutionRoute = BuildSolutionRoute(profile, buses, garages);
            var rule = GetPassengerFlowRule(profile);
            flowPlan.ConfigureSolutionRoute(solutionRoute, rule.MinGroupUnits, rule.MaxGroupUnits, true, seed);
            return flowPlan;
        }

        public static int GetRotaryCapacity(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return 25;
                case LevelDifficulty.SuperHard:
                    return 30;
                default:
                    return 20;
            }
        }

        public static RotaryRoadPresetId GetRoadPreset(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return RotaryRoadPresetId.WideTerminal;
                case LevelDifficulty.SuperHard:
                    return RotaryRoadPresetId.TallTerminal;
                default:
                    return RotaryRoadPresetId.CompactOval;
            }
        }

        private static List<BusDefinition> TryBuildVehicleSet(
            LevelDifficultyProfile profile,
            System.Random random,
            int targetVehicleCount,
            IReadOnlyList<GarageDefinition> garages,
            int layoutVariantIndex,
            bool useVisualPreviewQuality = false,
            int placementProbeIndex = 0)
        {
            var vehicles = new List<BusDefinition>();
            var colors = PickColorSet(profile.TargetColorCount);
            if ((garages == null || garages.Count == 0) &&
                DenseShowcaseLayoutEngine.TryBuildVehicles(
                    profile,
                    random,
                    targetVehicleCount,
                    layoutVariantIndex,
                    colors,
                    out var denseShowcaseVehicles,
                    useVisualPreviewQuality,
                    placementProbeIndex))
            {
                if (IsTemplateBackedHeartLayout(
                        profile,
                        layoutVariantIndex,
                        denseShowcaseVehicles.Count))
                {
                    // Three bounded mirror-pair passes are the authored Heart opening
                    // contract. The first creates the initial exits, the second repairs
                    // the tip/notch footprint, and the last handles the extra central
                    // bridge pair used by high-budget Hearts. Every pass preserves
                    // left/right symmetry and is rechecked by the final silhouette gate.
                    for (var openingPass = 0; openingPass < 3; openingPass++)
                    {
                        if (openingPass > 0 && HasGreedyExitOrder(denseShowcaseVehicles))
                        {
                            break;
                        }

                        denseShowcaseVehicles = PrepareVisualPreviewOpeningVehicles(
                            denseShowcaseVehicles,
                            profile,
                            layoutVariantIndex);
                    }
                }

                return RecolorShapeLibraryVehiclesForExitOrder(
                    denseShowcaseVehicles,
                    profile,
                    layoutVariantIndex,
                    colors);
            }

            TryPlacePatternVehicles(profile, random, targetVehicleCount, garages, colors, vehicles, layoutVariantIndex);

            for (var vehicleIndex = vehicles.Count; vehicleIndex < targetVehicleCount; vehicleIndex++)
            {
                if (!TryPlaceVehicle(profile, random, vehicles, colors, garages, vehicleIndex, out var vehicle))
                {
                    continue;
                }

                vehicles.Add(vehicle);
            }

            return RecolorShapeLibraryVehiclesForExitOrder(vehicles, profile, layoutVariantIndex, colors);
        }

        private static void TryPlacePatternVehicles(
            LevelDifficultyProfile profile,
            System.Random random,
            int targetVehicleCount,
            IReadOnlyList<GarageDefinition> garages,
            IReadOnlyList<PuzzleColor> colors,
            List<BusDefinition> vehicles,
            int layoutVariantIndex)
        {
            var slots = VehicleLayoutPatternEngine.CreateSlots(profile, random, targetVehicleCount, layoutVariantIndex);
            for (var slotIndex = 0; slotIndex < slots.Count && vehicles.Count < targetVehicleCount; slotIndex++)
            {
                var slot = slots[slotIndex];
                var vehicleIndex = vehicles.Count;
                var preferredSize = PickPatternSize(profile, random, slot, layoutVariantIndex);
                var color = PickSlotColor(slot, colors, vehicleIndex);
                if (TryCreatePatternVehicle(
                    profile,
                    color,
                    preferredSize,
                    slot,
                    vehicles,
                    garages,
                    out var vehicle))
                {
                    vehicles.Add(vehicle);
                }
            }
        }

        private static PuzzleColor PickSlotColor(
            VehicleLayoutSlot slot,
            IReadOnlyList<PuzzleColor> colors,
            int vehicleIndex)
        {
            if (slot.HasPreferredColor)
            {
                return slot.PreferredColor;
            }

            return colors[vehicleIndex % colors.Count];
        }

        private static List<BusDefinition> RecolorShapeLibraryVehiclesForExitOrder(
            List<BusDefinition> vehicles,
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<PuzzleColor> colors)
        {
            if (!VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex) ||
                vehicles == null ||
                vehicles.Count == 0)
            {
                return vehicles;
            }

            IReadOnlyList<PuzzleColor> palette = colors != null && colors.Count > 0 ? colors : ColorPool;
            VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                profile,
                vehicles.Count,
                layoutVariantIndex,
                out var shapeDefinition);
            var libraryId = (VehicleShapeLibraryId)libraryIndex;
            var orderedIndices = ShouldRecolorByVisualShapeOrder(libraryId)
                ? BuildCompleteOrder(vehicles.Count, null)
                : BuildCompleteOrder(
                    vehicles.Count,
                    LevelVehicleExitPlanner.TryFindExitOrder(vehicles, out var exitOrder, out _)
                        ? exitOrder
                        : null);
            var recolored = new List<BusDefinition>(vehicles);
            var paletteIndex = 0;
            for (var orderIndex = 0; orderIndex < orderedIndices.Count; orderIndex++)
            {
                var vehicleIndex = orderedIndices[orderIndex];
                var bus = recolored[vehicleIndex];
                if (ShouldPreserveShapeRoleColor(shapeDefinition, bus))
                {
                    continue;
                }

                var color = palette[paletteIndex % palette.Count];
                paletteIndex++;
                recolored[vehicleIndex] = new BusDefinition(
                    color,
                    bus.Size,
                    bus.Direction,
                    bus.GridPosition,
                    bus.AngleOffsetDegrees,
                    bus.PositionOffsetCells,
                    bus.StartsConcealed);
            }

            return recolored;
        }

        private static bool ShouldRecolorByVisualShapeOrder(VehicleShapeLibraryId libraryId)
        {
            return libraryId == VehicleShapeLibraryId.Star;
        }

        private static bool ShouldPreserveShapeRoleColor(
            VehicleShapeLayoutDefinition shapeDefinition,
            BusDefinition bus)
        {
            if (shapeDefinition.Kind == VehicleShapeLayoutKind.None)
            {
                return false;
            }

            var position = new Vector2(
                bus.GridPosition.x + bus.PositionOffsetCells.x,
                bus.GridPosition.y + bus.PositionOffsetCells.y);
            return VehicleShapeLayoutEngine.TryFindNearestShapeCell(
                    shapeDefinition,
                    position,
                    out var nearestCell,
                    out var distanceCells) &&
                distanceCells <= 0.72f &&
                VehicleShapeLayoutEngine.IsFeatureCell(shapeDefinition, nearestCell);
        }

        private static List<BusDefinition> PrepareVisualPreviewOpeningVehicles(
            List<BusDefinition> buses,
            LevelDifficultyProfile profile,
            int layoutVariantIndex)
        {
            if (buses == null || buses.Count == 0)
            {
                return buses;
            }

            var protectedLineIndices = BuildVisualPreviewProtectedLineIndices(
                buses,
                profile,
                layoutVariantIndex);
            var preserveHeartMirrorPairs = IsTemplateBackedHeartLayout(
                profile,
                layoutVariantIndex,
                buses.Count);
            var candidates = BuildVisualPreviewOpeningCandidates(
                buses,
                protectedLineIndices,
                preserveHeartMirrorPairs);
            var selectedIndices = new List<int>();
            var selected = new HashSet<int>();
            for (var candidateIndex = 0; candidateIndex < candidates.Count && selectedIndices.Count < 6; candidateIndex++)
            {
                var candidate = candidates[candidateIndex];
                if (selected.Contains(candidate.VehicleIndex))
                {
                    continue;
                }

                var adjusted = new List<BusDefinition>(buses);
                adjusted[candidate.VehicleIndex] = candidate.Bus;
                var mirroredPartnerIndex = -1;
                if (preserveHeartMirrorPairs)
                {
                    mirroredPartnerIndex = FindMirroredHeartPartnerIndex(
                        buses,
                        candidate.VehicleIndex);
                    if (mirroredPartnerIndex < 0 || selected.Contains(mirroredPartnerIndex))
                    {
                        continue;
                    }

                    adjusted[mirroredPartnerIndex] = CreateMirroredHeartBus(
                        candidate.Bus,
                        buses[mirroredPartnerIndex]);
                }

                if (HasVehicleStartOverlap(adjusted, candidate.VehicleIndex) ||
                    (mirroredPartnerIndex >= 0 &&
                        !IsWithinRecommendedBoardBounds(adjusted[mirroredPartnerIndex])) ||
                    !HasOpeningMove(adjusted, candidate.VehicleIndex) ||
                    (mirroredPartnerIndex >= 0 &&
                        (HasVehicleStartOverlap(adjusted, mirroredPartnerIndex) ||
                         !HasOpeningMove(adjusted, mirroredPartnerIndex))))
                {
                    continue;
                }

                buses[candidate.VehicleIndex] = candidate.Bus;
                selected.Add(candidate.VehicleIndex);
                selectedIndices.Add(candidate.VehicleIndex);
                if (mirroredPartnerIndex >= 0)
                {
                    buses[mirroredPartnerIndex] = adjusted[mirroredPartnerIndex];
                    selected.Add(mirroredPartnerIndex);
                    selectedIndices.Add(mirroredPartnerIndex);
                }
            }

            if (selectedIndices.Count == 0)
            {
                return buses;
            }

            if (TryBuildGreedyOrderedVehicles(buses, out var greedyOrdered))
            {
                return greedyOrdered;
            }

            if (TryRepairGreedyOrderedVehicles(
                    buses,
                    protectedLineIndices,
                    out var repairedOrdered))
            {
                // Heart repairs are allowed only as a bounded fallback. The caller's
                // mandatory silhouette/symmetry gate rejects any repair that makes the
                // named shape visually unacceptable.
                return repairedOrdered;
            }

            var ordered = new List<BusDefinition>(buses.Count);
            for (var index = 0; index < selectedIndices.Count; index++)
            {
                ordered.Add(buses[selectedIndices[index]]);
            }

            for (var index = 0; index < buses.Count; index++)
            {
                if (!selected.Contains(index))
                {
                    ordered.Add(buses[index]);
                }
            }

            return ordered;
        }

        private static List<VisualPreviewOpeningCandidate> BuildVisualPreviewOpeningCandidates(
            IReadOnlyList<BusDefinition> buses,
            HashSet<int> protectedLineIndices,
            bool prioritizeHeartTip)
        {
            var candidates = new List<VisualPreviewOpeningCandidate>();
            for (var index = 0; index < buses.Count; index++)
            {
                if (protectedLineIndices != null && protectedLineIndices.Contains(index))
                {
                    continue;
                }

                var bus = buses[index];
                var directions = GetVisualPreviewOpeningDirections(bus);
                for (var directionIndex = 0; directionIndex < directions.Count; directionIndex++)
                {
                    var direction = directions[directionIndex];
                    var adjusted = new BusDefinition(
                        bus.Color,
                        bus.Size,
                        direction,
                        bus.GridPosition,
                        0f,
                        GetVisualPreviewOpeningOffset(direction),
                        bus.StartsConcealed);
                    if (!IsWithinRecommendedBoardBounds(adjusted))
                    {
                        continue;
                    }

                    var edgeScore = GetVisualPreviewEdgeScore(bus.GridPosition, direction) +
                        GetHeartTipOpeningPriority(bus, direction, prioritizeHeartTip);
                    candidates.Add(new VisualPreviewOpeningCandidate(index, adjusted, edgeScore + directionIndex * 0.05f));
                }
            }

            candidates.Sort((left, right) => left.Score.CompareTo(right.Score));
            return candidates;
        }

        private static float GetHeartTipOpeningPriority(
            BusDefinition bus,
            GridDirection direction,
            bool prioritizeHeartTip)
        {
            if (!prioritizeHeartTip || direction != GridDirection.Down)
            {
                return 0f;
            }

            var centerX = (BoardLayoutConfig.GridColumns - 1) * 0.5f;
            return bus.GridPosition.y <= 3 &&
                Mathf.Abs(bus.GridPosition.x - centerX) <= 1.5f
                ? -100f
                : 0f;
        }

        private static List<GridDirection> GetVisualPreviewOpeningDirections(BusDefinition bus)
        {
            var distances = new List<VisualPreviewDirectionDistance>
            {
                new VisualPreviewDirectionDistance(GridDirection.Left, bus.GridPosition.x),
                new VisualPreviewDirectionDistance(GridDirection.Right, BoardLayoutConfig.GridColumns - 1 - bus.GridPosition.x),
                new VisualPreviewDirectionDistance(GridDirection.Down, bus.GridPosition.y),
                new VisualPreviewDirectionDistance(GridDirection.Up, BoardLayoutConfig.GridRows - 1 - bus.GridPosition.y)
            };

            distances.Sort((left, right) => left.Distance.CompareTo(right.Distance));
            var directions = new List<GridDirection>(4);
            for (var index = 0; index < distances.Count; index++)
            {
                directions.Add(distances[index].Direction);
            }

            return directions;
        }

        private static float GetVisualPreviewEdgeScore(Vector2Int gridPosition, GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Left:
                    return gridPosition.x;
                case GridDirection.Right:
                    return BoardLayoutConfig.GridColumns - 1 - gridPosition.x;
                case GridDirection.Down:
                    return gridPosition.y;
                case GridDirection.Up:
                    return BoardLayoutConfig.GridRows - 1 - gridPosition.y;
                default:
                    return 99f;
            }
        }

        private static Vector2 GetVisualPreviewOpeningOffset(GridDirection direction)
        {
            return GetVisualPreviewOpeningOffset(direction, 0);
        }

        private static Vector2 GetVisualPreviewOpeningOffset(GridDirection direction, int offsetIndex)
        {
            var offsetCells = offsetIndex == 0
                ? 0.34f
                : offsetIndex == 1
                    ? 0.22f
                    : 0f;
            switch (direction)
            {
                case GridDirection.Left:
                    return new Vector2(-offsetCells, 0f);
                case GridDirection.Right:
                    return new Vector2(offsetCells, 0f);
                case GridDirection.Down:
                    return new Vector2(0f, -offsetCells);
                case GridDirection.Up:
                    return new Vector2(0f, offsetCells);
                default:
                    return Vector2.zero;
            }
        }

        private static bool IsTemplateBackedHeartLayout(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            int vehicleCount)
        {
            return VehicleLayoutPatternEngine.TryCreateTemplateQualityShapeDefinition(
                    profile,
                    Mathf.Max(1, vehicleCount),
                    layoutVariantIndex,
                    out var definition) &&
                (definition.LibraryId == VehicleShapeLibraryId.Heart ||
                 definition.LibraryId == VehicleShapeLibraryId.HeartArrow);
        }

        private static int FindMirroredHeartPartnerIndex(
            IReadOnlyList<BusDefinition> buses,
            int vehicleIndex)
        {
            if (buses == null || vehicleIndex < 0 || vehicleIndex >= buses.Count)
            {
                return -1;
            }

            var source = buses[vehicleIndex];
            var mirrorX = BoardLayoutConfig.GridColumns - 1 - source.GridPosition.x;
            for (var index = 0; index < buses.Count; index++)
            {
                if (index == vehicleIndex)
                {
                    continue;
                }

                var candidate = buses[index];
                if (candidate.Size == source.Size &&
                    candidate.GridPosition.x == mirrorX &&
                    candidate.GridPosition.y == source.GridPosition.y)
                {
                    return index;
                }
            }

            return -1;
        }

        private static BusDefinition CreateMirroredHeartBus(
            BusDefinition source,
            BusDefinition partner)
        {
            var mirroredYaw = Mathf.Repeat(-source.YawDegrees + 360f, 360f);
            var direction = DirectionFromVisualYaw(mirroredYaw);
            var angleOffset = Mathf.DeltaAngle(
                GridDirectionUtility.ToYawDegrees(direction),
                mirroredYaw);
            return new BusDefinition(
                partner.Color,
                partner.Size,
                direction,
                partner.GridPosition,
                angleOffset,
                new Vector2(-source.PositionOffsetCells.x, source.PositionOffsetCells.y),
                partner.StartsConcealed);
        }

        private static GridDirection DirectionFromVisualYaw(float yaw)
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

        public static int CountOpeningMoves(IReadOnlyList<BusDefinition> buses)
        {
            if (buses == null || buses.Count == 0)
            {
                return 0;
            }

            var active = new bool[buses.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
            }

            var openingMoves = 0;
            for (var index = 0; index < buses.Count; index++)
            {
                if (LevelVehicleExitPlanner.IsPathClear(index, buses, active, out _))
                {
                    openingMoves++;
                }
            }

            return openingMoves;
        }

        public static bool HasGreedyExitOrder(IReadOnlyList<BusDefinition> buses)
        {
            return LevelVehicleExitPlanner.TryFindExitOrder(buses, out var exitOrder, out _) &&
                exitOrder.Count == (buses != null ? buses.Count : 0);
        }

        public static bool TryFindGreedyExitOrder(
            IReadOnlyList<BusDefinition> buses,
            out List<int> exitOrder,
            out List<int> stuckIndices)
        {
            return LevelVehicleExitPlanner.TryFindExitOrder(buses, out exitOrder, out stuckIndices);
        }

        public static bool TryBuildGreedyOrderedVehicles(
            IReadOnlyList<BusDefinition> buses,
            out List<BusDefinition> orderedVehicles)
        {
            orderedVehicles = new List<BusDefinition>();
            if (buses == null ||
                !LevelVehicleExitPlanner.TryFindExitOrder(buses, out var exitOrder, out _) ||
                exitOrder.Count != buses.Count)
            {
                return false;
            }

            for (var index = 0; index < exitOrder.Count; index++)
            {
                orderedVehicles.Add(buses[exitOrder[index]]);
            }

            return true;
        }

        public static bool TryConstrainOpeningMoves(
            IReadOnlyList<BusDefinition> buses,
            int minimumOpeningMoves,
            int maximumOpeningMoves,
            out List<BusDefinition> constrainedBuses)
        {
            return TryConstrainOpeningMoves(
                buses,
                null,
                int.MaxValue,
                minimumOpeningMoves,
                maximumOpeningMoves,
                out constrainedBuses);
        }

        public static bool TryConstrainOpeningMoves(
            IReadOnlyList<BusDefinition> buses,
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            int minimumOpeningMoves,
            int maximumOpeningMoves,
            out List<BusDefinition> constrainedBuses)
        {
            constrainedBuses = buses != null ? new List<BusDefinition>(buses) : new List<BusDefinition>();
            if (buses == null || buses.Count == 0)
            {
                return false;
            }

            minimumOpeningMoves = Mathf.Clamp(minimumOpeningMoves, 0, buses.Count);
            maximumOpeningMoves = Mathf.Clamp(Mathf.Max(minimumOpeningMoves, maximumOpeningMoves), minimumOpeningMoves, buses.Count);
            var openingMoveCount = CountOpeningMoves(constrainedBuses);
            if (openingMoveCount < minimumOpeningMoves)
            {
                return false;
            }

            var protectedLineIndices = BuildVisualPreviewProtectedLineIndices(
                constrainedBuses,
                profile,
                layoutVariantIndex);
            var safety = constrainedBuses.Count * 4;
            var hasGreedyExitOrder = HasGreedyExitOrder(constrainedBuses);
            if (IsTemplateBackedHeartLayout(profile, layoutVariantIndex, constrainedBuses.Count) &&
                (openingMoveCount > maximumOpeningMoves || !hasGreedyExitOrder))
            {
                // The generic reducer edits one pose at a time. A Heart is authored and
                // graded in mirror pairs, so reject this probe instead of silently
                // destroying symmetry after the pair-aware opening pass.
                return false;
            }

            while ((openingMoveCount > maximumOpeningMoves || !hasGreedyExitOrder) && safety-- > 0)
            {
                if (!TryFindOpeningReductionMove(
                    constrainedBuses,
                    protectedLineIndices,
                    minimumOpeningMoves,
                    maximumOpeningMoves,
                    openingMoveCount,
                    out var adjustedIndex,
                    out var adjustedBus,
                    out var adjustedOpeningMoveCount))
                {
                    return false;
                }

                constrainedBuses[adjustedIndex] = adjustedBus;
                openingMoveCount = adjustedOpeningMoveCount;
                hasGreedyExitOrder = HasGreedyExitOrder(constrainedBuses);
            }

            return openingMoveCount >= minimumOpeningMoves &&
                openingMoveCount <= maximumOpeningMoves &&
                hasGreedyExitOrder;
        }

        public static bool TryApplyStarSizeMixSizing(
            IReadOnlyList<BusDefinition> buses,
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            int minimumMediumLargeCount,
            int minimumLargeCount,
            out List<BusDefinition> resizedBuses)
        {
            resizedBuses = buses != null ? new List<BusDefinition>(buses) : new List<BusDefinition>();
            if (buses == null ||
                buses.Count == 0 ||
                !VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex) ||
                (VehicleShapeLibraryId)libraryIndex != VehicleShapeLibraryId.Star ||
                !VehicleLayoutPatternEngine.TryGetShapeLibraryVariantSeed(layoutVariantIndex, out var variantSeed) ||
                variantSeed != StageGenerationPlanner.StarSizeMixVariantSeed ||
                !VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                    profile,
                    Mathf.Max(buses.Count, profile != null ? profile.TargetVehicleCount : buses.Count),
                    layoutVariantIndex,
                    out var shapeDefinition))
            {
                return false;
            }

            PromoteStarSizeMixVehicles(
                resizedBuses,
                shapeDefinition,
                BusSize.Large,
                minimumLargeCount);
            PromoteStarSizeMixVehicles(
                resizedBuses,
                shapeDefinition,
                BusSize.Medium,
                minimumMediumLargeCount);

            return CountMediumLargeVehicles(resizedBuses) >= minimumMediumLargeCount &&
                CountLargeVehicles(resizedBuses) >= minimumLargeCount &&
                HasGreedyExitOrder(resizedBuses);
        }

        private static void PromoteStarSizeMixVehicles(
            List<BusDefinition> buses,
            VehicleShapeLayoutDefinition shapeDefinition,
            BusSize targetSize,
            int minimumCount)
        {
            var candidates = BuildStarSizeMixPromotionCandidates(buses, shapeDefinition, targetSize);
            for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                if (targetSize == BusSize.Large && CountLargeVehicles(buses) >= minimumCount)
                {
                    return;
                }

                if (targetSize == BusSize.Medium && CountMediumLargeVehicles(buses) >= minimumCount)
                {
                    return;
                }

                var vehicleIndex = candidates[candidateIndex];
                var bus = buses[vehicleIndex];
                if (bus.Size == targetSize ||
                    (targetSize == BusSize.Medium && bus.Size == BusSize.Large))
                {
                    continue;
                }

                var promoted = new BusDefinition(
                    bus.Color,
                    targetSize,
                    bus.Direction,
                    bus.GridPosition,
                    bus.AngleOffsetDegrees,
                    bus.PositionOffsetCells,
                    bus.StartsConcealed);
                var adjusted = new List<BusDefinition>(buses);
                adjusted[vehicleIndex] = promoted;
                if (!IsWithinRecommendedBoardBounds(promoted) ||
                    HasVehicleStartOverlap(adjusted, vehicleIndex) ||
                    !HasGreedyExitOrder(adjusted))
                {
                    continue;
                }

                buses[vehicleIndex] = promoted;
            }
        }

        private static List<int> BuildStarSizeMixPromotionCandidates(
            IReadOnlyList<BusDefinition> buses,
            VehicleShapeLayoutDefinition shapeDefinition,
            BusSize targetSize)
        {
            var candidates = new List<int>();
            if (buses == null)
            {
                return candidates;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                if (IsStarSizeMixPromotionCandidate(buses[index], shapeDefinition, targetSize))
                {
                    candidates.Add(index);
                }
            }

            candidates.Sort((left, right) =>
                GetStarSizeMixPromotionScore(buses[left], shapeDefinition, targetSize)
                    .CompareTo(GetStarSizeMixPromotionScore(buses[right], shapeDefinition, targetSize)));
            return candidates;
        }

        private static bool IsStarSizeMixPromotionCandidate(
            BusDefinition bus,
            VehicleShapeLayoutDefinition shapeDefinition,
            BusSize targetSize)
        {
            if (targetSize == BusSize.Large && bus.Size == BusSize.Large)
            {
                return false;
            }

            if (targetSize == BusSize.Medium && bus.Size != BusSize.Small)
            {
                return false;
            }

            if (!TryFindNearestShapeCell(bus, shapeDefinition, out var nearestCell, out var distanceCells) ||
                distanceCells > 1.15f ||
                nearestCell.Role == VehicleShapeCellRole.Accent)
            {
                return false;
            }

            if (targetSize == BusSize.Large)
            {
                return nearestCell.Role == VehicleShapeCellRole.Fill ||
                    nearestCell.Role == VehicleShapeCellRole.Outline;
            }

            return true;
        }

        private static float GetStarSizeMixPromotionScore(
            BusDefinition bus,
            VehicleShapeLayoutDefinition shapeDefinition,
            BusSize targetSize)
        {
            if (!TryFindNearestShapeCell(bus, shapeDefinition, out var nearestCell, out var distanceCells))
            {
                return 999f;
            }

            var roleScore = nearestCell.Role == VehicleShapeCellRole.Fill
                ? 0f
                : nearestCell.Role == VehicleShapeCellRole.Outline
                    ? 10f
                    : 100f;
            var sizeScore = bus.Size == BusSize.Medium && targetSize == BusSize.Large ? -2f : 0f;
            return roleScore + distanceCells + sizeScore;
        }

        private static bool TryFindNearestShapeCell(
            BusDefinition bus,
            VehicleShapeLayoutDefinition shapeDefinition,
            out VehicleShapeCell nearestCell,
            out float distanceCells)
        {
            var position = new Vector2(
                bus.GridPosition.x + bus.PositionOffsetCells.x,
                bus.GridPosition.y + bus.PositionOffsetCells.y);
            return VehicleShapeLayoutEngine.TryFindNearestShapeCell(
                shapeDefinition,
                position,
                out nearestCell,
                out distanceCells);
        }

        private static int CountMediumLargeVehicles(IReadOnlyList<BusDefinition> buses)
        {
            var count = 0;
            if (buses == null)
            {
                return count;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index].Size != BusSize.Small)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountLargeVehicles(IReadOnlyList<BusDefinition> buses)
        {
            var count = 0;
            if (buses == null)
            {
                return count;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index].Size == BusSize.Large)
                {
                    count++;
                }
            }

            return count;
        }

        private static HashSet<int> BuildVisualPreviewProtectedLineIndices(
            IReadOnlyList<BusDefinition> buses,
            LevelDifficultyProfile profile,
            int layoutVariantIndex)
        {
            var protectedIndices = new HashSet<int>();
            if (buses == null ||
                buses.Count == 0 ||
                !VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex) ||
                (VehicleShapeLibraryId)libraryIndex != VehicleShapeLibraryId.Star ||
                !VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                    profile,
                    Mathf.Max(1, buses.Count),
                    layoutVariantIndex,
                    out var shapeDefinition))
            {
                return protectedIndices;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                var position = new Vector2(
                    bus.GridPosition.x + bus.PositionOffsetCells.x,
                    bus.GridPosition.y + bus.PositionOffsetCells.y);
                if (VehicleShapeLayoutEngine.TryFindNearestShapeCell(
                        shapeDefinition,
                        position,
                        out var nearestCell,
                        out var distanceCells) &&
                    nearestCell.Role != VehicleShapeCellRole.Fill &&
                    distanceCells <= 0.95f)
                {
                    protectedIndices.Add(index);
                }
            }

            return protectedIndices;
        }

        private static bool HasOpeningMove(IReadOnlyList<BusDefinition> buses, int vehicleIndex)
        {
            var active = new bool[buses.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
            }

            return LevelVehicleExitPlanner.IsPathClear(vehicleIndex, buses, active, out _);
        }

        public static bool IsVehiclePathClearForValidation(
            IReadOnlyList<BusDefinition> buses,
            int vehicleIndex)
        {
            return buses != null &&
                vehicleIndex >= 0 &&
                vehicleIndex < buses.Count &&
                HasOpeningMove(buses, vehicleIndex);
        }

        private static bool TryFindOpeningReductionMove(
            IReadOnlyList<BusDefinition> buses,
            HashSet<int> protectedLineIndices,
            int minimumOpeningMoves,
            int maximumOpeningMoves,
            int currentOpeningMoves,
            out int adjustedIndex,
            out BusDefinition adjustedBus,
            out int adjustedOpeningMoveCount)
        {
            adjustedIndex = -1;
            adjustedBus = default;
            adjustedOpeningMoveCount = currentOpeningMoves;
            var bestScore = float.PositiveInfinity;
            var openingIndices = GetOpeningMoveIndices(buses);
            openingIndices.Sort((left, right) =>
                GetOpeningReductionVehiclePriority(buses[left]).CompareTo(GetOpeningReductionVehiclePriority(buses[right])));

            for (var openingIndex = 0; openingIndex < openingIndices.Count; openingIndex++)
            {
                var vehicleIndex = openingIndices[openingIndex];
                if (protectedLineIndices != null && protectedLineIndices.Contains(vehicleIndex))
                {
                    continue;
                }

                var bus = buses[vehicleIndex];
                var directions = GetVisualPreviewBlockingDirections(bus);
                for (var directionIndex = 0; directionIndex < directions.Count; directionIndex++)
                {
                    var direction = directions[directionIndex];
                    var offsets = GetVisualPreviewBlockingOffsets(bus);
                    for (var offsetIndex = 0; offsetIndex < offsets.Count; offsetIndex++)
                    {
                        var candidate = new BusDefinition(
                            bus.Color,
                            bus.Size,
                            direction,
                            bus.GridPosition,
                            0f,
                            offsets[offsetIndex],
                            bus.StartsConcealed);
                        if (!IsWithinRecommendedBoardBounds(candidate))
                        {
                            continue;
                        }

                        if (IsSamePose(bus, candidate))
                        {
                            continue;
                        }

                        var adjusted = new List<BusDefinition>(buses);
                        adjusted[vehicleIndex] = candidate;
                        if (HasVehicleStartOverlap(adjusted, vehicleIndex))
                        {
                            continue;
                        }

                        var openingMoves = CountOpeningMoves(adjusted);
                        if (openingMoves >= currentOpeningMoves || openingMoves < minimumOpeningMoves)
                        {
                            continue;
                        }

                        if (!HasGreedyExitOrder(adjusted))
                        {
                            continue;
                        }

                        var score = GetOpeningReductionMoveScore(
                            bus,
                            candidate,
                            openingMoves,
                            maximumOpeningMoves,
                            directionIndex,
                            offsetIndex);
                        if (score >= bestScore)
                        {
                            continue;
                        }

                        bestScore = score;
                        adjustedIndex = vehicleIndex;
                        adjustedBus = candidate;
                        adjustedOpeningMoveCount = openingMoves;
                    }
                }
            }

            return adjustedIndex >= 0;
        }

        private static List<int> GetOpeningMoveIndices(IReadOnlyList<BusDefinition> buses)
        {
            var indices = new List<int>();
            if (buses == null || buses.Count == 0)
            {
                return indices;
            }

            var active = new bool[buses.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                if (LevelVehicleExitPlanner.IsPathClear(index, buses, active, out _))
                {
                    indices.Add(index);
                }
            }

            return indices;
        }

        private static List<GridDirection> GetVisualPreviewBlockingDirections(BusDefinition bus)
        {
            var directions = GetVisualPreviewOpeningDirections(bus);
            directions.Reverse();
            return directions;
        }

        private static List<Vector2> GetVisualPreviewBlockingOffsets(BusDefinition bus)
        {
            var offsets = new List<Vector2> { bus.PositionOffsetCells };
            if (bus.PositionOffsetCells.sqrMagnitude > 0.0001f)
            {
                offsets.Add(Vector2.zero);
            }

            return offsets;
        }

        private static float GetOpeningReductionVehiclePriority(BusDefinition bus)
        {
            var sizePenalty = bus.Size == BusSize.Small
                ? 0f
                : bus.Size == BusSize.Medium
                    ? 2f
                    : 6f;
            var offsetBonus = bus.PositionOffsetCells.sqrMagnitude > 0.04f ? -0.5f : 0f;
            return sizePenalty + GetNearestBoardEdgeDistance(bus.GridPosition) * 0.05f + offsetBonus;
        }

        private static int GetNearestBoardEdgeDistance(Vector2Int gridPosition)
        {
            return Mathf.Min(
                Mathf.Min(gridPosition.x, BoardLayoutConfig.GridColumns - 1 - gridPosition.x),
                Mathf.Min(gridPosition.y, BoardLayoutConfig.GridRows - 1 - gridPosition.y));
        }

        private static float GetOpeningReductionMoveScore(
            BusDefinition original,
            BusDefinition candidate,
            int openingMoves,
            int maximumOpeningMoves,
            int directionIndex,
            int offsetIndex)
        {
            var rangeScore = openingMoves > maximumOpeningMoves
                ? (openingMoves - maximumOpeningMoves) * 1000f
                : (maximumOpeningMoves - openingMoves) * 20f;
            var sizePenalty = original.Size == BusSize.Small
                ? 0f
                : original.Size == BusSize.Medium
                    ? 4f
                    : 16f;
            var offsetPenalty = (candidate.PositionOffsetCells - original.PositionOffsetCells).sqrMagnitude * 12f;
            return rangeScore + sizePenalty + directionIndex * 0.5f + offsetIndex * 0.25f + offsetPenalty;
        }

        private static bool IsSamePose(BusDefinition left, BusDefinition right)
        {
            return left.Direction == right.Direction &&
                Mathf.Abs(left.AngleOffsetDegrees - right.AngleOffsetDegrees) <= 0.001f &&
                (left.PositionOffsetCells - right.PositionOffsetCells).sqrMagnitude <= 0.000001f;
        }

        private static bool HasVehicleStartOverlap(IReadOnlyList<BusDefinition> buses, int vehicleIndex)
        {
            if (buses == null || vehicleIndex < 0 || vehicleIndex >= buses.Count)
            {
                return true;
            }

            var footprint = BoardLayoutConfig.GetVehicleFootprintCells(buses[vehicleIndex]);
            var visualFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(buses[vehicleIndex]);
            for (var index = 0; index < buses.Count; index++)
            {
                if (index == vehicleIndex)
                {
                    continue;
                }

                if (footprint.Overlaps(BoardLayoutConfig.GetVehicleFootprintCells(buses[index])) ||
                    visualFootprint.Overlaps(BoardLayoutConfig.GetVehicleVisualFootprintCells(buses[index])))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryRepairGreedyOrderedVehicles(
            IReadOnlyList<BusDefinition> buses,
            HashSet<int> protectedLineIndices,
            out List<BusDefinition> orderedVehicles)
        {
            orderedVehicles = new List<BusDefinition>();
            if (buses == null || buses.Count == 0)
            {
                return false;
            }

            var repaired = new List<BusDefinition>(buses);
            var active = new bool[repaired.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
            }

            var exitOrder = new List<int>(repaired.Count);
            var safety = repaired.Count * 3;
            while (exitOrder.Count < repaired.Count && safety-- > 0)
            {
                var removedAny = false;
                for (var index = 0; index < repaired.Count; index++)
                {
                    if (!active[index] ||
                        !LevelVehicleExitPlanner.IsPathClear(index, repaired, active, out _))
                    {
                        continue;
                    }

                    active[index] = false;
                    exitOrder.Add(index);
                    removedAny = true;
                }

                if (removedAny)
                {
                    continue;
                }

                if (!TryFindGreedyRepairMove(
                        repaired,
                        active,
                        protectedLineIndices,
                        out var repairedIndex,
                        out var repairedBus))
                {
                    return false;
                }

                repaired[repairedIndex] = repairedBus;
                if (!LevelVehicleExitPlanner.IsPathClear(repairedIndex, repaired, active, out _))
                {
                    return false;
                }

                active[repairedIndex] = false;
                exitOrder.Add(repairedIndex);
            }

            if (exitOrder.Count != repaired.Count)
            {
                return false;
            }

            for (var index = 0; index < exitOrder.Count; index++)
            {
                orderedVehicles.Add(repaired[exitOrder[index]]);
            }

            return true;
        }

        private static bool TryFindGreedyRepairMove(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<bool> active,
            HashSet<int> protectedLineIndices,
            out int repairedIndex,
            out BusDefinition repairedBus)
        {
            repairedIndex = -1;
            repairedBus = default;
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < buses.Count; index++)
            {
                if (protectedLineIndices != null && protectedLineIndices.Contains(index))
                {
                    continue;
                }

                if (active != null && (index < 0 || index >= active.Count || !active[index]))
                {
                    continue;
                }

                var bus = buses[index];
                var directions = GetVisualPreviewOpeningDirections(bus);
                for (var directionIndex = 0; directionIndex < directions.Count; directionIndex++)
                {
                    var direction = directions[directionIndex];
                    for (var offsetIndex = 0; offsetIndex < 3; offsetIndex++)
                    {
                        var candidate = new BusDefinition(
                            bus.Color,
                            bus.Size,
                            direction,
                            bus.GridPosition,
                            0f,
                            GetVisualPreviewOpeningOffset(direction, offsetIndex),
                            bus.StartsConcealed);
                        if (!IsWithinRecommendedBoardBounds(candidate))
                        {
                            continue;
                        }

                        var adjusted = new List<BusDefinition>(buses);
                        adjusted[index] = candidate;
                        if (HasVehicleStartOverlap(adjusted, index) ||
                            !LevelVehicleExitPlanner.IsPathClear(index, adjusted, active, out _))
                        {
                            continue;
                        }

                        var score = GetVisualPreviewEdgeScore(bus.GridPosition, direction) +
                            directionIndex * 0.15f +
                            offsetIndex * 0.05f;
                        if (score >= bestScore)
                        {
                            continue;
                        }

                        bestScore = score;
                        repairedIndex = index;
                        repairedBus = candidate;
                    }
                }
            }

            return repairedIndex >= 0;
        }

        private readonly struct VisualPreviewOpeningCandidate
        {
            public VisualPreviewOpeningCandidate(int vehicleIndex, BusDefinition bus, float score)
            {
                VehicleIndex = vehicleIndex;
                Bus = bus;
                Score = score;
            }

            public int VehicleIndex { get; }
            public BusDefinition Bus { get; }
            public float Score { get; }
        }

        private readonly struct VisualPreviewDirectionDistance
        {
            public VisualPreviewDirectionDistance(GridDirection direction, float distance)
            {
                Direction = direction;
                Distance = distance;
            }

            public GridDirection Direction { get; }
            public float Distance { get; }
        }

        private static BusSize PickPatternSize(
            LevelDifficultyProfile profile,
            System.Random random,
            VehicleLayoutSlot slot,
            int layoutVariantIndex)
        {
            if (VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out _))
            {
                if (slot.ShapeRole != VehicleShapeCellRole.Fill)
                {
                    if (profile.Difficulty == LevelDifficulty.SuperHard &&
                        profile.TargetVehicleCount >= 46 &&
                        random.NextDouble() < 0.10d)
                    {
                        return BusSize.Large;
                    }

                    var mediumChance = profile.Difficulty == LevelDifficulty.Normal ? 0.22d : 0.34d;
                    return profile.TargetVehicleCount >= 34 && random.NextDouble() < mediumChance
                        ? BusSize.Medium
                        : BusSize.Small;
                }

                var largeChance = profile.Difficulty == LevelDifficulty.SuperHard
                    ? 0.20d
                    : profile.Difficulty == LevelDifficulty.Hard
                        ? 0.15d
                        : 0.12d;
                var fillMediumChance = profile.Difficulty == LevelDifficulty.Normal ? 0.34d : 0.46d;
                return profile.TargetVehicleCount >= 42 && random.NextDouble() < largeChance
                    ? BusSize.Large
                    : profile.TargetVehicleCount >= 34 && random.NextDouble() < fillMediumChance
                        ? BusSize.Medium
                        : BusSize.Small;
            }

            if (slot.ShapeKind == VehicleShapeLayoutKind.None)
            {
                return PickSize(profile.Difficulty, random);
            }

            if (slot.ShapeRole != VehicleShapeCellRole.Fill)
            {
                return BusSize.Small;
            }

            return profile.TargetVehicleCount >= 42 && random.NextDouble() < 0.18d
                ? BusSize.Medium
                : BusSize.Small;
        }

        private static bool TryCreatePatternVehicle(
            LevelDifficultyProfile profile,
            PuzzleColor color,
            BusSize preferredSize,
            VehicleLayoutSlot slot,
            IReadOnlyList<BusDefinition> placedVehicles,
            IReadOnlyList<GarageDefinition> garages,
            out BusDefinition vehicle)
        {
            if (TryCreatePatternVehicle(color, preferredSize, slot, placedVehicles, garages, out vehicle))
            {
                return true;
            }

            if (preferredSize == BusSize.Large &&
                TryCreatePatternVehicle(color, BusSize.Medium, slot, placedVehicles, garages, out vehicle))
            {
                return true;
            }

            if (preferredSize != BusSize.Small &&
                TryCreatePatternVehicle(color, BusSize.Small, slot, placedVehicles, garages, out vehicle))
            {
                return true;
            }

            return false;
        }

        private static bool TryCreatePatternVehicle(
            PuzzleColor color,
            BusSize size,
            VehicleLayoutSlot slot,
            IReadOnlyList<BusDefinition> placedVehicles,
            IReadOnlyList<GarageDefinition> garages,
            out BusDefinition vehicle)
        {
            vehicle = new BusDefinition(
                color,
                size,
                slot.Direction,
                slot.GridPosition,
                slot.AngleOffsetDegrees,
                slot.PositionOffsetCells);

            return IsPlaceable(vehicle, placedVehicles, garages);
        }

        private static bool TryPlaceVehicle(
            LevelDifficultyProfile profile,
            System.Random random,
            IReadOnlyList<BusDefinition> placedVehicles,
            IReadOnlyList<PuzzleColor> colors,
            IReadOnlyList<GarageDefinition> garages,
            int vehicleIndex,
            out BusDefinition vehicle)
        {
            for (var attempt = 0; attempt < MaxPlacementAttemptsPerVehicle; attempt++)
            {
                var size = PickSize(profile.Difficulty, random);
                var direction = PickDirection(random);
                var color = colors[vehicleIndex % colors.Count];
                var position = PickGridPosition(profile.ParkingTension, random);
                var angleOffsetDegrees = PickAngleOffset(profile, random);
                var positionOffset = PickPositionOffset(profile.ParkingTension, random);
                var candidate = new BusDefinition(color, size, direction, position, angleOffsetDegrees, positionOffset);

                if (IsPlaceable(candidate, placedVehicles, garages))
                {
                    vehicle = candidate;
                    return true;
                }
            }

            vehicle = default;
            return false;
        }

        private static bool IsPlaceable(
            BusDefinition candidate,
            IReadOnlyList<BusDefinition> placedVehicles,
            IReadOnlyList<GarageDefinition> garages)
        {
            if (!BoardLayoutConfig.IsInsideGrid(candidate.GridPosition) || IsNearBoardEdge(candidate))
            {
                return false;
            }

            var candidateFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(candidate);
            for (var index = 0; index < placedVehicles.Count; index++)
            {
                var otherFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(placedVehicles[index]);
                if (candidateFootprint.IsWithinPadding(otherFootprint, BoardLayoutConfig.VehicleNearPaddingCells))
                {
                    return false;
                }

                if (CreatesMutualPathBlock(candidate, placedVehicles[index]))
                {
                    return false;
                }
            }

            if (garages != null)
            {
                for (var index = 0; index < garages.Count; index++)
                {
                    if (IsVehicleTooCloseToGarage(candidateFootprint, garages[index]))
                    {
                        return false;
                    }

                    if (CreatesMutualPathBlock(candidate, garages[index].FrontVehicle))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool CreatesMutualPathBlock(BusDefinition first, BusDefinition second)
        {
            var pair = new List<BusDefinition> { first, second };
            var active = new[] { true, true };
            return !LevelVehicleExitPlanner.IsPathClear(0, pair, active, out var firstBlockingIndex) &&
                firstBlockingIndex == 1 &&
                !LevelVehicleExitPlanner.IsPathClear(1, pair, active, out var secondBlockingIndex) &&
                secondBlockingIndex == 0;
        }

        private static bool IsWithinRecommendedBoardBounds(BusDefinition vehicle)
        {
            var footprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(vehicle);
            const float minBoundary = -0.66f;
            var maxXBoundary = BoardLayoutConfig.GridColumns - 0.34f;
            var maxYBoundary = BoardLayoutConfig.GridRows - 0.34f;
            return footprint.ProjectMin(Vector2.right) >= minBoundary &&
                footprint.ProjectMax(Vector2.right) <= maxXBoundary &&
                footprint.ProjectMin(Vector2.up) >= minBoundary &&
                footprint.ProjectMax(Vector2.up) <= maxYBoundary;
        }

        private static bool IsVehicleTooCloseToGarage(VehicleFootprint vehicleFootprint, GarageDefinition garage)
        {
            if (vehicleFootprint.IsWithinPadding(GetGarageFootprint(garage), BoardLayoutConfig.VehicleNearPaddingCells))
            {
                return true;
            }

            foreach (var garageVehicle in garage.EnumerateVehicles())
            {
                if (vehicleFootprint.IsWithinPadding(
                    BoardLayoutConfig.GetVehicleVisualFootprintCells(garageVehicle),
                    BoardLayoutConfig.VehicleNearPaddingCells))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNearBoardEdge(BusDefinition vehicle)
        {
            var footprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(vehicle);
            const float boundaryPadding = 0.18f;
            return footprint.ProjectMin(Vector2.right) < -boundaryPadding ||
                footprint.ProjectMax(Vector2.right) > BoardLayoutConfig.GridColumns - 1f + boundaryPadding ||
                footprint.ProjectMin(Vector2.up) < -boundaryPadding ||
                footprint.ProjectMax(Vector2.up) > BoardLayoutConfig.GridRows - 1f + boundaryPadding;
        }

        private static List<SolutionBusStepDefinition> BuildSolutionRoute(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses)
        {
            var route = new List<SolutionBusStepDefinition>();
            if (buses == null)
            {
                return route;
            }

            LevelVehicleExitPlanner.TryFindExitOrder(buses, out var exitOrder, out _);
            var orderedIndices = BuildCompleteOrder(buses.Count, exitOrder);
            for (var index = 0; index < orderedIndices.Count; index++)
            {
                var bus = buses[orderedIndices[index]];
                route.Add(new SolutionBusStepDefinition(bus.Color, bus.Size, GetPreferredGroupUnits(profile, bus)));
            }

            return route;
        }

        private static List<SolutionBusStepDefinition> BuildVehicleOrderRoute(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses)
        {
            var route = new List<SolutionBusStepDefinition>();
            if (buses == null)
            {
                return route;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                route.Add(new SolutionBusStepDefinition(bus.Color, bus.Size, GetPreferredGroupUnits(profile, bus)));
            }

            return route;
        }

        private static List<SolutionBusStepDefinition> BuildSolutionRoute(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages)
        {
            var route = new List<SolutionBusStepDefinition>();
            var state = SolutionRouteState.Create(buses, garages);

            var removedAny = true;
            while (removedAny && state.HasActiveVehicles)
            {
                removedAny = false;

                for (var index = 0; index < state.Vehicles.Count; index++)
                {
                    if (!state.Active[index] ||
                        !LevelVehicleExitPlanner.IsPathClear(index, state.Vehicles, state.Active, state.ActiveGarageObstacles, out _))
                    {
                        continue;
                    }

                    var bus = state.Vehicles[index];
                    route.Add(new SolutionBusStepDefinition(bus.Color, bus.Size, GetPreferredGroupUnits(profile, bus)));
                    state.RemoveVehicle(index);
                    removedAny = true;
                }
            }

            AppendMissingRouteVehicles(profile, route, state);
            return route;
        }

        private static List<int> BuildCompleteOrder(int count, IReadOnlyList<int> preferredOrder)
        {
            var indices = new List<int>();
            if (preferredOrder != null)
            {
                for (var index = 0; index < preferredOrder.Count; index++)
                {
                    var vehicleIndex = preferredOrder[index];
                    if (vehicleIndex >= 0 && vehicleIndex < count && !indices.Contains(vehicleIndex))
                    {
                        indices.Add(vehicleIndex);
                    }
                }
            }

            for (var vehicleIndex = 0; vehicleIndex < count; vehicleIndex++)
            {
                if (!indices.Contains(vehicleIndex))
                {
                    indices.Add(vehicleIndex);
                }
            }

            return indices;
        }

        private static int GetPreferredGroupUnits(LevelDifficultyProfile profile, BusDefinition bus)
        {
            var rule = GetPassengerFlowRule(profile);
            return Mathf.Clamp(bus.CapacityUnits, rule.MinGroupUnits, rule.MaxGroupUnits);
        }

        private static PassengerFlowDifficultyRule GetPassengerFlowRule(LevelDifficultyProfile profile)
        {
            return profile != null
                ? profile.PassengerFlowRule
                : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal).PassengerFlowRule;
        }

        private static void AppendMissingRouteVehicles(
            LevelDifficultyProfile profile,
            List<SolutionBusStepDefinition> route,
            SolutionRouteState state)
        {
            if (state == null)
            {
                return;
            }

            for (var index = 0; index < state.Vehicles.Count; index++)
            {
                if (!state.Active[index])
                {
                    continue;
                }

                var bus = state.Vehicles[index];
                route.Add(new SolutionBusStepDefinition(bus.Color, bus.Size, GetPreferredGroupUnits(profile, bus)));
            }

            var queuedVehicles = state.GetQueuedGarageVehicles();
            for (var index = 0; index < queuedVehicles.Count; index++)
            {
                var bus = queuedVehicles[index];
                route.Add(new SolutionBusStepDefinition(bus.Color, bus.Size, GetPreferredGroupUnits(profile, bus)));
            }
        }

        private static List<PuzzleColor> PickColorSet(int targetColorCount)
        {
            var colors = new List<PuzzleColor>();
            var count = Mathf.Clamp(targetColorCount, 2, ColorPool.Length);
            for (var index = 0; index < count; index++)
            {
                colors.Add(ColorPool[index]);
            }

            return colors;
        }

        private static BusSize PickSize(LevelDifficulty difficulty, System.Random random)
        {
            var roll = random.NextDouble();
            switch (difficulty)
            {
                case LevelDifficulty.SuperHard:
                    if (roll < 0.22d)
                    {
                        return BusSize.Small;
                    }

                    return roll < 0.66d ? BusSize.Medium : BusSize.Large;
                case LevelDifficulty.Hard:
                    if (roll < 0.30d)
                    {
                        return BusSize.Small;
                    }

                    return roll < 0.74d ? BusSize.Medium : BusSize.Large;
                default:
                    if (roll < 0.42d)
                    {
                        return BusSize.Small;
                    }

                    return roll < 0.84d ? BusSize.Medium : BusSize.Large;
            }
        }

        private static GridDirection PickDirection(System.Random random)
        {
            return (GridDirection)random.Next(0, 4);
        }

        private static Vector2Int PickGridPosition(float parkingTension, System.Random random)
        {
            var margin = parkingTension > 0.65f ? 1 : 2;
            var x = random.Next(margin, BoardLayoutConfig.GridColumns - margin);
            var y = random.Next(margin, BoardLayoutConfig.GridRows - margin);
            return new Vector2Int(x, y);
        }

        private static float PickAngleOffset(LevelDifficultyProfile profile, System.Random random)
        {
            var maxAngle = Mathf.Lerp(2f, 12f, profile.ParkingTension);
            var steps = Mathf.FloorToInt(maxAngle / 4f);
            if (steps <= 0)
            {
                return 0f;
            }

            return random.Next(-steps, steps + 1) * 4f;
        }

        private static Vector2 PickPositionOffset(float parkingTension, System.Random random)
        {
            var maxOffset = Mathf.Lerp(0.01f, 0.08f, parkingTension);
            return new Vector2(
                Mathf.Lerp(-maxOffset, maxOffset, (float)random.NextDouble()),
                Mathf.Lerp(-maxOffset, maxOffset, (float)random.NextDouble()));
        }

        private static List<GarageDefinition> BuildGarages(
            StageGenerationRequest request,
            GarageGenerationRule garageRule,
            System.Random random,
            IReadOnlyList<PuzzleColor> colors)
        {
            var garages = new List<GarageDefinition>();
            if (request.GarageCount <= 0 || garageRule == null || !garageRule.Enabled)
            {
                return garages;
            }

            var vehicleCursor = 0;
            for (var garageIndex = 0; garageIndex < request.GarageCount; garageIndex++)
            {
                if (!TryPlaceGarage(request, random, colors, garages, ref vehicleCursor, out var garage))
                {
                    continue;
                }

                garages.Add(garage);
            }

            return garages;
        }

        private static bool TryPlaceGarage(
            StageGenerationRequest request,
            System.Random random,
            IReadOnlyList<PuzzleColor> colors,
            IReadOnlyList<GarageDefinition> placedGarages,
            ref int vehicleCursor,
            out GarageDefinition garage)
        {
            for (var attempt = 0; attempt < MaxPlacementAttemptsPerVehicle; attempt++)
            {
                var localVehicleCursor = vehicleCursor;
                var exitDirection = PickDirection(random);
                var garageCell = PickGarageGridPosition(exitDirection, random);
                var frontCell = garageCell + GridDirectionUtility.ToGridVector(exitDirection);
                if (!BoardLayoutConfig.IsInsideGrid(frontCell))
                {
                    continue;
                }

                var frontVehicle = CreateGarageVehicle(request, random, colors, localVehicleCursor++, exitDirection, frontCell);
                var queuedCount = random.Next(request.MinGarageQueuedVehicles, request.MaxGarageQueuedVehicles + 1);
                var queuedVehicles = new List<BusDefinition>();
                for (var queueIndex = 0; queueIndex < queuedCount; queueIndex++)
                {
                    queuedVehicles.Add(CreateGarageVehicle(request, random, colors, localVehicleCursor++, exitDirection, frontCell));
                }

                var candidate = new GarageDefinition(garageCell, exitDirection, frontVehicle, queuedVehicles);
                if (IsGaragePlaceable(candidate, placedGarages))
                {
                    vehicleCursor = localVehicleCursor;
                    garage = candidate;
                    return true;
                }
            }

            garage = default;
            return false;
        }

        private static BusDefinition CreateGarageVehicle(
            StageGenerationRequest request,
            System.Random random,
            IReadOnlyList<PuzzleColor> colors,
            int vehicleIndex,
            GridDirection exitDirection,
            Vector2Int frontCell)
        {
            var color = colors[vehicleIndex % colors.Count];
            var size = PickSize(request.Difficulty, random);
            var angleOffset = PickAngleOffset(request.Profile, random) * 0.35f;
            return new BusDefinition(color, size, exitDirection, frontCell, angleOffset, Vector2.zero);
        }

        private static Vector2Int PickGarageGridPosition(GridDirection exitDirection, System.Random random)
        {
            var margin = 1;
            var x = random.Next(margin, BoardLayoutConfig.GridColumns - margin);
            var y = random.Next(margin, BoardLayoutConfig.GridRows - margin);
            var cell = new Vector2Int(x, y);

            if (exitDirection == GridDirection.Left)
            {
                cell.x = Mathf.Max(1, cell.x);
            }
            else if (exitDirection == GridDirection.Right)
            {
                cell.x = Mathf.Min(BoardLayoutConfig.GridColumns - 2, cell.x);
            }
            else if (exitDirection == GridDirection.Down)
            {
                cell.y = Mathf.Max(1, cell.y);
            }
            else if (exitDirection == GridDirection.Up)
            {
                cell.y = Mathf.Min(BoardLayoutConfig.GridRows - 2, cell.y);
            }

            return cell;
        }

        private static bool IsGaragePlaceable(GarageDefinition candidate, IReadOnlyList<GarageDefinition> placedGarages)
        {
            var garageFootprint = GetGarageFootprint(candidate);
            var frontFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(candidate.FrontVehicle);
            if (garageFootprint.Overlaps(frontFootprint))
            {
                return false;
            }

            if (DoesGarageVehicleOverlapFootprint(candidate, garageFootprint))
            {
                return false;
            }

            for (var index = 0; index < placedGarages.Count; index++)
            {
                var placedGarageFootprint = GetGarageFootprint(placedGarages[index]);
                if (garageFootprint.IsWithinPadding(placedGarageFootprint, BoardLayoutConfig.VehicleNearPaddingCells) ||
                    DoesGarageVehicleConflictWithFootprint(candidate, placedGarageFootprint) ||
                    DoesGarageVehicleConflictWithGarage(candidate, placedGarages[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DoesGarageVehicleConflictWithFootprint(GarageDefinition garage, VehicleFootprint footprint)
        {
            foreach (var vehicle in garage.EnumerateVehicles())
            {
                if (BoardLayoutConfig.GetVehicleVisualFootprintCells(vehicle).IsWithinPadding(
                    footprint,
                    BoardLayoutConfig.VehicleNearPaddingCells))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DoesGarageVehicleOverlapFootprint(GarageDefinition garage, VehicleFootprint footprint)
        {
            foreach (var vehicle in garage.EnumerateVehicles())
            {
                if (BoardLayoutConfig.GetVehicleVisualFootprintCells(vehicle).Overlaps(footprint))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DoesGarageVehicleConflictWithGarage(GarageDefinition first, GarageDefinition second)
        {
            foreach (var firstVehicle in first.EnumerateVehicles())
            {
                var firstFootprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(firstVehicle);
                if (firstFootprint.IsWithinPadding(GetGarageFootprint(second), BoardLayoutConfig.VehicleNearPaddingCells))
                {
                    return true;
                }

                foreach (var secondVehicle in second.EnumerateVehicles())
                {
                    if (firstFootprint.IsWithinPadding(
                        BoardLayoutConfig.GetVehicleVisualFootprintCells(secondVehicle),
                        BoardLayoutConfig.VehicleNearPaddingCells))
                    {
                        return true;
                    }
                }
            }

            var firstGarageFootprint = GetGarageFootprint(first);
            foreach (var secondVehicle in second.EnumerateVehicles())
            {
                if (BoardLayoutConfig.GetVehicleVisualFootprintCells(secondVehicle).IsWithinPadding(
                    firstGarageFootprint,
                    BoardLayoutConfig.VehicleNearPaddingCells))
                {
                    return true;
                }
            }

            return false;
        }

        private static VehicleFootprint GetGarageFootprint(GarageDefinition garage)
        {
            return new VehicleFootprint(
                new Vector3(garage.GridPosition.x, 0f, garage.GridPosition.y),
                Vector3.right,
                Vector3.forward,
                0.45f,
                0.45f);
        }

        private static int CountGarageVehicles(IReadOnlyList<GarageDefinition> garages)
        {
            var count = 0;
            if (garages == null)
            {
                return count;
            }

            for (var index = 0; index < garages.Count; index++)
            {
                count += garages[index].TotalVehicleCount;
            }

            return count;
        }

        private sealed class SolutionRouteState
        {
            public readonly List<BusDefinition> Vehicles = new List<BusDefinition>();
            public readonly List<GarageDefinition> ActiveGarageObstacles = new List<GarageDefinition>();
            public bool[] Active;

            private readonly int[] garageIndexByVehicle;
            private readonly List<GarageDefinition> garages = new List<GarageDefinition>();
            private readonly List<Queue<BusDefinition>> garageQueues = new List<Queue<BusDefinition>>();

            private SolutionRouteState(int vehicleCount)
            {
                Active = new bool[vehicleCount];
                garageIndexByVehicle = new int[vehicleCount];
                for (var index = 0; index < garageIndexByVehicle.Length; index++)
                {
                    garageIndexByVehicle[index] = -1;
                }
            }

            public bool HasActiveVehicles
            {
                get
                {
                    for (var index = 0; index < Active.Length; index++)
                    {
                        if (Active[index])
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public static SolutionRouteState Create(
                IReadOnlyList<BusDefinition> buses,
                IReadOnlyList<GarageDefinition> garages)
            {
                var busCount = buses != null ? buses.Count : 0;
                var garageCount = garages != null ? garages.Count : 0;
                var state = new SolutionRouteState(busCount + garageCount);

                if (buses != null)
                {
                    for (var index = 0; index < buses.Count; index++)
                    {
                        state.Vehicles.Add(buses[index]);
                        state.Active[index] = true;
                    }
                }

                if (garages != null)
                {
                    for (var garageIndex = 0; garageIndex < garages.Count; garageIndex++)
                    {
                        var garage = garages[garageIndex];
                        var vehicleIndex = state.Vehicles.Count;
                        state.Vehicles.Add(garage.FrontVehicle);
                        state.Active[vehicleIndex] = true;
                        state.garageIndexByVehicle[vehicleIndex] = garageIndex;
                        state.garages.Add(garage);
                        state.garageQueues.Add(new Queue<BusDefinition>(garage.QueuedVehicles));
                        state.ActiveGarageObstacles.Add(garage);
                    }
                }

                return state;
            }

            public void RemoveVehicle(int vehicleIndex)
            {
                var garageIndex = garageIndexByVehicle[vehicleIndex];
                if (garageIndex < 0)
                {
                    Active[vehicleIndex] = false;
                    return;
                }

                var queue = garageQueues[garageIndex];
                if (queue.Count == 0)
                {
                    Active[vehicleIndex] = false;
                    RemoveGarageObstacle(garageIndex);
                    return;
                }

                Vehicles[vehicleIndex] = queue.Dequeue();
                if (queue.Count == 0)
                {
                    RemoveGarageObstacle(garageIndex);
                }
            }

            public List<BusDefinition> GetQueuedGarageVehicles()
            {
                var queuedVehicles = new List<BusDefinition>();
                for (var garageIndex = 0; garageIndex < garageQueues.Count; garageIndex++)
                {
                    foreach (var bus in garageQueues[garageIndex])
                    {
                        queuedVehicles.Add(bus);
                    }
                }

                return queuedVehicles;
            }

            private void RemoveGarageObstacle(int garageIndex)
            {
                if (garageIndex < 0 || garageIndex >= garages.Count)
                {
                    return;
                }

                var garagePosition = garages[garageIndex].GridPosition;
                for (var index = ActiveGarageObstacles.Count - 1; index >= 0; index--)
                {
                    if (ActiveGarageObstacles[index].GridPosition == garagePosition)
                    {
                        ActiveGarageObstacles.RemoveAt(index);
                        return;
                    }
                }
            }
        }
    }
}
