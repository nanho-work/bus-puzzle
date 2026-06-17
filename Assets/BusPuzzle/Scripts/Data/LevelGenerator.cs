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
            bool useSolutionAnalyzer = true)
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
                request.VehicleLayoutVariantIndex);
            buses = ApplyMysteryVehicleModifiers(buses, request.MysteryVehicleProfile, seed + 1699);
            var flowPlan = BuildPassengerFlowPlan(request.Profile, buses, garages, seed);

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
            int layoutVariantIndex = -1)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);

            targetVehicleCount = Mathf.Clamp(targetVehicleCount, 1, 50);
            maxGenerationAttempts = Mathf.Clamp(maxGenerationAttempts, 1, DefaultMaxGenerationAttempts);
            var bestVehicles = new List<BusDefinition>();
            var bestExitCount = -1;

            for (var attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                var random = new System.Random(seed + attempt * 9973);
                var vehicles = TryBuildVehicleSet(profile, random, targetVehicleCount, garages, layoutVariantIndex);
                if (HasPlayableExitOrder(vehicles, garages, useSolutionAnalyzer, out var exitOrder))
                {
                    return vehicles;
                }

                if (exitOrder.Count > bestExitCount)
                {
                    bestExitCount = exitOrder.Count;
                    bestVehicles = vehicles;
                }
            }

            return bestVehicles;
        }

        private static bool HasPlayableExitOrder(
            IReadOnlyList<BusDefinition> vehicles,
            IReadOnlyList<GarageDefinition> garages,
            bool useSolutionAnalyzer,
            out List<int> exitOrder)
        {
            LevelVehicleExitPlanner.TryFindExitOrder(vehicles, out exitOrder, out _);
            if (!useSolutionAnalyzer)
            {
                return vehicles != null && vehicles.Count > 0 && exitOrder.Count == vehicles.Count;
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
            int layoutVariantIndex)
        {
            var vehicles = new List<BusDefinition>();
            var colors = PickColorSet(profile.TargetColorCount);
            TryPlacePatternVehicles(profile, random, targetVehicleCount, garages, colors, vehicles, layoutVariantIndex);

            for (var vehicleIndex = vehicles.Count; vehicleIndex < targetVehicleCount; vehicleIndex++)
            {
                if (!TryPlaceVehicle(profile, random, vehicles, colors, garages, vehicleIndex, out var vehicle))
                {
                    continue;
                }

                vehicles.Add(vehicle);
            }

            return vehicles;
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
                var preferredSize = PickSize(profile.Difficulty, random);
                if (TryCreatePatternVehicle(
                    profile,
                    colors[vehicleIndex % colors.Count],
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

            if (profile.Difficulty != LevelDifficulty.SuperHard &&
                preferredSize != BusSize.Small &&
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
                    if (roll < 0.36d)
                    {
                        return BusSize.Small;
                    }

                    return roll < 0.74d ? BusSize.Medium : BusSize.Large;
                case LevelDifficulty.Hard:
                    if (roll < 0.45d)
                    {
                        return BusSize.Small;
                    }

                    return roll < 0.80d ? BusSize.Medium : BusSize.Large;
                default:
                    if (roll < 0.60d)
                    {
                        return BusSize.Small;
                    }

                    return roll < 0.90d ? BusSize.Medium : BusSize.Large;
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
            var maxAngle = Mathf.Lerp(10f, 35f, profile.ParkingTension);
            var steps = Mathf.FloorToInt(maxAngle / 5f);
            if (steps <= 0)
            {
                return 0f;
            }

            return random.Next(-steps, steps + 1) * 5f;
        }

        private static Vector2 PickPositionOffset(float parkingTension, System.Random random)
        {
            var maxOffset = Mathf.Lerp(0.04f, 0.20f, parkingTension);
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
