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
        private const string StageGenerationConfigResourcePath = "Levels/StageGenerationConfig";

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

            var stageGenerationConfig = Resources.Load<StageGenerationConfig>(StageGenerationConfigResourcePath);
            if (stageGenerationConfig != null && generatedSequence.Count > stageGenerationConfig.GeneratedStageCount)
            {
                throw new BuildFailedException(
                    $"Generated level sequence contains {generatedSequence.Count} stages, which is more than StageGenerationConfig expects " +
                    $"{stageGenerationConfig.GeneratedStageCount}. Rebuild generated stages or lower the generated sequence count.");
            }

            if (stageGenerationConfig != null && generatedSequence.Count < stageGenerationConfig.GeneratedStageCount)
            {
                Debug.LogWarning(
                    $"Generated level sequence contains {generatedSequence.Count} verified stages, while StageGenerationConfig expects " +
                    $"{stageGenerationConfig.GeneratedStageCount}. The release will use verified stages first and runtime-generate later stages.");
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
            return report != null && report.summary.options.ToString().Contains("Development");
        }
    }
}
#endif
