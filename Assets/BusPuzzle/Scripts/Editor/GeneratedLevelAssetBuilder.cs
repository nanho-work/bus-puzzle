#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BusPuzzle.EditorTools
{
    public static class GeneratedLevelAssetBuilder
    {
        private const string LevelDirectory = "Assets/BusPuzzle/Resources/Levels";
        private const string GeneratedLevelDirectory = LevelDirectory + "/Generated";
        private const string ShapeTemplateDirectory = "Assets/BusPuzzle/Resources/ShapeTemplates";
        private const string StarShapeTemplateDirectory = ShapeTemplateDirectory + "/Star";
        private const string StarBasicShapeTemplatePath = StarShapeTemplateDirectory + "/Star_Basic_01.asset";
        private const string StarBasicShapeTemplateDisplayName = "Star Basic 01";
        private const string HeartShapeTemplateDirectory = ShapeTemplateDirectory + "/Heart";
        private const string HeartBasicShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_Basic_01.asset";
        private const string HeartBasicShapeTemplateDisplayName = "Heart Basic 01";
        private const string HeartDirectionMixShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_DirectionMix_01.asset";
        private const string HeartDirectionMixShapeTemplateDisplayName = "Heart Direction Mix 01";
        private const string ActiveLevelSequencePath = LevelDirectory + "/LevelSequence.asset";
        private const string GeneratedLevelSequencePath = GeneratedLevelDirectory + "/GeneratedLevelSequence.asset";
        private const string StageGenerationConfigPath = LevelDirectory + "/StageGenerationConfig.asset";
        private const int ShapeTemplatePreviewStageNumber = 9;
        private const int GeneratedStageBatchSize = 25;
        private const int PreviewStageCount = 100;
        private const int ShapeLibraryPreviewStageCount = 31;
        private const int DefaultVisualPreviewMinimumOpeningMoveCount = 3;
        private const int StarPreviewMinimumOpeningMoveCount = 8;
        private const int StarPreviewMaximumOpeningMoveCount = 12;
        private const int StarSizeMixPreviewMaximumOpeningMoveCount = 16;
        private const int ManualHeartGridColumns = 14;
        private const int ManualHeartGridRows = 14;
        private const int ManualShapeSolutionNodeVisitLimit = 50000;

        private enum GeneratedStageBuildMode
        {
            FullSet,
            NextBatch,
            PreviewFirst100,
            ShapeLibraryPreview
        }

        private enum ManualHeartDirectionMode
        {
            Reference,
            DirectionMix
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Generated Stage Set")]
        public static void RebuildGeneratedStageSet()
        {
            BuildGeneratedStageSet(GeneratedStageBuildMode.FullSet);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild First 100 Generated Stages")]
        public static void RebuildFirst100GeneratedStages()
        {
            BuildGeneratedStageSet(GeneratedStageBuildMode.PreviewFirst100);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stages")]
        public static void RebuildShapeLibraryPreviewStages()
        {
            BuildGeneratedStageSet(GeneratedStageBuildMode.ShapeLibraryPreview);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Star")]
        public static void RebuildShapeLibraryPreviewStage09Star()
        {
            RebuildSingleShapeLibraryPreviewStage(ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Star Size Mix")]
        public static void RebuildShapeLibraryPreviewStage09StarSizeMix()
        {
            RebuildSingleShapeLibraryPreviewStage(ShapeTemplatePreviewStageNumber, StageGenerationPlanner.StarSizeMixVariantSeed);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart")]
        public static void RebuildShapeLibraryPreviewStage09Heart()
        {
            RebuildStage09ManualHeart(ManualHeartDirectionMode.Reference);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Direction Mix")]
        public static void RebuildShapeLibraryPreviewStage09HeartDirectionMix()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartDirectionMode.DirectionMix);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Star Basic 01")]
        public static void SaveStage09PreviewAsStarBasicTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                StarBasicShapeTemplatePath,
                StarBasicShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Star Basic 01 Into Stage 09 Preview")]
        public static void LoadStarBasicTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                StarBasicShapeTemplatePath,
                StarBasicShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Star Basic 01")]
        public static void ValidateStarBasicShapeTemplate()
        {
            ValidateShapeTemplate(StarBasicShapeTemplatePath, StarBasicShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Basic 01")]
        public static void SaveStage09PreviewAsHeartBasicTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartBasicShapeTemplatePath,
                HeartBasicShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Basic 01 Into Stage 09 Preview")]
        public static void LoadHeartBasicTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartBasicShapeTemplatePath,
                HeartBasicShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Basic 01")]
        public static void ValidateHeartBasicShapeTemplate()
        {
            ValidateShapeTemplate(HeartBasicShapeTemplatePath, HeartBasicShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Direction Mix 01")]
        public static void SaveStage09PreviewAsHeartDirectionMixTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartDirectionMixShapeTemplatePath,
                HeartDirectionMixShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Direction Mix 01 Into Stage 09 Preview")]
        public static void LoadHeartDirectionMixTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartDirectionMixShapeTemplatePath,
                HeartDirectionMixShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Direction Mix 01")]
        public static void ValidateHeartDirectionMixShapeTemplate()
        {
            ValidateShapeTemplate(HeartDirectionMixShapeTemplatePath, HeartDirectionMixShapeTemplateDisplayName);
        }

        public static void RebuildShapeLibraryPreviewStageFromCommandLine()
        {
            RebuildSingleShapeLibraryPreviewStage(ReadCommandLineStageNumber(ShapeTemplatePreviewStageNumber));
        }

        [MenuItem("Bus Puzzle/Levels/Build Next Generated Stage Batch")]
        public static void BuildNextGeneratedStageBatch()
        {
            BuildGeneratedStageSet(GeneratedStageBuildMode.NextBatch);
        }

        [MenuItem("Bus Puzzle/Levels/Refresh Generated Stage Sequence From Existing Levels")]
        public static void RefreshGeneratedStageSequenceFromExistingLevels()
        {
            var config = LoadConfig();
            Directory.CreateDirectory(GeneratedLevelDirectory);
            var savedLevels = new LevelData[config.GeneratedStageCount];
            var completedStageCount = LoadExistingGeneratedPrefix(config, savedLevels);

            SaveCompletedGeneratedSequence(
                savedLevels,
                completedStageCount,
                $"Refreshed generated stage sequences from {completedStageCount}/{config.GeneratedStageCount} existing levels.",
                true);
        }

        private static void BuildGeneratedStageSet(GeneratedStageBuildMode mode)
        {
            var config = LoadConfig();
            Directory.CreateDirectory(GeneratedLevelDirectory);
            var savedLevels = new LevelData[config.GeneratedStageCount];
            var completedStageCount = mode == GeneratedStageBuildMode.NextBatch
                ? LoadExistingGeneratedPrefix(config, savedLevels)
                : 0;
            var startStage = mode == GeneratedStageBuildMode.NextBatch
                ? completedStageCount + 1
                : 1;
            var targetStage = GetTargetStage(config, mode, completedStageCount);
            var displayStageCount = mode == GeneratedStageBuildMode.PreviewFirst100 ||
                mode == GeneratedStageBuildMode.ShapeLibraryPreview
                ? targetStage
                : config.GeneratedStageCount;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var cancelled = false;
            var timedOut = false;

            if (startStage > config.GeneratedStageCount)
            {
                SaveCompletedGeneratedSequence(
                    savedLevels,
                    config.GeneratedStageCount,
                    $"Generated Bus Pop stage set is already complete: {config.GeneratedStageCount}/{config.GeneratedStageCount}.",
                    true);
                return;
            }

            try
            {
                for (var stageNumber = startStage; stageNumber <= targetStage; stageNumber++)
                {
                    if (mode == GeneratedStageBuildMode.ShapeLibraryPreview &&
                        stageNumber == 1 &&
                        TryLoadExistingLevel("Level_001", out var tutorialLevel))
                    {
                        savedLevels[stageNumber - 1] = tutorialLevel;
                        completedStageCount = stageNumber;
                        Debug.Log($"Kept existing tutorial stage {stageNumber:000}/{displayStageCount:000}.");
                        continue;
                    }

                    var request = CreateRequest(config, mode, stageNumber);
                    if (TryReuseExistingLevel(config, request, out var existingLevel))
                    {
                        savedLevels[stageNumber - 1] = existingLevel;
                        completedStageCount = stageNumber;
                        Debug.Log($"Reused generated stage {stageNumber:000}/{config.GeneratedStageCount}: {request.Difficulty}.");
                        continue;
                    }

                    if (!StageCandidateBuilder.TryBuildVerifiedStageCandidate(
                        config,
                        request,
                        out var generatedLevel,
                        out var report,
                        out var analysis,
                        candidate =>
                        {
                            var runStageCount = Mathf.Max(1, targetStage - startStage + 1);
                            var stageProgress = (stageNumber - startStage + candidate / (float)config.CandidateAttemptsPerStage) /
                                runStageCount;
                            cancelled = EditorUtility.DisplayCancelableProgressBar(
                                GetProgressTitle(mode),
                                $"Stage {stageNumber:000}/{displayStageCount:000} - candidate {candidate + 1}/{config.CandidateAttemptsPerStage}",
                                Mathf.Clamp01(stageProgress));
                            if (cancelled)
                            {
                                return true;
                            }

                            timedOut = stopwatch.Elapsed.TotalSeconds > config.ReleaseBuildTimeBudgetSeconds;
                            return timedOut;
                        }))
                    {
                        if (cancelled)
                        {
                            SaveCompletedGeneratedSequence(
                                savedLevels,
                                completedStageCount,
                                $"Saved partial generated stage sequence after cancellation at stage {stageNumber:000}.",
                                true);
                            Debug.LogWarning($"Generated stage set rebuild cancelled at stage {stageNumber:000}.");
                            return;
                        }

                        if (timedOut)
                        {
                            SaveCompletedGeneratedSequence(
                                savedLevels,
                                completedStageCount,
                                $"Saved partial generated stage sequence after timeout at stage {stageNumber:000}.",
                                true);
                            Debug.LogError(
                                $"Generated stage set rebuild timed out at stage {stageNumber:000} after " +
                                $"{config.ReleaseBuildTimeBudgetSeconds} seconds. Lower generation pressure or try again.");
                            return;
                        }

                        SaveCompletedGeneratedSequence(
                            savedLevels,
                            completedStageCount,
                            $"Saved partial generated stage sequence after generation failed at stage {stageNumber:000}.",
                            true);
                        Debug.LogError(
                            $"Generated stage set rebuild aborted at stage {stageNumber:000}. " +
                            $"No verified candidate found after {config.CandidateAttemptsPerStage} attempts. " +
                            $"Last solution count: {analysis.SolutionCount}, target range: {request.MinSolutionCount}-{request.MaxSolutionCount}. " +
                            $"{CreateReportMessage(generatedLevel, report)}");
                        return;
                    }

                    generatedLevel.SetGenerationMetadata(
                        StageGenerationSignature.Create(config, request),
                        analysis.SolutionCount);
                    savedLevels[stageNumber - 1] = SaveLevel($"Level_{stageNumber:000}", generatedLevel);
                    completedStageCount = stageNumber;
                    if (stageNumber % 5 == 0)
                    {
                        SaveCompletedGeneratedSequence(savedLevels, completedStageCount, null, false);
                        AssetDatabase.SaveAssets();
                    }

                    Debug.Log(
                        $"Generated stage {stageNumber:000}/{displayStageCount:000}: " +
                        $"{request.Difficulty}, solutions {analysis.SolutionCount}, road {request.RoadPresetId}.");
                }

                EditorUtility.DisplayProgressBar(GetProgressTitle(mode), "Saving generated level sequence...", 1f);
                var completedMessage = mode == GeneratedStageBuildMode.PreviewFirst100
                    ? $"Generated Bus Pop preview stage set rebuilt: {completedStageCount}/{config.GeneratedStageCount} verified levels saved under {GeneratedLevelDirectory}."
                    : mode == GeneratedStageBuildMode.ShapeLibraryPreview
                    ? $"Generated Bus Pop shape library preview rebuilt: {completedStageCount}/{config.GeneratedStageCount} verified levels saved under {GeneratedLevelDirectory}."
                    : completedStageCount >= config.GeneratedStageCount
                    ? $"Generated Bus Pop stage set rebuilt: {completedStageCount}/{config.GeneratedStageCount} verified levels saved under {GeneratedLevelDirectory}."
                    : $"Generated Bus Pop stage batch saved: {completedStageCount}/{config.GeneratedStageCount} verified levels are now available.";
                SaveCompletedGeneratedSequence(savedLevels, completedStageCount, completedMessage, true);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string GetProgressTitle(GeneratedStageBuildMode mode)
        {
            switch (mode)
            {
                case GeneratedStageBuildMode.NextBatch:
                    return "Building Next Bus Pop Stage Batch";
                case GeneratedStageBuildMode.PreviewFirst100:
                    return "Rebuilding First 100 Bus Pop Stages";
                case GeneratedStageBuildMode.ShapeLibraryPreview:
                    return "Rebuilding Shape Library Preview";
                default:
                    return "Rebuilding Bus Pop Stage Set";
            }
        }

        private static int GetTargetStage(
            StageGenerationConfig config,
            GeneratedStageBuildMode mode,
            int completedStageCount)
        {
            switch (mode)
            {
                case GeneratedStageBuildMode.NextBatch:
                    return Mathf.Min(config.GeneratedStageCount, completedStageCount + GeneratedStageBatchSize);
                case GeneratedStageBuildMode.PreviewFirst100:
                    return Mathf.Min(config.GeneratedStageCount, PreviewStageCount);
                case GeneratedStageBuildMode.ShapeLibraryPreview:
                    return Mathf.Min(config.GeneratedStageCount, ShapeLibraryPreviewStageCount);
                default:
                    return config.GeneratedStageCount;
            }
        }

        private static StageGenerationRequest CreateRequest(
            StageGenerationConfig config,
            GeneratedStageBuildMode mode,
            int stageNumber)
        {
            return mode == GeneratedStageBuildMode.ShapeLibraryPreview
                ? StageGenerationPlanner.CreateShapeLibraryPreviewRequest(config, stageNumber)
                : StageGenerationPlanner.CreateRequest(config, stageNumber);
        }

        private static void RebuildSingleShapeLibraryPreviewStage(int stageNumber)
        {
            RebuildSingleShapeLibraryPreviewStage(stageNumber, 0);
        }

        private static void RebuildSingleShapeLibraryPreviewStage(
            int stageNumber,
            int shapeLibraryVariantSeed)
        {
            RebuildSingleShapeLibraryPreviewStage(stageNumber, -1, shapeLibraryVariantSeed);
        }

        private static void RebuildSingleShapeLibraryPreviewStage(
            int stageNumber,
            int shapeLibraryIndexOverride,
            int shapeLibraryVariantSeed)
        {
            var maxShapeLibraryStage = ShapeLibraryPreviewStageCount;
            if (stageNumber < 2 || stageNumber > maxShapeLibraryStage)
            {
                var message = $"Shape library preview stage must be between 2 and {maxShapeLibraryStage}; got {stageNumber}.";
                Debug.LogError(message);
                if (Application.isBatchMode)
                {
                    throw new System.InvalidOperationException(message);
                }

                return;
            }

            var config = LoadConfig();
            Directory.CreateDirectory(GeneratedLevelDirectory);
            var request = shapeLibraryIndexOverride >= 0
                ? StageGenerationPlanner.CreateShapeLibraryPreviewRequestForLibrary(
                    config,
                    stageNumber,
                    shapeLibraryIndexOverride,
                    shapeLibraryVariantSeed)
                : StageGenerationPlanner.CreateShapeLibraryPreviewRequest(config, stageNumber, shapeLibraryVariantSeed);
            var generationSignature = StageGenerationSignature.Create(config, request);
            var minimumVisualPreviewVehicleCount = ShapeLibraryVehicleCoverage.GetMinimumVehicleCount(
                request.Profile,
                request.VehicleLayoutVariantIndex);
            GetVisualPreviewOpeningMoveRange(request, out var minimumOpeningMoveCount, out var maximumOpeningMoveCount);
            var openingMoveCount = 0;
            var cancelled = false;
            LevelData generatedLevel = null;
            LevelValidationReport report = null;
            var candidateAttempts = StageGenerationPlanner.UsesStarSizeMixShapeLibraryTemplate(request)
                ? config.CandidateAttemptsPerStage * 4
                : config.CandidateAttemptsPerStage;
            var validationRejectedCount = 0;
            var vehicleCountRejectedCount = 0;
            var sizeMixRejectedCount = 0;
            var openingRejectedCount = 0;
            var greedyRejectedCount = 0;
            try
            {
                for (var candidate = 0; candidate < candidateAttempts; candidate++)
                {
                    cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Rebuilding Single Shape Library Preview",
                        $"Stage {stageNumber:000} visual candidate {candidate + 1}/{candidateAttempts}",
                        Mathf.Clamp01(candidate / (float)Mathf.Max(1, candidateAttempts)));
                    if (cancelled)
                    {
                        break;
                    }

                    var candidateLevel = LevelGenerator.CreateRuntimeStage(
                        request,
                        config.SuperHardGarageRule,
                        candidate,
                        config.ReleaseVehicleGenerationAttempts,
                        false,
                        true);
                    if (!TryApplyVisualPreviewVariantAdjustments(candidateLevel, request, candidate))
                    {
                        sizeMixRejectedCount++;
                        continue;
                    }

                    candidateLevel.SetGenerationMetadata(generationSignature, 1);
                    report = LevelValidator.Validate(candidateLevel, false);
                    if (report.HasErrors)
                    {
                        validationRejectedCount++;
                        continue;
                    }

                    if (candidateLevel.Buses == null ||
                        candidateLevel.Buses.Count < minimumVisualPreviewVehicleCount)
                    {
                        vehicleCountRejectedCount++;
                        continue;
                    }

                    if (!HasRequiredVisualPreviewVehicleSizes(request, candidateLevel.Buses))
                    {
                        sizeMixRejectedCount++;
                        continue;
                    }

                    openingMoveCount = LevelGenerator.CountOpeningMoves(candidateLevel.Buses);
                    var hasGreedyExitOrder = LevelGenerator.HasGreedyExitOrder(candidateLevel.Buses);
                    if (openingMoveCount < minimumOpeningMoveCount ||
                        openingMoveCount > maximumOpeningMoveCount ||
                        !hasGreedyExitOrder)
                    {
                        if (!TryApplyOpeningMoveConstraint(
                            candidateLevel,
                            request,
                            candidate,
                            generationSignature,
                            minimumOpeningMoveCount,
                            maximumOpeningMoveCount,
                            out openingMoveCount,
                            out report))
                        {
                            openingRejectedCount++;
                            continue;
                        }
                    }

                    if (!StageGenerationPlanner.UsesStarSizeMixShapeLibraryTemplate(request) &&
                        !LevelGenerator.HasGreedyExitOrder(candidateLevel.Buses))
                    {
                        greedyRejectedCount++;
                        continue;
                    }

                    generatedLevel = candidateLevel;
                    break;
                }

                if (generatedLevel == null)
                {
                    var message = cancelled
                        ? $"Shape library preview stage {stageNumber:000} rebuild cancelled."
                        : $"Failed to rebuild shape library preview stage {stageNumber:000}. " +
                        $"{CreateReportMessage(generatedLevel, report)} " +
                        $"Rejects validation={validationRejectedCount}, vehicles={vehicleCountRejectedCount}, " +
                        $"sizes={sizeMixRejectedCount}, opening={openingRejectedCount}, greedy={greedyRejectedCount}.";
                    Debug.LogError(message);
                    if (Application.isBatchMode)
                    {
                        throw new System.InvalidOperationException(message);
                    }

                    return;
                }

                generatedLevel.SetGenerationMetadata(generationSignature, 1);
                SaveLevel($"Level_{stageNumber:000}", generatedLevel);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    $"Rebuilt visual shape library preview stage {stageNumber:000}: " +
                    $"{request.Difficulty}, vehicles {generatedLevel.Buses.Count}, " +
                    $"opening moves {openingMoveCount}, road {request.RoadPresetId}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void RebuildStage09ManualHeart(ManualHeartDirectionMode directionMode)
        {
            var level = CreateManualHeartLevel(directionMode, $"Stage {ShapeTemplatePreviewStageNumber:000} Hard");
            if (level == null)
            {
                return;
            }

            var vehicles = level.Buses;
            var openingMoveCount = LevelGenerator.CountOpeningMoves(level.Buses);
            SaveLevel($"Level_{ShapeTemplatePreviewStageNumber:000}", level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Rebuilt manual heart {GetManualHeartModeLogName(directionMode)} preview stage {ShapeTemplatePreviewStageNumber:000}: " +
                $"vehicles {vehicles.Count}, opening moves {openingMoveCount}, " +
                $"solutions {level.GenerationSolutionCount}, road {level.RoadPresetId}.");
        }

        private static void SaveManualHeartReferenceTemplate()
        {
            var referenceLevel = CreateManualHeartLevel(
                ManualHeartDirectionMode.Reference,
                HeartBasicShapeTemplateDisplayName);
            if (referenceLevel == null)
            {
                return;
            }

            var template = SaveLevelAssetCopy(
                referenceLevel,
                HeartBasicShapeTemplatePath,
                Path.GetFileNameWithoutExtension(HeartBasicShapeTemplatePath),
                HeartBasicShapeTemplateDisplayName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Saved manual heart reference template {HeartBasicShapeTemplateDisplayName}: " +
                $"{HeartBasicShapeTemplatePath}. {CreateLevelSummary(template)}.");
        }

        private static LevelData CreateManualHeartLevel(ManualHeartDirectionMode directionMode, string levelName)
        {
            var vehicles = CreateManualHeartReferenceVehicles(directionMode);
            var profile = LevelDifficultyProfile.CreateCustom(
                LevelDifficulty.Hard,
                vehicles.Count,
                9,
                0.54f,
                0.48f,
                true);
            var flowPlan = LevelGenerator.BuildPassengerFlowPlanFromVehicleOrder(
                profile,
                vehicles,
                GetManualHeartPassengerSeed(directionMode));
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = HideFlags.None;
            level.ConfigureWithPassengerFlowPlan(
                levelName,
                profile,
                flowPlan,
                vehicles,
                25,
                RotaryRoadPresetId.SmallCircleTest);

            var report = LevelValidator.Validate(level, false);
            if (report.HasErrors)
            {
                FailAssetOperation(report.ToConsoleMessage($"Manual Heart {GetManualHeartModeLogName(directionMode)}"));
                return null;
            }

            var solutionAnalysis = StageSolutionAnalyzer.Analyze(
                level.Buses,
                level.Garages,
                1,
                ManualShapeSolutionNodeVisitLimit);
            if (!solutionAnalysis.IsSolvable)
            {
                FailAssetOperation($"Manual Heart {GetManualHeartModeLogName(directionMode)} is not clearable.");
                return null;
            }

            level.SetGenerationMetadata(GetManualHeartGenerationSignature(directionMode), solutionAnalysis.SolutionCount);
            return level;
        }

        private static List<BusDefinition> CreateManualHeartReferenceVehicles(ManualHeartDirectionMode directionMode)
        {
            var rows = new[]
            {
                new ManualHeartRow(11.2f, new[] { 3.4f, 4.8f, 8.2f, 9.6f }),
                new ManualHeartRow(10.2f, new[] { 2.4f, 3.8f, 5.2f, 7.8f, 9.2f, 10.6f }),
                new ManualHeartRow(9.2f, new[] { 1.6f, 3.0f, 4.4f, 5.8f, 7.2f, 8.6f, 10.0f, 11.4f }),
                new ManualHeartRow(8.2f, new[] { 1.2f, 2.6f, 4.0f, 5.4f, 6.8f, 8.2f, 9.6f, 11.0f, 12.4f }),
                new ManualHeartRow(7.2f, new[] { 1.5f, 2.9f, 4.3f, 5.7f, 7.1f, 8.5f, 9.9f, 11.3f }),
                new ManualHeartRow(6.2f, new[] { 2.0f, 3.4f, 4.8f, 6.2f, 7.6f, 9.0f, 10.4f, 11.8f }),
                new ManualHeartRow(5.2f, new[] { 2.8f, 4.2f, 5.6f, 7.0f, 8.4f, 9.8f, 11.2f }),
                new ManualHeartRow(4.2f, new[] { 3.6f, 5.0f, 6.4f, 7.8f, 9.2f, 10.6f }),
                new ManualHeartRow(3.2f, new[] { 4.4f, 5.8f, 7.2f, 8.6f, 10.0f }),
                new ManualHeartRow(2.2f, new[] { 5.2f, 6.6f, 8.0f, 9.4f }),
                new ManualHeartRow(1.2f, new[] { 6.0f, 7.4f, 8.8f })
            };
            var colors = new[]
            {
                PuzzleColor.Red,
                PuzzleColor.SkyBlue,
                PuzzleColor.Yellow,
                PuzzleColor.Purple,
                PuzzleColor.Pink,
                PuzzleColor.Blue,
                PuzzleColor.Green,
                PuzzleColor.Orange,
                PuzzleColor.Lime
            };
            var vehicles = new List<BusDefinition>();
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = rows[rowIndex];
                AppendManualHeartRowVehicles(vehicles, row, colors, rowIndex, directionMode);
            }

            return vehicles;
        }

        private static void AppendManualHeartRowVehicles(
            List<BusDefinition> vehicles,
            ManualHeartRow row,
            IReadOnlyList<PuzzleColor> colors,
            int rowIndex,
            ManualHeartDirectionMode directionMode)
        {
            var left = new List<float>();
            var right = new List<float>();
            for (var index = 0; index < row.Columns.Length; index++)
            {
                var column = row.Columns[index];
                if (column < 6.8f)
                {
                    left.Add(column);
                    continue;
                }

                right.Add(column);
            }

            left.Sort();
            right.Sort((a, b) => b.CompareTo(a));
            for (var index = 0; index < Mathf.Max(left.Count, right.Count); index++)
            {
                if (index < left.Count)
                {
                    AddManualHeartVehicle(vehicles, left[index], row.Row, colors, rowIndex, index, true, directionMode);
                }

                if (index < right.Count)
                {
                    AddManualHeartVehicle(vehicles, right[index], row.Row, colors, rowIndex, index, false, directionMode);
                }
            }
        }

        private static void AddManualHeartVehicle(
            List<BusDefinition> vehicles,
            float column,
            float row,
            IReadOnlyList<PuzzleColor> colors,
            int rowIndex,
            int localIndex,
            bool exitsLeft,
            ManualHeartDirectionMode directionMode)
        {
            var yaw = GetManualHeartYaw(column, row, rowIndex, localIndex, exitsLeft, directionMode);
            var direction = DirectionFromYaw(yaw);
            var angleOffset = Mathf.DeltaAngle(GridDirectionUtility.ToYawDegrees(direction), yaw);
            var color = colors[(vehicles.Count + rowIndex * 2 + localIndex) % colors.Count];
            var size = GetManualHeartSize(column, row);
            var gridPosition = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(column), 0, ManualHeartGridColumns - 1),
                Mathf.Clamp(Mathf.RoundToInt(row), 0, ManualHeartGridRows - 1));
            var positionOffset = new Vector2(column - gridPosition.x, row - gridPosition.y);
            vehicles.Add(new BusDefinition(
                color,
                size,
                direction,
                gridPosition,
                angleOffset,
                positionOffset));
        }

        private static float GetManualHeartYaw(
            float column,
            float row,
            int rowIndex,
            int localIndex,
            bool exitsLeft,
            ManualHeartDirectionMode directionMode)
        {
            if (directionMode == ManualHeartDirectionMode.DirectionMix)
            {
                return GetManualHeartDirectionMixYaw(column, row, rowIndex, localIndex, exitsLeft);
            }

            return GetManualHeartReferenceYaw(column, row, exitsLeft);
        }

        private static float GetManualHeartReferenceYaw(float column, float row, bool exitsLeft)
        {
            if (row <= 1.5f && column > 6.5f && column < 7.9f)
            {
                return 180f;
            }

            return exitsLeft ? -90f : 90f;
        }

        private static float GetManualHeartDirectionMixYaw(
            float column,
            float row,
            int rowIndex,
            int localIndex,
            bool exitsLeft)
        {
            if (Mathf.Abs(row - 11.2f) <= 0.05f &&
                (Mathf.Abs(column - 4.8f) <= 0.05f || Mathf.Abs(column - 8.2f) <= 0.05f))
            {
                return 0f;
            }

            if (row <= 1.5f && column >= 5.7f && column <= 8.9f)
            {
                return 180f;
            }

            if (row >= 2.1f && row <= 10.3f)
            {
                if (exitsLeft &&
                    rowIndex % 2 == 1 &&
                    column >= 5.0f &&
                    column <= 6.6f)
                {
                    return 90f;
                }

                if (!exitsLeft &&
                    rowIndex % 2 == 0 &&
                    column >= 7.0f &&
                    column <= 8.2f)
                {
                    return -90f;
                }
            }

            return GetManualHeartReferenceYaw(column, row, exitsLeft);
        }

        private static int GetManualHeartPassengerSeed(ManualHeartDirectionMode directionMode)
        {
            return directionMode == ManualHeartDirectionMode.DirectionMix ? 19082 : 19081;
        }

        private static string GetManualHeartGenerationSignature(ManualHeartDirectionMode directionMode)
        {
            return directionMode == ManualHeartDirectionMode.DirectionMix
                ? "manualShape=heart_direction_mix;stage=9;source=heart_reference_01;"
                : "manualShape=heart_reference;stage=9;source=ad_reference_heart;";
        }

        private static string GetManualHeartModeLogName(ManualHeartDirectionMode directionMode)
        {
            return directionMode == ManualHeartDirectionMode.DirectionMix ? "direction mix" : "reference";
        }

        private static GridDirection DirectionFromYaw(float yaw)
        {
            yaw = Mathf.Repeat(yaw + 360f, 360f);
            if (yaw >= 45f && yaw < 135f)
            {
                return GridDirection.Right;
            }

            if (yaw >= 135f && yaw < 225f)
            {
                return GridDirection.Down;
            }

            return yaw >= 225f && yaw < 315f ? GridDirection.Left : GridDirection.Up;
        }

        private static BusSize GetManualHeartSize(float column, float row)
        {
            return BusSize.Small;
        }

        private readonly struct ManualHeartRow
        {
            public ManualHeartRow(float row, float[] columns)
            {
                Row = row;
                Columns = columns;
            }

            public float Row { get; }
            public float[] Columns { get; }
        }

        private static void SavePreviewStageAsShapeTemplate(
            int previewStageNumber,
            string templatePath,
            string templateDisplayName)
        {
            var previewAssetName = $"Level_{previewStageNumber:000}";
            var previewPath = GetLevelPath(previewAssetName);
            var previewLevel = AssetDatabase.LoadAssetAtPath<LevelData>(previewPath);
            if (previewLevel == null)
            {
                FailAssetOperation($"Preview stage asset is missing: {previewPath}");
                return;
            }

            ValidateLevelForAssetOperation(previewLevel, $"preview stage {previewStageNumber:000}");
            var template = SaveLevelAssetCopy(
                previewLevel,
                templatePath,
                Path.GetFileNameWithoutExtension(templatePath),
                templateDisplayName);
            ValidateLevelForAssetOperation(template, templateDisplayName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Saved shape template {templateDisplayName} from preview stage {previewStageNumber:000}: " +
                $"{templatePath}. {CreateLevelSummary(template)}.");
        }

        private static void LoadShapeTemplateIntoPreviewStage(
            string templatePath,
            string templateDisplayName,
            int previewStageNumber)
        {
            var template = AssetDatabase.LoadAssetAtPath<LevelData>(templatePath);
            if (template == null)
            {
                FailAssetOperation($"Shape template asset is missing: {templatePath}");
                return;
            }

            ValidateLevelForAssetOperation(template, templateDisplayName);
            var previewAssetName = $"Level_{previewStageNumber:000}";
            var previewPath = GetLevelPath(previewAssetName);
            var preview = SaveLevelAssetCopy(template, previewPath, previewAssetName, previewAssetName);
            ValidateLevelForAssetOperation(preview, $"preview stage {previewStageNumber:000}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Loaded shape template {templateDisplayName} into preview stage {previewStageNumber:000}: " +
                $"{previewPath}. {CreateLevelSummary(preview)}.");
        }

        private static void ValidateShapeTemplate(string templatePath, string templateDisplayName)
        {
            var template = AssetDatabase.LoadAssetAtPath<LevelData>(templatePath);
            if (template == null)
            {
                FailAssetOperation($"Shape template asset is missing: {templatePath}");
                return;
            }

            ValidateLevelForAssetOperation(template, templateDisplayName);
            Debug.Log($"Shape template {templateDisplayName} passed validation: {templatePath}. {CreateLevelSummary(template)}.");
        }

        private static bool TryApplyOpeningMoveConstraint(
            LevelData level,
            StageGenerationRequest request,
            int candidateOffset,
            string generationSignature,
            int minimumOpeningMoveCount,
            int maximumOpeningMoveCount,
            out int openingMoveCount,
            out LevelValidationReport report)
        {
            openingMoveCount = level != null ? LevelGenerator.CountOpeningMoves(level.Buses) : 0;
            report = level != null ? LevelValidator.Validate(level, false) : null;
            if (level == null ||
                openingMoveCount < minimumOpeningMoveCount ||
                openingMoveCount <= maximumOpeningMoveCount ||
                !LevelGenerator.TryConstrainOpeningMoves(
                    level.Buses,
                    request.Profile,
                    request.VehicleLayoutVariantIndex,
                    minimumOpeningMoveCount,
                    maximumOpeningMoveCount,
                    out var constrainedBuses))
            {
                return false;
            }

            if (LevelGenerator.TryBuildGreedyOrderedVehicles(constrainedBuses, out var orderedBuses))
            {
                constrainedBuses = orderedBuses;
            }

            var seed = request.Seed + candidateOffset * 7919;
            var flowPlan = LevelGenerator.BuildPassengerFlowPlanFromVehicleOrder(request.Profile, constrainedBuses, seed);
            level.ConfigureWithPassengerFlowPlan(
                level.LevelName,
                level.DifficultyProfile,
                flowPlan,
                constrainedBuses,
                level.RotaryStartCapacity,
                level.RoadPresetId,
                null,
                level.Garages,
                level.PresentationMode);
            level.SetGenerationMetadata(generationSignature, 1);
            report = LevelValidator.Validate(level, false);
            if (report.HasErrors)
            {
                return false;
            }

            openingMoveCount = LevelGenerator.CountOpeningMoves(level.Buses);
            return openingMoveCount >= minimumOpeningMoveCount &&
                openingMoveCount <= maximumOpeningMoveCount &&
                HasRequiredVisualPreviewVehicleSizes(request, level.Buses) &&
                LevelGenerator.HasGreedyExitOrder(level.Buses);
        }

        private static bool TryApplyVisualPreviewVariantAdjustments(
            LevelData level,
            StageGenerationRequest request,
            int candidateOffset)
        {
            if (level == null ||
                !StageGenerationPlanner.UsesStarSizeMixShapeLibraryTemplate(request))
            {
                return true;
            }

            return true;
        }

        private static bool HasRequiredVisualPreviewVehicleSizes(
            StageGenerationRequest request,
            IReadOnlyList<BusDefinition> buses)
        {
            if (buses == null ||
                CountMediumLargeVehicles(buses) < GetMinimumVisualPreviewMediumLargeCount(buses.Count))
            {
                return false;
            }

            if (StageGenerationPlanner.UsesStarSizeMixShapeLibraryTemplate(request))
            {
                var minimumMediumLarge = Mathf.CeilToInt(buses.Count * 0.40f);
                return CountMediumLargeVehicles(buses) >= minimumMediumLarge &&
                    CountLargeVehicles(buses) >= 1;
            }

            return !StageGenerationPlanner.UsesStarShapeLibraryTemplate(request) ||
                CountLargeVehicles(buses) >= 1;
        }

        private static int CountMediumLargeVehicles(IReadOnlyList<BusDefinition> buses)
        {
            if (buses == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index].Size != BusSize.Small)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountLargeVehicles(IReadOnlyList<BusDefinition> buses)
        {
            if (buses == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index].Size == BusSize.Large)
                {
                    count++;
                }
            }

            return count;
        }

        private static int GetMinimumVisualPreviewMediumLargeCount(int vehicleCount)
        {
            if (vehicleCount >= 40)
            {
                return 4;
            }

            return vehicleCount >= 34 ? 3 : 2;
        }

        private static void GetVisualPreviewOpeningMoveRange(
            StageGenerationRequest request,
            out int minimum,
            out int maximum)
        {
            if (StageGenerationPlanner.UsesStarSizeMixShapeLibraryTemplate(request))
            {
                minimum = StarPreviewMinimumOpeningMoveCount;
                maximum = StarSizeMixPreviewMaximumOpeningMoveCount;
                return;
            }

            if (StageGenerationPlanner.UsesStarShapeLibraryTemplate(request))
            {
                minimum = StarPreviewMinimumOpeningMoveCount;
                maximum = StarPreviewMaximumOpeningMoveCount;
                return;
            }

            minimum = DefaultVisualPreviewMinimumOpeningMoveCount;
            maximum = int.MaxValue;
        }

        private static int ReadCommandLineStageNumber(int fallbackStageNumber)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                if ((arg == "-stage" || arg == "--stage") &&
                    index + 1 < args.Length &&
                    int.TryParse(args[index + 1], out var value))
                {
                    return value;
                }

                const string stagePrefix = "-stage=";
                const string longStagePrefix = "--stage=";
                if (arg.StartsWith(stagePrefix, System.StringComparison.Ordinal) &&
                    int.TryParse(arg.Substring(stagePrefix.Length), out value))
                {
                    return value;
                }

                if (arg.StartsWith(longStagePrefix, System.StringComparison.Ordinal) &&
                    int.TryParse(arg.Substring(longStagePrefix.Length), out value))
                {
                    return value;
                }
            }

            return fallbackStageNumber;
        }

        private static LevelData SaveLevelAssetCopy(
            LevelData source,
            string destinationPath,
            string assetName,
            string levelDisplayName)
        {
            EnsureAssetDirectory(Path.GetDirectoryName(destinationPath)?.Replace('\\', '/'));
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(destinationPath);
            if (existing == null)
            {
                existing = UnityEngine.Object.Instantiate(source);
                existing.hideFlags = HideFlags.None;
                EnsureLevelAssetName(existing, assetName);
                SetSerializedLevelName(existing, levelDisplayName);
                AssetDatabase.CreateAsset(existing, destinationPath);
                return existing;
            }

            EditorUtility.CopySerialized(source, existing);
            existing.hideFlags = HideFlags.None;
            EnsureLevelAssetName(existing, assetName);
            SetSerializedLevelName(existing, levelDisplayName);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void EnsureAssetDirectory(string assetDirectory)
        {
            if (string.IsNullOrEmpty(assetDirectory) ||
                AssetDatabase.IsValidFolder(assetDirectory))
            {
                return;
            }

            var parentDirectory = Path.GetDirectoryName(assetDirectory)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                EnsureAssetDirectory(parentDirectory);
            }

            if (!string.IsNullOrEmpty(parentDirectory) &&
                !AssetDatabase.IsValidFolder(assetDirectory))
            {
                AssetDatabase.CreateFolder(parentDirectory, Path.GetFileName(assetDirectory));
            }
        }

        private static void SetSerializedLevelName(LevelData level, string levelDisplayName)
        {
            if (level == null ||
                string.IsNullOrEmpty(levelDisplayName))
            {
                return;
            }

            var serializedLevel = new SerializedObject(level);
            var property = serializedLevel.FindProperty("levelName");
            if (property == null)
            {
                return;
            }

            property.stringValue = levelDisplayName;
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateLevelForAssetOperation(LevelData level, string displayName)
        {
            var report = LevelValidator.Validate(level);
            if (report.HasErrors)
            {
                FailAssetOperation(report.ToConsoleMessage(displayName));
                return;
            }

            if (report.HasIssues)
            {
                Debug.LogWarning(report.ToConsoleMessage(displayName), level);
            }
        }

        private static string CreateLevelSummary(LevelData level)
        {
            if (level == null)
            {
                return "missing level data";
            }

            var smallCount = 0;
            var mediumCount = 0;
            var largeCount = 0;
            var buses = level.Buses;
            for (var index = 0; index < buses.Count; index++)
            {
                switch (buses[index].Size)
                {
                    case BusSize.Medium:
                        mediumCount++;
                        break;
                    case BusSize.Large:
                        largeCount++;
                        break;
                    default:
                        smallCount++;
                        break;
                }
            }

            return $"vehicles {buses.Count}, Small {smallCount} / Medium {mediumCount} / Large {largeCount}, " +
                $"opening moves {LevelGenerator.CountOpeningMoves(buses)}, generation solutions {level.GenerationSolutionCount}";
        }

        private static void FailAssetOperation(string message)
        {
            Debug.LogError(message);
            if (Application.isBatchMode)
            {
                throw new System.InvalidOperationException(message);
            }
        }

        private static LevelData SaveLevel(string assetName, LevelData generatedLevel)
        {
            var path = GetLevelPath(assetName);
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            generatedLevel.hideFlags = HideFlags.None;
            EnsureLevelAssetName(generatedLevel, assetName);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generatedLevel, path);
                return generatedLevel;
            }

            EditorUtility.CopySerialized(generatedLevel, existing);
            existing.hideFlags = HideFlags.None;
            EnsureLevelAssetName(existing, assetName);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static bool TryReuseExistingLevel(
            StageGenerationConfig config,
            StageGenerationRequest request,
            out LevelData level)
        {
            level = AssetDatabase.LoadAssetAtPath<LevelData>(GetLevelPath($"Level_{request.StageNumber:000}"));
            if (level == null)
            {
                return false;
            }

            var expectedSignature = StageGenerationSignature.Create(config, request);
            if (!level.HasGenerationSignature(expectedSignature))
            {
                Debug.Log(
                    $"Existing generated stage {request.StageNumber:000} has no matching generation signature and will be rebuilt.");
                return false;
            }

            var solutionLimit = GetValidationSolutionLimit(config, request);
            var report = LevelValidator.Validate(level, false, solutionLimit);
            var analysis = StageSolutionAnalyzer.Analyze(
                level.Buses,
                level.Garages,
                solutionLimit,
                config.ReleaseSolutionNodeVisitLimit);
            if (!report.HasErrors && StageCandidateBuilder.IsSolutionCountAcceptable(request, analysis))
            {
                level.SetGenerationMetadata(expectedSignature, analysis.SolutionCount);
                level.hideFlags = HideFlags.None;
                EnsureLevelAssetName(level, $"Level_{request.StageNumber:000}");
                EditorUtility.SetDirty(level);
                return true;
            }

            Debug.LogWarning(
                $"Existing generated stage {request.StageNumber:000} failed validation or solution range checks and will be rebuilt. " +
                $"Solutions: {analysis.SolutionCount}, target range: {request.MinSolutionCount}-{request.MaxSolutionCount}. " +
                report.ToConsoleMessage(level.LevelName));
            return false;
        }

        private static int GetValidationSolutionLimit(StageGenerationConfig config, StageGenerationRequest request)
        {
            var upperBoundProbe = Mathf.Max(1, request.MaxSolutionCount + 1);
            return Mathf.Clamp(Mathf.Min(config.SolutionCountLimit, upperBoundProbe), 1, config.SolutionCountLimit);
        }

        private static string GetLevelPath(string assetName)
        {
            return $"{GeneratedLevelDirectory}/{assetName}.asset";
        }

        private static bool TryLoadExistingLevel(string assetName, out LevelData level)
        {
            level = AssetDatabase.LoadAssetAtPath<LevelData>(GetLevelPath(assetName));
            if (level == null)
            {
                return false;
            }

            level.hideFlags = HideFlags.None;
            EnsureLevelAssetName(level, assetName);
            EditorUtility.SetDirty(level);
            return true;
        }

        private static int LoadExistingGeneratedPrefix(StageGenerationConfig config, LevelData[] levels)
        {
            for (var stageNumber = 1; stageNumber <= config.GeneratedStageCount; stageNumber++)
            {
                var request = StageGenerationPlanner.CreateRequest(config, stageNumber);
                if (!TryLoadExistingLevelWithMatchingSignature(config, request, out var level))
                {
                    return stageNumber - 1;
                }

                levels[stageNumber - 1] = level;
            }

            return config.GeneratedStageCount;
        }

        private static bool TryLoadExistingLevelWithMatchingSignature(
            StageGenerationConfig config,
            StageGenerationRequest request,
            out LevelData level)
        {
            level = AssetDatabase.LoadAssetAtPath<LevelData>(GetLevelPath($"Level_{request.StageNumber:000}"));
            if (level == null)
            {
                return false;
            }

            var expectedSignature = StageGenerationSignature.Create(config, request);
            if (!level.HasGenerationSignature(expectedSignature))
            {
                return false;
            }

            level.hideFlags = HideFlags.None;
            EnsureLevelAssetName(level, $"Level_{request.StageNumber:000}");
            return true;
        }

        private static void EnsureLevelAssetName(LevelData level, string assetName)
        {
            if (level != null && level.name != assetName)
            {
                level.name = assetName;
            }
        }

        private static void SaveCompletedGeneratedSequence(
            LevelData[] levels,
            int completedStageCount,
            string logMessage,
            bool refresh)
        {
            var completedLevels = CollectCompletedLevels(levels, completedStageCount);
            if (completedLevels.Length == 0)
            {
                return;
            }

            SaveVerifiedGeneratedSequence(completedLevels, GeneratedLevelSequencePath);
            SaveVerifiedGeneratedSequence(completedLevels, ActiveLevelSequencePath);
            AssetDatabase.SaveAssets();
            if (refresh)
            {
                AssetDatabase.Refresh();
            }

            if (!string.IsNullOrEmpty(logMessage))
            {
                Debug.Log($"{logMessage} The active sequence now points to {ActiveLevelSequencePath}.");
            }
        }

        private static LevelData[] CollectCompletedLevels(LevelData[] levels, int completedStageCount)
        {
            var completedLevels = new List<LevelData>();
            var limit = Mathf.Clamp(completedStageCount, 0, levels != null ? levels.Length : 0);
            for (var index = 0; index < limit; index++)
            {
                var level = levels[index];
                if (level == null)
                {
                    break;
                }

                completedLevels.Add(level);
            }

            return completedLevels.ToArray();
        }

        private static void SaveVerifiedGeneratedSequence(LevelData[] levels, string path)
        {
            var sequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(path);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<LevelSequence>();
                AssetDatabase.CreateAsset(sequence, path);
            }

            sequence.ConfigureVerifiedGeneratedSet(levels);
            EditorUtility.SetDirty(sequence);
        }

        private static StageGenerationConfig LoadConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(StageGenerationConfigPath);
            if (config != null)
            {
                return config;
            }

            Debug.LogWarning($"Stage generation config not found at {StageGenerationConfigPath}; using runtime defaults.");
            return ScriptableObject.CreateInstance<StageGenerationConfig>();
        }

        private static string CreateReportMessage(LevelData level, LevelValidationReport report)
        {
            if (report == null || !report.HasIssues)
            {
                return string.Empty;
            }

            return report.ToConsoleMessage(level != null ? level.LevelName : "Candidate");
        }
    }
}
#endif
