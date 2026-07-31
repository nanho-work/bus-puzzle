#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BusPuzzle.EditorTools
{
    public static class LevelAssetValidatorMenu
    {
        private const string LevelDirectory = "Assets/BusPuzzle/Resources/Levels";
        private const string GeneratedLevelSequencePath = LevelDirectory + "/Generated/GeneratedLevelSequence.asset";

        [MenuItem("Bus Puzzle/Levels/Validate Level Assets")]
        public static void ValidateLevelAssets()
        {
            ValidateReleaseGeneratedLevelSequence();
        }

        [MenuItem("Bus Puzzle/Levels/Validate Release Generated Level Sequence")]
        public static void ValidateReleaseGeneratedLevelSequence()
        {
            ReleaseContentBuildValidator.ValidateReleaseContentOrThrow();
            var sequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(GeneratedLevelSequencePath);
            if (sequence == null || sequence.StaticLevels.Count == 0)
            {
                var message = $"Release generated level sequence is missing or empty: {GeneratedLevelSequencePath}";
                Debug.LogError(message, sequence);
                if (Application.isBatchMode)
                {
                    throw new BuildFailedException(message);
                }

                return;
            }

            ValidateLevels(sequence.StaticLevels, "release generated level sequence");
        }

        [MenuItem("Bus Puzzle/Levels/Validate All Level Assets")]
        public static void ValidateAllLevelAssets()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelDirectory });
            var levels = new LevelData[guids.Length];
            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                levels[index] = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            }

            ValidateLevels(levels, "all level assets");
        }

        private static void ValidateLevels(System.Collections.Generic.IReadOnlyList<LevelData> levels, string sourceName)
        {
            var issueCount = 0;
            var errorAssetCount = 0;

            for (var index = 0; index < levels.Count; index++)
            {
                var level = levels[index];
                var report = LevelValidator.Validate(level);
                if (!report.HasIssues)
                {
                    continue;
                }

                issueCount += report.Issues.Count;
                if (report.HasErrors)
                {
                    errorAssetCount++;
                    Debug.LogError(report.ToConsoleMessage(GetLevelDisplayName(level, sourceName, index)), level);
                    continue;
                }

                Debug.LogWarning(report.ToConsoleMessage(GetLevelDisplayName(level, sourceName, index)), level);
            }

            if (issueCount == 0)
            {
                Debug.Log($"All Bus Puzzle {sourceName} passed validation. Checked {levels.Count} level assets.");
                return;
            }

            var summary =
                $"Bus Puzzle {sourceName} validation finished with {issueCount} issue(s) across {levels.Count} level assets; {errorAssetCount} asset(s) have errors.";
            if (errorAssetCount > 0)
            {
                Debug.LogError(summary);
                if (Application.isBatchMode)
                {
                    throw new BuildFailedException(summary);
                }

                return;
            }

            Debug.LogWarning(summary);
        }

        private static string GetLevelDisplayName(LevelData level, string sourceName, int index)
        {
            return level != null ? level.LevelName : $"{sourceName} #{index + 1}";
        }
    }
}
#endif
