#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BusPuzzle.EditorTools
{
    /// <summary>
    /// Regression entry point for template-backed visual silhouette checks. This is kept
    /// separate from level validity: a level can be solvable and still look unlike its name.
    /// </summary>
    public static class VehicleShapeTemplateAudit
    {
        private const string HeartTemplateDirectory =
            "Assets/BusPuzzle/Resources/ShapeTemplates/Heart";
        private const string CurrentHeartPreviewPath =
            "Assets/BusPuzzle/ShapePreview/Heart_Current.asset";
        private const string GeneratedHeartPreviewPath =
            "Assets/BusPuzzle/ShapePreview/Levels/ShapePreview_007.asset";
        private const string StageGenerationConfigPath =
            "Assets/BusPuzzle/Resources/Levels/StageGenerationConfig.asset";
        private const int FirstAutomaticHeartAuditStage = 2;
        private const int AutomaticHeartCandidateProbes = 4;
        private const int AutomaticHeartVehicleGenerationAttempts = 4;

        [MenuItem("Bus Puzzle/Shape Templates/Audit Heart Visual Silhouette")]
        public static void AuditHeartVisualSilhouette()
        {
            ValidateHeartTemplatesFromCommandLine();
        }

        public static void ValidateHeartTemplatesFromCommandLine()
        {
            var assetPaths = CollectHeartLevelAssetPaths();
            var failures = new List<string>();
            for (var index = 0; index < assetPaths.Count; index++)
            {
                AuditHeartLevel(assetPaths[index], failures);
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Heart visual silhouette audit failed for {failures.Count}/{assetPaths.Count} asset(s):\n" +
                    string.Join("\n", failures));
            }

            Debug.Log($"Heart visual silhouette audit passed for {assetPaths.Count} LevelData asset(s).");
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Generated Heart Preview")]
        public static void ValidateGeneratedHeartPreviewFromCommandLine()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(GeneratedHeartPreviewPath);
            if (level == null)
            {
                throw new InvalidOperationException(
                    $"Generated Heart preview is missing at {GeneratedHeartPreviewPath}.");
            }

            if (string.IsNullOrEmpty(level.GenerationSignature) ||
                !level.GenerationSignature.StartsWith(
                    GeneratedLevelAssetBuilder.ShapePreviewSignaturePrefix,
                    StringComparison.Ordinal) ||
                !StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "layoutVariant",
                    out var layoutVariantIndex))
            {
                throw new InvalidOperationException(
                    "Generated Heart preview is not marked as an isolated shape preview or has no layout variant.");
            }

            if (!VehicleShapeSilhouetteQuality.TryEvaluate(
                    level.DifficultyProfile,
                    layoutVariantIndex,
                    level.Buses,
                    out var metrics))
            {
                throw new InvalidOperationException(
                    "Generated Heart preview could not be evaluated with the common template engine.");
            }

            if (VehicleShapeSilhouetteQuality.TryGetFailureMessage(
                    level.DifficultyProfile,
                    layoutVariantIndex,
                    level.Buses,
                    out var failureMessage))
            {
                throw new InvalidOperationException(
                    $"Generated Heart preview failed its silhouette gate: {failureMessage}");
            }

            var report = LevelValidator.Validate(level, false);
            if (report.HasErrors)
            {
                throw new InvalidOperationException(
                    report.ToConsoleMessage("Generated Heart preview"));
            }

            Debug.Log(
                $"Generated Heart preview validation passed: {metrics.ToCompactString()}, " +
                $"vehicles {level.Buses.Count}.",
                level);
        }

        [MenuItem("Bus Puzzle/Shape Templates/Validate Automatic Positive Heart Stage")]
        public static void ValidateAutomaticPositiveHeartStageFromCommandLine()
        {
            var config = AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(StageGenerationConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Stage generation config is missing at {StageGenerationConfigPath}.");
            }

            if (!TryFindAutomaticPositiveHeartRequest(
                    config,
                    false,
                    out var lowRequest,
                    out var lowFillInterior) ||
                !TryFindAutomaticPositiveHeartRequest(
                    config,
                    true,
                    out var highRequest,
                    out var highFillInterior))
            {
                throw new InvalidOperationException(
                    $"No automatic positive-layoutVariant ShapeHeart/ShowcaseHeart request was found " +
                    $"between stage {FirstAutomaticHeartAuditStage} and " +
                    $"{config.GeneratedStageCount}.");
            }

            var existingLevelIds = CaptureLoadedLevelIds();
            try
            {
                ValidateAutomaticPositiveHeartRequest(
                    config,
                    lowRequest,
                    lowFillInterior,
                    "low-budget");
                if (highRequest.StageNumber != lowRequest.StageNumber)
                {
                    ValidateAutomaticPositiveHeartRequest(
                        config,
                        highRequest,
                        highFillInterior,
                        "high-budget");
                }
            }
            finally
            {
                DestroyNewTransientLevels(existingLevelIds);
            }
        }

        private static void ValidateAutomaticPositiveHeartRequest(
            StageGenerationConfig config,
            StageGenerationRequest request,
            bool fillInterior,
            string budgetLabel)
        {
            var lastFailure = "no candidate was produced";
            for (var candidateOffset = 0;
                 candidateOffset < AutomaticHeartCandidateProbes;
                 candidateOffset++)
            {
                // Exercise the production positive-layout path directly, but keep this
                // visual regression bounded. The release builder may probe more seeds
                // and perform its separate solution-count search after this gate passes.
                var level = LevelGenerator.CreateRuntimeStage(
                    request,
                    config.SuperHardGarageRule,
                    candidateOffset,
                    AutomaticHeartVehicleGenerationAttempts,
                    false,
                    false);
                if (level == null)
                {
                    continue;
                }

                level.SetGenerationMetadata(
                    StageGenerationSignature.Create(config, request),
                    1);
                var validationReport = LevelValidator.Validate(level, false);
                if (validationReport.HasErrors)
                {
                    lastFailure = validationReport.ToConsoleMessage(
                        $"Automatic positive Heart stage {request.StageNumber:000}");
                    if (VehicleShapeSilhouetteQuality.TryEvaluate(
                            request.Profile,
                            request.VehicleLayoutVariantIndex,
                            level.Buses,
                            out var rejectedMetrics))
                    {
                        Debug.LogWarning(
                            $"Automatic Heart {budgetLabel} probe {candidateOffset + 1}/" +
                            $"{AutomaticHeartCandidateProbes} rejected: " +
                            $"{rejectedMetrics.ToCompactString()}, greedy " +
                            $"{LevelGenerator.HasGreedyExitOrder(level.Buses)}. {lastFailure}",
                            level);
                        if (candidateOffset == 0)
                        {
                            Debug.LogWarning(FormatVehicleLayout(level.Buses), level);
                        }
                    }
                    continue;
                }

                if (!LevelGenerator.HasGreedyExitOrder(level.Buses))
                {
                    lastFailure = "the generated Heart has no complete greedy exit order";
                    continue;
                }

                if (!VehicleShapeSilhouetteQuality.TryEvaluate(
                        request.Profile,
                        request.VehicleLayoutVariantIndex,
                        level.Buses,
                        out var metrics))
                {
                    lastFailure = "the Heart quality template could not evaluate the candidate";
                    continue;
                }

                if (VehicleShapeSilhouetteQuality.TryGetFailureMessage(
                        request.Profile,
                        request.VehicleLayoutVariantIndex,
                        level.Buses,
                        out var failureMessage))
                {
                    lastFailure = failureMessage;
                    continue;
                }

                Debug.Log(
                    $"Automatic positive Heart {budgetLabel} validation passed: " +
                    $"stage {request.StageNumber:000}, layoutVariant {request.VehicleLayoutVariantIndex}, " +
                    $"{(fillInterior ? "filled" : "outline")} Heart, " +
                    $"candidate {candidateOffset + 1}/{AutomaticHeartCandidateProbes}, " +
                    $"vehicle attempts {AutomaticHeartVehicleGenerationAttempts}, " +
                    $"vehicles {level.Buses.Count}, {metrics.ToCompactString()}.",
                    level);
                return;
            }

            throw new InvalidOperationException(
                $"Automatic positive Heart {budgetLabel} stage {request.StageNumber:000} failed all " +
                $"{AutomaticHeartCandidateProbes} bounded production-layout probes. " +
                $"Last failure: {lastFailure}");
        }

        public static void ValidateBadHeartBaselineIsRejectedFromCommandLine()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(CurrentHeartPreviewPath);
            if (level == null)
            {
                throw new InvalidOperationException(
                    $"Preserved bad Heart baseline is missing at {CurrentHeartPreviewPath}.");
            }

            if (!VehicleShapeSilhouetteQuality.TryEvaluateHeart(
                    level.Buses,
                    true,
                    1,
                    1f,
                    out var metrics) ||
                !VehicleShapeSilhouetteQuality.TryGetHeartFailureMessage(
                    level.Buses,
                    true,
                    1,
                    1f,
                    out var failureMessage))
            {
                throw new InvalidOperationException(
                    "The preserved bad Heart baseline unexpectedly passed the silhouette gate.");
            }

            Debug.Log(
                $"Bad Heart baseline rejection passed: {metrics.ToCompactString()}. " +
                $"Rejected because: {failureMessage}",
                level);
        }

        private static List<string> CollectHeartLevelAssetPaths()
        {
            var paths = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { HeartTemplateDirectory });
            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (!string.IsNullOrEmpty(path) && seen.Add(path))
                {
                    paths.Add(path);
                }
            }

            if (seen.Add(GeneratedHeartPreviewPath))
            {
                paths.Add(GeneratedHeartPreviewPath);
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        private static bool TryFindAutomaticPositiveHeartRequest(
            StageGenerationConfig config,
            bool preferHighestVehicleCount,
            out StageGenerationRequest request,
            out bool fillInterior)
        {
            request = default;
            fillInterior = false;
            var found = false;
            var bestVehicleCount = preferHighestVehicleCount ? int.MinValue : int.MaxValue;
            for (var stageNumber = FirstAutomaticHeartAuditStage;
                 stageNumber <= config.GeneratedStageCount;
                 stageNumber++)
            {
                var candidate = StageGenerationPlanner.CreateRequest(config, stageNumber);
                if (!StageGenerationPlanner.IsAutomaticTemplateBackedHeartRequest(
                        candidate,
                        out var candidateFillInterior) ||
                    candidate.GarageCount > 0)
                {
                    continue;
                }

                var candidateVehicleCount = candidate.Profile != null
                    ? candidate.Profile.TargetVehicleCount
                    : int.MaxValue;
                if (found &&
                    (preferHighestVehicleCount
                        ? candidateVehicleCount <= bestVehicleCount
                        : candidateVehicleCount >= bestVehicleCount))
                {
                    continue;
                }

                request = candidate;
                fillInterior = candidateFillInterior;
                bestVehicleCount = candidateVehicleCount;
                found = true;
            }

            return found;
        }

        private static HashSet<int> CaptureLoadedLevelIds()
        {
            var ids = new HashSet<int>();
            var levels = Resources.FindObjectsOfTypeAll<LevelData>();
            for (var index = 0; index < levels.Length; index++)
            {
                if (levels[index] != null)
                {
                    ids.Add(levels[index].GetInstanceID());
                }
            }

            return ids;
        }

        private static string FormatVehicleLayout(IReadOnlyList<BusDefinition> buses)
        {
            var builder = new StringBuilder("Automatic Heart vehicle layout:");
            if (buses == null)
            {
                return builder.Append(" <null>").ToString();
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                builder.Append('\n')
                    .Append(index)
                    .Append(": ")
                    .Append(bus.Size)
                    .Append(" @ ")
                    .Append(bus.GridPosition.x + bus.PositionOffsetCells.x)
                    .Append(", ")
                    .Append(bus.GridPosition.y + bus.PositionOffsetCells.y)
                    .Append(" yaw ")
                    .Append(bus.YawDegrees);
            }

            return builder.ToString();
        }

        private static void DestroyNewTransientLevels(HashSet<int> existingLevelIds)
        {
            var levels = Resources.FindObjectsOfTypeAll<LevelData>();
            for (var index = 0; index < levels.Length; index++)
            {
                var level = levels[index];
                if (level != null &&
                    !existingLevelIds.Contains(level.GetInstanceID()) &&
                    (level.hideFlags & HideFlags.DontSave) != 0)
                {
                    UnityEngine.Object.DestroyImmediate(level);
                }
            }
        }

        private static void AuditHeartLevel(string assetPath, List<string> failures)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (level == null)
            {
                failures.Add($"{assetPath}: LevelData is missing or unreadable.");
                return;
            }

            // Only the named double-outline reference uses a contour-band target. All filled
            // variants (including garages/mystery/color mixes) use the solid target.
            var fillInterior = assetPath.IndexOf("DoubleOutline", StringComparison.OrdinalIgnoreCase) < 0;
            var thickness = fillInterior ? 1 : 2;
            if (!VehicleShapeSilhouetteQuality.TryEvaluateHeart(
                    level.Buses,
                    fillInterior,
                    thickness,
                    1f,
                    out var metrics))
            {
                failures.Add($"{assetPath}: common Heart template could not be resolved.");
                return;
            }

            if (VehicleShapeSilhouetteQuality.TryGetHeartFailureMessage(
                    level.Buses,
                    fillInterior,
                    thickness,
                    1f,
                    out var failureMessage))
            {
                failures.Add($"{assetPath}: {failureMessage}");
                Debug.LogError($"{assetPath}: {failureMessage}", level);
                return;
            }

            Debug.Log(
                $"Heart visual silhouette audit passed: {assetPath}. {metrics.ToCompactString()}",
                level);
        }
    }
}
#endif
