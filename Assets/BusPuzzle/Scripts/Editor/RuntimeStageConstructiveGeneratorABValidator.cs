#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BusPuzzle.EditorTools
{
    /// <summary>
    /// Comparison-only gate for the experimental SuperHard + Garage
    /// constructive generator. This validator never changes production
    /// routing. Non-applicable stages are measured once with the production
    /// candidate and carried through both arms; applicable candidates use the
    /// same request, candidate offset and probe count in both arms.
    /// </summary>
    public static class RuntimeStageConstructiveGeneratorABValidator
    {
        private const string GeneratedSequencePath =
            "Assets/BusPuzzle/Resources/Levels/Generated/GeneratedLevelSequence.asset";
        private const string StageGenerationConfigPath =
            "Assets/BusPuzzle/Resources/Levels/StageGenerationConfig.asset";
        private const int DefaultStartStage = 201;
        private const int DefaultEndStage = 1000;
        private const int MaximumRuntimeCandidateAttempts = 4;
        private const int MaximumRuntimeVehicleGenerationAttempts = 6;
        private const int CandidateSeedStride = 7919;
        private const int GarageSolutionNodeLimit = 2048;
        private const int RegularSolutionNodeLimit = 8192;
        private const int MaximumSolutionProofCount = 256;
        private const int MaximumAcceptedSolutionDistance = 2;
        private const float MinimumVehicleTargetRatio = 0.75f;
        private const int MaximumVehicleTargetDelta = 8;
        private const float DifficultyComparisonEpsilon = 0.0001f;
        private const float MaximumOpeningMoveRatioIncrease = 0f;
        private const double MaximumCancellationMilliseconds = 250d;
        private const long AllocationSlackBytes = 4L * 1024L * 1024L;
        private const double AllocationRatioCeiling = 2d;

        private const int HistoricalTestedStageCount = 800;
        private const int HistoricalProceduralStageCount = 799;
        private const int HistoricalFallbackStageCount = 1;
        private const long HistoricalP99Milliseconds = 3774;
        private const long HistoricalMaximumMilliseconds = 6844;
        private const int HistoricalClearDeadlineFallbackCount = 16;

        private const string StagesArgument =
            "-busPuzzleConstructiveStages";
        private const string StartArgument =
            "-busPuzzleConstructiveStart";
        private const string EndArgument =
            "-busPuzzleConstructiveEnd";
        private const string OutputArgument =
            "-busPuzzleConstructiveOutput";
        private const string LegacyNodeLimitArgument =
            "-busPuzzleConstructiveLegacyNodeLimit";
        private const string LayoutProbeCountArgument =
            "-busPuzzleConstructiveLayoutProbeCount";

        private static readonly int[] DefaultTargetStages =
        {
            251,
            260,
            812
        };

        private static readonly HashSet<int> TargetStageSet =
            new HashSet<int>(DefaultTargetStages);

        [Serializable]
        private sealed class ComparisonSummary
        {
            public string startedAtUtc;
            public string finishedAtUtc;
            public string unityVersion;
            public string applicationVersion;
            public string stageSpecification;
            public string constructiveApi;
            public bool constructiveApiAvailable;
            public bool cancelled;
            public int requestedStageCount;
            public int applicableStageCount;
            public int passthroughStageCount;
            public int candidateCount;
            public int legacyCandidateSuccessCount;
            public int constructiveCandidateSuccessCount;
            public int legacySolvedStageCount;
            public int constructiveSolvedStageCount;
            public int constructiveTelemetryStageCount;
            public int constructiveRegularVehicleCountTotal;
            public int constructiveRegularPrefixCountTotal;
            public int constructiveRegularSuffixCountTotal;
            public double constructiveRegularPrefixRatioAverage;
            public double constructiveRegularSuffixRatioAverage;
            public int constructiveGarageDependencyTargetTotal;
            public int constructiveGarageDependencyActualTotal;
            public int constructiveGarageDependencyEvaluatedStageCount;
            public int constructiveSuffixOnlyReleasedGarageCountTotal;
            public int constructiveSuffixOnlyReleaseTargetTotal;
            public int constructiveSuffixOnlyReleaseEvaluatedStageCount;
            public int constructiveSuffixOnlyReleasePassedStageCount;
            public int recoveredStageCount;
            public int regressedStageCount;
            public int legacyValidationFailureCount;
            public int constructiveValidationFailureCount;
            public int witnessFailureCount;
            public int deterministicFailureCount;
            public int seedMismatchCount;
            public int difficultyContractFailureCount;
            public int difficultyProxyRegressionCount;
            public int shapeContractFailureCount;
            public int diversityFailureCount;
            public int constructiveTelemetryFailureCount;
            public int cancellationProbeCount;
            public int cancellationFailureCount;
            public double maximumCancellationMilliseconds;
            public int operationBudgetHitCount;
            public int allocationRegressionCount;
            public long legacyAllocatedBytes;
            public long constructiveAllocatedBytes;
            public double legacyCandidateTotalMilliseconds;
            public double constructiveCandidateTotalMilliseconds;
            public double legacyCandidateP95Milliseconds;
            public double constructiveCandidateP95Milliseconds;
            public double legacyCandidateP99Milliseconds;
            public double constructiveCandidateP99Milliseconds;
            public double legacyStageTotalMilliseconds;
            public double constructiveStageTotalMilliseconds;
            public double legacyStageP95Milliseconds;
            public double constructiveStageP95Milliseconds;
            public double legacyStageP99Milliseconds;
            public double constructiveStageP99Milliseconds;
            public double legacyStageMaximumMilliseconds;
            public double constructiveStageMaximumMilliseconds;
            public int historicalTestedStageCount;
            public int historicalProceduralStageCount;
            public int historicalFallbackStageCount;
            public long historicalP99Milliseconds;
            public long historicalMaximumMilliseconds;
            public int historicalClearDeadlineFallbackCount;
            public bool stageSuccessRateImproved;
            public bool endToEndP99Improved;
            public bool beatsHistoricalSuccessRate;
            public bool beatsHistoricalP99;
            public bool beatsHistoricalMaximum;
            public bool targetStagesPassed;
            public bool targetPerformanceGatePassed;
            public bool witnessGatePassed;
            public bool validationGatePassed;
            public bool determinismGatePassed;
            public bool sameSeedGatePassed;
            public bool difficultyGatePassed;
            public bool difficultyProxyGatePassed;
            public bool shapeGatePassed;
            public bool diversityGatePassed;
            public bool constructiveTelemetryGatePassed;
            public bool cancellationGatePassed;
            public bool operationBudgetGatePassed;
            public bool allocationGatePassed;
            public bool passed;
            public string csvPath;
            public StageDigest[] targetStages;
            public DifficultyDigest[] difficulty;
            public StageDigest[] slowestConstructiveStages;
            public StageDigest[] issues;
        }

        [Serializable]
        private sealed class StageDigest
        {
            public int stageNumber;
            public string difficulty;
            public int seed;
            public int requestedVehicles;
            public int requestedGarages;
            public bool constructiveApplicable;
            public int legacyAcceptedCandidate;
            public int constructiveAcceptedCandidate;
            public bool legacySucceeded;
            public bool constructiveSucceeded;
            public double legacyMilliseconds;
            public double constructiveMilliseconds;
            public int legacyOpeningMoves;
            public int constructiveOpeningMoves;
            public float legacyOpeningMoveRatio;
            public float constructiveOpeningMoveRatio;
            public int constructiveRegularVehicleCount;
            public int constructiveRegularPrefixCount;
            public int constructiveRegularSuffixCount;
            public float constructiveRegularPrefixRatio;
            public float constructiveRegularSuffixRatio;
            public int constructiveGarageDependencyTarget;
            public bool constructiveGarageDependencyEvaluated;
            public int constructiveGarageDependencyActual;
            public bool constructiveSuffixOnlyReleaseEvaluated;
            public int constructiveSuffixOnlyReleasedGarageCount;
            public int constructiveSuffixOnlyReleaseTarget;
            public bool constructiveSuffixOnlyReleasePassed;
            public string legacyFingerprint;
            public string constructiveFingerprint;
            public bool diversityPassed;
            public string verdict;
            public string diagnostic;
        }

        [Serializable]
        private sealed class DifficultyDigest
        {
            public string difficulty;
            public int stageCount;
            public int legacySuccessCount;
            public int constructiveSuccessCount;
            public double legacyOpeningMoveRatioAverage;
            public double constructiveOpeningMoveRatioAverage;
            public double legacyOpeningMoveRatioMedian;
            public double constructiveOpeningMoveRatioMedian;
        }

        private sealed class CandidateResult
        {
            public int StageNumber;
            public int CandidateIndex;
            public string Difficulty = string.Empty;
            public int Seed;
            public int LegacyCandidateSeed;
            public bool CandidateSeedMatched;
            public int RequestedVehicleCount;
            public int RequestedGarageCount;
            public bool ConstructiveApplicable;

            public bool LegacyCreated;
            public bool LegacySucceeded;
            public int LegacyVehicles;
            public int LegacyGarages;
            public int LegacyOpeningMoves;
            public float LegacyOpeningMoveRatio;
            public bool LegacyValidationPassed;
            public bool LegacyDifficultyContractPassed;
            public bool LegacyShapeCoveragePassed;
            public bool LegacyShapeQualityPassed;
            public bool LegacyIndependentSolvable;
            public bool LegacyIndependentHitLimit;
            public int LegacyIndependentSolutionCount;
            public int LegacyIndependentSolutionDistance;
            public double LegacyBuildMilliseconds;
            public double LegacyVerifyMilliseconds;
            public double LegacyTotalMilliseconds;
            public long LegacyAllocatedBytes;
            public string LegacyFingerprint = string.Empty;
            public string LegacyGeometryFingerprint = string.Empty;
            public bool LegacyRepeatMatched;

            public bool ConstructiveGeneratorSucceeded;
            public bool ConstructiveCreated;
            public bool ConstructiveSucceeded;
            public int ConstructiveVehicles;
            public int ConstructiveGarages;
            public int ConstructiveOpeningMoves;
            public float ConstructiveOpeningMoveRatio;
            public bool ConstructiveValidationPassed;
            public bool ConstructiveDifficultyContractPassed;
            public bool ConstructiveShapeCoveragePassed;
            public bool ConstructiveShapeQualityPassed;
            public bool ConstructiveIndependentSolvable;
            public bool ConstructiveIndependentHitLimit;
            public int ConstructiveIndependentSolutionCount;
            public int ConstructiveIndependentSolutionDistance;
            public bool ConstructiveGeneratorWitnessValidated;
            public bool ConstructiveWitnessReplayPassed;
            public int ConstructiveWitnessLength;
            public int ConstructiveCandidateSeed;
            public int ConstructiveLayoutProbeIndex;
            public int ConstructivePlacementProbeCount;
            public int ConstructivePathProbeCount;
            public int ConstructivePlacementProbeLimit;
            public int ConstructivePathProbeLimit;
            public bool ConstructiveHitOperationBudget;
            public int ConstructiveRegularVehicleCount;
            public int ConstructiveGarageVehicleCount;
            public int ConstructiveRegularPrefixCount;
            public int ConstructiveRegularSuffixCount;
            public float ConstructiveRegularPrefixRatio;
            public float ConstructiveRegularSuffixRatio;
            public int ConstructiveInitialOpeningCount;
            public int ConstructiveMaximumInitialOpeningCount;
            public int ConstructiveGarageDependencyTarget;
            public bool ConstructiveGarageDependencyEvaluated;
            public int ConstructiveGarageDependencyCount;
            public bool ConstructiveSuffixOnlyReleaseEvaluated;
            public int ConstructiveSuffixOnlyReleasedGarageCount;
            public int ConstructiveSuffixOnlyReleaseTarget;
            public bool ConstructiveSuffixOnlyReleasePassed;
            public double ConstructiveBuildMilliseconds;
            public double ConstructiveVerifyMilliseconds;
            public double ConstructiveWitnessReplayMilliseconds;
            public double ConstructiveTotalMilliseconds;
            public long ConstructiveAllocatedBytes;
            public string ConstructiveFingerprint = string.Empty;
            public string ConstructiveGeometryFingerprint = string.Empty;
            public string ConstructiveWitnessFingerprint = string.Empty;
            public bool ConstructiveRepeatMatched;

            public bool Failed;
            public string Verdict = string.Empty;
            public string Diagnostic = string.Empty;
        }

        private sealed class SideResult
        {
            public bool GeneratorSucceeded;
            public bool Created;
            public bool Succeeded;
            public int VehicleCount;
            public int GarageCount;
            public int OpeningMoves;
            public float OpeningMoveRatio;
            public bool ValidationPassed;
            public bool DifficultyContractPassed;
            public bool ShapeCoveragePassed;
            public bool ShapeQualityPassed;
            public bool IndependentSolvable;
            public bool IndependentHitLimit;
            public int IndependentSolutionCount;
            public int IndependentSolutionDistance;
            public bool GeneratorWitnessValidated;
            public bool WitnessReplayPassed;
            public int WitnessLength;
            public int CandidateSeed;
            public int LayoutProbeIndex;
            public int PlacementProbeCount;
            public int PathProbeCount;
            public int PlacementProbeLimit;
            public int PathProbeLimit;
            public bool HitOperationBudget;
            public int RegularVehicleCount;
            public int GarageVehicleCount;
            public int RegularPrefixCount;
            public int RegularSuffixCount;
            public float RegularPrefixRatio;
            public float RegularSuffixRatio;
            public int InitialOpeningCount;
            public int MaximumInitialOpeningCount;
            public int GarageDependencyTarget;
            public bool GarageDependencyEvaluated;
            public int GarageDependencyCount;
            public bool SuffixOnlyReleaseEvaluated;
            public int SuffixOnlyReleasedGarageCount;
            public int SuffixOnlyReleaseTarget;
            public bool SuffixOnlyReleasePassed;
            public double BuildMilliseconds;
            public double VerifyMilliseconds;
            public double WitnessReplayMilliseconds;
            public double TotalMilliseconds;
            public long AllocatedBytes;
            public string Fingerprint = string.Empty;
            public string GeometryFingerprint = string.Empty;
            public string WitnessFingerprint = string.Empty;
            public string Diagnostic = string.Empty;
        }

        private sealed class CancellationProbeResult
        {
            public int StageNumber;
            public bool Passed;
            public double Milliseconds;
            public string Diagnostic = string.Empty;
        }

        private sealed class Options
        {
            public int[] Stages;
            public string StageSpecification;
            public int LegacyNodeLimit;
            public int LayoutProbeCount;
            public string CsvPath;
            public string SummaryPath;
        }

        [MenuItem(
            "Bus Puzzle/Validation/Compare Constructive Generator (251, 260, 812)")]
        private static void RunTargetsFromMenu()
        {
            Run(
                CreateOptions(
                    (int[])DefaultTargetStages.Clone(),
                    "251,260,812",
                    GarageSolutionNodeLimit,
                    MaximumRuntimeVehicleGenerationAttempts,
                    null),
                true,
                false);
        }

        [MenuItem(
            "Bus Puzzle/Validation/Compare Constructive Generator (201-1000)")]
        private static void RunFullRangeFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Long-running constructive A/B",
                    "This builds and independently verifies production and " +
                    "constructive candidates for stages 201-1000. Continue?",
                    "Continue",
                    "Cancel"))
            {
                return;
            }

            Run(
                CreateOptions(
                    CreateRange(
                        DefaultStartStage,
                        DefaultEndStage),
                    "201-1000",
                    GarageSolutionNodeLimit,
                    MaximumRuntimeVehicleGenerationAttempts,
                    null),
                true,
                false);
        }

        /// <summary>
        /// Optional command-line arguments:
        /// -busPuzzleConstructiveStages 251,260,812
        /// -busPuzzleConstructiveStart 201
        /// -busPuzzleConstructiveEnd 1000
        /// -busPuzzleConstructiveLegacyNodeLimit 2048
        /// -busPuzzleConstructiveLayoutProbeCount 6
        /// -busPuzzleConstructiveOutput /path/result.csv
        /// </summary>
        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int[] stages;
            string specification;
            var explicitStages = ReadStringArgument(
                args,
                StagesArgument);
            if (!string.IsNullOrWhiteSpace(
                    explicitStages))
            {
                stages = ParseStages(
                    explicitStages);
                specification =
                    string.Join(",", stages);
            }
            else
            {
                var start = ReadIntArgument(
                    args,
                    StartArgument,
                    DefaultStartStage);
                var end = ReadIntArgument(
                    args,
                    EndArgument,
                    DefaultEndStage);
                stages = CreateRange(start, end);
                specification =
                    $"{start}-{end}";
            }

            var options = CreateOptions(
                stages,
                specification,
                ReadIntArgument(
                    args,
                    LegacyNodeLimitArgument,
                    GarageSolutionNodeLimit),
                ReadIntArgument(
                    args,
                    LayoutProbeCountArgument,
                    MaximumRuntimeVehicleGenerationAttempts),
                ReadStringArgument(
                    args,
                    OutputArgument));
            Run(options, false, true);
        }

        private static void Run(
            Options options,
            bool allowCancel,
            bool failOnGate)
        {
            var config =
                AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(
                    StageGenerationConfigPath);
            var releaseSequence =
                AssetDatabase.LoadAssetAtPath<LevelSequence>(
                    GeneratedSequencePath);
            if (config == null ||
                releaseSequence == null)
            {
                throw new BuildFailedException(
                    "Constructive A/B requires StageGenerationConfig and the release sequence.");
            }

            var constructiveApiAvailable =
                typeof(SuperHardGarageConstructiveGenerator)
                    .GetMethod(
                        "TryGenerateForComparison",
                        BindingFlags.Public |
                        BindingFlags.Static) != null;
            var releaseGeometry =
                CreateReleaseGeometryFingerprints(
                    releaseSequence);
            var legacyGeometryOwners =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            var constructiveGeometryOwners =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            var results =
                new List<CandidateResult>();
            var stages =
                new List<StageDigest>(
                    options.Stages.Length);
            var cancellationResults =
                new List<CancellationProbeResult>();
            var startedAt = DateTime.UtcNow;
            var cancelled = false;

            InitializeCsv(options.CsvPath);
            try
            {
                for (var stageIndex = 0;
                    stageIndex < options.Stages.Length;
                    stageIndex++)
                {
                    var stageNumber =
                        options.Stages[stageIndex];
                    if (allowCancel &&
                        EditorUtility.DisplayCancelableProgressBar(
                            "Constructive Generator A/B",
                            $"Stage {stageNumber} " +
                            $"({stageIndex + 1}/{options.Stages.Length})",
                            (float)stageIndex /
                            Mathf.Max(
                                1,
                                options.Stages.Length)))
                    {
                        cancelled = true;
                        break;
                    }

                    var request =
                        StageGenerationPlanner.CreateRequest(
                            config,
                            stageNumber);
                    CompareStage(
                        config,
                        request,
                        options,
                        releaseGeometry,
                        legacyGeometryOwners,
                        constructiveGeometryOwners,
                        results,
                        stages);
                }

                if (!cancelled &&
                    constructiveApiAvailable)
                {
                    RunCancellationProbes(
                        config,
                        options,
                        cancellationResults);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var summary = CreateSummary(
                startedAt,
                DateTime.UtcNow,
                options,
                constructiveApiAvailable,
                cancelled,
                results,
                stages,
                cancellationResults);
            WriteSummary(
                options.SummaryPath,
                summary);

            var message =
                $"Constructive generator A/B " +
                $"{(summary.passed ? "passed" : "failed")}: " +
                $"stages={summary.requestedStageCount}, " +
                $"legacy/constructive success=" +
                $"{summary.legacySolvedStageCount}/" +
                $"{summary.constructiveSolvedStageCount}, " +
                $"recovered={summary.recoveredStageCount}, " +
                $"regressed={summary.regressedStageCount}, " +
                $"stage P99={summary.legacyStageP99Milliseconds:0.###}/" +
                $"{summary.constructiveStageP99Milliseconds:0.###} ms, " +
                $"witness failures={summary.witnessFailureCount}, " +
                $"validation failures=" +
                $"{summary.constructiveValidationFailureCount}, " +
                $"determinism failures={summary.deterministicFailureCount}. " +
                $"CSV: {summary.csvPath}; summary: {options.SummaryPath}";

            if (!summary.passed)
            {
                Debug.LogError(message);
                if (failOnGate)
                {
                    throw new BuildFailedException(
                        message);
                }

                return;
            }

            Debug.Log(message);
        }

        private static void CompareStage(
            StageGenerationConfig config,
            StageGenerationRequest request,
            Options options,
            ISet<string> releaseGeometry,
            IDictionary<string, int> legacyGeometryOwners,
            IDictionary<string, int> constructiveGeometryOwners,
            ICollection<CandidateResult> allResults,
            ICollection<StageDigest> allStages)
        {
            var attempts = Mathf.Clamp(
                config.RuntimeCandidateAttemptsPerStage,
                1,
                MaximumRuntimeCandidateAttempts);
            var vehicleAttempts = Mathf.Clamp(
                config.RuntimeVehicleGenerationAttempts,
                1,
                MaximumRuntimeVehicleGenerationAttempts);
            var constructiveApplicable =
                request.Difficulty ==
                    LevelDifficulty.SuperHard &&
                request.GarageCount > 0;
            var stageResults =
                new List<CandidateResult>(attempts);

            for (var candidate = 0;
                candidate < attempts;
                candidate++)
            {
                var result = CompareCandidate(
                    config,
                    request,
                    candidate,
                    vehicleAttempts,
                    options,
                    constructiveApplicable);
                stageResults.Add(result);
                allResults.Add(result);
                AppendCsv(
                    options.CsvPath,
                    result);
            }

            var legacyAccepted =
                FindAcceptedCandidate(
                    stageResults,
                    false);
            var constructiveAccepted =
                constructiveApplicable
                    ? FindAcceptedCandidate(
                        stageResults,
                        true)
                    : legacyAccepted;
            var legacySuccess =
                legacyAccepted >= 0;
            var constructiveSuccess =
                constructiveAccepted >= 0;
            var legacyTime =
                CalculateStageTime(
                    stageResults,
                    legacyAccepted,
                    false);
            var constructiveTime =
                constructiveApplicable
                    ? CalculateStageTime(
                        stageResults,
                        constructiveAccepted,
                        true)
                    : legacyTime;
            var legacyAcceptedResult =
                legacyAccepted >= 0
                    ? stageResults[legacyAccepted]
                    : null;
            var constructiveAcceptedResult =
                constructiveAccepted >= 0
                    ? stageResults[
                        constructiveAccepted]
                    : null;
            var diversityPassed = true;
            var diversityDiagnostic =
                string.Empty;

            if (legacyAcceptedResult != null)
            {
                RegisterGeometry(
                    request.StageNumber,
                    legacyAcceptedResult
                        .LegacyGeometryFingerprint,
                    null,
                    legacyGeometryOwners,
                    out _);
            }

            if (constructiveAcceptedResult != null)
            {
                var constructiveGeometry =
                    constructiveApplicable
                        ? constructiveAcceptedResult
                            .ConstructiveGeometryFingerprint
                        : constructiveAcceptedResult
                            .LegacyGeometryFingerprint;
                diversityPassed =
                    RegisterGeometry(
                        request.StageNumber,
                        constructiveGeometry,
                        releaseGeometry,
                        constructiveGeometryOwners,
                        out diversityDiagnostic);
            }

            var verdict =
                !constructiveSuccess
                    ? legacySuccess
                        ? "constructive_regressed"
                        : "both_failed"
                    : !legacySuccess
                        ? "constructive_recovered"
                        : constructiveApplicable
                            ? "both_succeeded"
                            : "passthrough_succeeded";
            if (!diversityPassed)
            {
                verdict =
                    "constructive_diversity_failure";
            }

            allStages.Add(
                new StageDigest
                {
                    stageNumber =
                        request.StageNumber,
                    difficulty =
                        request.Difficulty.ToString(),
                    seed = request.Seed,
                    requestedVehicles =
                        request.Profile != null
                            ? request.Profile
                                .TargetVehicleCount
                            : 0,
                    requestedGarages =
                        request.GarageCount,
                    constructiveApplicable =
                        constructiveApplicable,
                    legacyAcceptedCandidate =
                        legacyAccepted,
                    constructiveAcceptedCandidate =
                        constructiveAccepted,
                    legacySucceeded =
                        legacySuccess,
                    constructiveSucceeded =
                        constructiveSuccess,
                    legacyMilliseconds =
                        legacyTime,
                    constructiveMilliseconds =
                        constructiveTime,
                    legacyOpeningMoves =
                        legacyAcceptedResult != null
                            ? legacyAcceptedResult
                                .LegacyOpeningMoves
                            : 0,
                    constructiveOpeningMoves =
                        constructiveAcceptedResult != null
                            ? constructiveApplicable
                                ? constructiveAcceptedResult
                                    .ConstructiveOpeningMoves
                                : constructiveAcceptedResult
                                    .LegacyOpeningMoves
                            : 0,
                    legacyOpeningMoveRatio =
                        legacyAcceptedResult != null
                            ? legacyAcceptedResult
                                .LegacyOpeningMoveRatio
                            : 0f,
                    constructiveOpeningMoveRatio =
                        constructiveAcceptedResult != null
                            ? constructiveApplicable
                                ? constructiveAcceptedResult
                                    .ConstructiveOpeningMoveRatio
                                : constructiveAcceptedResult
                                    .LegacyOpeningMoveRatio
                            : 0f,
                    constructiveRegularVehicleCount =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveRegularVehicleCount
                            : 0,
                    constructiveRegularPrefixCount =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveRegularPrefixCount
                            : 0,
                    constructiveRegularSuffixCount =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveRegularSuffixCount
                            : 0,
                    constructiveRegularPrefixRatio =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveRegularPrefixRatio
                            : 0f,
                    constructiveRegularSuffixRatio =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveRegularSuffixRatio
                            : 0f,
                    constructiveGarageDependencyTarget =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveGarageDependencyTarget
                            : 0,
                    constructiveGarageDependencyEvaluated =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null &&
                        constructiveAcceptedResult
                            .ConstructiveGarageDependencyEvaluated,
                    constructiveGarageDependencyActual =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveGarageDependencyCount
                            : 0,
                    constructiveSuffixOnlyReleaseEvaluated =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null &&
                        constructiveAcceptedResult
                            .ConstructiveSuffixOnlyReleaseEvaluated,
                    constructiveSuffixOnlyReleasedGarageCount =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveSuffixOnlyReleasedGarageCount
                            : 0,
                    constructiveSuffixOnlyReleaseTarget =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null
                            ? constructiveAcceptedResult
                                .ConstructiveSuffixOnlyReleaseTarget
                            : 0,
                    constructiveSuffixOnlyReleasePassed =
                        constructiveApplicable &&
                        constructiveAcceptedResult != null &&
                        constructiveAcceptedResult
                            .ConstructiveSuffixOnlyReleasePassed,
                    legacyFingerprint =
                        legacyAcceptedResult != null
                            ? legacyAcceptedResult
                                .LegacyFingerprint
                            : string.Empty,
                    constructiveFingerprint =
                        constructiveAcceptedResult != null
                            ? constructiveApplicable
                                ? constructiveAcceptedResult
                                    .ConstructiveFingerprint
                                : constructiveAcceptedResult
                                    .LegacyFingerprint
                            : string.Empty,
                    diversityPassed =
                        diversityPassed,
                    verdict = verdict,
                    diagnostic =
                        diversityDiagnostic
                });
        }

        private static CandidateResult CompareCandidate(
            StageGenerationConfig config,
            StageGenerationRequest request,
            int candidate,
            int vehicleAttempts,
            Options options,
            bool constructiveApplicable)
        {
            var result = new CandidateResult
            {
                StageNumber =
                    request.StageNumber,
                CandidateIndex = candidate,
                Difficulty =
                    request.Difficulty.ToString(),
                Seed = request.Seed,
                LegacyCandidateSeed =
                    unchecked(
                        request.Seed +
                        candidate *
                        CandidateSeedStride),
                RequestedVehicleCount =
                    request.Profile != null
                        ? request.Profile
                            .TargetVehicleCount
                        : 0,
                RequestedGarageCount =
                    request.GarageCount,
                ConstructiveApplicable =
                    constructiveApplicable,
                LegacyRepeatMatched = true,
                ConstructiveRepeatMatched = true
            };

            var legacyRunsFirst =
                ((request.StageNumber +
                    candidate) & 1) == 0;
            SideResult legacyWarmup;
            SideResult constructiveWarmup;
            if (constructiveApplicable &&
                legacyRunsFirst)
            {
                constructiveWarmup =
                    EvaluateConstructive(
                        request,
                        candidate,
                        options.LayoutProbeCount,
                        options.LegacyNodeLimit,
                        false);
                legacyWarmup = EvaluateLegacy(
                    config,
                    request,
                    candidate,
                    vehicleAttempts,
                    options.LegacyNodeLimit,
                    false);
            }
            else
            {
                legacyWarmup = EvaluateLegacy(
                    config,
                    request,
                    candidate,
                    vehicleAttempts,
                    options.LegacyNodeLimit,
                    false);
                constructiveWarmup =
                    constructiveApplicable
                        ? EvaluateConstructive(
                            request,
                            candidate,
                            options.LayoutProbeCount,
                            options.LegacyNodeLimit,
                            false)
                        : CloneSide(
                            legacyWarmup);
            }

            SideResult legacy;
            SideResult constructive;
            if (legacyRunsFirst)
            {
                legacy = EvaluateLegacy(
                    config,
                    request,
                    candidate,
                    vehicleAttempts,
                    options.LegacyNodeLimit,
                    true);
                constructive =
                    constructiveApplicable
                        ? EvaluateConstructive(
                            request,
                            candidate,
                            options.LayoutProbeCount,
                            options.LegacyNodeLimit,
                            true)
                        : CloneSide(legacy);
            }
            else
            {
                constructive =
                    constructiveApplicable
                        ? EvaluateConstructive(
                            request,
                            candidate,
                            options.LayoutProbeCount,
                            options.LegacyNodeLimit,
                            true)
                        : null;
                legacy = EvaluateLegacy(
                    config,
                    request,
                    candidate,
                    vehicleAttempts,
                    options.LegacyNodeLimit,
                    true);
                if (!constructiveApplicable)
                {
                    constructive =
                        CloneSide(legacy);
                }
            }

            result.LegacyRepeatMatched =
                SideDeterminismMatches(
                    legacy,
                    legacyWarmup,
                    false);

            if (constructiveApplicable)
            {
                result.ConstructiveRepeatMatched =
                    SideDeterminismMatches(
                        constructive,
                        constructiveWarmup,
                        true);
            }

            CopyLegacy(legacy, result);
            CopyConstructive(
                constructive,
                result);
            result.CandidateSeedMatched =
                !constructiveApplicable ||
                result.ConstructiveCandidateSeed ==
                    result.LegacyCandidateSeed;
            ClassifyCandidate(result);
            return result;
        }

        private static SideResult EvaluateLegacy(
            StageGenerationConfig config,
            StageGenerationRequest request,
            int candidate,
            int vehicleAttempts,
            int nodeLimit,
            bool fullValidation)
        {
            var result = new SideResult();
            LevelData level = null;
            try
            {
                var allocationBefore =
                    GC.GetAllocatedBytesForCurrentThread();
                var watch =
                    Stopwatch.StartNew();
                level =
                    LevelGenerator.CreateRuntimeStage(
                        request,
                        config.SuperHardGarageRule,
                        candidate,
                        vehicleAttempts,
                        false,
                        false);
                watch.Stop();
                result.AllocatedBytes =
                    Math.Max(
                        0L,
                        GC.GetAllocatedBytesForCurrentThread() -
                        allocationBefore);
                result.BuildMilliseconds =
                    ToMilliseconds(
                        watch.ElapsedTicks);
                result.TotalMilliseconds =
                    result.BuildMilliseconds;
                result.GeneratorSucceeded =
                    level != null;
                EvaluateLevel(
                    request,
                    level,
                    Array.Empty<
                        SuperHardGarageConstructiveWitnessStep>(),
                    false,
                    nodeLimit,
                    fullValidation,
                    result);
                return result;
            }
            catch (Exception exception)
            {
                result.Diagnostic =
                    exception.ToString();
                return result;
            }
            finally
            {
                DestroyLevel(level);
            }
        }

        private static SideResult EvaluateConstructive(
            StageGenerationRequest request,
            int candidate,
            int layoutProbeCount,
            int nodeLimit,
            bool fullValidation)
        {
            var result = new SideResult();
            LevelData level = null;
            try
            {
                var allocationBefore =
                    GC.GetAllocatedBytesForCurrentThread();
                var watch =
                    Stopwatch.StartNew();
                var generated =
                    SuperHardGarageConstructiveGenerator
                        .TryGenerateForComparison(
                            request,
                            candidate,
                            layoutProbeCount,
                            CancellationToken.None,
                            out var generation);
                watch.Stop();
                result.AllocatedBytes =
                    Math.Max(
                        0L,
                        GC.GetAllocatedBytesForCurrentThread() -
                        allocationBefore);
                result.BuildMilliseconds =
                    ToMilliseconds(
                        watch.ElapsedTicks);
                result.TotalMilliseconds =
                    result.BuildMilliseconds;
                result.GeneratorSucceeded =
                    generated &&
                    generation.Succeeded;
                result.GeneratorWitnessValidated =
                    generation.WitnessValidated;
                result.CandidateSeed =
                    generation.CandidateSeed;
                result.LayoutProbeIndex =
                    generation.LayoutProbeIndex;
                result.PlacementProbeCount =
                    generation.PlacementProbeCount;
                result.PathProbeCount =
                    generation.PathProbeCount;
                result.PlacementProbeLimit =
                    generation.PlacementProbeLimit;
                result.PathProbeLimit =
                    generation.PathProbeLimit;
                result.HitOperationBudget =
                    generation.HitOperationBudget;
                result.RegularVehicleCount =
                    generation.RegularVehicleCount;
                result.GarageVehicleCount =
                    generation.GarageVehicleCount;
                result.RegularPrefixCount =
                    generation.RegularPrefixCount;
                result.RegularSuffixCount =
                    Mathf.Max(
                        0,
                        result.RegularVehicleCount -
                        result.RegularPrefixCount);
                result.RegularPrefixRatio =
                    result.RegularVehicleCount > 0
                        ? result.RegularPrefixCount /
                          (float)result.RegularVehicleCount
                        : 0f;
                result.RegularSuffixRatio =
                    result.RegularVehicleCount > 0
                        ? result.RegularSuffixCount /
                          (float)result.RegularVehicleCount
                        : 0f;
                result.InitialOpeningCount =
                    generation.InitialOpeningCount;
                result.MaximumInitialOpeningCount =
                    generation.MaximumInitialOpeningCount;
                result.GarageDependencyTarget =
                    generation.GarageDependencyTarget;
                result.GarageDependencyEvaluated =
                    generation.GarageDependencyEvaluated;
                result.GarageDependencyCount =
                    generation.GarageDependencyCount;
                result.SuffixOnlyReleaseEvaluated =
                    generation.SuffixOnlyReleaseEvaluated;
                result.SuffixOnlyReleasedGarageCount =
                    generation
                        .SuffixOnlyReleasedGarageCount;
                result.SuffixOnlyReleaseTarget =
                    generation.SuffixOnlyReleaseTarget;
                result.SuffixOnlyReleasePassed =
                    result.SuffixOnlyReleaseEvaluated &&
                    result.SuffixOnlyReleaseTarget > 0 &&
                    result.SuffixOnlyReleasedGarageCount ==
                        result.SuffixOnlyReleaseTarget;
                result.Diagnostic =
                    generation.Diagnostic ??
                    string.Empty;
                level = generation.Level;
                EvaluateLevel(
                    request,
                    level,
                    generation.Witness,
                    true,
                    nodeLimit,
                    fullValidation,
                    result);
                return result;
            }
            catch (Exception exception)
            {
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        exception.ToString());
                return result;
            }
            finally
            {
                DestroyLevel(level);
            }
        }

        private static void EvaluateLevel(
            StageGenerationRequest request,
            LevelData level,
            IReadOnlyList<
                SuperHardGarageConstructiveWitnessStep>
                witness,
            bool requireWitness,
            int requestedNodeLimit,
            bool fullValidation,
            SideResult result)
        {
            if (level == null)
            {
                result.Created = false;
                result.Succeeded = false;
                return;
            }

            result.Created = true;
            result.VehicleCount =
                level.AllVehicles != null
                    ? level.AllVehicles.Count
                    : 0;
            result.GarageCount =
                level.Garages != null
                    ? level.Garages.Count
                    : 0;
            result.Fingerprint =
                CreateContentFingerprint(level);
            result.GeometryFingerprint =
                CreateGeometryFingerprint(level);
            result.WitnessLength =
                witness != null
                    ? witness.Count
                    : 0;
            result.WitnessFingerprint =
                CreateWitnessFingerprint(
                    witness);
            result.OpeningMoves =
                CountOpeningMoves(level);
            var startingSlots =
                (level.Buses != null
                    ? level.Buses.Count
                    : 0) +
                result.GarageCount;
            result.OpeningMoveRatio =
                startingSlots > 0
                    ? result.OpeningMoves /
                        (float)startingSlots
                    : 0f;

            if (!fullValidation)
            {
                return;
            }

            var verifyWatch =
                Stopwatch.StartNew();
            try
            {
                var report =
                    LevelValidator.Validate(
                        level,
                        false);
                result.ValidationPassed =
                    report != null &&
                    !report.HasErrors;
                if (!result.ValidationPassed)
                {
                    result.Diagnostic =
                        AppendDiagnostic(
                            result.Diagnostic,
                            report != null
                                ? report.ToConsoleMessage(
                                    level.LevelName)
                                : "LevelValidator returned no report.");
                }
            }
            catch (Exception exception)
            {
                result.ValidationPassed = false;
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        $"Validation exception: {exception}");
            }

            result.DifficultyContractPassed =
                ValidateDifficultyContract(
                    request,
                    level,
                    out var difficultyDiagnostic);
            result.Diagnostic =
                AppendDiagnostic(
                    result.Diagnostic,
                    difficultyDiagnostic);
            result.ShapeCoveragePassed =
                ShapeLibraryVehicleCoverage.IsSatisfied(
                    request.Profile,
                    request.VehicleLayoutVariantIndex,
                    level.Buses != null
                        ? level.Buses.Count
                        : 0);
            result.ShapeQualityPassed =
                InvokeShapeQuality(
                    request,
                    level,
                    out var shapeDiagnostic);
            result.Diagnostic =
                AppendDiagnostic(
                    result.Diagnostic,
                    shapeDiagnostic);

            if (requireWitness)
            {
                var witnessDiagnostic =
                    string.Empty;
                var replayWatch =
                    Stopwatch.StartNew();
                result.WitnessReplayPassed =
                    result.GeneratorWitnessValidated &&
                    result.WitnessLength ==
                        result.VehicleCount &&
                    ReplayWitness(
                        level,
                        witness,
                        out witnessDiagnostic);
                replayWatch.Stop();
                result.WitnessReplayMilliseconds =
                    ToMilliseconds(
                        replayWatch.ElapsedTicks);
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        witnessDiagnostic);
            }
            else
            {
                result.WitnessReplayPassed = true;
            }

            var nodeLimit =
                result.GarageCount > 0
                    ? Mathf.Max(
                        1,
                        requestedNodeLimit)
                    : RegularSolutionNodeLimit;
            var proofLimit =
                Mathf.Clamp(
                    request.MinSolutionCount -
                    MaximumAcceptedSolutionDistance,
                    1,
                    MaximumSolutionProofCount);
            StageSolutionAnalysis analysis;
            try
            {
                analysis =
                    StageSolutionAnalyzer.Analyze(
                        level.Buses,
                        level.Garages,
                        proofLimit,
                        nodeLimit);
            }
            catch (Exception exception)
            {
                verifyWatch.Stop();
                result.VerifyMilliseconds =
                    ToMilliseconds(
                        verifyWatch.ElapsedTicks);
                result.TotalMilliseconds =
                    result.BuildMilliseconds +
                    result.VerifyMilliseconds;
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        $"Independent solution proof failed: " +
                        $"{Unwrap(exception)}");
                result.Succeeded = false;
                return;
            }

            result.IndependentSolutionCount =
                analysis.SolutionCount;
            result.IndependentSolutionDistance =
                GetSolutionRangeDistance(
                    request,
                    analysis);
            verifyWatch.Stop();
            result.VerifyMilliseconds =
                ToMilliseconds(
                    verifyWatch.ElapsedTicks);
            result.TotalMilliseconds =
                result.BuildMilliseconds +
                result.VerifyMilliseconds;
            result.IndependentSolvable =
                analysis.IsSolvable &&
                result.IndependentSolutionDistance <=
                    MaximumAcceptedSolutionDistance;
            result.IndependentHitLimit =
                analysis.HitLimit;
            if (!result.IndependentSolvable)
            {
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        $"Independent solution proof rejected: " +
                        $"solvable={analysis.IsSolvable}, " +
                        $"solutions={analysis.SolutionCount}, " +
                        $"requested={request.MinSolutionCount}-" +
                        $"{request.MaxSolutionCount}, " +
                        $"distance={result.IndependentSolutionDistance}, " +
                        $"maximumDistance=" +
                        $"{MaximumAcceptedSolutionDistance}, " +
                        $"proofLimit={proofLimit}, " +
                        $"nodeLimit={nodeLimit}, " +
                        $"hitLimit={analysis.HitLimit}.");
            }
            result.Succeeded =
                result.GeneratorSucceeded &&
                result.ValidationPassed &&
                result.DifficultyContractPassed &&
                result.ShapeCoveragePassed &&
                result.ShapeQualityPassed &&
                result.IndependentSolvable &&
                (!requireWitness ||
                 result.WitnessReplayPassed);
        }

        private static int GetSolutionRangeDistance(
            StageGenerationRequest request,
            StageSolutionAnalysis analysis)
        {
            if (!analysis.IsSolvable)
            {
                return int.MaxValue;
            }

            if (analysis.SolutionCount <
                request.MinSolutionCount)
            {
                return request.MinSolutionCount -
                    analysis.SolutionCount;
            }

            if (analysis.SolutionCount >
                request.MaxSolutionCount)
            {
                return analysis.SolutionCount -
                    request.MaxSolutionCount;
            }

            return 0;
        }

        private static bool ValidateDifficultyContract(
            StageGenerationRequest request,
            LevelData level,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            var expected =
                request.Profile ??
                LevelDifficultyProfile.DefaultFor(
                    request.Difficulty);
            var actual =
                level.DifficultyProfile ??
                LevelDifficultyProfile.DefaultFor(
                    LevelDifficulty.Normal);
            var actualVehicles =
                level.AllVehicles != null
                    ? level.AllVehicles.Count
                    : 0;
            var minimumVehicles =
                Mathf.CeilToInt(
                    expected.TargetVehicleCount *
                    MinimumVehicleTargetRatio);
            var actualGarages =
                level.Garages != null
                    ? level.Garages.Count
                    : 0;
            var expectsGarages =
                request.GarageCount > 0 ||
                (request.Modifiers &
                 StageModifierFlags.Garages) != 0;
            var effectiveRotaryCapacity =
                RotaryCapacityPolicy.Resolve(
                    level,
                    level.RoadPreset);
            var usesExactRotaryCapacity =
                RotaryCapacityPolicy
                    .UsesExactRuntimeCapacity(
                        level);
            var expectsMystery =
                request.MysteryVehicleProfile.Enabled ||
                (request.Modifiers &
                 (StageModifierFlags.MysteryVehicles |
                  StageModifierFlags.LightMysteryVehicles)) != 0;
            var hasMystery =
                level.AllVehicles != null &&
                level.AllVehicles.Any(
                    vehicle =>
                        vehicle.StartsConcealed);
            var hasBlockedMystery =
                HasBlockedMysteryVehicle(
                    level);
            var snapshot =
                CreateDifficultyContractSnapshot(
                    request,
                    level,
                    expected,
                    actual,
                    actualVehicles,
                    minimumVehicles,
                    actualGarages,
                    expectsGarages,
                    effectiveRotaryCapacity,
                    usesExactRotaryCapacity,
                    expectsMystery,
                    hasMystery,
                    hasBlockedMystery);

            // This intentionally mirrors RuntimeStageRegressionValidator's
            // ValidateRequestContract, which is the production contract for
            // raw runtime candidates. Effective/exact rotary, profile pressure,
            // route preference and the upper vehicle delta are still reported
            // below, but belong to the later gameplay-safe fallback contract
            // and must not reject an otherwise valid raw legacy candidate.
            if (actual.Difficulty !=
                    request.Difficulty ||
                actual.TargetVehicleCount !=
                    expected.TargetVehicleCount ||
                actual.TargetColorCount !=
                    expected.TargetColorCount ||
                actualVehicles < minimumVehicles ||
                level.RotaryUnitCapacity !=
                    request.RotaryCapacity ||
                level.RoadPresetId !=
                    request.RoadPresetId ||
                (actualGarages > 0) !=
                    expectsGarages ||
                actualGarages !=
                    request.GarageCount)
            {
                diagnostic =
                    "Difficulty contract mismatch: " +
                    snapshot;
                return false;
            }

            for (var garageIndex = 0;
                garageIndex < actualGarages;
                garageIndex++)
            {
                var queueCount =
                    level.Garages[garageIndex]
                        .QueuedVehicles.Count;
                if (queueCount <
                        request.MinGarageQueuedVehicles ||
                    queueCount >
                        request.MaxGarageQueuedVehicles)
                {
                    diagnostic =
                        $"Garage {garageIndex + 1} queue {queueCount} " +
                        $"is outside {request.MinGarageQueuedVehicles}-" +
                        $"{request.MaxGarageQueuedVehicles}. " +
                        snapshot;
                    return false;
                }
            }

            if (expectsMystery !=
                hasMystery)
            {
                diagnostic =
                    $"Mystery contract mismatch: expected " +
                    $"{expectsMystery}, got {hasMystery}. " +
                    snapshot;
                return false;
            }

            if (expectsMystery &&
                !hasBlockedMystery)
            {
                diagnostic =
                    "Mystery contract mismatch: all concealed vehicles " +
                    "can exit immediately. " +
                    snapshot;
                return false;
            }

            return true;
        }

        private static string CreateDifficultyContractSnapshot(
            StageGenerationRequest request,
            LevelData level,
            LevelDifficultyProfile expected,
            LevelDifficultyProfile actual,
            int actualVehicles,
            int minimumVehicles,
            int actualGarages,
            bool expectsGarages,
            int effectiveRotaryCapacity,
            bool usesExactRotaryCapacity,
            bool expectsMystery,
            bool hasMystery,
            bool hasBlockedMystery)
        {
            var parkingDelta =
                Mathf.Abs(
                    actual.ParkingTension -
                    expected.ParkingTension);
            var stationDelta =
                Mathf.Abs(
                    actual.StationPressure -
                    expected.StationPressure);
            var vehicleDelta =
                Mathf.Abs(
                    actualVehicles -
                    expected.TargetVehicleCount);
            var garageQueueCounts =
                level.Garages != null
                    ? string.Join(
                        ",",
                        level.Garages.Select(
                            garage =>
                                garage.QueuedVehicles
                                    .Count))
                    : string.Empty;
            return
                $"difficulty={actual.Difficulty}/" +
                $"{request.Difficulty}; " +
                $"profileTargetVehicles=" +
                $"{actual.TargetVehicleCount}/" +
                $"{expected.TargetVehicleCount}; " +
                $"profileTargetColors=" +
                $"{actual.TargetColorCount}/" +
                $"{expected.TargetColorCount}; " +
                $"actualVehicles={actualVehicles}, " +
                $"minimum={minimumVehicles}, " +
                $"minimumPassed=" +
                $"{actualVehicles >= minimumVehicles}, " +
                $"delta={vehicleDelta}, " +
                $"withinFallbackDelta=" +
                $"{vehicleDelta <= MaximumVehicleTargetDelta}; " +
                $"parking=" +
                $"{FormatFloat(actual.ParkingTension)}/" +
                $"{FormatFloat(expected.ParkingTension)}, " +
                $"delta={FormatFloat(parkingDelta)}, " +
                $"match=" +
                $"{parkingDelta <= DifficultyComparisonEpsilon}; " +
                $"station=" +
                $"{FormatFloat(actual.StationPressure)}/" +
                $"{FormatFloat(expected.StationPressure)}, " +
                $"delta={FormatFloat(stationDelta)}, " +
                $"match=" +
                $"{stationDelta <= DifficultyComparisonEpsilon}; " +
                $"requireSolutionRoute=" +
                $"{actual.RequireSolutionRoute}/" +
                $"{expected.RequireSolutionRoute}; " +
                $"garages={actualGarages}/" +
                $"{request.GarageCount}, " +
                $"expectsGarages={expectsGarages}, " +
                $"queueRange=" +
                $"{request.MinGarageQueuedVehicles}-" +
                $"{request.MaxGarageQueuedVehicles}, " +
                $"queueCounts=[" +
                $"{garageQueueCounts}]; " +
                $"road={level.RoadPresetId}/" +
                $"{request.RoadPresetId}; " +
                $"rotaryStored=" +
                $"{level.RotaryUnitCapacity}, " +
                $"rotaryEffective=" +
                $"{effectiveRotaryCapacity}, " +
                $"rotaryRequested=" +
                $"{request.RotaryCapacity}, " +
                $"usesExactRotary=" +
                $"{usesExactRotaryCapacity}; " +
                $"mysteryExpected={expectsMystery}, " +
                $"mysteryPresent={hasMystery}, " +
                $"blockedMystery={hasBlockedMystery}.";
        }

        private static string FormatFloat(
            float value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static bool HasBlockedMysteryVehicle(
            LevelData level)
        {
            if (level?.Buses == null)
            {
                return false;
            }

            for (var index = 0;
                index < level.Buses.Count;
                index++)
            {
                if (level.Buses[index]
                        .StartsConcealed &&
                    !LevelGenerator
                        .IsVehiclePathClearForValidation(
                            level.Buses,
                            index))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InvokeShapeQuality(
            StageGenerationRequest request,
            LevelData level,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            try
            {
                var type =
                    typeof(LevelGenerator).Assembly.GetType(
                        "BusPuzzle.ShapeLibraryLayoutQuality");
                var method =
                    type?.GetMethod(
                        "IsSatisfied",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Static,
                        null,
                        new[]
                        {
                            typeof(
                                LevelDifficultyProfile),
                            typeof(int),
                            typeof(
                                IReadOnlyList<
                                    BusDefinition>),
                            typeof(bool)
                        },
                        null);
                if (method == null)
                {
                    diagnostic =
                        "ShapeLibraryLayoutQuality.IsSatisfied was unavailable.";
                    return false;
                }

                var value = method.Invoke(
                    null,
                    new object[]
                    {
                        request.Profile,
                        request.VehicleLayoutVariantIndex,
                        level.Buses,
                        true
                    });
                return value is bool passed &&
                    passed;
            }
            catch (Exception exception)
            {
                diagnostic =
                    $"Shape quality probe failed: " +
                    $"{Unwrap(exception)}";
                return false;
            }
        }

        private static bool ReplayWitness(
            LevelData level,
            IReadOnlyList<
                SuperHardGarageConstructiveWitnessStep>
                witness,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            try
            {
                var publicWitness =
                    new StageSolutionWitnessStep[
                        witness != null
                            ? witness.Count
                            : 0];
                for (var index = 0;
                    index < publicWitness.Length;
                    index++)
                {
                    var step = witness[index];
                    publicWitness[index] =
                        new StageSolutionWitnessStep(
                            step.VehicleIndex,
                            step.GarageIndex,
                            step.GarageProgress);
                }

                var method =
                    typeof(StageSolutionAnalyzer)
                        .GetMethod(
                            "ValidateMemoizedWitness",
                            BindingFlags.NonPublic |
                            BindingFlags.Static);
                if (method == null)
                {
                    diagnostic =
                        "Independent witness replay method was unavailable.";
                    return false;
                }

                var value = method.Invoke(
                    null,
                    new object[]
                    {
                        level.Buses,
                        level.Garages,
                        publicWitness,
                        CancellationToken.None
                    });
                if (value is bool passed &&
                    passed)
                {
                    return true;
                }

                diagnostic =
                    "Independent witness replay rejected the authored route.";
                return false;
            }
            catch (Exception exception)
            {
                diagnostic =
                    $"Witness replay failed: " +
                    $"{Unwrap(exception)}";
                return false;
            }
        }

        private static int CountOpeningMoves(
            LevelData level)
        {
            if (level == null)
            {
                return 0;
            }

            var buses =
                new List<BusDefinition>();
            if (level.Buses != null)
            {
                buses.AddRange(level.Buses);
            }

            if (level.Garages != null)
            {
                for (var index = 0;
                    index < level.Garages.Count;
                    index++)
                {
                    buses.Add(
                        level.Garages[index]
                            .FrontVehicle);
                }
            }

            var active =
                Enumerable.Repeat(
                        true,
                        buses.Count)
                    .ToArray();
            try
            {
                var planner =
                    typeof(LevelGenerator).Assembly.GetType(
                        "BusPuzzle.LevelVehicleExitPlanner");
                var method =
                    planner?.GetMethods(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static)
                        .FirstOrDefault(candidate =>
                        {
                            if (candidate.Name !=
                                "IsPathClear")
                            {
                                return false;
                            }

                            var parameters =
                                candidate.GetParameters();
                            return parameters.Length == 5 &&
                                parameters[3].ParameterType ==
                                typeof(
                                    IReadOnlyList<
                                        GarageDefinition>);
                        });
                if (method == null)
                {
                    return CountRegularOpeningMoves(
                        level.Buses);
                }

                var count = 0;
                for (var index = 0;
                    index < buses.Count;
                    index++)
                {
                    var args = new object[]
                    {
                        index,
                        buses,
                        active,
                        level.Garages,
                        -1
                    };
                    if (method.Invoke(
                            null,
                            args) is bool clear &&
                        clear)
                    {
                        count++;
                    }
                }

                return count;
            }
            catch
            {
                return CountRegularOpeningMoves(
                    level.Buses);
            }
        }

        private static int CountRegularOpeningMoves(
            IReadOnlyList<BusDefinition> buses)
        {
            var count = 0;
            if (buses == null)
            {
                return count;
            }

            for (var index = 0;
                index < buses.Count;
                index++)
            {
                if (LevelGenerator
                    .IsVehiclePathClearForValidation(
                        buses,
                        index))
                {
                    count++;
                }
            }

            return count;
        }

        private static void RunCancellationProbes(
            StageGenerationConfig config,
            Options options,
            ICollection<CancellationProbeResult> results)
        {
            for (var targetIndex = 0;
                targetIndex < DefaultTargetStages.Length;
                targetIndex++)
            {
                var stageNumber =
                    DefaultTargetStages[targetIndex];
                if (!options.Stages.Contains(
                        stageNumber))
                {
                    continue;
                }

                var request =
                    StageGenerationPlanner.CreateRequest(
                        config,
                        stageNumber);
                using var cancellation =
                    new CancellationTokenSource();
                cancellation.Cancel();
                var watch =
                    Stopwatch.StartNew();
                LevelData level = null;
                var passed = false;
                var diagnostic =
                    string.Empty;
                try
                {
                    SuperHardGarageConstructiveGenerator
                        .TryGenerateForComparison(
                            request,
                            0,
                            options.LayoutProbeCount,
                            cancellation.Token,
                            out var generation);
                    level = generation.Level;
                    diagnostic =
                        "Pre-cancelled constructive generation returned normally.";
                }
                catch (OperationCanceledException)
                {
                    passed = true;
                }
                catch (Exception exception)
                {
                    diagnostic =
                        $"Unexpected cancellation exception: " +
                        $"{Unwrap(exception)}";
                }
                finally
                {
                    watch.Stop();
                    DestroyLevel(level);
                }

                results.Add(
                    new CancellationProbeResult
                    {
                        StageNumber =
                            stageNumber,
                        Passed = passed,
                        Milliseconds =
                            ToMilliseconds(
                                watch.ElapsedTicks),
                        Diagnostic =
                            diagnostic
                    });
            }
        }

        private static void CopyLegacy(
            SideResult side,
            CandidateResult result)
        {
            result.LegacyCreated =
                side.Created;
            result.LegacySucceeded =
                side.Succeeded;
            result.LegacyVehicles =
                side.VehicleCount;
            result.LegacyGarages =
                side.GarageCount;
            result.LegacyOpeningMoves =
                side.OpeningMoves;
            result.LegacyOpeningMoveRatio =
                side.OpeningMoveRatio;
            result.LegacyValidationPassed =
                side.ValidationPassed;
            result.LegacyDifficultyContractPassed =
                side.DifficultyContractPassed;
            result.LegacyShapeCoveragePassed =
                side.ShapeCoveragePassed;
            result.LegacyShapeQualityPassed =
                side.ShapeQualityPassed;
            result.LegacyIndependentSolvable =
                side.IndependentSolvable;
            result.LegacyIndependentHitLimit =
                side.IndependentHitLimit;
            result.LegacyIndependentSolutionCount =
                side.IndependentSolutionCount;
            result.LegacyIndependentSolutionDistance =
                side.IndependentSolutionDistance;
            result.LegacyBuildMilliseconds =
                side.BuildMilliseconds;
            result.LegacyVerifyMilliseconds =
                side.VerifyMilliseconds;
            result.LegacyTotalMilliseconds =
                side.TotalMilliseconds;
            result.LegacyAllocatedBytes =
                side.AllocatedBytes;
            result.LegacyFingerprint =
                side.Fingerprint;
            result.LegacyGeometryFingerprint =
                side.GeometryFingerprint;
            result.Diagnostic =
                AppendDiagnostic(
                    result.Diagnostic,
                    side.Diagnostic);
        }

        private static void CopyConstructive(
            SideResult side,
            CandidateResult result)
        {
            result.ConstructiveGeneratorSucceeded =
                side.GeneratorSucceeded;
            result.ConstructiveCreated =
                side.Created;
            result.ConstructiveSucceeded =
                side.Succeeded;
            result.ConstructiveVehicles =
                side.VehicleCount;
            result.ConstructiveGarages =
                side.GarageCount;
            result.ConstructiveOpeningMoves =
                side.OpeningMoves;
            result.ConstructiveOpeningMoveRatio =
                side.OpeningMoveRatio;
            result.ConstructiveValidationPassed =
                side.ValidationPassed;
            result.ConstructiveDifficultyContractPassed =
                side.DifficultyContractPassed;
            result.ConstructiveShapeCoveragePassed =
                side.ShapeCoveragePassed;
            result.ConstructiveShapeQualityPassed =
                side.ShapeQualityPassed;
            result.ConstructiveIndependentSolvable =
                side.IndependentSolvable;
            result.ConstructiveIndependentHitLimit =
                side.IndependentHitLimit;
            result.ConstructiveIndependentSolutionCount =
                side.IndependentSolutionCount;
            result.ConstructiveIndependentSolutionDistance =
                side.IndependentSolutionDistance;
            result.ConstructiveGeneratorWitnessValidated =
                side.GeneratorWitnessValidated;
            result.ConstructiveWitnessReplayPassed =
                side.WitnessReplayPassed;
            result.ConstructiveWitnessLength =
                side.WitnessLength;
            result.ConstructiveCandidateSeed =
                side.CandidateSeed;
            result.ConstructiveLayoutProbeIndex =
                side.LayoutProbeIndex;
            result.ConstructivePlacementProbeCount =
                side.PlacementProbeCount;
            result.ConstructivePathProbeCount =
                side.PathProbeCount;
            result.ConstructivePlacementProbeLimit =
                side.PlacementProbeLimit;
            result.ConstructivePathProbeLimit =
                side.PathProbeLimit;
            result.ConstructiveHitOperationBudget =
                side.HitOperationBudget;
            result.ConstructiveRegularVehicleCount =
                side.RegularVehicleCount;
            result.ConstructiveGarageVehicleCount =
                side.GarageVehicleCount;
            result.ConstructiveRegularPrefixCount =
                side.RegularPrefixCount;
            result.ConstructiveRegularSuffixCount =
                side.RegularSuffixCount;
            result.ConstructiveRegularPrefixRatio =
                side.RegularPrefixRatio;
            result.ConstructiveRegularSuffixRatio =
                side.RegularSuffixRatio;
            result.ConstructiveInitialOpeningCount =
                side.InitialOpeningCount;
            result.ConstructiveMaximumInitialOpeningCount =
                side.MaximumInitialOpeningCount;
            result.ConstructiveGarageDependencyTarget =
                side.GarageDependencyTarget;
            result.ConstructiveGarageDependencyEvaluated =
                side.GarageDependencyEvaluated;
            result.ConstructiveGarageDependencyCount =
                side.GarageDependencyCount;
            result.ConstructiveSuffixOnlyReleaseEvaluated =
                side.SuffixOnlyReleaseEvaluated;
            result.ConstructiveSuffixOnlyReleasedGarageCount =
                side.SuffixOnlyReleasedGarageCount;
            result.ConstructiveSuffixOnlyReleaseTarget =
                side.SuffixOnlyReleaseTarget;
            result.ConstructiveSuffixOnlyReleasePassed =
                side.SuffixOnlyReleasePassed;
            result.ConstructiveBuildMilliseconds =
                side.BuildMilliseconds;
            result.ConstructiveVerifyMilliseconds =
                side.VerifyMilliseconds;
            result.ConstructiveWitnessReplayMilliseconds =
                side.WitnessReplayMilliseconds;
            result.ConstructiveTotalMilliseconds =
                side.TotalMilliseconds;
            result.ConstructiveAllocatedBytes =
                side.AllocatedBytes;
            result.ConstructiveFingerprint =
                side.Fingerprint;
            result.ConstructiveGeometryFingerprint =
                side.GeometryFingerprint;
            result.ConstructiveWitnessFingerprint =
                side.WitnessFingerprint;
            result.Diagnostic =
                AppendDiagnostic(
                    result.Diagnostic,
                    side.Diagnostic);
        }

        private static void ClassifyCandidate(
            CandidateResult result)
        {
            if (!result.CandidateSeedMatched)
            {
                result.Failed = true;
                result.Verdict =
                    "candidate_seed_mismatch";
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        $"Candidate seed mismatch: legacy " +
                        $"{result.LegacyCandidateSeed}, constructive " +
                        $"{result.ConstructiveCandidateSeed}.");
                return;
            }

            if (!result.LegacyRepeatMatched ||
                !result.ConstructiveRepeatMatched)
            {
                result.Failed = true;
                result.Verdict =
                    "determinism_failure";
                return;
            }

            if (!result.ConstructiveApplicable)
            {
                result.Verdict =
                    result.LegacySucceeded
                        ? "passthrough_succeeded"
                        : "passthrough_failed";
                return;
            }

            if (result.ConstructiveGeneratorSucceeded &&
                (!result.ConstructiveGeneratorWitnessValidated ||
                 !result.ConstructiveWitnessReplayPassed ||
                 result.ConstructiveWitnessLength !=
                    result.ConstructiveVehicles))
            {
                result.Failed = true;
                result.Verdict =
                    "constructive_witness_failure";
                return;
            }

            if (result.ConstructiveGeneratorSucceeded &&
                !HasValidConstructiveTelemetry(
                    result,
                    out var telemetryDiagnostic))
            {
                result.Failed = true;
                result.Verdict =
                    "constructive_telemetry_failure";
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        telemetryDiagnostic);
                return;
            }

            if (result.LegacySucceeded &&
                !result.ConstructiveSucceeded)
            {
                result.Verdict =
                    "constructive_regression";
                return;
            }

            if (!result.LegacySucceeded &&
                result.ConstructiveSucceeded)
            {
                result.Verdict =
                    "constructive_recovered";
                return;
            }

            result.Verdict =
                result.ConstructiveSucceeded
                    ? "both_succeeded"
                    : "both_failed";
        }

        private static bool HasValidConstructiveTelemetry(
            CandidateResult result)
        {
            return HasValidConstructiveTelemetry(
                result,
                out _);
        }

        private static bool HasValidConstructiveTelemetry(
            CandidateResult result,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (result.ConstructiveRegularVehicleCount <= 0 ||
                result.ConstructiveRegularPrefixCount <= 0 ||
                result.ConstructiveRegularSuffixCount <= 0 ||
                result.ConstructiveRegularPrefixCount +
                    result.ConstructiveRegularSuffixCount !=
                    result.ConstructiveRegularVehicleCount)
            {
                diagnostic =
                    $"Invalid regular partition telemetry: regular=" +
                    $"{result.ConstructiveRegularVehicleCount}, prefix=" +
                    $"{result.ConstructiveRegularPrefixCount}, suffix=" +
                    $"{result.ConstructiveRegularSuffixCount}.";
                return false;
            }

            var expectedPrefixRatio =
                result.ConstructiveRegularPrefixCount /
                (float)result.ConstructiveRegularVehicleCount;
            var expectedSuffixRatio =
                result.ConstructiveRegularSuffixCount /
                (float)result.ConstructiveRegularVehicleCount;
            if (!Mathf.Approximately(
                    result.ConstructiveRegularPrefixRatio,
                    expectedPrefixRatio) ||
                !Mathf.Approximately(
                    result.ConstructiveRegularSuffixRatio,
                    expectedSuffixRatio) ||
                !Mathf.Approximately(
                    result.ConstructiveRegularPrefixRatio +
                    result.ConstructiveRegularSuffixRatio,
                    1f))
            {
                diagnostic =
                    $"Invalid regular partition ratios: prefix=" +
                    $"{result.ConstructiveRegularPrefixRatio}, suffix=" +
                    $"{result.ConstructiveRegularSuffixRatio}.";
                return false;
            }

            if (!result.ConstructiveGarageDependencyEvaluated ||
                result.ConstructiveGarageDependencyTarget <= 0 ||
                result.ConstructiveGarageDependencyTarget >
                    result.RequestedGarageCount ||
                result.ConstructiveGarageDependencyCount <
                    result.ConstructiveGarageDependencyTarget ||
                result.ConstructiveGarageDependencyCount >
                    result.RequestedGarageCount)
            {
                diagnostic =
                    $"Invalid garage dependency telemetry: evaluated=" +
                    $"{result.ConstructiveGarageDependencyEvaluated}, actual=" +
                    $"{result.ConstructiveGarageDependencyCount}, target=" +
                    $"{result.ConstructiveGarageDependencyTarget}, garages=" +
                    $"{result.RequestedGarageCount}.";
                return false;
            }

            if (!result.ConstructiveSuffixOnlyReleaseEvaluated ||
                result.ConstructiveSuffixOnlyReleaseTarget <= 0 ||
                result.ConstructiveSuffixOnlyReleaseTarget !=
                    result.RequestedGarageCount ||
                result.ConstructiveSuffixOnlyReleasedGarageCount !=
                    result.ConstructiveSuffixOnlyReleaseTarget ||
                !result.ConstructiveSuffixOnlyReleasePassed)
            {
                diagnostic =
                    $"Invalid suffix-only release telemetry: evaluated=" +
                    $"{result.ConstructiveSuffixOnlyReleaseEvaluated}, released=" +
                    $"{result.ConstructiveSuffixOnlyReleasedGarageCount}, target=" +
                    $"{result.ConstructiveSuffixOnlyReleaseTarget}, passed=" +
                    $"{result.ConstructiveSuffixOnlyReleasePassed}.";
                return false;
            }

            return true;
        }

        private static bool SideDeterminismMatches(
            SideResult first,
            SideResult second,
            bool includeWitness)
        {
            return first.GeneratorSucceeded ==
                    second.GeneratorSucceeded &&
                first.Created == second.Created &&
                first.VehicleCount ==
                    second.VehicleCount &&
                first.GarageCount ==
                    second.GarageCount &&
                string.Equals(
                    first.Fingerprint,
                    second.Fingerprint,
                    StringComparison.Ordinal) &&
                string.Equals(
                    first.GeometryFingerprint,
                    second.GeometryFingerprint,
                    StringComparison.Ordinal) &&
                (!includeWitness ||
                 (first.GeneratorWitnessValidated ==
                        second.GeneratorWitnessValidated &&
                  first.CandidateSeed ==
                        second.CandidateSeed &&
                  first.LayoutProbeIndex ==
                        second.LayoutProbeIndex &&
                  first.PlacementProbeCount ==
                        second.PlacementProbeCount &&
                  first.PathProbeCount ==
                        second.PathProbeCount &&
                  first.PlacementProbeLimit ==
                        second.PlacementProbeLimit &&
                  first.PathProbeLimit ==
                        second.PathProbeLimit &&
                  first.HitOperationBudget ==
                        second.HitOperationBudget &&
                  first.RegularVehicleCount ==
                        second.RegularVehicleCount &&
                  first.GarageVehicleCount ==
                        second.GarageVehicleCount &&
                  first.RegularPrefixCount ==
                        second.RegularPrefixCount &&
                  first.RegularSuffixCount ==
                        second.RegularSuffixCount &&
                  Mathf.Approximately(
                      first.RegularPrefixRatio,
                      second.RegularPrefixRatio) &&
                  Mathf.Approximately(
                      first.RegularSuffixRatio,
                      second.RegularSuffixRatio) &&
                  first.InitialOpeningCount ==
                        second.InitialOpeningCount &&
                  first.MaximumInitialOpeningCount ==
                        second.MaximumInitialOpeningCount &&
                  first.GarageDependencyTarget ==
                        second.GarageDependencyTarget &&
                  first.GarageDependencyEvaluated ==
                        second.GarageDependencyEvaluated &&
                  first.GarageDependencyCount ==
                        second.GarageDependencyCount &&
                  first.SuffixOnlyReleaseEvaluated ==
                        second.SuffixOnlyReleaseEvaluated &&
                  first.SuffixOnlyReleasedGarageCount ==
                        second.SuffixOnlyReleasedGarageCount &&
                  first.SuffixOnlyReleaseTarget ==
                        second.SuffixOnlyReleaseTarget &&
                  first.SuffixOnlyReleasePassed ==
                        second.SuffixOnlyReleasePassed &&
                  first.WitnessLength ==
                        second.WitnessLength &&
                  string.Equals(
                      first.WitnessFingerprint,
                      second.WitnessFingerprint,
                      StringComparison.Ordinal)));
        }

        private static SideResult CloneSide(
            SideResult source)
        {
            return new SideResult
            {
                GeneratorSucceeded =
                    source.GeneratorSucceeded,
                Created = source.Created,
                Succeeded = source.Succeeded,
                VehicleCount = source.VehicleCount,
                GarageCount = source.GarageCount,
                OpeningMoves = source.OpeningMoves,
                OpeningMoveRatio =
                    source.OpeningMoveRatio,
                ValidationPassed =
                    source.ValidationPassed,
                DifficultyContractPassed =
                    source.DifficultyContractPassed,
                ShapeCoveragePassed =
                    source.ShapeCoveragePassed,
                ShapeQualityPassed =
                    source.ShapeQualityPassed,
                IndependentSolvable =
                    source.IndependentSolvable,
                IndependentHitLimit =
                    source.IndependentHitLimit,
                IndependentSolutionCount =
                    source.IndependentSolutionCount,
                IndependentSolutionDistance =
                    source.IndependentSolutionDistance,
                GeneratorWitnessValidated = true,
                WitnessReplayPassed = true,
                WitnessLength = 0,
                CandidateSeed =
                    source.CandidateSeed,
                LayoutProbeIndex =
                    source.LayoutProbeIndex,
                PlacementProbeCount =
                    source.PlacementProbeCount,
                PathProbeCount =
                    source.PathProbeCount,
                PlacementProbeLimit =
                    source.PlacementProbeLimit,
                PathProbeLimit =
                    source.PathProbeLimit,
                HitOperationBudget =
                    source.HitOperationBudget,
                RegularVehicleCount =
                    source.RegularVehicleCount,
                GarageVehicleCount =
                    source.GarageVehicleCount,
                RegularPrefixCount =
                    source.RegularPrefixCount,
                RegularSuffixCount =
                    source.RegularSuffixCount,
                RegularPrefixRatio =
                    source.RegularPrefixRatio,
                RegularSuffixRatio =
                    source.RegularSuffixRatio,
                InitialOpeningCount =
                    source.InitialOpeningCount,
                MaximumInitialOpeningCount =
                    source.MaximumInitialOpeningCount,
                GarageDependencyTarget =
                    source.GarageDependencyTarget,
                GarageDependencyEvaluated =
                    source.GarageDependencyEvaluated,
                GarageDependencyCount =
                    source.GarageDependencyCount,
                SuffixOnlyReleaseEvaluated =
                    source.SuffixOnlyReleaseEvaluated,
                SuffixOnlyReleasedGarageCount =
                    source.SuffixOnlyReleasedGarageCount,
                SuffixOnlyReleaseTarget =
                    source.SuffixOnlyReleaseTarget,
                SuffixOnlyReleasePassed =
                    source.SuffixOnlyReleasePassed,
                BuildMilliseconds =
                    source.BuildMilliseconds,
                VerifyMilliseconds =
                    source.VerifyMilliseconds,
                TotalMilliseconds =
                    source.TotalMilliseconds,
                AllocatedBytes =
                    source.AllocatedBytes,
                Fingerprint =
                    source.Fingerprint,
                GeometryFingerprint =
                    source.GeometryFingerprint,
                WitnessFingerprint =
                    source.WitnessFingerprint,
                Diagnostic =
                    source.Diagnostic
            };
        }

        private static int FindAcceptedCandidate(
            IReadOnlyList<CandidateResult> candidates,
            bool constructive)
        {
            for (var index = 0;
                index < candidates.Count;
                index++)
            {
                if (constructive
                        ? !candidates[index].Failed &&
                            candidates[index]
                                .ConstructiveSucceeded
                        : candidates[index]
                            .LegacySucceeded)
                {
                    return index;
                }
            }

            return -1;
        }

        private static double CalculateStageTime(
            IReadOnlyList<CandidateResult> candidates,
            int acceptedCandidate,
            bool constructive)
        {
            var finalIndex =
                acceptedCandidate >= 0
                    ? acceptedCandidate
                    : candidates.Count - 1;
            var total = 0d;
            for (var index = 0;
                index <= finalIndex;
                index++)
            {
                total += constructive
                    ? candidates[index]
                        .ConstructiveTotalMilliseconds
                    : candidates[index]
                        .LegacyTotalMilliseconds;
            }

            return total;
        }

        private static bool RegisterGeometry(
            int stageNumber,
            string fingerprint,
            ISet<string> releaseGeometry,
            IDictionary<string, int> owners,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (string.IsNullOrWhiteSpace(
                    fingerprint))
            {
                diagnostic =
                    "Accepted stage has no geometry fingerprint.";
                return false;
            }

            if (releaseGeometry != null &&
                releaseGeometry.Contains(
                    fingerprint))
            {
                diagnostic =
                    "Accepted stage repeats a locked 1-200 geometry.";
                return false;
            }

            if (owners.TryGetValue(
                    fingerprint,
                    out var owner))
            {
                diagnostic =
                    $"Accepted stage repeats stage {owner} geometry.";
                return false;
            }

            owners.Add(
                fingerprint,
                stageNumber);
            return true;
        }

        private static ComparisonSummary CreateSummary(
            DateTime startedAt,
            DateTime finishedAt,
            Options options,
            bool constructiveApiAvailable,
            bool cancelled,
            IReadOnlyList<CandidateResult> candidates,
            IReadOnlyList<StageDigest> stages,
            IReadOnlyList<CancellationProbeResult>
                cancellation)
        {
            var legacyCandidateTimes =
                candidates.Select(
                        result =>
                            result.LegacyTotalMilliseconds)
                    .OrderBy(value => value)
                    .ToArray();
            var constructiveCandidateTimes =
                candidates.Select(
                        result =>
                            result.ConstructiveTotalMilliseconds)
                    .OrderBy(value => value)
                    .ToArray();
            var legacyStageTimes =
                stages.Select(
                        stage =>
                            stage.legacyMilliseconds)
                    .OrderBy(value => value)
                    .ToArray();
            var constructiveStageTimes =
                stages.Select(
                        stage =>
                            stage.constructiveMilliseconds)
                    .OrderBy(value => value)
                    .ToArray();
            var constructiveTelemetryStages =
                stages.Where(
                        stage =>
                            stage.constructiveApplicable &&
                            stage.constructiveSucceeded)
                    .ToArray();
            var summary = new ComparisonSummary
            {
                startedAtUtc =
                    startedAt.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                finishedAtUtc =
                    finishedAt.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                unityVersion =
                    Application.unityVersion,
                applicationVersion =
                    Application.version,
                stageSpecification =
                    options.StageSpecification,
                constructiveApi =
                    "SuperHardGarageConstructiveGenerator.TryGenerateForComparison",
                constructiveApiAvailable =
                    constructiveApiAvailable,
                cancelled = cancelled,
                requestedStageCount =
                    options.Stages.Length,
                applicableStageCount =
                    stages.Count(
                        stage =>
                            stage.constructiveApplicable),
                passthroughStageCount =
                    stages.Count(
                        stage =>
                            !stage.constructiveApplicable),
                candidateCount =
                    candidates.Count,
                legacyCandidateSuccessCount =
                    candidates.Count(
                        candidate =>
                            candidate.LegacySucceeded),
                constructiveCandidateSuccessCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveSucceeded &&
                            !candidate.Failed),
                legacySolvedStageCount =
                    stages.Count(
                        stage =>
                            stage.legacySucceeded),
                constructiveSolvedStageCount =
                    stages.Count(
                        stage =>
                            stage.constructiveSucceeded),
                constructiveTelemetryStageCount =
                    constructiveTelemetryStages.Length,
                constructiveRegularVehicleCountTotal =
                    constructiveTelemetryStages.Sum(
                        stage =>
                            stage.constructiveRegularVehicleCount),
                constructiveRegularPrefixCountTotal =
                    constructiveTelemetryStages.Sum(
                        stage =>
                            stage.constructiveRegularPrefixCount),
                constructiveRegularSuffixCountTotal =
                    constructiveTelemetryStages.Sum(
                        stage =>
                            stage.constructiveRegularSuffixCount),
                constructiveRegularPrefixRatioAverage =
                    constructiveTelemetryStages.Length > 0
                        ? constructiveTelemetryStages.Average(
                            stage =>
                                (double)stage
                                    .constructiveRegularPrefixRatio)
                        : 0d,
                constructiveRegularSuffixRatioAverage =
                    constructiveTelemetryStages.Length > 0
                        ? constructiveTelemetryStages.Average(
                            stage =>
                                (double)stage
                                    .constructiveRegularSuffixRatio)
                        : 0d,
                constructiveGarageDependencyTargetTotal =
                    constructiveTelemetryStages.Sum(
                        stage =>
                            stage.constructiveGarageDependencyTarget),
                constructiveGarageDependencyActualTotal =
                    constructiveTelemetryStages.Sum(
                        stage =>
                            stage.constructiveGarageDependencyActual),
                constructiveGarageDependencyEvaluatedStageCount =
                    constructiveTelemetryStages.Count(
                        stage =>
                            stage.constructiveGarageDependencyEvaluated),
                constructiveSuffixOnlyReleasedGarageCountTotal =
                    constructiveTelemetryStages.Sum(
                        stage =>
                            stage
                                .constructiveSuffixOnlyReleasedGarageCount),
                constructiveSuffixOnlyReleaseTargetTotal =
                    constructiveTelemetryStages.Sum(
                        stage =>
                            stage.constructiveSuffixOnlyReleaseTarget),
                constructiveSuffixOnlyReleaseEvaluatedStageCount =
                    constructiveTelemetryStages.Count(
                        stage =>
                            stage
                                .constructiveSuffixOnlyReleaseEvaluated),
                constructiveSuffixOnlyReleasePassedStageCount =
                    constructiveTelemetryStages.Count(
                        stage =>
                            stage.constructiveSuffixOnlyReleasePassed),
                recoveredStageCount =
                    stages.Count(
                        stage =>
                            !stage.legacySucceeded &&
                            stage.constructiveSucceeded),
                regressedStageCount =
                    stages.Count(
                        stage =>
                            stage.legacySucceeded &&
                            !stage.constructiveSucceeded),
                legacyValidationFailureCount =
                    candidates.Count(
                        candidate =>
                            candidate.LegacyCreated &&
                            !candidate
                                .LegacyValidationPassed),
                constructiveValidationFailureCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveApplicable &&
                            candidate.ConstructiveCreated &&
                            !candidate
                                .ConstructiveValidationPassed),
                witnessFailureCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveApplicable &&
                            candidate
                                .ConstructiveGeneratorSucceeded &&
                            (!candidate
                                 .ConstructiveGeneratorWitnessValidated ||
                             !candidate
                                 .ConstructiveWitnessReplayPassed ||
                             candidate
                                 .ConstructiveWitnessLength !=
                             candidate
                                 .ConstructiveVehicles)),
                deterministicFailureCount =
                    candidates.Count(
                        candidate =>
                            !candidate
                                .LegacyRepeatMatched ||
                            !candidate
                                .ConstructiveRepeatMatched),
                seedMismatchCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveApplicable &&
                            !candidate.CandidateSeedMatched),
                difficultyContractFailureCount =
                    candidates.Count(
                        candidate =>
                            (candidate.LegacyCreated &&
                             !candidate
                                 .LegacyDifficultyContractPassed) ||
                            (candidate.ConstructiveApplicable &&
                             candidate.ConstructiveCreated &&
                             !candidate
                                 .ConstructiveDifficultyContractPassed)),
                difficultyProxyRegressionCount =
                    stages.Count(
                        stage =>
                            stage.constructiveApplicable &&
                            stage.legacySucceeded &&
                            stage.constructiveSucceeded &&
                            stage.constructiveOpeningMoveRatio >
                                stage.legacyOpeningMoveRatio +
                                MaximumOpeningMoveRatioIncrease),
                shapeContractFailureCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveApplicable &&
                            candidate.ConstructiveCreated &&
                            (!candidate
                                 .ConstructiveShapeCoveragePassed ||
                             !candidate
                                 .ConstructiveShapeQualityPassed)),
                diversityFailureCount =
                    stages.Count(
                        stage =>
                            stage.constructiveSucceeded &&
                            !stage.diversityPassed),
                constructiveTelemetryFailureCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveApplicable &&
                            candidate
                                .ConstructiveGeneratorSucceeded &&
                            !HasValidConstructiveTelemetry(
                                candidate)),
                cancellationProbeCount =
                    cancellation.Count,
                cancellationFailureCount =
                    cancellation.Count(
                        result =>
                            !result.Passed),
                maximumCancellationMilliseconds =
                    cancellation.Count > 0
                        ? cancellation.Max(
                            result =>
                                result.Milliseconds)
                        : 0d,
                operationBudgetHitCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveApplicable &&
                            candidate
                                .ConstructiveHitOperationBudget),
                allocationRegressionCount =
                    candidates.Count(
                        candidate =>
                            candidate.ConstructiveApplicable &&
                            candidate.ConstructiveAllocatedBytes >
                            Math.Max(
                                candidate.LegacyAllocatedBytes *
                                AllocationRatioCeiling,
                                candidate.LegacyAllocatedBytes +
                                AllocationSlackBytes)),
                legacyAllocatedBytes =
                    candidates.Sum(
                        candidate =>
                            candidate.LegacyAllocatedBytes),
                constructiveAllocatedBytes =
                    candidates.Sum(
                        candidate =>
                            candidate
                                .ConstructiveAllocatedBytes),
                legacyCandidateTotalMilliseconds =
                    legacyCandidateTimes.Sum(),
                constructiveCandidateTotalMilliseconds =
                    constructiveCandidateTimes.Sum(),
                legacyCandidateP95Milliseconds =
                    Percentile(
                        legacyCandidateTimes,
                        0.95d),
                constructiveCandidateP95Milliseconds =
                    Percentile(
                        constructiveCandidateTimes,
                        0.95d),
                legacyCandidateP99Milliseconds =
                    Percentile(
                        legacyCandidateTimes,
                        0.99d),
                constructiveCandidateP99Milliseconds =
                    Percentile(
                        constructiveCandidateTimes,
                        0.99d),
                legacyStageTotalMilliseconds =
                    legacyStageTimes.Sum(),
                constructiveStageTotalMilliseconds =
                    constructiveStageTimes.Sum(),
                legacyStageP95Milliseconds =
                    Percentile(
                        legacyStageTimes,
                        0.95d),
                constructiveStageP95Milliseconds =
                    Percentile(
                        constructiveStageTimes,
                        0.95d),
                legacyStageP99Milliseconds =
                    Percentile(
                        legacyStageTimes,
                        0.99d),
                constructiveStageP99Milliseconds =
                    Percentile(
                        constructiveStageTimes,
                        0.99d),
                legacyStageMaximumMilliseconds =
                    legacyStageTimes.Length > 0
                        ? legacyStageTimes[
                            legacyStageTimes.Length - 1]
                        : 0d,
                constructiveStageMaximumMilliseconds =
                    constructiveStageTimes.Length > 0
                        ? constructiveStageTimes[
                            constructiveStageTimes.Length - 1]
                        : 0d,
                historicalTestedStageCount =
                    HistoricalTestedStageCount,
                historicalProceduralStageCount =
                    HistoricalProceduralStageCount,
                historicalFallbackStageCount =
                    HistoricalFallbackStageCount,
                historicalP99Milliseconds =
                    HistoricalP99Milliseconds,
                historicalMaximumMilliseconds =
                    HistoricalMaximumMilliseconds,
                historicalClearDeadlineFallbackCount =
                    HistoricalClearDeadlineFallbackCount,
                csvPath =
                    options.CsvPath,
                difficulty =
                    CreateDifficultyDigests(stages),
                targetStages =
                    stages.Where(
                            stage =>
                                TargetStageSet.Contains(
                                    stage.stageNumber))
                        .OrderBy(
                            stage =>
                                stage.stageNumber)
                        .ToArray(),
                slowestConstructiveStages =
                    stages.OrderByDescending(
                            stage =>
                                stage
                                    .constructiveMilliseconds)
                        .Take(20)
                        .ToArray(),
                issues =
                    stages.Where(
                            stage =>
                                !stage.constructiveSucceeded ||
                                stage.verdict !=
                                    "both_succeeded" &&
                                stage.verdict !=
                                    "passthrough_succeeded" ||
                                !stage.diversityPassed)
                        .ToArray()
            };

            summary.stageSuccessRateImproved =
                summary.constructiveSolvedStageCount >
                summary.legacySolvedStageCount;
            summary.endToEndP99Improved =
                summary.constructiveStageP99Milliseconds <
                summary.legacyStageP99Milliseconds;
            summary.beatsHistoricalSuccessRate =
                options.Stages.Length !=
                    HistoricalTestedStageCount ||
                summary.constructiveSolvedStageCount >
                    HistoricalProceduralStageCount;
            summary.beatsHistoricalP99 =
                options.Stages.Length !=
                    HistoricalTestedStageCount ||
                summary.constructiveStageP99Milliseconds <
                    HistoricalP99Milliseconds;
            summary.beatsHistoricalMaximum =
                options.Stages.Length !=
                    HistoricalTestedStageCount ||
                summary.constructiveStageMaximumMilliseconds <=
                    HistoricalMaximumMilliseconds;
            summary.targetStagesPassed =
                DefaultTargetStages.All(
                    target =>
                        !options.Stages.Contains(target) ||
                        stages.Any(
                            stage =>
                                stage.stageNumber ==
                                    target &&
                                stage
                                    .constructiveSucceeded));
            summary.targetPerformanceGatePassed =
                DefaultTargetStages.All(
                    target =>
                        !options.Stages.Contains(target) ||
                        stages.Any(
                            stage =>
                                stage.stageNumber ==
                                    target &&
                                stage.constructiveSucceeded &&
                                (!stage.legacySucceeded ||
                                 stage.constructiveMilliseconds <=
                                    stage.legacyMilliseconds)));
            summary.witnessGatePassed =
                summary.witnessFailureCount == 0;
            summary.validationGatePassed =
                summary.legacyValidationFailureCount == 0 &&
                summary.constructiveValidationFailureCount == 0;
            summary.determinismGatePassed =
                summary.deterministicFailureCount == 0;
            summary.sameSeedGatePassed =
                summary.seedMismatchCount == 0;
            summary.difficultyGatePassed =
                summary.difficultyContractFailureCount == 0;
            summary.difficultyProxyGatePassed =
                summary.difficultyProxyRegressionCount == 0;
            summary.shapeGatePassed =
                summary.shapeContractFailureCount == 0;
            summary.diversityGatePassed =
                summary.diversityFailureCount == 0;
            summary.constructiveTelemetryGatePassed =
                summary.constructiveTelemetryFailureCount == 0;
            summary.cancellationGatePassed =
                summary.cancellationProbeCount > 0 &&
                summary.cancellationFailureCount == 0 &&
                summary.maximumCancellationMilliseconds <=
                    MaximumCancellationMilliseconds;
            summary.operationBudgetGatePassed =
                summary.operationBudgetHitCount == 0;
            summary.allocationGatePassed =
                summary.allocationRegressionCount == 0;
            summary.passed =
                constructiveApiAvailable &&
                !cancelled &&
                stages.Count ==
                    options.Stages.Length &&
                summary.stageSuccessRateImproved &&
                summary.endToEndP99Improved &&
                summary.regressedStageCount == 0 &&
                summary.targetStagesPassed &&
                summary.targetPerformanceGatePassed &&
                summary.witnessGatePassed &&
                summary.validationGatePassed &&
                summary.determinismGatePassed &&
                summary.sameSeedGatePassed &&
                summary.difficultyGatePassed &&
                summary.difficultyProxyGatePassed &&
                summary.shapeGatePassed &&
                summary.diversityGatePassed &&
                summary.constructiveTelemetryGatePassed &&
                summary.cancellationGatePassed &&
                summary.operationBudgetGatePassed &&
                summary.allocationGatePassed &&
                summary.beatsHistoricalSuccessRate &&
                summary.beatsHistoricalP99 &&
                summary.beatsHistoricalMaximum;
            return summary;
        }

        private static DifficultyDigest[]
            CreateDifficultyDigests(
                IReadOnlyList<StageDigest> stages)
        {
            return Enum.GetValues(
                    typeof(LevelDifficulty))
                .Cast<LevelDifficulty>()
                .Select(difficulty =>
                {
                    var matching =
                        stages.Where(
                                stage =>
                                    string.Equals(
                                        stage.difficulty,
                                        difficulty
                                            .ToString(),
                                        StringComparison
                                            .Ordinal))
                            .ToArray();
                    var legacyRatios =
                        matching.Where(
                                stage =>
                                    stage.legacySucceeded)
                            .Select(
                                stage =>
                                    (double)stage
                                        .legacyOpeningMoveRatio)
                            .OrderBy(
                                value => value)
                            .ToArray();
                    var constructiveRatios =
                        matching.Where(
                                stage =>
                                    stage
                                        .constructiveSucceeded)
                            .Select(
                                stage =>
                                    (double)stage
                                        .constructiveOpeningMoveRatio)
                            .OrderBy(
                                value => value)
                            .ToArray();
                    return new DifficultyDigest
                    {
                        difficulty =
                            difficulty.ToString(),
                        stageCount =
                            matching.Length,
                        legacySuccessCount =
                            matching.Count(
                                stage =>
                                    stage
                                        .legacySucceeded),
                        constructiveSuccessCount =
                            matching.Count(
                                stage =>
                                    stage
                                        .constructiveSucceeded),
                        legacyOpeningMoveRatioAverage =
                            legacyRatios.Length > 0
                                ? legacyRatios.Average()
                                : 0d,
                        constructiveOpeningMoveRatioAverage =
                            constructiveRatios.Length > 0
                                ? constructiveRatios
                                    .Average()
                                : 0d,
                        legacyOpeningMoveRatioMedian =
                            Percentile(
                                legacyRatios,
                                0.50d),
                        constructiveOpeningMoveRatioMedian =
                            Percentile(
                                constructiveRatios,
                                0.50d)
                    };
                })
                .ToArray();
        }

        private static HashSet<string>
            CreateReleaseGeometryFingerprints(
                LevelSequence sequence)
        {
            var fingerprints =
                new HashSet<string>(
                    StringComparer.Ordinal);
            if (sequence?.StaticLevels == null)
            {
                return fingerprints;
            }

            for (var index = 0;
                index < sequence.StaticLevels.Count;
                index++)
            {
                var level =
                    sequence.StaticLevels[index];
                if (level != null)
                {
                    fingerprints.Add(
                        CreateGeometryFingerprint(
                            level));
                }
            }

            return fingerprints;
        }

        private static string CreateContentFingerprint(
            LevelData level)
        {
            var builder =
                new StringBuilder(4096);
            var profile =
                level.DifficultyProfile ??
                LevelDifficultyProfile.DefaultFor(
                    LevelDifficulty.Normal);
            builder.Append("road=")
                .Append((int)level.RoadPresetId)
                .Append(";rotary=")
                .Append(level.RotaryUnitCapacity)
                .Append(";presentation=")
                .Append((int)level.PresentationMode)
                .Append(";difficulty=")
                .Append((int)profile.Difficulty)
                .Append(";targetVehicles=")
                .Append(profile.TargetVehicleCount)
                .Append(";targetColors=")
                .Append(profile.TargetColorCount)
                .Append(";parking=");
            AppendFloat(
                builder,
                profile.ParkingTension);
            builder.Append(";station=");
            AppendFloat(
                builder,
                profile.StationPressure);
            builder.Append(";route=")
                .Append(
                    profile.RequireSolutionRoute
                        ? 1
                        : 0)
                .Append(";passengers=");
            if (level.PassengerUnits != null)
            {
                for (var index = 0;
                    index < level.PassengerUnits.Count;
                    index++)
                {
                    builder.Append(
                            (int)level.PassengerUnits[
                                index])
                        .Append(',');
                }
            }

            AppendPassengerFlow(
                builder,
                level.PassengerFlowPlan);
            builder.Append(";buses=");
            if (level.Buses != null)
            {
                for (var index = 0;
                    index < level.Buses.Count;
                    index++)
                {
                    AppendVehicle(
                        builder,
                        level.Buses[index],
                        true);
                }
            }

            builder.Append(";garages=");
            if (level.Garages != null)
            {
                for (var index = 0;
                    index < level.Garages.Count;
                    index++)
                {
                    AppendGarage(
                        builder,
                        level.Garages[index],
                        true);
                }
            }

            return Hash(builder.ToString());
        }

        private static void AppendPassengerFlow(
            StringBuilder builder,
            PassengerFlowPlan plan)
        {
            if (plan == null)
            {
                builder.Append(";flow=null");
                return;
            }

            builder.Append(";flow=")
                .Append(plan.Enabled ? 1 : 0)
                .Append(',')
                .Append((int)plan.Mode)
                .Append(',')
                .Append(plan.Seed)
                .Append(',')
                .Append(plan.MinGroupUnits)
                .Append(',')
                .Append(plan.MaxGroupUnits)
                .Append(',')
                .Append(
                    plan.AutoFillMissingCapacity
                        ? 1
                        : 0)
                .Append(";groups=");
            for (var index = 0;
                index < plan.Groups.Count;
                index++)
            {
                builder.Append(
                        (int)plan.Groups[index]
                            .Color)
                    .Append(':')
                    .Append(
                        plan.Groups[index]
                            .UnitCount)
                    .Append(',');
            }

            builder.Append(";solutionRoute=");
            for (var index = 0;
                index < plan.SolutionRoute.Count;
                index++)
            {
                var step =
                    plan.SolutionRoute[index];
                builder.Append((int)step.Color)
                    .Append(':')
                    .Append((int)step.Size)
                    .Append(':')
                    .Append(step.OverrideUnitCount)
                    .Append(':')
                    .Append(
                        step.PreferredGroupUnitCount)
                    .Append(',');
            }
        }

        private static string CreateGeometryFingerprint(
            LevelData level)
        {
            var busTokens =
                new List<string>();
            if (level.Buses != null)
            {
                for (var index = 0;
                    index < level.Buses.Count;
                    index++)
                {
                    var token =
                        new StringBuilder(96);
                    AppendVehicle(
                        token,
                        level.Buses[index],
                        false);
                    busTokens.Add(
                        token.ToString());
                }
            }

            busTokens.Sort(
                StringComparer.Ordinal);
            var garageTokens =
                new List<string>();
            if (level.Garages != null)
            {
                for (var index = 0;
                    index < level.Garages.Count;
                    index++)
                {
                    var token =
                        new StringBuilder(256);
                    AppendGarage(
                        token,
                        level.Garages[index],
                        false);
                    garageTokens.Add(
                        token.ToString());
                }
            }

            garageTokens.Sort(
                StringComparer.Ordinal);
            return Hash(
                string.Join(
                    string.Empty,
                    busTokens) +
                "|" +
                string.Join(
                    string.Empty,
                    garageTokens));
        }

        private static string CreateWitnessFingerprint(
            IReadOnlyList<
                SuperHardGarageConstructiveWitnessStep>
                witness)
        {
            if (witness == null ||
                witness.Count == 0)
            {
                return string.Empty;
            }

            var builder =
                new StringBuilder(
                    witness.Count * 48);
            for (var index = 0;
                index < witness.Count;
                index++)
            {
                var step = witness[index];
                builder.Append(
                        step.VehicleIndex)
                    .Append(',')
                    .Append(step.GarageIndex)
                    .Append(',')
                    .Append(step.GarageProgress)
                    .Append('|');
                AppendVehicle(
                    builder,
                    step.Vehicle,
                    true);
            }

            return Hash(builder.ToString());
        }

        private static void AppendGarage(
            StringBuilder builder,
            GarageDefinition garage,
            bool includeColor)
        {
            builder.Append('[')
                .Append(garage.GridPosition.x)
                .Append(',')
                .Append(garage.GridPosition.y)
                .Append(',')
                .Append((int)garage.ExitDirection)
                .Append('|');
            AppendVehicle(
                builder,
                garage.FrontVehicle,
                includeColor);
            for (var index = 0;
                index < garage.QueuedVehicles.Count;
                index++)
            {
                AppendVehicle(
                    builder,
                    garage.QueuedVehicles[index],
                    includeColor);
            }

            builder.Append(']');
        }

        private static void AppendVehicle(
            StringBuilder builder,
            BusDefinition vehicle,
            bool includeColor)
        {
            builder.Append('{');
            if (includeColor)
            {
                builder.Append((int)vehicle.Color)
                    .Append(',');
            }

            builder.Append((int)vehicle.Size)
                .Append(',')
                .Append((int)vehicle.Direction)
                .Append(',')
                .Append(vehicle.GridPosition.x)
                .Append(',')
                .Append(vehicle.GridPosition.y)
                .Append(',')
                .Append(
                    vehicle.AngleOffsetDegrees
                        .ToString(
                            "R",
                            CultureInfo
                                .InvariantCulture))
                .Append(',')
                .Append(
                    vehicle.PositionOffsetCells.x
                        .ToString(
                            "R",
                            CultureInfo
                                .InvariantCulture))
                .Append(',')
                .Append(
                    vehicle.PositionOffsetCells.y
                        .ToString(
                            "R",
                            CultureInfo
                                .InvariantCulture));
            if (includeColor)
            {
                builder.Append(',')
                    .Append(
                        vehicle.StartsConcealed
                            ? 1
                            : 0);
            }

            builder.Append('}');
        }

        private static void AppendFloat(
            StringBuilder builder,
            float value)
        {
            builder.Append(
                value.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
        }

        private static string Hash(
            string text)
        {
            using var sha =
                SHA256.Create();
            var bytes =
                sha.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        text ?? string.Empty));
            var builder =
                new StringBuilder(
                    bytes.Length * 2);
            for (var index = 0;
                index < bytes.Length;
                index++)
            {
                builder.Append(
                    bytes[index].ToString(
                        "x2",
                        CultureInfo
                            .InvariantCulture));
            }

            return builder.ToString();
        }

        private static void InitializeCsv(
            string path)
        {
            EnsureParentDirectory(path);
            File.WriteAllText(
                path,
                "stage,candidate,difficulty,seed,legacyCandidateSeed,candidateSeedMatched," +
                "requestedVehicles,requestedGarages," +
                "constructiveApplicable,legacyCreated,legacySucceeded,legacyVehicles," +
                "legacyGarages,legacyOpeningMoves,legacyOpeningRatio,legacyValidationPassed," +
                "legacyDifficultyPassed,legacyShapeCoveragePassed,legacyShapeQualityPassed," +
                "legacySolvable,legacyHitLimit,legacySolutionCount,legacySolutionDistance," +
                "legacyBuildMs,legacyVerifyMs,legacyTotalMs," +
                "legacyAllocatedBytes,legacyFingerprint,legacyGeometryFingerprint," +
                "legacyRepeatMatched,constructiveGeneratorSucceeded,constructiveCreated," +
                "constructiveSucceeded,constructiveVehicles,constructiveGarages," +
                "constructiveOpeningMoves,constructiveOpeningRatio,constructiveValidationPassed," +
                "constructiveDifficultyPassed,constructiveShapeCoveragePassed," +
                "constructiveShapeQualityPassed,constructiveSolvable,constructiveHitLimit," +
                "constructiveSolutionCount,constructiveSolutionDistance," +
                "generatorWitnessValidated,witnessReplayPassed,witnessLength,candidateSeed," +
                "layoutProbeIndex,placementProbeCount,pathProbeCount,placementProbeLimit," +
                "pathProbeLimit,hitOperationBudget,regularVehicles,garageVehicles," +
                "regularPrefixCount,regularSuffixCount,regularPrefixRatio," +
                "regularSuffixRatio,initialOpeningCount,maximumInitialOpeningCount," +
                "garageDependencyTarget,garageDependencyEvaluated," +
                "garageDependencyActual,suffixOnlyReleaseEvaluated," +
                "suffixOnlyReleasedGarages,suffixOnlyReleaseTarget," +
                "suffixOnlyReleasePassed," +
                "constructiveBuildMs,constructiveVerifyMs,witnessReplayMs,constructiveTotalMs," +
                "constructiveAllocatedBytes,constructiveFingerprint," +
                "constructiveGeometryFingerprint,witnessFingerprint," +
                "constructiveRepeatMatched,failed,verdict,diagnostic" +
                Environment.NewLine,
                new UTF8Encoding(true));
        }

        private static void AppendCsv(
            string path,
            CandidateResult result)
        {
            var values = new object[]
            {
                result.StageNumber,
                result.CandidateIndex,
                result.Difficulty,
                result.Seed,
                result.LegacyCandidateSeed,
                result.CandidateSeedMatched,
                result.RequestedVehicleCount,
                result.RequestedGarageCount,
                result.ConstructiveApplicable,
                result.LegacyCreated,
                result.LegacySucceeded,
                result.LegacyVehicles,
                result.LegacyGarages,
                result.LegacyOpeningMoves,
                result.LegacyOpeningMoveRatio,
                result.LegacyValidationPassed,
                result.LegacyDifficultyContractPassed,
                result.LegacyShapeCoveragePassed,
                result.LegacyShapeQualityPassed,
                result.LegacyIndependentSolvable,
                result.LegacyIndependentHitLimit,
                result.LegacyIndependentSolutionCount,
                result.LegacyIndependentSolutionDistance,
                result.LegacyBuildMilliseconds,
                result.LegacyVerifyMilliseconds,
                result.LegacyTotalMilliseconds,
                result.LegacyAllocatedBytes,
                result.LegacyFingerprint,
                result.LegacyGeometryFingerprint,
                result.LegacyRepeatMatched,
                result.ConstructiveGeneratorSucceeded,
                result.ConstructiveCreated,
                result.ConstructiveSucceeded,
                result.ConstructiveVehicles,
                result.ConstructiveGarages,
                result.ConstructiveOpeningMoves,
                result.ConstructiveOpeningMoveRatio,
                result.ConstructiveValidationPassed,
                result.ConstructiveDifficultyContractPassed,
                result.ConstructiveShapeCoveragePassed,
                result.ConstructiveShapeQualityPassed,
                result.ConstructiveIndependentSolvable,
                result.ConstructiveIndependentHitLimit,
                result.ConstructiveIndependentSolutionCount,
                result.ConstructiveIndependentSolutionDistance,
                result.ConstructiveGeneratorWitnessValidated,
                result.ConstructiveWitnessReplayPassed,
                result.ConstructiveWitnessLength,
                result.ConstructiveCandidateSeed,
                result.ConstructiveLayoutProbeIndex,
                result.ConstructivePlacementProbeCount,
                result.ConstructivePathProbeCount,
                result.ConstructivePlacementProbeLimit,
                result.ConstructivePathProbeLimit,
                result.ConstructiveHitOperationBudget,
                result.ConstructiveRegularVehicleCount,
                result.ConstructiveGarageVehicleCount,
                result.ConstructiveRegularPrefixCount,
                result.ConstructiveRegularSuffixCount,
                result.ConstructiveRegularPrefixRatio,
                result.ConstructiveRegularSuffixRatio,
                result.ConstructiveInitialOpeningCount,
                result.ConstructiveMaximumInitialOpeningCount,
                result.ConstructiveGarageDependencyTarget,
                result.ConstructiveGarageDependencyEvaluated,
                result.ConstructiveGarageDependencyCount,
                result.ConstructiveSuffixOnlyReleaseEvaluated,
                result.ConstructiveSuffixOnlyReleasedGarageCount,
                result.ConstructiveSuffixOnlyReleaseTarget,
                result.ConstructiveSuffixOnlyReleasePassed,
                result.ConstructiveBuildMilliseconds,
                result.ConstructiveVerifyMilliseconds,
                result.ConstructiveWitnessReplayMilliseconds,
                result.ConstructiveTotalMilliseconds,
                result.ConstructiveAllocatedBytes,
                result.ConstructiveFingerprint,
                result.ConstructiveGeometryFingerprint,
                result.ConstructiveWitnessFingerprint,
                result.ConstructiveRepeatMatched,
                result.Failed,
                result.Verdict,
                result.Diagnostic
            };
            var builder =
                new StringBuilder(2048);
            for (var index = 0;
                index < values.Length;
                index++)
            {
                AppendCsvValue(
                    builder,
                    values[index],
                    index ==
                    values.Length - 1);
            }

            File.AppendAllText(
                path,
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static void AppendCsvValue(
            StringBuilder builder,
            object value,
            bool endOfLine)
        {
            var text =
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture) ??
                string.Empty;
            builder.Append('"')
                .Append(
                    text.Replace(
                        "\"",
                        "\"\""))
                .Append('"')
                .Append(
                    endOfLine
                        ? Environment.NewLine
                        : ",");
        }

        private static void WriteSummary(
            string path,
            ComparisonSummary summary)
        {
            EnsureParentDirectory(path);
            File.WriteAllText(
                path,
                JsonUtility.ToJson(
                    summary,
                    true),
                new UTF8Encoding(true));
        }

        private static Options CreateOptions(
            int[] stages,
            string specification,
            int nodeLimit,
            int layoutProbeCount,
            string output)
        {
            if (stages == null ||
                stages.Length == 0)
            {
                throw new ArgumentException(
                    "At least one stage is required.");
            }

            stages = stages
                .Where(stage => stage > 0)
                .Distinct()
                .OrderBy(stage => stage)
                .ToArray();
            var paths =
                ResolveOutputPaths(
                    output,
                    specification);
            return new Options
            {
                Stages = stages,
                StageSpecification =
                    specification,
                LegacyNodeLimit =
                    Mathf.Max(
                        1,
                        nodeLimit),
                LayoutProbeCount =
                    Mathf.Clamp(
                        layoutProbeCount,
                        1,
                        MaximumRuntimeVehicleGenerationAttempts),
                CsvPath = paths.csv,
                SummaryPath = paths.summary
            };
        }

        private static (
            string csv,
            string summary)
            ResolveOutputPaths(
                string output,
                string specification)
        {
            var projectRoot =
                Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        ".."));
            var safeSpecification =
                new string(
                    (specification ??
                     "targeted")
                    .Select(character =>
                        char.IsLetterOrDigit(character)
                            ? character
                            : '-')
                    .ToArray());
            var csv =
                string.IsNullOrWhiteSpace(
                    output)
                    ? Path.Combine(
                        projectRoot,
                        "Build",
                        "Validation",
                        $"runtime-constructive-ab-{safeSpecification}.csv")
                    : output;
            if (!Path.IsPathRooted(csv))
            {
                csv =
                    Path.GetFullPath(
                        Path.Combine(
                            projectRoot,
                            csv));
            }

            if (!string.Equals(
                    Path.GetExtension(csv),
                    ".csv",
                    StringComparison.OrdinalIgnoreCase))
            {
                csv += ".csv";
            }

            return (
                csv,
                Path.ChangeExtension(
                    csv,
                    ".summary.json"));
        }

        private static int[] ParseStages(
            string value)
        {
            var stages =
                new List<int>();
            var tokens =
                value.Split(
                    new[] { ',', ';', ' ' },
                    StringSplitOptions
                        .RemoveEmptyEntries);
            for (var index = 0;
                index < tokens.Length;
                index++)
            {
                if (!int.TryParse(
                        tokens[index],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var stage) ||
                    stage <= 0)
                {
                    throw new ArgumentException(
                        $"Invalid stage '{tokens[index]}'.");
                }

                stages.Add(stage);
            }

            return stages
                .Distinct()
                .OrderBy(stage => stage)
                .ToArray();
        }

        private static int[] CreateRange(
            int start,
            int end)
        {
            if (start <= 0 ||
                end < start ||
                end - start > 10000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(start),
                    $"Invalid stage range {start}-{end}.");
            }

            return Enumerable.Range(
                    start,
                    end - start + 1)
                .ToArray();
        }

        private static double Percentile(
            IReadOnlyList<double> sortedValues,
            double percentile)
        {
            if (sortedValues == null ||
                sortedValues.Count == 0)
            {
                return 0d;
            }

            var index =
                (int)Math.Ceiling(
                    percentile *
                    sortedValues.Count) - 1;
            return sortedValues[
                Mathf.Clamp(
                    index,
                    0,
                    sortedValues.Count - 1)];
        }

        private static double ToMilliseconds(
            long ticks)
        {
            return ticks *
                1000d /
                Stopwatch.Frequency;
        }

        private static string AppendDiagnostic(
            string current,
            string addition)
        {
            if (string.IsNullOrWhiteSpace(
                    addition))
            {
                return current ??
                    string.Empty;
            }

            return string.IsNullOrWhiteSpace(
                    current)
                ? addition
                : $"{current} | {addition}";
        }

        private static string Unwrap(
            Exception exception)
        {
            while (exception is
                       TargetInvocationException target &&
                   target.InnerException != null)
            {
                exception =
                    target.InnerException;
            }

            return exception.ToString();
        }

        private static void DestroyLevel(
            LevelData level)
        {
            if (level != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    level);
            }
        }

        private static void EnsureParentDirectory(
            string path)
        {
            var directory =
                Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }
        }

        private static int ReadIntArgument(
            IReadOnlyList<string> args,
            string name,
            int fallback)
        {
            for (var index = 0;
                index + 1 < args.Count;
                index++)
            {
                if (string.Equals(
                        args[index],
                        name,
                        StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(
                        args[index + 1],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value))
                {
                    return value;
                }
            }

            return fallback;
        }

        private static string ReadStringArgument(
            IReadOnlyList<string> args,
            string name)
        {
            for (var index = 0;
                index + 1 < args.Count;
                index++)
            {
                if (string.Equals(
                        args[index],
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return null;
        }
    }
}
#endif
