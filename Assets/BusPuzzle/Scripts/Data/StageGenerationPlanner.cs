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
        public readonly int VehicleLayoutVariantIndex;
        public readonly int VehicleLayoutVariantPoolSize;
        public readonly int GarageCount;
        public readonly int MinSolutionCount;
        public readonly int MaxSolutionCount;

        public StageGenerationRequest(
            int stageNumber,
            int seed,
            LevelDifficulty difficulty,
            LevelDifficultyProfile profile,
            RotaryRoadPresetId roadPresetId,
            int vehicleLayoutVariantIndex,
            int vehicleLayoutVariantPoolSize,
            int garageCount,
            int minSolutionCount,
            int maxSolutionCount)
        {
            StageNumber = stageNumber;
            Seed = seed;
            Difficulty = difficulty;
            Profile = profile;
            RoadPresetId = roadPresetId;
            VehicleLayoutVariantIndex = vehicleLayoutVariantIndex;
            VehicleLayoutVariantPoolSize = vehicleLayoutVariantPoolSize;
            GarageCount = garageCount;
            MinSolutionCount = minSolutionCount;
            MaxSolutionCount = maxSolutionCount;
        }
    }

    public static class StageGenerationPlanner
    {
        private static readonly RotaryRoadPresetId[] RoadPresetPattern =
        {
            RotaryRoadPresetId.CompactOval,
            RotaryRoadPresetId.WideTerminal,
            RotaryRoadPresetId.TallTerminal,
            RotaryRoadPresetId.LeftHook,
            RotaryRoadPresetId.RightHook,
            RotaryRoadPresetId.Roundabout,
            RotaryRoadPresetId.Small,
            RotaryRoadPresetId.Medium,
            RotaryRoadPresetId.Large
        };

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
                PickRoadPreset(stageNumber, config.BaseSeed),
                PickVehicleLayoutVariant(stageNumber, config.BaseSeed),
                VehicleLayoutPatternEngine.UniqueLayoutVariantCount,
                garageCount,
                rule.MinSolutionCount,
                rule.MaxSolutionCount);
        }

        private static RotaryRoadPresetId PickRoadPreset(int stageNumber, int baseSeed)
        {
            var seedOffset = Mathf.Abs(baseSeed) % RoadPresetPattern.Length;
            var index = Mathf.Abs(stageNumber - 1 + seedOffset) % RoadPresetPattern.Length;
            return RoadPresetPattern[index];
        }

        private static int PickVehicleLayoutVariant(int stageNumber, int baseSeed)
        {
            var poolSize = VehicleLayoutPatternEngine.UniqueLayoutVariantCount;
            if (poolSize <= 1)
            {
                return 0;
            }

            var zeroBasedStage = Mathf.Max(0, stageNumber - 1);
            var cycle = zeroBasedStage / poolSize;
            var indexInCycle = zeroBasedStage % poolSize;
            return PickShuffledPoolIndex(indexInCycle, poolSize, baseSeed + cycle * 15485863);
        }

        private static int PickShuffledPoolIndex(int indexInCycle, int poolSize, int seed)
        {
            var values = new int[poolSize];
            for (var index = 0; index < values.Length; index++)
            {
                values[index] = index;
            }

            var random = new System.Random(seed);
            for (var index = values.Length - 1; index > 0; index--)
            {
                var swapIndex = random.Next(0, index + 1);
                var value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }

            return values[Mathf.Clamp(indexInCycle, 0, values.Length - 1)];
        }
    }
}
