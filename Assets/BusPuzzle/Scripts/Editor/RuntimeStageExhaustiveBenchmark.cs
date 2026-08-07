#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace BusPuzzle.EditorTools
{
    /// <summary>
    /// Runs the production background stage generator without playing each level.
    /// The exhaustive pass records per-stage generation/finalization/build timings
    /// and exercises actual BoardView rebuilds around the reported Stage 450 range.
    /// Generated levels are transient and are never added to the player build.
    /// </summary>
    public static class RuntimeStageExhaustiveBenchmark
    {
        private const string GeneratedSequencePath =
            "Assets/BusPuzzle/Resources/Levels/Generated/GeneratedLevelSequence.asset";
        private const string StageGenerationConfigPath =
            "Assets/BusPuzzle/Resources/Levels/StageGenerationConfig.asset";
        private const int DefaultStartStage = 201;
        private const int DefaultEndStage = 1000;
        private const int DefaultBoardStressStartStage = 430;
        private const int DefaultBoardStressEndStage = 470;
        private const int MaximumPerStageWaitMilliseconds = 20000;
        private const int ClearTransitionWaitMilliseconds = 3250;
        private const long MaximumStartMilliseconds = 32;
        private const long MaximumFinalizeMilliseconds = 1500;
        private const string StartArgument = "-busPuzzleBenchmarkStart";
        private const string EndArgument = "-busPuzzleBenchmarkEnd";
        private const string BoardStartArgument = "-busPuzzleBoardStressStart";
        private const string BoardEndArgument = "-busPuzzleBoardStressEnd";
        private const string OutputArgument = "-busPuzzleBenchmarkOutput";

        [Serializable]
        private sealed class BenchmarkSummary
        {
            public string startedAtUtc;
            public string finishedAtUtc;
            public string unityVersion;
            public string applicationVersion;
            public int startStage;
            public int endStage;
            public int boardStressStartStage;
            public int boardStressEndStage;
            public int testedStageCount;
            public int proceduralStageCount;
            public int fallbackStageCount;
            public int unsafeFailureCount;
            public int generationTimeoutCount;
            public int proceduralFailureCount;
            public int wouldFallbackAtClearDeadlineCount;
            public int clearBudgetExceededCount;
            public int clearProceduralBoardCount;
            public int clearFallbackBoardCount;
            public int startBudgetViolationCount;
            public int finalizeBudgetViolationCount;
            public int boardBuildFailureCount;
            public int runtimeOwnedMeshBaseline;
            public int runtimeOwnedMeshAfterCleanup;
            public int runtimeOwnedMeshLeakCount;
            public long totalElapsedMilliseconds;
            public double generationAverageMilliseconds;
            public long generationP50Milliseconds;
            public long generationP95Milliseconds;
            public long generationP99Milliseconds;
            public long generationMaximumMilliseconds;
            public int slowestStage;
            public string csvPath;
            public BenchmarkStageDigest[] slowestStages;
            public BenchmarkStageDigest[] issues;
        }

        [Serializable]
        private sealed class BenchmarkStageDigest
        {
            public int stageNumber;
            public string outcome;
            public long generationMilliseconds;
            public long finalizeMilliseconds;
            public long clearTransitionMilliseconds;
            public string clearTransitionOutcome;
            public string diagnostic;
        }

        private sealed class StageResult
        {
            public int StageNumber;
            public string Difficulty = string.Empty;
            public int Seed;
            public int TargetVehicleCount;
            public int ActualVehicleCount;
            public int GarageCount;
            public int MysteryVehicleCount;
            public int SolutionCount;
            public long StartMilliseconds;
            public long GenerationMilliseconds;
            public long FinalizeMilliseconds;
            public long BoardBuildMilliseconds;
            public long ClearTransitionMilliseconds;
            public long ManagedMemoryBytes;
            public long NativeAllocatedMemoryBytes;
            public int RuntimeOwnedMeshCount;
            public string Outcome = string.Empty;
            public string ClearTransitionOutcome = string.Empty;
            public bool ProceduralSucceeded;
            public bool FallbackSucceeded;
            public bool ValidationSucceeded;
            public bool BoardBuildSucceeded;
            public bool ClearTransitionSucceeded;
            public bool ClearProceduralSucceeded;
            public bool ClearFallbackSucceeded;
            public bool WouldFallbackAtClearDeadline;
            public bool ClearBudgetExceeded;
            public bool TimedOut;
            public bool StartBudgetExceeded;
            public bool FinalizeBudgetExceeded;
            public string Diagnostic = string.Empty;

            public bool IsSafe =>
                (ProceduralSucceeded || FallbackSucceeded) &&
                ValidationSucceeded &&
                BoardBuildSucceeded &&
                ClearTransitionSucceeded &&
                !TimedOut;
        }

        [MenuItem("Bus Puzzle/Validation/Benchmark Runtime Stages 201-1000")]
        private static void BenchmarkDefaultRangeFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Long-running runtime benchmark",
                    "This executes the production worker one stage at a time. " +
                    "In the worst case 201-1000 can take about 2 hours 40 minutes " +
                    "and the Editor main thread remains occupied while polling. " +
                    "Batch mode is recommended. Continue in this Editor?",
                    "Continue",
                    "Cancel"))
            {
                return;
            }

            Run(
                DefaultStartStage,
                DefaultEndStage,
                DefaultBoardStressStartStage,
                DefaultBoardStressEndStage,
                null,
                true,
                false);
        }

        [MenuItem("Bus Puzzle/Validation/Benchmark Critical Stages 430-470")]
        private static void BenchmarkCriticalRangeFromMenu()
        {
            Run(
                DefaultBoardStressStartStage,
                DefaultBoardStressEndStage,
                DefaultBoardStressStartStage,
                DefaultBoardStressEndStage,
                null,
                true,
                false);
        }

        /// <summary>
        /// Unity command-line entry point. Optional arguments:
        /// -busPuzzleBenchmarkStart 201
        /// -busPuzzleBenchmarkEnd 1000
        /// -busPuzzleBoardStressStart 430
        /// -busPuzzleBoardStressEnd 470
        /// -busPuzzleBenchmarkOutput /absolute/or/project/relative/path
        /// </summary>
        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var startStage = ReadIntArgument(
                args,
                StartArgument,
                DefaultStartStage);
            var endStage = ReadIntArgument(
                args,
                EndArgument,
                DefaultEndStage);
            var boardStartStage = ReadIntArgument(
                args,
                BoardStartArgument,
                DefaultBoardStressStartStage);
            var boardEndStage = ReadIntArgument(
                args,
                BoardEndArgument,
                DefaultBoardStressEndStage);
            var outputPath = ReadStringArgument(
                args,
                OutputArgument);

            Run(
                startStage,
                endStage,
                boardStartStage,
                boardEndStage,
                outputPath,
                false,
                true);
        }

        private static void Run(
            int startStage,
            int endStage,
            int boardStressStartStage,
            int boardStressEndStage,
            string outputPath,
            bool allowCancel,
            bool failOnUnsafeResult)
        {
            ValidateRange(
                startStage,
                endStage,
                boardStressStartStage,
                boardStressEndStage);

            var releaseSequence =
                AssetDatabase.LoadAssetAtPath<LevelSequence>(
                    GeneratedSequencePath);
            var config =
                AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(
                    StageGenerationConfigPath);
            if (releaseSequence == null || config == null)
            {
                throw new BuildFailedException(
                    "Runtime benchmark requires the release sequence and StageGenerationConfig.");
            }

            var paths = ResolveOutputPaths(outputPath);
            InitializeCsv(paths.csvPath);
            var runtimeSequence = LevelSequence.CreateRuntimeGenerated(
                config,
                releaseSequence.StaticLevels);
            var boardRoot = new GameObject(
                "Runtime Stage Benchmark Board")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var boardView = boardRoot.AddComponent<BoardView>();
            var passengers = new List<PassengerView>();
            var buses = new List<BusView>();
            var results = new List<StageResult>(
                endStage - startStage + 1);
            var startedAtUtc = DateTime.UtcNow;
            var totalWatch = Stopwatch.StartNew();
            var cancelled = false;
            Exception fatalException = null;
            var activeStageNumber = 0;
            var runtimeOwnedMeshBaseline =
                BoardView.RuntimeOwnedMeshCount;
            var runtimeOwnedMeshAfterCleanup =
                runtimeOwnedMeshBaseline;

            try
            {
                for (var stageNumber = startStage;
                    stageNumber <= endStage;
                    stageNumber++)
                {
                    if (allowCancel &&
                        EditorUtility.DisplayCancelableProgressBar(
                            "Bus Pop Runtime Stage Benchmark",
                            $"Generating Stage {stageNumber}/{endStage}",
                            Mathf.InverseLerp(
                                startStage,
                                endStage,
                                stageNumber)))
                    {
                        cancelled = true;
                        break;
                    }

                    activeStageNumber = stageNumber;
                    var isConfiguredBoardStressStage =
                        stageNumber >= boardStressStartStage &&
                        stageNumber <= boardStressEndStage;
                    var result = BenchmarkStage(
                        runtimeSequence,
                        config,
                        stageNumber);
                    if (isConfiguredBoardStressStage ||
                        result.WouldFallbackAtClearDeadline ||
                        !result.ProceduralSucceeded)
                    {
                        BenchmarkClearTransition(
                            releaseSequence,
                            config,
                            boardView,
                            passengers,
                            buses,
                            result);
                    }

                    results.Add(result);
                    AppendCsvResult(
                        paths.csvPath,
                        result);
                    LogStageResult(result);

                    // A worker that ignored both its internal and external
                    // cancellation budgets can contaminate every subsequent
                    // single-flight queue measurement. Keep the partial report
                    // and stop instead of producing misleading timings.
                    if (result.TimedOut &&
                        result.Outcome == "benchmark_timeout")
                    {
                        fatalException = new TimeoutException(
                            result.Diagnostic);
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                fatalException = exception;
                if (activeStageNumber > 0 &&
                    !results.Any(result =>
                        result.StageNumber ==
                        activeStageNumber))
                {
                    var failedResult = new StageResult
                    {
                        StageNumber = activeStageNumber,
                        Outcome = "benchmark_exception",
                        Diagnostic = exception.ToString(),
                        BoardBuildSucceeded = false,
                        ClearTransitionSucceeded = false
                    };
                    results.Add(failedResult);
                    AppendCsvResult(
                        paths.csvPath,
                        failedResult);
                }
            }
            finally
            {
                totalWatch.Stop();
                EditorUtility.ClearProgressBar();
                try
                {
                    boardView.ReleaseRuntimeRenderResources();
                    UnityEngine.Object.DestroyImmediate(boardRoot);
                    runtimeSequence.ReleaseRuntimeResources();
                    UnityEngine.Object.DestroyImmediate(runtimeSequence);
                }
                catch (Exception exception)
                {
                    fatalException = fatalException ?? exception;
                    Debug.LogError(
                        $"Runtime benchmark cleanup failed: {exception}");
                }

                runtimeOwnedMeshAfterCleanup =
                    BoardView.RuntimeOwnedMeshCount;
            }

            if (cancelled)
            {
                Debug.LogWarning(
                    $"Runtime stage benchmark cancelled after {results.Count} stage(s).");
            }

            var summary = CreateSummary(
                startedAtUtc,
                DateTime.UtcNow,
                startStage,
                endStage,
                boardStressStartStage,
                boardStressEndStage,
                totalWatch.ElapsedMilliseconds,
                paths.csvPath,
                runtimeOwnedMeshBaseline,
                runtimeOwnedMeshAfterCleanup,
                results);
            WriteSummary(paths.summaryPath, summary);

            var completionMessage =
                $"Runtime stage benchmark {(cancelled ? "stopped" : "completed")}: " +
                $"{results.Count} stage(s), procedural={summary.proceduralStageCount}, " +
                $"fallback={summary.fallbackStageCount}, unsafe={summary.unsafeFailureCount}, " +
                $"would fallback at 3.25s={summary.wouldFallbackAtClearDeadlineCount}, " +
                $"clear fallback={summary.clearFallbackBoardCount}, " +
                $"generation p95={summary.generationP95Milliseconds} ms, " +
                $"max={summary.generationMaximumMilliseconds} ms at Stage {summary.slowestStage}. " +
                $"CSV: {paths.csvPath}; summary: {paths.summaryPath}";
            var releaseGateFailed =
                summary.unsafeFailureCount > 0 ||
                summary.startBudgetViolationCount > 0 ||
                summary.finalizeBudgetViolationCount > 0 ||
                summary.runtimeOwnedMeshLeakCount != 0 ||
                fatalException != null ||
                cancelled;
            if (releaseGateFailed && failOnUnsafeResult)
            {
                throw new BuildFailedException(
                    fatalException != null
                        ? $"{completionMessage} Fatal error: {fatalException}"
                        : completionMessage);
            }

            if (releaseGateFailed)
            {
                Debug.LogWarning(completionMessage);
            }
            else
            {
                Debug.Log(completionMessage);
            }
        }

        private static StageResult BenchmarkStage(
            LevelSequence runtimeSequence,
            StageGenerationConfig config,
            int stageNumber)
        {
            var result = new StageResult
            {
                StageNumber = stageNumber,
                BoardBuildSucceeded = true,
                ClearTransitionSucceeded = true
            };
            var request = StageGenerationPlanner.CreateRequest(
                config,
                stageNumber);
            result.Difficulty = request.Difficulty.ToString();
            result.Seed = request.Seed;
            result.TargetVehicleCount =
                request.Profile != null
                    ? request.Profile.TargetVehicleCount
                    : 0;

            var levelIndex = stageNumber - 1;
            var startWatch = Stopwatch.StartNew();
            var started =
                runtimeSequence.StartRuntimeLevelGeneration(
                    levelIndex);
            startWatch.Stop();
            result.StartMilliseconds =
                startWatch.ElapsedMilliseconds;
            result.StartBudgetExceeded =
                result.StartMilliseconds >
                MaximumStartMilliseconds;

            if (!started)
            {
                result.Outcome = "start_failed";
                result.Diagnostic =
                    "Background generation did not start.";
                ResolveFallback(
                    runtimeSequence,
                    levelIndex,
                    result);
                FinalizeLevelInspection(
                    runtimeSequence,
                    config,
                    result);
                result.WouldFallbackAtClearDeadline =
                    true;
                return result;
            }

            var generationWatch = Stopwatch.StartNew();
            while (runtimeSequence.IsRuntimeLevelGenerationPending(
                levelIndex))
            {
                if (generationWatch.ElapsedMilliseconds >
                    MaximumPerStageWaitMilliseconds)
                {
                    generationWatch.Stop();
                    result.GenerationMilliseconds =
                        generationWatch.ElapsedMilliseconds;
                    result.TimedOut = true;
                    result.Outcome = "benchmark_timeout";
                    result.Diagnostic =
                        $"No terminal result after {MaximumPerStageWaitMilliseconds} ms.";
                    runtimeSequence.CancelRuntimeLevelGeneration(
                        levelIndex);
                    ResolveFallback(
                        runtimeSequence,
                        levelIndex,
                        result);
                    FinalizeLevelInspection(
                        runtimeSequence,
                        config,
                        result);
                    result.WouldFallbackAtClearDeadline =
                        true;
                    return result;
                }

                Thread.Sleep(2);
            }

            generationWatch.Stop();
            result.GenerationMilliseconds =
                generationWatch.ElapsedMilliseconds;
            var finalizeWatch = Stopwatch.StartNew();
            var committed =
                runtimeSequence.TryFinalizeRuntimeLevelGeneration(
                    levelIndex,
                    -1,
                    out var finished,
                    out var diagnostic);
            finalizeWatch.Stop();
            result.FinalizeMilliseconds =
                finalizeWatch.ElapsedMilliseconds;
            result.FinalizeBudgetExceeded =
                result.FinalizeMilliseconds >
                MaximumFinalizeMilliseconds;
            result.Diagnostic =
                diagnostic ?? string.Empty;
            result.ProceduralSucceeded =
                finished && committed;

            if (!finished)
            {
                result.Outcome = "finalize_missing";
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        "Generation stopped pending but returned no terminal result.");
            }
            else if (committed)
            {
                result.Outcome = "procedural";
            }
            else
            {
                result.TimedOut =
                    IsGenerationTimeoutDiagnostic(
                        result.Diagnostic);
                result.Outcome =
                    result.TimedOut
                        ? "generation_timeout"
                        : "procedural_rejected";
            }

            if (!result.ProceduralSucceeded)
            {
                ResolveFallback(
                    runtimeSequence,
                    levelIndex,
                    result);
            }

            FinalizeLevelInspection(
                runtimeSequence,
                config,
                result);
            result.WouldFallbackAtClearDeadline =
                !result.ProceduralSucceeded ||
                result.GenerationMilliseconds +
                result.FinalizeMilliseconds >
                ClearTransitionWaitMilliseconds;
            return result;
        }

        private static void ResolveFallback(
            LevelSequence runtimeSequence,
            int levelIndex,
            StageResult result)
        {
            try
            {
                result.FallbackSucceeded =
                    runtimeSequence.PrepareSafeGameplayLevel(
                        levelIndex,
                        "exhaustive benchmark fallback",
                        false);
                if (!result.FallbackSucceeded)
                {
                    result.Outcome += "_fallback_failed";
                    return;
                }

                var originalOutcome = result.Outcome;
                if (runtimeSequence.TryGetPreparedLevel(
                        levelIndex,
                        out var fallbackLevel) &&
                    fallbackLevel != null &&
                    TryClassifyFallback(
                        fallbackLevel,
                        out var fallbackKind))
                {
                    result.Outcome =
                        $"{originalOutcome}_{fallbackKind}";
                    return;
                }

                result.FallbackSucceeded = false;
                result.Outcome =
                    $"{originalOutcome}_unknown_fallback";
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        "Prepared fallback had neither runtimeSafeCatalog=1 nor runtimeEmergency=1.");
            }
            catch (Exception exception)
            {
                result.FallbackSucceeded = false;
                result.Outcome += "_fallback_exception";
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        exception.ToString());
            }
        }

        private static bool TryClassifyFallback(
            LevelData level,
            out string fallbackKind)
        {
            fallbackKind = string.Empty;
            if (level == null)
            {
                return false;
            }

            if (StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "runtimeSafeCatalog",
                    out var runtimeSafeCatalog) &&
                runtimeSafeCatalog == 1)
            {
                fallbackKind = "safe_catalog_fallback";
                return true;
            }

            if (StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "runtimeEmergency",
                    out var runtimeEmergency) &&
                runtimeEmergency == 1)
            {
                fallbackKind = "emergency_fallback";
                return true;
            }

            return false;
        }

        private static bool IsGenerationTimeoutDiagnostic(
            string diagnostic)
        {
            return !string.IsNullOrWhiteSpace(diagnostic) &&
                (diagnostic.IndexOf(
                    "exceeded its",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                 diagnostic.IndexOf(
                    "generation was cancelled",
                    StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void FinalizeLevelInspection(
            LevelSequence runtimeSequence,
            StageGenerationConfig config,
            StageResult result)
        {
            if (!runtimeSequence.TryGetPreparedLevel(
                    result.StageNumber - 1,
                    out var level) ||
                level == null)
            {
                result.ValidationSucceeded = false;
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        "No prepared level was available.");
                CaptureMemory(result);
                return;
            }

            result.ActualVehicleCount =
                level.AllVehicles != null
                    ? level.AllVehicles.Count
                    : 0;
            result.GarageCount =
                level.Garages != null
                    ? level.Garages.Count
                    : 0;
            result.MysteryVehicleCount =
                CountMysteryVehicles(level);
            result.SolutionCount =
                level.GenerationSolutionCount;

            try
            {
                var report =
                    LevelValidator.Validate(
                        level,
                        false);
                result.ValidationSucceeded =
                    report != null &&
                    !report.HasErrors;
                if (!result.ValidationSucceeded)
                {
                    result.Diagnostic =
                        AppendDiagnostic(
                            result.Diagnostic,
                            report != null
                                ? report.ToConsoleMessage(
                                    level.LevelName)
                                : "Level validation returned no report.");
                }
            }
            catch (Exception exception)
            {
                result.ValidationSucceeded = false;
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        $"Validation exception: {exception}");
            }

            if (result.ProceduralSucceeded)
            {
                var stageNumber = result.StageNumber;
                var levelIndex = stageNumber - 1;
                if (!runtimeSequence
                        .IsProcedurallyGeneratedLevelCached(
                            levelIndex))
                {
                    RejectValidationContract(
                        result,
                        "Accepted level was not cached with procedural origin.");
                }

                if (!StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "runtimeProcedural",
                        out var runtimeProcedural) ||
                    runtimeProcedural != 1 ||
                    !StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "background",
                        out var background) ||
                    background != 1 ||
                    !StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "stage",
                        out var signatureStageNumber) ||
                    signatureStageNumber != stageNumber)
                {
                    RejectValidationContract(
                        result,
                        "Background procedural signature did not match the requested stage.");
                }

                var request =
                    StageGenerationPlanner.CreateRequest(
                        config,
                        stageNumber);
                var minimumVehicleCount =
                    Mathf.CeilToInt(
                        request.Profile.TargetVehicleCount *
                        0.75f);
                if (result.ActualVehicleCount <
                    minimumVehicleCount)
                {
                    RejectValidationContract(
                        result,
                        $"Generated {result.ActualVehicleCount} vehicles; " +
                        $"minimum is {minimumVehicleCount} for target " +
                        $"{request.Profile.TargetVehicleCount}.");
                }

                var solutionDistance =
                    result.SolutionCount <
                        request.MinSolutionCount
                        ? request.MinSolutionCount -
                            result.SolutionCount
                        : result.SolutionCount >
                            request.MaxSolutionCount
                            ? result.SolutionCount -
                                request.MaxSolutionCount
                            : 0;
                if (solutionDistance > 2)
                {
                    RejectValidationContract(
                        result,
                        $"Recorded {result.SolutionCount} solutions; preferred " +
                        $"{request.MinSolutionCount}-{request.MaxSolutionCount} " +
                        "with maximum near-range distance 2.");
                }
            }

            CaptureMemory(result);
        }

        private static void RejectValidationContract(
            StageResult result,
            string diagnostic)
        {
            result.ValidationSucceeded = false;
            result.Diagnostic =
                AppendDiagnostic(
                    result.Diagnostic,
                    diagnostic);
        }

        private static void BenchmarkClearTransition(
            LevelSequence releaseSequence,
            StageGenerationConfig config,
            BoardView boardView,
            List<PassengerView> passengers,
            List<BusView> buses,
            StageResult result)
        {
            result.BoardBuildSucceeded = false;
            result.ClearTransitionSucceeded = false;
            result.ClearTransitionOutcome = "not_started";
            LevelSequence transitionSequence = null;
            var transitionWatch = Stopwatch.StartNew();
            try
            {
                transitionSequence =
                    LevelSequence.CreateRuntimeGenerated(
                        config,
                        releaseSequence.StaticLevels);
                var levelIndex =
                    result.StageNumber - 1;
                var started =
                    transitionSequence
                        .StartRuntimeLevelGeneration(
                            levelIndex);
                if (!started)
                {
                    result.ClearTransitionOutcome =
                        "start_failed";
                }
                else
                {
                    while (transitionSequence
                               .IsRuntimeLevelGenerationPending(
                                   levelIndex) &&
                        transitionWatch.ElapsedMilliseconds <
                        ClearTransitionWaitMilliseconds)
                    {
                        Thread.Sleep(2);
                    }

                    if (transitionSequence
                        .IsRuntimeLevelGenerationPending(
                            levelIndex))
                    {
                        result.ClearBudgetExceeded = true;
                        transitionSequence
                            .CancelRuntimeLevelGeneration(
                                levelIndex);
                        result.ClearTransitionOutcome =
                            "deadline_fallback";
                    }
                    else
                    {
                        var committed =
                            transitionSequence
                                .TryFinalizeRuntimeLevelGeneration(
                                    levelIndex,
                                    -1,
                                    out var finished,
                                    out var diagnostic);
                        if (!string.IsNullOrWhiteSpace(
                                diagnostic))
                        {
                            result.Diagnostic =
                                AppendDiagnostic(
                                    result.Diagnostic,
                                    $"Clear transition: {diagnostic}");
                        }

                        if (finished && committed)
                        {
                            result.ClearProceduralSucceeded =
                                true;
                            result.ClearTransitionOutcome =
                                "procedural_board";
                        }
                        else
                        {
                            result.ClearTransitionOutcome =
                                finished
                                    ? "rejected_fallback"
                                    : "missing_result_fallback";
                        }
                    }
                }

                if (!transitionSequence.TryGetPreparedLevel(
                        levelIndex,
                        out var transitionLevel) ||
                    transitionLevel == null)
                {
                    var fallbackPrepared =
                        transitionSequence
                            .PrepareSafeGameplayLevel(
                                levelIndex,
                                "clear-screen generation timeout fallback",
                                false);
                    if (!fallbackPrepared ||
                        !transitionSequence
                            .TryGetPreparedLevel(
                                levelIndex,
                                out transitionLevel) ||
                        transitionLevel == null ||
                        !TryClassifyFallback(
                            transitionLevel,
                            out var fallbackKind))
                    {
                        result.ClearTransitionOutcome +=
                            "_fallback_failed";
                        result.Diagnostic =
                            AppendDiagnostic(
                                result.Diagnostic,
                                "Clear transition could not prepare a classified safe fallback.");
                        return;
                    }

                    result.ClearFallbackSucceeded = true;
                    result.ClearTransitionOutcome +=
                        $"_{fallbackKind}_board";
                }

                result.ClearTransitionMilliseconds =
                    transitionWatch.ElapsedMilliseconds;
                if (result.ClearTransitionMilliseconds >
                    ClearTransitionWaitMilliseconds)
                {
                    result.ClearBudgetExceeded = true;
                }

                var validationReport =
                    LevelValidator.Validate(
                        transitionLevel,
                        false);
                if (validationReport == null ||
                    validationReport.HasErrors)
                {
                    result.ClearTransitionOutcome +=
                        "_validation_failed";
                    result.Diagnostic =
                        AppendDiagnostic(
                            result.Diagnostic,
                            validationReport != null
                                ? validationReport
                                    .ToConsoleMessage(
                                        transitionLevel
                                            .LevelName)
                                : "Clear transition validation returned no report.");
                    return;
                }

                var boardWatch = Stopwatch.StartNew();
                try
                {
                    boardView.BuildLevel(
                        transitionLevel,
                        passengers,
                        buses,
                        result.StageNumber);
                }
                finally
                {
                    boardWatch.Stop();
                    result.BoardBuildMilliseconds =
                        boardWatch.ElapsedMilliseconds;
                }

                var expectedStartingVisibleVehicles =
                    transitionLevel.Buses.Count +
                    transitionLevel.Garages.Count;
                if (buses.Count !=
                        expectedStartingVisibleVehicles ||
                    passengers.Count !=
                        transitionLevel.PassengerUnits.Count)
                {
                    result.ClearTransitionOutcome +=
                        "_view_count_mismatch";
                    result.Diagnostic =
                        AppendDiagnostic(
                            result.Diagnostic,
                            $"Board materialized {buses.Count}/" +
                            $"{expectedStartingVisibleVehicles} starting-visible vehicles and " +
                            $"{passengers.Count}/" +
                            $"{transitionLevel.PassengerUnits.Count} passenger units.");
                    return;
                }

                result.BoardBuildSucceeded = true;
                result.ClearTransitionSucceeded = true;
            }
            catch (Exception exception)
            {
                result.ClearTransitionOutcome +=
                    "_exception";
                result.Diagnostic =
                    AppendDiagnostic(
                        result.Diagnostic,
                        $"Clear transition exception: {exception}");
            }
            finally
            {
                transitionWatch.Stop();
                if (result.ClearTransitionMilliseconds == 0L)
                {
                    result.ClearTransitionMilliseconds =
                        transitionWatch.ElapsedMilliseconds;
                }

                if (transitionSequence != null)
                {
                    transitionSequence
                        .ReleaseRuntimeResources();
                    UnityEngine.Object.DestroyImmediate(
                        transitionSequence);
                }

                CaptureMemory(result);
            }
        }

        private static int CountMysteryVehicles(
            LevelData level)
        {
            var count = 0;
            var vehicles = level.AllVehicles;
            if (vehicles == null)
            {
                return count;
            }

            for (var index = 0;
                index < vehicles.Count;
                index++)
            {
                if (vehicles[index].StartsConcealed)
                {
                    count++;
                }
            }

            return count;
        }

        private static void CaptureMemory(
            StageResult result)
        {
            result.ManagedMemoryBytes =
                GC.GetTotalMemory(false);
            result.NativeAllocatedMemoryBytes =
                Profiler.GetTotalAllocatedMemoryLong();
            result.RuntimeOwnedMeshCount =
                BoardView.RuntimeOwnedMeshCount;
        }

        private static void LogStageResult(
            StageResult result)
        {
            var message =
                $"Runtime benchmark Stage {result.StageNumber:0000}: " +
                $"outcome={result.Outcome}, start={result.StartMilliseconds} ms, " +
                $"generation={result.GenerationMilliseconds} ms, " +
                $"finalize={result.FinalizeMilliseconds} ms, " +
                $"clear={result.ClearTransitionMilliseconds} ms " +
                $"({result.ClearTransitionOutcome}), " +
                $"board={result.BoardBuildMilliseconds} ms, " +
                $"vehicles={result.ActualVehicleCount}/{result.TargetVehicleCount}, " +
                $"solutions={result.SolutionCount}, safe={result.IsSafe}.";
            if (result.IsSafe &&
                !result.StartBudgetExceeded &&
                !result.FinalizeBudgetExceeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(
                    $"{message} {result.Diagnostic}");
            }
        }

        private static BenchmarkSummary CreateSummary(
            DateTime startedAtUtc,
            DateTime finishedAtUtc,
            int startStage,
            int endStage,
            int boardStressStartStage,
            int boardStressEndStage,
            long totalElapsedMilliseconds,
            string csvPath,
            int runtimeOwnedMeshBaseline,
            int runtimeOwnedMeshAfterCleanup,
            IReadOnlyList<StageResult> results)
        {
            var generationTimes = results
                .Where(result =>
                    result.ProceduralSucceeded)
                .Select(result =>
                    result.GenerationMilliseconds)
                .OrderBy(value => value)
                .ToArray();
            var slowest =
                results
                    .Where(result =>
                        result.ProceduralSucceeded)
                    .OrderByDescending(result =>
                        result.GenerationMilliseconds)
                    .FirstOrDefault();
            return new BenchmarkSummary
            {
                startedAtUtc =
                    startedAtUtc.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                finishedAtUtc =
                    finishedAtUtc.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                applicationVersion = Application.version,
                startStage = startStage,
                endStage = endStage,
                boardStressStartStage =
                    boardStressStartStage,
                boardStressEndStage =
                    boardStressEndStage,
                testedStageCount = results.Count,
                proceduralStageCount =
                    results.Count(result =>
                        result.ProceduralSucceeded),
                fallbackStageCount =
                    results.Count(result =>
                        result.FallbackSucceeded),
                unsafeFailureCount =
                    results.Count(result =>
                        !result.IsSafe),
                generationTimeoutCount =
                    results.Count(result =>
                        result.TimedOut),
                proceduralFailureCount =
                    results.Count(result =>
                        !result.ProceduralSucceeded),
                wouldFallbackAtClearDeadlineCount =
                    results.Count(result =>
                        result.WouldFallbackAtClearDeadline),
                clearBudgetExceededCount =
                    results.Count(result =>
                        result.ClearBudgetExceeded),
                clearProceduralBoardCount =
                    results.Count(result =>
                        result.ClearProceduralSucceeded &&
                        result.BoardBuildSucceeded),
                clearFallbackBoardCount =
                    results.Count(result =>
                        result.ClearFallbackSucceeded &&
                        result.BoardBuildSucceeded),
                startBudgetViolationCount =
                    results.Count(result =>
                        result.StartBudgetExceeded),
                finalizeBudgetViolationCount =
                    results.Count(result =>
                        result.FinalizeBudgetExceeded),
                boardBuildFailureCount =
                    results.Count(result =>
                        !result.BoardBuildSucceeded),
                runtimeOwnedMeshBaseline =
                    runtimeOwnedMeshBaseline,
                runtimeOwnedMeshAfterCleanup =
                    runtimeOwnedMeshAfterCleanup,
                runtimeOwnedMeshLeakCount =
                    runtimeOwnedMeshAfterCleanup -
                    runtimeOwnedMeshBaseline,
                totalElapsedMilliseconds =
                    totalElapsedMilliseconds,
                generationAverageMilliseconds =
                    generationTimes.Length > 0
                        ? generationTimes.Average(
                            value => (double)value)
                        : 0d,
                generationP50Milliseconds =
                    Percentile(generationTimes, 0.50d),
                generationP95Milliseconds =
                    Percentile(generationTimes, 0.95d),
                generationP99Milliseconds =
                    Percentile(generationTimes, 0.99d),
                generationMaximumMilliseconds =
                    generationTimes.Length > 0
                        ? generationTimes[
                            generationTimes.Length - 1]
                        : 0L,
                slowestStage =
                    slowest != null
                        ? slowest.StageNumber
                        : 0,
                csvPath = csvPath,
                slowestStages = results
                    .Where(result =>
                        result.ProceduralSucceeded)
                    .OrderByDescending(result =>
                        result.GenerationMilliseconds)
                    .Take(20)
                    .Select(CreateDigest)
                    .ToArray(),
                issues = results
                    .Where(result =>
                        !result.IsSafe ||
                        result.StartBudgetExceeded ||
                        result.FinalizeBudgetExceeded ||
                        result.WouldFallbackAtClearDeadline ||
                        result.ClearBudgetExceeded)
                    .Select(CreateDigest)
                    .ToArray()
            };
        }

        private static BenchmarkStageDigest CreateDigest(
            StageResult result)
        {
            return new BenchmarkStageDigest
            {
                stageNumber = result.StageNumber,
                outcome = result.Outcome,
                generationMilliseconds =
                    result.GenerationMilliseconds,
                finalizeMilliseconds =
                    result.FinalizeMilliseconds,
                clearTransitionMilliseconds =
                    result.ClearTransitionMilliseconds,
                clearTransitionOutcome =
                    result.ClearTransitionOutcome,
                diagnostic = result.Diagnostic
            };
        }

        private static long Percentile(
            IReadOnlyList<long> sortedValues,
            double percentile)
        {
            if (sortedValues == null ||
                sortedValues.Count == 0)
            {
                return 0L;
            }

            var index = (int)Math.Ceiling(
                percentile * sortedValues.Count) - 1;
            return sortedValues[
                Mathf.Clamp(
                    index,
                    0,
                    sortedValues.Count - 1)];
        }

        private static void InitializeCsv(
            string path)
        {
            EnsureParentDirectory(path);
            File.WriteAllText(
                path,
                "stage,difficulty,seed,targetVehicles,actualVehicles,garages,mysteryVehicles," +
                "solutions,startMs,generationMs,finalizeMs,clearPreparationMs,boardBuildMs," +
                "managedMemoryBytes,nativeAllocatedMemoryBytes,runtimeOwnedMeshes,outcome," +
                "clearTransitionOutcome,proceduralSucceeded,fallbackSucceeded,validationSucceeded," +
                "clearTransitionSucceeded,clearProceduralSucceeded,clearFallbackSucceeded," +
                "wouldFallbackAt3250Ms,clearBudgetExceeded,boardBuildSucceeded,timedOut,startBudgetExceeded," +
                "finalizeBudgetExceeded,isSafe,diagnostic" +
                Environment.NewLine,
                new UTF8Encoding(true));
        }

        private static void AppendCsvResult(
            string path,
            StageResult result)
        {
            var builder = new StringBuilder(512);
            AppendCsv(builder, result.StageNumber);
            AppendCsv(builder, result.Difficulty);
            AppendCsv(builder, result.Seed);
            AppendCsv(builder, result.TargetVehicleCount);
            AppendCsv(builder, result.ActualVehicleCount);
            AppendCsv(builder, result.GarageCount);
            AppendCsv(builder, result.MysteryVehicleCount);
            AppendCsv(builder, result.SolutionCount);
            AppendCsv(builder, result.StartMilliseconds);
            AppendCsv(builder, result.GenerationMilliseconds);
            AppendCsv(builder, result.FinalizeMilliseconds);
            AppendCsv(
                builder,
                result.ClearTransitionMilliseconds);
            AppendCsv(builder, result.BoardBuildMilliseconds);
            AppendCsv(builder, result.ManagedMemoryBytes);
            AppendCsv(
                builder,
                result.NativeAllocatedMemoryBytes);
            AppendCsv(builder, result.RuntimeOwnedMeshCount);
            AppendCsv(builder, result.Outcome);
            AppendCsv(
                builder,
                result.ClearTransitionOutcome);
            AppendCsv(
                builder,
                result.ProceduralSucceeded);
            AppendCsv(
                builder,
                result.FallbackSucceeded);
            AppendCsv(
                builder,
                result.ValidationSucceeded);
            AppendCsv(
                builder,
                result.ClearTransitionSucceeded);
            AppendCsv(
                builder,
                result.ClearProceduralSucceeded);
            AppendCsv(
                builder,
                result.ClearFallbackSucceeded);
            AppendCsv(
                builder,
                result.WouldFallbackAtClearDeadline);
            AppendCsv(
                builder,
                result.ClearBudgetExceeded);
            AppendCsv(
                builder,
                result.BoardBuildSucceeded);
            AppendCsv(builder, result.TimedOut);
            AppendCsv(
                builder,
                result.StartBudgetExceeded);
            AppendCsv(
                builder,
                result.FinalizeBudgetExceeded);
            AppendCsv(builder, result.IsSafe);
            AppendCsv(
                builder,
                result.Diagnostic,
                true);
            File.AppendAllText(
                path,
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static void WriteSummary(
            string path,
            BenchmarkSummary summary)
        {
            EnsureParentDirectory(path);
            File.WriteAllText(
                path,
                JsonUtility.ToJson(
                    summary,
                    true),
                new UTF8Encoding(true));
        }

        private static void AppendCsv(
            StringBuilder builder,
            object value,
            bool endOfLine = false)
        {
            var text = Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ??
                string.Empty;
            if (text.IndexOfAny(
                    new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                builder
                    .Append('"')
                    .Append(
                        text.Replace(
                            "\"",
                            "\"\""))
                    .Append('"');
            }
            else
            {
                builder.Append(text);
            }

            if (endOfLine)
            {
                builder.AppendLine();
            }
            else
            {
                builder.Append(',');
            }
        }

        private static (
            string csvPath,
            string summaryPath)
            ResolveOutputPaths(
                string requestedPath)
        {
            var projectRoot = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    ".."));
            string csvPath;
            if (string.IsNullOrWhiteSpace(
                    requestedPath))
            {
                var stamp = DateTime.UtcNow.ToString(
                    "yyyyMMdd_HHmmss",
                    CultureInfo.InvariantCulture);
                csvPath = Path.Combine(
                    projectRoot,
                    "Build",
                    "Validation",
                    $"runtime-stage-benchmark-{stamp}.csv");
            }
            else
            {
                csvPath = Path.IsPathRooted(
                    requestedPath)
                    ? requestedPath
                    : Path.Combine(
                        projectRoot,
                        requestedPath);
                if (!string.Equals(
                        Path.GetExtension(csvPath),
                        ".csv",
                        StringComparison.OrdinalIgnoreCase))
                {
                    csvPath += ".csv";
                }
            }

            csvPath = Path.GetFullPath(csvPath);
            var summaryPath = Path.ChangeExtension(
                csvPath,
                ".summary.json");
            return (csvPath, summaryPath);
        }

        private static void EnsureParentDirectory(
            string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string AppendDiagnostic(
            string current,
            string next)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                return next ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(next))
            {
                return current;
            }

            return $"{current} | {next}";
        }

        private static void ValidateRange(
            int startStage,
            int endStage,
            int boardStressStartStage,
            int boardStressEndStage)
        {
            if (startStage < 1 ||
                endStage < startStage)
            {
                throw new BuildFailedException(
                    $"Invalid benchmark range {startStage}-{endStage}.");
            }

            if (boardStressStartStage < 1 ||
                boardStressEndStage <
                boardStressStartStage)
            {
                throw new BuildFailedException(
                    $"Invalid board stress range " +
                    $"{boardStressStartStage}-{boardStressEndStage}.");
            }
        }

        private static int ReadIntArgument(
            IReadOnlyList<string> args,
            string name,
            int fallback)
        {
            var value = ReadStringArgument(
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

        private static string ReadStringArgument(
            IReadOnlyList<string> args,
            string name)
        {
            if (args == null)
            {
                return string.Empty;
            }

            for (var index = 0;
                index < args.Count - 1;
                index++)
            {
                if (string.Equals(
                        args[index],
                        name,
                        StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }

            return string.Empty;
        }
    }
}
#endif
