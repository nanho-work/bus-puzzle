#if UNITY_EDITOR
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

        [MenuItem("Bus Puzzle/Levels/Rebuild Generated Stage Set")]
        public static void RebuildGeneratedStageSet()
        {
            var config = LoadConfig();
            Directory.CreateDirectory(GeneratedLevelDirectory);
            var savedLevels = new LevelData[config.GeneratedStageCount];
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var cancelled = false;
            var timedOut = false;

            try
            {
                for (var stageNumber = 1; stageNumber <= config.GeneratedStageCount; stageNumber++)
                {
                    var request = StageGenerationPlanner.CreateRequest(config, stageNumber);
                    if (TryReuseExistingLevel(config, request, out var existingLevel))
                    {
                        savedLevels[stageNumber - 1] = existingLevel;
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
                            var stageProgress = (stageNumber - 1f + candidate / (float)config.CandidateAttemptsPerStage) /
                                config.GeneratedStageCount;
                            cancelled = EditorUtility.DisplayCancelableProgressBar(
                                "Rebuilding Bus Pop Stage Set",
                                $"Stage {stageNumber:000}/{config.GeneratedStageCount} - candidate {candidate + 1}/{config.CandidateAttemptsPerStage}",
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
                            Debug.LogWarning($"Generated stage set rebuild cancelled at stage {stageNumber:000}.");
                            return;
                        }

                        if (timedOut)
                        {
                            Debug.LogError(
                                $"Generated stage set rebuild timed out at stage {stageNumber:000} after " +
                                $"{config.ReleaseBuildTimeBudgetSeconds} seconds. Lower generation pressure or try again.");
                            return;
                        }

                        Debug.LogError(
                            $"Generated stage set rebuild aborted at stage {stageNumber:000}. " +
                            $"No verified candidate found after {config.CandidateAttemptsPerStage} attempts. " +
                            $"Last solution count: {analysis.SolutionCount}, target range: {request.MinSolutionCount}-{request.MaxSolutionCount}. " +
                            $"{CreateReportMessage(generatedLevel, report)}");
                        return;
                    }

                    savedLevels[stageNumber - 1] = SaveLevel($"Level_{stageNumber:000}", generatedLevel);
                    if (stageNumber % 5 == 0)
                    {
                        AssetDatabase.SaveAssets();
                    }

                    Debug.Log(
                        $"Generated stage {stageNumber:000}/{config.GeneratedStageCount}: " +
                        $"{request.Difficulty}, solutions {analysis.SolutionCount}, road {request.RoadPresetId}.");
                }

                EditorUtility.DisplayProgressBar("Rebuilding Bus Pop Stage Set", "Saving generated level sequence...", 1f);
                SaveVerifiedGeneratedSequence(savedLevels, GeneratedLevelSequencePath);
                SaveVerifiedGeneratedSequence(savedLevels, ActiveLevelSequencePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    $"Generated Bus Pop stage set rebuilt: {savedLevels.Length} verified levels saved under {GeneratedLevelDirectory}. " +
                    $"The active sequence now points to the generated set: {ActiveLevelSequencePath}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static LevelData SaveLevel(string assetName, LevelData generatedLevel)
        {
            var path = GetLevelPath(assetName);
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            generatedLevel.hideFlags = HideFlags.None;
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generatedLevel, path);
                return generatedLevel;
            }

            EditorUtility.CopySerialized(generatedLevel, existing);
            existing.hideFlags = HideFlags.None;
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

            var report = LevelValidator.Validate(level, false, GetValidationSolutionLimit(config, request));
            if (!report.HasErrors)
            {
                level.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(level);
                return true;
            }

            Debug.LogWarning(
                $"Existing generated stage {request.StageNumber:000} failed validation and will be rebuilt. " +
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
