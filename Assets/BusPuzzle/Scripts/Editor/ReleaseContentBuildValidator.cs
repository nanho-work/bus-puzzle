#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class ReleaseContentBuildValidator : IPreprocessBuildWithReport
    {
        private const string GeneratedLevelSequenceResourcePath = "Levels/Generated/GeneratedLevelSequence";

        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || IsDevelopmentBuild(report))
            {
                return;
            }

            var generatedSequence = Resources.Load<LevelSequence>(GeneratedLevelSequenceResourcePath);
            if (generatedSequence == null)
            {
                throw new BuildFailedException(
                    "Verified generated level sequence is missing. " +
                    "Run Bus Puzzle/Levels/Rebuild Generated Stage Set before release builds.");
            }

            if (!generatedSequence.IsVerifiedGeneratedSet)
            {
                throw new BuildFailedException("Generated level sequence is not marked as a verified generated set.");
            }

            if (generatedSequence.Count <= 0)
            {
                throw new BuildFailedException("Generated level sequence contains no stages.");
            }

            for (var index = 0; index < generatedSequence.Count; index++)
            {
                var level = generatedSequence.GetLevel(index);
                var reportResult = LevelValidator.Validate(level, true);
                if (!reportResult.HasErrors)
                {
                    continue;
                }

                throw new BuildFailedException(reportResult.ToConsoleMessage(level != null ? level.LevelName : $"Stage {index + 1:00}"));
            }
        }

        private static bool IsDevelopmentBuild(BuildReport report)
        {
            return (report.summary.options & BuildOptions.Development) != 0;
        }
    }
}
#endif
