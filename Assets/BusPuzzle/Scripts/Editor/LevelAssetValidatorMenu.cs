#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BusPuzzle.EditorTools
{
    public static class LevelAssetValidatorMenu
    {
        private const string LevelDirectory = "Assets/BusPuzzle/Resources/Levels";

        [MenuItem("Bus Puzzle/Levels/Validate Level Assets")]
        public static void ValidateLevelAssets()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelDirectory });
            var issueCount = 0;
            var errorAssetCount = 0;

            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                var report = LevelValidator.Validate(level);
                if (!report.HasIssues)
                {
                    continue;
                }

                issueCount += report.Issues.Count;
                if (report.HasErrors)
                {
                    errorAssetCount++;
                    Debug.LogError(report.ToConsoleMessage(level != null ? level.LevelName : path), level);
                    continue;
                }

                Debug.LogWarning(report.ToConsoleMessage(level.LevelName), level);
            }

            if (issueCount == 0)
            {
                Debug.Log($"All Bus Puzzle level assets passed validation. Checked {guids.Length} level assets.");
                return;
            }

            Debug.LogWarning(
                $"Bus Puzzle level validation finished with {issueCount} issue(s) across {guids.Length} level assets; {errorAssetCount} asset(s) have errors.");
        }
    }
}
#endif
