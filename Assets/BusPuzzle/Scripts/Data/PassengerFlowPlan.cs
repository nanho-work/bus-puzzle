using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public enum PassengerFlowPlanMode
    {
        ManualGroups = 0,
        SolutionRoute = 1,
        RatioByDifficulty = 2
    }

    [Serializable]
    public struct PassengerFlowDifficultyRule
    {
        [SerializeField, Range(0.05f, 1f)] private float minMainGroupRatio;
        [SerializeField, Range(0.05f, 1f)] private float maxMainGroupRatio;
        [SerializeField, Range(1, 24)] private int minGroupUnits;
        [SerializeField, Range(1, 24)] private int maxGroupUnits;
        [SerializeField, Range(0f, 1f)] private float interferenceRatio;
        [SerializeField] private bool preserveSolutionRoute;

        public PassengerFlowDifficultyRule(
            float minMainGroupRatio,
            float maxMainGroupRatio,
            int minGroupUnits,
            int maxGroupUnits,
            float interferenceRatio,
            bool preserveSolutionRoute)
        {
            this.minMainGroupRatio = minMainGroupRatio;
            this.maxMainGroupRatio = maxMainGroupRatio;
            this.minGroupUnits = minGroupUnits;
            this.maxGroupUnits = maxGroupUnits;
            this.interferenceRatio = interferenceRatio;
            this.preserveSolutionRoute = preserveSolutionRoute;
        }

        public float MinMainGroupRatio => Mathf.Clamp(minMainGroupRatio, 0.05f, 1f);
        public float MaxMainGroupRatio => Mathf.Max(MinMainGroupRatio, Mathf.Clamp(maxMainGroupRatio, 0.05f, 1f));
        public int MinGroupUnits => Mathf.Clamp(minGroupUnits, 1, 24);
        public int MaxGroupUnits => Mathf.Max(MinGroupUnits, Mathf.Clamp(maxGroupUnits, 1, 24));
        public float InterferenceRatio => Mathf.Clamp01(interferenceRatio);
        public bool PreserveSolutionRoute => preserveSolutionRoute;
        public bool HasUsableValues => maxMainGroupRatio > 0f && maxGroupUnits > 0;

        public static PassengerFlowDifficultyRule DefaultFor(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return new PassengerFlowDifficultyRule(0.25f, 0.50f, 2, 7, 0.38f, true);
                case LevelDifficulty.SuperHard:
                    return new PassengerFlowDifficultyRule(0.15f, 0.35f, 1, 5, 0.58f, true);
                default:
                    return new PassengerFlowDifficultyRule(0.45f, 0.70f, 3, 10, 0.22f, true);
            }
        }
    }

    [Serializable]
    public struct PassengerGroupDefinition
    {
        [SerializeField] private PuzzleColor color;
        [SerializeField] private int unitCount;

        public PassengerGroupDefinition(PuzzleColor color, int unitCount)
        {
            this.color = color;
            this.unitCount = unitCount;
        }

        public PuzzleColor Color => color;
        public int UnitCount => Mathf.Max(0, unitCount);
    }

    [Serializable]
    public struct SolutionBusStepDefinition
    {
        [SerializeField] private PuzzleColor color;
        [SerializeField] private BusSize size;
        [SerializeField] private int overrideUnitCount;
        [SerializeField] private int preferredGroupUnitCount;

        public SolutionBusStepDefinition(PuzzleColor color, BusSize size, int preferredGroupUnitCount = 0, int overrideUnitCount = 0)
        {
            this.color = color;
            this.size = size;
            this.overrideUnitCount = overrideUnitCount;
            this.preferredGroupUnitCount = preferredGroupUnitCount;
        }

        public PuzzleColor Color => color;
        public BusSize Size => size;
        public int OverrideUnitCount => Mathf.Max(0, overrideUnitCount);
        public int PreferredGroupUnitCount => Mathf.Max(0, preferredGroupUnitCount);
        public int CapacityUnits => OverrideUnitCount > 0 ? OverrideUnitCount : BusSizeUtility.ToPassengerUnits(size);
    }

    [Serializable]
    public sealed class PassengerFlowPlan
    {
        [SerializeField] private bool enabled;
        [SerializeField] private PassengerFlowPlanMode mode = PassengerFlowPlanMode.ManualGroups;
        [SerializeField] private int seed = 1;
        [SerializeField, Range(1, 24)] private int minGroupUnits = 3;
        [SerializeField, Range(1, 24)] private int maxGroupUnits = 7;
        [SerializeField] private bool autoFillMissingCapacity = true;
        [SerializeField] private List<PassengerGroupDefinition> groups = new List<PassengerGroupDefinition>();
        [SerializeField] private List<SolutionBusStepDefinition> solutionRoute = new List<SolutionBusStepDefinition>();

        public bool Enabled => enabled;
        public PassengerFlowPlanMode Mode => mode;
        public int Seed => seed;
        public int MinGroupUnits => Mathf.Clamp(minGroupUnits, 1, 24);
        public int MaxGroupUnits => Mathf.Max(MinGroupUnits, Mathf.Clamp(maxGroupUnits, 1, 24));
        public bool AutoFillMissingCapacity => autoFillMissingCapacity;
        public IReadOnlyList<PassengerGroupDefinition> Groups => groups ?? EmptyPassengerGroups;
        public IReadOnlyList<SolutionBusStepDefinition> SolutionRoute => solutionRoute ?? EmptySolutionRoute;

        private static readonly IReadOnlyList<PassengerGroupDefinition> EmptyPassengerGroups = Array.Empty<PassengerGroupDefinition>();
        private static readonly IReadOnlyList<SolutionBusStepDefinition> EmptySolutionRoute = Array.Empty<SolutionBusStepDefinition>();

        public void ConfigureManualGroups(
            IEnumerable<PassengerGroupDefinition> newGroups,
            bool shouldAutoFillMissingCapacity = true,
            int newSeed = 1)
        {
            enabled = true;
            mode = PassengerFlowPlanMode.ManualGroups;
            autoFillMissingCapacity = shouldAutoFillMissingCapacity;
            seed = newSeed;
            groups = new List<PassengerGroupDefinition>(newGroups);
        }

        public void ConfigureSolutionRoute(
            IEnumerable<SolutionBusStepDefinition> newSolutionRoute,
            int newMinGroupUnits,
            int newMaxGroupUnits,
            bool shouldAutoFillMissingCapacity = true,
            int newSeed = 1)
        {
            enabled = true;
            mode = PassengerFlowPlanMode.SolutionRoute;
            minGroupUnits = Mathf.Max(1, newMinGroupUnits);
            maxGroupUnits = Mathf.Max(minGroupUnits, newMaxGroupUnits);
            autoFillMissingCapacity = shouldAutoFillMissingCapacity;
            seed = newSeed;
            solutionRoute = new List<SolutionBusStepDefinition>(newSolutionRoute);
        }

        public void ConfigureRatioByDifficulty(int newSeed = 1, bool shouldAutoFillMissingCapacity = true)
        {
            enabled = true;
            mode = PassengerFlowPlanMode.RatioByDifficulty;
            autoFillMissingCapacity = shouldAutoFillMissingCapacity;
            seed = newSeed;
        }

        public void ConfigureRatioByDifficultyWithSolutionRoute(
            IEnumerable<SolutionBusStepDefinition> newSolutionRoute,
            int newSeed = 1,
            bool shouldAutoFillMissingCapacity = true)
        {
            ConfigureRatioByDifficulty(newSeed, shouldAutoFillMissingCapacity);
            solutionRoute = newSolutionRoute != null
                ? new List<SolutionBusStepDefinition>(newSolutionRoute)
                : new List<SolutionBusStepDefinition>();
        }
    }
}
