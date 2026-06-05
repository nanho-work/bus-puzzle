using System.Collections.Generic;

namespace BusPuzzle
{
    internal static class LevelPassengerBuilder
    {
        public static List<PuzzleColor> BuildPassengerUnits(
            LevelDifficultyProfile difficultyProfile,
            PassengerFlowPlan flowPlan,
            IReadOnlyList<PuzzleColor> fallbackUnits,
            IReadOnlyList<BusDefinition> buses)
        {
            if (flowPlan == null || !flowPlan.Enabled)
            {
                return CopyUnits(fallbackUnits);
            }

            var units = BuildFromFlowPlan(difficultyProfile, flowPlan, buses);

            if (flowPlan.AutoFillMissingCapacity)
            {
                AppendMissingCapacity(units, buses, flowPlan.MaxGroupUnits);
            }

            return units.Count > 0 ? units : CopyUnits(fallbackUnits);
        }

        private static List<PuzzleColor> BuildFromFlowPlan(
            LevelDifficultyProfile difficultyProfile,
            PassengerFlowPlan flowPlan,
            IReadOnlyList<BusDefinition> buses)
        {
            switch (flowPlan.Mode)
            {
                case PassengerFlowPlanMode.SolutionRoute:
                    return BuildFromSolutionRoute(flowPlan, buses);
                case PassengerFlowPlanMode.RatioByDifficulty:
                    return BuildFromDifficultyRatio(difficultyProfile, flowPlan, buses);
                default:
                    return BuildFromManualGroups(flowPlan.Groups);
            }
        }

        private static List<PuzzleColor> BuildFromManualGroups(IReadOnlyList<PassengerGroupDefinition> groups)
        {
            var units = new List<PuzzleColor>();
            if (groups == null)
            {
                return units;
            }

            for (var index = 0; index < groups.Count; index++)
            {
                AppendGroup(units, groups[index].Color, groups[index].UnitCount);
            }

            return units;
        }

        private static List<PuzzleColor> BuildFromDifficultyRatio(
            LevelDifficultyProfile difficultyProfile,
            PassengerFlowPlan flowPlan,
            IReadOnlyList<BusDefinition> buses)
        {
            var capacities = CountBusCapacityByColor(buses);
            var rule = GetPassengerFlowRule(difficultyProfile);
            var colorOrder = GetColorOrder(flowPlan, buses, rule);
            var groupsByColor = new Dictionary<PuzzleColor, Queue<PassengerGroupDefinition>>();
            var random = new System.Random(flowPlan.Seed);

            for (var index = 0; index < colorOrder.Count; index++)
            {
                var color = colorOrder[index];
                if (!capacities.TryGetValue(color, out var totalUnits) || totalUnits <= 0)
                {
                    continue;
                }

                groupsByColor[color] = new Queue<PassengerGroupDefinition>(BuildRatioGroups(color, totalUnits, rule, random));
            }

            return FlattenGroups(ArrangeGroupsByInterference(groupsByColor, colorOrder, rule));
        }

        private static List<PuzzleColor> BuildFromSolutionRoute(PassengerFlowPlan flowPlan, IReadOnlyList<BusDefinition> buses)
        {
            var units = new List<PuzzleColor>();
            var remainingCapacity = CountBusCapacityByColor(buses);

            if (flowPlan.SolutionRoute.Count > 0)
            {
                for (var index = 0; index < flowPlan.SolutionRoute.Count; index++)
                {
                    var step = flowPlan.SolutionRoute[index];
                    if (!remainingCapacity.TryGetValue(step.Color, out var remainingUnits) || remainingUnits <= 0)
                    {
                        continue;
                    }

                    var stepUnits = ClampToRemaining(step.CapacityUnits, remainingUnits);
                    AppendSplitGroups(units, step.Color, stepUnits, GetPreferredGroupSize(flowPlan, step));
                    remainingCapacity[step.Color] = remainingUnits - stepUnits;
                }

                return units;
            }

            if (buses == null)
            {
                return units;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (!remainingCapacity.TryGetValue(bus.Color, out var remainingUnits) || remainingUnits <= 0)
                {
                    continue;
                }

                var stepUnits = ClampToRemaining(bus.CapacityUnits, remainingUnits);
                AppendSplitGroups(units, bus.Color, stepUnits, flowPlan.MaxGroupUnits);
                remainingCapacity[bus.Color] = remainingUnits - stepUnits;
            }

            return units;
        }

        private static int GetPreferredGroupSize(PassengerFlowPlan flowPlan, SolutionBusStepDefinition step)
        {
            return step.PreferredGroupUnitCount > 0
                ? step.PreferredGroupUnitCount
                : flowPlan.MaxGroupUnits;
        }

        private static List<PassengerGroupDefinition> BuildRatioGroups(
            PuzzleColor color,
            int totalUnits,
            PassengerFlowDifficultyRule rule,
            System.Random random)
        {
            var groups = new List<PassengerGroupDefinition>();
            if (totalUnits <= 0)
            {
                return groups;
            }

            var mainRatio = Lerp(rule.MinMainGroupRatio, rule.MaxMainGroupRatio, (float)random.NextDouble());
            var mainGroupUnits = UnityEngine.Mathf.RoundToInt(totalUnits * mainRatio);
            var minimumMainGroupUnits = UnityEngine.Mathf.Min(rule.MinGroupUnits, totalUnits);
            var maximumMainGroupUnits = UnityEngine.Mathf.Max(minimumMainGroupUnits, UnityEngine.Mathf.Min(rule.MaxGroupUnits, totalUnits));
            mainGroupUnits = UnityEngine.Mathf.Clamp(mainGroupUnits, minimumMainGroupUnits, maximumMainGroupUnits);
            groups.Add(new PassengerGroupDefinition(color, mainGroupUnits));

            var remainingUnits = totalUnits - mainGroupUnits;
            while (remainingUnits > 0)
            {
                var maxGroupUnits = UnityEngine.Mathf.Min(rule.MaxGroupUnits, remainingUnits);
                var minGroupUnits = UnityEngine.Mathf.Min(rule.MinGroupUnits, maxGroupUnits);
                var groupUnits = random.Next(minGroupUnits, maxGroupUnits + 1);

                if (remainingUnits - groupUnits > 0 && remainingUnits - groupUnits < rule.MinGroupUnits)
                {
                    groupUnits = UnityEngine.Mathf.Max(minGroupUnits, remainingUnits - rule.MinGroupUnits);
                }

                groups.Add(new PassengerGroupDefinition(color, groupUnits));
                remainingUnits -= groupUnits;
            }

            return groups;
        }

        private static List<PassengerGroupDefinition> ArrangeGroupsByInterference(
            Dictionary<PuzzleColor, Queue<PassengerGroupDefinition>> groupsByColor,
            IReadOnlyList<PuzzleColor> colorOrder,
            PassengerFlowDifficultyRule rule)
        {
            var arrangedGroups = new List<PassengerGroupDefinition>();
            if (colorOrder == null || colorOrder.Count == 0)
            {
                return arrangedGroups;
            }

            var sameColorBudget = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(Lerp(4f, 1f, rule.InterferenceRatio)));
            var cursor = 0;
            while (HasQueuedGroups(groupsByColor))
            {
                var color = FindNextColorWithGroups(groupsByColor, colorOrder, ref cursor);
                if (!groupsByColor.TryGetValue(color, out var queue))
                {
                    break;
                }

                var takeCount = UnityEngine.Mathf.Min(sameColorBudget, queue.Count);
                for (var index = 0; index < takeCount; index++)
                {
                    arrangedGroups.Add(queue.Dequeue());
                }

                cursor = (cursor + 1) % colorOrder.Count;
            }

            return arrangedGroups;
        }

        private static PuzzleColor FindNextColorWithGroups(
            Dictionary<PuzzleColor, Queue<PassengerGroupDefinition>> groupsByColor,
            IReadOnlyList<PuzzleColor> colorOrder,
            ref int cursor)
        {
            for (var offset = 0; offset < colorOrder.Count; offset++)
            {
                var index = (cursor + offset) % colorOrder.Count;
                var color = colorOrder[index];
                if (groupsByColor.TryGetValue(color, out var queue) && queue.Count > 0)
                {
                    cursor = index;
                    return color;
                }
            }

            return colorOrder[0];
        }

        private static bool HasQueuedGroups(Dictionary<PuzzleColor, Queue<PassengerGroupDefinition>> groupsByColor)
        {
            foreach (var pair in groupsByColor)
            {
                if (pair.Value.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<PuzzleColor> FlattenGroups(IReadOnlyList<PassengerGroupDefinition> groups)
        {
            var units = new List<PuzzleColor>();
            if (groups == null)
            {
                return units;
            }

            for (var index = 0; index < groups.Count; index++)
            {
                AppendGroup(units, groups[index].Color, groups[index].UnitCount);
            }

            return units;
        }

        private static PassengerFlowDifficultyRule GetPassengerFlowRule(LevelDifficultyProfile difficultyProfile)
        {
            return difficultyProfile != null
                ? difficultyProfile.PassengerFlowRule
                : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal).PassengerFlowRule;
        }

        private static List<PuzzleColor> GetColorOrder(
            PassengerFlowPlan flowPlan,
            IReadOnlyList<BusDefinition> buses,
            PassengerFlowDifficultyRule rule)
        {
            var colors = new List<PuzzleColor>();
            if (rule.PreserveSolutionRoute && flowPlan.SolutionRoute.Count > 0)
            {
                for (var index = 0; index < flowPlan.SolutionRoute.Count; index++)
                {
                    AddColor(colors, flowPlan.SolutionRoute[index].Color);
                }
            }

            if (buses != null)
            {
                for (var index = 0; index < buses.Count; index++)
                {
                    AddColor(colors, buses[index].Color);
                }
            }

            return colors;
        }

        private static int ClampToRemaining(int desiredUnits, int remainingUnits)
        {
            return desiredUnits <= 0 ? remainingUnits : UnityEngine.Mathf.Min(desiredUnits, remainingUnits);
        }

        private static void AppendMissingCapacity(List<PuzzleColor> units, IReadOnlyList<BusDefinition> buses, int maxGroupUnits)
        {
            var passengerCounts = CountPassengerUnitsByColor(units);
            var capacityCounts = CountBusCapacityByColor(buses);
            var colorOrder = GetColorOrderFromBuses(buses);

            for (var index = 0; index < colorOrder.Count; index++)
            {
                var color = colorOrder[index];
                passengerCounts.TryGetValue(color, out var passengerUnits);
                var missingUnits = capacityCounts[color] - passengerUnits;
                if (missingUnits <= 0)
                {
                    continue;
                }

                AppendSplitGroups(units, color, missingUnits, maxGroupUnits);
            }
        }

        private static List<PuzzleColor> GetColorOrderFromBuses(IReadOnlyList<BusDefinition> buses)
        {
            var colors = new List<PuzzleColor>();
            if (buses == null)
            {
                return colors;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                AddColor(colors, buses[index].Color);
            }

            return colors;
        }

        private static Dictionary<PuzzleColor, int> CountPassengerUnitsByColor(IReadOnlyList<PuzzleColor> units)
        {
            var counts = new Dictionary<PuzzleColor, int>();
            if (units == null)
            {
                return counts;
            }

            for (var index = 0; index < units.Count; index++)
            {
                AddCount(counts, units[index], 1);
            }

            return counts;
        }

        private static Dictionary<PuzzleColor, int> CountBusCapacityByColor(IReadOnlyList<BusDefinition> buses)
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

        private static void AppendSplitGroups(List<PuzzleColor> units, PuzzleColor color, int unitCount, int groupSize)
        {
            groupSize = UnityEngine.Mathf.Max(1, groupSize);
            while (unitCount > 0)
            {
                var amount = UnityEngine.Mathf.Min(groupSize, unitCount);
                AppendGroup(units, color, amount);
                unitCount -= amount;
            }
        }

        private static void AppendGroup(List<PuzzleColor> units, PuzzleColor color, int unitCount)
        {
            for (var index = 0; index < unitCount; index++)
            {
                units.Add(color);
            }
        }

        private static void AddCount(Dictionary<PuzzleColor, int> counts, PuzzleColor color, int amount)
        {
            counts.TryGetValue(color, out var current);
            counts[color] = current + amount;
        }

        private static void AddColor(List<PuzzleColor> colors, PuzzleColor color)
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

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * UnityEngine.Mathf.Clamp01(t);
        }

        private static List<PuzzleColor> CopyUnits(IReadOnlyList<PuzzleColor> units)
        {
            return units != null ? new List<PuzzleColor>(units) : new List<PuzzleColor>();
        }
    }
}
