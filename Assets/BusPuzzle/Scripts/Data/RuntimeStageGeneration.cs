using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BusPuzzle
{
    internal enum RuntimeStageGenerationOutcome
    {
        Succeeded,
        Exhausted,
        Cancelled,
        Faulted
    }

    /// <summary>
    /// Marks the calculation-only part of runtime generation. Code in this scope
    /// must not create, destroy, load, or otherwise touch UnityEngine.Object
    /// instances. Value types from UnityEngine are allowed.
    /// </summary>
    internal static class RuntimeGenerationThreadGuard
    {
        [ThreadStatic] private static int workerScopeDepth;

        public static bool IsWorkerThread => workerScopeDepth > 0;

        public static IDisposable EnterWorkerScope()
        {
            workerScopeDepth++;
            return new WorkerScope();
        }

        private sealed class WorkerScope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                workerScopeDepth = Math.Max(0, workerScopeDepth - 1);
            }
        }
    }

    /// <summary>
    /// Unity-object-free stage payload produced by the background generator.
    /// It is materialized as LevelData only after the main thread has accepted it.
    /// </summary>
    internal sealed class RuntimeStageData
    {
        private readonly List<BusDefinition> buses;
        private readonly List<GarageDefinition> garages;

        public RuntimeStageData(
            string levelName,
            LevelDifficultyProfile profile,
            PassengerFlowPlan passengerFlowPlan,
            IEnumerable<BusDefinition> busDefinitions,
            int rotaryUnitCapacity,
            RotaryRoadPresetId roadPresetId,
            IEnumerable<GarageDefinition> garageDefinitions)
        {
            LevelName = string.IsNullOrWhiteSpace(levelName) ? "Runtime Stage" : levelName;
            Profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            PassengerFlowPlan = passengerFlowPlan ?? new PassengerFlowPlan();
            buses = busDefinitions != null
                ? new List<BusDefinition>(busDefinitions)
                : new List<BusDefinition>();
            garages = garageDefinitions != null
                ? new List<GarageDefinition>(garageDefinitions)
                : new List<GarageDefinition>();
            RotaryUnitCapacity = Mathf.Clamp(
                rotaryUnitCapacity,
                LevelData.MinRotaryUnitCapacity,
                LevelData.MaxRotaryUnitCapacity);
            RoadPresetId = roadPresetId;
        }

        public string LevelName { get; }
        public LevelDifficultyProfile Profile { get; }
        public PassengerFlowPlan PassengerFlowPlan { get; }
        public IReadOnlyList<BusDefinition> Buses => buses;
        public IReadOnlyList<GarageDefinition> Garages => garages;
        public int RotaryUnitCapacity { get; }
        public RotaryRoadPresetId RoadPresetId { get; }
        public string GenerationSignature { get; private set; } = string.Empty;
        public int GenerationSolutionCount { get; private set; }

        public int TotalVehicleCount
        {
            get
            {
                var count = buses.Count;
                for (var garageIndex = 0; garageIndex < garages.Count; garageIndex++)
                {
                    count += garages[garageIndex].TotalVehicleCount;
                }

                return count;
            }
        }

        public void SetGenerationMetadata(string signature, int solutionCount)
        {
            GenerationSignature = signature ?? string.Empty;
            GenerationSolutionCount = Mathf.Max(0, solutionCount);
        }

        public LevelData Materialize()
        {
            if (RuntimeGenerationThreadGuard.IsWorkerThread)
            {
                throw new InvalidOperationException(
                    "RuntimeStageData must be materialized on the Unity main thread.");
            }

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = HideFlags.DontSave;
            level.ConfigureWithPassengerFlowPlan(
                LevelName,
                Profile,
                PassengerFlowPlan,
                buses,
                RotaryUnitCapacity,
                RoadPresetId,
                null,
                garages);
            level.SetGenerationMetadata(GenerationSignature, GenerationSolutionCount);
            return level;
        }
    }

    internal readonly struct RuntimeStageGenerationOptions
    {
        public readonly int CandidateAttempts;
        public readonly int VehicleGenerationAttempts;
        public readonly bool GarageGenerationEnabled;
        public readonly string BaseGenerationSignature;

        public RuntimeStageGenerationOptions(
            int candidateAttempts,
            int vehicleGenerationAttempts,
            bool garageGenerationEnabled,
            string baseGenerationSignature)
        {
            CandidateAttempts = Mathf.Clamp(candidateAttempts, 1, 4);
            VehicleGenerationAttempts = Mathf.Clamp(vehicleGenerationAttempts, 1, 6);
            GarageGenerationEnabled = garageGenerationEnabled;
            BaseGenerationSignature = baseGenerationSignature ?? string.Empty;
        }

        public static RuntimeStageGenerationOptions Create(
            StageGenerationConfig config,
            StageGenerationRequest request)
        {
            if (RuntimeGenerationThreadGuard.IsWorkerThread)
            {
                throw new InvalidOperationException(
                    "Runtime generation options must be snapshotted on the Unity main thread.");
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new RuntimeStageGenerationOptions(
                config.RuntimeCandidateAttemptsPerStage,
                config.RuntimeVehicleGenerationAttempts,
                config.SuperHardGarageRule != null &&
                    config.SuperHardGarageRule.Enabled,
                StageGenerationSignature.Create(config, request));
        }
    }

    internal sealed class RuntimeStageGenerationResult
    {
        private RuntimeStageGenerationResult(
            RuntimeStageGenerationOutcome outcome,
            RuntimeStageData data,
            StageSolutionAnalysis analysis,
            int candidateIndex,
            int maximumGeneratedVehicleCount,
            string diagnostic)
        {
            Outcome = outcome;
            Data = data;
            Analysis = analysis;
            CandidateIndex = candidateIndex;
            MaximumGeneratedVehicleCount = maximumGeneratedVehicleCount;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public RuntimeStageGenerationOutcome Outcome { get; }
        public RuntimeStageData Data { get; }
        public StageSolutionAnalysis Analysis { get; }
        public int CandidateIndex { get; }
        public int MaximumGeneratedVehicleCount { get; }
        public string Diagnostic { get; }
        public bool Succeeded => Outcome == RuntimeStageGenerationOutcome.Succeeded && Data != null;

        public static RuntimeStageGenerationResult Success(
            RuntimeStageData data,
            StageSolutionAnalysis analysis,
            int candidateIndex,
            int maximumGeneratedVehicleCount)
        {
            return new RuntimeStageGenerationResult(
                RuntimeStageGenerationOutcome.Succeeded,
                data,
                analysis,
                candidateIndex,
                maximumGeneratedVehicleCount,
                string.Empty);
        }

        public static RuntimeStageGenerationResult Failure(
            RuntimeStageGenerationOutcome outcome,
            int maximumGeneratedVehicleCount,
            string diagnostic)
        {
            return new RuntimeStageGenerationResult(
                outcome,
                null,
                default,
                -1,
                maximumGeneratedVehicleCount,
                diagnostic);
        }
    }

    internal static class RuntimeStageDataGenerator
    {
        private const int RuntimeGeneratorVersion = 6;
        private const int MaximumRuntimeSolutionCountLimit = 256;
        private const int RuntimeSolutionNodeVisitLimit = 8192;
        private const int RuntimeGarageSolutionNodeVisitLimit = 2048;
        private const int RuntimeGarageMemoizedStateLimit = 2048;
        private const int MaximumAcceptedSolutionDistance = 2;
        private const float MinimumVehicleTargetRatio = 0.75f;

        public static RuntimeStageGenerationResult Generate(
            RuntimeStageGenerationOptions options,
            StageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            var nullCandidateCount = 0;
            var shapeCoverageRejectionCount = 0;
            var vehicleCountRejectionCount = 0;
            var solutionRejectionCount = 0;
            var solutionRangeRejectionCount = 0;
            var maximumGeneratedVehicleCount = 0;
            RuntimeStageData bestSolvableData = null;
            StageSolutionAnalysis bestSolvableAnalysis = default;
            var bestSolvableCandidate = -1;
            var bestSolutionDistance = int.MaxValue;

            try
            {
                for (var candidate = 0; candidate < options.CandidateAttempts; candidate++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var candidateData = LevelGenerator.BuildRuntimeStageData(
                        request,
                        options.GarageGenerationEnabled,
                        candidate,
                        options.VehicleGenerationAttempts,
                        false,
                        false,
                        cancellationToken);
                    if (candidateData == null)
                    {
                        nullCandidateCount++;
                        continue;
                    }

                    maximumGeneratedVehicleCount = Mathf.Max(
                        maximumGeneratedVehicleCount,
                        candidateData.TotalVehicleCount);

                    if (!HasRequiredShapeCoverage(request, candidateData.Buses))
                    {
                        shapeCoverageRejectionCount++;
                        continue;
                    }

                    var requestedProfile = request.Profile ??
                        LevelDifficultyProfile.DefaultFor(request.Difficulty);
                    var minimumVehicleCount = Mathf.CeilToInt(
                        requestedProfile.TargetVehicleCount * MinimumVehicleTargetRatio);
                    if (candidateData.TotalVehicleCount < minimumVehicleCount)
                    {
                        vehicleCountRejectionCount++;
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var analysis = AnalyzeSolution(
                        candidateData.Buses,
                        candidateData.Garages,
                        request,
                        cancellationToken);
                    if (!analysis.IsSolvable)
                    {
                        solutionRejectionCount++;
                        continue;
                    }

                    var solutionDistance = GetSolutionRangeDistance(
                        request,
                        analysis);
                    var solutionNodeVisitLimit =
                        GetSolutionNodeVisitLimit(
                            candidateData.Garages);
                    candidateData.SetGenerationMetadata(
                        CreateProceduralSignature(
                            options,
                            request,
                            candidate,
                            candidateData.TotalVehicleCount,
                            solutionNodeVisitLimit),
                        analysis.SolutionCount);
                    if (solutionDistance <=
                        MaximumAcceptedSolutionDistance)
                    {
                        return RuntimeStageGenerationResult.Success(
                            candidateData,
                            analysis,
                            candidate,
                            maximumGeneratedVehicleCount);
                    }

                    solutionRangeRejectionCount++;
                    if (solutionDistance < bestSolutionDistance)
                    {
                        bestSolutionDistance = solutionDistance;
                        bestSolvableData = candidateData;
                        bestSolvableAnalysis = analysis;
                        bestSolvableCandidate = candidate;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return RuntimeStageGenerationResult.Failure(
                    RuntimeStageGenerationOutcome.Cancelled,
                    maximumGeneratedVehicleCount,
                    $"Stage {request.StageNumber:000} background generation was cancelled.");
            }
            catch (Exception exception)
            {
                return RuntimeStageGenerationResult.Failure(
                    RuntimeStageGenerationOutcome.Faulted,
                    maximumGeneratedVehicleCount,
                    $"Stage {request.StageNumber:000} background generation failed: " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }

            if (bestSolvableData != null &&
                bestSolutionDistance <=
                    MaximumAcceptedSolutionDistance)
            {
                return RuntimeStageGenerationResult.Success(
                    bestSolvableData,
                    bestSolvableAnalysis,
                    bestSolvableCandidate,
                    maximumGeneratedVehicleCount);
            }

            return RuntimeStageGenerationResult.Failure(
                RuntimeStageGenerationOutcome.Exhausted,
                maximumGeneratedVehicleCount,
                $"Stage {request.StageNumber:000} exhausted {options.CandidateAttempts} bounded probes. " +
                $"Rejections: null={nullCandidateCount}, shape={shapeCoverageRejectionCount}, " +
                $"vehicles={vehicleCountRejectionCount}, solution={solutionRejectionCount}, " +
                $"solutionRange={solutionRangeRejectionCount}; " +
                $"best vehicle count={maximumGeneratedVehicleCount}/" +
                $"{(request.Profile != null ? request.Profile.TargetVehicleCount : 0)}.");
        }

        /// <summary>
        /// Opt-in A/B probe for the SuperHard + Garage memoized witness
        /// analyzer. Generate intentionally continues to use AnalyzeSolution
        /// until exhaustive comparison proves this path is faster without
        /// changing acceptance.
        /// </summary>
        internal static bool TryAnalyzeSuperHardGarageMemoizedWitnessForComparison(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            StageGenerationRequest request,
            CancellationToken cancellationToken,
            out StageMemoizedWitnessAnalysis analysis)
        {
            analysis = default;
            cancellationToken.ThrowIfCancellationRequested();
            if (request.Difficulty != LevelDifficulty.SuperHard ||
                garages == null ||
                garages.Count == 0)
            {
                return false;
            }

            analysis = StageSolutionAnalyzer.AnalyzeMemoizedWitness(
                buses,
                garages,
                RuntimeGarageSolutionNodeVisitLimit,
                RuntimeGarageMemoizedStateLimit,
                cancellationToken);
            return true;
        }

        private static bool HasRequiredShapeCoverage(
            StageGenerationRequest request,
            IReadOnlyList<BusDefinition> buses)
        {
            var profile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var count = buses != null ? buses.Count : 0;
            return ShapeLibraryVehicleCoverage.IsSatisfied(
                    profile,
                    request.VehicleLayoutVariantIndex,
                    count) &&
                ShapeLibraryLayoutQuality.IsSatisfied(
                    profile,
                    request.VehicleLayoutVariantIndex,
                    buses);
        }

        private static StageSolutionAnalysis AnalyzeSolution(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            StageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((garages == null || garages.Count == 0) &&
                buses != null &&
                (!LevelVehicleExitPlanner.TryFindExitOrder(
                     buses,
                     out var exitOrder,
                     out _,
                     cancellationToken) ||
                 exitOrder.Count != buses.Count))
            {
                return new StageSolutionAnalysis(false, 0, false);
            }

            return StageSolutionAnalyzer.Analyze(
                buses,
                garages,
                GetRequiredSolutionProofCount(request),
                GetSolutionNodeVisitLimit(garages),
                cancellationToken);
        }

        private static int GetSolutionNodeVisitLimit(
            IReadOnlyList<GarageDefinition> garages)
        {
            return garages != null &&
                garages.Count > 0
                    ? RuntimeGarageSolutionNodeVisitLimit
                    : RuntimeSolutionNodeVisitLimit;
        }

        private static int GetRequiredSolutionProofCount(
            StageGenerationRequest request)
        {
            // Runtime acceptance permits a candidate up to two solutions below
            // the preferred range, and also accepts counts above its upper
            // bound. Proving more than this lower acceptance threshold cannot
            // change accept/reject, but it can multiply garage DFS cost.
            return Mathf.Clamp(
                request.MinSolutionCount -
                MaximumAcceptedSolutionDistance,
                1,
                MaximumRuntimeSolutionCountLimit);
        }

        private static int GetSolutionRangeDistance(
            StageGenerationRequest request,
            StageSolutionAnalysis analysis)
        {
            if (!analysis.IsSolvable)
            {
                return int.MaxValue;
            }

            if (analysis.SolutionCount < request.MinSolutionCount)
            {
                return request.MinSolutionCount - analysis.SolutionCount;
            }

            if (analysis.SolutionCount > request.MaxSolutionCount)
            {
                return analysis.SolutionCount - request.MaxSolutionCount;
            }

            return 0;
        }

        private static string CreateProceduralSignature(
            RuntimeStageGenerationOptions options,
            StageGenerationRequest request,
            int candidateIndex,
            int actualVehicleCount,
            int solutionNodeVisitLimit)
        {
            return
                $"runtimeProcedural=1;runtimeGenerator={RuntimeGeneratorVersion};" +
                $"background=1;stage={request.StageNumber};seed={request.Seed};" +
                $"candidate={candidateIndex};vehicleAttempts={options.VehicleGenerationAttempts};" +
                $"actualVehicles={actualVehicleCount};solutionNodeLimit={solutionNodeVisitLimit};" +
                $"solutionCountLimit={GetRequiredSolutionProofCount(request)};" +
                options.BaseGenerationSignature;
        }
    }

    /// <summary>
    /// Single-flight background queue. Task completion is polled from the main
    /// thread; no continuation is allowed to call Unity APIs.
    /// </summary>
    internal sealed class RuntimeStageGenerationService : IDisposable
    {
        private const int MaximumGenerationMilliseconds = 12000;

        private sealed class Job
        {
            public Job(
                Task<RuntimeStageGenerationResult> task,
                CancellationTokenSource cancellation)
            {
                Task = task;
                Cancellation = cancellation;
            }

            public Task<RuntimeStageGenerationResult> Task { get; }
            public CancellationTokenSource Cancellation { get; }
        }

        private readonly object gate = new object();
        private readonly Dictionary<int, Job> jobs = new Dictionary<int, Job>();
        private readonly CancellationTokenSource lifetimeCancellation = new CancellationTokenSource();
        private Task generationTail = Task.CompletedTask;
        private bool disposed;

        public bool Start(
            int levelIndex,
            RuntimeStageGenerationOptions options,
            StageGenerationRequest request)
        {
            if (levelIndex < 0 || disposed)
            {
                return false;
            }

            // This method is main-thread only. Deep-copy every Resources-backed
            // shape template before Task.Run so the worker never touches a
            // ScriptableObject or performs a Resources load.
            VehicleShapeTemplateCatalog.PrimeRuntimeGenerationTemplates();

            lock (gate)
            {
                if (disposed || jobs.ContainsKey(levelIndex))
                {
                    return false;
                }

                var cancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        lifetimeCancellation.Token);
                var token = cancellation.Token;
                var predecessor = generationTail;
                var task = predecessor.ContinueWith(
                    _ =>
                    {
                        try
                        {
                            cancellation.CancelAfter(
                                MaximumGenerationMilliseconds);
                            token.ThrowIfCancellationRequested();
                            using (RuntimeGenerationThreadGuard.EnterWorkerScope())
                            {
                                return RuntimeStageDataGenerator.Generate(
                                    options,
                                    request,
                                    token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return RuntimeStageGenerationResult.Failure(
                                RuntimeStageGenerationOutcome.Cancelled,
                                0,
                                $"Stage {request.StageNumber:000} background generation was cancelled " +
                                $"or exceeded its {MaximumGenerationMilliseconds / 1000f:0.#} second budget.");
                        }
                        catch (Exception exception)
                        {
                            return RuntimeStageGenerationResult.Failure(
                                RuntimeStageGenerationOutcome.Faulted,
                                0,
                                $"Stage {request.StageNumber:000} background generation failed: " +
                                $"{exception.GetType().Name}: {exception.Message}");
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);
                jobs.Add(
                    levelIndex,
                    new Job(task, cancellation));
                generationTail = task;
                return true;
            }
        }

        public bool IsPending(int levelIndex)
        {
            lock (gate)
            {
                return jobs.TryGetValue(levelIndex, out var job) &&
                    !job.Task.IsCompleted;
            }
        }

        public bool Cancel(int levelIndex)
        {
            Job job;
            lock (gate)
            {
                if (!jobs.TryGetValue(levelIndex, out job))
                {
                    return false;
                }

                jobs.Remove(levelIndex);
            }

            CancelAndDisposeWhenCompleted(job);
            return true;
        }

        public int CancelOutsideRange(
            int minimumLevelIndex,
            int maximumLevelIndex)
        {
            var cancelledJobs = new List<Job>();
            lock (gate)
            {
                if (jobs.Count == 0)
                {
                    return 0;
                }

                var cancelledIndices = new List<int>();
                foreach (var pair in jobs)
                {
                    if (pair.Key < minimumLevelIndex ||
                        pair.Key > maximumLevelIndex)
                    {
                        cancelledIndices.Add(pair.Key);
                        cancelledJobs.Add(pair.Value);
                    }
                }

                for (var index = 0;
                    index < cancelledIndices.Count;
                    index++)
                {
                    jobs.Remove(cancelledIndices[index]);
                }
            }

            for (var index = 0;
                index < cancelledJobs.Count;
                index++)
            {
                CancelAndDisposeWhenCompleted(
                    cancelledJobs[index]);
            }

            return cancelledJobs.Count;
        }

        public bool TryTakeCompleted(
            int levelIndex,
            out RuntimeStageGenerationResult result)
        {
            result = null;
            Job job;
            lock (gate)
            {
                if (!jobs.TryGetValue(levelIndex, out job) ||
                    !job.Task.IsCompleted)
                {
                    return false;
                }

                jobs.Remove(levelIndex);
            }

            try
            {
                result = job.Task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                result = RuntimeStageGenerationResult.Failure(
                    RuntimeStageGenerationOutcome.Cancelled,
                    0,
                    $"Stage {levelIndex + 1:000} background generation was cancelled.");
            }
            catch (Exception exception)
            {
                result = RuntimeStageGenerationResult.Failure(
                    RuntimeStageGenerationOutcome.Faulted,
                    0,
                    $"Stage {levelIndex + 1:000} background task failed: " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                job.Cancellation.Dispose();
            }

            return true;
        }

        public void Dispose()
        {
            List<Job> cancelledJobs;
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                cancelledJobs = new List<Job>(
                    jobs.Values);
                jobs.Clear();
            }

            lifetimeCancellation.Cancel();
            for (var index = 0;
                index < cancelledJobs.Count;
                index++)
            {
                CancelAndDisposeWhenCompleted(
                    cancelledJobs[index]);
            }
        }

        private static void CancelAndDisposeWhenCompleted(
            Job job)
        {
            if (job == null)
            {
                return;
            }

            try
            {
                job.Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            job.Task.ContinueWith(
                _ => job.Cancellation.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
