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
        // The shipped 1-200 set was authored against eleven patterns x twenty variants.
        // Keep that permutation width stable even as new patterns are added for endless
        // runtime content, otherwise a generator update silently remaps every locked stage.
        private const int LockedReleaseLayoutVariantPoolSize = 220;

        public const int StarSizeMixVariantSeed = 41;

        public static int HeartShapeLibraryIndex => (int)VehicleShapeLibraryId.Heart;

        private const int ShapeLibraryPreviewLineVehicleCount = 38;
        private const int ShapeLibraryPreviewHardLineVehicleCount = 42;
        private const int ShapeLibraryPreviewSuperHardLineVehicleCount = 46;
        private const int ShapeLibraryPreviewPathVehicleCount = 10;
        private const int ShapeLibraryPreviewHardPathVehicleCount = 12;
        private const int ShapeLibraryPreviewSuperHardPathVehicleCount = 14;
        private const int ShapeLibraryPreviewLongPathVehicleCount = 10;
        private const int ShapeLibraryPreviewHardLongPathVehicleCount = 12;
        private const int ShapeLibraryPreviewSuperHardLongPathVehicleCount = 14;
        private const int ShapeLibraryPreviewRadialVehicleCount = 30;
        private const int ShapeLibraryPreviewHardRadialVehicleCount = 34;
        private const int ShapeLibraryPreviewSuperHardRadialVehicleCount = 38;
        private const int ShapeLibraryPreviewStarVehicleCount = 32;
        private const int ShapeLibraryPreviewHardStarVehicleCount = 34;
        private const int ShapeLibraryPreviewSuperHardStarVehicleCount = 40;
        private const int ShapeLibraryPreviewHollowVehicleCount = 28;
        private const int ShapeLibraryPreviewHardHollowVehicleCount = 32;
        private const int ShapeLibraryPreviewSuperHardHollowVehicleCount = 36;
        private const int ShapeLibraryPreviewGeometryVehicleCount = 34;
        private const int ShapeLibraryPreviewHardGeometryVehicleCount = 38;
        private const int ShapeLibraryPreviewSuperHardGeometryVehicleCount = 42;
        private const int ShapeLibraryPreviewMazeVehicleCount = 18;
        private const int ShapeLibraryPreviewHardMazeVehicleCount = 20;
        private const int ShapeLibraryPreviewSuperHardMazeVehicleCount = 22;
        private const int ShapeLibraryPreviewCrownVehicleCount = 24;
        private const int ShapeLibraryPreviewHardCrownVehicleCount = 28;
        private const int ShapeLibraryPreviewSuperHardCrownVehicleCount = 32;
        private const int ShapeLibraryPreviewEightVehicleCount = 24;
        private const int ShapeLibraryPreviewHardEightVehicleCount = 28;
        private const int ShapeLibraryPreviewSuperHardEightVehicleCount = 32;
        private const int ShapeLibraryPreviewFanVehicleCount = 12;
        private const int ShapeLibraryPreviewHardFanVehicleCount = 16;
        private const int ShapeLibraryPreviewSuperHardFanVehicleCount = 20;
        // Keep the preview generation budget bounded. Perceptual raster closing joins the
        // normal visual gaps between these non-overlapping vehicles; raising this into the
        // 50s makes dense placement prohibitively expensive without improving gameplay.
        private const int ShapeLibraryPreviewHeartVehicleCount = 32;
        private const int ShapeLibraryPreviewHardHeartVehicleCount = 34;
        private const int ShapeLibraryPreviewSuperHardHeartVehicleCount = 38;
        private const int ShapeLibraryPreviewIconVehicleCount = 30;
        private const int ShapeLibraryPreviewHardIconVehicleCount = 34;
        private const int ShapeLibraryPreviewSuperHardIconVehicleCount = 38;
        private const int ShapeLibraryPreviewNarrowVehicleCount = 18;
        private const int ShapeLibraryPreviewHardNarrowVehicleCount = 22;
        private const int ShapeLibraryPreviewSuperHardNarrowVehicleCount = 26;
        private const int ShapeLibraryPreviewSunburstVehicleCount = 16;
        private const int ShapeLibraryPreviewHardSunburstVehicleCount = 18;
        private const int ShapeLibraryPreviewSuperHardSunburstVehicleCount = 20;
        private const int ShapeLibraryPreviewFilledVehicleCount = 46;
        private const int ShapeLibraryPreviewHardFilledVehicleCount = 52;
        private const int ShapeLibraryPreviewSuperHardFilledVehicleCount = 56;

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

        private static readonly RotaryRoadPresetId[] EndlessRoadPresetPool =
        {
            RotaryRoadPresetId.SmallCircleTest,
            RotaryRoadPresetId.LargeCircleTest,
            RotaryRoadPresetId.OvalTest,
            RotaryRoadPresetId.RoundedSquareTest,
            RotaryRoadPresetId.HeartTest,
            RotaryRoadPresetId.DropTest,
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
            var modifiers = config.GetModifiersForStage(stageNumber);
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
            var vehicleLayoutVariant = PickVehicleLayoutVariant(
                stageNumber,
                config.BaseSeed,
                config.GeneratedStageCount);
            if (stageNumber > config.GeneratedStageCount)
            {
                vehicleLayoutVariant = VehicleLayoutPatternEngine.GetEndlessCompatibleLayoutVariantIndex(
                    profile,
                    vehicleLayoutVariant);
            }

            return new StageGenerationRequest(
                stageNumber,
                seed,
                difficulty,
                modifiers,
                profile,
                progress,
                post50Pressure,
                PickRoadPreset(stageNumber, config.BaseSeed, config.GeneratedStageCount),
                vehicleLayoutVariant,
                GetVehicleLayoutVariantPoolSize(stageNumber, config.GeneratedStageCount),
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
            return CreateShapeLibraryPreviewRequest(config, stageNumber, 0);
        }

        public static StageGenerationRequest CreateShapeLibraryPreviewRequest(
            StageGenerationConfig config,
            int stageNumber,
            int shapeLibraryVariantSeed)
        {
            var request = CreateRequest(config, stageNumber);
            var libraryIndex = stageNumber - 2;
            return CreateShapeLibraryPreviewRequestForLibrary(request, libraryIndex, shapeLibraryVariantSeed);
        }

        public static StageGenerationRequest CreateShapeLibraryPreviewRequestForLibrary(
            StageGenerationConfig config,
            int stageNumber,
            int libraryIndex,
            int shapeLibraryVariantSeed)
        {
            return CreateShapeLibraryPreviewRequestForLibrary(
                CreateRequest(config, stageNumber),
                libraryIndex,
                shapeLibraryVariantSeed);
        }

        private static StageGenerationRequest CreateShapeLibraryPreviewRequestForLibrary(
            StageGenerationRequest request,
            int libraryIndex,
            int shapeLibraryVariantSeed)
        {
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
                VehicleLayoutPatternEngine.GetShapeLibraryVariantIndex(libraryIndex, shapeLibraryVariantSeed),
                VehicleLayoutPatternEngine.ShapeLibraryVariantCount,
                0,
                1,
                1,
                request.RotaryCapacity,
                MysteryVehicleGenerationProfile.Disabled,
                1,
                1);
        }

        public static bool UsesStarShapeLibraryTemplate(StageGenerationRequest request)
        {
            return VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(request.VehicleLayoutVariantIndex, out var libraryIndex) &&
                (VehicleShapeLibraryId)libraryIndex == VehicleShapeLibraryId.Star;
        }

        public static bool UsesStarSizeMixShapeLibraryTemplate(StageGenerationRequest request)
        {
            return UsesStarShapeLibraryTemplate(request) &&
                VehicleLayoutPatternEngine.TryGetShapeLibraryVariantSeed(request.VehicleLayoutVariantIndex, out var variantSeed) &&
                variantSeed == StarSizeMixVariantSeed;
        }

        public static bool IsAutomaticTemplateBackedHeartRequest(
            StageGenerationRequest request,
            out bool fillInterior)
        {
            fillInterior = false;
            if (request.VehicleLayoutVariantIndex < 0 ||
                VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(
                    request.VehicleLayoutVariantIndex,
                    out _))
            {
                return false;
            }

            var profile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            if (!VehicleLayoutPatternEngine.TryCreateTemplateQualityShapeDefinition(
                    profile,
                    Mathf.Max(1, profile.TargetVehicleCount),
                    request.VehicleLayoutVariantIndex,
                    out var definition) ||
                (definition.LibraryId != VehicleShapeLibraryId.Heart &&
                 definition.LibraryId != VehicleShapeLibraryId.HeartArrow))
            {
                return false;
            }

            fillInterior = definition.FillInterior;
            return true;
        }

        private static LevelDifficultyProfile CreateShapeLibraryPreviewProfile(
            LevelDifficultyProfile profile,
            int libraryIndex)
        {
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var previewVehicleCount = GetShapeLibraryPreviewVehicleCount(libraryIndex, profile.Difficulty);
            var libraryId = (VehicleShapeLibraryId)Mathf.Clamp(
                libraryIndex,
                0,
                VehicleShapeLayoutEngine.ShapeLibraryCount - 1);
            var targetVehicleCount = UsesExactShapeLibraryPreviewCount(libraryId)
                ? previewVehicleCount
                : Mathf.Max(profile.TargetVehicleCount, previewVehicleCount);
            var targetColorCount = GetShapeLibraryPreviewColorCount(
                libraryId,
                previewVehicleCount,
                profile.TargetColorCount);
            if (targetVehicleCount == profile.TargetVehicleCount &&
                targetColorCount == profile.TargetColorCount)
            {
                return profile;
            }

            return LevelDifficultyProfile.CreateCustom(
                profile.Difficulty,
                profile.PassengerFlowRule,
                targetVehicleCount,
                targetColorCount,
                Mathf.Max(profile.ParkingTension, 0.54f),
                Mathf.Max(profile.StationPressure, 0.48f),
                profile.RequireSolutionRoute);
        }

        private static int GetShapeLibraryPreviewColorCount(
            VehicleShapeLibraryId libraryId,
            int previewVehicleCount,
            int defaultColorCount)
        {
            var targetColorCount = previewVehicleCount <= 14
                ? 5
                : previewVehicleCount <= 22
                    ? 6
                    : previewVehicleCount <= 32
                        ? 7
                        : defaultColorCount;
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Fan:
                    targetColorCount = Mathf.Min(targetColorCount, 5);
                    break;
            }

            return Mathf.Clamp(targetColorCount, 2, defaultColorCount);
        }

        private static bool UsesExactShapeLibraryPreviewCount(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static int GetShapeLibraryPreviewVehicleCount(int libraryIndex, LevelDifficulty difficulty)
        {
            var libraryId = (VehicleShapeLibraryId)Mathf.Clamp(
                libraryIndex,
                0,
                VehicleShapeLayoutEngine.ShapeLibraryCount - 1);
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardPathVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardPathVehicleCount
                            : ShapeLibraryPreviewPathVehicleCount;
                case VehicleShapeLibraryId.Stairs:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardLongPathVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardLongPathVehicleCount
                            : ShapeLibraryPreviewLongPathVehicleCount;
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardPathVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardPathVehicleCount
                            : ShapeLibraryPreviewPathVehicleCount;
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Spiral:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardLineVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardLineVehicleCount
                            : ShapeLibraryPreviewLineVehicleCount;
                case VehicleShapeLibraryId.HollowSquare:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardHollowVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardHollowVehicleCount
                            : ShapeLibraryPreviewHollowVehicleCount;
                case VehicleShapeLibraryId.Sunburst:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardSunburstVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardSunburstVehicleCount
                            : ShapeLibraryPreviewSunburstVehicleCount;
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Grid:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardGeometryVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardGeometryVehicleCount
                            : ShapeLibraryPreviewGeometryVehicleCount;
                case VehicleShapeLibraryId.Triangle:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardCrownVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardCrownVehicleCount
                            : ShapeLibraryPreviewCrownVehicleCount;
                case VehicleShapeLibraryId.Crown:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardCrownVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardCrownVehicleCount
                            : ShapeLibraryPreviewCrownVehicleCount;
                case VehicleShapeLibraryId.MazeBox:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardMazeVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardMazeVehicleCount
                            : ShapeLibraryPreviewMazeVehicleCount;
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardHeartVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardHeartVehicleCount
                            : ShapeLibraryPreviewHeartVehicleCount;
                case VehicleShapeLibraryId.Shield:
                case VehicleShapeLibraryId.Smile:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardIconVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardIconVehicleCount
                            : ShapeLibraryPreviewIconVehicleCount;
                case VehicleShapeLibraryId.Clover:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardFanVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardFanVehicleCount
                            : ShapeLibraryPreviewFanVehicleCount;
                case VehicleShapeLibraryId.Eight:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardEightVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardEightVehicleCount
                            : ShapeLibraryPreviewEightVehicleCount;
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardNarrowVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardNarrowVehicleCount
                            : ShapeLibraryPreviewNarrowVehicleCount;
                case VehicleShapeLibraryId.Flower:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardRadialVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardRadialVehicleCount
                            : ShapeLibraryPreviewRadialVehicleCount;
                case VehicleShapeLibraryId.Star:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardStarVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardStarVehicleCount
                            : ShapeLibraryPreviewStarVehicleCount;
                case VehicleShapeLibraryId.Fan:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardFanVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardFanVehicleCount
                            : ShapeLibraryPreviewFanVehicleCount;
                default:
                    return difficulty == LevelDifficulty.SuperHard
                        ? ShapeLibraryPreviewSuperHardFilledVehicleCount
                        : difficulty == LevelDifficulty.Hard
                            ? ShapeLibraryPreviewHardFilledVehicleCount
                            : ShapeLibraryPreviewFilledVehicleCount;
            }
        }

        private static RotaryRoadPresetId PickRoadPreset(
            int stageNumber,
            int baseSeed,
            int generatedStageCount)
        {
            if (stageNumber > generatedStageCount)
            {
                var zeroBasedEndlessStage = Mathf.Max(0, stageNumber - generatedStageCount - 1);
                var cycle = zeroBasedEndlessStage / EndlessRoadPresetPool.Length;
                var indexInCycle = zeroBasedEndlessStage % EndlessRoadPresetPool.Length;
                var previousCycleLastIndex = cycle > 0
                    ? PickShuffledPoolIndex(
                        EndlessRoadPresetPool.Length - 1,
                        EndlessRoadPresetPool.Length,
                        baseSeed + (cycle - 1) * 32452843)
                    : -1;
                var shuffledIndex = PickShuffledPoolIndex(
                    indexInCycle,
                    EndlessRoadPresetPool.Length,
                    baseSeed + cycle * 32452843,
                    previousCycleLastIndex);
                return EndlessRoadPresetPool[shuffledIndex];
            }

            var seedOffset = Mathf.Abs(baseSeed) % RoadPresetPattern.Length;
            var index = Mathf.Abs(stageNumber - 1 + seedOffset) % RoadPresetPattern.Length;
            return RoadPresetPattern[index];
        }

        private static int PickVehicleLayoutVariant(
            int stageNumber,
            int baseSeed,
            int generatedStageCount)
        {
            var poolSize = GetVehicleLayoutVariantPoolSize(stageNumber, generatedStageCount);
            if (poolSize <= 1)
            {
                return 0;
            }

            var zeroBasedStage = Mathf.Max(0, stageNumber - 1);
            var cycle = zeroBasedStage / poolSize;
            var indexInCycle = zeroBasedStage % poolSize;
            if (stageNumber <= generatedStageCount)
            {
                // This is the exact release-era mapping. Keep stages 1-200 byte-for-byte
                // reproducible; the boundary de-duplication below belongs only to endless
                // runtime content.
                return PickShuffledPoolIndex(
                    indexInCycle,
                    poolSize,
                    baseSeed + cycle * 15485863);
            }

            var previousCycleLastIndex = cycle > 0
                ? PickShuffledPoolIndex(
                    poolSize - 1,
                    poolSize,
                    baseSeed + (cycle - 1) * 15485863)
                : -1;
            return PickShuffledPoolIndex(
                indexInCycle,
                poolSize,
                baseSeed + cycle * 15485863,
                previousCycleLastIndex);
        }

        private static int GetVehicleLayoutVariantPoolSize(
            int stageNumber,
            int generatedStageCount)
        {
            var availablePoolSize = Mathf.Max(1, VehicleLayoutPatternEngine.UniqueLayoutVariantCount);
            return stageNumber <= generatedStageCount
                ? Mathf.Min(LockedReleaseLayoutVariantPoolSize, availablePoolSize)
                : availablePoolSize;
        }

        private static int PickShuffledPoolIndex(
            int indexInCycle,
            int poolSize,
            int seed,
            int forbiddenFirstValue = -1)
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

            if (values.Length > 1 && values[0] == forbiddenFirstValue)
            {
                var value = values[0];
                values[0] = values[1];
                values[1] = value;
            }

            return values[Mathf.Clamp(indexInCycle, 0, values.Length - 1)];
        }
    }
}
