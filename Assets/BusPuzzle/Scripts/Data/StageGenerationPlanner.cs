using UnityEngine;

namespace BusPuzzle
{
    public readonly struct StageGenerationRequest
    {
        public readonly int StageNumber;
        public readonly int Seed;
        public readonly LevelDifficulty Difficulty;
        public readonly StageModifierFlags Modifiers;
        public readonly LevelDifficultyProfile Profile;
        public readonly float Progress;
        public readonly float Post50Pressure;
        public readonly RotaryRoadPresetId RoadPresetId;
        public readonly int VehicleLayoutVariantIndex;
        public readonly int VehicleLayoutVariantPoolSize;
        public readonly int GarageCount;
        public readonly int MinGarageQueuedVehicles;
        public readonly int MaxGarageQueuedVehicles;
        public readonly int RotaryCapacity;
        public readonly MysteryVehicleGenerationProfile MysteryVehicleProfile;
        public readonly int MinSolutionCount;
        public readonly int MaxSolutionCount;

        public StageGenerationRequest(
            int stageNumber,
            int seed,
            LevelDifficulty difficulty,
            StageModifierFlags modifiers,
            LevelDifficultyProfile profile,
            float progress,
            float post50Pressure,
            RotaryRoadPresetId roadPresetId,
            int vehicleLayoutVariantIndex,
            int vehicleLayoutVariantPoolSize,
            int garageCount,
            int minGarageQueuedVehicles,
            int maxGarageQueuedVehicles,
            int rotaryCapacity,
            MysteryVehicleGenerationProfile mysteryVehicleProfile,
            int minSolutionCount,
            int maxSolutionCount)
        {
            StageNumber = stageNumber;
            Seed = seed;
            Difficulty = difficulty;
            Modifiers = modifiers;
            Profile = profile;
            Progress = Mathf.Clamp01(progress);
            Post50Pressure = Mathf.Clamp01(post50Pressure);
            RoadPresetId = roadPresetId;
            VehicleLayoutVariantIndex = vehicleLayoutVariantIndex;
            VehicleLayoutVariantPoolSize = vehicleLayoutVariantPoolSize;
            GarageCount = garageCount;
            MinGarageQueuedVehicles = Mathf.Clamp(minGarageQueuedVehicles, 1, 8);
            MaxGarageQueuedVehicles = Mathf.Clamp(Mathf.Max(MinGarageQueuedVehicles, maxGarageQueuedVehicles), 1, 8);
            RotaryCapacity = Mathf.Clamp(rotaryCapacity, LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity);
            MysteryVehicleProfile = mysteryVehicleProfile;
            MinSolutionCount = Mathf.Max(1, minSolutionCount);
            MaxSolutionCount = Mathf.Max(MinSolutionCount, maxSolutionCount);
        }
    }

    public static class StageGenerationPlanner
    {
        private const int ShapeLibraryPreviewLineVehicleCount = 30;
        private const int ShapeLibraryPreviewFilledVehicleCount = 38;

        // Keep the pattern length stable while avoiding experimental shapes whose road offsets can overlap.
        // Snake/Clover/Cloud/Loop/Arrow/Ribbon presets remain available for validation passes before rotation.
        private static readonly RotaryRoadPresetId[] RoadPresetPattern =
        {
            RotaryRoadPresetId.SmallCircleTest,
            RotaryRoadPresetId.LargeCircleTest,
            RotaryRoadPresetId.OvalTest,
            RotaryRoadPresetId.RoundedSquareTest,
            RotaryRoadPresetId.HeartTest,
            RotaryRoadPresetId.LargeCircleTest,
            RotaryRoadPresetId.DropTest,
            RotaryRoadPresetId.OvalTest,
            RotaryRoadPresetId.HeartTest,
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

            var patternEntry = config.GetPatternEntryForStage(stageNumber);
            var difficulty = patternEntry.Difficulty;
            var progress = config.GetProgress(stageNumber);
            var post50Pressure = config.GetPost50Pressure(stageNumber);
            var modifiers = config.GetPost50AdjustedModifiers(difficulty, patternEntry.Modifiers, post50Pressure);
            var rule = config.GetRule(difficulty);
            var profile = config.ApplyLongRunVehicleGrowth(rule.CreateProfile(progress), stageNumber);
            var seed = config.BaseSeed + stageNumber * 1009;
            var random = new System.Random(seed);
            var garageCount = (modifiers & StageModifierFlags.Garages) != 0
                ? config.SuperHardGarageRule.PickGarageCount(random, progress)
                : 0;
            config.SuperHardGarageRule.GetQueuedVehicleRange(post50Pressure, out var minGarageQueue, out var maxGarageQueue);
            config.GetSolutionRange(
                difficulty,
                rule.MinSolutionCount,
                rule.MaxSolutionCount,
                post50Pressure,
                out var minSolutionCount,
                out var maxSolutionCount);
            var rotaryCapacity = config.GetRotaryCapacity(
                difficulty,
                LevelGenerator.GetRotaryCapacity(difficulty),
                post50Pressure);
            var mysteryVehicleProfile = config.GetMysteryVehicleProfile(modifiers, profile, post50Pressure);

            return new StageGenerationRequest(
                stageNumber,
                seed,
                difficulty,
                modifiers,
                profile,
                progress,
                post50Pressure,
                PickRoadPreset(stageNumber, config.BaseSeed),
                PickVehicleLayoutVariant(stageNumber, config.BaseSeed),
                VehicleLayoutPatternEngine.UniqueLayoutVariantCount,
                garageCount,
                minGarageQueue,
                maxGarageQueue,
                rotaryCapacity,
                mysteryVehicleProfile,
                minSolutionCount,
                maxSolutionCount);
        }

        public static StageGenerationRequest CreateShapeLibraryPreviewRequest(StageGenerationConfig config, int stageNumber)
        {
            var request = CreateRequest(config, stageNumber);
            var libraryIndex = stageNumber - 2;
            if (libraryIndex < 0 || libraryIndex >= VehicleLayoutPatternEngine.ShapeLibraryVariantCount)
            {
                return request;
            }

            var previewProfile = CreateShapeLibraryPreviewProfile(request.Profile, libraryIndex);
            return new StageGenerationRequest(
                request.StageNumber,
                request.Seed,
                request.Difficulty,
                request.Modifiers,
                previewProfile,
                request.Progress,
                request.Post50Pressure,
                request.RoadPresetId,
                VehicleLayoutPatternEngine.GetShapeLibraryVariantIndex(libraryIndex),
                VehicleLayoutPatternEngine.ShapeLibraryVariantCount,
                0,
                1,
                1,
                request.RotaryCapacity,
                MysteryVehicleGenerationProfile.Disabled,
                request.MinSolutionCount,
                request.MaxSolutionCount);
        }

        private static LevelDifficultyProfile CreateShapeLibraryPreviewProfile(
            LevelDifficultyProfile profile,
            int libraryIndex)
        {
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var targetVehicleCount = Mathf.Max(
                profile.TargetVehicleCount,
                GetShapeLibraryPreviewVehicleCount(libraryIndex));
            if (targetVehicleCount == profile.TargetVehicleCount)
            {
                return profile;
            }

            return LevelDifficultyProfile.CreateCustom(
                profile.Difficulty,
                profile.PassengerFlowRule,
                targetVehicleCount,
                profile.TargetColorCount,
                Mathf.Max(profile.ParkingTension, 0.54f),
                Mathf.Max(profile.StationPressure, 0.48f),
                profile.RequireSolutionRoute);
        }

        private static int GetShapeLibraryPreviewVehicleCount(int libraryIndex)
        {
            var libraryId = (VehicleShapeLibraryId)Mathf.Clamp(
                libraryIndex,
                0,
                VehicleShapeLayoutEngine.ShapeLibraryCount - 1);
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                    return ShapeLibraryPreviewLineVehicleCount;
                default:
                    return ShapeLibraryPreviewFilledVehicleCount;
            }
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
