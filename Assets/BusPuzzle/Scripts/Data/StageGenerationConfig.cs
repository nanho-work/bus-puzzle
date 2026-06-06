using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [Serializable]
    public sealed class StageDifficultyGenerationRule
    {
        [SerializeField] private LevelDifficulty difficulty = LevelDifficulty.Normal;
        [SerializeField, Range(4, 50)] private int earlyVehicleCount = 25;
        [SerializeField, Range(4, 50)] private int lateVehicleCount = 35;
        [SerializeField, Range(2, 10)] private int earlyColorCount = 5;
        [SerializeField, Range(2, 10)] private int lateColorCount = 8;
        [SerializeField, Range(0f, 1f)] private float earlyParkingTension = 0.30f;
        [SerializeField, Range(0f, 1f)] private float lateParkingTension = 0.55f;
        [SerializeField, Range(0f, 1f)] private float earlyStationPressure = 0.25f;
        [SerializeField, Range(0f, 1f)] private float lateStationPressure = 0.55f;
        [SerializeField, Range(1, 512)] private int minSolutionCount = 80;
        [SerializeField, Range(1, 512)] private int maxSolutionCount = 256;
        [SerializeField] private bool requireSolutionRoute;

        public LevelDifficulty Difficulty => difficulty;
        public int MinSolutionCount => Mathf.Max(1, minSolutionCount);
        public int MaxSolutionCount => Mathf.Max(MinSolutionCount, maxSolutionCount);
        public bool RequireSolutionRoute => requireSolutionRoute;

        public LevelDifficultyProfile CreateProfile(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return LevelDifficultyProfile.CreateCustom(
                difficulty,
                Mathf.RoundToInt(Mathf.Lerp(earlyVehicleCount, lateVehicleCount, progress)),
                Mathf.RoundToInt(Mathf.Lerp(earlyColorCount, lateColorCount, progress)),
                Mathf.Lerp(earlyParkingTension, lateParkingTension, progress),
                Mathf.Lerp(earlyStationPressure, lateStationPressure, progress),
                requireSolutionRoute);
        }

        public static StageDifficultyGenerationRule DefaultFor(LevelDifficulty difficulty)
        {
            var rule = new StageDifficultyGenerationRule();
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    rule.difficulty = LevelDifficulty.Hard;
                    rule.earlyVehicleCount = 28;
                    rule.lateVehicleCount = 42;
                    rule.earlyColorCount = 6;
                    rule.lateColorCount = 9;
                    rule.earlyParkingTension = 0.45f;
                    rule.lateParkingTension = 0.68f;
                    rule.earlyStationPressure = 0.40f;
                    rule.lateStationPressure = 0.68f;
                    rule.minSolutionCount = 18;
                    rule.maxSolutionCount = 96;
                    rule.requireSolutionRoute = true;
                    break;
                case LevelDifficulty.SuperHard:
                    rule.difficulty = LevelDifficulty.SuperHard;
                    rule.earlyVehicleCount = 32;
                    rule.lateVehicleCount = 50;
                    rule.earlyColorCount = 7;
                    rule.lateColorCount = 10;
                    rule.earlyParkingTension = 0.58f;
                    rule.lateParkingTension = 0.82f;
                    rule.earlyStationPressure = 0.55f;
                    rule.lateStationPressure = 0.82f;
                    rule.minSolutionCount = 1;
                    rule.maxSolutionCount = 24;
                    rule.requireSolutionRoute = true;
                    break;
                default:
                    rule.difficulty = LevelDifficulty.Normal;
                    rule.earlyVehicleCount = 25;
                    rule.lateVehicleCount = 38;
                    rule.earlyColorCount = 5;
                    rule.lateColorCount = 8;
                    rule.earlyParkingTension = 0.30f;
                    rule.lateParkingTension = 0.55f;
                    rule.earlyStationPressure = 0.25f;
                    rule.lateStationPressure = 0.52f;
                    rule.minSolutionCount = 80;
                    rule.maxSolutionCount = 256;
                    rule.requireSolutionRoute = false;
                    break;
            }

            return rule;
        }
    }

    [Serializable]
    public sealed class GarageGenerationRule
    {
        [SerializeField] private bool enabled = true;
        [SerializeField, Range(0, 5)] private int earlyMinGarageCount = 1;
        [SerializeField, Range(0, 5)] private int earlyMaxGarageCount = 2;
        [SerializeField, Range(0, 5)] private int lateMinGarageCount = 3;
        [SerializeField, Range(0, 5)] private int lateMaxGarageCount = 5;
        [SerializeField, Range(1, 8)] private int minQueuedVehiclesPerGarage = 1;
        [SerializeField, Range(1, 8)] private int maxQueuedVehiclesPerGarage = 4;

        public bool Enabled => enabled;
        public int MinQueuedVehiclesPerGarage => Mathf.Max(1, minQueuedVehiclesPerGarage);
        public int MaxQueuedVehiclesPerGarage => Mathf.Max(MinQueuedVehiclesPerGarage, maxQueuedVehiclesPerGarage);

        public int PickGarageCount(System.Random random, float progress)
        {
            if (!enabled)
            {
                return 0;
            }

            progress = Mathf.Clamp01(progress);
            var minCount = Mathf.RoundToInt(Mathf.Lerp(earlyMinGarageCount, lateMinGarageCount, progress));
            var maxCount = Mathf.RoundToInt(Mathf.Lerp(earlyMaxGarageCount, lateMaxGarageCount, progress));
            minCount = Mathf.Clamp(minCount, 1, 5);
            maxCount = Mathf.Clamp(Mathf.Max(minCount, maxCount), 1, 5);
            return random.Next(minCount, maxCount + 1);
        }
    }

    [CreateAssetMenu(menuName = "Bus Puzzle/Stage Generation Config", fileName = "StageGenerationConfig")]
    public sealed class StageGenerationConfig : ScriptableObject
    {
        [SerializeField, Range(1, 500)] private int generatedStageCount = 50;
        [SerializeField] private int baseSeed = 10000;
        [SerializeField, Range(1, 300)] private int candidateAttemptsPerStage = 80;
        [SerializeField, Range(1, 20)] private int runtimeCandidateAttemptsPerStage = 8;
        [SerializeField, Range(1, 80)] private int runtimeVehicleGenerationAttempts = 8;
        [SerializeField, Range(1, 512)] private int solutionCountLimit = 256;
        [SerializeField, Range(0, 10)] private int runtimePreloadAheadCount = 3;
        [SerializeField] private List<LevelDifficulty> difficultyPattern = new List<LevelDifficulty>
        {
            LevelDifficulty.Normal,
            LevelDifficulty.Normal,
            LevelDifficulty.Normal,
            LevelDifficulty.Hard,
            LevelDifficulty.SuperHard
        };
        [SerializeField] private StageDifficultyGenerationRule normalRule = StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.Normal);
        [SerializeField] private StageDifficultyGenerationRule hardRule = StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.Hard);
        [SerializeField] private StageDifficultyGenerationRule superHardRule = StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.SuperHard);
        [SerializeField] private GarageGenerationRule superHardGarageRule = new GarageGenerationRule();

        public int GeneratedStageCount => Mathf.Max(1, generatedStageCount);
        public int BaseSeed => baseSeed;
        public int CandidateAttemptsPerStage => Mathf.Max(1, candidateAttemptsPerStage);
        public int RuntimeCandidateAttemptsPerStage => Mathf.Clamp(runtimeCandidateAttemptsPerStage, 1, 20);
        public int RuntimeVehicleGenerationAttempts => Mathf.Clamp(runtimeVehicleGenerationAttempts, 1, 80);
        public int SolutionCountLimit => Mathf.Max(1, solutionCountLimit);
        public int RuntimePreloadAheadCount => Mathf.Clamp(runtimePreloadAheadCount, 0, 10);
        public GarageGenerationRule SuperHardGarageRule => superHardGarageRule ?? new GarageGenerationRule();

        public LevelDifficulty GetDifficultyForStage(int stageNumber)
        {
            var pattern = difficultyPattern;
            if (pattern == null || pattern.Count == 0)
            {
                return LevelDifficulty.Normal;
            }

            return pattern[Mathf.Abs(stageNumber - 1) % pattern.Count];
        }

        public StageDifficultyGenerationRule GetRule(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return hardRule ?? StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.Hard);
                case LevelDifficulty.SuperHard:
                    return superHardRule ?? StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.SuperHard);
                default:
                    return normalRule ?? StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.Normal);
            }
        }

        public float GetProgress(int stageNumber)
        {
            return GeneratedStageCount <= 1
                ? 0f
                : Mathf.Clamp01((stageNumber - 1f) / (GeneratedStageCount - 1f));
        }
    }
}
