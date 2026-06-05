using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public static class LevelGenerator
    {
        private const int MaxGenerationAttempts = 80;
        private const int MaxPlacementAttemptsPerVehicle = 420;

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
            PuzzleColor.SkyBlue
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

        public static List<BusDefinition> BuildVehicles(LevelDifficultyProfile profile, int seed)
        {
            profile = profile != null ? profile : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);

            var targetVehicleCount = profile.TargetVehicleCount;
            var bestVehicles = new List<BusDefinition>();
            var bestExitCount = -1;

            for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                var random = new System.Random(seed + attempt * 9973);
                var vehicles = TryBuildVehicleSet(profile, random, targetVehicleCount);
                if (LevelVehicleExitPlanner.TryFindExitOrder(vehicles, out var exitOrder, out _))
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

        public static PassengerFlowPlan BuildPassengerFlowPlan(
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses,
            int seed)
        {
            var flowPlan = new PassengerFlowPlan();
            var solutionRoute = BuildSolutionRoute(profile, buses);
            flowPlan.ConfigureRatioByDifficultyWithSolutionRoute(solutionRoute, seed, true);
            return flowPlan;
        }

        public static int GetRotaryCapacity(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return 35;
                case LevelDifficulty.SuperHard:
                    return 40;
                default:
                    return 30;
            }
        }

        public static RotaryRoadPresetId GetRoadPreset(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return RotaryRoadPresetId.Medium;
                case LevelDifficulty.SuperHard:
                    return RotaryRoadPresetId.Large;
                default:
                    return RotaryRoadPresetId.Small;
            }
        }

        private static List<BusDefinition> TryBuildVehicleSet(
            LevelDifficultyProfile profile,
            System.Random random,
            int targetVehicleCount)
        {
            var vehicles = new List<BusDefinition>();
            var colors = PickColorSet(profile.TargetColorCount);

            for (var vehicleIndex = 0; vehicleIndex < targetVehicleCount; vehicleIndex++)
            {
                if (!TryPlaceVehicle(profile, random, vehicles, colors, vehicleIndex, out var vehicle))
                {
                    continue;
                }

                vehicles.Add(vehicle);
            }

            return vehicles;
        }

        private static bool TryPlaceVehicle(
            LevelDifficultyProfile profile,
            System.Random random,
            IReadOnlyList<BusDefinition> placedVehicles,
            IReadOnlyList<PuzzleColor> colors,
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

                if (IsPlaceable(candidate, placedVehicles))
                {
                    vehicle = candidate;
                    return true;
                }
            }

            vehicle = default;
            return false;
        }

        private static bool IsPlaceable(BusDefinition candidate, IReadOnlyList<BusDefinition> placedVehicles)
        {
            if (!BoardLayoutConfig.IsInsideGrid(candidate.GridPosition) || IsNearBoardEdge(candidate))
            {
                return false;
            }

            var candidateFootprint = BoardLayoutConfig.GetVehicleFootprintCells(candidate);
            for (var index = 0; index < placedVehicles.Count; index++)
            {
                var otherFootprint = BoardLayoutConfig.GetVehicleFootprintCells(placedVehicles[index]);
                if (candidateFootprint.Overlaps(otherFootprint, BoardLayoutConfig.VehicleNearPaddingCells))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsNearBoardEdge(BusDefinition vehicle)
        {
            var footprint = BoardLayoutConfig.GetVehicleFootprintCells(vehicle);
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
            var rule = profile != null
                ? profile.PassengerFlowRule
                : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal).PassengerFlowRule;
            return Mathf.Clamp(bus.CapacityUnits, rule.MinGroupUnits, rule.MaxGroupUnits);
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
    }
}
