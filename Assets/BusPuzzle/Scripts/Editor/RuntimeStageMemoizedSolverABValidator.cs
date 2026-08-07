#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BusPuzzle.EditorTools
{
    /// <summary>
    /// Compares the shipped bounded solution counter with the opt-in memoized
    /// witness solver on identical, transient runtime candidates. This validator
    /// never enables the memoized solver in production.
    /// </summary>
    public static class RuntimeStageMemoizedSolverABValidator
    {
        private const string StageGenerationConfigPath =
            "Assets/BusPuzzle/Resources/Levels/StageGenerationConfig.asset";
        private const string StageListArgument =
            "-busPuzzleMemoStages";
        private const string StartArgument =
            "-busPuzzleMemoStart";
        private const string EndArgument =
            "-busPuzzleMemoEnd";
        private const string LegacyNodeLimitArgument =
            "-busPuzzleLegacyNodeLimit";
        private const string MemoNodeLimitArgument =
            "-busPuzzleMemoNodeLimit";
        private const string MemoStateLimitArgument =
            "-busPuzzleMemoStateLimit";
        private const string OracleNodeLimitArgument =
            "-busPuzzleOracleNodeLimit";
        private const string OracleAllArgument =
            "-busPuzzleOracleAll";
        private const string OutputArgument =
            "-busPuzzleMemoOutput";
        private const int DefaultLegacyNodeLimit = 2048;
        private const int DefaultMemoNodeLimit = 2048;
        private const int DefaultMemoStateLimit = 2048;
        private const int DefaultTargetedOracleNodeLimit = 200000;
        private const int DefaultRangeOracleNodeLimit = 20000;
        private const int MaximumAcceptedLimit = 2000000;
        private const int MaximumRuntimeCandidateAttempts = 4;
        private const int MaximumRuntimeVehicleGenerationAttempts = 6;
        private const float MinimumVehicleTargetRatio = 0.75f;

        private static readonly int[] DefaultTargetStages =
        {
            251,
            260,
            812
        };

        [Serializable]
        private sealed class ComparisonSummary
        {
            public string startedAtUtc;
            public string finishedAtUtc;
            public string unityVersion;
            public string stageSpecification;
            public int requestedStageCount;
            public int applicableStageCount;
            public int skippedStageCount;
            public int candidateCount;
            public int nullCandidateCount;
            public int basicEligibleCandidateCount;
            public int legacySolvedCandidateCount;
            public int memoSolvedCandidateCount;
            public int recoveredCandidateCount;
            public int regressedCandidateCount;
            public int witnessFailureCount;
            public int deterministicFailureCount;
            public int contradictionCount;
            public int oracleRunCount;
            public int oracleInconclusiveCount;
            public int legacySolvedStageCount;
            public int memoSolvedStageCount;
            public int recoveredStageCount;
            public int regressedStageCount;
            public int memoUnsolvedStageCount;
            public int legacyNodeLimit;
            public int memoNodeLimit;
            public int memoStateLimit;
            public int oracleNodeLimit;
            public bool oracleAll;
            public double legacyTotalMilliseconds;
            public double memoTotalMilliseconds;
            public double legacyP95Milliseconds;
            public double memoP95Milliseconds;
            public double legacyP99Milliseconds;
            public double memoP99Milliseconds;
            public bool stageGenerationRateImproved;
            public bool p99Improved;
            public bool passed;
            public string csvPath;
            public StageDigest[] stages;
        }

        [Serializable]
        private sealed class StageDigest
        {
            public int stageNumber;
            public string difficulty;
            public int seed;
            public int requestedGarageCount;
            public int legacyAcceptedCandidate;
            public int memoAcceptedCandidate;
            public string verdict;
        }

        private sealed class CandidateResult
        {
            public int StageNumber;
            public int CandidateIndex;
            public string Difficulty = string.Empty;
            public int Seed;
            public int RequestedGarageCount;
            public int ActualGarageCount;
            public int VehicleCount;
            public bool BasicEligible;
            public bool CandidateCreated;
            public string Fingerprint = string.Empty;
            public double BuildMilliseconds;
            public bool LegacySolvable;
            public bool LegacyHitLimit;
            public double LegacyMilliseconds;
            public bool MemoSolvable;
            public bool MemoHitNodeLimit;
            public bool MemoHitStateLimit;
            public int MemoVisitedNodes;
            public int MemoStateCount;
            public int MemoHits;
            public int MemoWitnessLength;
            public bool MemoWitnessValidated;
            public double MemoMilliseconds;
            public bool MemoRepeatMatched;
            public bool OracleRan;
            public bool OracleSolvable;
            public bool OracleHitLimit;
            public double OracleMilliseconds;
            public string Verdict = string.Empty;
            public string Diagnostic = string.Empty;
            public bool Failed;
        }

        private sealed class Options
        {
            public int[] Stages = Array.Empty<int>();
            public string StageSpecification = string.Empty;
            public int LegacyNodeLimit;
            public int MemoNodeLimit;
            public int MemoStateLimit;
            public int OracleNodeLimit;
            public bool OracleAll;
            public string CsvPath = string.Empty;
            public string SummaryPath = string.Empty;
        }

        [MenuItem(
            "Bus Puzzle/Validation/Compare Memoized Solver (251, 260, 812)")]
        private static void RunTargetedFromMenu()
        {
            Run(
                CreateDefaultTargetedOptions(null),
                false,
                true);
        }

        [MenuItem(
            "Bus Puzzle/Validation/Compare Memoized Solver (201-1000)")]
        private static void RunFullRangeFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Long-running solver comparison",
                    "This creates up to four transient candidates for every " +
                    "SuperHard + Garage stage from 201 through 1000. Continue?",
                    "Continue",
                    "Cancel"))
            {
                return;
            }

            Run(
                CreateRangeOptions(
                    201,
                    1000,
                    null,
                    DefaultRangeOracleNodeLimit),
                true,
                true);
        }

        /// <summary>
        /// Batch entry point. Examples:
        /// -busPuzzleMemoStages 251,260,812
        /// -busPuzzleMemoStart 201 -busPuzzleMemoEnd 1000
        /// -busPuzzleLegacyNodeLimit 2048
        /// -busPuzzleMemoNodeLimit 2048
        /// -busPuzzleMemoStateLimit 2048
        /// -busPuzzleOracleNodeLimit 200000
        /// -busPuzzleOracleAll false
        /// -busPuzzleMemoOutput Build/Validation/memo-ab.csv
        /// </summary>
        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var hasRange =
                HasArgument(args, StartArgument) ||
                HasArgument(args, EndArgument);
            var explicitStageList =
                ReadStringArgument(
                    args,
                    StageListArgument);
            int[] stages;
            string stageSpecification;
            var defaultOracleLimit =
                DefaultTargetedOracleNodeLimit;
            if (!string.IsNullOrWhiteSpace(
                    explicitStageList))
            {
                stages = ParseStageList(
                    explicitStageList);
                stageSpecification =
                    string.Join(",", stages);
            }
            else if (hasRange)
            {
                var start = ReadIntArgument(
                    args,
                    StartArgument,
                    201);
                var end = ReadIntArgument(
                    args,
                    EndArgument,
                    1000);
                stages = CreateStageRange(
                    start,
                    end);
                stageSpecification =
                    $"{start}-{end}";
                defaultOracleLimit =
                    DefaultRangeOracleNodeLimit;
            }
            else
            {
                stages =
                    (int[])DefaultTargetStages.Clone();
                stageSpecification =
                    string.Join(",", stages);
            }

            var output = ReadStringArgument(
                args,
                OutputArgument);
            var options = CreateOptions(
                stages,
                stageSpecification,
                ReadIntArgument(
                    args,
                    LegacyNodeLimitArgument,
                    DefaultLegacyNodeLimit),
                ReadIntArgument(
                    args,
                    MemoNodeLimitArgument,
                    DefaultMemoNodeLimit),
                ReadIntArgument(
                    args,
                    MemoStateLimitArgument,
                    DefaultMemoStateLimit),
                ReadIntArgument(
                    args,
                    OracleNodeLimitArgument,
                    defaultOracleLimit),
                ReadBoolArgument(
                    args,
                    OracleAllArgument,
                    false),
                output);
            Run(
                options,
                false,
                true);
        }

        private static void Run(
            Options options,
            bool allowCancel,
            bool failOnGate)
        {
            var config =
                AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(
                    StageGenerationConfigPath);
            if (config == null)
            {
                throw new BuildFailedException(
                    "Memoized solver comparison requires StageGenerationConfig.");
            }

            InitializeCsv(options.CsvPath);
            var startedAt = DateTime.UtcNow;
            var candidateResults =
                new List<CandidateResult>();
            var stageDigests =
                new List<StageDigest>();
            var cancelled = false;
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
                            "Memoized Solver A/B",
                            $"Stage {stageNumber} " +
                            $"({stageIndex + 1}/{options.Stages.Length})",
                            (float)stageIndex /
                            Mathf.Max(1, options.Stages.Length)))
                    {
                        cancelled = true;
                        break;
                    }

                    var request =
                        StageGenerationPlanner.CreateRequest(
                            config,
                            stageNumber);
                    if (request.Difficulty !=
                            LevelDifficulty.SuperHard ||
                        request.GarageCount <= 0)
                    {
                        stageDigests.Add(
                            new StageDigest
                            {
                                stageNumber =
                                    stageNumber,
                                difficulty =
                                    request.Difficulty.ToString(),
                                seed = request.Seed,
                                requestedGarageCount =
                                    request.GarageCount,
                                legacyAcceptedCandidate = -1,
                                memoAcceptedCandidate = -1,
                                verdict =
                                    "skipped_not_superhard_garage"
                            });
                        continue;
                    }

                    CompareStage(
                        config,
                        request,
                        options,
                        candidateResults,
                        stageDigests);
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
                candidateResults,
                stageDigests,
                cancelled);
            WriteSummary(
                options.SummaryPath,
                summary);
            var message =
                $"Memoized solver A/B {(summary.passed ? "passed" : "failed")}: " +
                $"applicable stages={summary.applicableStageCount}, " +
                $"legacy/memo solved stages={summary.legacySolvedStageCount}/" +
                $"{summary.memoSolvedStageCount}, recovered={summary.recoveredStageCount}, " +
                $"regressed={summary.regressedStageCount}, " +
                $"witness failures={summary.witnessFailureCount}, " +
                $"determinism failures={summary.deterministicFailureCount}, " +
                $"generation rate improved={summary.stageGenerationRateImproved}, " +
                $"P99 improved={summary.p99Improved}. " +
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
            ICollection<CandidateResult> allResults,
            ICollection<StageDigest> stageDigests)
        {
            var attempts = Mathf.Clamp(
                config.RuntimeCandidateAttemptsPerStage,
                1,
                MaximumRuntimeCandidateAttempts);
            var vehicleAttempts = Mathf.Clamp(
                config.RuntimeVehicleGenerationAttempts,
                1,
                MaximumRuntimeVehicleGenerationAttempts);
            var legacyAcceptedCandidate = -1;
            var memoAcceptedCandidate = -1;
            for (var candidateIndex = 0;
                candidateIndex < attempts;
                candidateIndex++)
            {
                var result = CompareCandidate(
                    config,
                    request,
                    candidateIndex,
                    vehicleAttempts,
                    options);
                allResults.Add(result);
                AppendCsv(
                    options.CsvPath,
                    result);

                if (result.BasicEligible &&
                    result.LegacySolvable &&
                    legacyAcceptedCandidate < 0)
                {
                    legacyAcceptedCandidate =
                        candidateIndex;
                }

                if (result.BasicEligible &&
                    result.MemoSolvable &&
                    result.MemoWitnessValidated &&
                    memoAcceptedCandidate < 0)
                {
                    memoAcceptedCandidate =
                        candidateIndex;
                }
            }

            var verdict =
                memoAcceptedCandidate < 0
                    ? legacyAcceptedCandidate >= 0
                        ? "stage_regressed"
                        : "stage_memo_unsolved"
                    : legacyAcceptedCandidate < 0
                        ? "stage_recovered"
                        : "stage_solved";
            stageDigests.Add(
                new StageDigest
                {
                    stageNumber =
                        request.StageNumber,
                    difficulty =
                        request.Difficulty.ToString(),
                    seed = request.Seed,
                    requestedGarageCount =
                        request.GarageCount,
                    legacyAcceptedCandidate =
                        legacyAcceptedCandidate,
                    memoAcceptedCandidate =
                        memoAcceptedCandidate,
                    verdict = verdict
                });
        }

        private static CandidateResult CompareCandidate(
            StageGenerationConfig config,
            StageGenerationRequest request,
            int candidateIndex,
            int vehicleAttempts,
            Options options)
        {
            var result = new CandidateResult
            {
                StageNumber = request.StageNumber,
                CandidateIndex = candidateIndex,
                Difficulty = request.Difficulty.ToString(),
                Seed = request.Seed,
                RequestedGarageCount = request.GarageCount,
                MemoRepeatMatched = true
            };
            LevelData candidate = null;
            try
            {
                var buildWatch = Stopwatch.StartNew();
                candidate =
                    LevelGenerator.CreateRuntimeStage(
                        request,
                        config.SuperHardGarageRule,
                        candidateIndex,
                        vehicleAttempts,
                        false,
                        false);
                buildWatch.Stop();
                result.BuildMilliseconds =
                    ToMilliseconds(
                        buildWatch.ElapsedTicks);
                if (candidate == null)
                {
                    result.Verdict =
                        "candidate_null";
                    result.Diagnostic =
                        "Runtime candidate builder returned null.";
                    return result;
                }

                result.CandidateCreated = true;
                result.ActualGarageCount =
                    candidate.Garages.Count;
                result.VehicleCount =
                    candidate.AllVehicles.Count;
                result.Fingerprint =
                    CreateFingerprint(
                        candidate);
                var minimumVehicleCount =
                    Mathf.CeilToInt(
                        request.Profile.TargetVehicleCount *
                        MinimumVehicleTargetRatio);
                result.BasicEligible =
                    result.VehicleCount >=
                        minimumVehicleCount &&
                    ShapeLibraryVehicleCoverage.IsSatisfied(
                        request.Profile,
                        request.VehicleLayoutVariantIndex,
                        candidate.Buses.Count);

                StageSolutionAnalysis legacy;
                StageMemoizedWitnessAnalysis memo;
                var legacyTicks = 0L;
                var memoTicks = 0L;
                if (((request.StageNumber +
                        candidateIndex) & 1) == 0)
                {
                    legacy = RunLegacy(
                        candidate,
                        options.LegacyNodeLimit,
                        out legacyTicks);
                    memo = RunMemo(
                        candidate,
                        options,
                        out memoTicks);
                }
                else
                {
                    memo = RunMemo(
                        candidate,
                        options,
                        out memoTicks);
                    legacy = RunLegacy(
                        candidate,
                        options.LegacyNodeLimit,
                        out legacyTicks);
                }

                result.LegacySolvable =
                    legacy.IsSolvable;
                result.LegacyHitLimit =
                    legacy.HitLimit;
                result.LegacyMilliseconds =
                    ToMilliseconds(
                        legacyTicks);
                CopyMemoResult(
                    memo,
                    memoTicks,
                    result);

                var repeatedMemo = RunMemo(
                    candidate,
                    options,
                    out _);
                result.MemoRepeatMatched =
                    MemoResultsMatch(
                        memo,
                        repeatedMemo);
                if (!result.MemoRepeatMatched)
                {
                    result.Failed = true;
                    result.Verdict =
                        "memo_nondeterministic";
                    result.Diagnostic =
                        "Repeated memoized analysis differed on identical candidate data.";
                    return result;
                }

                if (memo.IsSolvable &&
                    (!memo.WitnessValidated ||
                     memo.Witness.Count !=
                        result.VehicleCount))
                {
                    result.Failed = true;
                    result.Verdict =
                        "memo_invalid_witness";
                    result.Diagnostic =
                        $"Memo reported solvable with witness " +
                        $"{memo.Witness.Count}/{result.VehicleCount}, " +
                        $"validated={memo.WitnessValidated}.";
                    return result;
                }

                var disagreement =
                    legacy.IsSolvable !=
                    memo.IsSolvable;
                if (options.OracleNodeLimit > 0 &&
                    (options.OracleAll ||
                     disagreement))
                {
                    result.OracleRan = true;
                    var oracle = RunLegacy(
                        candidate,
                        options.OracleNodeLimit,
                        out var oracleTicks);
                    result.OracleSolvable =
                        oracle.IsSolvable;
                    result.OracleHitLimit =
                        oracle.HitLimit;
                    result.OracleMilliseconds =
                        ToMilliseconds(
                            oracleTicks);
                }

                ClassifyResult(
                    legacy,
                    memo,
                    result);
                return result;
            }
            catch (Exception exception)
            {
                result.Failed = true;
                result.Verdict =
                    "exception";
                result.Diagnostic =
                    exception.ToString();
                return result;
            }
            finally
            {
                if (candidate != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        candidate);
                }
            }
        }

        private static void ClassifyResult(
            StageSolutionAnalysis legacy,
            StageMemoizedWitnessAnalysis memo,
            CandidateResult result)
        {
            if (legacy.IsSolvable &&
                !memo.IsSolvable)
            {
                result.Failed = true;
                result.Verdict =
                    "memo_regression";
                result.Diagnostic =
                    "Legacy found a solution but memo did not.";
                return;
            }

            if (memo.IsSolvable &&
                result.OracleRan &&
                !result.OracleSolvable &&
                !result.OracleHitLimit)
            {
                result.Failed = true;
                result.Verdict =
                    "oracle_contradiction";
                result.Diagnostic =
                    "Memo witness conflicts with an exhaustively negative high-budget legacy result.";
                return;
            }

            if (!legacy.IsSolvable &&
                memo.IsSolvable)
            {
                result.Verdict =
                    result.OracleRan &&
                    result.OracleSolvable
                        ? "memo_recovered_oracle_confirmed"
                        : result.OracleRan &&
                          result.OracleHitLimit
                            ? "memo_recovered_oracle_inconclusive"
                            : "memo_recovered_witness_confirmed";
                return;
            }

            if (legacy.IsSolvable)
            {
                result.Verdict =
                    "both_solved";
                return;
            }

            result.Verdict =
                "both_unsolved_within_budget";
        }

        private static StageSolutionAnalysis RunLegacy(
            LevelData candidate,
            int nodeLimit,
            out long elapsedTicks)
        {
            var watch = Stopwatch.StartNew();
            var analysis =
                StageSolutionAnalyzer.Analyze(
                    candidate.Buses,
                    candidate.Garages,
                    1,
                    nodeLimit);
            watch.Stop();
            elapsedTicks =
                watch.ElapsedTicks;
            return analysis;
        }

        private static StageMemoizedWitnessAnalysis RunMemo(
            LevelData candidate,
            Options options,
            out long elapsedTicks)
        {
            var watch = Stopwatch.StartNew();
            var analysis =
                StageSolutionAnalyzer
                    .AnalyzeMemoizedWitness(
                        candidate.Buses,
                        candidate.Garages,
                        options.MemoNodeLimit,
                        options.MemoStateLimit);
            watch.Stop();
            elapsedTicks =
                watch.ElapsedTicks;
            return analysis;
        }

        private static void CopyMemoResult(
            StageMemoizedWitnessAnalysis memo,
            long elapsedTicks,
            CandidateResult result)
        {
            result.MemoSolvable =
                memo.IsSolvable;
            result.MemoHitNodeLimit =
                memo.HitNodeLimit;
            result.MemoHitStateLimit =
                memo.HitMemoLimit;
            result.MemoVisitedNodes =
                memo.VisitedNodes;
            result.MemoStateCount =
                memo.MemoizedStateCount;
            result.MemoHits =
                memo.MemoHits;
            result.MemoWitnessLength =
                memo.Witness.Count;
            result.MemoWitnessValidated =
                memo.WitnessValidated;
            result.MemoMilliseconds =
                ToMilliseconds(
                    elapsedTicks);
        }

        private static bool MemoResultsMatch(
            StageMemoizedWitnessAnalysis first,
            StageMemoizedWitnessAnalysis second)
        {
            if (first.IsSolvable != second.IsSolvable ||
                first.SolutionCount != second.SolutionCount ||
                first.HitNodeLimit != second.HitNodeLimit ||
                first.HitMemoLimit != second.HitMemoLimit ||
                first.VisitedNodes != second.VisitedNodes ||
                first.MemoizedStateCount !=
                    second.MemoizedStateCount ||
                first.MemoHits != second.MemoHits ||
                first.WitnessValidated !=
                    second.WitnessValidated ||
                first.Witness.Count !=
                    second.Witness.Count)
            {
                return false;
            }

            for (var index = 0;
                index < first.Witness.Count;
                index++)
            {
                var firstStep =
                    first.Witness[index];
                var secondStep =
                    second.Witness[index];
                if (firstStep.VehicleIndex !=
                        secondStep.VehicleIndex ||
                    firstStep.GarageIndex !=
                        secondStep.GarageIndex ||
                    firstStep.GarageProgress !=
                        secondStep.GarageProgress)
                {
                    return false;
                }
            }

            return true;
        }

        private static ComparisonSummary CreateSummary(
            DateTime startedAt,
            DateTime finishedAt,
            Options options,
            IReadOnlyList<CandidateResult> candidates,
            IReadOnlyList<StageDigest> stages,
            bool cancelled)
        {
            var legacyTimes = candidates
                .Where(candidate =>
                    candidate.CandidateCreated)
                .Select(candidate =>
                    candidate.LegacyMilliseconds)
                .OrderBy(value => value)
                .ToArray();
            var memoTimes = candidates
                .Where(candidate =>
                    candidate.CandidateCreated)
                .Select(candidate =>
                    candidate.MemoMilliseconds)
                .OrderBy(value => value)
                .ToArray();
            var applicableStages = stages
                .Where(stage =>
                    stage.verdict !=
                    "skipped_not_superhard_garage")
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
                stageSpecification =
                    options.StageSpecification,
                requestedStageCount =
                    stages.Count,
                applicableStageCount =
                    applicableStages.Length,
                skippedStageCount =
                    stages.Count -
                    applicableStages.Length,
                candidateCount =
                    candidates.Count,
                nullCandidateCount =
                    candidates.Count(candidate =>
                        !candidate.CandidateCreated),
                basicEligibleCandidateCount =
                    candidates.Count(candidate =>
                        candidate.BasicEligible),
                legacySolvedCandidateCount =
                    candidates.Count(candidate =>
                        candidate.LegacySolvable),
                memoSolvedCandidateCount =
                    candidates.Count(candidate =>
                        candidate.MemoSolvable),
                recoveredCandidateCount =
                    candidates.Count(candidate =>
                        candidate.MemoSolvable &&
                        !candidate.LegacySolvable),
                regressedCandidateCount =
                    candidates.Count(candidate =>
                        candidate.LegacySolvable &&
                        !candidate.MemoSolvable),
                witnessFailureCount =
                    candidates.Count(candidate =>
                        candidate.Verdict ==
                        "memo_invalid_witness"),
                deterministicFailureCount =
                    candidates.Count(candidate =>
                        candidate.Verdict ==
                        "memo_nondeterministic"),
                contradictionCount =
                    candidates.Count(candidate =>
                        candidate.Verdict ==
                        "oracle_contradiction"),
                oracleRunCount =
                    candidates.Count(candidate =>
                        candidate.OracleRan),
                oracleInconclusiveCount =
                    candidates.Count(candidate =>
                        candidate.OracleRan &&
                        !candidate.OracleSolvable &&
                        candidate.OracleHitLimit),
                legacySolvedStageCount =
                    applicableStages.Count(stage =>
                        stage.legacyAcceptedCandidate >= 0),
                memoSolvedStageCount =
                    applicableStages.Count(stage =>
                        stage.memoAcceptedCandidate >= 0),
                recoveredStageCount =
                    applicableStages.Count(stage =>
                        stage.verdict ==
                        "stage_recovered"),
                regressedStageCount =
                    applicableStages.Count(stage =>
                        stage.verdict ==
                        "stage_regressed"),
                memoUnsolvedStageCount =
                    applicableStages.Count(stage =>
                        stage.memoAcceptedCandidate < 0),
                legacyNodeLimit =
                    options.LegacyNodeLimit,
                memoNodeLimit =
                    options.MemoNodeLimit,
                memoStateLimit =
                    options.MemoStateLimit,
                oracleNodeLimit =
                    options.OracleNodeLimit,
                oracleAll =
                    options.OracleAll,
                legacyTotalMilliseconds =
                    legacyTimes.Sum(),
                memoTotalMilliseconds =
                    memoTimes.Sum(),
                legacyP95Milliseconds =
                    Percentile(
                        legacyTimes,
                        0.95d),
                memoP95Milliseconds =
                    Percentile(
                        memoTimes,
                        0.95d),
                legacyP99Milliseconds =
                    Percentile(
                        legacyTimes,
                        0.99d),
                memoP99Milliseconds =
                    Percentile(
                        memoTimes,
                        0.99d),
                stageGenerationRateImproved = false,
                p99Improved = false,
                csvPath =
                    options.CsvPath,
                stages =
                    stages.ToArray()
            };
            summary.stageGenerationRateImproved =
                summary.memoSolvedStageCount >
                summary.legacySolvedStageCount;
            summary.p99Improved =
                summary.memoP99Milliseconds <
                summary.legacyP99Milliseconds;
            summary.passed =
                !cancelled &&
                candidates.All(candidate =>
                    !candidate.Failed) &&
                summary.regressedStageCount == 0 &&
                summary.stageGenerationRateImproved &&
                summary.p99Improved;
            return summary;
        }

        private static Options CreateDefaultTargetedOptions(
            string output)
        {
            return CreateOptions(
                (int[])DefaultTargetStages.Clone(),
                string.Join(",", DefaultTargetStages),
                DefaultLegacyNodeLimit,
                DefaultMemoNodeLimit,
                DefaultMemoStateLimit,
                DefaultTargetedOracleNodeLimit,
                false,
                output);
        }

        private static Options CreateRangeOptions(
            int start,
            int end,
            string output,
            int oracleLimit)
        {
            return CreateOptions(
                CreateStageRange(
                    start,
                    end),
                $"{start}-{end}",
                DefaultLegacyNodeLimit,
                DefaultMemoNodeLimit,
                DefaultMemoStateLimit,
                oracleLimit,
                false,
                output);
        }

        private static Options CreateOptions(
            int[] stages,
            string stageSpecification,
            int legacyNodeLimit,
            int memoNodeLimit,
            int memoStateLimit,
            int oracleNodeLimit,
            bool oracleAll,
            string output)
        {
            if (stages == null ||
                stages.Length == 0)
            {
                throw new BuildFailedException(
                    "Memoized solver comparison requires at least one stage.");
            }

            stages = stages
                .Where(stage =>
                    stage > 0)
                .Distinct()
                .OrderBy(stage =>
                    stage)
                .ToArray();
            if (stages.Length == 0)
            {
                throw new BuildFailedException(
                    "Every requested stage was invalid.");
            }

            var paths = ResolveOutputPaths(
                output,
                stageSpecification);
            return new Options
            {
                Stages = stages,
                StageSpecification =
                    stageSpecification,
                LegacyNodeLimit =
                    Mathf.Clamp(
                        legacyNodeLimit,
                        1,
                        MaximumAcceptedLimit),
                MemoNodeLimit =
                    Mathf.Clamp(
                        memoNodeLimit,
                        1,
                        MaximumAcceptedLimit),
                MemoStateLimit =
                    Mathf.Clamp(
                        memoStateLimit,
                        1,
                        MaximumAcceptedLimit),
                OracleNodeLimit =
                    Mathf.Clamp(
                        oracleNodeLimit,
                        0,
                        MaximumAcceptedLimit),
                OracleAll =
                    oracleAll,
                CsvPath =
                    paths.csv,
                SummaryPath =
                    paths.summary
            };
        }

        private static int[] ParseStageList(
            string value)
        {
            var tokens = value.Split(
                new[]
                {
                    ',',
                    ';',
                    ' ',
                    '\t'
                },
                StringSplitOptions.RemoveEmptyEntries);
            var stages = new List<int>(
                tokens.Length);
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
                    throw new BuildFailedException(
                        $"Invalid stage '{tokens[index]}' in {StageListArgument}.");
                }

                stages.Add(stage);
            }

            return stages
                .Distinct()
                .OrderBy(stage =>
                    stage)
                .ToArray();
        }

        private static int[] CreateStageRange(
            int start,
            int end)
        {
            if (start <= 0 ||
                end < start ||
                end - start > 99999)
            {
                throw new BuildFailedException(
                    $"Invalid memoized solver range {start}-{end}.");
            }

            var stages =
                new int[end - start + 1];
            for (var index = 0;
                index < stages.Length;
                index++)
            {
                stages[index] =
                    start + index;
            }

            return stages;
        }

        private static (
            string csv,
            string summary)
            ResolveOutputPaths(
                string output,
                string stageSpecification)
        {
            var projectRoot =
                Directory.GetParent(
                    Application.dataPath)?.FullName ??
                Environment.CurrentDirectory;
            var safeSpecification =
                new string(
                    (stageSpecification ??
                     "targeted")
                    .Select(character =>
                        char.IsLetterOrDigit(character)
                            ? character
                            : '-')
                    .ToArray());
            var csv = string.IsNullOrWhiteSpace(
                    output)
                ? Path.Combine(
                    projectRoot,
                    "Build",
                    "Validation",
                    $"runtime-memo-ab-{safeSpecification}.csv")
                : output;
            if (!Path.IsPathRooted(csv))
            {
                csv = Path.GetFullPath(
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

        private static void InitializeCsv(
            string path)
        {
            EnsureParentDirectory(
                path);
            File.WriteAllText(
                path,
                "stage,candidate,difficulty,seed,requestedGarages,actualGarages,vehicles," +
                "basicEligible,candidateCreated,fingerprint,buildMs,legacySolvable,legacyHitLimit," +
                "legacyMs,memoSolvable,memoHitNodeLimit,memoHitStateLimit,memoVisitedNodes," +
                "memoStates,memoHits,memoWitnessLength,memoWitnessValidated,memoMs," +
                "memoRepeatMatched,oracleRan,oracleSolvable,oracleHitLimit,oracleMs," +
                "failed,verdict,diagnostic" +
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
                result.RequestedGarageCount,
                result.ActualGarageCount,
                result.VehicleCount,
                result.BasicEligible,
                result.CandidateCreated,
                result.Fingerprint,
                result.BuildMilliseconds,
                result.LegacySolvable,
                result.LegacyHitLimit,
                result.LegacyMilliseconds,
                result.MemoSolvable,
                result.MemoHitNodeLimit,
                result.MemoHitStateLimit,
                result.MemoVisitedNodes,
                result.MemoStateCount,
                result.MemoHits,
                result.MemoWitnessLength,
                result.MemoWitnessValidated,
                result.MemoMilliseconds,
                result.MemoRepeatMatched,
                result.OracleRan,
                result.OracleSolvable,
                result.OracleHitLimit,
                result.OracleMilliseconds,
                result.Failed,
                result.Verdict,
                result.Diagnostic
            };
            var builder = new StringBuilder(
                512);
            for (var index = 0;
                index < values.Length;
                index++)
            {
                AppendCsvValue(
                    builder,
                    values[index]);
                builder.Append(
                    index + 1 <
                        values.Length
                            ? ','
                            : '\n');
            }

            File.AppendAllText(
                path,
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static void AppendCsvValue(
            StringBuilder builder,
            object value)
        {
            var text =
                value is IFormattable formattable
                    ? formattable.ToString(
                        null,
                        CultureInfo.InvariantCulture)
                    : value?.ToString() ??
                      string.Empty;
            builder.Append('"')
                .Append(
                    text.Replace(
                        "\"",
                        "\"\""))
                .Append('"');
        }

        private static void WriteSummary(
            string path,
            ComparisonSummary summary)
        {
            EnsureParentDirectory(
                path);
            File.WriteAllText(
                path,
                JsonUtility.ToJson(
                    summary,
                    true),
                new UTF8Encoding(true));
        }

        private static void EnsureParentDirectory(
            string path)
        {
            var parent =
                Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(
                    parent))
            {
                Directory.CreateDirectory(
                    parent);
            }
        }

        private static string CreateFingerprint(
            LevelData level)
        {
            var builder = new StringBuilder(
                4096);
            builder.Append("buses=");
            for (var index = 0;
                index < level.Buses.Count;
                index++)
            {
                AppendBus(
                    builder,
                    level.Buses[index]);
            }

            builder.Append(";garages=");
            for (var garageIndex = 0;
                garageIndex < level.Garages.Count;
                garageIndex++)
            {
                var garage =
                    level.Garages[garageIndex];
                builder.Append('[')
                    .Append(garage.GridPosition.x)
                    .Append(',')
                    .Append(garage.GridPosition.y)
                    .Append(',')
                    .Append((int)garage.ExitDirection)
                    .Append('|');
                AppendBus(
                    builder,
                    garage.FrontVehicle);
                for (var queueIndex = 0;
                    queueIndex <
                        garage.QueuedVehicles.Count;
                    queueIndex++)
                {
                    AppendBus(
                        builder,
                        garage.QueuedVehicles[queueIndex]);
                }

                builder.Append(']');
            }

            using (var sha =
                SHA256.Create())
            {
                var bytes = sha.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        builder.ToString()));
                var hash =
                    new StringBuilder(
                        bytes.Length * 2);
                for (var index = 0;
                    index < bytes.Length;
                    index++)
                {
                    hash.Append(
                        bytes[index].ToString(
                            "x2",
                            CultureInfo.InvariantCulture));
                }

                return hash.ToString();
            }
        }

        private static void AppendBus(
            StringBuilder builder,
            BusDefinition bus)
        {
            builder.Append('{')
                .Append((int)bus.Color)
                .Append(',')
                .Append((int)bus.Size)
                .Append(',')
                .Append((int)bus.Direction)
                .Append(',')
                .Append(bus.GridPosition.x)
                .Append(',')
                .Append(bus.GridPosition.y)
                .Append(',')
                .Append(
                    bus.AngleOffsetDegrees.ToString(
                        "R",
                        CultureInfo.InvariantCulture))
                .Append(',')
                .Append(
                    bus.PositionOffsetCells.x.ToString(
                        "R",
                        CultureInfo.InvariantCulture))
                .Append(',')
                .Append(
                    bus.PositionOffsetCells.y.ToString(
                        "R",
                        CultureInfo.InvariantCulture))
                .Append(',')
                .Append(
                    bus.StartsConcealed
                        ? 1
                        : 0)
                .Append('}');
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

        private static bool HasArgument(
            IReadOnlyList<string> args,
            string name)
        {
            for (var index = 0;
                index < args.Count;
                index++)
            {
                if (string.Equals(
                        args[index],
                        name,
                        StringComparison.OrdinalIgnoreCase) ||
                    args[index].StartsWith(
                        name + "=",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int ReadIntArgument(
            IReadOnlyList<string> args,
            string name,
            int fallback)
        {
            var value =
                ReadStringArgument(
                    args,
                    name);
            return int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsed)
                ? parsed
                : fallback;
        }

        private static bool ReadBoolArgument(
            IReadOnlyList<string> args,
            string name,
            bool fallback)
        {
            var value =
                ReadStringArgument(
                    args,
                    name);
            return bool.TryParse(
                    value,
                    out var parsed)
                ? parsed
                : fallback;
        }

        private static string ReadStringArgument(
            IReadOnlyList<string> args,
            string name)
        {
            for (var index = 0;
                index < args.Count;
                index++)
            {
                var argument =
                    args[index];
                if (argument.StartsWith(
                        name + "=",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return argument.Substring(
                        name.Length + 1);
                }

                if (string.Equals(
                        argument,
                        name,
                        StringComparison.OrdinalIgnoreCase) &&
                    index + 1 <
                        args.Count)
                {
                    return args[index + 1];
                }
            }

            return string.Empty;
        }
    }
}
#endif
