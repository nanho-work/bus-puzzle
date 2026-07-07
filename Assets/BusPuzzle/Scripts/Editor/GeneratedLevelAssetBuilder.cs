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
        private const string HeartColor4ShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_Color4_01.asset";
        private const string HeartColor4ShapeTemplateDisplayName = "Heart Color 4 01";
        private const string HeartSizeMixShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_SizeMix_01.asset";
        private const string HeartSizeMixShapeTemplateDisplayName = "Heart Size Mix 01";
        private const string HeartMysteryShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_Mystery_01.asset";
        private const string HeartMysteryShapeTemplateDisplayName = "Heart Mystery 01";
        private const string HeartDoubleOutlineShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_DoubleOutline_01.asset";
        private const string HeartDoubleOutlineShapeTemplateDisplayName = "Heart Double Outline 01";
        private const string HeartGarageShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_Garage_01.asset";
        private const string HeartGarageShapeTemplateDisplayName = "Heart Garage 01";
        private const string HeartGarageMysteryShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_GarageMystery_01.asset";
        private const string HeartGarageMysteryShapeTemplateDisplayName = "Heart Garage Mystery 01";
        private const string HeartColor4GarageMysteryShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_Color4GarageMystery_01.asset";
        private const string HeartColor4GarageMysteryShapeTemplateDisplayName = "Heart Color 4 Garage Mystery 01";
        private const string HeartFullColorGarageMysteryShapeTemplatePath = HeartShapeTemplateDirectory + "/Heart_FullColorGarageMystery_01.asset";
        private const string HeartFullColorGarageMysteryShapeTemplateDisplayName = "Heart Full Color Garage Mystery 01";
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
        private const int ManualHeartDirectionMixOuterPoseCount = 11;
        private const int ManualHeartDoubleOutlineOuterPoseCount = 31;
        private const int ManualHeartColor4GarageMysteryOuterPoseCount = 48;
        private const int ManualShapeSolutionNodeVisitLimit = 50000;

        private enum GeneratedStageBuildMode
        {
            FullSet,
            NextBatch,
            PreviewFirst100,
            ShapeLibraryPreview
        }

        private enum ManualHeartVariantMode
        {
            Reference,
            DirectionMix,
            Color4,
            SizeMix,
            Mystery,
            DoubleOutline,
            Garage,
            GarageMystery,
            Color4GarageMystery,
            FullColorGarageMystery
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
            RebuildStage09ManualHeart(ManualHeartVariantMode.Reference);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Direction Mix")]
        public static void RebuildShapeLibraryPreviewStage09HeartDirectionMix()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.DirectionMix);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Color 4")]
        public static void RebuildShapeLibraryPreviewStage09HeartColor4()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.Color4);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Size Mix")]
        public static void RebuildShapeLibraryPreviewStage09HeartSizeMix()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.SizeMix);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Mystery")]
        public static void RebuildShapeLibraryPreviewStage09HeartMystery()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.Mystery);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Double Outline")]
        public static void RebuildShapeLibraryPreviewStage09HeartDoubleOutline()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.DoubleOutline);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Garage")]
        public static void RebuildShapeLibraryPreviewStage09HeartGarage()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.Garage);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Garage Mystery")]
        public static void RebuildShapeLibraryPreviewStage09HeartGarageMystery()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.GarageMystery);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Color 4 Garage Mystery")]
        public static void RebuildShapeLibraryPreviewStage09HeartColor4GarageMystery()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.Color4GarageMystery);
        }

        [MenuItem("Bus Puzzle/Levels/Rebuild Shape Library Preview Stage 09 Heart Full Color Garage Mystery")]
        public static void RebuildShapeLibraryPreviewStage09HeartFullColorGarageMystery()
        {
            SaveManualHeartReferenceTemplate();
            RebuildStage09ManualHeart(ManualHeartVariantMode.FullColorGarageMystery);
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

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Color 4 01")]
        public static void SaveStage09PreviewAsHeartColor4Template()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartColor4ShapeTemplatePath,
                HeartColor4ShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Color 4 01 Into Stage 09 Preview")]
        public static void LoadHeartColor4TemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartColor4ShapeTemplatePath,
                HeartColor4ShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Color 4 01")]
        public static void ValidateHeartColor4ShapeTemplate()
        {
            ValidateShapeTemplate(HeartColor4ShapeTemplatePath, HeartColor4ShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Size Mix 01")]
        public static void SaveStage09PreviewAsHeartSizeMixTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartSizeMixShapeTemplatePath,
                HeartSizeMixShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Size Mix 01 Into Stage 09 Preview")]
        public static void LoadHeartSizeMixTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartSizeMixShapeTemplatePath,
                HeartSizeMixShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Size Mix 01")]
        public static void ValidateHeartSizeMixShapeTemplate()
        {
            ValidateShapeTemplate(HeartSizeMixShapeTemplatePath, HeartSizeMixShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Mystery 01")]
        public static void SaveStage09PreviewAsHeartMysteryTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartMysteryShapeTemplatePath,
                HeartMysteryShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Mystery 01 Into Stage 09 Preview")]
        public static void LoadHeartMysteryTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartMysteryShapeTemplatePath,
                HeartMysteryShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Mystery 01")]
        public static void ValidateHeartMysteryShapeTemplate()
        {
            ValidateShapeTemplate(HeartMysteryShapeTemplatePath, HeartMysteryShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Double Outline 01")]
        public static void SaveStage09PreviewAsHeartDoubleOutlineTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartDoubleOutlineShapeTemplatePath,
                HeartDoubleOutlineShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Double Outline 01 Into Stage 09 Preview")]
        public static void LoadHeartDoubleOutlineTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartDoubleOutlineShapeTemplatePath,
                HeartDoubleOutlineShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Double Outline 01")]
        public static void ValidateHeartDoubleOutlineShapeTemplate()
        {
            ValidateShapeTemplate(HeartDoubleOutlineShapeTemplatePath, HeartDoubleOutlineShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Garage 01")]
        public static void SaveStage09PreviewAsHeartGarageTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartGarageShapeTemplatePath,
                HeartGarageShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Garage 01 Into Stage 09 Preview")]
        public static void LoadHeartGarageTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartGarageShapeTemplatePath,
                HeartGarageShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Garage 01")]
        public static void ValidateHeartGarageShapeTemplate()
        {
            ValidateShapeTemplate(HeartGarageShapeTemplatePath, HeartGarageShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Garage Mystery 01")]
        public static void SaveStage09PreviewAsHeartGarageMysteryTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartGarageMysteryShapeTemplatePath,
                HeartGarageMysteryShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Garage Mystery 01 Into Stage 09 Preview")]
        public static void LoadHeartGarageMysteryTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartGarageMysteryShapeTemplatePath,
                HeartGarageMysteryShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Garage Mystery 01")]
        public static void ValidateHeartGarageMysteryShapeTemplate()
        {
            ValidateShapeTemplate(HeartGarageMysteryShapeTemplatePath, HeartGarageMysteryShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Color 4 Garage Mystery 01")]
        public static void SaveStage09PreviewAsHeartColor4GarageMysteryTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartColor4GarageMysteryShapeTemplatePath,
                HeartColor4GarageMysteryShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Color 4 Garage Mystery 01 Into Stage 09 Preview")]
        public static void LoadHeartColor4GarageMysteryTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartColor4GarageMysteryShapeTemplatePath,
                HeartColor4GarageMysteryShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Color 4 Garage Mystery 01")]
        public static void ValidateHeartColor4GarageMysteryShapeTemplate()
        {
            ValidateShapeTemplate(
                HeartColor4GarageMysteryShapeTemplatePath,
                HeartColor4GarageMysteryShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Save Stage 09 Preview As Heart Full Color Garage Mystery 01")]
        public static void SaveStage09PreviewAsHeartFullColorGarageMysteryTemplate()
        {
            SavePreviewStageAsShapeTemplate(
                ShapeTemplatePreviewStageNumber,
                HeartFullColorGarageMysteryShapeTemplatePath,
                HeartFullColorGarageMysteryShapeTemplateDisplayName);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Load Heart Full Color Garage Mystery 01 Into Stage 09 Preview")]
        public static void LoadHeartFullColorGarageMysteryTemplateIntoStage09Preview()
        {
            LoadShapeTemplateIntoPreviewStage(
                HeartFullColorGarageMysteryShapeTemplatePath,
                HeartFullColorGarageMysteryShapeTemplateDisplayName,
                ShapeTemplatePreviewStageNumber);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Heart Full Color Garage Mystery 01")]
        public static void ValidateHeartFullColorGarageMysteryShapeTemplate()
        {
            ValidateShapeTemplate(
                HeartFullColorGarageMysteryShapeTemplatePath,
                HeartFullColorGarageMysteryShapeTemplateDisplayName);
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

        private static void RebuildStage09ManualHeart(ManualHeartVariantMode variantMode)
        {
            var level = CreateManualHeartLevel(variantMode, $"Stage {ShapeTemplatePreviewStageNumber:000} Hard");
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
                $"Rebuilt manual heart {GetManualHeartVariantLogName(variantMode)} preview stage {ShapeTemplatePreviewStageNumber:000}: " +
                $"vehicles {vehicles.Count}, opening moves {openingMoveCount}, " +
                $"solutions {level.GenerationSolutionCount}, road {level.RoadPresetId}.");
        }

        private static void SaveManualHeartReferenceTemplate()
        {
            var referenceLevel = CreateManualHeartLevel(
                ManualHeartVariantMode.Reference,
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

        private static LevelData CreateManualHeartLevel(ManualHeartVariantMode variantMode, string levelName)
        {
            var vehicles = CreateManualHeartReferenceVehicles(variantMode);
            var garages = CreateManualHeartGarages(variantMode, GetManualHeartColors(variantMode));
            if (garages.Count > 0)
            {
                vehicles = RemoveManualHeartVehiclesConflictingWithGarages(vehicles, garages, variantMode);
            }

            if (IsManualHeartGarageMysteryVariant(variantMode))
            {
                vehicles = ApplyManualHeartGarageMysteryConcealment(vehicles, garages, variantMode);
            }

            if (!LevelGenerator.TryBuildGreedyOrderedVehicles(vehicles, out var orderedVehicles))
            {
                Debug.LogWarning(CreateManualHeartGreedyFailureSummary(vehicles));
                FailAssetOperation($"Manual Heart {GetManualHeartVariantLogName(variantMode)} does not have a greedy clear order.");
                return null;
            }

            vehicles = orderedVehicles;
            var profile = LevelDifficultyProfile.CreateCustom(
                LevelDifficulty.Hard,
                vehicles.Count,
                GetManualHeartTargetColorCount(variantMode),
                0.54f,
                0.48f,
                true);
            var flowPlan = garages.Count > 0
                ? LevelGenerator.BuildPassengerFlowPlan(profile, vehicles, garages, GetManualHeartPassengerSeed(variantMode))
                : LevelGenerator.BuildPassengerFlowPlanFromVehicleOrder(
                    profile,
                    vehicles,
                    GetManualHeartPassengerSeed(variantMode));
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = HideFlags.None;
            level.ConfigureWithPassengerFlowPlan(
                levelName,
                profile,
                flowPlan,
                vehicles,
                25,
                RotaryRoadPresetId.SmallCircleTest,
                null,
                garages);

            var report = LevelValidator.Validate(level, false);
            if (report.HasErrors)
            {
                FailAssetOperation(report.ToConsoleMessage($"Manual Heart {GetManualHeartVariantLogName(variantMode)}"));
                return null;
            }

            var solutionAnalysis = StageSolutionAnalyzer.Analyze(
                level.Buses,
                level.Garages,
                1,
                ManualShapeSolutionNodeVisitLimit);
            if (!solutionAnalysis.IsSolvable)
            {
                FailAssetOperation($"Manual Heart {GetManualHeartVariantLogName(variantMode)} is not clearable.");
                return null;
            }

            level.SetGenerationMetadata(GetManualHeartGenerationSignature(variantMode), solutionAnalysis.SolutionCount);
            return level;
        }

        private static List<BusDefinition> CreateManualHeartReferenceVehicles(ManualHeartVariantMode variantMode)
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
            var colors = GetManualHeartColors(variantMode);
            if (variantMode == ManualHeartVariantMode.DirectionMix ||
                variantMode == ManualHeartVariantMode.SizeMix ||
                variantMode == ManualHeartVariantMode.Mystery ||
                variantMode == ManualHeartVariantMode.DoubleOutline ||
                variantMode == ManualHeartVariantMode.Garage ||
                IsManualHeartGarageMysteryVariant(variantMode))
            {
                return CreateManualHeartDirectionFirstVehicles(colors, variantMode);
            }

            var vehicles = new List<BusDefinition>();
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = rows[rowIndex];
                AppendManualHeartRowVehicles(vehicles, row, colors, rowIndex, variantMode);
            }

            return vehicles;
        }

        private static string CreateManualHeartGreedyFailureSummary(IReadOnlyList<BusDefinition> vehicles)
        {
            if (!LevelGenerator.TryFindGreedyExitOrder(vehicles, out var exitOrder, out var stuckIndices))
            {
                var lines = new List<string>
                {
                    $"Manual Heart greedy debug: exited {exitOrder.Count}/{vehicles.Count}, stuck {stuckIndices.Count}."
                };
                var limit = Mathf.Min(stuckIndices.Count, 12);
                for (var index = 0; index < limit; index++)
                {
                    var vehicleIndex = stuckIndices[index];
                    var vehicle = vehicles[vehicleIndex];
                    var position = new Vector2(
                        vehicle.GridPosition.x + vehicle.PositionOffsetCells.x,
                        vehicle.GridPosition.y + vehicle.PositionOffsetCells.y);
                    lines.Add(
                        $"stuck #{vehicleIndex + 1}: {vehicle.Color} {vehicle.Size} {vehicle.Direction} yaw {vehicle.YawDegrees:0.#} at ({position.x:0.##}, {position.y:0.##})");
                }

                return string.Join("\n", lines);
            }

            return $"Manual Heart greedy debug: exit planner succeeded unexpectedly with {exitOrder.Count}/{vehicles.Count}.";
        }

        private static List<BusDefinition> CreateManualHeartDirectionFirstVehicles(
            IReadOnlyList<PuzzleColor> colors,
            ManualHeartVariantMode variantMode)
        {
            var poses = CreateManualHeartVariantPoses(variantMode);
            var outerPoseCount = GetManualHeartOuterPoseCount(variantMode);
            var vehicles = new List<BusDefinition>(poses.Count);
            var skippedOuterPoseCount = 0;
            var skippedInteriorPoseCount = 0;
            for (var index = 0; index < poses.Count; index++)
            {
                var isOuterPose = index < outerPoseCount;
                var pose = poses[index];
                var size = GetManualHeartPoseSize(variantMode, index);
                var vehicleCountBeforeAdd = vehicles.Count;
                if (!TryAddManualHeartPoseVehicle(vehicles, pose, colors, index, isOuterPose, size, variantMode))
                {
                    if (isOuterPose)
                    {
                        skippedOuterPoseCount++;
                    }
                    else
                    {
                        skippedInteriorPoseCount++;
                    }
                }

                if (variantMode == ManualHeartVariantMode.Mystery &&
                    IsManualHeartMysteryPose(index) &&
                    vehicles.Count > vehicleCountBeforeAdd)
                {
                    var lastIndex = vehicles.Count - 1;
                    vehicles[lastIndex] = vehicles[lastIndex].WithStartsConcealed(true);
                }
            }

            if (skippedOuterPoseCount > 0)
            {
                Debug.LogWarning(
                    $"Manual Heart {GetManualHeartVariantLogName(variantMode)} skipped {skippedOuterPoseCount} outer contour pose(s) that would visually overlap the heart contour.");
            }

            if (skippedInteriorPoseCount > 0)
            {
                Debug.Log(
                    $"Manual Heart {GetManualHeartVariantLogName(variantMode)} skipped {skippedInteriorPoseCount} interior pose(s) that would visually overlap the outer heart contour.");
            }

            return vehicles;
        }

        private static IReadOnlyList<ManualHeartPose> CreateManualHeartVariantPoses(ManualHeartVariantMode variantMode)
        {
            if (IsManualHeartDenseGarageMysteryVariant(variantMode))
            {
                return CreateManualHeartColor4GarageMysteryDensePoses();
            }

            if (variantMode == ManualHeartVariantMode.DoubleOutline ||
                variantMode == ManualHeartVariantMode.Garage ||
                IsManualHeartGarageMysteryVariant(variantMode))
            {
                return CreateManualHeartDoubleOutlinePoses();
            }

            return CreateManualHeartAdCopyPoses();
        }

        private static int GetManualHeartOuterPoseCount(ManualHeartVariantMode variantMode)
        {
            if (IsManualHeartDenseGarageMysteryVariant(variantMode))
            {
                return ManualHeartColor4GarageMysteryOuterPoseCount;
            }

            return variantMode == ManualHeartVariantMode.DoubleOutline ||
                variantMode == ManualHeartVariantMode.Garage ||
                IsManualHeartGarageMysteryVariant(variantMode)
                ? ManualHeartDoubleOutlineOuterPoseCount
                : ManualHeartDirectionMixOuterPoseCount;
        }

        private static List<GarageDefinition> CreateManualHeartGarages(
            ManualHeartVariantMode variantMode,
            IReadOnlyList<PuzzleColor> colors)
        {
            var garages = new List<GarageDefinition>();
            if (variantMode != ManualHeartVariantMode.Garage &&
                !IsManualHeartGarageMysteryVariant(variantMode))
            {
                return garages;
            }

            var frontSize = IsManualHeartGarageMysteryVariant(variantMode)
                ? BusSize.Medium
                : BusSize.Small;
            var queuedSize = IsManualHeartGarageMysteryVariant(variantMode)
                ? BusSize.Large
                : BusSize.Small;

            garages.Add(CreateManualHeartGarage(
                new Vector2Int(5, 7),
                GridDirection.Left,
                colors[0 % colors.Count],
                colors[1 % colors.Count],
                frontSize,
                queuedSize));
            garages.Add(CreateManualHeartGarage(
                new Vector2Int(9, 7),
                GridDirection.Right,
                colors[2 % colors.Count],
                colors[3 % colors.Count],
                frontSize,
                queuedSize));
            return garages;
        }

        private static GarageDefinition CreateManualHeartGarage(
            Vector2Int garageCell,
            GridDirection exitDirection,
            PuzzleColor frontColor,
            PuzzleColor queuedColor,
            BusSize frontSize,
            BusSize queuedSize)
        {
            var frontCell = garageCell + GridDirectionUtility.ToGridVector(exitDirection);
            var frontVehicle = new BusDefinition(frontColor, frontSize, exitDirection, frontCell, 0f, Vector2.zero);
            var queuedVehicles = new[]
            {
                new BusDefinition(queuedColor, queuedSize, exitDirection, frontCell, 0f, Vector2.zero)
            };
            return new GarageDefinition(garageCell, exitDirection, frontVehicle, queuedVehicles);
        }

        private static List<BusDefinition> ApplyManualHeartGarageMysteryConcealment(
            IReadOnlyList<BusDefinition> vehicles,
            IReadOnlyList<GarageDefinition> garages,
            ManualHeartVariantMode variantMode)
        {
            var result = new List<BusDefinition>(vehicles.Count);
            var active = new bool[vehicles.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
            }

            var concealedCount = 0;
            var outerLimit = Mathf.Min(GetManualHeartOuterPoseCount(variantMode), vehicles.Count);
            for (var index = 0; index < vehicles.Count; index++)
            {
                var vehicle = vehicles[index];
                if (index < outerLimit &&
                    !ManualHeartIsInitialPathClear(index, vehicles, active, garages))
                {
                    vehicle = vehicle.WithStartsConcealed(true);
                    concealedCount++;
                }

                result.Add(vehicle);
            }

            Debug.Log(
                $"Manual Heart {GetManualHeartVariantLogName(variantMode)} concealed {concealedCount} outer vehicle(s), leaving immediate opening vehicles visible.");
            return result;
        }

        private static bool ManualHeartIsInitialPathClear(
            int movingIndex,
            IReadOnlyList<BusDefinition> vehicles,
            IReadOnlyList<bool> active,
            IReadOnlyList<GarageDefinition> garages)
        {
            const float exitClearanceCells = 0.75f;
            const float sweepStepCells = 0.16f;
            const float collisionClearanceCells = 0.035f;
            if (vehicles == null || movingIndex < 0 || movingIndex >= vehicles.Count)
            {
                return false;
            }

            var movingVehicle = vehicles[movingIndex];
            var worldDirection = movingVehicle.Rotation * Vector3.forward;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            worldDirection.Normalize();
            var movingRoot = GetManualHeartRootPositionCells(movingVehicle);
            var movingFootprint = GetManualHeartVisualFootprintCells(movingVehicle);
            var sweepDistance = GetManualHeartBoardExitSweepDistance(movingFootprint, worldDirection, exitClearanceCells);
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(sweepDistance / sweepStepCells));
            for (var sample = 1; sample <= sampleCount; sample++)
            {
                var distance = Mathf.Min(sweepDistance, sample * sweepStepCells);
                var footprint = GetManualHeartVisualFootprintCells(
                    movingVehicle,
                    movingRoot + worldDirection * distance);

                for (var index = 0; index < vehicles.Count; index++)
                {
                    if (index == movingIndex ||
                        (active != null && index >= 0 && index < active.Count && !active[index]))
                    {
                        continue;
                    }

                    if (footprint.IsWithinPadding(
                        GetManualHeartVisualFootprintCells(vehicles[index]),
                        collisionClearanceCells))
                    {
                        return false;
                    }
                }

                for (var garageIndex = 0; garageIndex < garages.Count; garageIndex++)
                {
                    if (footprint.IsWithinPadding(
                        GetManualHeartGarageFootprintCells(garages[garageIndex]),
                        collisionClearanceCells))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static float GetManualHeartBoardExitSweepDistance(
            VehicleFootprint footprint,
            Vector3 worldDirection,
            float exitClearanceCells)
        {
            var leftBoundary = -0.5f - exitClearanceCells;
            var rightBoundary = ManualHeartGridColumns - 0.5f + exitClearanceCells;
            var bottomBoundary = -0.5f - exitClearanceCells;
            var topBoundary = ManualHeartGridRows - 0.5f + exitClearanceCells;
            var bestDistance = float.PositiveInfinity;

            if (worldDirection.x > 0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (rightBoundary - footprint.ProjectMax(Vector2.right)) / worldDirection.x);
            }
            else if (worldDirection.x < -0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (leftBoundary - footprint.ProjectMin(Vector2.right)) / worldDirection.x);
            }

            if (worldDirection.z > 0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (topBoundary - footprint.ProjectMax(Vector2.up)) / worldDirection.z);
            }
            else if (worldDirection.z < -0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (bottomBoundary - footprint.ProjectMin(Vector2.up)) / worldDirection.z);
            }

            if (float.IsInfinity(bestDistance) || bestDistance < 0f)
            {
                return Mathf.Max(ManualHeartGridColumns, ManualHeartGridRows);
            }

            return bestDistance;
        }

        private static List<BusDefinition> RemoveManualHeartVehiclesConflictingWithGarages(
            IReadOnlyList<BusDefinition> vehicles,
            IReadOnlyList<GarageDefinition> garages,
            ManualHeartVariantMode variantMode)
        {
            var filtered = new List<BusDefinition>(vehicles.Count);
            for (var index = 0; index < vehicles.Count; index++)
            {
                if (!ManualHeartVehicleConflictsWithGarages(vehicles[index], garages))
                {
                    filtered.Add(vehicles[index]);
                }
            }

            var removedCount = vehicles.Count - filtered.Count;
            if (removedCount > 0)
            {
                Debug.Log(
                    $"Manual Heart {GetManualHeartVariantLogName(variantMode)} removed {removedCount} center vehicle(s) to place garage anchors.");
            }

            return filtered;
        }

        private static bool ManualHeartVehicleConflictsWithGarages(
            BusDefinition vehicle,
            IReadOnlyList<GarageDefinition> garages)
        {
            const float garageClearanceCells = 0.06f;
            var vehicleFootprint = GetManualHeartVisualFootprintCells(vehicle);
            for (var garageIndex = 0; garageIndex < garages.Count; garageIndex++)
            {
                var garage = garages[garageIndex];
                if (vehicleFootprint.IsWithinPadding(GetManualHeartGarageFootprintCells(garage), garageClearanceCells))
                {
                    return true;
                }

                foreach (var garageVehicle in garage.EnumerateVehicles())
                {
                    if (vehicleFootprint.IsWithinPadding(
                        GetManualHeartVisualFootprintCells(garageVehicle),
                        garageClearanceCells))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static VehicleFootprint GetManualHeartGarageFootprintCells(GarageDefinition garage)
        {
            return new VehicleFootprint(
                new Vector3(garage.GridPosition.x, 0f, garage.GridPosition.y),
                Vector3.right,
                Vector3.forward,
                0.45f,
                0.45f);
        }

        private static IReadOnlyList<ManualHeartPose> CreateManualHeartDoubleOutlinePoses()
        {
            var basePoses = CreateManualHeartAdCopyPoses();
            var poses = new List<ManualHeartPose>(basePoses.Count + 20);
            for (var index = 0; index < ManualHeartDirectionMixOuterPoseCount; index++)
            {
                poses.Add(basePoses[index]);
            }

            poses.AddRange(new[]
            {
                new ManualHeartPose(4.5f, 11.6f, -62f),
                new ManualHeartPose(2.7f, 11.0f, -110f),
                new ManualHeartPose(1.4f, 9.6f, -150f),
                new ManualHeartPose(0.8f, 8.0f, -174f),
                new ManualHeartPose(0.9f, 6.5f, 174f),
                new ManualHeartPose(1.4f, 5.2f, 162f),
                new ManualHeartPose(2.2f, 4.0f, 148f),
                new ManualHeartPose(3.3f, 2.7f, 136f),
                new ManualHeartPose(4.6f, 1.5f, 124f),
                new ManualHeartPose(5.9f, 0.7f, 112f),
                new ManualHeartPose(7.0f, 0.4f, 180f),
                new ManualHeartPose(8.2f, 0.7f, 68f),
                new ManualHeartPose(9.5f, 1.5f, 56f),
                new ManualHeartPose(10.8f, 2.7f, 44f),
                new ManualHeartPose(11.8f, 4.1f, 30f),
                new ManualHeartPose(12.5f, 5.6f, 18f),
                new ManualHeartPose(12.7f, 7.0f, 6f),
                new ManualHeartPose(12.4f, 8.3f, -18f),
                new ManualHeartPose(11.7f, 9.6f, -34f),
                new ManualHeartPose(10.4f, 10.8f, -64f)
            });

            for (var index = ManualHeartDirectionMixOuterPoseCount; index < basePoses.Count; index++)
            {
                poses.Add(basePoses[index]);
            }

            return poses;
        }

        private static IReadOnlyList<ManualHeartPose> CreateManualHeartColor4GarageMysteryDensePoses()
        {
            var basePoses = CreateManualHeartAdCopyPoses();
            var poses = new List<ManualHeartPose>(basePoses.Count + 37);
            for (var index = 0; index < ManualHeartDirectionMixOuterPoseCount; index++)
            {
                poses.Add(basePoses[index]);
            }

            poses.AddRange(new[]
            {
                new ManualHeartPose(4.5f, 11.6f, -62f),
                new ManualHeartPose(3.7f, 11.3f, -82f),
                new ManualHeartPose(2.7f, 11.0f, -110f),
                new ManualHeartPose(1.9f, 10.3f, -130f),
                new ManualHeartPose(1.4f, 9.6f, -150f),
                new ManualHeartPose(1.0f, 8.8f, -162f),
                new ManualHeartPose(0.8f, 8.0f, -174f),
                new ManualHeartPose(0.9f, 6.5f, 174f),
                new ManualHeartPose(1.1f, 5.9f, 168f),
                new ManualHeartPose(1.4f, 5.2f, 162f),
                new ManualHeartPose(1.7f, 4.6f, 155f),
                new ManualHeartPose(2.2f, 4.0f, 148f),
                new ManualHeartPose(2.8f, 3.3f, 142f),
                new ManualHeartPose(3.3f, 2.7f, 136f),
                new ManualHeartPose(3.9f, 2.1f, 130f),
                new ManualHeartPose(4.6f, 1.5f, 124f),
                new ManualHeartPose(5.2f, 1.0f, 118f),
                new ManualHeartPose(5.9f, 0.7f, 112f),
                new ManualHeartPose(7.0f, 0.4f, 180f),
                new ManualHeartPose(8.2f, 0.7f, 68f),
                new ManualHeartPose(8.8f, 1.0f, 62f),
                new ManualHeartPose(9.5f, 1.5f, 56f),
                new ManualHeartPose(10.1f, 2.1f, 50f),
                new ManualHeartPose(10.8f, 2.7f, 44f),
                new ManualHeartPose(11.3f, 3.4f, 38f),
                new ManualHeartPose(11.8f, 4.1f, 30f),
                new ManualHeartPose(12.2f, 4.9f, 24f),
                new ManualHeartPose(12.5f, 5.6f, 18f),
                new ManualHeartPose(12.6f, 6.3f, 12f),
                new ManualHeartPose(12.7f, 7.0f, 6f),
                new ManualHeartPose(12.6f, 7.8f, -8f),
                new ManualHeartPose(12.4f, 8.3f, -18f),
                new ManualHeartPose(12.1f, 9.0f, -25f),
                new ManualHeartPose(11.7f, 9.6f, -34f),
                new ManualHeartPose(11.0f, 10.2f, -48f),
                new ManualHeartPose(10.4f, 10.8f, -64f),
                new ManualHeartPose(9.6f, 11.3f, -84f)
            });

            for (var index = ManualHeartDirectionMixOuterPoseCount; index < basePoses.Count; index++)
            {
                poses.Add(basePoses[index]);
            }

            return poses;
        }

        private static IReadOnlyList<ManualHeartPose> CreateManualHeartAdCopyPoses()
        {
            return new[]
            {
                new ManualHeartPose(5.0f, 10.7f, -78.11134f),
                new ManualHeartPose(1.9f, 8.6f, -159.27446f),
                new ManualHeartPose(2.7f, 4.7f, 145.124f),
                new ManualHeartPose(4.1f, 3.2f, 132.1376f),
                new ManualHeartPose(5.8f, 1.9f, 124.59229f),
                new ManualHeartPose(8.3f, 1.9f, 56.30993f),
                new ManualHeartPose(10.0f, 3.2f, 43.15239f),
                new ManualHeartPose(11.3f, 5.1f, 26.56505f),
                new ManualHeartPose(11.6f, 9.1f, -27.34988f),
                new ManualHeartPose(8.7f, 10.7f, -104.82648f),
                new ManualHeartPose(7.0f, 9.4f, -90f),

                new ManualHeartPose(4.6f, 8.7f, -90f),
                new ManualHeartPose(6.0f, 8.5f, -90f),
                new ManualHeartPose(8.0f, 8.5f, 90f),
                new ManualHeartPose(9.4f, 8.7f, 90f),
                new ManualHeartPose(3.7f, 7.2f, -90f),
                new ManualHeartPose(5.3f, 7.2f, -90f),
                new ManualHeartPose(6.8f, 7.0f, 180f),
                new ManualHeartPose(8.3f, 7.2f, 90f),
                new ManualHeartPose(9.9f, 7.2f, 90f),
                new ManualHeartPose(4.4f, 5.8f, -90f),
                new ManualHeartPose(6.0f, 5.7f, -90f),
                new ManualHeartPose(7.8f, 5.7f, 90f),
                new ManualHeartPose(9.4f, 5.8f, 90f),
                new ManualHeartPose(5.1f, 4.3f, -90f),
                new ManualHeartPose(6.8f, 4.1f, 180f),
                new ManualHeartPose(8.5f, 4.3f, 90f),
                new ManualHeartPose(6.3f, 2.8f, 180f),
                new ManualHeartPose(7.7f, 2.8f, 180f),
                new ManualHeartPose(6.6f, 3.2f, 180f),
                new ManualHeartPose(3.4f, 6.2f, -90f),
                new ManualHeartPose(8.6f, 3.2f, 90f),
                new ManualHeartPose(10.0f, 9.2f, 90f),
                new ManualHeartPose(3.0f, 8.4f, -90f),
                new ManualHeartPose(11.3f, 7.2f, 90f),
                new ManualHeartPose(2.0f, 6.2f, -90f),
                new ManualHeartPose(3.0f, 9.2f, -90f),
                new ManualHeartPose(3.8f, 10.2f, -90f),
                new ManualHeartPose(11.8f, 6.2f, 90f),
                new ManualHeartPose(7.4f, 1.2f, 180f)
            };
        }

        private static bool TryAddManualHeartPoseVehicle(
            List<BusDefinition> vehicles,
            ManualHeartPose pose,
            IReadOnlyList<PuzzleColor> colors,
            int index,
            bool isOuterPose,
            BusSize size,
            ManualHeartVariantMode variantMode)
        {
            var direction = DirectionFromYaw(pose.Yaw);
            var angleOffset = Mathf.DeltaAngle(GridDirectionUtility.ToYawDegrees(direction), pose.Yaw);
            var color = colors[index % colors.Count];
            if (TryFindManualHeartPosePlacement(
                vehicles,
                pose,
                color,
                size,
                direction,
                angleOffset,
                isOuterPose,
                out var placement))
            {
                vehicles.Add(placement);
                return true;
            }

            if (isOuterPose)
            {
                Debug.LogWarning(
                    $"Manual Heart {GetManualHeartVariantLogName(variantMode)} could not place outer contour pose #{index + 1} without overlap; skipping it to keep the asset valid.");
            }

            return false;
        }

        private static bool TryFindManualHeartPosePlacement(
            IReadOnlyList<BusDefinition> placedVehicles,
            ManualHeartPose pose,
            PuzzleColor color,
            BusSize size,
            GridDirection direction,
            float angleOffset,
            bool isOuterPose,
            out BusDefinition placement)
        {
            var searchDirections = GetManualHeartPosePlacementSearchDirections(pose);
            var maxSteps = isOuterPose ? 9 : 4;
            var stepDistance = isOuterPose ? 0.12f : 0.10f;
            var bestCandidate = default(BusDefinition);
            var bestScore = float.MaxValue;
            var foundCandidate = false;

            for (var step = 0; step <= maxSteps; step++)
            {
                var distance = step * stepDistance;
                for (var directionIndex = 0; directionIndex < searchDirections.Count; directionIndex++)
                {
                    if (step == 0 && directionIndex > 0)
                    {
                        continue;
                    }

                    var searchDirection = searchDirections[directionIndex];
                    var visualCenter = ClampManualHeartPosition(pose.Position + searchDirection * distance);
                    var rootPosition = GetManualHeartRootPositionFromVisualCenter(visualCenter, pose.Yaw, size);
                    var candidate = CreateManualHeartBusDefinition(color, size, direction, angleOffset, rootPosition);
                    if (ManualHeartOverlapsPlacedVehicles(candidate, placedVehicles))
                    {
                        continue;
                    }

                    var score = (visualCenter - pose.Position).sqrMagnitude + directionIndex * 0.001f;
                    if (score < bestScore)
                    {
                        bestCandidate = candidate;
                        bestScore = score;
                        foundCandidate = true;
                    }
                }
            }

            placement = bestCandidate;
            return foundCandidate;
        }

        private static IReadOnlyList<Vector2> GetManualHeartPosePlacementSearchDirections(ManualHeartPose pose)
        {
            var fromCenter = pose.Position - new Vector2(6.8f, 6.35f);
            var outward = fromCenter.sqrMagnitude > 0.0001f ? fromCenter.normalized : Vector2.up;
            var tangent = new Vector2(outward.y, -outward.x);
            return new[]
            {
                Vector2.zero,
                outward,
                tangent,
                -tangent,
                outward + tangent,
                outward - tangent,
                -outward,
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            };
        }

        private static Vector2 GetManualHeartRootPositionFromVisualCenter(
            Vector2 visualCenter,
            float yaw,
            BusSize size)
        {
            var radians = yaw * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            var visualLength = BusSizeUtility.ToVisualLengthCells(size);
            var visualCharacterLength = visualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(size));
            return visualCenter - forward * ((visualLength - visualCharacterLength) * 0.5f);
        }

        private static List<ManualHeartVehicleSeed> CreateManualHeartVehicleSeeds(IReadOnlyList<ManualHeartRow> rows)
        {
            var seeds = new List<ManualHeartVehicleSeed>();
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
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
                        seeds.Add(new ManualHeartVehicleSeed(left[index], row.Row, rowIndex, index, true, seeds.Count));
                    }

                    if (index < right.Count)
                    {
                        seeds.Add(new ManualHeartVehicleSeed(right[index], row.Row, rowIndex, index, false, seeds.Count));
                    }
                }
            }

            return seeds;
        }

        private static int CompareManualHeartDirectionMixSeeds(ManualHeartVehicleSeed left, ManualHeartVehicleSeed right)
        {
            var contourCompare = GetManualHeartContourPriority(left).CompareTo(GetManualHeartContourPriority(right));
            if (contourCompare != 0)
            {
                return contourCompare;
            }

            return left.SourceIndex.CompareTo(right.SourceIndex);
        }

        private static int GetManualHeartContourPriority(ManualHeartVehicleSeed seed)
        {
            if (TryGetManualHeartAdContourYaw(
                seed.Column,
                seed.Row,
                seed.RowIndex,
                seed.LocalIndex,
                seed.ExitsLeft,
                out _))
            {
                return 0;
            }

            return 1;
        }

        private static void AppendManualHeartRowVehicles(
            List<BusDefinition> vehicles,
            ManualHeartRow row,
            IReadOnlyList<PuzzleColor> colors,
            int rowIndex,
            ManualHeartVariantMode variantMode)
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
                    AddManualHeartVehicle(vehicles, left[index], row.Row, colors, rowIndex, index, true, variantMode);
                }

                if (index < right.Count)
                {
                    AddManualHeartVehicle(vehicles, right[index], row.Row, colors, rowIndex, index, false, variantMode);
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
            ManualHeartVariantMode variantMode)
        {
            var yaw = GetManualHeartYaw(column, row, rowIndex, localIndex, exitsLeft, variantMode);
            var direction = DirectionFromYaw(yaw);
            var angleOffset = Mathf.DeltaAngle(GridDirectionUtility.ToYawDegrees(direction), yaw);
            var color = colors[(vehicles.Count + rowIndex * 2 + localIndex) % colors.Count];
            var size = GetManualHeartSize(column, row);
            var adjustedPosition = GetManualHeartPosition(column, row, variantMode);
            var gridPosition = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(adjustedPosition.x), 0, ManualHeartGridColumns - 1),
                Mathf.Clamp(Mathf.RoundToInt(adjustedPosition.y), 0, ManualHeartGridRows - 1));
            var positionOffset = new Vector2(adjustedPosition.x - gridPosition.x, adjustedPosition.y - gridPosition.y);
            vehicles.Add(new BusDefinition(
                color,
                size,
                direction,
                gridPosition,
                angleOffset,
                positionOffset));
        }

        private static void AddManualHeartDirectionMixVehicle(
            List<BusDefinition> vehicles,
            ManualHeartVehicleSeed seed,
            IReadOnlyList<PuzzleColor> colors)
        {
            var yaw = GetManualHeartDirectionMixYaw(
                seed.Column,
                seed.Row,
                seed.RowIndex,
                seed.LocalIndex,
                seed.ExitsLeft);
            var direction = DirectionFromYaw(yaw);
            var angleOffset = Mathf.DeltaAngle(GridDirectionUtility.ToYawDegrees(direction), yaw);
            var color = colors[(seed.SourceIndex + seed.RowIndex * 2 + seed.LocalIndex) % colors.Count];
            var size = GetManualHeartSize(seed.Column, seed.Row);
            var basePosition = GetManualHeartDirectionFirstPosition(seed);
            var placement = FindManualHeartDirectionFirstPlacement(
                vehicles,
                color,
                size,
                direction,
                angleOffset,
                basePosition,
                seed);
            vehicles.Add(placement);
        }

        private static Vector2 GetManualHeartDirectionFirstPosition(ManualHeartVehicleSeed seed)
        {
            var shapeCenter = new Vector2(6.8f, 6.35f);
            var position = new Vector2(seed.Column, seed.Row);
            return shapeCenter + (position - shapeCenter) * 1.1f;
        }

        private static BusDefinition FindManualHeartDirectionFirstPlacement(
            IReadOnlyList<BusDefinition> placedVehicles,
            PuzzleColor color,
            BusSize size,
            GridDirection direction,
            float angleOffset,
            Vector2 basePosition,
            ManualHeartVehicleSeed seed)
        {
            var searchDirections = GetManualHeartPlacementSearchDirections(basePosition, seed);
            var bestCandidate = CreateManualHeartBusDefinition(color, size, direction, angleOffset, basePosition);
            var bestScore = float.MaxValue;
            for (var directionIndex = 0; directionIndex < searchDirections.Count; directionIndex++)
            {
                var searchDirection = searchDirections[directionIndex];
                for (var step = 0; step <= 4; step++)
                {
                    var distance = step * 0.08f;
                    if (step == 0 && directionIndex > 0)
                    {
                        continue;
                    }

                    var candidatePosition = ClampManualHeartPosition(basePosition + searchDirection * distance);
                    var candidate = CreateManualHeartBusDefinition(color, size, direction, angleOffset, candidatePosition);
                    if (ManualHeartOverlapsPlacedVehicles(candidate, placedVehicles))
                    {
                        continue;
                    }

                    var score = (candidatePosition - basePosition).sqrMagnitude + directionIndex * 0.0005f;
                    if (score < bestScore)
                    {
                        bestCandidate = candidate;
                        bestScore = score;
                    }
                }
            }

            return bestCandidate;
        }

        private static IReadOnlyList<Vector2> GetManualHeartPlacementSearchDirections(
            Vector2 basePosition,
            ManualHeartVehicleSeed seed)
        {
            var fromCenter = basePosition - new Vector2(6.8f, 6.35f);
            var outward = fromCenter.sqrMagnitude > 0.0001f ? fromCenter.normalized : Vector2.up;
            var side = seed.ExitsLeft ? Vector2.left : Vector2.right;
            return new[]
            {
                Vector2.zero,
                outward,
                -outward,
                side,
                -side,
                Vector2.up,
                Vector2.down
            };
        }

        private static Vector2 ClampManualHeartPosition(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, 0.2f, ManualHeartGridColumns - 1.2f),
                Mathf.Clamp(position.y, 0.2f, ManualHeartGridRows - 1.2f));
        }

        private static BusDefinition CreateManualHeartBusDefinition(
            PuzzleColor color,
            BusSize size,
            GridDirection direction,
            float angleOffset,
            Vector2 position)
        {
            var gridPosition = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(position.x), 0, ManualHeartGridColumns - 1),
                Mathf.Clamp(Mathf.RoundToInt(position.y), 0, ManualHeartGridRows - 1));
            var positionOffset = new Vector2(position.x - gridPosition.x, position.y - gridPosition.y);
            return new BusDefinition(color, size, direction, gridPosition, angleOffset, positionOffset);
        }

        private static bool ManualHeartOverlapsPlacedVehicles(
            BusDefinition candidate,
            IReadOnlyList<BusDefinition> placedVehicles)
        {
            var footprint = GetManualHeartVisualFootprintCells(candidate);
            for (var index = 0; index < placedVehicles.Count; index++)
            {
                if (footprint.Overlaps(GetManualHeartVisualFootprintCells(placedVehicles[index])))
                {
                    return true;
                }
            }

            return false;
        }

        private static VehicleFootprint GetManualHeartVisualFootprintCells(BusDefinition bus)
        {
            return GetManualHeartVisualFootprintCells(bus, GetManualHeartRootPositionCells(bus));
        }

        private static Vector3 GetManualHeartRootPositionCells(BusDefinition bus)
        {
            return new Vector3(
                bus.GridPosition.x + bus.PositionOffsetCells.x,
                0f,
                bus.GridPosition.y + bus.PositionOffsetCells.y);
        }

        private static VehicleFootprint GetManualHeartVisualFootprintCells(BusDefinition bus, Vector3 rootPosition)
        {
            const float visualWidthCells = 0.72f * 1.32f;
            const float bodyVisualWidthScale = 0.99f;
            const float bodyVisualLengthScale = 1.03f;
            var visualLength = BusSizeUtility.ToVisualLengthCells(bus.Size);
            var visualCharacterLength = visualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(bus.Size));
            var rotation = bus.Rotation;
            var visualCenter = rootPosition + rotation * new Vector3(0f, 0f, (visualLength - visualCharacterLength) * 0.5f);
            return new VehicleFootprint(
                visualCenter,
                rotation * Vector3.right,
                rotation * Vector3.forward,
                visualWidthCells * bodyVisualWidthScale * 0.5f,
                visualLength * bodyVisualLengthScale * 0.5f);
        }

        private static float GetManualHeartYaw(
            float column,
            float row,
            int rowIndex,
            int localIndex,
            bool exitsLeft,
            ManualHeartVariantMode variantMode)
        {
            if (variantMode == ManualHeartVariantMode.DirectionMix)
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
            if (TryGetManualHeartAdContourYaw(column, row, rowIndex, localIndex, exitsLeft, out var yaw))
            {
                return yaw;
            }

            return GetManualHeartReferenceYaw(column, row, exitsLeft);
        }

        private static Vector2 GetManualHeartPosition(
            float column,
            float row,
            ManualHeartVariantMode variantMode)
        {
            var position = new Vector2(column, row);
            if (variantMode != ManualHeartVariantMode.DirectionMix)
            {
                return position;
            }

            var shapeCenter = new Vector2(6.8f, 6.35f);
            return shapeCenter + (position - shapeCenter) * 1.1f;
        }

        private static bool TryGetManualHeartAdContourYaw(
            float column,
            float row,
            int rowIndex,
            int localIndex,
            bool exitsLeft,
            out float yaw)
        {
            if (TryGetManualHeartBottomContourYaw(column, rowIndex, out yaw))
            {
                return true;
            }

            if (rowIndex == 0)
            {
                yaw = GetManualHeartTopLobeYaw(column, localIndex, exitsLeft);
                return true;
            }

            if (IsManualHeartInnerNotchContour(column, row, rowIndex))
            {
                yaw = exitsLeft ? 58f : -58f;
                return true;
            }

            if (localIndex == 0)
            {
                yaw = GetManualHeartOuterSideYaw(rowIndex, exitsLeft);
                return true;
            }

            return false;
        }

        private static float GetManualHeartTopLobeYaw(float column, int localIndex, bool exitsLeft)
        {
            if (localIndex == 0)
            {
                return exitsLeft ? -68f : 68f;
            }

            return column < 6.8f ? 22f : -22f;
        }

        private static float GetManualHeartOuterSideYaw(int rowIndex, bool exitsLeft)
        {
            var sideYaw = 90f;
            switch (rowIndex)
            {
                case 1:
                    sideYaw = 72f;
                    break;
                case 2:
                    sideYaw = 86f;
                    break;
                case 3:
                    sideYaw = 96f;
                    break;
                case 4:
                    sideYaw = 100f;
                    break;
                case 5:
                    sideYaw = 106f;
                    break;
                case 6:
                    sideYaw = 112f;
                    break;
                case 7:
                    sideYaw = 108f;
                    break;
                case 8:
                    sideYaw = 104f;
                    break;
            }

            return exitsLeft ? -sideYaw : sideYaw;
        }

        private static bool TryGetManualHeartBottomContourYaw(float column, int rowIndex, out float yaw)
        {
            yaw = 0f;
            if (rowIndex < 10)
            {
                return false;
            }

            var distanceFromPoint = column - 7.4f;
            if (Mathf.Abs(distanceFromPoint) <= 0.45f)
            {
                yaw = 180f;
                return true;
            }

            yaw = distanceFromPoint < 0f ? -126f : 126f;
            return true;
        }

        private static bool IsManualHeartInnerNotchContour(float column, float row, int rowIndex)
        {
            return rowIndex <= 1 && row >= 9.1f && column >= 5.0f && column <= 8.0f;
        }

        private static IReadOnlyList<PuzzleColor> GetManualHeartColors(ManualHeartVariantMode variantMode)
        {
            if (variantMode == ManualHeartVariantMode.Color4 ||
                variantMode == ManualHeartVariantMode.Color4GarageMystery)
            {
                return new[]
                {
                    PuzzleColor.Red,
                    PuzzleColor.SkyBlue,
                    PuzzleColor.Yellow,
                    PuzzleColor.Purple
                };
            }

            return new[]
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
        }

        private static int GetManualHeartTargetColorCount(ManualHeartVariantMode variantMode)
        {
            return variantMode == ManualHeartVariantMode.Color4 ||
                variantMode == ManualHeartVariantMode.Color4GarageMystery
                ? 4
                : 9;
        }

        private static int GetManualHeartPassengerSeed(ManualHeartVariantMode variantMode)
        {
            switch (variantMode)
            {
                case ManualHeartVariantMode.DirectionMix:
                    return 19082;
                case ManualHeartVariantMode.Color4:
                    return 19083;
                case ManualHeartVariantMode.SizeMix:
                    return 19084;
                case ManualHeartVariantMode.Mystery:
                    return 19085;
                case ManualHeartVariantMode.DoubleOutline:
                    return 19086;
                case ManualHeartVariantMode.Garage:
                    return 19087;
                case ManualHeartVariantMode.GarageMystery:
                    return 19088;
                case ManualHeartVariantMode.Color4GarageMystery:
                    return 19089;
                case ManualHeartVariantMode.FullColorGarageMystery:
                    return 19090;
                default:
                    return 19081;
            }
        }

        private static string GetManualHeartGenerationSignature(ManualHeartVariantMode variantMode)
        {
            switch (variantMode)
            {
                case ManualHeartVariantMode.DirectionMix:
                    return "manualShape=heart_direction_mix;stage=9;source=heart_basic_01;directionMode=manual_front_tangent_fill_v10;";
                case ManualHeartVariantMode.Color4:
                    return "manualShape=heart_color4;stage=9;source=heart_basic_01;colorCount=4;";
                case ManualHeartVariantMode.SizeMix:
                    return "manualShape=heart_size_mix;stage=9;source=heart_basic_01;sizeMode=manual_medium_large_v1;";
                case ManualHeartVariantMode.Mystery:
                    return "manualShape=heart_mystery;stage=9;source=heart_direction_mix_01;mysteryMode=inner_8_v1;";
                case ManualHeartVariantMode.DoubleOutline:
                    return "manualShape=heart_double_outline;stage=9;source=heart_direction_mix_01;outlineMode=double_ring_tight_v2;";
                case ManualHeartVariantMode.Garage:
                    return "manualShape=heart_garage;stage=9;source=heart_double_outline_01;garageMode=side_pair_v1;";
                case ManualHeartVariantMode.GarageMystery:
                    return "manualShape=heart_garage_mystery;stage=9;source=heart_garage_01;mysteryMode=outer_non_opening_v1;sizeMode=inner_and_garage_medium_large_v1;";
                case ManualHeartVariantMode.Color4GarageMystery:
                    return "manualShape=heart_color4_garage_mystery;stage=9;source=heart_garage_mystery_01;colorCount=4;mysteryMode=outer_non_opening_v1;sizeMode=inner_and_garage_medium_large_v1;";
                case ManualHeartVariantMode.FullColorGarageMystery:
                    return "manualShape=heart_full_color_garage_mystery;stage=9;source=heart_garage_mystery_01;colorCount=9;mysteryMode=outer_non_opening_v1;sizeMode=inner_and_garage_medium_large_v1;densityMode=outer_dense_v1;";
                default:
                    return "manualShape=heart_reference;stage=9;source=ad_reference_heart;";
            }
        }

        private static string GetManualHeartVariantLogName(ManualHeartVariantMode variantMode)
        {
            switch (variantMode)
            {
                case ManualHeartVariantMode.DirectionMix:
                    return "direction mix";
                case ManualHeartVariantMode.Color4:
                    return "color 4";
                case ManualHeartVariantMode.SizeMix:
                    return "size mix";
                case ManualHeartVariantMode.Mystery:
                    return "mystery";
                case ManualHeartVariantMode.DoubleOutline:
                    return "double outline";
                case ManualHeartVariantMode.Garage:
                    return "garage";
                case ManualHeartVariantMode.GarageMystery:
                    return "garage mystery";
                case ManualHeartVariantMode.Color4GarageMystery:
                    return "color 4 garage mystery";
                case ManualHeartVariantMode.FullColorGarageMystery:
                    return "full color garage mystery";
                default:
                    return "reference";
            }
        }

        private static bool IsManualHeartGarageMysteryVariant(ManualHeartVariantMode variantMode)
        {
            return variantMode == ManualHeartVariantMode.GarageMystery ||
                variantMode == ManualHeartVariantMode.Color4GarageMystery ||
                variantMode == ManualHeartVariantMode.FullColorGarageMystery;
        }

        private static bool IsManualHeartDenseGarageMysteryVariant(ManualHeartVariantMode variantMode)
        {
            return variantMode == ManualHeartVariantMode.Color4GarageMystery ||
                variantMode == ManualHeartVariantMode.FullColorGarageMystery;
        }

        private static bool IsManualHeartMysteryPose(int poseIndex)
        {
            switch (poseIndex)
            {
                case 12:
                case 13:
                case 17:
                case 21:
                case 22:
                case 25:
                case 30:
                case 34:
                    return true;
                default:
                    return false;
            }
        }

        private static BusSize GetManualHeartPoseSize(ManualHeartVariantMode variantMode, int poseIndex)
        {
            var outerPoseCount = GetManualHeartOuterPoseCount(variantMode);
            if (IsManualHeartGarageMysteryVariant(variantMode) &&
                poseIndex >= outerPoseCount)
            {
                switch (poseIndex - outerPoseCount)
                {
                    case 11:
                    case 16:
                    case 21:
                        return BusSize.Large;
                    default:
                        return BusSize.Medium;
                }
            }

            if (variantMode != ManualHeartVariantMode.SizeMix)
            {
                return BusSize.Small;
            }

            switch (poseIndex)
            {
                case 25:
                    return BusSize.Large;
                case 12:
                case 13:
                case 21:
                case 22:
                case 30:
                case 31:
                    return BusSize.Medium;
                default:
                    return BusSize.Small;
            }
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

        private readonly struct ManualHeartVehicleSeed
        {
            public ManualHeartVehicleSeed(
                float column,
                float row,
                int rowIndex,
                int localIndex,
                bool exitsLeft,
                int sourceIndex)
            {
                Column = column;
                Row = row;
                RowIndex = rowIndex;
                LocalIndex = localIndex;
                ExitsLeft = exitsLeft;
                SourceIndex = sourceIndex;
            }

            public float Column { get; }
            public float Row { get; }
            public int RowIndex { get; }
            public int LocalIndex { get; }
            public bool ExitsLeft { get; }
            public int SourceIndex { get; }
        }

        private readonly struct ManualHeartPose
        {
            public ManualHeartPose(float x, float y, float yaw)
            {
                Position = new Vector2(x, y);
                Yaw = yaw;
            }

            public Vector2 Position { get; }
            public float Yaw { get; }
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
            var vehicles = level.AllVehicles;
            for (var index = 0; index < vehicles.Count; index++)
            {
                switch (vehicles[index].Size)
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

            var garageCount = level.Garages.Count;
            var vehicleSummary = garageCount > 0
                ? $"vehicles {vehicles.Count} (visible {buses.Count}, garages {garageCount})"
                : $"vehicles {vehicles.Count}";
            return $"{vehicleSummary}, Small {smallCount} / Medium {mediumCount} / Large {largeCount}, " +
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
