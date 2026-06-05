using UnityEngine;

namespace BusPuzzle
{
    public readonly struct StageGenerationRequest
    {
        public readonly int StageNumber;
        public readonly int Seed;
        public readonly LevelDifficulty Difficulty;
        public readonly LevelDifficultyProfile Profile;
        public readonly RotaryRoadPresetId RoadPresetId;
        public readonly int GarageCount;
        public readonly int MinSolutionCount;
        public readonly int MaxSolutionCount;

        public StageGenerationRequest(
            int stageNumber,
            int seed,
            LevelDifficulty difficulty,
            LevelDifficultyProfile profile,
            RotaryRoadPresetId roadPresetId,
            int garageCount,
            int minSolutionCount,
            int maxSolutionCount)
        {
            StageNumber = stageNumber;
            Seed = seed;
            Difficulty = difficulty;
            Profile = profile;
            RoadPresetId = roadPresetId;
            GarageCount = garageCount;
            MinSolutionCount = minSolutionCount;
            MaxSolutionCount = maxSolutionCount;
        }
    }

    public static class StageGenerationPlanner
    {
        public static StageGenerationRequest CreateRequest(StageGenerationConfig config, int stageNumber)
        {
            config = config != null ? config : ScriptableObject.CreateInstance<StageGenerationConfig>();

            var difficulty = config.GetDifficultyForStage(stageNumber);
            var progress = config.GetProgress(stageNumber);
            var rule = config.GetRule(difficulty);
            var seed = config.BaseSeed + stageNumber * 1009;
            var random = new System.Random(seed);
            var garageCount = difficulty == LevelDifficulty.SuperHard
                ? config.SuperHardGarageRule.PickGarageCount(random, progress)
                : 0;

            return new StageGenerationRequest(
                stageNumber,
                seed,
                difficulty,
                rule.CreateProfile(progress),
                PickRoadPreset(difficulty, random),
                garageCount,
                rule.MinSolutionCount,
                rule.MaxSolutionCount);
        }

        private static RotaryRoadPresetId PickRoadPreset(LevelDifficulty difficulty, System.Random random)
        {
            if (difficulty == LevelDifficulty.SuperHard)
            {
                return random.NextDouble() < 0.65d ? RotaryRoadPresetId.Large : RotaryRoadPresetId.Medium;
            }

            if (difficulty == LevelDifficulty.Hard)
            {
                return random.NextDouble() < 0.55d ? RotaryRoadPresetId.Medium : RotaryRoadPresetId.Small;
            }

            return random.NextDouble() < 0.70d ? RotaryRoadPresetId.Small : RotaryRoadPresetId.Medium;
        }
    }
}
