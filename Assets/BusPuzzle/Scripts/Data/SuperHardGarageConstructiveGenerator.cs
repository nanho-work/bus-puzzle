using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace BusPuzzle
{
    /// <summary>
    /// One move in the constructive generator's authored linear proof.
    /// Regular vehicles use GarageIndex/GarageProgress = -1. A garage vehicle
    /// reuses the stable solver slot regularCount + garageIndex while progress
    /// advances from its front vehicle through the queue.
    /// </summary>
    public readonly struct SuperHardGarageConstructiveWitnessStep
    {
        public readonly int VehicleIndex;
        public readonly int GarageIndex;
        public readonly int GarageProgress;
        public readonly BusDefinition Vehicle;

        public SuperHardGarageConstructiveWitnessStep(
            int vehicleIndex,
            int garageIndex,
            int garageProgress,
            BusDefinition vehicle)
        {
            VehicleIndex = vehicleIndex;
            GarageIndex = garageIndex;
            GarageProgress = garageProgress;
            Vehicle = vehicle;
        }
    }

    /// <summary>
    /// Calculation-only result for editor A/B probes. A successful result is
    /// accepted only after its pre-authored witness has been replayed linearly;
    /// neither the legacy counter nor the memoized solver participates in the
    /// constructive acceptance decision.
    /// </summary>
    internal readonly struct SuperHardGarageConstructiveGenerationResult
    {
        public readonly bool Succeeded;
        public readonly RuntimeStageData Data;
        public readonly IReadOnlyList<SuperHardGarageConstructiveWitnessStep>
            Witness;
        public readonly bool WitnessValidated;
        public readonly int CandidateSeed;
        public readonly int LayoutProbeIndex;
        public readonly int PlacementProbeCount;
        public readonly int PathProbeCount;
        public readonly int PlacementProbeLimit;
        public readonly int PathProbeLimit;
        public readonly bool HitOperationBudget;
        public readonly int RegularVehicleCount;
        public readonly int GarageVehicleCount;
        public readonly int RegularPrefixCount;
        public readonly int InitialOpeningCount;
        public readonly int MaximumInitialOpeningCount;
        public readonly int GarageDependencyTarget;
        public readonly bool GarageDependencyEvaluated;
        public readonly int GarageDependencyCount;
        public readonly bool SuffixOnlyReleaseEvaluated;
        public readonly int SuffixOnlyReleasedGarageCount;
        public readonly int SuffixOnlyReleaseTarget;
        public readonly string Diagnostic;

        public SuperHardGarageConstructiveGenerationResult(
            bool succeeded,
            RuntimeStageData data,
            IReadOnlyList<SuperHardGarageConstructiveWitnessStep> witness,
            bool witnessValidated,
            int candidateSeed,
            int layoutProbeIndex,
            int placementProbeCount,
            int pathProbeCount,
            int placementProbeLimit,
            int pathProbeLimit,
            bool hitOperationBudget,
            int regularVehicleCount,
            int garageVehicleCount,
            int regularPrefixCount,
            int initialOpeningCount,
            int maximumInitialOpeningCount,
            int garageDependencyTarget,
            bool garageDependencyEvaluated,
            int garageDependencyCount,
            bool suffixOnlyReleaseEvaluated,
            int suffixOnlyReleasedGarageCount,
            int suffixOnlyReleaseTarget,
            string diagnostic)
        {
            Succeeded = succeeded;
            Data = data;
            Witness = witness ??
                Array.Empty<SuperHardGarageConstructiveWitnessStep>();
            WitnessValidated = witnessValidated;
            CandidateSeed = candidateSeed;
            LayoutProbeIndex = layoutProbeIndex;
            PlacementProbeCount = Mathf.Max(0, placementProbeCount);
            PathProbeCount = Mathf.Max(0, pathProbeCount);
            PlacementProbeLimit = Mathf.Max(1, placementProbeLimit);
            PathProbeLimit = Mathf.Max(1, pathProbeLimit);
            HitOperationBudget = hitOperationBudget;
            RegularVehicleCount = Mathf.Max(0, regularVehicleCount);
            GarageVehicleCount = Mathf.Max(0, garageVehicleCount);
            RegularPrefixCount = Mathf.Max(0, regularPrefixCount);
            InitialOpeningCount = Mathf.Max(0, initialOpeningCount);
            MaximumInitialOpeningCount = Mathf.Max(
                0,
                maximumInitialOpeningCount);
            GarageDependencyTarget = Mathf.Max(
                0,
                garageDependencyTarget);
            GarageDependencyEvaluated =
                garageDependencyEvaluated;
            GarageDependencyCount = Mathf.Max(
                0,
                garageDependencyCount);
            SuffixOnlyReleaseEvaluated =
                suffixOnlyReleaseEvaluated;
            SuffixOnlyReleaseTarget = Mathf.Max(
                0,
                suffixOnlyReleaseTarget);
            SuffixOnlyReleasedGarageCount =
                Mathf.Max(
                    0,
                    suffixOnlyReleasedGarageCount);
            Diagnostic = diagnostic ?? string.Empty;
        }
    }

    /// <summary>
    /// Public, editor-facing projection of a constructive result. The LevelData
    /// payload is materialized only by the explicit comparison facade and must
    /// be destroyed by the editor caller after measurement.
    /// </summary>
    public readonly struct SuperHardGarageConstructiveComparisonResult
    {
        public readonly bool Succeeded;
        public readonly LevelData Level;
        public readonly IReadOnlyList<SuperHardGarageConstructiveWitnessStep>
            Witness;
        public readonly bool WitnessValidated;
        public readonly int CandidateSeed;
        public readonly int LayoutProbeIndex;
        public readonly int PlacementProbeCount;
        public readonly int PathProbeCount;
        public readonly int PlacementProbeLimit;
        public readonly int PathProbeLimit;
        public readonly bool HitOperationBudget;
        public readonly int RegularVehicleCount;
        public readonly int GarageVehicleCount;
        public readonly int RegularPrefixCount;
        public readonly int InitialOpeningCount;
        public readonly int MaximumInitialOpeningCount;
        public readonly int GarageDependencyTarget;
        public readonly bool GarageDependencyEvaluated;
        public readonly int GarageDependencyCount;
        public readonly bool SuffixOnlyReleaseEvaluated;
        public readonly int SuffixOnlyReleasedGarageCount;
        public readonly int SuffixOnlyReleaseTarget;
        public readonly string Diagnostic;

        internal SuperHardGarageConstructiveComparisonResult(
            SuperHardGarageConstructiveGenerationResult source,
            LevelData level)
        {
            Succeeded = source.Succeeded;
            Level = level;
            Witness = source.Witness;
            WitnessValidated = source.WitnessValidated;
            CandidateSeed = source.CandidateSeed;
            LayoutProbeIndex = source.LayoutProbeIndex;
            PlacementProbeCount = source.PlacementProbeCount;
            PathProbeCount = source.PathProbeCount;
            PlacementProbeLimit = source.PlacementProbeLimit;
            PathProbeLimit = source.PathProbeLimit;
            HitOperationBudget = source.HitOperationBudget;
            RegularVehicleCount = source.RegularVehicleCount;
            GarageVehicleCount = source.GarageVehicleCount;
            RegularPrefixCount = source.RegularPrefixCount;
            InitialOpeningCount = source.InitialOpeningCount;
            MaximumInitialOpeningCount =
                source.MaximumInitialOpeningCount;
            GarageDependencyTarget =
                source.GarageDependencyTarget;
            GarageDependencyEvaluated =
                source.GarageDependencyEvaluated;
            GarageDependencyCount =
                source.GarageDependencyCount;
            SuffixOnlyReleaseEvaluated =
                source.SuffixOnlyReleaseEvaluated;
            SuffixOnlyReleasedGarageCount =
                source.SuffixOnlyReleasedGarageCount;
            SuffixOnlyReleaseTarget =
                source.SuffixOnlyReleaseTarget;
            Diagnostic = source.Diagnostic;
        }
    }

    /// <summary>
    /// Experimental witness-first constructor for SuperHard + Garage stages.
    ///
    /// The logical move order is fixed before geometry is placed: a bounded
    /// regular blocker prefix exits first, every garage queue is then drained
    /// in deterministic order, and the flexible remaining suffix exits last.
    /// Outward-facing near-edge garages keep their exit corridors short while
    /// leaving a bounded blocker lane for real prefix-to-garage dependencies.
    /// Both regular segments are inserted in reverse witness order and every
    /// insertion must have an exact clear exit in its authored replay state.
    ///
    /// This class is deliberately disconnected from the shipped generation
    /// route. It only operates on value data and is safe to invoke from the
    /// background generation worker or an editor A/B validator.
    /// </summary>
    public static class SuperHardGarageConstructiveGenerator
    {
        private const string ExperimentalStrategy =
            "rollingExactSuffixRootsBoundedPrefixFullDomainRepairWitnessV12A";
        private const int CandidateStride = 7919;
        private const int LayoutProbeStride = 130363;
        private const int MinimumRegularVehicleCount = 4;
        private const int MaximumLayoutProbeCount = 6;
        private const int MaximumSuffixStaticProbeCountPerVehicle = 64;
        private const int MaximumScoredSuffixCandidateCount = 16;
        private const int MaximumExactSuffixFinalistCount = 4;
        private const int MaximumValidExactSuffixRootOptionCount = 2;
        private const int MaximumPrefixStaticProbeCountPerVehicle = 64;
        private const int MaximumPrefixFallbackStaticProbeCountPerVehicle = 128;
        private const int MaximumScoredPrefixCandidateCount = 16;
        private const int MaximumExactPrefixFinalistCount = 16;
        private const int MaximumTargetedMotifCandidateCountPerVehicle = 64;
        private const int MaximumExactTargetedMotifFinalistCount = 16;
        private const int MaximumPrefixRepairDepth = 2;
        private const int MaximumPrefixRepairWidth = 2;
        private const int MaximumPrefixRepairNodeCount = 8;
        private const int MaximumOpeningRootBlocksPerFuturePrefix = 2;
        private const int MinimumRegularSuffixCount = 2;
        private const int MaximumConstrainedRegularPrefixCount = 16;
        private const int MaximumTotalPlacementProbeCount = 20000;
        private const int MaximumTotalPathProbeCount = 6000;
        private const float PlacementBoundaryPaddingCells = 0.16f;
        // Candidate blocker roots start one cell in from the board edge. The
        // reserve also includes a regular vehicle's lateral half-width plus
        // the corridor clearance, so Medium-only queues still choose inset 5.
        private const float MinimumGarageBlockerLaneCells = 1.55f;
        private const float GarageCorridorPaddingCells = 0.06f;
        private const float TargetedMotifSampleStepCells = 0.16f;
        private const float MaximumTargetedMotifPositionOffsetCells = 0.45f;

        private static readonly PuzzleColor[] ColorPool =
        {
            PuzzleColor.Red,
            PuzzleColor.Orange,
            PuzzleColor.Yellow,
            PuzzleColor.Green,
            PuzzleColor.Blue,
            PuzzleColor.Purple,
            PuzzleColor.White,
            PuzzleColor.Black,
            PuzzleColor.Pink,
            PuzzleColor.SkyBlue,
            PuzzleColor.Lime,
            PuzzleColor.Brown
        };

        /// <summary>
        /// Main-thread-only facade for editor A/B validation. Production code
        /// should call the calculation-only internal overload and materialize
        /// its RuntimeStageData only after the worker result is accepted.
        /// </summary>
        public static bool TryGenerateForComparison(
            StageGenerationRequest request,
            int candidateOffset,
            int layoutProbeCount,
            CancellationToken cancellationToken,
            out SuperHardGarageConstructiveComparisonResult result)
        {
            if (RuntimeGenerationThreadGuard.IsWorkerThread)
            {
                throw new InvalidOperationException(
                    "Constructive comparison results must be materialized on the Unity main thread.");
            }

            var succeeded = TryGenerate(
                request,
                candidateOffset,
                layoutProbeCount,
                cancellationToken,
                out var generationResult);
            var level = succeeded &&
                generationResult.Data != null
                    ? generationResult.Data.Materialize()
                    : null;
            result =
                new SuperHardGarageConstructiveComparisonResult(
                    generationResult,
                    level);
            return succeeded;
        }

        internal static bool TryGenerate(
            StageGenerationRequest request,
            int candidateOffset,
            int layoutProbeCount,
            CancellationToken cancellationToken,
            out SuperHardGarageConstructiveGenerationResult result)
        {
            cancellationToken.ThrowIfCancellationRequested();
            candidateOffset = Mathf.Max(0, candidateOffset);
            layoutProbeCount = Mathf.Clamp(
                layoutProbeCount,
                1,
                MaximumLayoutProbeCount);
            var candidateSeed = unchecked(
                request.Seed +
                candidateOffset * CandidateStride);
            var operationBudget =
                new ConstructiveOperationBudget(
                    MaximumTotalPlacementProbeCount,
                    MaximumTotalPathProbeCount);

            if (request.Difficulty != LevelDifficulty.SuperHard ||
                request.GarageCount <= 0)
            {
                result = Failure(
                    candidateSeed,
                    0,
                    0,
                    "Constructive generation applies only to SuperHard stages with garages.");
                return false;
            }

            var profile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(LevelDifficulty.SuperHard);
            if (!TryCreateLogicalPlan(
                    request,
                    profile,
                    candidateSeed,
                    cancellationToken,
                    out var logicalPlan,
                    out var planDiagnostic))
            {
                result = Failure(
                    candidateSeed,
                    0,
                    0,
                    planDiagnostic);
                return false;
            }

            var garageDependencyTarget =
                GetGarageDependencyTarget(
                    profile,
                    request.GarageCount,
                    logicalPlan.RegularPrefixCount);
            var suffixOnlyReleaseTarget =
                Mathf.Max(
                    0,
                    request.GarageCount);
            if (!TryPlaceEdgeGarages(
                    logicalPlan,
                    garageDependencyTarget,
                    candidateSeed,
                    cancellationToken,
                    out var garages,
                    out var garageDiagnostic))
            {
                result = Failure(
                    candidateSeed,
                    logicalPlan.RegularVehicles.Count,
                    logicalPlan.GarageVehicleCount,
                    garageDiagnostic,
                    regularPrefixCount:
                        logicalPlan.RegularPrefixCount,
                    garageDependencyTarget:
                        garageDependencyTarget,
                    suffixOnlyReleaseTarget:
                        suffixOnlyReleaseTarget);
                return false;
            }

            var garageDependencyEvaluated = false;
            var garageDependencyCount = 0;
            var suffixOnlyReleaseEvaluated = false;
            var suffixOnlyReleasedGarageCount = 0;
            var finalRollingSuffixRootCount = 0;
            var suffixRootEvaluationCount = 0;
            var chainLinkCount = 0;
            var chainBlockedRootCount = 0;
            var chainMergeCount = 0;
            var requiredMergeCount = 0;
            var targetedMotifAttemptCount = 0;
            var targetedMotifSuccessCount = 0;
            var prefixRepairAttemptCount = 0;
            var prefixRepairNodeCount = 0;
            var prefixRepairSuccessCount = 0;
            var lastDiagnostic = string.Empty;
            for (var layoutProbe = 0;
                layoutProbe < layoutProbeCount;
                layoutProbe++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remainingLayoutCount =
                    layoutProbeCount - layoutProbe;
                var remainingPlacementBudget =
                    operationBudget.PlacementLimit -
                    operationBudget.PlacementCount;
                if (remainingPlacementBudget <= 0)
                {
                    operationBudget.TryConsumePlacement();
                    lastDiagnostic =
                        AppendSuffixRootTelemetry(
                            operationBudget.CreateDiagnostic(),
                            finalRollingSuffixRootCount,
                            suffixRootEvaluationCount);
                    break;
                }

                var layoutPlacementQuota = Mathf.Max(
                    1,
                    remainingPlacementBudget /
                    Mathf.Max(1, remainingLayoutCount));
                var layoutPlacementLimit = Mathf.Min(
                    operationBudget.PlacementLimit,
                    operationBudget.PlacementCount +
                    layoutPlacementQuota);
                if (!TryPlaceRegularVehiclesInReverseWitnessOrder(
                        request,
                        profile,
                        logicalPlan,
                        garages,
                        garageDependencyTarget,
                        candidateSeed,
                        layoutProbe,
                        cancellationToken,
                        operationBudget,
                        layoutPlacementLimit,
                        out var regularVehicles,
                        out var initialOpeningCount,
                        out var maximumInitialOpeningCount,
                        out garageDependencyEvaluated,
                        out garageDependencyCount,
                        out suffixOnlyReleaseEvaluated,
                        out suffixOnlyReleasedGarageCount,
                        out finalRollingSuffixRootCount,
                        out suffixRootEvaluationCount,
                        out chainLinkCount,
                        out chainBlockedRootCount,
                        out chainMergeCount,
                        out requiredMergeCount,
                        out targetedMotifAttemptCount,
                        out targetedMotifSuccessCount,
                        out prefixRepairAttemptCount,
                        out prefixRepairNodeCount,
                        out prefixRepairSuccessCount,
                        out lastDiagnostic))
                {
                    lastDiagnostic =
                        AppendSuffixRootTelemetry(
                            lastDiagnostic,
                            finalRollingSuffixRootCount,
                            suffixRootEvaluationCount);
                    if (operationBudget.HitLimit)
                    {
                        lastDiagnostic =
                            AppendSuffixRootTelemetry(
                                operationBudget.CreateDiagnostic(),
                                finalRollingSuffixRootCount,
                                suffixRootEvaluationCount);
                        break;
                    }

                    continue;
                }

                operationBudget.SetPhase(
                    $"layout-{layoutProbe}:mystery-modifiers");
                if (!TryApplyMysteryVehicleModifiers(
                        regularVehicles,
                        request.MysteryVehicleProfile,
                        unchecked(candidateSeed + 1699),
                        cancellationToken,
                        operationBudget,
                        out regularVehicles))
                {
                    lastDiagnostic =
                        AppendSuffixRootTelemetry(
                            operationBudget.CreateDiagnostic(),
                            finalRollingSuffixRootCount,
                            suffixRootEvaluationCount);
                    break;
                }

                // Shape quality computes one exact opening path per regular
                // vehicle. Reserve those operations before entering the gate
                // so the total path budget remains explicit and bounded.
                operationBudget.SetPhase(
                    $"layout-{layoutProbe}:shape-quality");
                if (!operationBudget.TryConsumePaths(
                        regularVehicles.Count))
                {
                    lastDiagnostic =
                        AppendSuffixRootTelemetry(
                            operationBudget.CreateDiagnostic(),
                            finalRollingSuffixRootCount,
                            suffixRootEvaluationCount);
                    break;
                }

                if (!HasRequiredShapeQuality(
                        request,
                        profile,
                        regularVehicles,
                        out lastDiagnostic))
                {
                    lastDiagnostic =
                        AppendSuffixRootTelemetry(
                            lastDiagnostic,
                            finalRollingSuffixRootCount,
                            suffixRootEvaluationCount);
                    continue;
                }

                if (!HasPreservedGenerationContract(
                        request,
                        logicalPlan,
                        regularVehicles,
                        garages,
                        out lastDiagnostic))
                {
                    lastDiagnostic =
                        AppendSuffixRootTelemetry(
                            lastDiagnostic,
                            finalRollingSuffixRootCount,
                            suffixRootEvaluationCount);
                    continue;
                }

                var witness = BuildWitness(
                    logicalPlan,
                    regularVehicles,
                    garages,
                    cancellationToken);
                operationBudget.SetPhase(
                    $"layout-{layoutProbe}:final-witness-replay");
                if (!ValidateLinearWitness(
                        regularVehicles,
                        garages,
                        witness,
                        cancellationToken,
                        operationBudget,
                        out lastDiagnostic))
                {
                    lastDiagnostic =
                        AppendSuffixRootTelemetry(
                            lastDiagnostic,
                            finalRollingSuffixRootCount,
                            suffixRootEvaluationCount);
                    if (operationBudget.HitLimit)
                    {
                        lastDiagnostic =
                            AppendSuffixRootTelemetry(
                                operationBudget.CreateDiagnostic(),
                                finalRollingSuffixRootCount,
                                suffixRootEvaluationCount);
                        break;
                    }

                    continue;
                }

                var passengerFlowPlan = BuildPassengerFlowPlan(
                    profile,
                    witness,
                    candidateSeed);
                var data = new RuntimeStageData(
                    $"Stage {request.StageNumber:000} {request.Difficulty}",
                    profile,
                    passengerFlowPlan,
                    regularVehicles,
                    request.RotaryCapacity,
                    request.RoadPresetId,
                    garages);
                data.SetGenerationMetadata(
                    CreateExperimentalSignature(
                        request,
                        candidateOffset,
                        candidateSeed,
                        layoutProbe,
                        data.TotalVehicleCount,
                        witness.Count,
                        logicalPlan.RegularPrefixCount,
                        logicalPlan.RegularVehicles.Count -
                            logicalPlan.RegularPrefixCount,
                        initialOpeningCount,
                        maximumInitialOpeningCount,
                        garageDependencyCount,
                        garageDependencyTarget,
                        chainLinkCount,
                        chainBlockedRootCount,
                        chainMergeCount,
                        requiredMergeCount,
                        targetedMotifAttemptCount,
                        targetedMotifSuccessCount,
                        prefixRepairAttemptCount,
                        prefixRepairNodeCount,
                        prefixRepairSuccessCount,
                        finalRollingSuffixRootCount,
                        suffixRootEvaluationCount),
                    1);

                result =
                    new SuperHardGarageConstructiveGenerationResult(
                        true,
                        data,
                        witness,
                        true,
                        candidateSeed,
                        layoutProbe,
                        operationBudget.PlacementCount,
                        operationBudget.PathCount,
                        operationBudget.PlacementLimit,
                        operationBudget.PathLimit,
                        operationBudget.HitLimit,
                        regularVehicles.Count,
                        logicalPlan.GarageVehicleCount,
                        logicalPlan.RegularPrefixCount,
                        initialOpeningCount,
                        maximumInitialOpeningCount,
                        garageDependencyTarget,
                        garageDependencyEvaluated,
                        garageDependencyCount,
                        suffixOnlyReleaseEvaluated,
                        suffixOnlyReleasedGarageCount,
                        suffixOnlyReleaseTarget,
                        string.Empty);
                return true;
            }

            result = Failure(
                candidateSeed,
                logicalPlan.RegularVehicles.Count,
                logicalPlan.GarageVehicleCount,
                string.IsNullOrWhiteSpace(lastDiagnostic)
                    ? $"All {layoutProbeCount} constructive layout probes were exhausted."
                    : lastDiagnostic,
                operationBudget.PlacementCount,
                operationBudget.PathCount,
                operationBudget.HitLimit,
                logicalPlan.RegularPrefixCount,
                0,
                GetMaximumInitialOpeningCount(
                    profile,
                    logicalPlan.RegularVehicles.Count +
                    garages.Count,
                    garages.Count),
                garageDependencyTarget,
                garageDependencyEvaluated,
                garageDependencyCount,
                suffixOnlyReleaseEvaluated,
                suffixOnlyReleasedGarageCount,
                suffixOnlyReleaseTarget);
            return false;
        }

        private static SuperHardGarageConstructiveGenerationResult Failure(
            int candidateSeed,
            int regularVehicleCount,
            int garageVehicleCount,
            string diagnostic,
            int placementProbeCount = 0,
            int pathProbeCount = 0,
            bool hitOperationBudget = false,
            int regularPrefixCount = 0,
            int initialOpeningCount = 0,
            int maximumInitialOpeningCount = 0,
            int garageDependencyTarget = 0,
            bool garageDependencyEvaluated = false,
            int garageDependencyCount = 0,
            bool suffixOnlyReleaseEvaluated = false,
            int suffixOnlyReleasedGarageCount = 0,
            int suffixOnlyReleaseTarget = 0)
        {
            return new SuperHardGarageConstructiveGenerationResult(
                false,
                null,
                Array.Empty<SuperHardGarageConstructiveWitnessStep>(),
                false,
                candidateSeed,
                -1,
                placementProbeCount,
                pathProbeCount,
                MaximumTotalPlacementProbeCount,
                MaximumTotalPathProbeCount,
                hitOperationBudget,
                regularVehicleCount,
                garageVehicleCount,
                regularPrefixCount,
                initialOpeningCount,
                maximumInitialOpeningCount,
                garageDependencyTarget,
                garageDependencyEvaluated,
                garageDependencyCount,
                suffixOnlyReleaseEvaluated,
                suffixOnlyReleasedGarageCount,
                suffixOnlyReleaseTarget,
                diagnostic);
        }

        private static bool TryCreateLogicalPlan(
            StageGenerationRequest request,
            LevelDifficultyProfile profile,
            int candidateSeed,
            CancellationToken cancellationToken,
            out LogicalPlan plan,
            out string diagnostic)
        {
            cancellationToken.ThrowIfCancellationRequested();
            plan = null;
            diagnostic = string.Empty;

            var targetVehicleCount = profile.TargetVehicleCount;
            var garageCount = request.GarageCount;
            var minimumQueueCount = request.MinGarageQueuedVehicles;
            var maximumQueueCount = request.MaxGarageQueuedVehicles;
            var shapeMinimum = ShapeLibraryVehicleCoverage.GetMinimumVehicleCount(
                profile,
                request.VehicleLayoutVariantIndex);
            var minimumRegularCount = Mathf.Max(
                MinimumRegularVehicleCount,
                shapeMinimum);
            var minimumGarageVehicleCount =
                garageCount * (1 + minimumQueueCount);
            if (targetVehicleCount <
                minimumRegularCount + minimumGarageVehicleCount)
            {
                diagnostic =
                    $"Target {targetVehicleCount} cannot preserve {garageCount} garages " +
                    $"with queue minimum {minimumQueueCount} and regular minimum " +
                    $"{minimumRegularCount}.";
                return false;
            }

            var random = new System.Random(candidateSeed);
            var queueBudget =
                targetVehicleCount -
                minimumRegularCount -
                garageCount;
            var queueCounts = new int[garageCount];
            var remainingQueueBudget = queueBudget;
            for (var garageIndex = 0;
                garageIndex < garageCount;
                garageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remainingGarageCount =
                    garageCount - garageIndex - 1;
                var maximumForThisGarage = Mathf.Min(
                    maximumQueueCount,
                    remainingQueueBudget -
                    remainingGarageCount * minimumQueueCount);
                if (maximumForThisGarage < minimumQueueCount)
                {
                    diagnostic =
                        "Garage queue budget became smaller than the requested minimum.";
                    return false;
                }

                var desired = random.Next(
                    minimumQueueCount,
                    maximumQueueCount + 1);
                var queueCount = Mathf.Min(
                    desired,
                    maximumForThisGarage);
                queueCounts[garageIndex] = queueCount;
                remainingQueueBudget -= queueCount;
            }

            var garageVehicleCount = garageCount;
            for (var index = 0; index < queueCounts.Length; index++)
            {
                garageVehicleCount += queueCounts[index];
            }

            var regularVehicleCount =
                targetVehicleCount - garageVehicleCount;
            if (regularVehicleCount < minimumRegularCount)
            {
                diagnostic =
                    $"Constructive regular count {regularVehicleCount} is below " +
                    $"required minimum {minimumRegularCount}.";
                return false;
            }

            var colorCount = Mathf.Clamp(
                profile.TargetColorCount,
                2,
                ColorPool.Length);
            var colorOffset = random.Next(0, colorCount);
            var globalVehicleCursor = 0;
            var garageVehicles =
                new LogicalVehicleSpec[garageCount][];
            for (var garageIndex = 0;
                garageIndex < garageCount;
                garageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sequenceLength =
                    1 + queueCounts[garageIndex];
                var sequence =
                    new LogicalVehicleSpec[sequenceLength];
                for (var progress = 0;
                    progress < sequenceLength;
                    progress++)
                {
                    sequence[progress] =
                        CreateLogicalVehicleSpec(
                            random,
                            colorCount,
                            colorOffset,
                            globalVehicleCursor++);
                }

                garageVehicles[garageIndex] = sequence;
            }

            var sortableRegular =
                new List<SortableLogicalVehicleSpec>(
                    regularVehicleCount);
            for (var index = 0;
                index < regularVehicleCount;
                index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sortableRegular.Add(
                    new SortableLogicalVehicleSpec(
                        CreateLogicalVehicleSpec(
                            random,
                            colorCount,
                            colorOffset,
                            globalVehicleCursor++),
                        random.Next(),
                        index));
            }

            // Large vehicles are late in the logical route, so reverse placement
            // gives them first choice of the emptiest pattern slots.
            sortableRegular.Sort(
                (left, right) =>
                {
                    var sizeComparison =
                        ((int)left.Spec.Size).CompareTo(
                            (int)right.Spec.Size);
                    if (sizeComparison != 0)
                    {
                        return sizeComparison;
                    }

                    var tieComparison =
                        left.TieBreaker.CompareTo(
                            right.TieBreaker);
                    return tieComparison != 0
                        ? tieComparison
                        : left.OriginalIndex.CompareTo(
                            right.OriginalIndex);
                });
            var regularVehicles =
                new List<LogicalVehicleSpec>(
                    regularVehicleCount);
            for (var index = 0;
                index < sortableRegular.Count;
                index++)
            {
                regularVehicles.Add(
                    sortableRegular[index].Spec);
            }

            var regularPrefixCount =
                GetRegularPrefixCount(
                    profile,
                    regularVehicles.Count);
            var regularSuffixCount =
                regularVehicles.Count -
                regularPrefixCount;
            // The suffix owns every regular vehicle left by the bounded prefix.
            // It has no fixed upper limit; keep only the two-vehicle minimum.
            var maximumSuffixCount = Mathf.Max(
                MinimumRegularSuffixCount,
                regularVehicles.Count - 1);
            if (regularSuffixCount <
                    MinimumRegularSuffixCount ||
                regularSuffixCount >
                    maximumSuffixCount)
            {
                diagnostic =
                    $"Constructive regular suffix {regularSuffixCount} is outside " +
                    $"the flexible required range " +
                    $"{MinimumRegularSuffixCount}..{maximumSuffixCount}; " +
                    $"boundedPrefix={regularPrefixCount}/" +
                    $"{MaximumConstrainedRegularPrefixCount}.";
                return false;
            }

            var witnessTokens =
                new List<LogicalWitnessToken>(
                    targetVehicleCount);
            for (var regularIndex = 0;
                regularIndex < regularPrefixCount;
                regularIndex++)
            {
                witnessTokens.Add(
                    LogicalWitnessToken.ForRegular(
                        regularIndex));
            }

            for (var garageIndex = 0;
                garageIndex < garageVehicles.Length;
                garageIndex++)
            {
                for (var progress = 0;
                    progress <
                        garageVehicles[garageIndex].Length;
                    progress++)
                {
                    witnessTokens.Add(
                        LogicalWitnessToken.ForGarage(
                            garageIndex,
                            progress));
                }
            }

            for (var regularIndex = regularPrefixCount;
                regularIndex < regularVehicles.Count;
                regularIndex++)
            {
                witnessTokens.Add(
                    LogicalWitnessToken.ForRegular(
                        regularIndex));
            }

            plan = new LogicalPlan(
                regularVehicles,
                garageVehicles,
                witnessTokens,
                targetVehicleCount,
                colorCount,
                regularPrefixCount);
            return true;
        }

        private static int GetRegularPrefixCount(
            LevelDifficultyProfile profile,
            int regularVehicleCount)
        {
            if (regularVehicleCount <
                MinimumRegularSuffixCount)
            {
                return 0;
            }

            var pressure = Mathf.Clamp01(
                profile.ParkingTension * 0.65f +
                profile.StationPressure * 0.35f);
            var prefixRatio = Mathf.Lerp(
                0.68f,
                0.82f,
                pressure);
            var desiredPrefixCount =
                Mathf.RoundToInt(
                    regularVehicleCount *
                    prefixRatio);
            // Preserve the pressure-derived ratio, then constrain only the
            // geometry-heavy prefix. Every overflow vehicle moves to suffix.
            var maximumPrefixCount = Mathf.Min(
                MaximumConstrainedRegularPrefixCount,
                regularVehicleCount -
                MinimumRegularSuffixCount);
            return Mathf.Clamp(
                desiredPrefixCount,
                1,
                maximumPrefixCount);
        }

        private static int GetGarageDependencyTarget(
            LevelDifficultyProfile profile,
            int garageCount,
            int regularPrefixCount)
        {
            garageCount = Mathf.Max(0, garageCount);
            regularPrefixCount = Mathf.Max(
                0,
                regularPrefixCount);
            var maximumTarget = Mathf.Min(
                garageCount,
                regularPrefixCount);
            if (maximumTarget <= 0)
            {
                return 0;
            }

            var pressure = Mathf.Clamp01(
                profile.ParkingTension * 0.65f +
                profile.StationPressure * 0.35f);
            var targetRatio = Mathf.Lerp(
                0.55f,
                0.85f,
                pressure);
            return Mathf.Clamp(
                Mathf.CeilToInt(
                    garageCount *
                    targetRatio),
                1,
                maximumTarget);
        }

        private static LogicalVehicleSpec CreateLogicalVehicleSpec(
            System.Random random,
            int colorCount,
            int colorOffset,
            int vehicleIndex)
        {
            var color =
                ColorPool[(vehicleIndex + colorOffset) % colorCount];
            return new LogicalVehicleSpec(
                color,
                PickSuperHardSize(random));
        }

        private static BusSize PickSuperHardSize(
            System.Random random)
        {
            var roll = random.NextDouble();
            if (roll < 0.22d)
            {
                return BusSize.Small;
            }

            return roll < 0.66d
                ? BusSize.Medium
                : BusSize.Large;
        }

        private static bool TryPlaceEdgeGarages(
            LogicalPlan plan,
            int dependencyTarget,
            int candidateSeed,
            CancellationToken cancellationToken,
            out List<GarageDefinition> garages,
            out string diagnostic)
        {
            garages = new List<GarageDefinition>(
                plan.GarageVehicles.Length);
            diagnostic = string.Empty;
            var portalInset =
                GetRequiredGaragePortalInset(
                    plan,
                    dependencyTarget);
            var candidates =
                CreateEdgeGarageCandidates(
                    candidateSeed,
                    portalInset);

            for (var garageIndex = 0;
                garageIndex < plan.GarageVehicles.Length;
                garageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var placed = false;
                for (var candidateIndex = 0;
                    candidateIndex < candidates.Count;
                    candidateIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var portal = candidates[candidateIndex];
                    var garage = CreateGarage(
                        portal,
                        plan.GarageVehicles[garageIndex]);
                    if (!IsGaragePlacementCompatible(
                            garage,
                            garages))
                    {
                        continue;
                    }

                    garages.Add(garage);
                    candidates.RemoveAt(candidateIndex);
                    placed = true;
                    break;
                }

                if (placed)
                {
                    continue;
                }

                diagnostic =
                    $"Could not place near-edge garage {garageIndex + 1}/" +
                    $"{plan.GarageVehicles.Length} without overlap.";
                return false;
            }

            return garages.Count ==
                plan.GarageVehicles.Length;
        }

        private static List<EdgeGaragePortal> CreateEdgeGarageCandidates(
            int seed,
            int portalInset)
        {
            var candidates =
                new List<EdgeGaragePortal>(24);
            var edgeCoordinates =
                new[] { 2, 4, 6, 8, 10, 12 };
            var primarySide = PositiveModulo(
                unchecked(seed ^ 0x2c9277b5),
                4);
            var coordinateOffset = PositiveModulo(
                unchecked(seed ^ 0x13579bdf),
                edgeCoordinates.Length);
            for (var sideOffset = 0;
                sideOffset < 4;
                sideOffset++)
            {
                var side =
                    (primarySide + sideOffset) % 4;
                for (var coordinateIndex = 0;
                    coordinateIndex <
                        edgeCoordinates.Length;
                    coordinateIndex++)
                {
                    var coordinate =
                        edgeCoordinates[
                            (coordinateOffset +
                             coordinateIndex) %
                            edgeCoordinates.Length];
                    switch (side)
                    {
                        case 0:
                            candidates.Add(
                                new EdgeGaragePortal(
                                    new Vector2Int(
                                        portalInset,
                                        coordinate),
                                    GridDirection.Left));
                            break;
                        case 1:
                            candidates.Add(
                                new EdgeGaragePortal(
                                    new Vector2Int(
                                        BoardLayoutConfig
                                            .GridColumns - 1 -
                                        portalInset,
                                        coordinate),
                                    GridDirection.Right));
                            break;
                        case 2:
                            candidates.Add(
                                new EdgeGaragePortal(
                                    new Vector2Int(
                                        coordinate,
                                        portalInset),
                                    GridDirection.Down));
                            break;
                        default:
                            candidates.Add(
                                new EdgeGaragePortal(
                                    new Vector2Int(
                                        coordinate,
                                        BoardLayoutConfig
                                            .GridRows - 1 -
                                        portalInset),
                                    GridDirection.Up));
                            break;
                    }
                }
            }

            return candidates;
        }

        private static int GetRequiredGaragePortalInset(
            LogicalPlan plan,
            int dependencyTarget)
        {
            var maximumForwardExtent = 0f;
            for (var garageIndex = 0;
                garageIndex <
                    plan.GarageVehicles.Length;
                garageIndex++)
            {
                var sequence =
                    plan.GarageVehicles[garageIndex];
                for (var progress = 0;
                    progress < sequence.Length;
                    progress++)
                {
                    var probe =
                        new BusDefinition(
                            sequence[progress].Color,
                            sequence[progress].Size,
                            GridDirection.Right,
                            Vector2Int.zero);
                    maximumForwardExtent = Mathf.Max(
                        maximumForwardExtent,
                        BoardLayoutConfig
                            .GetVehicleVisualFootprintCells(
                                probe)
                            .ProjectMax(
                                Vector2.right));
                }
            }

            var frontInset = Mathf.CeilToInt(
                maximumForwardExtent +
                MinimumGarageBlockerLaneCells);
            var maximumPortalInset = Mathf.Max(
                2,
                (Mathf.Min(
                     BoardLayoutConfig.GridColumns,
                     BoardLayoutConfig.GridRows) -
                 2) /
                2);
            var nonLargePrefixCount = 0;
            for (var regularIndex = 0;
                regularIndex <
                    plan.RegularPrefixCount;
                regularIndex++)
            {
                if (plan.RegularVehicles[regularIndex].Size !=
                    BusSize.Large)
                {
                    nonLargePrefixCount++;
                }
            }

            // Small and Medium blockers fit at root depth one with a portal
            // inset of five. If there are not enough such prefix vehicles to
            // satisfy the dependency target, reserve the depth-two Large
            // fallback by moving every portal to inset six.
            var blockerSafeInset =
                nonLargePrefixCount >= dependencyTarget
                    ? 5
                    : 6;
            return Mathf.Clamp(
                Mathf.Max(
                    frontInset + 1,
                    blockerSafeInset),
                3,
                maximumPortalInset);
        }

        private static GarageDefinition CreateGarage(
            EdgeGaragePortal portal,
            IReadOnlyList<LogicalVehicleSpec> sequence)
        {
            var frontCell =
                portal.GridPosition +
                GridDirectionUtility.ToGridVector(
                    portal.ExitDirection);
            var frontVehicle = CreateGarageVehicle(
                sequence[0],
                portal.ExitDirection,
                frontCell);
            var queuedVehicles =
                new List<BusDefinition>(
                    Mathf.Max(0, sequence.Count - 1));
            for (var index = 1;
                index < sequence.Count;
                index++)
            {
                queuedVehicles.Add(
                    CreateGarageVehicle(
                        sequence[index],
                        portal.ExitDirection,
                        frontCell));
            }

            return new GarageDefinition(
                portal.GridPosition,
                portal.ExitDirection,
                frontVehicle,
                queuedVehicles);
        }

        private static BusDefinition CreateGarageVehicle(
            LogicalVehicleSpec spec,
            GridDirection direction,
            Vector2Int frontCell)
        {
            return new BusDefinition(
                spec.Color,
                spec.Size,
                direction,
                frontCell,
                0f,
                Vector2.zero,
                false);
        }

        private static bool IsGaragePlacementCompatible(
            GarageDefinition candidate,
            IReadOnlyList<GarageDefinition> placed)
        {
            if (!BoardLayoutConfig.IsInsideGrid(
                    candidate.GridPosition) ||
                !BoardLayoutConfig.IsInsideGrid(
                    candidate.FrontVehicleGridPosition))
            {
                return false;
            }

            var candidatePortal =
                GetGarageFootprint(candidate);
            foreach (var vehicle in
                candidate.EnumerateVehicles())
            {
                if (BoardLayoutConfig
                    .GetVehicleVisualFootprintCells(vehicle)
                    .Overlaps(candidatePortal))
                {
                    return false;
                }
            }

            for (var index = 0;
                index < placed.Count;
                index++)
            {
                var other = placed[index];
                var otherPortal =
                    GetGarageFootprint(other);
                if (candidatePortal.Overlaps(otherPortal))
                {
                    return false;
                }

                foreach (var candidateVehicle in
                    candidate.EnumerateVehicles())
                {
                    var candidateVehicleFootprint =
                        BoardLayoutConfig
                            .GetVehicleVisualFootprintCells(
                                candidateVehicle);
                    if (candidateVehicleFootprint
                        .Overlaps(otherPortal))
                    {
                        return false;
                    }

                    foreach (var otherVehicle in
                        other.EnumerateVehicles())
                    {
                        if (candidateVehicleFootprint
                            .Overlaps(
                                BoardLayoutConfig
                                    .GetVehicleVisualFootprintCells(
                                        otherVehicle)))
                        {
                            return false;
                        }
                    }
                }

                foreach (var otherVehicle in
                    other.EnumerateVehicles())
                {
                    if (BoardLayoutConfig
                        .GetVehicleVisualFootprintCells(
                            otherVehicle)
                        .Overlaps(candidatePortal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool
            TryPlaceRegularVehiclesInReverseWitnessOrder(
                StageGenerationRequest request,
                LevelDifficultyProfile profile,
                LogicalPlan plan,
                IReadOnlyList<GarageDefinition> garages,
                int dependencyTarget,
                int candidateSeed,
                int layoutProbeIndex,
                CancellationToken cancellationToken,
                ConstructiveOperationBudget operationBudget,
                int layoutPlacementLimit,
                out List<BusDefinition> regularVehicles,
                out int initialOpeningCount,
                out int maximumInitialOpeningCount,
                out bool garageDependencyEvaluated,
                out int garageDependencyCount,
                out bool suffixOnlyReleaseEvaluated,
                out int suffixOnlyReleasedGarageCount,
                out int finalRollingSuffixRootCount,
                out int suffixRootEvaluationCount,
                out int chainLinkCount,
                out int chainBlockedRootCount,
                out int chainMergeCount,
                out int requiredMergeCount,
                out int targetedMotifAttemptCount,
                out int targetedMotifSuccessCount,
                out int prefixRepairAttemptCount,
                out int prefixRepairNodeCount,
                out int prefixRepairSuccessCount,
                out string diagnostic)
        {
            diagnostic = string.Empty;
            regularVehicles = null;
            initialOpeningCount = 0;
            garageDependencyEvaluated = false;
            garageDependencyCount = 0;
            suffixOnlyReleaseEvaluated = false;
            suffixOnlyReleasedGarageCount = 0;
            finalRollingSuffixRootCount = 0;
            suffixRootEvaluationCount = 0;
            chainLinkCount = 0;
            chainBlockedRootCount = 0;
            chainMergeCount = 0;
            requiredMergeCount = 0;
            targetedMotifAttemptCount = 0;
            targetedMotifSuccessCount = 0;
            prefixRepairAttemptCount = 0;
            prefixRepairNodeCount = 0;
            prefixRepairSuccessCount = 0;
            maximumInitialOpeningCount =
                GetMaximumInitialOpeningCount(
                    profile,
                    plan.RegularVehicles.Count +
                    garages.Count,
                    garages.Count);
            var placementSeed = unchecked(
                candidateSeed +
                (layoutProbeIndex + 1) *
                LayoutProbeStride);
            var slots =
                VehicleLayoutPatternEngine.CreateSlots(
                    profile,
                    new System.Random(placementSeed),
                    plan.RegularVehicles.Count,
                    request.VehicleLayoutVariantIndex);
            var authoredSlotCount = slots.Count;
            var designatedGarageByRegularIndex =
                BuildDesignatedGarageBlockerAssignments(
                    plan,
                    garages.Count,
                    dependencyTarget,
                    layoutProbeIndex);
            var designatedDependencySlotByRegularIndex =
                new int[plan.RegularPrefixCount];
            for (var regularIndex = 0;
                regularIndex <
                    designatedDependencySlotByRegularIndex.Length;
                regularIndex++)
            {
                designatedDependencySlotByRegularIndex[
                    regularIndex] = -1;
            }

            var dependencyBlockerSlotStart =
                slots.Count;
            AppendGarageBlockerCandidateSlots(
                slots,
                garages,
                plan,
                designatedGarageByRegularIndex,
                designatedDependencySlotByRegularIndex);
            var dependencyBlockerSlotCount =
                slots.Count -
                dependencyBlockerSlotStart;
            AppendGridCandidateSlots(
                slots,
                placementSeed);
            if (slots.Count <
                plan.RegularVehicles.Count)
            {
                diagnostic =
                    $"Layout probe {layoutProbeIndex} produced only {slots.Count} " +
                    $"slots for {plan.RegularVehicles.Count} regular vehicles.";
                return false;
            }

            var slotOrder =
                new List<int>(slots.Count);
            for (var index = 0;
                index < slots.Count;
                index++)
            {
                slotOrder.Add(index);
            }

            if (layoutProbeIndex > 0)
            {
                Shuffle(
                    slotOrder,
                    new System.Random(
                        unchecked(
                            placementSeed ^
                            0x5f3759df)));
            }

            var usedSlots = new bool[slots.Count];
            var placedLaterVehicles =
                new List<BusDefinition>(
                    plan.RegularVehicles.Count);
            var placements =
                new BusDefinition[
                    plan.RegularVehicles.Count];
            var pathProbeBuses =
                new List<BusDefinition>(
                    plan.RegularVehicles.Count +
                    garages.Count + 1);
            var pathProbeActive =
                new bool[
                    plan.RegularVehicles.Count +
                    garages.Count + 1];
            var garageCorridors =
                BuildGarageExitCorridors(
                    garages);
            operationBudget.SetPhase(
                $"layout-{layoutProbeIndex}:suffix-root-bootstrap");
            var hasRollingSuffixOpeningState =
                TryCreateOpeningChainState(
                    Array.Empty<BusDefinition>(),
                    garages,
                    pathProbeBuses,
                    pathProbeActive,
                    cancellationToken,
                    operationBudget,
                    out var rollingSuffixOpeningState,
                    out _,
                    out _,
                    out var rollingSuffixDiagnostic);
            finalRollingSuffixRootCount =
                rollingSuffixOpeningState.Targets.Count;
            if (!hasRollingSuffixOpeningState)
            {
                diagnostic =
                    "Garage-only rolling suffix-root bootstrap failed: " +
                    rollingSuffixDiagnostic;
                return false;
            }

            operationBudget.SetPhase(
                $"layout-{layoutProbeIndex}:suffix-placement");

            // The suffix is replayed only after every garage has drained.
            // Reverse-insert it first while rolling an exact opening-root
            // state from the garage-only replay. Static-ranked finalists must
            // keep their own exit and every garage root open; among at most
            // two valid exact options, prefer the smallest next root count.
            // The complete suffix is still checked by the unchanged full
            // exact state and counterfactual gates below.
            for (var regularIndex =
                    plan.RegularVehicles.Count - 1;
                regularIndex >=
                    plan.RegularPrefixCount;
                regularIndex--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var spec =
                    plan.RegularVehicles[regularIndex];
                var clearLaterExitCorridors =
                    BuildCurrentlyClearVehicleExitCorridors(
                        placedLaterVehicles);
                var suffixCandidates =
                    new List<SuffixPlacementCandidate>(
                        MaximumScoredSuffixCandidateCount);
                var startOffset = slotOrder.Count > 0
                    ? PositiveModulo(
                        regularIndex * 17 +
                        layoutProbeIndex * 31,
                        slotOrder.Count)
                    : 0;
                var localProbeCount = 0;
                for (var orderOffset = 0;
                    orderOffset < slotOrder.Count &&
                    localProbeCount <
                        MaximumSuffixStaticProbeCountPerVehicle &&
                    suffixCandidates.Count <
                        MaximumScoredSuffixCandidateCount;
                    orderOffset++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var orderIndex =
                        (startOffset + orderOffset) %
                        slotOrder.Count;
                    var slotIndex =
                        slotOrder[orderIndex];
                    if (usedSlots[slotIndex])
                    {
                        continue;
                    }

                    var slot = slots[slotIndex];
                    var directions =
                        GetDirectionCandidates(slot);
                    for (var directionIndex = 0;
                        directionIndex <
                            directions.Count &&
                        localProbeCount <
                            MaximumSuffixStaticProbeCountPerVehicle &&
                        suffixCandidates.Count <
                            MaximumScoredSuffixCandidateCount;
                        directionIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        localProbeCount++;
                        if (operationBudget.PlacementCount >=
                            layoutPlacementLimit)
                        {
                            diagnostic =
                                $"Layout probe {layoutProbeIndex} placement quota " +
                                $"exhausted at {operationBudget.PlacementCount}/" +
                                $"{layoutPlacementLimit} during suffix vehicle " +
                                $"{regularIndex + 1}/{plan.RegularVehicles.Count}.";
                            return false;
                        }

                        if (!operationBudget
                            .TryConsumePlacement())
                        {
                            diagnostic =
                                operationBudget
                                    .CreateDiagnostic();
                            return false;
                        }

                        var candidate =
                            new BusDefinition(
                                spec.Color,
                                spec.Size,
                                directions[directionIndex],
                                slot.GridPosition,
                                slot.AngleOffsetDegrees,
                                slot.PositionOffsetCells,
                                false);
                        if (!IsRegularPlacementValid(
                                candidate,
                                placedLaterVehicles,
                                garages))
                        {
                            continue;
                        }

                        if (DoesVehicleIntersectGarageCorridors(
                                candidate,
                                garageCorridors))
                        {
                            continue;
                        }

                        if (!IsExitCorridorClearAgainstLaterVehicles(
                                candidate,
                                placedLaterVehicles))
                        {
                            continue;
                        }

                        var blockedClearExitCount =
                            CountCorridorsBlockedByVehicle(
                                candidate,
                                clearLaterExitCorridors);
                        suffixCandidates.Add(
                            new SuffixPlacementCandidate(
                                candidate,
                                slotIndex,
                                blockedClearExitCount,
                                slotIndex <
                                    authoredSlotCount,
                                suffixCandidates.Count));
                    }
                }

                if (suffixCandidates.Count == 0)
                {
                    diagnostic =
                        $"Layout probe {layoutProbeIndex} could not reverse-place " +
                        $"regular suffix witness vehicle {regularIndex + 1}/" +
                        $"{plan.RegularVehicles.Count} after {localProbeCount} probes.";
                    return false;
                }

                suffixCandidates.Sort(
                    (left, right) =>
                    {
                        var blockedComparison =
                            right.BlockedClearExitCount
                                .CompareTo(
                                    left.BlockedClearExitCount);
                        if (blockedComparison != 0)
                        {
                            return blockedComparison;
                        }

                        var authoredComparison =
                            right.IsAuthoredSlot
                                .CompareTo(
                                    left.IsAuthoredSlot);
                        return authoredComparison != 0
                            ? authoredComparison
                            : left.Ordinal.CompareTo(
                                right.Ordinal);
                    });
                var exactFinalistCount = Mathf.Min(
                    MaximumExactSuffixFinalistCount,
                    suffixCandidates.Count);
                var selected =
                    default(SuffixPlacementCandidate);
                var selectedExact = false;
                List<int> selectedBlockedTargetIndices =
                    null;
                var selectedNextRootCount =
                    int.MaxValue;
                var validExactOptionCount = 0;
                var theoreticalRootFloor =
                    garages.Count + 1;
                for (var finalistIndex = 0;
                    finalistIndex <
                        exactFinalistCount &&
                    validExactOptionCount <
                        MaximumValidExactSuffixRootOptionCount;
                    finalistIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var finalist =
                        suffixCandidates[
                            finalistIndex];
                    suffixRootEvaluationCount++;
                    if (!TryEvaluateOpeningChainTransition(
                            finalist.Candidate,
                            placedLaterVehicles,
                            garages,
                            rollingSuffixOpeningState,
                            pathProbeBuses,
                            pathProbeActive,
                            cancellationToken,
                            operationBudget,
                            out var candidateExitClear,
                            out var blockedTargetIndices,
                            out var blockedGarageCount))
                    {
                        diagnostic =
                            operationBudget
                                .CreateDiagnostic();
                        return false;
                    }

                    if (!candidateExitClear ||
                        blockedGarageCount != 0)
                    {
                        continue;
                    }

                    validExactOptionCount++;
                    var nextRootCount =
                        rollingSuffixOpeningState
                            .Targets.Count -
                        blockedTargetIndices.Count +
                        1;
                    var isBetterRootOption =
                        !selectedExact ||
                        nextRootCount <
                            selectedNextRootCount ||
                        (nextRootCount ==
                             selectedNextRootCount &&
                         (finalist.BlockedClearExitCount >
                              selected.BlockedClearExitCount ||
                          (finalist.BlockedClearExitCount ==
                               selected.BlockedClearExitCount &&
                           (finalist.IsAuthoredSlot &&
                            !selected.IsAuthoredSlot ||
                            (finalist.IsAuthoredSlot ==
                                 selected.IsAuthoredSlot &&
                             finalist.Ordinal <
                                 selected.Ordinal)))));
                    if (isBetterRootOption)
                    {
                        selected = finalist;
                        selectedExact = true;
                        selectedBlockedTargetIndices =
                            blockedTargetIndices;
                        selectedNextRootCount =
                            nextRootCount;
                    }

                    if (nextRootCount <=
                        theoreticalRootFloor)
                    {
                        break;
                    }
                }

                if (!selectedExact)
                {
                    diagnostic =
                        $"Layout probe {layoutProbeIndex} suffix vehicle " +
                        $"{regularIndex + 1}/{plan.RegularVehicles.Count} had " +
                        $"{suffixCandidates.Count} geometric candidates but no " +
                        $"garage-preserving exact-clear top-{exactFinalistCount} " +
                        $"finalist after {localProbeCount} static probes.";
                    return false;
                }

                rollingSuffixOpeningState.ApplyTransition(
                    selectedBlockedTargetIndices,
                    0,
                    new OpeningTarget(
                        false,
                        placedLaterVehicles.Count,
                        -1,
                        selected.Candidate,
                        BuildVehicleExitCorridor(
                            selected.Candidate)));
                finalRollingSuffixRootCount =
                    rollingSuffixOpeningState
                        .Targets.Count;
                placements[regularIndex] =
                    selected.Candidate;
                placedLaterVehicles.Add(
                    selected.Candidate);
                usedSlots[selected.SlotIndex] = true;
            }

            // This exact state is restored after the regular prefix has left.
            // Preserve it for the unchanged final release gate, and also use
            // it as the exact opening-root state for incremental chain debt.
            var suffixVehicles =
                new List<BusDefinition>(
                    placedLaterVehicles);
            operationBudget.SetPhase(
                $"layout-{layoutProbeIndex}:opening-chain-root");
            if (!TryCreateOpeningChainState(
                    suffixVehicles,
                    garages,
                    pathProbeBuses,
                    pathProbeActive,
                    cancellationToken,
                    operationBudget,
                    out var openingChainState,
                    out suffixOnlyReleaseEvaluated,
                    out suffixOnlyReleasedGarageCount,
                    out diagnostic))
            {
                return false;
            }

            if (openingChainState.Targets.Count !=
                finalRollingSuffixRootCount)
            {
                diagnostic =
                    $"Rolling suffix roots {finalRollingSuffixRootCount} " +
                    $"diverged from authoritative full exact roots " +
                    $"{openingChainState.Targets.Count}.";
                return false;
            }

            requiredMergeCount = Mathf.Max(
                0,
                openingChainState.Targets.Count -
                maximumInitialOpeningCount);
            operationBudget.SetPhase(
                $"layout-{layoutProbeIndex}:prefix-placement");
            var recentPrefixDecisions =
                new List<PrefixDecisionFrame>(
                    MaximumPrefixRepairDepth);

            // Prefix insertion maintains a monotonic exact-root invariant.
            // If O roots are currently open and r prefix vehicles remain,
            // M=max(0,O+r-cap) roots still have to be blocked. ceil(M/r) is
            // the preferred balanced transition, while the hard minimum only
            // prevents consuming more debt than pair-merges can repay later.
            for (var regularIndex =
                    plan.RegularPrefixCount - 1;
                regularIndex >= 0;
                regularIndex--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var spec =
                    plan.RegularVehicles[regularIndex];
                var remainingPrefixCount =
                    regularIndex + 1;
                var openingDebt = Mathf.Max(
                    0,
                    openingChainState.Targets.Count +
                    remainingPrefixCount -
                    maximumInitialOpeningCount);
                var desiredExactBlockedRoots =
                    openingDebt > 0
                        ? Mathf.CeilToInt(
                            (float)openingDebt /
                            remainingPrefixCount)
                        : 0;
                var minimumRequiredBlockedRoots =
                    Mathf.Max(
                        0,
                        openingDebt -
                        MaximumOpeningRootBlocksPerFuturePrefix *
                        (remainingPrefixCount - 1));
                var garageDebt = Mathf.Max(
                    0,
                    dependencyTarget -
                    openingChainState
                        .BlockedGarageCount);
                var minimumExactBlockedGarages =
                    Mathf.Max(
                        0,
                        garageDebt -
                        (remainingPrefixCount - 1));
                var assignedGarageIndex =
                    regularIndex <
                    designatedGarageByRegularIndex.Length
                        ? designatedGarageByRegularIndex[
                            regularIndex]
                        : -1;
                var assignedGarageMustBeBlocked =
                    assignedGarageIndex >= 0 &&
                    openingChainState.HasOpenGarage(
                        assignedGarageIndex);
                var requiresExtendedPrefixProbe =
                    minimumRequiredBlockedRoots > 0 ||
                    minimumExactBlockedGarages > 0 ||
                    assignedGarageMustBeBlocked;
                var targetedHasSelected = false;
                var targetedSelected =
                    default(PrefixPlacementCandidate);
                List<int> targetedSelectedBlockedTargetIndices =
                    null;
                var targetedSelectedBlockedGarageCount = 0;
                var targetedHasFallback = false;
                var targetedFallback =
                    default(PrefixPlacementCandidate);
                List<int> targetedFallbackBlockedTargetIndices =
                    null;
                var targetedFallbackBlockedGarageCount = 0;
                var targetedFallbackBlockedRootCount = -1;
                if (desiredExactBlockedRoots > 0)
                {
                    var hasSuccessorSpec =
                        regularIndex > 0;
                    var successorSpec =
                        hasSuccessorSpec
                            ? plan.RegularVehicles[
                                regularIndex - 1]
                            : default(LogicalVehicleSpec);
                    var targetedMotifCandidates =
                        BuildTargetedTMotifCandidates(
                            spec,
                            openingChainState,
                            placedLaterVehicles,
                            garages,
                            desiredExactBlockedRoots,
                            assignedGarageIndex,
                            remainingPrefixCount,
                            openingDebt,
                            hasSuccessorSpec,
                            successorSpec);
                    var exactTargetedFinalistCount =
                        Mathf.Min(
                            MaximumExactTargetedMotifFinalistCount,
                            targetedMotifCandidates.Count);
                    for (var targetedIndex = 0;
                        targetedIndex <
                            exactTargetedFinalistCount;
                        targetedIndex++)
                    {
                        cancellationToken
                            .ThrowIfCancellationRequested();
                        if (operationBudget.PlacementCount >=
                            layoutPlacementLimit)
                        {
                            diagnostic =
                                $"Layout probe {layoutProbeIndex} placement quota " +
                                $"exhausted at {operationBudget.PlacementCount}/" +
                                $"{layoutPlacementLimit} during targeted motif " +
                                $"for prefix vehicle {regularIndex + 1}/" +
                                $"{plan.RegularPrefixCount}; " +
                                CreateOpeningChainDiagnostic(
                                    openingChainState,
                                    chainLinkCount,
                                    chainBlockedRootCount,
                                    chainMergeCount,
                                    requiredMergeCount);
                            return false;
                        }

                        if (!operationBudget
                                .TryConsumePlacement())
                        {
                            diagnostic =
                                operationBudget
                                    .CreateDiagnostic();
                            return false;
                        }

                        openingChainState
                            .RecordTargetedMotifAttempt();
                        var targeted =
                            targetedMotifCandidates[
                                targetedIndex];
                        if (!TryEvaluateOpeningChainTransition(
                                targeted.Candidate,
                                placedLaterVehicles,
                                garages,
                                openingChainState,
                                pathProbeBuses,
                                pathProbeActive,
                                cancellationToken,
                                operationBudget,
                                out var candidateExitClear,
                                out var blockedTargetIndices,
                                out var blockedGarageCount))
                        {
                            diagnostic =
                                operationBudget
                                    .CreateDiagnostic();
                            return false;
                        }

                        if (!candidateExitClear ||
                            !ContainsBlockedTargetIndex(
                                blockedTargetIndices,
                                targeted
                                    .PrimaryTargetIndex) ||
                            (targeted.IsPair &&
                             desiredExactBlockedRoots > 1 &&
                             !ContainsBlockedTargetIndex(
                                 blockedTargetIndices,
                                 targeted
                                     .SecondaryTargetIndex)) ||
                            blockedGarageCount <
                                minimumExactBlockedGarages ||
                            (assignedGarageMustBeBlocked &&
                             !ContainsBlockedGarageTarget(
                                 openingChainState,
                                 blockedTargetIndices,
                                 assignedGarageIndex)))
                        {
                            continue;
                        }

                        var exactBlockedRootCount =
                            blockedTargetIndices.Count;
                        if (exactBlockedRootCount <
                            minimumRequiredBlockedRoots)
                        {
                            continue;
                        }

                        var blocksAssignedGarage =
                            assignedGarageIndex >= 0 &&
                            ContainsBlockedGarageTarget(
                                openingChainState,
                                blockedTargetIndices,
                                assignedGarageIndex);
                        var score =
                            CalculateOpeningChainCandidateScore(
                                exactBlockedRootCount,
                                blockedGarageCount,
                                blocksAssignedGarage,
                                desiredExactBlockedRoots,
                                minimumExactBlockedGarages,
                                assignedGarageMustBeBlocked);
                        var finalist =
                            new PrefixPlacementCandidate(
                                targeted.Candidate,
                                -1,
                                score,
                                false,
                                targeted.Ordinal);
                        if (exactBlockedRootCount >=
                            desiredExactBlockedRoots)
                        {
                            targetedHasSelected = true;
                            targetedSelected = finalist;
                            targetedSelectedBlockedTargetIndices =
                                blockedTargetIndices;
                            targetedSelectedBlockedGarageCount =
                                blockedGarageCount;
                            break;
                        }

                        var isBetterTargetedFallback =
                            !targetedHasFallback ||
                            exactBlockedRootCount >
                                targetedFallbackBlockedRootCount ||
                            (exactBlockedRootCount ==
                                 targetedFallbackBlockedRootCount &&
                             (finalist.Score <
                                  targetedFallback.Score ||
                              (finalist.Score ==
                                   targetedFallback.Score &&
                               finalist.Ordinal <
                                   targetedFallback.Ordinal)));
                        if (!isBetterTargetedFallback)
                        {
                            continue;
                        }

                        targetedHasFallback = true;
                        targetedFallback = finalist;
                        targetedFallbackBlockedTargetIndices =
                            blockedTargetIndices;
                        targetedFallbackBlockedGarageCount =
                            blockedGarageCount;
                        targetedFallbackBlockedRootCount =
                            exactBlockedRootCount;
                    }
                }

                var prefixSlotOrder =
                    BuildPrefixCandidateSlotOrder(
                        slotOrder,
                        dependencyBlockerSlotStart,
                        dependencyBlockerSlotCount,
                        regularIndex <
                            designatedDependencySlotByRegularIndex
                                .Length
                            ? designatedDependencySlotByRegularIndex[
                                regularIndex]
                            : -1,
                        regularIndex,
                        layoutProbeIndex);
                OrderPrefixCandidateSlotsByOpeningOverlap(
                    prefixSlotOrder,
                    slots,
                    spec,
                    openingChainState,
                    assignedGarageIndex,
                    false);
                var localProbeCount = 0;
                var prefixCandidates =
                    new List<PrefixPlacementCandidate>(
                        MaximumPrefixStaticProbeCountPerVehicle);

                for (var orderOffset = 0;
                    !targetedHasSelected &&
                    orderOffset <
                        prefixSlotOrder.Count &&
                    localProbeCount <
                        MaximumPrefixFallbackStaticProbeCountPerVehicle &&
                    (localProbeCount <
                         MaximumPrefixStaticProbeCountPerVehicle ||
                     requiresExtendedPrefixProbe ||
                     prefixCandidates.Count == 0);
                    orderOffset++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var slotIndex =
                        prefixSlotOrder[orderOffset];
                    if (usedSlots[slotIndex])
                    {
                        continue;
                    }

                    var slot = slots[slotIndex];
                    var directions =
                        GetDirectionCandidates(slot);
                    for (var directionIndex = 0;
                        !targetedHasSelected &&
                        directionIndex <
                            directions.Count &&
                        localProbeCount <
                            MaximumPrefixFallbackStaticProbeCountPerVehicle &&
                        (localProbeCount <
                             MaximumPrefixStaticProbeCountPerVehicle ||
                         requiresExtendedPrefixProbe ||
                         prefixCandidates.Count == 0);
                        directionIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        localProbeCount++;
                        if (operationBudget.PlacementCount >=
                            layoutPlacementLimit)
                        {
                            diagnostic =
                                $"Layout probe {layoutProbeIndex} placement quota " +
                                $"exhausted at {operationBudget.PlacementCount}/" +
                                $"{layoutPlacementLimit} during prefix vehicle " +
                                $"{regularIndex + 1}/{plan.RegularPrefixCount}; " +
                                CreateOpeningChainDiagnostic(
                                    openingChainState,
                                    chainLinkCount,
                                    chainBlockedRootCount,
                                    chainMergeCount,
                                    requiredMergeCount);
                            return false;
                        }

                        if (!operationBudget
                            .TryConsumePlacement())
                        {
                            diagnostic =
                                operationBudget
                                    .CreateDiagnostic();
                            return false;
                        }

                        var candidate =
                            new BusDefinition(
                                spec.Color,
                                spec.Size,
                                directions[directionIndex],
                                slot.GridPosition,
                                slot.AngleOffsetDegrees,
                                slot.PositionOffsetCells,
                                false);
                        if (TryCreatePrefixPlacementCandidate(
                                candidate,
                                slotIndex,
                                slotIndex <
                                    authoredSlotCount,
                                prefixCandidates.Count,
                                placedLaterVehicles,
                                garages,
                                openingChainState,
                                desiredExactBlockedRoots,
                                minimumExactBlockedGarages,
                                assignedGarageIndex,
                                assignedGarageMustBeBlocked,
                                out var scoredCandidate))
                        {
                            prefixCandidates.Add(
                                scoredCandidate);
                        }
                    }
                }

                if (prefixCandidates.Count == 0 &&
                    !targetedHasSelected &&
                    !targetedHasFallback)
                {
                    var prefixFailureDiagnostic =
                        $"Layout probe {layoutProbeIndex} could not reverse-place " +
                        $"regular prefix witness vehicle {regularIndex + 1}/" +
                        $"{plan.RegularPrefixCount} after {localProbeCount} probes; " +
                        $"desiredRootBlocks={desiredExactBlockedRoots}, " +
                        $"hardRootBlocks>={minimumRequiredBlockedRoots}; " +
                        CreateOpeningChainDiagnostic(
                            openingChainState,
                            chainLinkCount,
                            chainBlockedRootCount,
                            chainMergeCount,
                            requiredMergeCount);
                    if (TryRepairPrefixDeadEnd(
                            plan,
                            garages,
                            dependencyTarget,
                            layoutProbeIndex,
                            cancellationToken,
                            operationBudget,
                            layoutPlacementLimit,
                            regularIndex,
                            maximumInitialOpeningCount,
                            slots,
                            slotOrder,
                            authoredSlotCount,
                            dependencyBlockerSlotStart,
                            dependencyBlockerSlotCount,
                            designatedDependencySlotByRegularIndex,
                            designatedGarageByRegularIndex,
                            placedLaterVehicles,
                            placements,
                            usedSlots,
                            openingChainState,
                            pathProbeBuses,
                            pathProbeActive,
                            recentPrefixDecisions,
                            ref chainLinkCount,
                            ref chainBlockedRootCount,
                            ref chainMergeCount,
                            out var repairDiagnostic))
                    {
                        continue;
                    }

                    diagnostic =
                        prefixFailureDiagnostic +
                        $" boundedRepair={repairDiagnostic}; " +
                        CreateOpeningChainDiagnostic(
                            openingChainState,
                            chainLinkCount,
                            chainBlockedRootCount,
                            chainMergeCount,
                            requiredMergeCount);
                    return false;
                }

                prefixCandidates.Sort(
                    (left, right) =>
                    {
                        var scoreComparison =
                            left.Score.CompareTo(
                                right.Score);
                        if (scoreComparison != 0)
                        {
                            return scoreComparison;
                        }

                        var authoredComparison =
                            right.IsAuthoredSlot
                                .CompareTo(
                                    left.IsAuthoredSlot);
                        return authoredComparison != 0
                            ? authoredComparison
                            : left.Ordinal.CompareTo(
                                right.Ordinal);
                    });
                if (prefixCandidates.Count >
                    MaximumScoredPrefixCandidateCount)
                {
                    prefixCandidates.RemoveRange(
                        MaximumScoredPrefixCandidateCount,
                        prefixCandidates.Count -
                        MaximumScoredPrefixCandidateCount);
                }

                var exactFinalistCount = Mathf.Min(
                    MaximumExactPrefixFinalistCount,
                    prefixCandidates.Count);
                var hasSelected =
                    targetedHasSelected;
                var selected =
                    targetedSelected;
                List<int> selectedBlockedTargetIndices =
                    targetedSelectedBlockedTargetIndices;
                var selectedBlockedGarageCount =
                    targetedSelectedBlockedGarageCount;
                var selectedFromTargetedMotif =
                    targetedHasSelected;
                var hasFallback =
                    targetedHasFallback;
                var fallback =
                    targetedFallback;
                List<int> fallbackBlockedTargetIndices =
                    targetedFallbackBlockedTargetIndices;
                var fallbackBlockedGarageCount =
                    targetedFallbackBlockedGarageCount;
                var fallbackBlockedRootCount =
                    targetedFallbackBlockedRootCount;
                var fallbackFromTargetedMotif =
                    targetedHasFallback;
                for (var finalistIndex = 0;
                    !hasSelected &&
                    finalistIndex <
                        exactFinalistCount;
                    finalistIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var finalist =
                        prefixCandidates[
                            finalistIndex];
                    if (!TryEvaluateOpeningChainTransition(
                            finalist.Candidate,
                            placedLaterVehicles,
                            garages,
                            openingChainState,
                            pathProbeBuses,
                            pathProbeActive,
                            cancellationToken,
                            operationBudget,
                            out var candidateExitClear,
                            out var blockedTargetIndices,
                            out var blockedGarageCount))
                    {
                        diagnostic =
                            operationBudget.CreateDiagnostic();
                        return false;
                    }

                    if (!candidateExitClear ||
                        blockedGarageCount <
                            minimumExactBlockedGarages ||
                        (assignedGarageMustBeBlocked &&
                         !ContainsBlockedGarageTarget(
                             openingChainState,
                             blockedTargetIndices,
                             assignedGarageIndex)))
                    {
                        continue;
                    }

                    var exactBlockedRootCount =
                        blockedTargetIndices.Count;
                    if (exactBlockedRootCount <
                        minimumRequiredBlockedRoots)
                    {
                        continue;
                    }

                    if (exactBlockedRootCount >=
                        desiredExactBlockedRoots)
                    {
                        hasSelected = true;
                        selected = finalist;
                        selectedBlockedTargetIndices =
                            blockedTargetIndices;
                        selectedBlockedGarageCount =
                            blockedGarageCount;
                        selectedFromTargetedMotif =
                            false;
                        break;
                    }

                    var isBetterFallback =
                        !hasFallback ||
                        exactBlockedRootCount >
                            fallbackBlockedRootCount ||
                        (exactBlockedRootCount ==
                             fallbackBlockedRootCount &&
                         (finalist.Score <
                              fallback.Score ||
                          (finalist.Score ==
                               fallback.Score &&
                           finalist.Ordinal <
                               fallback.Ordinal)));
                    if (!isBetterFallback)
                    {
                        continue;
                    }

                    hasFallback = true;
                    fallback = finalist;
                    fallbackBlockedTargetIndices =
                        blockedTargetIndices;
                    fallbackBlockedGarageCount =
                        blockedGarageCount;
                    fallbackBlockedRootCount =
                        exactBlockedRootCount;
                    fallbackFromTargetedMotif =
                        false;
                }

                if (!hasSelected &&
                    hasFallback)
                {
                    hasSelected = true;
                    selected = fallback;
                    selectedBlockedTargetIndices =
                        fallbackBlockedTargetIndices;
                    selectedBlockedGarageCount =
                        fallbackBlockedGarageCount;
                    selectedFromTargetedMotif =
                        fallbackFromTargetedMotif;
                }

                if (!hasSelected)
                {
                    var prefixFailureDiagnostic =
                        $"Layout probe {layoutProbeIndex} prefix vehicle " +
                        $"{regularIndex + 1}/{plan.RegularPrefixCount} had " +
                        $"{prefixCandidates.Count} scored candidates but no " +
                        $"exact top-{exactFinalistCount} transition satisfying " +
                        $"desiredRootBlocks={desiredExactBlockedRoots}, " +
                        $"hardRootBlocks>={minimumRequiredBlockedRoots}, " +
                        $"garageBlocks>={minimumExactBlockedGarages}; " +
                        CreateOpeningChainDiagnostic(
                            openingChainState,
                            chainLinkCount,
                            chainBlockedRootCount,
                            chainMergeCount,
                            requiredMergeCount);
                    if (TryRepairPrefixDeadEnd(
                            plan,
                            garages,
                            dependencyTarget,
                            layoutProbeIndex,
                            cancellationToken,
                            operationBudget,
                            layoutPlacementLimit,
                            regularIndex,
                            maximumInitialOpeningCount,
                            slots,
                            slotOrder,
                            authoredSlotCount,
                            dependencyBlockerSlotStart,
                            dependencyBlockerSlotCount,
                            designatedDependencySlotByRegularIndex,
                            designatedGarageByRegularIndex,
                            placedLaterVehicles,
                            placements,
                            usedSlots,
                            openingChainState,
                            pathProbeBuses,
                            pathProbeActive,
                            recentPrefixDecisions,
                            ref chainLinkCount,
                            ref chainBlockedRootCount,
                            ref chainMergeCount,
                            out var repairDiagnostic))
                    {
                        continue;
                    }

                    diagnostic =
                        prefixFailureDiagnostic +
                        $" boundedRepair={repairDiagnostic}; " +
                        CreateOpeningChainDiagnostic(
                            openingChainState,
                            chainLinkCount,
                            chainBlockedRootCount,
                            chainMergeCount,
                            requiredMergeCount);
                    return false;
                }

                var decisionSnapshot =
                    new PrefixPlacementSnapshot(
                        placedLaterVehicles,
                        placements,
                        usedSlots,
                        openingChainState,
                        chainLinkCount,
                        chainBlockedRootCount,
                        chainMergeCount);
                if (selectedFromTargetedMotif)
                {
                    openingChainState
                        .RecordTargetedMotifSuccess();
                }

                var blockedRootCount =
                    selectedBlockedTargetIndices.Count;
                if (blockedRootCount > 0)
                {
                    chainLinkCount++;
                }

                chainBlockedRootCount +=
                    blockedRootCount;
                chainMergeCount += Mathf.Max(
                    0,
                    blockedRootCount - 1);
                openingChainState.ApplyTransition(
                    selectedBlockedTargetIndices,
                    selectedBlockedGarageCount,
                    new OpeningTarget(
                        false,
                        placedLaterVehicles.Count,
                        -1,
                        selected.Candidate,
                        BuildVehicleExitCorridor(
                            selected.Candidate)));
                placements[regularIndex] =
                    selected.Candidate;
                placedLaterVehicles.Add(
                    selected.Candidate);
                if (selected.SlotIndex >= 0)
                {
                    usedSlots[selected.SlotIndex] = true;
                }

                AddRecentPrefixDecision(
                    recentPrefixDecisions,
                    new PrefixDecisionFrame(
                        regularIndex,
                        new PrefixRepairOption(
                            selected,
                            selectedBlockedTargetIndices,
                            selectedBlockedGarageCount,
                            selectedFromTargetedMotif),
                        decisionSnapshot));
            }

            if (openingChainState.Targets.Count >
                    maximumInitialOpeningCount ||
                openingChainState.BlockedGarageCount <
                    dependencyTarget ||
                chainMergeCount <
                    requiredMergeCount)
            {
                diagnostic =
                    "Opening-chain invariant ended outside its exact bounds; " +
                    CreateOpeningChainDiagnostic(
                        openingChainState,
                        chainLinkCount,
                        chainBlockedRootCount,
                        chainMergeCount,
                        requiredMergeCount);
                return false;
            }

            regularVehicles =
                new List<BusDefinition>(placements);

            operationBudget.SetPhase(
                $"layout-{layoutProbeIndex}:suffix-counterfactual");
            if (!AreGarageFrontPathsClear(
                    suffixVehicles,
                    garages,
                    pathProbeBuses,
                    pathProbeActive,
                    cancellationToken,
                    operationBudget,
                    out suffixOnlyReleaseEvaluated,
                    out suffixOnlyReleasedGarageCount))
            {
                diagnostic = operationBudget.HitLimit
                    ? operationBudget.CreateDiagnostic()
                    : $"Regular suffix releases " +
                      $"{suffixOnlyReleasedGarageCount}/{garages.Count} " +
                      "garage corridors; " +
                      CreateOpeningChainDiagnostic(
                          openingChainState,
                          chainLinkCount,
                          chainBlockedRootCount,
                          chainMergeCount,
                          requiredMergeCount);
                regularVehicles = null;
                return false;
            }

            operationBudget.SetPhase(
                $"layout-{layoutProbeIndex}:dependency-gate");
            if (!TryCountBlockedGarageFronts(
                    regularVehicles,
                    garages,
                    pathProbeBuses,
                    pathProbeActive,
                    cancellationToken,
                    operationBudget,
                    out garageDependencyCount))
            {
                diagnostic =
                    operationBudget.CreateDiagnostic();
                regularVehicles = null;
                return false;
            }
            garageDependencyEvaluated = true;

            if (garageDependencyCount <
                dependencyTarget)
            {
                diagnostic =
                    $"Regular prefix blocked {garageDependencyCount} garage " +
                    $"fronts, below dependency target {dependencyTarget}; " +
                    CreateOpeningChainDiagnostic(
                        openingChainState,
                        chainLinkCount,
                        chainBlockedRootCount,
                        chainMergeCount,
                        requiredMergeCount);
                regularVehicles = null;
                return false;
            }

            operationBudget.SetPhase(
                $"layout-{layoutProbeIndex}:opening-gate");
            if (!TryCountInitialOpeningMoves(
                    regularVehicles,
                    garages,
                    pathProbeBuses,
                    pathProbeActive,
                    cancellationToken,
                    operationBudget,
                    out initialOpeningCount))
            {
                diagnostic =
                    operationBudget.CreateDiagnostic();
                regularVehicles = null;
                return false;
            }

            if (initialOpeningCount >
                maximumInitialOpeningCount)
            {
                diagnostic =
                    $"Initial openings {initialOpeningCount} exceed generic " +
                    $"SuperHard+Garage maximum {maximumInitialOpeningCount}; " +
                    CreateOpeningChainDiagnostic(
                        openingChainState,
                        chainLinkCount,
                        chainBlockedRootCount,
                        chainMergeCount,
                        requiredMergeCount);
                regularVehicles = null;
                return false;
            }

            targetedMotifAttemptCount =
                openingChainState.TargetedMotifAttemptCount;
            targetedMotifSuccessCount =
                openingChainState.TargetedMotifSuccessCount;
            prefixRepairAttemptCount =
                openingChainState.PrefixRepairAttemptCount;
            prefixRepairNodeCount =
                openingChainState.PrefixRepairNodeCount;
            prefixRepairSuccessCount =
                openingChainState.PrefixRepairSuccessCount;
            return true;
        }

        private static void AppendGridCandidateSlots(
            List<VehicleLayoutSlot> slots,
            int seed)
        {
            var gridSlots =
                new List<VehicleLayoutSlot>(
                    BoardLayoutConfig.GridColumns *
                    BoardLayoutConfig.GridRows);
            for (var y = 1;
                y < BoardLayoutConfig.GridRows - 1;
                y++)
            {
                for (var x = 1;
                    x < BoardLayoutConfig.GridColumns - 1;
                    x++)
                {
                    var cell = new Vector2Int(x, y);
                    gridSlots.Add(
                        new VehicleLayoutSlot(
                            cell,
                            GetNearestEdgeDirection(cell),
                            0f,
                            Vector2.zero));
                }
            }

            Shuffle(
                gridSlots,
                new System.Random(
                    unchecked(seed ^ 0x13579bdf)));
            slots.AddRange(gridSlots);
        }

        private static int[]
            BuildDesignatedGarageBlockerAssignments(
                LogicalPlan plan,
                int garageCount,
                int dependencyTarget,
                int layoutProbeIndex)
        {
            var assignments =
                new int[plan.RegularPrefixCount];
            for (var index = 0;
                index < assignments.Length;
                index++)
            {
                assignments[index] = -1;
            }

            var eligibleRegularIndices =
                new List<int>(
                    plan.RegularPrefixCount);
            for (var regularIndex =
                    plan.RegularPrefixCount - 1;
                regularIndex >= 0;
                regularIndex--)
            {
                if (plan.RegularVehicles[regularIndex].Size !=
                    BusSize.Large)
                {
                    eligibleRegularIndices.Add(
                        regularIndex);
                }
            }

            // Large roots are a deterministic depth-two fallback only when
            // the Small/Medium reserve cannot cover the dependency target.
            for (var regularIndex =
                    plan.RegularPrefixCount - 1;
                regularIndex >= 0;
                regularIndex--)
            {
                if (plan.RegularVehicles[regularIndex].Size ==
                    BusSize.Large)
                {
                    eligibleRegularIndices.Add(
                        regularIndex);
                }
            }

            var assignmentCount = Mathf.Min(
                Mathf.Min(
                    dependencyTarget,
                    garageCount),
                eligibleRegularIndices.Count);
            for (var assignmentIndex = 0;
                assignmentIndex < assignmentCount;
                assignmentIndex++)
            {
                var regularIndex =
                    eligibleRegularIndices[
                        assignmentIndex];
                assignments[regularIndex] =
                    PositiveModulo(
                        assignmentIndex +
                        layoutProbeIndex,
                        Mathf.Max(1, garageCount));
            }

            return assignments;
        }

        private static void AppendGarageBlockerCandidateSlots(
            ICollection<VehicleLayoutSlot> slots,
            IReadOnlyList<GarageDefinition> garages,
            LogicalPlan plan,
            IReadOnlyList<int> designatedGarageByRegularIndex,
            int[] designatedDependencySlotByRegularIndex)
        {
            for (var regularIndex = 0;
                regularIndex <
                    designatedGarageByRegularIndex.Count;
                regularIndex++)
            {
                var garageIndex =
                    designatedGarageByRegularIndex[
                        regularIndex];
                if (garageIndex < 0 ||
                    garageIndex >= garages.Count)
                {
                    continue;
                }

                var garage = garages[garageIndex];
                var frontCell =
                    garage.FrontVehicleGridPosition;
                var blockerDepth =
                    plan.RegularVehicles[regularIndex].Size ==
                        BusSize.Large
                        ? 2
                        : 1;
                Vector2Int blockerCell;
                switch (garage.ExitDirection)
                {
                    case GridDirection.Left:
                        blockerCell = new Vector2Int(
                            blockerDepth,
                            frontCell.y);
                        break;
                    case GridDirection.Right:
                        blockerCell = new Vector2Int(
                            BoardLayoutConfig.GridColumns - 1 -
                            blockerDepth,
                            frontCell.y);
                        break;
                    case GridDirection.Down:
                        blockerCell = new Vector2Int(
                            frontCell.x,
                            blockerDepth);
                        break;
                    default:
                        blockerCell = new Vector2Int(
                            frontCell.x,
                            BoardLayoutConfig.GridRows - 1 -
                            blockerDepth);
                        break;
                }

                designatedDependencySlotByRegularIndex[
                    regularIndex] = slots.Count;
                slots.Add(
                    new VehicleLayoutSlot(
                        blockerCell,
                        garage.ExitDirection,
                        0f,
                        Vector2.zero));
            }
        }

        private static List<int>
            BuildPrefixCandidateSlotOrder(
                IReadOnlyList<int> slotOrder,
                int dependencyBlockerSlotStart,
                int dependencyBlockerSlotCount,
                int designatedDependencySlotIndex,
                int regularIndex,
                int layoutProbeIndex)
        {
            var orderedSlots =
                new List<int>(slotOrder.Count);
            if (designatedDependencySlotIndex >=
                    dependencyBlockerSlotStart &&
                designatedDependencySlotIndex <
                    dependencyBlockerSlotStart +
                    dependencyBlockerSlotCount)
            {
                orderedSlots.Add(
                    designatedDependencySlotIndex);
            }

            if (slotOrder.Count <= 0)
            {
                return orderedSlots;
            }

            var generalOffset = PositiveModulo(
                regularIndex * 17 +
                layoutProbeIndex * 31,
                slotOrder.Count);
            var dependencyBlockerSlotEnd =
                dependencyBlockerSlotStart +
                dependencyBlockerSlotCount;
            for (var offset = 0;
                offset < slotOrder.Count;
                offset++)
            {
                var slotIndex =
                    slotOrder[
                        (generalOffset + offset) %
                        slotOrder.Count];
                if (slotIndex >=
                        dependencyBlockerSlotStart &&
                    slotIndex <
                        dependencyBlockerSlotEnd)
                {
                    continue;
                }

                orderedSlots.Add(slotIndex);
            }

            return orderedSlots;
        }

        private static void
            OrderPrefixCandidateSlotsByOpeningOverlap(
                List<int> slotIndices,
                IReadOnlyList<VehicleLayoutSlot> slots,
                LogicalVehicleSpec spec,
                OpeningChainState openingChainState,
                int assignedGarageIndex,
                bool preferFewerBlockedRoots)
        {
            var priorities =
                new List<PrefixSlotPriority>(
                    slotIndices.Count);
            for (var ordinal = 0;
                ordinal < slotIndices.Count;
                ordinal++)
            {
                var slotIndex =
                    slotIndices[ordinal];
                var slot = slots[slotIndex];
                var directions =
                    GetDirectionCandidates(slot);
                var maximumBlockedRoots = 0;
                var maximumBlockedGarages = 0;
                var blocksAssignedGarage = false;
                for (var directionIndex = 0;
                    directionIndex < directions.Count;
                    directionIndex++)
                {
                    var candidate =
                        new BusDefinition(
                            spec.Color,
                            spec.Size,
                            directions[directionIndex],
                            slot.GridPosition,
                            slot.AngleOffsetDegrees,
                            slot.PositionOffsetCells,
                            false);
                    var blockedRoots =
                        CountOpeningTargetsBlockedByVehicle(
                            candidate,
                            openingChainState.Targets,
                            assignedGarageIndex,
                            out var blockedGarages,
                            out var blocksAssigned);
                    maximumBlockedRoots = Mathf.Max(
                        maximumBlockedRoots,
                        blockedRoots);
                    maximumBlockedGarages = Mathf.Max(
                        maximumBlockedGarages,
                        blockedGarages);
                    blocksAssignedGarage |=
                        blocksAssigned;
                }

                priorities.Add(
                    new PrefixSlotPriority(
                        slotIndex,
                        maximumBlockedRoots,
                        maximumBlockedGarages,
                        blocksAssignedGarage,
                        ordinal));
            }

            priorities.Sort(
                (left, right) =>
                {
                    var assignedComparison =
                        right.BlocksAssignedGarage
                            .CompareTo(
                                left.BlocksAssignedGarage);
                    if (assignedComparison != 0)
                    {
                        return assignedComparison;
                    }

                    var rootComparison =
                        preferFewerBlockedRoots
                            ? left.BlockedRootCount
                                .CompareTo(
                                    right.BlockedRootCount)
                            : right.BlockedRootCount
                                .CompareTo(
                                    left.BlockedRootCount);
                    if (rootComparison != 0)
                    {
                        return rootComparison;
                    }

                    var garageComparison =
                        right.BlockedGarageCount
                            .CompareTo(
                                left.BlockedGarageCount);
                    return garageComparison != 0
                        ? garageComparison
                        : left.Ordinal.CompareTo(
                            right.Ordinal);
                });
            slotIndices.Clear();
            for (var index = 0;
                index < priorities.Count;
                index++)
            {
                slotIndices.Add(
                    priorities[index].SlotIndex);
            }
        }

        private static bool
            TryCreatePrefixPlacementCandidate(
                BusDefinition candidate,
                int slotIndex,
                bool isAuthoredSlot,
                int ordinal,
                IReadOnlyList<BusDefinition> placedLaterVehicles,
                IReadOnlyList<GarageDefinition> garages,
                OpeningChainState openingChainState,
                int desiredExactBlockedRoots,
                int minimumExactBlockedGarages,
                int assignedGarageIndex,
                bool assignedGarageMustBeBlocked,
                out PrefixPlacementCandidate scoredCandidate)
        {
            scoredCandidate =
                default(PrefixPlacementCandidate);
            if (!IsRegularPlacementValid(
                    candidate,
                    placedLaterVehicles,
                    garages) ||
                !IsExitCorridorClearAgainstActiveState(
                    candidate,
                    placedLaterVehicles,
                    garages))
            {
                return false;
            }

            var blockedRootCount =
                CountOpeningTargetsBlockedByVehicle(
                    candidate,
                    openingChainState.Targets,
                    assignedGarageIndex,
                    out var blockedGarageCount,
                    out var blocksAssignedGarage);
            var score =
                CalculateOpeningChainCandidateScore(
                    blockedRootCount,
                    blockedGarageCount,
                    blocksAssignedGarage,
                    desiredExactBlockedRoots,
                    minimumExactBlockedGarages,
                    assignedGarageMustBeBlocked);
            scoredCandidate =
                new PrefixPlacementCandidate(
                    candidate,
                    slotIndex,
                    score,
                    isAuthoredSlot,
                    ordinal);
            return true;
        }

        private static int
            CountOpeningTargetsBlockedByVehicle(
                BusDefinition vehicle,
                IReadOnlyList<OpeningTarget> targets,
                int assignedGarageIndex,
                out int blockedGarageCount,
                out bool blocksAssignedGarage)
        {
            var footprint =
                BoardLayoutConfig
                    .GetVehicleFootprintCells(
                        vehicle);
            var blockedRootCount = 0;
            blockedGarageCount = 0;
            blocksAssignedGarage = false;
            for (var targetIndex = 0;
                targetIndex < targets.Count;
                targetIndex++)
            {
                var target = targets[targetIndex];
                if (!target.Corridor.Overlaps(
                        footprint,
                        GarageCorridorPaddingCells))
                {
                    continue;
                }

                blockedRootCount++;
                if (!target.IsGarage)
                {
                    continue;
                }

                blockedGarageCount++;
                blocksAssignedGarage |=
                    target.GarageIndex ==
                    assignedGarageIndex;
            }

            return blockedRootCount;
        }

        private static List<TargetedTMotifCandidate>
            BuildTargetedTMotifCandidates(
                LogicalVehicleSpec spec,
                OpeningChainState openingChainState,
                IReadOnlyList<BusDefinition> placedLaterVehicles,
                IReadOnlyList<GarageDefinition> garages,
                int desiredExactBlockedRoots,
                int assignedGarageIndex,
                int remainingPrefixCount,
                int openingDebt,
                bool hasSuccessorSpec,
                LogicalVehicleSpec successorSpec)
        {
            var candidates =
                new List<TargetedTMotifCandidate>(
                    MaximumTargetedMotifCandidateCountPerVehicle);
            var ordinal = 0;
            for (var targetIndex = 0;
                targetIndex <
                    openingChainState.Targets.Count &&
                candidates.Count <
                    MaximumTargetedMotifCandidateCountPerVehicle;
                targetIndex++)
            {
                var target =
                    openingChainState.Targets[targetIndex];
                var targetFootprint =
                    BoardLayoutConfig
                        .GetVehicleFootprintCells(
                            target.Vehicle);
                var targetForward =
                    GetVehicleForwardCells(
                        target.Vehicle);
                if (targetForward.sqrMagnitude <
                    0.0001f)
                {
                    continue;
                }

                var targetWorldDirection =
                    new Vector3(
                        targetForward.x,
                        0f,
                        targetForward.y);
                var sweepDistance =
                    GetVehicleExitSweepDistance(
                        targetFootprint,
                        targetWorldDirection);
                var sampleCount = Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        sweepDistance /
                        TargetedMotifSampleStepCells));
                var clockwise =
                    RotateClockwise(
                        target.Vehicle.Direction);
                var counterClockwise =
                    Opposite(clockwise);
                for (var sampleIndex = 1;
                    sampleIndex <= sampleCount &&
                    candidates.Count <
                        MaximumTargetedMotifCandidateCountPerVehicle;
                    sampleIndex++)
                {
                    var travelDistance = Mathf.Min(
                        sweepDistance,
                        sampleIndex *
                        TargetedMotifSampleStepCells);
                    var desiredCenter =
                        targetFootprint.Center +
                        targetForward *
                        travelDistance;
                    for (var directionIndex = 0;
                        directionIndex < 2 &&
                        candidates.Count <
                            MaximumTargetedMotifCandidateCountPerVehicle;
                        directionIndex++)
                    {
                        var direction =
                            directionIndex == 0
                                ? clockwise
                                : counterClockwise;
                        if (!TryCreateTargetedTMotifPose(
                                spec,
                                desiredCenter,
                                direction,
                                target.Vehicle
                                    .AngleOffsetDegrees,
                                out var candidate) ||
                            !IsRegularPlacementValid(
                                candidate,
                                placedLaterVehicles,
                                garages))
                        {
                            continue;
                        }

                        var candidateFootprint =
                            BoardLayoutConfig
                                .GetVehicleFootprintCells(
                                    candidate);
                        if (!target.Corridor.Overlaps(
                                candidateFootprint,
                                GarageCorridorPaddingCells) ||
                            ContainsTargetedTMotifPose(
                                candidates,
                                candidate))
                        {
                            continue;
                        }

                        var geometricBlockedRootCount =
                            CountOpeningTargetsBlockedByVehicle(
                                candidate,
                                openingChainState.Targets,
                                assignedGarageIndex,
                                out var geometricBlockedGarageCount,
                                out var blocksAssignedGarage);
                        var secondaryTargetIndex =
                            FindSecondaryBlockedTargetIndex(
                                candidateFootprint,
                                openingChainState.Targets,
                                targetIndex);
                        var requiresSuccessor =
                            hasSuccessorSpec &&
                            remainingPrefixCount > 1 &&
                            openingDebt -
                                geometricBlockedRootCount >
                            0;
                        var hasFeasibleSuccessor =
                            !requiresSuccessor ||
                            HasFeasibleTargetedTMotifSuccessor(
                                candidate,
                                successorSpec,
                                placedLaterVehicles,
                                garages);
                        candidates.Add(
                            new TargetedTMotifCandidate(
                                candidate,
                                targetIndex,
                                secondaryTargetIndex,
                                geometricBlockedRootCount,
                                geometricBlockedGarageCount,
                                blocksAssignedGarage,
                                hasFeasibleSuccessor,
                                ordinal++));
                    }
                }
            }

            candidates.Sort(
                (left, right) =>
                {
                    var assignedComparison =
                        right.BlocksAssignedGarage
                            .CompareTo(
                                left.BlocksAssignedGarage);
                    if (assignedComparison != 0)
                    {
                        return assignedComparison;
                    }

                    if (desiredExactBlockedRoots > 1)
                    {
                        var pairComparison =
                            right.IsPair.CompareTo(
                                left.IsPair);
                        if (pairComparison != 0)
                        {
                            return pairComparison;
                        }
                    }

                    var successorComparison =
                        right.HasFeasibleSuccessor
                            .CompareTo(
                                left.HasFeasibleSuccessor);
                    if (successorComparison != 0)
                    {
                        return successorComparison;
                    }

                    var rootComparison =
                        right.GeometricBlockedRootCount
                            .CompareTo(
                                left.GeometricBlockedRootCount);
                    if (rootComparison != 0)
                    {
                        return rootComparison;
                    }

                    var garageComparison =
                        right.GeometricBlockedGarageCount
                            .CompareTo(
                                left.GeometricBlockedGarageCount);
                    return garageComparison != 0
                        ? garageComparison
                        : left.Ordinal.CompareTo(
                            right.Ordinal);
                });
            return candidates;
        }

        private static bool TryCreateTargetedTMotifPose(
            LogicalVehicleSpec spec,
            Vector2 desiredCenter,
            GridDirection direction,
            float angleOffsetDegrees,
            out BusDefinition candidate)
        {
            var rotation =
                GridDirectionUtility.ToRotation(
                    direction,
                    angleOffsetDegrees);
            var worldForward =
                rotation * Vector3.forward;
            var candidateForward =
                new Vector2(
                    worldForward.x,
                    worldForward.z);
            if (candidateForward.sqrMagnitude <
                0.0001f)
            {
                candidate = default(BusDefinition);
                return false;
            }

            candidateForward.Normalize();
            var visualLength =
                BusSizeUtility.ToVisualLengthCells(
                    spec.Size);
            var characterLength =
                visualLength /
                Mathf.Max(
                    1,
                    BusSizeUtility
                        .ToVisualCharacterUnits(
                            spec.Size));
            var rootPosition =
                desiredCenter -
                candidateForward *
                ((visualLength -
                  characterLength) *
                 0.5f);
            if (!TryAnchorTargetedTMotifRoot(
                    rootPosition,
                    out var gridPosition,
                    out var positionOffsetCells))
            {
                candidate = default(BusDefinition);
                return false;
            }

            candidate =
                new BusDefinition(
                    spec.Color,
                    spec.Size,
                    direction,
                    gridPosition,
                    angleOffsetDegrees,
                    positionOffsetCells,
                    false);
            return true;
        }

        private static bool TryAnchorTargetedTMotifRoot(
            Vector2 rootPosition,
            out Vector2Int gridPosition,
            out Vector2 positionOffsetCells)
        {
            gridPosition = default(Vector2Int);
            positionOffsetCells = Vector2.zero;
            if (!IsFinite(rootPosition.x) ||
                !IsFinite(rootPosition.y))
            {
                return false;
            }

            gridPosition =
                new Vector2Int(
                    Mathf.RoundToInt(
                        rootPosition.x),
                    Mathf.RoundToInt(
                        rootPosition.y));
            positionOffsetCells =
                rootPosition -
                new Vector2(
                    gridPosition.x,
                    gridPosition.y);
            return
                BoardLayoutConfig.IsInsideGrid(
                    gridPosition) &&
                positionOffsetCells.sqrMagnitude <=
                    MaximumTargetedMotifPositionOffsetCells *
                    MaximumTargetedMotifPositionOffsetCells;
        }

        private static bool IsFinite(float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }

        private static Vector2 GetVehicleForwardCells(
            BusDefinition vehicle)
        {
            var worldForward =
                vehicle.Rotation *
                Vector3.forward;
            var flat =
                new Vector2(
                    worldForward.x,
                    worldForward.z);
            return flat.sqrMagnitude > 0.0001f
                ? flat.normalized
                : Vector2.zero;
        }

        private static GridDirection RotateClockwise(
            GridDirection direction)
        {
            return (GridDirection)(
                ((int)direction + 1) & 3);
        }

        private static int FindSecondaryBlockedTargetIndex(
            VehicleFootprint candidateFootprint,
            IReadOnlyList<OpeningTarget> targets,
            int primaryTargetIndex)
        {
            for (var targetIndex = 0;
                targetIndex < targets.Count;
                targetIndex++)
            {
                if (targetIndex ==
                        primaryTargetIndex ||
                    !targets[targetIndex]
                        .Corridor.Overlaps(
                            candidateFootprint,
                            GarageCorridorPaddingCells))
                {
                    continue;
                }

                return targetIndex;
            }

            return -1;
        }

        private static bool ContainsTargetedTMotifPose(
            IReadOnlyList<TargetedTMotifCandidate> candidates,
            BusDefinition candidate)
        {
            for (var index = 0;
                index < candidates.Count;
                index++)
            {
                if (SameVehicleContract(
                        candidates[index].Candidate,
                        candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool
            HasFeasibleTargetedTMotifSuccessor(
                BusDefinition currentCandidate,
                LogicalVehicleSpec successorSpec,
                IReadOnlyList<BusDefinition> placedLaterVehicles,
                IReadOnlyList<GarageDefinition> garages)
        {
            var placedWithCurrent =
                new List<BusDefinition>(
                    placedLaterVehicles.Count + 1);
            for (var index = 0;
                index < placedLaterVehicles.Count;
                index++)
            {
                placedWithCurrent.Add(
                    placedLaterVehicles[index]);
            }

            placedWithCurrent.Add(
                currentCandidate);
            var footprint =
                BoardLayoutConfig
                    .GetVehicleFootprintCells(
                        currentCandidate);
            var forward =
                GetVehicleForwardCells(
                    currentCandidate);
            if (forward.sqrMagnitude <
                0.0001f)
            {
                return false;
            }

            var sweepDistance =
                GetVehicleExitSweepDistance(
                    footprint,
                    new Vector3(
                        forward.x,
                        0f,
                        forward.y));
            var sampleCount = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    sweepDistance /
                    TargetedMotifSampleStepCells));
            var clockwise =
                RotateClockwise(
                    currentCandidate.Direction);
            var counterClockwise =
                Opposite(clockwise);
            var currentCorridor =
                BuildVehicleExitCorridor(
                    currentCandidate);
            for (var sampleIndex = 1;
                sampleIndex <= sampleCount;
                sampleIndex++)
            {
                var desiredCenter =
                    footprint.Center +
                    forward *
                    Mathf.Min(
                        sweepDistance,
                        sampleIndex *
                        TargetedMotifSampleStepCells);
                for (var directionIndex = 0;
                    directionIndex < 2;
                    directionIndex++)
                {
                    var direction =
                        directionIndex == 0
                            ? clockwise
                            : counterClockwise;
                    if (!TryCreateTargetedTMotifPose(
                            successorSpec,
                            desiredCenter,
                            direction,
                            currentCandidate
                                .AngleOffsetDegrees,
                            out var successor) ||
                        !IsRegularPlacementValid(
                            successor,
                            placedWithCurrent,
                            garages) ||
                        !currentCorridor.Overlaps(
                            BoardLayoutConfig
                                .GetVehicleFootprintCells(
                                    successor),
                            GarageCorridorPaddingCells))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private static void AddRecentPrefixDecision(
            List<PrefixDecisionFrame> recentDecisions,
            PrefixDecisionFrame decision)
        {
            recentDecisions.Add(
                decision);
            while (recentDecisions.Count >
                MaximumPrefixRepairDepth)
            {
                recentDecisions.RemoveAt(0);
            }
        }

        private static bool TryRepairPrefixDeadEnd(
            LogicalPlan plan,
            IReadOnlyList<GarageDefinition> garages,
            int dependencyTarget,
            int layoutProbeIndex,
            CancellationToken cancellationToken,
            ConstructiveOperationBudget operationBudget,
            int layoutPlacementLimit,
            int failedRegularIndex,
            int maximumInitialOpeningCount,
            IReadOnlyList<VehicleLayoutSlot> slots,
            IReadOnlyList<int> slotOrder,
            int authoredSlotCount,
            int dependencyBlockerSlotStart,
            int dependencyBlockerSlotCount,
            IReadOnlyList<int>
                designatedDependencySlotByRegularIndex,
            IReadOnlyList<int>
                designatedGarageByRegularIndex,
            List<BusDefinition> placedLaterVehicles,
            BusDefinition[] placements,
            bool[] usedSlots,
            OpeningChainState openingChainState,
            List<BusDefinition> pathProbeBuses,
            bool[] pathProbeActive,
            List<PrefixDecisionFrame>
                recentPrefixDecisions,
            ref int chainLinkCount,
            ref int chainBlockedRootCount,
            ref int chainMergeCount,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            var rollbackDepth = Mathf.Min(
                MaximumPrefixRepairDepth,
                recentPrefixDecisions.Count);
            if (rollbackDepth <= 0)
            {
                return false;
            }

            var originalState =
                new PrefixPlacementSnapshot(
                    placedLaterVehicles,
                    placements,
                    usedSlots,
                    openingChainState,
                    chainLinkCount,
                    chainBlockedRootCount,
                    chainMergeCount);
            var originalDecisions =
                new List<PrefixDecisionFrame>(
                    recentPrefixDecisions);
            var startPosition =
                originalDecisions.Count -
                rollbackDepth;
            var startFrame =
                originalDecisions[
                    startPosition];
            if (startFrame.RegularIndex <=
                    failedRegularIndex ||
                startFrame.RegularIndex -
                    failedRegularIndex >
                    MaximumPrefixRepairDepth)
            {
                return false;
            }

            var layoutRepairNodeCountBeforeAttempt =
                openingChainState
                    .PrefixRepairNodeCount;
            var maximumAttemptRepairNodeCount =
                MaximumPrefixRepairNodeCount -
                layoutRepairNodeCountBeforeAttempt;
            if (maximumAttemptRepairNodeCount <= 0)
            {
                diagnostic =
                    $"Layout probe {layoutProbeIndex} bounded prefix repair " +
                    $"layout node quota exhausted at " +
                    $"{layoutRepairNodeCountBeforeAttempt}/" +
                    $"{MaximumPrefixRepairNodeCount}.";
                return false;
            }

            openingChainState
                .RecordPrefixRepairAttempt();
            startFrame.Before.Restore(
                placedLaterVehicles,
                placements,
                usedSlots,
                openingChainState,
                ref chainLinkCount,
                ref chainBlockedRootCount,
                ref chainMergeCount);
            var initialFrames =
                new List<PrefixDecisionFrame>();
            var frontier =
                new List<PrefixRepairBeamState>
                {
                    new PrefixRepairBeamState(
                        startFrame.Before,
                        initialFrames,
                        true,
                        0,
                        openingChainState.Targets.Count,
                        chainMergeCount,
                        0L,
                        0)
                };
            var repairNodeCount = 0;
            var beamOrdinal = 0;
            for (var regularIndex =
                    startFrame.RegularIndex;
                regularIndex >=
                    failedRegularIndex;
                regularIndex--)
            {
                var expanded =
                    new List<PrefixRepairBeamState>(
                        MaximumPrefixRepairWidth *
                        MaximumPrefixRepairWidth);
                for (var frontierIndex = 0;
                    frontierIndex <
                        frontier.Count &&
                    repairNodeCount <
                        maximumAttemptRepairNodeCount;
                    frontierIndex++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();
                    var parent =
                        frontier[frontierIndex];
                    parent.State.Restore(
                        placedLaterVehicles,
                        placements,
                        usedSlots,
                        openingChainState,
                        ref chainLinkCount,
                        ref chainBlockedRootCount,
                        ref chainMergeCount);
                    var originalFrame =
                        FindPrefixDecisionFrame(
                            originalDecisions,
                            regularIndex);
                    if (!TryGetPrefixRepairOptions(
                            plan,
                            garages,
                            dependencyTarget,
                            layoutProbeIndex,
                            cancellationToken,
                            operationBudget,
                            layoutPlacementLimit,
                            regularIndex,
                            maximumInitialOpeningCount,
                            slots,
                            slotOrder,
                            authoredSlotCount,
                            dependencyBlockerSlotStart,
                            dependencyBlockerSlotCount,
                            designatedDependencySlotByRegularIndex,
                            designatedGarageByRegularIndex,
                            placedLaterVehicles,
                            usedSlots,
                            openingChainState,
                            pathProbeBuses,
                            pathProbeActive,
                            out var alternatives,
                            out diagnostic))
                    {
                        originalState.Restore(
                            placedLaterVehicles,
                            placements,
                            usedSlots,
                            openingChainState,
                            ref chainLinkCount,
                            ref chainBlockedRootCount,
                            ref chainMergeCount);
                        return false;
                    }

                    var orderedOptions =
                        BuildOrderedRepairOptions(
                            alternatives,
                            originalFrame,
                            parent.FollowsOriginalPath,
                            regularIndex ==
                                startFrame.RegularIndex &&
                            rollbackDepth > 1);
                    for (var optionIndex = 0;
                        optionIndex <
                            orderedOptions.Count &&
                        optionIndex <
                            MaximumPrefixRepairWidth &&
                        repairNodeCount <
                            maximumAttemptRepairNodeCount;
                        optionIndex++)
                    {
                        parent.State.Restore(
                            placedLaterVehicles,
                            placements,
                            usedSlots,
                            openingChainState,
                            ref chainLinkCount,
                            ref chainBlockedRootCount,
                            ref chainMergeCount);
                        var before =
                            new PrefixPlacementSnapshot(
                                placedLaterVehicles,
                                placements,
                                usedSlots,
                                openingChainState,
                                chainLinkCount,
                                chainBlockedRootCount,
                                chainMergeCount);
                        var option =
                            orderedOptions[
                                optionIndex];
                        ApplyPrefixRepairOption(
                            regularIndex,
                            option,
                            placedLaterVehicles,
                            placements,
                            usedSlots,
                            openingChainState,
                            ref chainLinkCount,
                            ref chainBlockedRootCount,
                            ref chainMergeCount);
                        repairNodeCount++;
                        openingChainState
                            .RecordPrefixRepairNode();
                        var frames =
                            new List<PrefixDecisionFrame>(
                                parent.Frames);
                        frames.Add(
                            new PrefixDecisionFrame(
                                regularIndex,
                                option,
                                before));
                        var followsOriginal =
                            parent.FollowsOriginalPath &&
                            originalFrame != null &&
                            SameVehicleContract(
                                option.Placement
                                    .Candidate,
                                originalFrame
                                    .SelectedOption
                                    .Placement
                                    .Candidate);
                        var nextRegularIndex =
                            regularIndex - 1;
                        var remainingPrefixCount =
                            nextRegularIndex + 1;
                        var openingDebt = Mathf.Max(
                            0,
                            openingChainState
                                .Targets.Count +
                            remainingPrefixCount -
                            maximumInitialOpeningCount);
                        expanded.Add(
                            new PrefixRepairBeamState(
                                new PrefixPlacementSnapshot(
                                    placedLaterVehicles,
                                    placements,
                                    usedSlots,
                                    openingChainState,
                                    chainLinkCount,
                                    chainBlockedRootCount,
                                    chainMergeCount),
                                frames,
                                followsOriginal,
                                openingDebt,
                                openingChainState
                                    .Targets.Count,
                                chainMergeCount,
                                option.Placement.Score,
                                beamOrdinal++));
                    }
                }

                if (expanded.Count == 0)
                {
                    originalState.Restore(
                        placedLaterVehicles,
                        placements,
                        usedSlots,
                        openingChainState,
                        ref chainLinkCount,
                        ref chainBlockedRootCount,
                        ref chainMergeCount);
                    if (string.IsNullOrWhiteSpace(
                            diagnostic))
                    {
                        diagnostic =
                            $"Layout probe {layoutProbeIndex} bounded prefix repair " +
                            $"found no frontier at regular index {regularIndex}; " +
                            $"attemptNodes={repairNodeCount}/" +
                            $"{maximumAttemptRepairNodeCount}, layoutNodes=" +
                            $"{openingChainState.PrefixRepairNodeCount}/" +
                            $"{MaximumPrefixRepairNodeCount}.";
                    }

                    return false;
                }

                expanded.Sort(
                    ComparePrefixRepairBeamStates);
                if (expanded.Count >
                    MaximumPrefixRepairWidth)
                {
                    expanded.RemoveRange(
                        MaximumPrefixRepairWidth,
                        expanded.Count -
                        MaximumPrefixRepairWidth);
                }

                frontier = expanded;
            }

            var winner = frontier[0];
            winner.State.Restore(
                placedLaterVehicles,
                placements,
                usedSlots,
                openingChainState,
                ref chainLinkCount,
                ref chainBlockedRootCount,
                ref chainMergeCount);
            recentPrefixDecisions.Clear();
            for (var index = 0;
                index < startPosition;
                index++)
            {
                AddRecentPrefixDecision(
                    recentPrefixDecisions,
                    originalDecisions[index]);
            }

            for (var index = 0;
                index < winner.Frames.Count;
                index++)
            {
                AddRecentPrefixDecision(
                    recentPrefixDecisions,
                    winner.Frames[index]);
            }

            openingChainState
                .RecordPrefixRepairSuccess();
            diagnostic =
                $"Layout probe {layoutProbeIndex} bounded prefix repair " +
                $"succeeded through regular index {failedRegularIndex}; " +
                $"depth={rollbackDepth}, attemptNodes={repairNodeCount}/" +
                $"{maximumAttemptRepairNodeCount}, layoutNodes=" +
                $"{openingChainState.PrefixRepairNodeCount}/" +
                $"{MaximumPrefixRepairNodeCount}.";
            return true;
        }

        private static PrefixDecisionFrame
            FindPrefixDecisionFrame(
                IReadOnlyList<PrefixDecisionFrame> frames,
                int regularIndex)
        {
            for (var index = 0;
                index < frames.Count;
                index++)
            {
                if (frames[index].RegularIndex ==
                    regularIndex)
                {
                    return frames[index];
                }
            }

            return null;
        }

        private static List<PrefixRepairOption>
            BuildOrderedRepairOptions(
                IReadOnlyList<PrefixRepairOption>
                    alternatives,
                PrefixDecisionFrame originalFrame,
                bool followsOriginalPath,
                bool allowOriginalAtStart)
        {
            var ordered =
                new List<PrefixRepairOption>(
                    MaximumPrefixRepairWidth);
            if (followsOriginalPath &&
                allowOriginalAtStart &&
                originalFrame != null)
            {
                ordered.Add(
                    originalFrame.SelectedOption);
            }

            for (var index = 0;
                index < alternatives.Count &&
                ordered.Count <
                    MaximumPrefixRepairWidth;
                index++)
            {
                var alternative =
                    alternatives[index];
                if (followsOriginalPath &&
                    originalFrame != null &&
                    SameVehicleContract(
                        alternative.Placement
                            .Candidate,
                        originalFrame
                            .SelectedOption
                            .Placement
                            .Candidate))
                {
                    continue;
                }

                ordered.Add(
                    alternative);
            }

            return ordered;
        }

        private static bool TryGetPrefixRepairOptions(
            LogicalPlan plan,
            IReadOnlyList<GarageDefinition> garages,
            int dependencyTarget,
            int layoutProbeIndex,
            CancellationToken cancellationToken,
            ConstructiveOperationBudget operationBudget,
            int layoutPlacementLimit,
            int regularIndex,
            int maximumInitialOpeningCount,
            IReadOnlyList<VehicleLayoutSlot> slots,
            IReadOnlyList<int> slotOrder,
            int authoredSlotCount,
            int dependencyBlockerSlotStart,
            int dependencyBlockerSlotCount,
            IReadOnlyList<int>
                designatedDependencySlotByRegularIndex,
            IReadOnlyList<int>
                designatedGarageByRegularIndex,
            List<BusDefinition> placedLaterVehicles,
            bool[] usedSlots,
            OpeningChainState openingChainState,
            List<BusDefinition> pathProbeBuses,
            bool[] pathProbeActive,
            out List<PrefixRepairOption> options,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            // Repair options are rebuilt from the restored live topology.
            // Re-run both the targeted motif and the normal authored/grid
            // domains so every returned option owns a fresh exact transition.
            options =
                new List<PrefixRepairOption>(
                    MaximumPrefixRepairWidth + 1);
            var remainingPrefixCount =
                regularIndex + 1;
            var openingDebt = Mathf.Max(
                0,
                openingChainState.Targets.Count +
                remainingPrefixCount -
                maximumInitialOpeningCount);
            var desiredExactBlockedRoots =
                openingDebt > 0
                    ? Mathf.CeilToInt(
                        (float)openingDebt /
                        remainingPrefixCount)
                    : 0;
            var minimumRequiredBlockedRoots =
                Mathf.Max(
                    0,
                    openingDebt -
                    MaximumOpeningRootBlocksPerFuturePrefix *
                    (remainingPrefixCount - 1));
            var garageDebt = Mathf.Max(
                0,
                dependencyTarget -
                openingChainState
                    .BlockedGarageCount);
            var minimumExactBlockedGarages =
                Mathf.Max(
                    0,
                    garageDebt -
                    (remainingPrefixCount - 1));
            var assignedGarageIndex =
                regularIndex <
                    designatedGarageByRegularIndex.Count
                    ? designatedGarageByRegularIndex[
                        regularIndex]
                    : -1;
            var assignedGarageMustBeBlocked =
                assignedGarageIndex >= 0 &&
                openingChainState.HasOpenGarage(
                    assignedGarageIndex);
            var hasSuccessorSpec =
                regularIndex > 0;
            var spec =
                plan.RegularVehicles[
                    regularIndex];
            var zeroBlockOptions =
                new List<PrefixRepairOption>(
                    MaximumPrefixRepairWidth + 1);
            var preferred =
                new List<PrefixRepairOption>(
                    MaximumScoredPrefixCandidateCount);
            var fallbacks =
                new List<PrefixRepairOption>(
                    MaximumScoredPrefixCandidateCount);
            var motifCandidates =
                desiredExactBlockedRoots > 0
                    ? BuildTargetedTMotifCandidates(
                        spec,
                        openingChainState,
                        placedLaterVehicles,
                        garages,
                        desiredExactBlockedRoots,
                        assignedGarageIndex,
                        remainingPrefixCount,
                        openingDebt,
                        hasSuccessorSpec,
                        hasSuccessorSpec
                            ? plan.RegularVehicles[
                                regularIndex - 1]
                            : default(LogicalVehicleSpec))
                    : new List<TargetedTMotifCandidate>();
            var exactMotifFinalistCount = Mathf.Min(
                MaximumExactTargetedMotifFinalistCount,
                motifCandidates.Count);
            for (var finalistIndex = 0;
                finalistIndex <
                    exactMotifFinalistCount;
                finalistIndex++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();
                if (operationBudget.PlacementCount >=
                    layoutPlacementLimit)
                {
                    diagnostic =
                        $"Layout probe {layoutProbeIndex} placement quota " +
                        $"exhausted during bounded prefix repair at " +
                        $"{operationBudget.PlacementCount}/" +
                        $"{layoutPlacementLimit}.";
                    return false;
                }

                if (!operationBudget
                        .TryConsumePlacement())
                {
                    diagnostic =
                        operationBudget
                            .CreateDiagnostic();
                    return false;
                }

                openingChainState
                    .RecordTargetedMotifAttempt();
                var targeted =
                    motifCandidates[
                        finalistIndex];
                if (!TryEvaluateOpeningChainTransition(
                        targeted.Candidate,
                        placedLaterVehicles,
                        garages,
                        openingChainState,
                        pathProbeBuses,
                        pathProbeActive,
                        cancellationToken,
                        operationBudget,
                        out var candidateExitClear,
                        out var blockedTargetIndices,
                        out var blockedGarageCount))
                {
                    diagnostic =
                        operationBudget
                            .CreateDiagnostic();
                    return false;
                }

                if (!candidateExitClear ||
                    !ContainsBlockedTargetIndex(
                        blockedTargetIndices,
                        targeted.PrimaryTargetIndex) ||
                    (targeted.IsPair &&
                     desiredExactBlockedRoots > 1 &&
                     !ContainsBlockedTargetIndex(
                         blockedTargetIndices,
                         targeted
                             .SecondaryTargetIndex)) ||
                    blockedGarageCount <
                        minimumExactBlockedGarages ||
                    (assignedGarageMustBeBlocked &&
                     !ContainsBlockedGarageTarget(
                         openingChainState,
                         blockedTargetIndices,
                         assignedGarageIndex)))
                {
                    continue;
                }

                var exactBlockedRootCount =
                    blockedTargetIndices.Count;
                if (exactBlockedRootCount <
                    minimumRequiredBlockedRoots)
                {
                    continue;
                }

                var blocksAssignedGarage =
                    assignedGarageIndex >= 0 &&
                    ContainsBlockedGarageTarget(
                        openingChainState,
                        blockedTargetIndices,
                        assignedGarageIndex);
                var score =
                    CalculateOpeningChainCandidateScore(
                        exactBlockedRootCount,
                        blockedGarageCount,
                        blocksAssignedGarage,
                        desiredExactBlockedRoots,
                        minimumExactBlockedGarages,
                        assignedGarageMustBeBlocked);
                var option =
                    new PrefixRepairOption(
                        new PrefixPlacementCandidate(
                            targeted.Candidate,
                            -1,
                            score,
                            false,
                            targeted.Ordinal),
                        blockedTargetIndices,
                        blockedGarageCount,
                        true);
                if (exactBlockedRootCount >=
                    desiredExactBlockedRoots)
                {
                    preferred.Add(
                        option);
                }
                else
                {
                    fallbacks.Add(
                        option);
                }
            }

            var designatedDependencySlotIndex =
                regularIndex <
                    designatedDependencySlotByRegularIndex.Count
                    ? designatedDependencySlotByRegularIndex[
                        regularIndex]
                    : -1;
            var preferZeroBlock =
                desiredExactBlockedRoots <= 0 &&
                minimumRequiredBlockedRoots <= 0 &&
                minimumExactBlockedGarages <= 0 &&
                !assignedGarageMustBeBlocked;
            var prefixSlotOrder =
                BuildPrefixCandidateSlotOrder(
                    slotOrder,
                    dependencyBlockerSlotStart,
                    dependencyBlockerSlotCount,
                    designatedDependencySlotIndex,
                    regularIndex,
                    layoutProbeIndex);
            OrderPrefixCandidateSlotsByOpeningOverlap(
                prefixSlotOrder,
                slots,
                spec,
                openingChainState,
                assignedGarageIndex,
                preferZeroBlock);
            var requiresExtendedPrefixProbe =
                minimumRequiredBlockedRoots > 0 ||
                minimumExactBlockedGarages > 0 ||
                assignedGarageMustBeBlocked;
            var localProbeCount = 0;
            var normalCandidates =
                new List<PrefixPlacementCandidate>(
                    MaximumPrefixStaticProbeCountPerVehicle);
            for (var orderOffset = 0;
                orderOffset < prefixSlotOrder.Count &&
                localProbeCount <
                    MaximumPrefixFallbackStaticProbeCountPerVehicle &&
                (localProbeCount <
                     MaximumPrefixStaticProbeCountPerVehicle ||
                 requiresExtendedPrefixProbe ||
                 normalCandidates.Count == 0);
                orderOffset++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();
                var slotIndex =
                    prefixSlotOrder[orderOffset];
                if (usedSlots[slotIndex])
                {
                    continue;
                }

                var slot = slots[slotIndex];
                var directions =
                    GetDirectionCandidates(slot);
                for (var directionIndex = 0;
                    directionIndex < directions.Count &&
                    localProbeCount <
                        MaximumPrefixFallbackStaticProbeCountPerVehicle &&
                    (localProbeCount <
                         MaximumPrefixStaticProbeCountPerVehicle ||
                     requiresExtendedPrefixProbe ||
                     normalCandidates.Count == 0);
                    directionIndex++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();
                    localProbeCount++;
                    if (operationBudget.PlacementCount >=
                        layoutPlacementLimit)
                    {
                        diagnostic =
                            $"Layout probe {layoutProbeIndex} placement quota " +
                            $"exhausted during bounded prefix repair at " +
                            $"{operationBudget.PlacementCount}/" +
                            $"{layoutPlacementLimit}.";
                        return false;
                    }

                    if (!operationBudget
                            .TryConsumePlacement())
                    {
                        diagnostic =
                            operationBudget
                                .CreateDiagnostic();
                        return false;
                    }

                    var candidate =
                        new BusDefinition(
                            spec.Color,
                            spec.Size,
                            directions[directionIndex],
                            slot.GridPosition,
                            slot.AngleOffsetDegrees,
                            slot.PositionOffsetCells,
                            false);
                    if (TryCreatePrefixPlacementCandidate(
                            candidate,
                            slotIndex,
                            slotIndex <
                                authoredSlotCount,
                            normalCandidates.Count,
                            placedLaterVehicles,
                            garages,
                            openingChainState,
                            desiredExactBlockedRoots,
                            minimumExactBlockedGarages,
                            assignedGarageIndex,
                            assignedGarageMustBeBlocked,
                            out var scoredCandidate))
                    {
                        normalCandidates.Add(
                            scoredCandidate);
                    }
                }
            }

            normalCandidates.Sort(
                (left, right) =>
                {
                    var scoreComparison =
                        preferZeroBlock
                            ? right.Score.CompareTo(
                                left.Score)
                            : left.Score.CompareTo(
                                right.Score);
                    if (scoreComparison != 0)
                    {
                        return scoreComparison;
                    }

                    var authoredComparison =
                        right.IsAuthoredSlot
                            .CompareTo(
                                left.IsAuthoredSlot);
                    return authoredComparison != 0
                        ? authoredComparison
                        : left.Ordinal.CompareTo(
                            right.Ordinal);
                });
            if (normalCandidates.Count >
                MaximumScoredPrefixCandidateCount)
            {
                normalCandidates.RemoveRange(
                    MaximumScoredPrefixCandidateCount,
                    normalCandidates.Count -
                    MaximumScoredPrefixCandidateCount);
            }

            var exactNormalFinalistCount = Mathf.Min(
                MaximumExactPrefixFinalistCount,
                normalCandidates.Count);
            for (var finalistIndex = 0;
                finalistIndex <
                    exactNormalFinalistCount;
                finalistIndex++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();
                var finalist =
                    normalCandidates[
                        finalistIndex];
                if (!TryEvaluateOpeningChainTransition(
                        finalist.Candidate,
                        placedLaterVehicles,
                        garages,
                        openingChainState,
                        pathProbeBuses,
                        pathProbeActive,
                        cancellationToken,
                        operationBudget,
                        out var candidateExitClear,
                        out var blockedTargetIndices,
                        out var blockedGarageCount))
                {
                    diagnostic =
                        operationBudget
                            .CreateDiagnostic();
                    return false;
                }

                if (!candidateExitClear ||
                    blockedGarageCount <
                        minimumExactBlockedGarages ||
                    (assignedGarageMustBeBlocked &&
                     !ContainsBlockedGarageTarget(
                         openingChainState,
                         blockedTargetIndices,
                         assignedGarageIndex)))
                {
                    continue;
                }

                var exactBlockedRootCount =
                    blockedTargetIndices.Count;
                if (exactBlockedRootCount <
                    minimumRequiredBlockedRoots)
                {
                    continue;
                }

                var option =
                    new PrefixRepairOption(
                        finalist,
                        blockedTargetIndices,
                        blockedGarageCount,
                        false);
                if (exactBlockedRootCount == 0 &&
                    desiredExactBlockedRoots <= 0)
                {
                    zeroBlockOptions.Add(
                        option);
                }
                else if (exactBlockedRootCount >=
                    desiredExactBlockedRoots)
                {
                    preferred.Add(
                        option);
                }
                else
                {
                    fallbacks.Add(
                        option);
                }
            }

            zeroBlockOptions.Sort(
                ComparePrefixRepairOptions);
            preferred.Sort(
                ComparePrefixRepairOptions);
            fallbacks.Sort(
                ComparePrefixRepairOptions);
            AddPrefixRepairOptions(
                options,
                zeroBlockOptions,
                MaximumPrefixRepairWidth + 1);
            AddPrefixRepairOptions(
                options,
                preferred,
                MaximumPrefixRepairWidth + 1);
            AddPrefixRepairOptions(
                options,
                fallbacks,
                MaximumPrefixRepairWidth + 1);
            return true;
        }

        private static int ComparePrefixRepairOptions(
            PrefixRepairOption left,
            PrefixRepairOption right)
        {
            var scoreComparison =
                left.Placement.Score.CompareTo(
                    right.Placement.Score);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            var authoredComparison =
                right.Placement.IsAuthoredSlot
                    .CompareTo(
                        left.Placement.IsAuthoredSlot);
            return authoredComparison != 0
                ? authoredComparison
                : left.Placement.Ordinal.CompareTo(
                    right.Placement.Ordinal);
        }

        private static void AddPrefixRepairOptions(
            List<PrefixRepairOption> destination,
            IReadOnlyList<PrefixRepairOption> source,
            int maximumCount)
        {
            for (var index = 0;
                index < source.Count &&
                destination.Count <
                    maximumCount;
                index++)
            {
                var duplicate = false;
                for (var destinationIndex = 0;
                    destinationIndex <
                        destination.Count;
                    destinationIndex++)
                {
                    if (!SameVehicleContract(
                            destination[
                                destinationIndex]
                                .Placement
                                .Candidate,
                            source[index]
                                .Placement
                                .Candidate))
                    {
                        continue;
                    }

                    duplicate = true;
                    break;
                }

                if (duplicate)
                {
                    continue;
                }

                destination.Add(
                    source[index]);
            }
        }

        private static void ApplyPrefixRepairOption(
            int regularIndex,
            PrefixRepairOption option,
            List<BusDefinition> placedLaterVehicles,
            BusDefinition[] placements,
            bool[] usedSlots,
            OpeningChainState openingChainState,
            ref int chainLinkCount,
            ref int chainBlockedRootCount,
            ref int chainMergeCount)
        {
            if (option.FromTargetedMotif)
            {
                openingChainState
                    .RecordTargetedMotifSuccess();
            }

            var blockedRootCount =
                option.BlockedTargetIndices.Count;
            if (blockedRootCount > 0)
            {
                chainLinkCount++;
            }

            chainBlockedRootCount +=
                blockedRootCount;
            chainMergeCount += Mathf.Max(
                0,
                blockedRootCount - 1);
            openingChainState.ApplyTransition(
                option.BlockedTargetIndices,
                option.BlockedGarageCount,
                new OpeningTarget(
                    false,
                    placedLaterVehicles.Count,
                    -1,
                    option.Placement.Candidate,
                    BuildVehicleExitCorridor(
                        option.Placement.Candidate)));
            placements[regularIndex] =
                option.Placement.Candidate;
            placedLaterVehicles.Add(
                option.Placement.Candidate);
            if (option.Placement.SlotIndex >= 0)
            {
                usedSlots[
                    option.Placement.SlotIndex] =
                    true;
            }
        }

        private static int ComparePrefixRepairBeamStates(
            PrefixRepairBeamState left,
            PrefixRepairBeamState right)
        {
            var debtComparison =
                left.OpeningDebt.CompareTo(
                    right.OpeningDebt);
            if (debtComparison != 0)
            {
                return debtComparison;
            }

            var rootComparison =
                left.OpenRootCount.CompareTo(
                    right.OpenRootCount);
            if (rootComparison != 0)
            {
                return rootComparison;
            }

            var mergeComparison =
                right.ChainMergeCount.CompareTo(
                    left.ChainMergeCount);
            if (mergeComparison != 0)
            {
                return mergeComparison;
            }

            var scoreComparison =
                left.CandidateScore.CompareTo(
                    right.CandidateScore);
            return scoreComparison != 0
                ? scoreComparison
                : left.Ordinal.CompareTo(
                    right.Ordinal);
        }

        private static List<GridDirection>
            GetDirectionCandidates(
                VehicleLayoutSlot slot)
        {
            var directions =
                new List<GridDirection>(4);
            AddDirection(
                directions,
                slot.Direction);
            AddDirection(
                directions,
                Opposite(slot.Direction));
            AddDirection(
                directions,
                GetNearestEdgeDirection(
                    slot.GridPosition));
            AddDirection(
                directions,
                GridDirection.Up);
            AddDirection(
                directions,
                GridDirection.Right);
            AddDirection(
                directions,
                GridDirection.Down);
            AddDirection(
                directions,
                GridDirection.Left);
            return directions;
        }

        private static void AddDirection(
            ICollection<GridDirection> directions,
            GridDirection direction)
        {
            if (!directions.Contains(direction))
            {
                directions.Add(direction);
            }
        }

        private static GridDirection GetNearestEdgeDirection(
            Vector2Int cell)
        {
            var leftDistance = cell.x;
            var rightDistance =
                BoardLayoutConfig.GridColumns - 1 - cell.x;
            var downDistance = cell.y;
            var upDistance =
                BoardLayoutConfig.GridRows - 1 - cell.y;
            var minimum = Mathf.Min(
                Mathf.Min(leftDistance, rightDistance),
                Mathf.Min(downDistance, upDistance));
            if (leftDistance == minimum)
            {
                return GridDirection.Left;
            }

            if (rightDistance == minimum)
            {
                return GridDirection.Right;
            }

            if (downDistance == minimum)
            {
                return GridDirection.Down;
            }

            return GridDirection.Up;
        }

        private static GridDirection Opposite(
            GridDirection direction)
        {
            return (GridDirection)(
                ((int)direction + 2) & 3);
        }

        private static bool IsRegularPlacementValid(
            BusDefinition candidate,
            IReadOnlyList<BusDefinition> placedVehicles,
            IReadOnlyList<GarageDefinition> garages)
        {
            if (!BoardLayoutConfig.IsInsideGrid(
                    candidate.GridPosition) ||
                !IsWithinBoardBounds(candidate))
            {
                return false;
            }

            var logicalFootprint =
                BoardLayoutConfig.GetVehicleFootprintCells(
                    candidate);
            var visualFootprint =
                BoardLayoutConfig
                    .GetVehicleVisualFootprintCells(
                        candidate);
            for (var index = 0;
                index < placedVehicles.Count;
                index++)
            {
                if (logicalFootprint.Overlaps(
                        BoardLayoutConfig
                            .GetVehicleFootprintCells(
                                placedVehicles[index])) ||
                    visualFootprint.Overlaps(
                        BoardLayoutConfig
                            .GetVehicleVisualFootprintCells(
                                placedVehicles[index])))
                {
                    return false;
                }
            }

            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                var garage = garages[garageIndex];
                if (logicalFootprint.Overlaps(
                        GetGarageFootprint(garage)))
                {
                    return false;
                }

                foreach (var garageVehicle in
                    garage.EnumerateVehicles())
                {
                    if (visualFootprint.Overlaps(
                            BoardLayoutConfig
                                .GetVehicleVisualFootprintCells(
                                    garageVehicle)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsWithinBoardBounds(
            BusDefinition vehicle)
        {
            var footprint =
                BoardLayoutConfig
                    .GetVehicleVisualFootprintCells(
                        vehicle);
            var minimum =
                -0.5f -
                PlacementBoundaryPaddingCells;
            var maximumX =
                BoardLayoutConfig.GridColumns -
                0.5f +
                PlacementBoundaryPaddingCells;
            var maximumY =
                BoardLayoutConfig.GridRows -
                0.5f +
                PlacementBoundaryPaddingCells;
            return footprint.ProjectMin(
                       Vector2.right) >= minimum &&
                footprint.ProjectMax(
                    Vector2.right) <= maximumX &&
                footprint.ProjectMin(
                    Vector2.up) >= minimum &&
                footprint.ProjectMax(
                    Vector2.up) <= maximumY;
        }

        private static List<GarageExitCorridor>
            BuildGarageExitCorridors(
                IReadOnlyList<GarageDefinition> garages)
        {
            var corridors =
                new List<GarageExitCorridor>(
                    garages.Count);
            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                var garage = garages[garageIndex];
                var minimumX = float.PositiveInfinity;
                var maximumX = float.NegativeInfinity;
                var minimumY = float.PositiveInfinity;
                var maximumY = float.NegativeInfinity;
                foreach (var vehicle in
                    garage.EnumerateVehicles())
                {
                    var footprint =
                        BoardLayoutConfig
                            .GetVehicleFootprintCells(
                                vehicle);
                    minimumX = Mathf.Min(
                        minimumX,
                        footprint.ProjectMin(
                            Vector2.right));
                    maximumX = Mathf.Max(
                        maximumX,
                        footprint.ProjectMax(
                            Vector2.right));
                    minimumY = Mathf.Min(
                        minimumY,
                        footprint.ProjectMin(
                            Vector2.up));
                    maximumY = Mathf.Max(
                        maximumY,
                        footprint.ProjectMax(
                            Vector2.up));
                }

                const float exitClearanceCells =
                    0.75f;
                switch (garage.ExitDirection)
                {
                    case GridDirection.Left:
                        minimumX =
                            -0.5f -
                            exitClearanceCells;
                        break;
                    case GridDirection.Right:
                        maximumX =
                            BoardLayoutConfig
                                .GridColumns -
                            0.5f +
                            exitClearanceCells;
                        break;
                    case GridDirection.Down:
                        minimumY =
                            -0.5f -
                            exitClearanceCells;
                        break;
                    case GridDirection.Up:
                        maximumY =
                            BoardLayoutConfig
                                .GridRows -
                            0.5f +
                            exitClearanceCells;
                        break;
                }

                corridors.Add(
                    new GarageExitCorridor(
                        minimumX,
                        maximumX,
                        minimumY,
                        maximumY));
            }

            return corridors;
        }

        private static bool
            DoesVehicleIntersectGarageCorridors(
                BusDefinition vehicle,
                IReadOnlyList<GarageExitCorridor> corridors)
        {
            var footprint =
                BoardLayoutConfig
                    .GetVehicleFootprintCells(
                        vehicle);
            for (var index = 0;
                index < corridors.Count;
                index++)
            {
                if (corridors[index].Overlaps(
                        footprint,
                        GarageCorridorPaddingCells))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<GarageExitCorridor>
            BuildCurrentlyClearVehicleExitCorridors(
                IReadOnlyList<BusDefinition> vehicles)
        {
            var clearCorridors =
                new List<GarageExitCorridor>(
                    vehicles.Count);
            for (var movingIndex = 0;
                movingIndex < vehicles.Count;
                movingIndex++)
            {
                var corridor =
                    BuildVehicleExitCorridor(
                        vehicles[movingIndex]);
                var clear = true;
                for (var otherIndex = 0;
                    otherIndex < vehicles.Count;
                    otherIndex++)
                {
                    if (otherIndex == movingIndex)
                    {
                        continue;
                    }

                    if (corridor.Overlaps(
                            BoardLayoutConfig
                                .GetVehicleFootprintCells(
                                    vehicles[otherIndex]),
                            GarageCorridorPaddingCells))
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    clearCorridors.Add(
                        corridor);
                }
            }

            return clearCorridors;
        }

        private static int CountCorridorsBlockedByVehicle(
            BusDefinition vehicle,
            IReadOnlyList<GarageExitCorridor> corridors)
        {
            var footprint =
                BoardLayoutConfig
                    .GetVehicleFootprintCells(
                        vehicle);
            var blockedCount = 0;
            for (var index = 0;
                index < corridors.Count;
                index++)
            {
                if (corridors[index].Overlaps(
                        footprint,
                        GarageCorridorPaddingCells))
                {
                    blockedCount++;
                }
            }

            return blockedCount;
        }

        private static bool
            IsExitCorridorClearAgainstLaterVehicles(
                BusDefinition candidate,
                IReadOnlyList<BusDefinition> laterVehicles)
        {
            var corridor =
                BuildVehicleExitCorridor(
                    candidate);
            for (var index = 0;
                index < laterVehicles.Count;
                index++)
            {
                if (corridor.Overlaps(
                        BoardLayoutConfig
                            .GetVehicleFootprintCells(
                                laterVehicles[index]),
                        GarageCorridorPaddingCells))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool
            IsExitCorridorClearAgainstActiveState(
                BusDefinition candidate,
                IReadOnlyList<BusDefinition> laterVehicles,
                IReadOnlyList<GarageDefinition> garages)
        {
            var corridor =
                BuildVehicleExitCorridor(
                    candidate);
            for (var index = 0;
                index < laterVehicles.Count;
                index++)
            {
                if (corridor.Overlaps(
                        BoardLayoutConfig
                            .GetVehicleFootprintCells(
                                laterVehicles[index]),
                        GarageCorridorPaddingCells))
                {
                    return false;
                }
            }

            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                var garage = garages[garageIndex];
                if (corridor.Overlaps(
                        GetGarageFootprint(garage),
                        GarageCorridorPaddingCells) ||
                    corridor.Overlaps(
                        BoardLayoutConfig
                            .GetVehicleFootprintCells(
                                garage.FrontVehicle),
                        GarageCorridorPaddingCells))
                {
                    return false;
                }
            }

            return true;
        }

        private static int
            CountBlockedGarageCorridorsWithCandidate(
                BusDefinition candidate,
                IReadOnlyList<BusDefinition> otherRegularVehicles,
                IReadOnlyList<GarageExitCorridor> garageCorridors)
        {
            var candidateFootprint =
                BoardLayoutConfig
                    .GetVehicleFootprintCells(
                        candidate);
            var blockedCount = 0;
            for (var corridorIndex = 0;
                corridorIndex <
                    garageCorridors.Count;
                corridorIndex++)
            {
                var corridor =
                    garageCorridors[corridorIndex];
                var blocked = corridor.Overlaps(
                    candidateFootprint,
                    GarageCorridorPaddingCells);
                for (var vehicleIndex = 0;
                    !blocked &&
                    vehicleIndex <
                        otherRegularVehicles.Count;
                    vehicleIndex++)
                {
                    blocked = corridor.Overlaps(
                        BoardLayoutConfig
                            .GetVehicleFootprintCells(
                                otherRegularVehicles[
                                    vehicleIndex]),
                        GarageCorridorPaddingCells);
                }

                if (blocked)
                {
                    blockedCount++;
                }
            }

            return blockedCount;
        }

        private static int
            CountGeometricInitialOpeningMoves(
                IReadOnlyList<BusDefinition> buses,
                IReadOnlyList<GarageDefinition> garages)
        {
            var openingCount = 0;
            for (var movingIndex = 0;
                movingIndex < buses.Count;
                movingIndex++)
            {
                var corridor =
                    BuildVehicleExitCorridor(
                        buses[movingIndex]);
                var clear = true;
                for (var otherIndex = 0;
                    otherIndex < buses.Count;
                    otherIndex++)
                {
                    if (otherIndex == movingIndex)
                    {
                        continue;
                    }

                    if (corridor.Overlaps(
                            BoardLayoutConfig
                                .GetVehicleFootprintCells(
                                    buses[otherIndex]),
                            GarageCorridorPaddingCells))
                    {
                        clear = false;
                        break;
                    }
                }

                for (var garageIndex = 0;
                    clear &&
                    garageIndex < garages.Count;
                    garageIndex++)
                {
                    if (corridor.Overlaps(
                            GetGarageFootprint(
                                garages[garageIndex]),
                            GarageCorridorPaddingCells))
                    {
                        clear = false;
                    }
                }

                if (clear)
                {
                    openingCount++;
                }
            }

            return openingCount;
        }

        private static GarageExitCorridor
            BuildVehicleExitCorridor(
                BusDefinition vehicle)
        {
            var footprint =
                BoardLayoutConfig
                    .GetVehicleFootprintCells(
                        vehicle);
            var minimumX =
                footprint.ProjectMin(
                    Vector2.right);
            var maximumX =
                footprint.ProjectMax(
                    Vector2.right);
            var minimumY =
                footprint.ProjectMin(
                    Vector2.up);
            var maximumY =
                footprint.ProjectMax(
                    Vector2.up);
            var worldDirection =
                vehicle.Rotation *
                Vector3.forward;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <
                0.0001f)
            {
                return new GarageExitCorridor(
                    minimumX,
                    maximumX,
                    minimumY,
                    maximumY);
            }

            worldDirection.Normalize();
            var sweepDistance =
                GetVehicleExitSweepDistance(
                    footprint,
                    worldDirection);
            var deltaX =
                worldDirection.x *
                sweepDistance;
            var deltaY =
                worldDirection.z *
                sweepDistance;
            return new GarageExitCorridor(
                Mathf.Min(
                    minimumX,
                    minimumX + deltaX),
                Mathf.Max(
                    maximumX,
                    maximumX + deltaX),
                Mathf.Min(
                    minimumY,
                    minimumY + deltaY),
                Mathf.Max(
                    maximumY,
                    maximumY + deltaY));
        }

        private static float GetVehicleExitSweepDistance(
            VehicleFootprint footprint,
            Vector3 worldDirection)
        {
            const float exitClearanceCells =
                0.75f;
            var leftBoundary =
                -0.5f -
                exitClearanceCells;
            var rightBoundary =
                BoardLayoutConfig.GridColumns -
                0.5f +
                exitClearanceCells;
            var bottomBoundary =
                -0.5f -
                exitClearanceCells;
            var topBoundary =
                BoardLayoutConfig.GridRows -
                0.5f +
                exitClearanceCells;
            var bestDistance =
                float.PositiveInfinity;
            if (worldDirection.x > 0.001f)
            {
                bestDistance = Mathf.Min(
                    bestDistance,
                    (rightBoundary -
                     footprint.ProjectMax(
                         Vector2.right)) /
                    worldDirection.x);
            }
            else if (worldDirection.x < -0.001f)
            {
                bestDistance = Mathf.Min(
                    bestDistance,
                    (footprint.ProjectMin(
                         Vector2.right) -
                     leftBoundary) /
                    -worldDirection.x);
            }

            if (worldDirection.z > 0.001f)
            {
                bestDistance = Mathf.Min(
                    bestDistance,
                    (topBoundary -
                     footprint.ProjectMax(
                         Vector2.up)) /
                    worldDirection.z);
            }
            else if (worldDirection.z < -0.001f)
            {
                bestDistance = Mathf.Min(
                    bestDistance,
                    (footprint.ProjectMin(
                         Vector2.up) -
                     bottomBoundary) /
                    -worldDirection.z);
            }

            if (float.IsInfinity(bestDistance) ||
                float.IsNaN(bestDistance))
            {
                return Mathf.Max(
                    BoardLayoutConfig.GridColumns,
                    BoardLayoutConfig.GridRows);
            }

            return Mathf.Max(
                0.5f,
                bestDistance);
        }

        private static bool TryCreateOpeningChainState(
            IReadOnlyList<BusDefinition> suffixVehicles,
            IReadOnlyList<GarageDefinition> garages,
            List<BusDefinition> buses,
            bool[] active,
            CancellationToken cancellationToken,
            ConstructiveOperationBudget operationBudget,
            out OpeningChainState openingChainState,
            out bool garageReleaseEvaluated,
            out int releasedGarageCount,
            out string diagnostic)
        {
            openingChainState =
                new OpeningChainState();
            garageReleaseEvaluated = false;
            releasedGarageCount = 0;
            diagnostic = string.Empty;
            PopulatePathProbeState(
                suffixVehicles,
                default,
                false,
                garages,
                buses,
                active,
                out _);

            for (var regularIndex = 0;
                regularIndex < suffixVehicles.Count;
                regularIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!operationBudget.TryConsumePath())
                {
                    diagnostic =
                        operationBudget.CreateDiagnostic();
                    return false;
                }

                if (LevelVehicleExitPlanner.IsPathClear(
                        regularIndex,
                        buses,
                        active,
                        garages,
                        out _,
                        cancellationToken))
                {
                    var vehicle =
                        suffixVehicles[regularIndex];
                    openingChainState.Targets.Add(
                        new OpeningTarget(
                            false,
                            regularIndex,
                            -1,
                            vehicle,
                            BuildVehicleExitCorridor(
                                vehicle)));
                }
            }

            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!operationBudget.TryConsumePath())
                {
                    diagnostic =
                        operationBudget.CreateDiagnostic();
                    return false;
                }

                if (!LevelVehicleExitPlanner.IsPathClear(
                        suffixVehicles.Count +
                        garageIndex,
                        buses,
                        active,
                        garages,
                        out _,
                        cancellationToken))
                {
                    continue;
                }

                releasedGarageCount++;
                var vehicle =
                    garages[garageIndex].FrontVehicle;
                openingChainState.Targets.Add(
                    new OpeningTarget(
                        true,
                        -1,
                        garageIndex,
                        vehicle,
                        BuildVehicleExitCorridor(
                            vehicle)));
            }

            garageReleaseEvaluated = true;
            if (releasedGarageCount != garages.Count)
            {
                diagnostic =
                    $"Regular suffix releases {releasedGarageCount}/" +
                    $"{garages.Count} garage corridors before prefix " +
                    "construction.";
                return false;
            }

            return true;
        }

        private static bool
            TryEvaluateOpeningChainTransition(
                BusDefinition candidate,
                IReadOnlyList<BusDefinition> laterVehicles,
                IReadOnlyList<GarageDefinition> garages,
                OpeningChainState openingChainState,
                List<BusDefinition> buses,
                bool[] active,
                CancellationToken cancellationToken,
                ConstructiveOperationBudget operationBudget,
                out bool candidateExitClear,
                out List<int> blockedTargetIndices,
                out int blockedGarageCount)
        {
            candidateExitClear = false;
            blockedTargetIndices =
                new List<int>();
            blockedGarageCount = 0;
            PopulatePathProbeState(
                laterVehicles,
                candidate,
                true,
                garages,
                buses,
                active,
                out var candidateIndex);
            if (!operationBudget.TryConsumePath())
            {
                return false;
            }

            candidateExitClear =
                LevelVehicleExitPlanner.IsPathClear(
                    candidateIndex,
                    buses,
                    active,
                    garages,
                    out _,
                    cancellationToken);
            if (!candidateExitClear)
            {
                return true;
            }

            var candidateFootprint =
                BoardLayoutConfig
                    .GetVehicleFootprintCells(
                        candidate);
            for (var targetIndex = 0;
                targetIndex <
                    openingChainState.Targets.Count;
                targetIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var target =
                    openingChainState.Targets[
                        targetIndex];
                if (!target.Corridor.Overlaps(
                        candidateFootprint,
                        GarageCorridorPaddingCells))
                {
                    continue;
                }

                if (!operationBudget.TryConsumePath())
                {
                    return false;
                }

                var movingIndex = target.IsGarage
                    ? candidateIndex + 1 +
                      target.GarageIndex
                    : target.RegularStateIndex;
                if (LevelVehicleExitPlanner.IsPathClear(
                        movingIndex,
                        buses,
                        active,
                        garages,
                        out _,
                        cancellationToken))
                {
                    continue;
                }

                blockedTargetIndices.Add(
                    targetIndex);
                if (target.IsGarage)
                {
                    blockedGarageCount++;
                }
            }

            return true;
        }

        private static bool ContainsBlockedGarageTarget(
            OpeningChainState openingChainState,
            IReadOnlyList<int> blockedTargetIndices,
            int garageIndex)
        {
            for (var index = 0;
                index <
                    blockedTargetIndices.Count;
                index++)
            {
                var target =
                    openingChainState.Targets[
                        blockedTargetIndices[index]];
                if (target.IsGarage &&
                    target.GarageIndex ==
                    garageIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsBlockedTargetIndex(
            IReadOnlyList<int> blockedTargetIndices,
            int targetIndex)
        {
            for (var index = 0;
                index < blockedTargetIndices.Count;
                index++)
            {
                if (blockedTargetIndices[index] ==
                    targetIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static string CreateOpeningChainDiagnostic(
            OpeningChainState openingChainState,
            int chainLinkCount,
            int chainBlockedRootCount,
            int chainMergeCount,
            int requiredMergeCount)
        {
            return
                $"openRoots={openingChainState.Targets.Count}, " +
                $"blockedGarages={openingChainState.BlockedGarageCount}, " +
                $"chainLinks={chainLinkCount}, " +
                $"chainBlockedRoots={chainBlockedRootCount}, " +
                $"chainMerges={chainMergeCount}, " +
                $"requiredMergeDebt={requiredMergeCount}, " +
                $"targetedMotifAttempts=" +
                $"{openingChainState.TargetedMotifAttemptCount}, " +
                $"targetedMotifSuccesses=" +
                $"{openingChainState.TargetedMotifSuccessCount}, " +
                $"repairAttempts=" +
                $"{openingChainState.PrefixRepairAttemptCount}, " +
                $"repairNodes=" +
                $"{openingChainState.PrefixRepairNodeCount}/" +
                $"{MaximumPrefixRepairNodeCount}, " +
                $"repairSuccesses=" +
                $"{openingChainState.PrefixRepairSuccessCount}.";
        }

        private static bool HasExactExitAgainstLaterVehicles(
            BusDefinition candidate,
            IReadOnlyList<BusDefinition> laterVehicles,
            List<BusDefinition> buses,
            bool[] active,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            buses.Clear();
            for (var index = 0;
                index < laterVehicles.Count;
                index++)
            {
                buses.Add(laterVehicles[index]);
            }

            var candidateIndex = buses.Count;
            buses.Add(candidate);
            Array.Clear(
                active,
                0,
                active.Length);
            for (var index = 0;
                index < buses.Count;
                index++)
            {
                active[index] = true;
            }

            return LevelVehicleExitPlanner.IsPathClear(
                candidateIndex,
                buses,
                active,
                out _,
                cancellationToken);
        }

        private static bool HasExactExitWithActiveGarages(
            BusDefinition candidate,
            IReadOnlyList<BusDefinition> laterVehicles,
            IReadOnlyList<GarageDefinition> garages,
            List<BusDefinition> buses,
            bool[] active,
            CancellationToken cancellationToken)
        {
            PopulatePathProbeState(
                laterVehicles,
                candidate,
                true,
                garages,
                buses,
                active,
                out var candidateIndex);
            return LevelVehicleExitPlanner.IsPathClear(
                candidateIndex,
                buses,
                active,
                garages,
                out _,
                cancellationToken);
        }

        private static bool AreGarageFrontPathsClear(
            IReadOnlyList<BusDefinition> regularVehicles,
            IReadOnlyList<GarageDefinition> garages,
            List<BusDefinition> buses,
            bool[] active,
            CancellationToken cancellationToken,
            ConstructiveOperationBudget operationBudget,
            out bool evaluated,
            out int clearGarageCount)
        {
            evaluated = false;
            clearGarageCount = 0;
            PopulatePathProbeState(
                regularVehicles,
                default,
                false,
                garages,
                buses,
                active,
                out _);
            return AreGarageFrontPathsClearInPreparedState(
                regularVehicles.Count,
                garages,
                buses,
                active,
                cancellationToken,
                operationBudget,
                out evaluated,
                out clearGarageCount);
        }

        private static bool
            AreGarageFrontPathsClearInPreparedState(
                int regularCount,
                IReadOnlyList<GarageDefinition> garages,
                IReadOnlyList<BusDefinition> buses,
                IReadOnlyList<bool> active,
                CancellationToken cancellationToken,
                ConstructiveOperationBudget operationBudget,
                out bool evaluated,
                out int clearGarageCount)
        {
            evaluated = false;
            clearGarageCount = 0;
            var allClear = true;
            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!operationBudget.TryConsumePath())
                {
                    return false;
                }

                if (LevelVehicleExitPlanner.IsPathClear(
                        regularCount + garageIndex,
                        buses,
                        active,
                        garages,
                        out _,
                        cancellationToken))
                {
                    clearGarageCount++;
                }
                else
                {
                    allClear = false;
                }
            }

            evaluated = true;
            return allClear;
        }

        private static bool TryCountBlockedGarageFronts(
            IReadOnlyList<BusDefinition> regularVehicles,
            IReadOnlyList<GarageDefinition> garages,
            List<BusDefinition> buses,
            bool[] active,
            CancellationToken cancellationToken,
            ConstructiveOperationBudget operationBudget,
            out int blockedGarageCount)
        {
            PopulatePathProbeState(
                regularVehicles,
                default,
                false,
                garages,
                buses,
                active,
                out _);
            return TryCountBlockedGarageFrontsInPreparedState(
                regularVehicles.Count,
                garages,
                buses,
                active,
                cancellationToken,
                operationBudget,
                out blockedGarageCount);
        }

        private static bool
            TryCountBlockedGarageFrontsInPreparedState(
                int regularCount,
                IReadOnlyList<GarageDefinition> garages,
                IReadOnlyList<BusDefinition> buses,
                IReadOnlyList<bool> active,
                CancellationToken cancellationToken,
                ConstructiveOperationBudget operationBudget,
                out int blockedGarageCount)
        {
            blockedGarageCount = 0;
            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!operationBudget.TryConsumePath())
                {
                    blockedGarageCount = 0;
                    return false;
                }

                if (!LevelVehicleExitPlanner.IsPathClear(
                        regularCount + garageIndex,
                        buses,
                        active,
                        garages,
                        out _,
                        cancellationToken))
                {
                    blockedGarageCount++;
                }
            }

            return true;
        }

        private static bool TryCountInitialOpeningMoves(
            IReadOnlyList<BusDefinition> regularVehicles,
            IReadOnlyList<GarageDefinition> garages,
            List<BusDefinition> buses,
            bool[] active,
            CancellationToken cancellationToken,
            ConstructiveOperationBudget operationBudget,
            out int openingCount)
        {
            PopulatePathProbeState(
                regularVehicles,
                default,
                false,
                garages,
                buses,
                active,
                out _);
            return TryCountInitialOpeningMovesInPreparedState(
                garages,
                buses,
                active,
                cancellationToken,
                operationBudget,
                out openingCount);
        }

        private static bool
            TryCountInitialOpeningMovesInPreparedState(
                IReadOnlyList<GarageDefinition> garages,
                IReadOnlyList<BusDefinition> buses,
                IReadOnlyList<bool> active,
                CancellationToken cancellationToken,
                ConstructiveOperationBudget operationBudget,
                out int openingCount)
        {
            openingCount = 0;
            for (var index = 0;
                index < buses.Count;
                index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!operationBudget.TryConsumePath())
                {
                    openingCount = 0;
                    return false;
                }

                if (LevelVehicleExitPlanner.IsPathClear(
                        index,
                        buses,
                        active,
                        garages,
                        out _,
                        cancellationToken))
                {
                    openingCount++;
                }
            }

            return true;
        }

        private static void PopulatePathProbeState(
            IReadOnlyList<BusDefinition> regularVehicles,
            BusDefinition candidate,
            bool includeCandidate,
            IReadOnlyList<GarageDefinition> garages,
            List<BusDefinition> buses,
            bool[] active,
            out int candidateIndex)
        {
            buses.Clear();
            for (var index = 0;
                index < regularVehicles.Count;
                index++)
            {
                buses.Add(regularVehicles[index]);
            }

            candidateIndex = -1;
            if (includeCandidate)
            {
                candidateIndex = buses.Count;
                buses.Add(candidate);
            }

            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                buses.Add(
                    garages[garageIndex]
                        .FrontVehicle);
            }

            Array.Clear(
                active,
                0,
                active.Length);
            for (var index = 0;
                index < buses.Count;
                index++)
            {
                active[index] = true;
            }
        }

        private static long
            CalculateOpeningChainCandidateScore(
                int blockedRootCount,
                int blockedGarageCount,
                bool blocksAssignedGarage,
                int desiredExactBlockedRoots,
                int minimumExactBlockedGarages,
                bool assignedGarageMustBeBlocked)
        {
            var rootShortfall = Mathf.Max(
                0,
                desiredExactBlockedRoots -
                blockedRootCount);
            var garageShortfall = Mathf.Max(
                0,
                minimumExactBlockedGarages -
                blockedGarageCount);
            var assignedShortfall =
                assignedGarageMustBeBlocked &&
                !blocksAssignedGarage
                    ? 1
                    : 0;
            return
                rootShortfall * 1000000000000L +
                assignedShortfall * 500000000000L +
                garageShortfall * 100000000000L -
                blockedRootCount * 10000000L -
                blockedGarageCount * 1000000L;
        }

        private static int GetMaximumInitialOpeningCount(
            LevelDifficultyProfile profile,
            int activeVehicleCount,
            int garageCount)
        {
            activeVehicleCount =
                Mathf.Max(1, activeVehicleCount);
            garageCount = Mathf.Max(0, garageCount);
            var pressure = Mathf.Clamp01(
                profile.ParkingTension * 0.65f +
                profile.StationPressure * 0.35f);
            var openingRatio = Mathf.Lerp(
                0.36f,
                0.30f,
                pressure);
            var densityLimit = Mathf.FloorToInt(
                activeVehicleCount *
                openingRatio);
            var lowerBound = Mathf.Max(
                4,
                Mathf.Min(
                    6,
                    garageCount + 1));
            var upperBound = Mathf.Max(
                lowerBound,
                Mathf.FloorToInt(
                    activeVehicleCount *
                    0.34f));
            return Mathf.Clamp(
                densityLimit,
                lowerBound,
                upperBound);
        }

        private static bool
            TryApplyMysteryVehicleModifiers(
                IReadOnlyList<BusDefinition> buses,
                MysteryVehicleGenerationProfile profile,
                int seed,
                CancellationToken cancellationToken,
                ConstructiveOperationBudget operationBudget,
                out List<BusDefinition> result)
        {
            result =
                buses != null
                    ? new List<BusDefinition>(buses)
                    : new List<BusDefinition>();
            if (!profile.Enabled ||
                result.Count == 0)
            {
                return true;
            }

            var active = new bool[result.Count];
            for (var index = 0;
                index < result.Count;
                index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                active[index] = true;
                result[index] =
                    result[index].WithStartsConcealed(false);
            }

            var candidates = new List<int>();
            for (var index = 0;
                index < result.Count;
                index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!operationBudget.TryConsumePath())
                {
                    return false;
                }

                if (!LevelVehicleExitPlanner.IsPathClear(
                        index,
                        result,
                        active,
                        out var blockingIndex,
                        cancellationToken) &&
                    blockingIndex >= 0)
                {
                    candidates.Add(index);
                }
            }

            if (candidates.Count == 0)
            {
                return true;
            }

            var target = Mathf.RoundToInt(
                result.Count * profile.Ratio);
            target = Mathf.Clamp(
                target,
                Mathf.Min(
                    profile.MinVehicles,
                    candidates.Count),
                Mathf.Min(
                    profile.MaxVehicles,
                    candidates.Count));
            Shuffle(
                candidates,
                new System.Random(
                    unchecked(
                        seed ^ 0x5f3759df)));
            var selected = new HashSet<int>();
            for (var index = 0;
                index < target;
                index++)
            {
                selected.Add(candidates[index]);
            }

            for (var index = 0;
                index < result.Count;
                index++)
            {
                result[index] =
                    result[index]
                        .WithStartsConcealed(
                            selected.Contains(index));
            }

            return true;
        }

        private static bool HasRequiredShapeQuality(
            StageGenerationRequest request,
            LevelDifficultyProfile profile,
            IReadOnlyList<BusDefinition> regularVehicles,
            out string diagnostic)
        {
            if (!ShapeLibraryVehicleCoverage.IsSatisfied(
                    profile,
                    request.VehicleLayoutVariantIndex,
                    regularVehicles.Count))
            {
                diagnostic =
                    $"Constructive shape coverage {regularVehicles.Count}/" +
                    $"{profile.TargetVehicleCount} is below the required minimum.";
                return false;
            }

            if (ShapeLibraryLayoutQuality.TryGetFailureMessage(
                    profile,
                    request.VehicleLayoutVariantIndex,
                    regularVehicles,
                    out diagnostic))
            {
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool HasPreservedGenerationContract(
            StageGenerationRequest request,
            LogicalPlan plan,
            IReadOnlyList<BusDefinition> regularVehicles,
            IReadOnlyList<GarageDefinition> garages,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (regularVehicles.Count +
                    CountGarageVehicles(garages) !=
                plan.TargetVehicleCount)
            {
                diagnostic =
                    "Constructive placement changed the target vehicle count.";
                return false;
            }

            if (garages.Count !=
                request.GarageCount)
            {
                diagnostic =
                    $"Constructive garage count {garages.Count} differs from " +
                    $"requested {request.GarageCount}.";
                return false;
            }

            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                var queued =
                    garages[garageIndex]
                        .QueuedVehicleCount;
                if (queued <
                        request.MinGarageQueuedVehicles ||
                    queued >
                        request.MaxGarageQueuedVehicles)
                {
                    diagnostic =
                        $"Garage {garageIndex} queue {queued} is outside " +
                        $"{request.MinGarageQueuedVehicles}-" +
                        $"{request.MaxGarageQueuedVehicles}.";
                    return false;
                }
            }

            var expected =
                CountLogicalVehicleContracts(plan);
            var actual =
                CountPlacedVehicleContracts(
                    regularVehicles,
                    garages);
            if (!ContractCountsEqual(
                    expected,
                    actual))
            {
                diagnostic =
                    "Constructive placement changed the planned color/size multiset.";
                return false;
            }

            var uniqueColors =
                new HashSet<PuzzleColor>();
            for (var index = 0;
                index < regularVehicles.Count;
                index++)
            {
                uniqueColors.Add(
                    regularVehicles[index].Color);
            }

            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                foreach (var vehicle in
                    garages[garageIndex]
                        .EnumerateVehicles())
                {
                    uniqueColors.Add(vehicle.Color);
                }
            }

            if (uniqueColors.Count !=
                plan.TargetColorCount)
            {
                diagnostic =
                    $"Constructive color count {uniqueColors.Count} differs from " +
                    $"target {plan.TargetColorCount}.";
                return false;
            }

            return true;
        }

        private static Dictionary<int, int>
            CountLogicalVehicleContracts(
                LogicalPlan plan)
        {
            var counts =
                new Dictionary<int, int>();
            for (var index = 0;
                index < plan.RegularVehicles.Count;
                index++)
            {
                AddVehicleContract(
                    counts,
                    plan.RegularVehicles[index].Color,
                    plan.RegularVehicles[index].Size);
            }

            for (var garageIndex = 0;
                garageIndex <
                    plan.GarageVehicles.Length;
                garageIndex++)
            {
                var sequence =
                    plan.GarageVehicles[garageIndex];
                for (var progress = 0;
                    progress < sequence.Length;
                    progress++)
                {
                    AddVehicleContract(
                        counts,
                        sequence[progress].Color,
                        sequence[progress].Size);
                }
            }

            return counts;
        }

        private static Dictionary<int, int>
            CountPlacedVehicleContracts(
                IReadOnlyList<BusDefinition> buses,
                IReadOnlyList<GarageDefinition> garages)
        {
            var counts =
                new Dictionary<int, int>();
            for (var index = 0;
                index < buses.Count;
                index++)
            {
                AddVehicleContract(
                    counts,
                    buses[index].Color,
                    buses[index].Size);
            }

            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                foreach (var vehicle in
                    garages[garageIndex]
                        .EnumerateVehicles())
                {
                    AddVehicleContract(
                        counts,
                        vehicle.Color,
                        vehicle.Size);
                }
            }

            return counts;
        }

        private static void AddVehicleContract(
            IDictionary<int, int> counts,
            PuzzleColor color,
            BusSize size)
        {
            var key =
                ((int)color << 4) |
                (int)size;
            counts.TryGetValue(
                key,
                out var count);
            counts[key] = count + 1;
        }

        private static bool ContractCountsEqual(
            IReadOnlyDictionary<int, int> expected,
            IReadOnlyDictionary<int, int> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            foreach (var pair in expected)
            {
                if (!actual.TryGetValue(
                        pair.Key,
                        out var value) ||
                    value != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<
            SuperHardGarageConstructiveWitnessStep>
            BuildWitness(
                LogicalPlan plan,
                IReadOnlyList<BusDefinition> regularVehicles,
                IReadOnlyList<GarageDefinition> garages,
                CancellationToken cancellationToken)
        {
            var witness =
                new List<
                    SuperHardGarageConstructiveWitnessStep>(
                    plan.Witness.Count);
            for (var index = 0;
                index < plan.Witness.Count;
                index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var token = plan.Witness[index];
                if (token.GarageIndex >= 0)
                {
                    var vehicle =
                        GetGarageVehicle(
                            garages[token.GarageIndex],
                            token.GarageProgress);
                    witness.Add(
                        new SuperHardGarageConstructiveWitnessStep(
                            regularVehicles.Count +
                            token.GarageIndex,
                            token.GarageIndex,
                            token.GarageProgress,
                            vehicle));
                }
                else
                {
                    witness.Add(
                        new SuperHardGarageConstructiveWitnessStep(
                            token.RegularIndex,
                            -1,
                            -1,
                            regularVehicles[
                                token.RegularIndex]));
                }
            }

            return witness;
        }

        private static BusDefinition GetGarageVehicle(
            GarageDefinition garage,
            int progress)
        {
            return progress == 0
                ? garage.FrontVehicle
                : garage.QueuedVehicles[
                    progress - 1];
        }

        private static bool ValidateLinearWitness(
            IReadOnlyList<BusDefinition> regularVehicles,
            IReadOnlyList<GarageDefinition> garages,
            IReadOnlyList<
                SuperHardGarageConstructiveWitnessStep>
                witness,
            CancellationToken cancellationToken,
            ConstructiveOperationBudget operationBudget,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            cancellationToken.ThrowIfCancellationRequested();
            var regularCount =
                regularVehicles.Count;
            var buses =
                new List<BusDefinition>(
                    regularCount + garages.Count);
            for (var index = 0;
                index < regularCount;
                index++)
            {
                buses.Add(regularVehicles[index]);
            }

            var active =
                new bool[
                    regularCount + garages.Count];
            for (var index = 0;
                index < regularCount;
                index++)
            {
                active[index] = true;
            }

            var sequences =
                new BusDefinition[garages.Count][];
            var garageProgress =
                new int[garages.Count];
            var activeGarageObstacles =
                new List<GarageDefinition>(
                    garages.Count);
            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var garage = garages[garageIndex];
                var sequence =
                    new BusDefinition[
                        garage.TotalVehicleCount];
                sequence[0] =
                    garage.FrontVehicle;
                for (var queueIndex = 0;
                    queueIndex <
                        garage.QueuedVehicles.Count;
                    queueIndex++)
                {
                    sequence[queueIndex + 1] =
                        garage.QueuedVehicles[queueIndex];
                }

                sequences[garageIndex] =
                    sequence;
                buses.Add(sequence[0]);
                active[regularCount +
                    garageIndex] = true;
                activeGarageObstacles.Add(
                    garage);
            }

            for (var stepIndex = 0;
                stepIndex < witness.Count;
                stepIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = witness[stepIndex];
                if (step.VehicleIndex < 0 ||
                    step.VehicleIndex >=
                        active.Length ||
                    !active[step.VehicleIndex])
                {
                    diagnostic =
                        $"Witness step {stepIndex} targets an inactive vehicle slot.";
                    return false;
                }

                var expectedGarageIndex =
                    step.VehicleIndex -
                    regularCount;
                if (expectedGarageIndex < 0)
                {
                    if (step.GarageIndex != -1 ||
                        step.GarageProgress != -1)
                    {
                        diagnostic =
                            $"Witness step {stepIndex} has invalid regular metadata.";
                        return false;
                    }
                }
                else if (step.GarageIndex !=
                             expectedGarageIndex ||
                    step.GarageProgress !=
                        garageProgress[
                            expectedGarageIndex])
                {
                    diagnostic =
                        $"Witness step {stepIndex} has invalid garage progress.";
                    return false;
                }

                if (!SameVehicleContract(
                        buses[step.VehicleIndex],
                        step.Vehicle))
                {
                    diagnostic =
                        $"Witness step {stepIndex} vehicle contract changed.";
                    return false;
                }

                if (!operationBudget.TryConsumePath())
                {
                    diagnostic =
                        operationBudget.CreateDiagnostic();
                    return false;
                }

                if (!LevelVehicleExitPlanner.IsPathClear(
                        step.VehicleIndex,
                        buses,
                        active,
                        activeGarageObstacles,
                        out _,
                        cancellationToken))
                {
                    diagnostic =
                        $"Witness step {stepIndex} has no exact clear exit.";
                    return false;
                }

                if (expectedGarageIndex < 0)
                {
                    active[step.VehicleIndex] =
                        false;
                    continue;
                }

                var previousProgress =
                    garageProgress[
                        expectedGarageIndex];
                var sequence =
                    sequences[
                        expectedGarageIndex];
                var remainingQueuedBefore =
                    sequence.Length -
                    1 -
                    previousProgress;
                garageProgress[
                    expectedGarageIndex] =
                    previousProgress + 1;
                if (remainingQueuedBefore <= 0)
                {
                    active[step.VehicleIndex] =
                        false;
                    RemoveGarageObstacle(
                        activeGarageObstacles,
                        garages[
                            expectedGarageIndex]);
                }
                else
                {
                    buses[step.VehicleIndex] =
                        sequence[
                            previousProgress + 1];
                    if (remainingQueuedBefore == 1)
                    {
                        RemoveGarageObstacle(
                            activeGarageObstacles,
                            garages[
                                expectedGarageIndex]);
                    }
                }
            }

            for (var index = 0;
                index < active.Length;
                index++)
            {
                if (active[index])
                {
                    diagnostic =
                        "Linear witness ended with active vehicles.";
                    return false;
                }
            }

            if (activeGarageObstacles.Count != 0)
            {
                diagnostic =
                    "Linear witness ended with active garage obstacles.";
                return false;
            }

            for (var garageIndex = 0;
                garageIndex < sequences.Length;
                garageIndex++)
            {
                if (garageProgress[garageIndex] !=
                    sequences[garageIndex].Length)
                {
                    diagnostic =
                        $"Linear witness did not drain garage {garageIndex}.";
                    return false;
                }
            }

            return true;
        }

        private static bool SameVehicleContract(
            BusDefinition left,
            BusDefinition right)
        {
            return left.Color == right.Color &&
                left.Size == right.Size &&
                left.Direction == right.Direction &&
                left.GridPosition ==
                    right.GridPosition &&
                Mathf.Abs(
                    left.AngleOffsetDegrees -
                    right.AngleOffsetDegrees) <=
                    0.0001f &&
                (left.PositionOffsetCells -
                    right.PositionOffsetCells)
                    .sqrMagnitude <= 0.000001f &&
                left.StartsConcealed ==
                    right.StartsConcealed;
        }

        private static void RemoveGarageObstacle(
            IList<GarageDefinition> activeGarages,
            GarageDefinition garage)
        {
            for (var index =
                    activeGarages.Count - 1;
                index >= 0;
                index--)
            {
                if (activeGarages[index]
                    .GridPosition ==
                    garage.GridPosition)
                {
                    activeGarages.RemoveAt(index);
                    return;
                }
            }
        }

        private static PassengerFlowPlan
            BuildPassengerFlowPlan(
                LevelDifficultyProfile profile,
                IReadOnlyList<
                    SuperHardGarageConstructiveWitnessStep>
                    witness,
                int seed)
        {
            var route =
                new List<SolutionBusStepDefinition>(
                    witness.Count);
            var rule = profile.PassengerFlowRule;
            for (var index = 0;
                index < witness.Count;
                index++)
            {
                var vehicle =
                    witness[index].Vehicle;
                var preferredGroupUnits =
                    Mathf.Clamp(
                        vehicle.CapacityUnits,
                        rule.MinGroupUnits,
                        rule.MaxGroupUnits);
                route.Add(
                    new SolutionBusStepDefinition(
                        vehicle.Color,
                        vehicle.Size,
                        preferredGroupUnits));
            }

            var flowPlan =
                new PassengerFlowPlan();
            flowPlan.ConfigureSolutionRoute(
                route,
                rule.MinGroupUnits,
                rule.MaxGroupUnits,
                true,
                seed);
            return flowPlan;
        }

        private static string CreateExperimentalSignature(
            StageGenerationRequest request,
            int candidateOffset,
            int candidateSeed,
            int layoutProbeIndex,
            int vehicleCount,
            int witnessLength,
            int regularPrefixCount,
            int regularSuffixCount,
            int initialOpeningCount,
            int maximumInitialOpeningCount,
            int garageDependencyCount,
            int garageDependencyTarget,
            int chainLinkCount,
            int chainBlockedRootCount,
            int chainMergeCount,
            int requiredMergeCount,
            int targetedMotifAttemptCount,
            int targetedMotifSuccessCount,
            int prefixRepairAttemptCount,
            int prefixRepairNodeCount,
            int prefixRepairSuccessCount,
            int finalRollingSuffixRootCount,
            int suffixRootEvaluationCount)
        {
            var profile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(
                    request.Difficulty);
            return
                "runtimeConstructiveExperimental=1;" +
                $"signature={StageGenerationSignature.CurrentVersion};" +
                $"strategy={ExperimentalStrategy};" +
                $"stage={request.StageNumber};" +
                $"seed={request.Seed};" +
                $"candidateSeed={candidateSeed};" +
                $"candidate={candidateOffset};" +
                $"layoutProbe={layoutProbeIndex};" +
                $"layoutVariant={request.VehicleLayoutVariantIndex};" +
                $"layoutPool={request.VehicleLayoutVariantPoolSize};" +
                $"garages={request.GarageCount};" +
                $"garageQueueMin={request.MinGarageQueuedVehicles};" +
                $"garageQueueMax={request.MaxGarageQueuedVehicles};" +
                $"targetVehicles={profile.TargetVehicleCount};" +
                $"targetColors={profile.TargetColorCount};" +
                $"vehicles={profile.TargetVehicleCount};" +
                $"colors={profile.TargetColorCount};" +
                $"parking={FormatSignatureFloat(profile.ParkingTension)};" +
                $"station={FormatSignatureFloat(profile.StationPressure)};" +
                $"modifiers={(int)request.Modifiers};" +
                $"solutionMin={request.MinSolutionCount};" +
                $"solutionMax={request.MaxSolutionCount};" +
                $"actualVehicles={vehicleCount};" +
                $"witnessLength={witnessLength};" +
                $"regularPrefixCap={MaximumConstrainedRegularPrefixCount};" +
                $"regularPrefix={regularPrefixCount};" +
                $"regularSuffixMinimum={MinimumRegularSuffixCount};" +
                $"regularSuffix={regularSuffixCount};" +
                $"rollingSuffixRoots={finalRollingSuffixRootCount};" +
                $"suffixRootEvaluations={suffixRootEvaluationCount};" +
                $"initialOpenings={initialOpeningCount};" +
                $"initialOpeningMax={maximumInitialOpeningCount};" +
                $"garageDependencies={garageDependencyCount};" +
                $"garageDependencyTarget={garageDependencyTarget};" +
                $"chainLinks={chainLinkCount};" +
                $"chainBlockedRoots={chainBlockedRootCount};" +
                $"chainMerges={chainMergeCount};" +
                $"requiredMergeDebt={requiredMergeCount};" +
                $"targetedMotifAttempts={targetedMotifAttemptCount};" +
                $"targetedMotifSuccesses={targetedMotifSuccessCount};" +
                $"prefixRepairAttempts={prefixRepairAttemptCount};" +
                $"prefixRepairNodeCap={MaximumPrefixRepairNodeCount};" +
                $"prefixRepairNodes={prefixRepairNodeCount};" +
                $"prefixRepairSuccesses={prefixRepairSuccessCount};";
        }

        private static string AppendSuffixRootTelemetry(
            string diagnostic,
            int finalRollingSuffixRootCount,
            int suffixRootEvaluationCount)
        {
            var prefix =
                string.IsNullOrWhiteSpace(diagnostic)
                    ? string.Empty
                    : diagnostic.TrimEnd() + " ";
            return
                prefix +
                $"strategy={ExperimentalStrategy}; " +
                $"rollingSuffixRoots={finalRollingSuffixRootCount}; " +
                $"suffixRootEvaluations={suffixRootEvaluationCount}.";
        }

        private static string FormatSignatureFloat(
            float value)
        {
            return value.ToString(
                "0.####",
                CultureInfo.InvariantCulture);
        }

        private static int CountGarageVehicles(
            IReadOnlyList<GarageDefinition> garages)
        {
            var count = 0;
            for (var index = 0;
                index < garages.Count;
                index++)
            {
                count +=
                    garages[index]
                        .TotalVehicleCount;
            }

            return count;
        }

        private static VehicleFootprint
            GetGarageFootprint(
                GarageDefinition garage)
        {
            return new VehicleFootprint(
                new Vector3(
                    garage.GridPosition.x,
                    0f,
                    garage.GridPosition.y),
                Vector3.right,
                Vector3.forward,
                0.45f,
                0.45f);
        }

        private static int PositiveModulo(
            int value,
            int divisor)
        {
            var result = value % divisor;
            return result < 0
                ? result + divisor
                : result;
        }

        private static void Shuffle<T>(
            IList<T> values,
            System.Random random)
        {
            for (var index =
                    values.Count - 1;
                index > 0;
                index--)
            {
                var swapIndex =
                    random.Next(0, index + 1);
                var value = values[index];
                values[index] =
                    values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private sealed class ConstructiveOperationBudget
        {
            public readonly int PlacementLimit;
            public readonly int PathLimit;

            public int PlacementCount { get; private set; }
            public int PathCount { get; private set; }
            public bool HitOperationLimit { get; private set; }
            public bool HitPathLimit { get; private set; }
            public bool HitLimit =>
                HitOperationLimit || HitPathLimit;
            public string Phase { get; private set; } =
                "initialization";

            public ConstructiveOperationBudget(
                int placementLimit,
                int pathLimit)
            {
                PlacementLimit =
                    Mathf.Max(1, placementLimit);
                PathLimit =
                    Mathf.Max(1, pathLimit);
            }

            public void SetPhase(string phase)
            {
                Phase = string.IsNullOrWhiteSpace(
                        phase)
                    ? "unspecified"
                    : phase;
            }

            public bool TryConsumePlacement()
            {
                if (PlacementCount >=
                    PlacementLimit)
                {
                    HitOperationLimit = true;
                    return false;
                }

                PlacementCount++;
                return true;
            }

            public bool TryConsumePath()
            {
                return TryConsumePaths(1);
            }

            public bool TryConsumePaths(int count)
            {
                count = Mathf.Max(0, count);
                if (count >
                    PathLimit - PathCount)
                {
                    HitPathLimit = true;
                    return false;
                }

                PathCount += count;
                return true;
            }

            public string CreateDiagnostic()
            {
                var exhausted = HitOperationLimit &&
                    HitPathLimit
                        ? "placement and path"
                        : HitOperationLimit
                            ? "placement"
                            : HitPathLimit
                                ? "path"
                                : "none";
                return
                    $"Constructive operation budget exhausted={exhausted}; " +
                    $"phase={Phase}; " +
                    $"placement={PlacementCount}/{PlacementLimit}, " +
                    $"path={PathCount}/{PathLimit}.";
            }
        }

        private readonly struct LogicalVehicleSpec
        {
            public readonly PuzzleColor Color;
            public readonly BusSize Size;

            public LogicalVehicleSpec(
                PuzzleColor color,
                BusSize size)
            {
                Color = color;
                Size = size;
            }
        }

        private readonly struct
            SortableLogicalVehicleSpec
        {
            public readonly LogicalVehicleSpec Spec;
            public readonly int TieBreaker;
            public readonly int OriginalIndex;

            public SortableLogicalVehicleSpec(
                LogicalVehicleSpec spec,
                int tieBreaker,
                int originalIndex)
            {
                Spec = spec;
                TieBreaker = tieBreaker;
                OriginalIndex = originalIndex;
            }
        }

        private readonly struct LogicalWitnessToken
        {
            public readonly int RegularIndex;
            public readonly int GarageIndex;
            public readonly int GarageProgress;

            private LogicalWitnessToken(
                int regularIndex,
                int garageIndex,
                int garageProgress)
            {
                RegularIndex = regularIndex;
                GarageIndex = garageIndex;
                GarageProgress = garageProgress;
            }

            public static LogicalWitnessToken
                ForRegular(int regularIndex)
            {
                return new LogicalWitnessToken(
                    regularIndex,
                    -1,
                    -1);
            }

            public static LogicalWitnessToken
                ForGarage(
                    int garageIndex,
                    int garageProgress)
            {
                return new LogicalWitnessToken(
                    -1,
                    garageIndex,
                    garageProgress);
            }
        }

        private sealed class LogicalPlan
        {
            public readonly List<LogicalVehicleSpec>
                RegularVehicles;
            public readonly LogicalVehicleSpec[][]
                GarageVehicles;
            public readonly List<LogicalWitnessToken>
                Witness;
            public readonly int TargetVehicleCount;
            public readonly int TargetColorCount;
            public readonly int RegularPrefixCount;

            public LogicalPlan(
                List<LogicalVehicleSpec> regularVehicles,
                LogicalVehicleSpec[][] garageVehicles,
                List<LogicalWitnessToken> witness,
                int targetVehicleCount,
                int targetColorCount,
                int regularPrefixCount)
            {
                RegularVehicles =
                    regularVehicles;
                GarageVehicles =
                    garageVehicles;
                Witness = witness;
                TargetVehicleCount =
                    targetVehicleCount;
                TargetColorCount =
                    targetColorCount;
                RegularPrefixCount = Mathf.Clamp(
                    regularPrefixCount,
                    0,
                    regularVehicles.Count);
            }

            public int GarageVehicleCount
            {
                get
                {
                    var count = 0;
                    for (var index = 0;
                        index <
                            GarageVehicles.Length;
                        index++)
                    {
                        count +=
                            GarageVehicles[index]
                                .Length;
                    }

                    return count;
                }
            }
        }

        private readonly struct EdgeGaragePortal
        {
            public readonly Vector2Int GridPosition;
            public readonly GridDirection ExitDirection;

            public EdgeGaragePortal(
                Vector2Int gridPosition,
                GridDirection exitDirection)
            {
                GridPosition = gridPosition;
                ExitDirection = exitDirection;
            }
        }

        private readonly struct GarageExitCorridor
        {
            private readonly float minimumX;
            private readonly float maximumX;
            private readonly float minimumY;
            private readonly float maximumY;

            public GarageExitCorridor(
                float minimumX,
                float maximumX,
                float minimumY,
                float maximumY)
            {
                this.minimumX = minimumX;
                this.maximumX = maximumX;
                this.minimumY = minimumY;
                this.maximumY = maximumY;
            }

            public bool Overlaps(
                VehicleFootprint footprint,
                float padding)
            {
                padding = Mathf.Max(0f, padding);
                return
                    footprint.ProjectMax(
                        Vector2.right) >=
                    minimumX - padding &&
                    footprint.ProjectMin(
                        Vector2.right) <=
                    maximumX + padding &&
                    footprint.ProjectMax(
                        Vector2.up) >=
                    minimumY - padding &&
                    footprint.ProjectMin(
                        Vector2.up) <=
                    maximumY + padding;
            }
        }

        private sealed class OpeningChainState
        {
            public readonly List<OpeningTarget> Targets =
                new List<OpeningTarget>();

            public int BlockedGarageCount
            {
                get;
                private set;
            }

            public int TargetedMotifAttemptCount
            {
                get;
                private set;
            }

            public int TargetedMotifSuccessCount
            {
                get;
                private set;
            }

            public int PrefixRepairAttemptCount
            {
                get;
                private set;
            }

            public int PrefixRepairNodeCount
            {
                get;
                private set;
            }

            public int PrefixRepairSuccessCount
            {
                get;
                private set;
            }

            public void RecordTargetedMotifAttempt()
            {
                TargetedMotifAttemptCount++;
            }

            public void RecordTargetedMotifSuccess()
            {
                TargetedMotifSuccessCount++;
            }

            public void RecordPrefixRepairAttempt()
            {
                PrefixRepairAttemptCount++;
            }

            public void RecordPrefixRepairNode()
            {
                PrefixRepairNodeCount++;
            }

            public void RecordPrefixRepairSuccess()
            {
                PrefixRepairSuccessCount++;
            }

            public OpeningChainState CloneStructure()
            {
                var clone =
                    new OpeningChainState
                    {
                        BlockedGarageCount =
                            BlockedGarageCount,
                        TargetedMotifSuccessCount =
                            TargetedMotifSuccessCount
                    };
                clone.Targets.AddRange(
                    Targets);
                return clone;
            }

            public void RestoreStructureFrom(
                OpeningChainState snapshot)
            {
                Targets.Clear();
                Targets.AddRange(
                    snapshot.Targets);
                BlockedGarageCount =
                    snapshot.BlockedGarageCount;
                TargetedMotifSuccessCount =
                    snapshot.TargetedMotifSuccessCount;
            }

            public bool HasOpenGarage(int garageIndex)
            {
                for (var index = 0;
                    index < Targets.Count;
                    index++)
                {
                    if (Targets[index].IsGarage &&
                        Targets[index].GarageIndex ==
                        garageIndex)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void ApplyTransition(
                IReadOnlyList<int> blockedTargetIndices,
                int blockedGarageCount,
                OpeningTarget newTarget)
            {
                for (var index =
                        blockedTargetIndices.Count - 1;
                    index >= 0;
                    index--)
                {
                    Targets.RemoveAt(
                        blockedTargetIndices[index]);
                }

                BlockedGarageCount +=
                    Mathf.Max(0, blockedGarageCount);
                Targets.Add(newTarget);
            }
        }

        private readonly struct OpeningTarget
        {
            public readonly bool IsGarage;
            public readonly int RegularStateIndex;
            public readonly int GarageIndex;
            public readonly BusDefinition Vehicle;
            public readonly GarageExitCorridor Corridor;

            public OpeningTarget(
                bool isGarage,
                int regularStateIndex,
                int garageIndex,
                BusDefinition vehicle,
                GarageExitCorridor corridor)
            {
                IsGarage = isGarage;
                RegularStateIndex = regularStateIndex;
                GarageIndex = garageIndex;
                Vehicle = vehicle;
                Corridor = corridor;
            }
        }

        private readonly struct PrefixSlotPriority
        {
            public readonly int SlotIndex;
            public readonly int BlockedRootCount;
            public readonly int BlockedGarageCount;
            public readonly bool BlocksAssignedGarage;
            public readonly int Ordinal;

            public PrefixSlotPriority(
                int slotIndex,
                int blockedRootCount,
                int blockedGarageCount,
                bool blocksAssignedGarage,
                int ordinal)
            {
                SlotIndex = slotIndex;
                BlockedRootCount = blockedRootCount;
                BlockedGarageCount = blockedGarageCount;
                BlocksAssignedGarage = blocksAssignedGarage;
                Ordinal = ordinal;
            }
        }

        private readonly struct PrefixPlacementCandidate
        {
            public readonly BusDefinition Candidate;
            public readonly int SlotIndex;
            public readonly long Score;
            public readonly bool IsAuthoredSlot;
            public readonly int Ordinal;

            public PrefixPlacementCandidate(
                BusDefinition candidate,
                int slotIndex,
                long score,
                bool isAuthoredSlot,
                int ordinal)
            {
                Candidate = candidate;
                SlotIndex = slotIndex;
                Score = score;
                IsAuthoredSlot = isAuthoredSlot;
                Ordinal = ordinal;
            }
        }

        private readonly struct TargetedTMotifCandidate
        {
            public readonly BusDefinition Candidate;
            public readonly int PrimaryTargetIndex;
            public readonly int SecondaryTargetIndex;
            public readonly int GeometricBlockedRootCount;
            public readonly int GeometricBlockedGarageCount;
            public readonly bool BlocksAssignedGarage;
            public readonly bool HasFeasibleSuccessor;
            public readonly int Ordinal;

            public TargetedTMotifCandidate(
                BusDefinition candidate,
                int primaryTargetIndex,
                int secondaryTargetIndex,
                int geometricBlockedRootCount,
                int geometricBlockedGarageCount,
                bool blocksAssignedGarage,
                bool hasFeasibleSuccessor,
                int ordinal)
            {
                Candidate = candidate;
                PrimaryTargetIndex = primaryTargetIndex;
                SecondaryTargetIndex = secondaryTargetIndex;
                GeometricBlockedRootCount =
                    geometricBlockedRootCount;
                GeometricBlockedGarageCount =
                    geometricBlockedGarageCount;
                BlocksAssignedGarage =
                    blocksAssignedGarage;
                HasFeasibleSuccessor =
                    hasFeasibleSuccessor;
                Ordinal = ordinal;
            }

            public bool IsPair =>
                SecondaryTargetIndex >= 0;
        }

        private sealed class PrefixPlacementSnapshot
        {
            public readonly List<BusDefinition>
                PlacedLaterVehicles;
            public readonly BusDefinition[] Placements;
            public readonly bool[] UsedSlots;
            public readonly OpeningChainState OpeningChain;
            public readonly int ChainLinkCount;
            public readonly int ChainBlockedRootCount;
            public readonly int ChainMergeCount;

            public PrefixPlacementSnapshot(
                IReadOnlyList<BusDefinition>
                    placedLaterVehicles,
                BusDefinition[] placements,
                bool[] usedSlots,
                OpeningChainState openingChain,
                int chainLinkCount,
                int chainBlockedRootCount,
                int chainMergeCount)
            {
                PlacedLaterVehicles =
                    new List<BusDefinition>(
                        placedLaterVehicles);
                Placements =
                    (BusDefinition[])
                    placements.Clone();
                UsedSlots =
                    (bool[])usedSlots.Clone();
                OpeningChain =
                    openingChain.CloneStructure();
                ChainLinkCount = chainLinkCount;
                ChainBlockedRootCount =
                    chainBlockedRootCount;
                ChainMergeCount = chainMergeCount;
            }

            public void Restore(
                List<BusDefinition>
                    placedLaterVehicles,
                BusDefinition[] placements,
                bool[] usedSlots,
                OpeningChainState openingChain,
                ref int chainLinkCount,
                ref int chainBlockedRootCount,
                ref int chainMergeCount)
            {
                placedLaterVehicles.Clear();
                placedLaterVehicles.AddRange(
                    PlacedLaterVehicles);
                Array.Copy(
                    Placements,
                    placements,
                    Placements.Length);
                Array.Copy(
                    UsedSlots,
                    usedSlots,
                    UsedSlots.Length);
                openingChain.RestoreStructureFrom(
                    OpeningChain);
                chainLinkCount = ChainLinkCount;
                chainBlockedRootCount =
                    ChainBlockedRootCount;
                chainMergeCount = ChainMergeCount;
            }

        }

        private sealed class PrefixDecisionFrame
        {
            public readonly int RegularIndex;
            public readonly PrefixRepairOption SelectedOption;
            public readonly PrefixPlacementSnapshot Before;

            public PrefixDecisionFrame(
                int regularIndex,
                PrefixRepairOption selectedOption,
                PrefixPlacementSnapshot before)
            {
                RegularIndex = regularIndex;
                SelectedOption =
                    selectedOption;
                Before = before;
            }
        }

        private readonly struct PrefixRepairOption
        {
            public readonly PrefixPlacementCandidate
                Placement;
            public readonly List<int>
                BlockedTargetIndices;
            public readonly int BlockedGarageCount;
            public readonly bool FromTargetedMotif;

            public PrefixRepairOption(
                PrefixPlacementCandidate placement,
                IReadOnlyList<int>
                    blockedTargetIndices,
                int blockedGarageCount,
                bool fromTargetedMotif)
            {
                Placement = placement;
                BlockedTargetIndices =
                    new List<int>(
                        blockedTargetIndices);
                BlockedGarageCount =
                    blockedGarageCount;
                FromTargetedMotif =
                    fromTargetedMotif;
            }
        }

        private sealed class PrefixRepairBeamState
        {
            public readonly PrefixPlacementSnapshot State;
            public readonly List<PrefixDecisionFrame> Frames;
            public readonly bool FollowsOriginalPath;
            public readonly int OpeningDebt;
            public readonly int OpenRootCount;
            public readonly int ChainMergeCount;
            public readonly long CandidateScore;
            public readonly int Ordinal;

            public PrefixRepairBeamState(
                PrefixPlacementSnapshot state,
                List<PrefixDecisionFrame> frames,
                bool followsOriginalPath,
                int openingDebt,
                int openRootCount,
                int chainMergeCount,
                long candidateScore,
                int ordinal)
            {
                State = state;
                Frames = frames;
                FollowsOriginalPath =
                    followsOriginalPath;
                OpeningDebt = openingDebt;
                OpenRootCount = openRootCount;
                ChainMergeCount = chainMergeCount;
                CandidateScore = candidateScore;
                Ordinal = ordinal;
            }
        }

        private readonly struct SuffixPlacementCandidate
        {
            public readonly BusDefinition Candidate;
            public readonly int SlotIndex;
            public readonly int BlockedClearExitCount;
            public readonly bool IsAuthoredSlot;
            public readonly int Ordinal;

            public SuffixPlacementCandidate(
                BusDefinition candidate,
                int slotIndex,
                int blockedClearExitCount,
                bool isAuthoredSlot,
                int ordinal)
            {
                Candidate = candidate;
                SlotIndex = slotIndex;
                BlockedClearExitCount =
                    blockedClearExitCount;
                IsAuthoredSlot = isAuthoredSlot;
                Ordinal = ordinal;
            }
        }

    }
}
