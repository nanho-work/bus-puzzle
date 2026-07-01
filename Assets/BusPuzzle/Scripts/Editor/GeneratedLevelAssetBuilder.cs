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
        private const string ActiveLevelSequencePath = LevelDirectory + "/LevelSequence.asset";
        private const string GeneratedLevelSequencePath = GeneratedLevelDirectory + "/GeneratedLevelSequence.asset";
        private const string StageGenerationConfigPath = LevelDirectory + "/StageGenerationConfig.asset";
        private const int GeneratedStageBatchSize = 25;
        private const int PreviewStageCount = 100;
        private const int ShapeLibraryPreviewStageCount = 31;

        private enum GeneratedStageBuildMode
        {
            FullSet,
            NextBatch,
            PreviewFirst100,
            ShapeLibraryPreview
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
