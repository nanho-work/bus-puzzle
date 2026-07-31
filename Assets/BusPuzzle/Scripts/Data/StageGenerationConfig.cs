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
        [SerializeField, Range(2, 12)] private int earlyColorCount = 5;
        [SerializeField, Range(2, 12)] private int lateColorCount = 8;
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
                    rule.earlyVehicleCount = 30;
                    rule.lateVehicleCount = 44;
                    rule.earlyColorCount = 6;
                    rule.lateColorCount = 9;
                    rule.earlyParkingTension = 0.50f;
                    rule.lateParkingTension = 0.72f;
                    rule.earlyStationPressure = 0.45f;
                    rule.lateStationPressure = 0.72f;
                    rule.minSolutionCount = 10;
                    rule.maxSolutionCount = 70;
                    rule.requireSolutionRoute = true;
                    break;
                case LevelDifficulty.SuperHard:
                    rule.difficulty = LevelDifficulty.SuperHard;
                    rule.earlyVehicleCount = 34;
                    rule.lateVehicleCount = 50;
                    rule.earlyColorCount = 7;
                    rule.lateColorCount = 12;
                    rule.earlyParkingTension = 0.62f;
                    rule.lateParkingTension = 0.84f;
                    rule.earlyStationPressure = 0.58f;
                    rule.lateStationPressure = 0.84f;
                    rule.minSolutionCount = 1;
                    rule.maxSolutionCount = 18;
                    rule.requireSolutionRoute = true;
                    break;
                default:
                    rule.difficulty = LevelDifficulty.Normal;
                    rule.earlyVehicleCount = 26;
                    rule.lateVehicleCount = 40;
                    rule.earlyColorCount = 5;
                    rule.lateColorCount = 8;
                    rule.earlyParkingTension = 0.34f;
                    rule.lateParkingTension = 0.60f;
                    rule.earlyStationPressure = 0.30f;
                    rule.lateStationPressure = 0.58f;
                    rule.minSolutionCount = 48;
                    rule.maxSolutionCount = 180;
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
        [SerializeField, Range(1, 8)] private int post50MinQueuedVehiclesPerGarage = 2;
        [SerializeField, Range(1, 8)] private int post50MaxQueuedVehiclesPerGarage = 6;

        public bool Enabled => enabled;
        public int MinQueuedVehiclesPerGarage => Mathf.Max(1, minQueuedVehiclesPerGarage);
        public int MaxQueuedVehiclesPerGarage => Mathf.Max(MinQueuedVehiclesPerGarage, maxQueuedVehiclesPerGarage);
        public int Post50MinQueuedVehiclesPerGarage => Mathf.Max(1, post50MinQueuedVehiclesPerGarage);
        public int Post50MaxQueuedVehiclesPerGarage => Mathf.Max(Post50MinQueuedVehiclesPerGarage, post50MaxQueuedVehiclesPerGarage);

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

        public void GetQueuedVehicleRange(float post50Pressure, out int minCount, out int maxCount)
        {
            post50Pressure = Mathf.Clamp01(post50Pressure);
            minCount = Mathf.RoundToInt(Mathf.Lerp(MinQueuedVehiclesPerGarage, Post50MinQueuedVehiclesPerGarage, post50Pressure));
            maxCount = Mathf.RoundToInt(Mathf.Lerp(MaxQueuedVehiclesPerGarage, Post50MaxQueuedVehiclesPerGarage, post50Pressure));
            minCount = Mathf.Clamp(minCount, 1, 8);
            maxCount = Mathf.Clamp(Mathf.Max(minCount, maxCount), 1, 8);
        }
    }

    public readonly struct MysteryVehicleGenerationProfile
    {
        public readonly bool Enabled;
        public readonly int MinVehicles;
        public readonly int MaxVehicles;
        public readonly float Ratio;

        public MysteryVehicleGenerationProfile(bool enabled, int minVehicles, int maxVehicles, float ratio)
        {
            Enabled = enabled;
            MinVehicles = Mathf.Max(0, minVehicles);
            MaxVehicles = Mathf.Max(MinVehicles, maxVehicles);
            Ratio = Mathf.Clamp01(ratio);
        }

        public static MysteryVehicleGenerationProfile Disabled => new MysteryVehicleGenerationProfile(false, 0, 0, 0f);
    }

    [Serializable]
    public sealed class MysteryVehicleGenerationRule
    {
        [SerializeField, Range(0, 50)] private int minVehicles = 5;
        [SerializeField, Range(0, 50)] private int maxVehicles = 12;
        [SerializeField, Range(0f, 1f)] private float earlyRatio = 0.18f;
        [SerializeField, Range(0f, 1f)] private float lateRatio = 0.30f;
        [SerializeField, Range(0, 50)] private int post50MinVehicles = 8;
        [SerializeField, Range(0, 50)] private int post50MaxVehicles = 20;
        [SerializeField, Range(0f, 1f)] private float post50Ratio = 0.46f;

        public int MinVehicles => Mathf.Max(0, minVehicles);
        public int MaxVehicles => Mathf.Max(MinVehicles, maxVehicles);
        public float EarlyRatio => Mathf.Clamp01(earlyRatio);
        public float LateRatio => Mathf.Clamp01(lateRatio);
        public int Post50MinVehicles => Mathf.Max(0, post50MinVehicles);
        public int Post50MaxVehicles => Mathf.Max(Post50MinVehicles, post50MaxVehicles);
        public float Post50Ratio => Mathf.Clamp01(post50Ratio);

        public MysteryVehicleGenerationProfile CreateProfile(float tension, float post50Pressure)
        {
            tension = Mathf.Clamp01(tension);
            post50Pressure = Mathf.Clamp01(post50Pressure);
            var baseRatio = Mathf.Lerp(EarlyRatio, LateRatio, tension);
            return new MysteryVehicleGenerationProfile(
                true,
                Mathf.RoundToInt(Mathf.Lerp(MinVehicles, Post50MinVehicles, post50Pressure)),
                Mathf.RoundToInt(Mathf.Lerp(MaxVehicles, Post50MaxVehicles, post50Pressure)),
                Mathf.Lerp(baseRatio, Post50Ratio, post50Pressure));
        }

        public static MysteryVehicleGenerationRule DefaultMystery()
        {
            return new MysteryVehicleGenerationRule();
        }

        public static MysteryVehicleGenerationRule DefaultLightMystery()
        {
            return new MysteryVehicleGenerationRule
            {
                minVehicles = 2,
                maxVehicles = 7,
                earlyRatio = 0.08f,
                lateRatio = 0.16f,
                post50MinVehicles = 5,
                post50MaxVehicles = 14,
                post50Ratio = 0.30f
            };
        }
    }

    [Serializable]
    public sealed class StagePatternEntry
    {
        [SerializeField] private LevelDifficulty difficulty = LevelDifficulty.Normal;
        [SerializeField] private StageModifierFlags modifiers = StageModifierFlags.None;

        public LevelDifficulty Difficulty => difficulty;
        public StageModifierFlags Modifiers => modifiers;

        public static StagePatternEntry Create(LevelDifficulty difficulty, StageModifierFlags modifiers)
        {
            return new StagePatternEntry
            {
                difficulty = difficulty,
                modifiers = modifiers
            };
        }
    }

    [CreateAssetMenu(menuName = "Bus Puzzle/Stage Generation Config", fileName = "StageGenerationConfig")]
    public sealed class StageGenerationConfig : ScriptableObject
    {
        public const int EndlessScheduleVersion = 2;

        private const int MinimumEndlessIntensity = 0;
        private const int MaximumEndlessIntensity = 5;

        [SerializeField, Range(1, 500)] private int generatedStageCount = 50;
        [SerializeField, Range(1, 500)] private int difficultyRampStartStage = 11;
        [SerializeField, Range(1, 500)] private int difficultyRampReferenceStage = 30;
        [SerializeField, Range(1, 500)] private int difficultyRampMaxStage = 50;
        [SerializeField, Range(1, 499)] private int post50RampStartStage = 50;
        [SerializeField, Range(2, 500)] private int post50RampMaxStage = 100;
        [SerializeField, Range(1, 512)] private int post50NormalMinSolutionCount = 24;
        [SerializeField, Range(1, 512)] private int post50NormalMaxSolutionCount = 90;
        [SerializeField, Range(1, 512)] private int post50HardMinSolutionCount = 4;
        [SerializeField, Range(1, 512)] private int post50HardMaxSolutionCount = 18;
        [SerializeField, Range(1, 512)] private int post50SuperHardMinSolutionCount = 1;
        [SerializeField, Range(1, 512)] private int post50SuperHardMaxSolutionCount = 5;
        [SerializeField, Range(LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity)] private int post50NormalRotaryCapacity = 20;
        [SerializeField, Range(LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity)] private int post50HardRotaryCapacity = 22;
        [SerializeField, Range(LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity)] private int post50SuperHardRotaryCapacity = 22;
        [SerializeField, Range(1, 5000)] private int longRunVehicleRampStartStage = 60;
        [SerializeField, Range(1f, 2000f)] private float longRunVehicleRampSoftnessStages = 220f;
        [SerializeField, Range(4, 80)] private int longRunNormalVehicleCap = 56;
        [SerializeField, Range(4, 80)] private int longRunHardVehicleCap = 62;
        [SerializeField, Range(4, 80)] private int longRunSuperHardVehicleCap = 72;
        [SerializeField] private int baseSeed = 10000;
        [SerializeField, Range(1, 300)] private int candidateAttemptsPerStage = 36;
        [SerializeField, Range(1, 80)] private int releaseVehicleGenerationAttempts = 8;
        [SerializeField, Range(512, 50000)] private int releaseSolutionNodeVisitLimit = 12000;
        [SerializeField, Range(10, 7200)] private int releaseBuildTimeBudgetSeconds = 600;
        [SerializeField, Range(1, 20)] private int runtimeCandidateAttemptsPerStage = 8;
        [SerializeField, Range(1, 80)] private int runtimeVehicleGenerationAttempts = 8;
        [SerializeField, Range(1, 512)] private int solutionCountLimit = 256;
        [SerializeField, Range(0, 10)] private int runtimePreloadAheadCount = 3;
        [SerializeField, Range(1f, 5000f)] private float endlessMasterySoftnessStages = 800f;
        [SerializeField, Range(0f, 0.75f)] private float endlessMasteryIntensityFloor = 0.75f;
        [SerializeField, Range(4, 80)] private int endlessNormalVehicleMin = 38;
        [SerializeField, Range(4, 80)] private int endlessNormalVehicleMax = 42;
        [SerializeField, Range(4, 80)] private int endlessHardVehicleMin = 43;
        [SerializeField, Range(4, 80)] private int endlessHardVehicleMax = 46;
        [SerializeField, Range(4, 80)] private int endlessSuperHardVehicleMin = 47;
        [SerializeField, Range(4, 80)] private int endlessSuperHardVehicleMax = 50;
        [SerializeField] private List<StagePatternEntry> stagePattern = new List<StagePatternEntry>
        {
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.MysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.SuperHard, StageModifierFlags.Garages | StageModifierFlags.LightMysteryVehicles)
        };
        // A prime-length challenge rhythm is intentionally different from the road and
        // layout cycles. Their combined tuple therefore takes a long time to repeat even
        // though every individual axis remains bounded and deterministic.
        [SerializeField] private List<StagePatternEntry> endlessStagePattern = new List<StagePatternEntry>
        {
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.MysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.SuperHard, StageModifierFlags.Garages | StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.MysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.SuperHard, StageModifierFlags.Garages),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.SuperHard, StageModifierFlags.Garages | StageModifierFlags.MysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.MysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.SuperHard, StageModifierFlags.Garages | StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.LightMysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None),
            StagePatternEntry.Create(LevelDifficulty.Hard, StageModifierFlags.MysteryVehicles),
            StagePatternEntry.Create(LevelDifficulty.SuperHard, StageModifierFlags.Garages | StageModifierFlags.MysteryVehicles)
        };
        [SerializeField] private List<int> endlessIntensityPattern = new List<int>
        {
            5, 4, 0, 3, 5, 4, 0, 2, 1, 1,
            3, 0, 4, 2, 1, 1, 0, 3, 2, 4,
            1, 0, 5, 2, 3, 1, 2, 0, 3
        };
        [SerializeField, HideInInspector] private List<LevelDifficulty> difficultyPattern = new List<LevelDifficulty>
        {
            LevelDifficulty.Normal,
            LevelDifficulty.Hard,
            LevelDifficulty.Normal,
            LevelDifficulty.Hard,
            LevelDifficulty.SuperHard
        };
        [SerializeField] private StageDifficultyGenerationRule normalRule = StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.Normal);
        [SerializeField] private StageDifficultyGenerationRule hardRule = StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.Hard);
        [SerializeField] private StageDifficultyGenerationRule superHardRule = StageDifficultyGenerationRule.DefaultFor(LevelDifficulty.SuperHard);
        [SerializeField] private GarageGenerationRule superHardGarageRule = new GarageGenerationRule();
        [SerializeField] private MysteryVehicleGenerationRule mysteryVehicleRule = MysteryVehicleGenerationRule.DefaultMystery();
        [SerializeField] private MysteryVehicleGenerationRule lightMysteryVehicleRule = MysteryVehicleGenerationRule.DefaultLightMystery();

        public int GeneratedStageCount => Mathf.Max(1, generatedStageCount);
        public int DifficultyRampStartStage => Mathf.Clamp(difficultyRampStartStage, 1, 500);
        public int DifficultyRampReferenceStage => Mathf.Clamp(difficultyRampReferenceStage, 1, 500);
        public int DifficultyRampMaxStage => Mathf.Max(DifficultyRampStartStage, Mathf.Clamp(difficultyRampMaxStage, 1, 500));
        public int Post50RampStartStage => Mathf.Clamp(post50RampStartStage, 1, 499);
        public int Post50RampMaxStage => Mathf.Max(Post50RampStartStage + 1, Mathf.Clamp(post50RampMaxStage, 2, 500));
        public int BaseSeed => baseSeed;
        public int CandidateAttemptsPerStage => Mathf.Max(1, candidateAttemptsPerStage);
        public int ReleaseVehicleGenerationAttempts => Mathf.Clamp(releaseVehicleGenerationAttempts, 1, 80);
        public int ReleaseSolutionNodeVisitLimit => Mathf.Clamp(releaseSolutionNodeVisitLimit, 512, 50000);
        public int ReleaseBuildTimeBudgetSeconds => Mathf.Clamp(releaseBuildTimeBudgetSeconds, 10, 7200);
        public int RuntimeCandidateAttemptsPerStage => Mathf.Clamp(runtimeCandidateAttemptsPerStage, 1, 20);
        public int RuntimeVehicleGenerationAttempts => Mathf.Clamp(runtimeVehicleGenerationAttempts, 1, 80);
        public int SolutionCountLimit => Mathf.Max(1, solutionCountLimit);
        public int RuntimePreloadAheadCount => Mathf.Clamp(runtimePreloadAheadCount, 0, 10);
        public int EndlessPatternLength => GetUsableEndlessPattern().Count;
        public int EndlessIntensityPatternLength => GetUsableEndlessIntensityPattern().Count;
        public float EndlessMasteryIntensityFloor => Mathf.Clamp(endlessMasteryIntensityFloor, 0f, 0.75f);
        public GarageGenerationRule SuperHardGarageRule => superHardGarageRule ?? new GarageGenerationRule();
        public MysteryVehicleGenerationRule MysteryVehicleRule => mysteryVehicleRule ?? MysteryVehicleGenerationRule.DefaultMystery();
        public MysteryVehicleGenerationRule LightMysteryVehicleRule => lightMysteryVehicleRule ?? MysteryVehicleGenerationRule.DefaultLightMystery();

        public StagePatternEntry GetPatternEntryForStage(int stageNumber)
        {
            if (stageNumber > GeneratedStageCount)
            {
                var endlessPattern = GetUsableEndlessPattern();
                var endlessEntry = endlessPattern[GetEndlessBeat(stageNumber)];
                if (endlessEntry != null)
                {
                    return endlessEntry;
                }
            }

            if (stagePattern != null && stagePattern.Count > 0)
            {
                var entry = stagePattern[Mathf.Abs(stageNumber - 1) % stagePattern.Count];
                if (entry != null)
                {
                    return entry;
                }
            }

            var difficulty = GetLegacyDifficultyForStage(stageNumber);
            return StagePatternEntry.Create(difficulty, GetDefaultModifiers(difficulty));
        }

        public LevelDifficulty GetDifficultyForStage(int stageNumber)
        {
            return GetPatternEntryForStage(stageNumber).Difficulty;
        }

        public StageModifierFlags GetModifiersForStage(int stageNumber)
        {
            var entry = GetPatternEntryForStage(stageNumber);
            if (stageNumber > GeneratedStageCount)
            {
                return entry.Modifiers;
            }

            return GetPost50AdjustedModifiers(entry.Difficulty, entry.Modifiers, GetPost50Pressure(stageNumber));
        }

        public int GetEndlessBeat(int stageNumber)
        {
            return GetEndlessPatternIndex(stageNumber, GetUsableEndlessPattern().Count);
        }

        public int GetEndlessEpoch(int stageNumber)
        {
            var patternLength = Mathf.Max(1, GetUsableEndlessPattern().Count);
            var zeroBasedEndlessStage = GetZeroBasedEndlessStage(stageNumber);
            return (int)Math.Min(int.MaxValue, zeroBasedEndlessStage / patternLength);
        }

        public int GetEndlessIntensity(int stageNumber)
        {
            var pattern = GetUsableEndlessIntensityPattern();
            var index = GetEndlessPatternIndex(stageNumber, pattern.Count);
            return Mathf.Clamp(pattern[index], MinimumEndlessIntensity, MaximumEndlessIntensity);
        }

        public float GetEndlessMasteryPressure(int stageNumber)
        {
            if (stageNumber <= GeneratedStageCount)
            {
                return 0f;
            }

            var zeroBasedEndlessStage = GetZeroBasedEndlessStage(stageNumber);
            var softness = Mathf.Max(1f, endlessMasterySoftnessStages);
            return Mathf.Clamp01(1f - Mathf.Exp(-(float)(zeroBasedEndlessStage / softness)));
        }

        public float GetEndlessChallengeProgress(int stageNumber)
        {
            var intensity = GetEndlessIntensity(stageNumber);
            var intensityProgress = Mathf.InverseLerp(
                MinimumEndlessIntensity,
                MaximumEndlessIntensity,
                intensity);
            // Intensity zero is a deliberate recovery beat. It must remain genuinely
            // lighter even after long-run mastery has raised the rest of the schedule.
            if (intensity <= MinimumEndlessIntensity)
            {
                return 0f;
            }

            var masteryPressure = GetEndlessMasteryPressure(stageNumber);
            var masteryFloor = masteryPressure * EndlessMasteryIntensityFloor;
            return Mathf.Clamp01(
                masteryFloor + intensityProgress * (1f - masteryFloor));
        }

        public StageModifierFlags GetPost50AdjustedModifiers(
            LevelDifficulty difficulty,
            StageModifierFlags modifiers,
            float post50Pressure)
        {
            post50Pressure = Mathf.Clamp01(post50Pressure);
            if (post50Pressure <= 0f)
            {
                return modifiers;
            }

            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return post50Pressure >= 0.35f
                        ? modifiers | StageModifierFlags.MysteryVehicles
                        : modifiers | StageModifierFlags.LightMysteryVehicles;
                case LevelDifficulty.SuperHard:
                    if (post50Pressure >= 0.70f)
                    {
                        return modifiers | StageModifierFlags.MysteryVehicles;
                    }

                    return post50Pressure >= 0.25f
                        ? modifiers | StageModifierFlags.LightMysteryVehicles
                        : modifiers;
                default:
                    return modifiers | StageModifierFlags.LightMysteryVehicles;
            }
        }

        public static StageModifierFlags GetDefaultModifiers(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return StageModifierFlags.MysteryVehicles;
                case LevelDifficulty.SuperHard:
                    return StageModifierFlags.Garages;
                default:
                    return StageModifierFlags.None;
            }
        }

        private LevelDifficulty GetLegacyDifficultyForStage(int stageNumber)
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
            var baseline = GetLinearProgress(stageNumber);
            if (stageNumber < DifficultyRampStartStage)
            {
                return baseline;
            }

            var referenceProgress = GetLinearProgress(DifficultyRampReferenceStage);
            var boostedProgress = Mathf.Lerp(
                referenceProgress,
                1f,
                Mathf.InverseLerp(DifficultyRampStartStage, DifficultyRampMaxStage, stageNumber));
            return Mathf.Clamp01(Mathf.Max(baseline, boostedProgress));
        }

        public float GetPost50Pressure(int stageNumber)
        {
            var startStage = Post50RampStartStage;
            if (stageNumber <= startStage)
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.InverseLerp(startStage, Post50RampMaxStage, stageNumber));
        }

        public LevelDifficultyProfile ApplyLongRunVehicleGrowth(LevelDifficultyProfile profile, int stageNumber)
        {
            if (profile == null)
            {
                return null;
            }

            if (stageNumber > GeneratedStageCount)
            {
                return ApplyEndlessProfileVariation(profile, stageNumber);
            }

            var pressure = GetLongRunVehiclePressure(stageNumber);
            if (pressure <= 0f)
            {
                return profile;
            }

            var baseVehicleCount = profile.TargetVehicleCount;
            var targetVehicleCount = Mathf.Max(baseVehicleCount, GetLongRunVehicleCap(profile.Difficulty));
            var vehicleCount = Mathf.RoundToInt(Mathf.Lerp(baseVehicleCount, targetVehicleCount, pressure));
            if (vehicleCount <= baseVehicleCount)
            {
                return profile;
            }

            return LevelDifficultyProfile.CreateCustom(
                profile.Difficulty,
                profile.PassengerFlowRule,
                vehicleCount,
                profile.TargetColorCount,
                profile.ParkingTension,
                profile.StationPressure,
                profile.RequireSolutionRoute);
        }

        private LevelDifficultyProfile ApplyEndlessProfileVariation(
            LevelDifficultyProfile profile,
            int stageNumber)
        {
            GetEndlessVehicleRange(profile.Difficulty, out var minVehicleCount, out var maxVehicleCount);
            var intensityProgress = GetEndlessChallengeProgress(stageNumber);
            var targetVehicleCount = Mathf.RoundToInt(
                Mathf.Lerp(minVehicleCount, maxVehicleCount, intensityProgress));

            int minColorCount;
            int maxColorCount;
            float minParkingTension;
            float maxParkingTension;
            float minStationPressure;
            float maxStationPressure;
            switch (profile.Difficulty)
            {
                case LevelDifficulty.Hard:
                    minColorCount = 9;
                    maxColorCount = 10;
                    minParkingTension = 0.68f;
                    maxParkingTension = 0.76f;
                    minStationPressure = 0.68f;
                    maxStationPressure = 0.76f;
                    break;
                case LevelDifficulty.SuperHard:
                    minColorCount = 11;
                    maxColorCount = 12;
                    minParkingTension = 0.78f;
                    maxParkingTension = 0.84f;
                    minStationPressure = 0.78f;
                    maxStationPressure = 0.84f;
                    break;
                default:
                    minColorCount = 8;
                    maxColorCount = 9;
                    minParkingTension = 0.58f;
                    maxParkingTension = 0.64f;
                    minStationPressure = 0.56f;
                    maxStationPressure = 0.62f;
                    break;
            }

            return LevelDifficultyProfile.CreateCustom(
                profile.Difficulty,
                profile.PassengerFlowRule,
                targetVehicleCount,
                Mathf.RoundToInt(Mathf.Lerp(minColorCount, maxColorCount, intensityProgress)),
                Mathf.Lerp(minParkingTension, maxParkingTension, intensityProgress),
                Mathf.Lerp(minStationPressure, maxStationPressure, intensityProgress),
                profile.RequireSolutionRoute);
        }

        public void GetSolutionRange(
            LevelDifficulty difficulty,
            int baseMinSolutionCount,
            int baseMaxSolutionCount,
            float post50Pressure,
            out int minSolutionCount,
            out int maxSolutionCount)
        {
            var targetMin = GetPost50MinSolutionCount(difficulty);
            var targetMax = GetPost50MaxSolutionCount(difficulty);
            post50Pressure = Mathf.Clamp01(post50Pressure);
            minSolutionCount = Mathf.RoundToInt(Mathf.Lerp(baseMinSolutionCount, targetMin, post50Pressure));
            maxSolutionCount = Mathf.RoundToInt(Mathf.Lerp(baseMaxSolutionCount, targetMax, post50Pressure));
            minSolutionCount = Mathf.Max(1, minSolutionCount);
            maxSolutionCount = Mathf.Max(minSolutionCount, maxSolutionCount);
        }

        public int GetRotaryCapacity(LevelDifficulty difficulty, int baseCapacity, float post50Pressure)
        {
            var targetCapacity = GetPost50RotaryCapacity(difficulty);
            return Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(baseCapacity, targetCapacity, Mathf.Clamp01(post50Pressure))),
                LevelData.MinRotaryUnitCapacity,
                LevelData.MaxRotaryUnitCapacity);
        }

        public MysteryVehicleGenerationProfile GetMysteryVehicleProfile(
            StageModifierFlags modifiers,
            LevelDifficultyProfile profile,
            float post50Pressure)
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
            return hasMystery
                ? MysteryVehicleRule.CreateProfile(tension, post50Pressure)
                : LightMysteryVehicleRule.CreateProfile(tension, post50Pressure);
        }

        private float GetLinearProgress(int stageNumber)
        {
            return GeneratedStageCount <= 1
                ? 0f
                : Mathf.Clamp01((stageNumber - 1f) / (GeneratedStageCount - 1f));
        }

        private float GetLongRunVehiclePressure(int stageNumber)
        {
            var startStage = Mathf.Max(1, longRunVehicleRampStartStage);
            if (stageNumber <= startStage)
            {
                return 0f;
            }

            var softness = Mathf.Max(1f, longRunVehicleRampSoftnessStages);
            return Mathf.Clamp01(1f - Mathf.Exp(-(stageNumber - startStage) / softness));
        }

        private int GetLongRunVehicleCap(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return Mathf.Clamp(longRunHardVehicleCap, 4, 80);
                case LevelDifficulty.SuperHard:
                    return Mathf.Clamp(longRunSuperHardVehicleCap, 4, 80);
                default:
                    return Mathf.Clamp(longRunNormalVehicleCap, 4, 80);
            }
        }

        private void GetEndlessVehicleRange(
            LevelDifficulty difficulty,
            out int minVehicleCount,
            out int maxVehicleCount)
        {
            // Reserve at least one vehicle between the complete tier bands. This keeps
            // Normal < Hard < SuperHard even if somebody later enters overlapping values
            // in the inspector, while still respecting LevelDifficultyProfile's hard cap.
            var normalMin = Mathf.Clamp(endlessNormalVehicleMin, 4, 78);
            var normalMax = Mathf.Clamp(
                Mathf.Max(normalMin, endlessNormalVehicleMax),
                normalMin,
                78);
            var hardMin = Mathf.Clamp(
                Mathf.Max(normalMax + 1, endlessHardVehicleMin),
                normalMax + 1,
                79);
            var hardMax = Mathf.Clamp(
                Mathf.Max(hardMin, endlessHardVehicleMax),
                hardMin,
                79);
            var superHardMin = Mathf.Clamp(
                Mathf.Max(hardMax + 1, endlessSuperHardVehicleMin),
                hardMax + 1,
                80);
            var superHardMax = Mathf.Clamp(
                Mathf.Max(superHardMin, endlessSuperHardVehicleMax),
                superHardMin,
                80);

            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    minVehicleCount = hardMin;
                    maxVehicleCount = hardMax;
                    break;
                case LevelDifficulty.SuperHard:
                    minVehicleCount = superHardMin;
                    maxVehicleCount = superHardMax;
                    break;
                default:
                    minVehicleCount = normalMin;
                    maxVehicleCount = normalMax;
                    break;
            }
        }

        private List<StagePatternEntry> GetUsableEndlessPattern()
        {
            if (endlessStagePattern != null && endlessStagePattern.Count > 0)
            {
                return endlessStagePattern;
            }

            if (stagePattern != null && stagePattern.Count > 0)
            {
                return stagePattern;
            }

            return new List<StagePatternEntry>
            {
                StagePatternEntry.Create(LevelDifficulty.Normal, StageModifierFlags.None)
            };
        }

        private List<int> GetUsableEndlessIntensityPattern()
        {
            if (endlessIntensityPattern != null && endlessIntensityPattern.Count > 0)
            {
                return endlessIntensityPattern;
            }

            return new List<int> { MinimumEndlessIntensity };
        }

        private int GetEndlessPatternIndex(int stageNumber, int patternLength)
        {
            patternLength = Mathf.Max(1, patternLength);
            return (int)(GetZeroBasedEndlessStage(stageNumber) % patternLength);
        }

        private long GetZeroBasedEndlessStage(int stageNumber)
        {
            return Math.Max(0L, (long)stageNumber - GeneratedStageCount - 1L);
        }

        private int GetPost50MinSolutionCount(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return Mathf.Max(1, post50HardMinSolutionCount);
                case LevelDifficulty.SuperHard:
                    return Mathf.Max(1, post50SuperHardMinSolutionCount);
                default:
                    return Mathf.Max(1, post50NormalMinSolutionCount);
            }
        }

        private int GetPost50MaxSolutionCount(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return Mathf.Max(GetPost50MinSolutionCount(difficulty), post50HardMaxSolutionCount);
                case LevelDifficulty.SuperHard:
                    return Mathf.Max(GetPost50MinSolutionCount(difficulty), post50SuperHardMaxSolutionCount);
                default:
                    return Mathf.Max(GetPost50MinSolutionCount(difficulty), post50NormalMaxSolutionCount);
            }
        }

        private int GetPost50RotaryCapacity(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return Mathf.Clamp(post50HardRotaryCapacity, LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity);
                case LevelDifficulty.SuperHard:
                    return Mathf.Clamp(post50SuperHardRotaryCapacity, LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity);
                default:
                    return Mathf.Clamp(post50NormalRotaryCapacity, LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity);
            }
        }
    }
}
