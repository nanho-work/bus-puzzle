using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public enum LevelValidationSeverity
    {
        Warning,
        Error
    }

    public readonly struct LevelValidationIssue
    {
        public readonly LevelValidationSeverity Severity;
        public readonly string Message;

        public LevelValidationIssue(LevelValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    public sealed class LevelValidationReport
    {
        private readonly List<LevelValidationIssue> issues = new List<LevelValidationIssue>();

        public IReadOnlyList<LevelValidationIssue> Issues => issues;
        public bool HasIssues => issues.Count > 0;
        public bool HasErrors
        {
            get
            {
                for (var index = 0; index < issues.Count; index++)
                {
                    if (issues[index].Severity == LevelValidationSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Add(LevelValidationSeverity severity, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                issues.Add(new LevelValidationIssue(severity, message));
            }
        }

        public string ToConsoleMessage(string levelName)
        {
            var prefix = HasErrors ? "validation errors" : "validation warnings";
            var lines = new List<string> { $"{levelName} {prefix}:" };
            for (var index = 0; index < issues.Count; index++)
            {
                lines.Add($"- {issues[index].Severity}: {issues[index].Message}");
            }

            return string.Join("\n", lines);
        }
    }

    public static class LevelValidator
    {
        private const float BoardBoundaryMin = -0.5f;
        private const float BoardBoundaryPadding = 0.16f;
        private const float MaxRecommendedPositionOffsetCells = 0.45f;
        private const float MaxRecommendedAngleOffsetDegrees = 35f;
        private const int VehicleExitNodeVisitLimit = 20000;

        public static LevelValidationReport Validate(
            LevelData levelData,
            bool validateVehicleExitSequence = true,
            int vehicleExitSolutionLimit = 256)
        {
            var report = new LevelValidationReport();
            if (levelData == null)
            {
                report.Add(LevelValidationSeverity.Error, "LevelData is missing.");
                return report;
            }

            var passengers = levelData.PassengerUnits;
            var buses = levelData.Buses;
            var allVehicles = levelData.AllVehicles;
            var profile = levelData.DifficultyProfile;

            ValidateBasicCounts(report, passengers, allVehicles);
            ValidateRotaryOpening(report, levelData, passengers);
            ValidateCapacityMatch(report, levelData);
            var usesShapeLibraryLayout = UsesShapeLibraryLayout(levelData);
            ValidateDifficultyProfile(report, profile, levelData.PassengerFlowPlan, passengers, allVehicles, usesShapeLibraryLayout);
            ValidateSolutionRoute(report, levelData.PassengerFlowPlan, allVehicles);
            ValidatePassengerRuns(report, profile, passengers, usesShapeLibraryLayout);
            ValidateVehiclePlacement(report, buses, usesShapeLibraryLayout);
            ValidateGaragePlacement(report, levelData.Garages, buses);
            if (validateVehicleExitSequence)
            {
                ValidateVehicleExitSequence(report, profile, buses, levelData.Garages, vehicleExitSolutionLimit);
            }

            return report;
        }

        private static void ValidateBasicCounts(
            LevelValidationReport report,
            IReadOnlyList<PuzzleColor> passengers,
            IReadOnlyList<BusDefinition> buses)
        {
            if (passengers == null || passengers.Count == 0)
            {
                report.Add(LevelValidationSeverity.Error, "No passenger units were generated.");
            }

            if (buses == null || buses.Count == 0)
            {
                report.Add(LevelValidationSeverity.Error, "No vehicles are defined.");
            }
        }

        private static void ValidateCapacityMatch(LevelValidationReport report, LevelData levelData)
        {
            if (levelData.TryGetCapacityMismatchMessage(out var mismatchMessage))
            {
                report.Add(LevelValidationSeverity.Error, mismatchMessage);
            }
        }

        private static void ValidateRotaryOpening(
            LevelValidationReport report,
            LevelData levelData,
            IReadOnlyList<PuzzleColor> passengers)
        {
            if (levelData == null || passengers == null || passengers.Count == 0)
            {
                return;
            }

            var rotaryCapacity = levelData.RotaryStartCapacity;
            if (passengers.Count < rotaryCapacity)
            {
                report.Add(
                    LevelValidationSeverity.Warning,
                    $"Starting rotary has {passengers.Count} passenger units for {rotaryCapacity} visible slots; opening may look empty.");
            }
        }

        private static void ValidateDifficultyProfile(
            LevelValidationReport report,
            LevelDifficultyProfile profile,
            PassengerFlowPlan flowPlan,
            IReadOnlyList<PuzzleColor> passengers,
            IReadOnlyList<BusDefinition> buses,
            bool usesShapeLibraryLayout)
        {
            if (profile == null)
            {
                report.Add(LevelValidationSeverity.Warning, "Difficulty profile is missing; Normal defaults will be used.");
                return;
            }

            var vehicleCount = buses?.Count ?? 0;
            var colorCount = CountUniqueBusColors(buses);
            var vehicleTolerance = GetVehicleCountTolerance(profile.Difficulty);
            var colorTolerance = GetColorCountTolerance(profile.Difficulty);

            if (!usesShapeLibraryLayout && Mathf.Abs(vehicleCount - profile.TargetVehicleCount) > vehicleTolerance)
            {
                report.Add(
                    LevelValidationSeverity.Warning,
                    $"Vehicle count {vehicleCount} is far from {profile.Difficulty} target {profile.TargetVehicleCount}.");
            }

            if (Mathf.Abs(colorCount - profile.TargetColorCount) > colorTolerance)
            {
                report.Add(
                    LevelValidationSeverity.Warning,
                    $"Vehicle color count {colorCount} is far from {profile.Difficulty} target {profile.TargetColorCount}.");
            }

            if (profile.RequireSolutionRoute && (flowPlan == null || flowPlan.SolutionRoute.Count == 0))
            {
                report.Add(LevelValidationSeverity.Warning, $"{profile.Difficulty} profile expects a solution route.");
            }

            if (flowPlan != null && flowPlan.Mode == PassengerFlowPlanMode.RatioByDifficulty && passengers != null && passengers.Count > 0)
            {
                var passengerColorCount = CountUniquePassengerColors(passengers);
                if (passengerColorCount != colorCount)
                {
                    report.Add(
                        LevelValidationSeverity.Warning,
                        $"Passenger color count {passengerColorCount} does not match vehicle color count {colorCount}.");
                }
            }
        }

        private static void ValidatePassengerRuns(
            LevelValidationReport report,
            LevelDifficultyProfile profile,
            IReadOnlyList<PuzzleColor> passengers,
            bool allowsShowcasePassengerRuns)
        {
            if (allowsShowcasePassengerRuns || profile == null || passengers == null || passengers.Count == 0)
            {
                return;
            }

            var longestRun = GetLongestColorRun(passengers);
            var maxExpectedRun = Mathf.Max(1, profile.PassengerFlowRule.MaxGroupUnits);
            if (longestRun > maxExpectedRun + GetRunTolerance(profile.Difficulty))
            {
                report.Add(
                    LevelValidationSeverity.Warning,
                    $"Longest passenger color run is {longestRun} units, above {profile.Difficulty} target group size {maxExpectedRun}.");
            }
        }

        private static void ValidateSolutionRoute(
            LevelValidationReport report,
            PassengerFlowPlan flowPlan,
            IReadOnlyList<BusDefinition> buses)
        {
            if (flowPlan == null || !flowPlan.Enabled || flowPlan.Mode != PassengerFlowPlanMode.SolutionRoute || flowPlan.SolutionRoute.Count == 0)
            {
                return;
            }

            var capacityByColor = CountBusCapacityUnitsByColor(buses);
            var routeUnitsByColor = new Dictionary<PuzzleColor, int>();

            for (var index = 0; index < flowPlan.SolutionRoute.Count; index++)
            {
                var step = flowPlan.SolutionRoute[index];
                AddCount(routeUnitsByColor, step.Color, step.CapacityUnits);

                if (!HasMatchingBusForSolutionStep(buses, step))
                {
                    report.Add(
                        LevelValidationSeverity.Warning,
                        $"Solution route step {index + 1} targets {PuzzlePalette.DisplayName(step.Color)} {BusSizeUtility.DisplayName(step.Size)}, but no matching vehicle exists.");
                }
            }

            foreach (var pair in routeUnitsByColor)
            {
                capacityByColor.TryGetValue(pair.Key, out var capacityUnits);
                if (pair.Value > capacityUnits)
                {
                    report.Add(
                        LevelValidationSeverity.Error,
                        $"Solution route uses {PuzzlePalette.DisplayName(pair.Key)} passengers {pair.Value * PassengerUnitLayout.PeoplePerUnit}, above vehicle capacity {capacityUnits * PassengerUnitLayout.PeoplePerUnit}.");
                }
                else if (pair.Value < capacityUnits && !flowPlan.AutoFillMissingCapacity)
                {
                    report.Add(
                        LevelValidationSeverity.Error,
                        $"Solution route leaves {PuzzlePalette.DisplayName(pair.Key)} capacity {(capacityUnits - pair.Value) * PassengerUnitLayout.PeoplePerUnit} unfilled while auto-fill is disabled.");
                }
                else if (pair.Value < capacityUnits)
                {
                    report.Add(
                        LevelValidationSeverity.Warning,
                        $"Solution route leaves {PuzzlePalette.DisplayName(pair.Key)} capacity {(capacityUnits - pair.Value) * PassengerUnitLayout.PeoplePerUnit}; auto-fill will add passengers.");
                }
            }
        }

        private static void ValidateVehiclePlacement(
            LevelValidationReport report,
            IReadOnlyList<BusDefinition> buses,
            bool allowsDenseVehicleSpacing)
        {
            if (buses == null || buses.Count == 0)
            {
                return;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                ValidateVehicleGridCell(report, bus, index);
                ValidateVehicleFineTuning(report, bus, index, allowsDenseVehicleSpacing);
                ValidateVehicleFootprintBounds(report, bus, index);
            }

            for (var firstIndex = 0; firstIndex < buses.Count; firstIndex++)
            {
                var firstFootprint = GetDefinitionFootprint(buses[firstIndex]);
                var firstVisualFootprint = GetDefinitionVisualFootprint(buses[firstIndex]);
                for (var secondIndex = firstIndex + 1; secondIndex < buses.Count; secondIndex++)
                {
                    var secondFootprint = GetDefinitionFootprint(buses[secondIndex]);
                    if (firstFootprint.Overlaps(secondFootprint))
                    {
                        report.Add(
                            LevelValidationSeverity.Error,
                            $"Vehicle #{firstIndex + 1} {DescribeVehicle(buses[firstIndex])} overlaps vehicle #{secondIndex + 1} {DescribeVehicle(buses[secondIndex])} at start.");
                    }

                    var secondVisualFootprint = GetDefinitionVisualFootprint(buses[secondIndex]);
                    if (firstVisualFootprint.Overlaps(secondVisualFootprint))
                    {
                        report.Add(
                            LevelValidationSeverity.Error,
                            $"Vehicle #{firstIndex + 1} {DescribeVehicle(buses[firstIndex])} visually overlaps vehicle #{secondIndex + 1} {DescribeVehicle(buses[secondIndex])} at start.");
                    }
                    else if (!allowsDenseVehicleSpacing &&
                        firstVisualFootprint.IsWithinPadding(secondVisualFootprint, BoardLayoutConfig.VehicleNearPaddingCells))
                    {
                        report.Add(
                            LevelValidationSeverity.Warning,
                            $"Vehicle #{firstIndex + 1} {DescribeVehicle(buses[firstIndex])} is very close to vehicle #{secondIndex + 1} {DescribeVehicle(buses[secondIndex])}; movement may feel visually blocked.");
                    }
                }
            }
        }

        private static bool UsesShapeLibraryLayout(LevelData levelData)
        {
            return levelData != null &&
                StageGenerationSignature.TryGetInt(levelData.GenerationSignature, "layoutVariant", out var layoutVariantIndex) &&
                VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out _);
        }

        private static void ValidateVehicleExitSequence(
            LevelValidationReport report,
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int solutionCountLimit)
        {
            if ((buses == null || buses.Count == 0) && (garages == null || garages.Count == 0))
            {
                return;
            }

            var analysis = StageSolutionAnalyzer.Analyze(buses, garages, solutionCountLimit, VehicleExitNodeVisitLimit);
            if (analysis.IsSolvable)
            {
                return;
            }

            var severity = profile != null && profile.RequireSolutionRoute
                ? LevelValidationSeverity.Error
                : LevelValidationSeverity.Warning;
            report.Add(
                severity,
                "No complete vehicle exit sequence found.");
        }

        private static void ValidateGaragePlacement(
            LevelValidationReport report,
            IReadOnlyList<GarageDefinition> garages,
            IReadOnlyList<BusDefinition> buses)
        {
            if (garages == null || garages.Count == 0)
            {
                return;
            }

            for (var index = 0; index < garages.Count; index++)
            {
                var garage = garages[index];
                if (!BoardLayoutConfig.IsInsideGrid(garage.GridPosition))
                {
                    report.Add(
                        LevelValidationSeverity.Error,
                        $"Garage #{index + 1} starts outside the {BoardLayoutConfig.GridColumns}x{BoardLayoutConfig.GridRows} parking grid at {garage.GridPosition}.");
                }

                if (!BoardLayoutConfig.IsInsideGrid(garage.FrontVehicleGridPosition))
                {
                    report.Add(
                        LevelValidationSeverity.Error,
                        $"Garage #{index + 1} front vehicle cell {garage.FrontVehicleGridPosition} is outside the parking grid.");
                }

                if (garage.QueuedVehicleCount == 0)
                {
                    report.Add(LevelValidationSeverity.Warning, $"Garage #{index + 1} has no queued vehicles.");
                }

                var garageFootprint = GetGarageFootprint(garage);
                var visibleBuses = buses ?? EmptyBuses;
                for (var busIndex = 0; busIndex < visibleBuses.Count; busIndex++)
                {
                    if (garageFootprint.Overlaps(GetDefinitionFootprint(visibleBuses[busIndex])))
                    {
                        report.Add(
                            LevelValidationSeverity.Error,
                            $"Garage #{index + 1} overlaps vehicle #{busIndex + 1} {DescribeVehicle(visibleBuses[busIndex])}.");
                    }
                }

                ValidateGarageVehiclePlacement(report, garage, index, visibleBuses);

                for (var otherIndex = index + 1; otherIndex < garages.Count; otherIndex++)
                {
                    if (garageFootprint.Overlaps(GetGarageFootprint(garages[otherIndex])))
                    {
                        report.Add(LevelValidationSeverity.Error, $"Garage #{index + 1} overlaps garage #{otherIndex + 1}.");
                    }

                    ValidateGarageVehicleSeparation(report, garage, index, garages[otherIndex], otherIndex);
                }
            }
        }

        private static readonly IReadOnlyList<BusDefinition> EmptyBuses = new BusDefinition[0];

        private static void ValidateVehicleGridCell(LevelValidationReport report, BusDefinition bus, int index)
        {
            var cell = bus.GridPosition;
            if (!BoardLayoutConfig.IsInsideGrid(cell))
            {
                report.Add(
                    LevelValidationSeverity.Error,
                    $"Vehicle #{index + 1} {DescribeVehicle(bus)} starts outside the {BoardLayoutConfig.GridColumns}x{BoardLayoutConfig.GridRows} parking grid at {cell}.");
            }
        }

        private static void ValidateVehicleFineTuning(
            LevelValidationReport report,
            BusDefinition bus,
            int index,
            bool allowsShowcaseFineTuning)
        {
            if (allowsShowcaseFineTuning)
            {
                return;
            }

            if (bus.PositionOffsetCells.magnitude > MaxRecommendedPositionOffsetCells)
            {
                report.Add(
                    LevelValidationSeverity.Warning,
                    $"Vehicle #{index + 1} {DescribeVehicle(bus)} has a large position offset {bus.PositionOffsetCells}; keep offsets small unless the level needs a precise blocker.");
            }

            if (Mathf.Abs(bus.AngleOffsetDegrees) > MaxRecommendedAngleOffsetDegrees)
            {
                report.Add(
                    LevelValidationSeverity.Warning,
                    $"Vehicle #{index + 1} {DescribeVehicle(bus)} has angle offset {bus.AngleOffsetDegrees:0.#} degrees; large offsets can make the visual direction hard to read.");
            }
        }

        private static void ValidateVehicleFootprintBounds(LevelValidationReport report, BusDefinition bus, int index)
        {
            var footprint = GetDefinitionVisualFootprint(bus);
            var minX = footprint.ProjectMin(Vector2.right);
            var maxX = footprint.ProjectMax(Vector2.right);
            var minY = footprint.ProjectMin(Vector2.up);
            var maxY = footprint.ProjectMax(Vector2.up);
            var minBoundary = BoardBoundaryMin - BoardBoundaryPadding;
            var maxXBoundary = BoardLayoutConfig.GridColumns - BoardBoundaryMin + BoardBoundaryPadding - 1f;
            var maxYBoundary = BoardLayoutConfig.GridRows - BoardBoundaryMin + BoardBoundaryPadding - 1f;

            if (minX < minBoundary || maxX > maxXBoundary || minY < minBoundary || maxY > maxYBoundary)
            {
                report.Add(
                    LevelValidationSeverity.Warning,
                    $"Vehicle #{index + 1} {DescribeVehicle(bus)} footprint is close to or outside the parking grid boundary.");
            }
        }

        private static VehicleFootprint GetDefinitionFootprint(BusDefinition bus)
        {
            return BoardLayoutConfig.GetVehicleFootprintCells(bus);
        }

        private static VehicleFootprint GetDefinitionVisualFootprint(BusDefinition bus)
        {
            return BoardLayoutConfig.GetVehicleVisualFootprintCells(bus);
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

        private static string DescribeVehicle(BusDefinition bus)
        {
            return $"{PuzzlePalette.DisplayName(bus.Color)} {BusSizeUtility.DisplayName(bus.Size)}";
        }

        private static void ValidateGarageVehiclePlacement(
            LevelValidationReport report,
            GarageDefinition garage,
            int garageIndex,
            IReadOnlyList<BusDefinition> visibleBuses)
        {
            var vehicleIndex = 0;
            foreach (var garageVehicle in garage.EnumerateVehicles())
            {
                vehicleIndex++;
                ValidateGarageVehicleGridCell(report, garageVehicle, garageIndex, vehicleIndex);
                var garageVehicleFootprint = GetDefinitionVisualFootprint(garageVehicle);
                if (garageVehicleFootprint.Overlaps(GetGarageFootprint(garage)))
                {
                    report.Add(
                        LevelValidationSeverity.Error,
                        $"Garage #{garageIndex + 1} vehicle #{vehicleIndex} {DescribeVehicle(garageVehicle)} visually overlaps its garage.");
                }

                for (var busIndex = 0; busIndex < visibleBuses.Count; busIndex++)
                {
                    if (garageVehicleFootprint.Overlaps(GetDefinitionVisualFootprint(visibleBuses[busIndex])))
                    {
                        report.Add(
                            LevelValidationSeverity.Error,
                            $"Garage #{garageIndex + 1} vehicle #{vehicleIndex} {DescribeVehicle(garageVehicle)} visually overlaps vehicle #{busIndex + 1} {DescribeVehicle(visibleBuses[busIndex])}.");
                    }
                }
            }
        }

        private static void ValidateGarageVehicleGridCell(
            LevelValidationReport report,
            BusDefinition garageVehicle,
            int garageIndex,
            int vehicleIndex)
        {
            if (!BoardLayoutConfig.IsInsideGrid(garageVehicle.GridPosition))
            {
                report.Add(
                    LevelValidationSeverity.Error,
                    $"Garage #{garageIndex + 1} vehicle #{vehicleIndex} starts outside the {BoardLayoutConfig.GridColumns}x{BoardLayoutConfig.GridRows} parking grid at {garageVehicle.GridPosition}.");
            }
        }

        private static void ValidateGarageVehicleSeparation(
            LevelValidationReport report,
            GarageDefinition firstGarage,
            int firstGarageIndex,
            GarageDefinition secondGarage,
            int secondGarageIndex)
        {
            foreach (var firstVehicle in firstGarage.EnumerateVehicles())
            {
                var firstFootprint = GetDefinitionVisualFootprint(firstVehicle);
                if (firstFootprint.Overlaps(GetGarageFootprint(secondGarage)))
                {
                    report.Add(
                        LevelValidationSeverity.Error,
                        $"Garage #{firstGarageIndex + 1} vehicle {DescribeVehicle(firstVehicle)} visually overlaps garage #{secondGarageIndex + 1}.");
                }

                foreach (var secondVehicle in secondGarage.EnumerateVehicles())
                {
                    if (firstFootprint.Overlaps(GetDefinitionVisualFootprint(secondVehicle)))
                    {
                        report.Add(
                            LevelValidationSeverity.Error,
                            $"Garage #{firstGarageIndex + 1} vehicle {DescribeVehicle(firstVehicle)} visually overlaps garage #{secondGarageIndex + 1} vehicle {DescribeVehicle(secondVehicle)}.");
                    }
                }
            }

            foreach (var secondVehicle in secondGarage.EnumerateVehicles())
            {
                if (GetDefinitionVisualFootprint(secondVehicle).Overlaps(GetGarageFootprint(firstGarage)))
                {
                    report.Add(
                        LevelValidationSeverity.Error,
                        $"Garage #{secondGarageIndex + 1} vehicle {DescribeVehicle(secondVehicle)} visually overlaps garage #{firstGarageIndex + 1}.");
                }
            }
        }

        private static string DescribeVehicleIndices(IReadOnlyList<BusDefinition> buses, IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count == 0)
            {
                return "none";
            }

            var descriptions = new List<string>();
            for (var index = 0; index < indices.Count; index++)
            {
                var vehicleIndex = indices[index];
                if (vehicleIndex < 0 || vehicleIndex >= buses.Count)
                {
                    continue;
                }

                descriptions.Add($"#{vehicleIndex + 1} {DescribeVehicle(buses[vehicleIndex])}");
            }

            return string.Join(", ", descriptions);
        }

        private static int CountUniqueBusColors(IReadOnlyList<BusDefinition> buses)
        {
            var colors = new List<PuzzleColor>();
            if (buses == null)
            {
                return 0;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                AddUniqueColor(colors, buses[index].Color);
            }

            return colors.Count;
        }

        private static Dictionary<PuzzleColor, int> CountBusCapacityUnitsByColor(IReadOnlyList<BusDefinition> buses)
        {
            var counts = new Dictionary<PuzzleColor, int>();
            if (buses == null)
            {
                return counts;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                AddCount(counts, buses[index].Color, buses[index].CapacityUnits);
            }

            return counts;
        }

        private static bool HasMatchingBusForSolutionStep(IReadOnlyList<BusDefinition> buses, SolutionBusStepDefinition step)
        {
            if (buses == null)
            {
                return false;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus.Color == step.Color && bus.Size == step.Size)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountUniquePassengerColors(IReadOnlyList<PuzzleColor> passengers)
        {
            var colors = new List<PuzzleColor>();
            if (passengers == null)
            {
                return 0;
            }

            for (var index = 0; index < passengers.Count; index++)
            {
                AddUniqueColor(colors, passengers[index]);
            }

            return colors.Count;
        }

        private static int GetLongestColorRun(IReadOnlyList<PuzzleColor> passengers)
        {
            var longestRun = 0;
            var currentRun = 0;
            var currentColor = default(PuzzleColor);

            for (var index = 0; index < passengers.Count; index++)
            {
                if (index == 0 || passengers[index] != currentColor)
                {
                    currentColor = passengers[index];
                    currentRun = 1;
                }
                else
                {
                    currentRun++;
                }

                longestRun = Mathf.Max(longestRun, currentRun);
            }

            return longestRun;
        }

        private static int GetVehicleCountTolerance(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 6;
                case LevelDifficulty.Hard:
                    return 5;
                default:
                    return 4;
            }
        }

        private static int GetColorCountTolerance(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 1;
                case LevelDifficulty.Hard:
                    return 2;
                default:
                    return 4;
            }
        }

        private static int GetRunTolerance(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 1;
                case LevelDifficulty.Hard:
                    return 2;
                default:
                    return 4;
            }
        }

        private static void AddUniqueColor(List<PuzzleColor> colors, PuzzleColor color)
        {
            for (var index = 0; index < colors.Count; index++)
            {
                if (colors[index] == color)
                {
                    return;
                }
            }

            colors.Add(color);
        }

        private static void AddCount(Dictionary<PuzzleColor, int> counts, PuzzleColor color, int amount)
        {
            counts.TryGetValue(color, out var current);
            counts[color] = current + amount;
        }
    }
}
