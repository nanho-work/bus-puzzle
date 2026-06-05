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
            var generatedLevels = new LevelData[config.GeneratedStageCount];
            for (var stageNumber = 1; stageNumber <= config.GeneratedStageCount; stageNumber++)
            {
                var request = StageGenerationPlanner.CreateRequest(config, stageNumber);
                if (!StageCandidateBuilder.TryBuildVerifiedStageCandidate(
                    config,
                    request,
                    out var generatedLevel,
                    out var report,
                    out var analysis))
                {
                    Debug.LogError(
                        $"Generated stage set rebuild aborted at stage {stageNumber:000}. " +
                        $"No verified candidate found after {config.CandidateAttemptsPerStage} attempts. " +
                        $"Last solution count: {analysis.SolutionCount}, target range: {request.MinSolutionCount}-{request.MaxSolutionCount}. " +
                        $"{CreateReportMessage(generatedLevel, report)}");
                    return;
                }

                generatedLevels[stageNumber - 1] = generatedLevel;
            }

            Directory.CreateDirectory(GeneratedLevelDirectory);
            var savedLevels = new LevelData[generatedLevels.Length];
            for (var index = 0; index < generatedLevels.Length; index++)
            {
                savedLevels[index] = SaveLevel($"Level_{index + 1:000}", generatedLevels[index]);
            }

            SaveVerifiedGeneratedSequence(savedLevels, GeneratedLevelSequencePath);
            SaveVerifiedGeneratedSequence(savedLevels, ActiveLevelSequencePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Generated Bus Puzzle stage set rebuilt: {savedLevels.Length} verified levels saved under {GeneratedLevelDirectory}. " +
                $"The active sequence now points to the generated set: {ActiveLevelSequencePath}.");
        }

        private static LevelData SaveLevel(string assetName, LevelData generatedLevel)
        {
            var path = $"{GeneratedLevelDirectory}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generatedLevel, path);
                return generatedLevel;
            }

            EditorUtility.CopySerialized(generatedLevel, existing);
            EditorUtility.SetDirty(existing);
            return existing;
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
