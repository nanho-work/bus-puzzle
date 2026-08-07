using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BusPuzzle
{
    public readonly struct StageSolutionAnalysis
    {
        public readonly bool IsSolvable;
        public readonly int SolutionCount;
        public readonly bool HitLimit;

        public StageSolutionAnalysis(bool isSolvable, int solutionCount, bool hitLimit)
        {
            IsSolvable = isSolvable;
            SolutionCount = solutionCount;
            HitLimit = hitLimit;
        }
    }

    /// <summary>
    /// Result of the opt-in memoized witness search. This search proves at most
    /// one solution and is intentionally separate from the solution-counting
    /// analyzer so callers can compare both implementations without changing
    /// the shipped acceptance rules.
    /// </summary>
    public readonly struct StageSolutionWitnessStep
    {
        public readonly int VehicleIndex;
        public readonly int GarageIndex;
        public readonly int GarageProgress;

        public StageSolutionWitnessStep(
            int vehicleIndex,
            int garageIndex,
            int garageProgress)
        {
            VehicleIndex = vehicleIndex;
            GarageIndex = garageIndex;
            GarageProgress = garageProgress;
        }
    }

    public readonly struct StageMemoizedWitnessAnalysis
    {
        public readonly StageSolutionAnalysis Analysis;
        public readonly int VisitedNodes;
        public readonly int MemoizedStateCount;
        public readonly int MemoHits;
        public readonly bool HitNodeLimit;
        public readonly bool HitMemoLimit;
        public readonly bool WitnessValidated;
        public readonly IReadOnlyList<StageSolutionWitnessStep> Witness;

        public StageMemoizedWitnessAnalysis(
            StageSolutionAnalysis analysis,
            int visitedNodes,
            int memoizedStateCount,
            int memoHits,
            bool hitNodeLimit,
            bool hitMemoLimit,
            bool witnessValidated,
            IReadOnlyList<StageSolutionWitnessStep> witness)
        {
            Analysis = analysis;
            VisitedNodes = Mathf.Max(0, visitedNodes);
            MemoizedStateCount = Mathf.Max(0, memoizedStateCount);
            MemoHits = Mathf.Max(0, memoHits);
            HitNodeLimit = hitNodeLimit;
            HitMemoLimit = hitMemoLimit;
            WitnessValidated = witnessValidated;
            Witness = witness ?? Array.Empty<StageSolutionWitnessStep>();
        }

        public bool IsSolvable => Analysis.IsSolvable;
        public int SolutionCount => Analysis.SolutionCount;
        public bool HitLimit => Analysis.HitLimit;
    }

    public static class StageSolutionAnalyzer
    {
        public static StageSolutionAnalysis Analyze(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int solutionCountLimit)
        {
            return Analyze(buses, garages, solutionCountLimit, int.MaxValue);
        }

        public static StageSolutionAnalysis Analyze(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int solutionCountLimit,
            CancellationToken cancellationToken)
        {
            return Analyze(
                buses,
                garages,
                solutionCountLimit,
                int.MaxValue,
                cancellationToken);
        }

        public static StageSolutionAnalysis Analyze(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int solutionCountLimit,
            int nodeVisitLimit)
        {
            return Analyze(
                buses,
                garages,
                solutionCountLimit,
                nodeVisitLimit,
                CancellationToken.None);
        }

        public static StageSolutionAnalysis Analyze(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int solutionCountLimit,
            int nodeVisitLimit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            solutionCountLimit = Mathf.Max(1, solutionCountLimit);
            nodeVisitLimit = Mathf.Max(1, nodeVisitLimit);
            var state = StageSolutionState.Create(buses, garages);
            cancellationToken.ThrowIfCancellationRequested();
            var visitedNodes = 0;
            var hitNodeLimit = false;
            var count = CountSolutions(
                state,
                solutionCountLimit,
                nodeVisitLimit,
                ref visitedNodes,
                ref hitNodeLimit,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new StageSolutionAnalysis(count > 0, count, count >= solutionCountLimit || hitNodeLimit);
        }

        public static StageMemoizedWitnessAnalysis AnalyzeMemoizedWitness(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int nodeVisitLimit,
            int memoizedStateLimit)
        {
            return AnalyzeMemoizedWitness(
                buses,
                garages,
                nodeVisitLimit,
                memoizedStateLimit,
                CancellationToken.None);
        }

        public static StageMemoizedWitnessAnalysis AnalyzeMemoizedWitness(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int nodeVisitLimit,
            int memoizedStateLimit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            nodeVisitLimit = Mathf.Max(1, nodeVisitLimit);
            memoizedStateLimit = Mathf.Max(1, memoizedStateLimit);
            var state = MemoizedWitnessState.Create(buses, garages);
            var deadStates =
                new HashSet<MemoizedStageSolutionStateKey>();
            var visitedNodes = 0;
            var memoHits = 0;
            var hitNodeLimit = false;
            var hitMemoLimit = false;
            var witness = new List<StageSolutionWitnessStep>();
            cancellationToken.ThrowIfCancellationRequested();
            var searchOutcome = TryFindMemoizedWitness(
                state,
                nodeVisitLimit,
                memoizedStateLimit,
                deadStates,
                ref visitedNodes,
                ref memoHits,
                ref hitNodeLimit,
                ref hitMemoLimit,
                witness,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var hasWitness =
                searchOutcome == MemoizedSearchOutcome.Solved;
            var witnessValidated = hasWitness &&
                ValidateMemoizedWitness(
                    buses,
                    garages,
                    witness,
                    cancellationToken);
            if (hasWitness && !witnessValidated)
            {
                hasWitness = false;
                witness.Clear();
            }

            var analysis = new StageSolutionAnalysis(
                hasWitness,
                hasWitness ? 1 : 0,
                hasWitness || hitNodeLimit);
            return new StageMemoizedWitnessAnalysis(
                analysis,
                visitedNodes,
                deadStates.Count,
                memoHits,
                hitNodeLimit,
                hitMemoLimit,
                witnessValidated,
                witness.ToArray());
        }

        private static int CountSolutions(
            StageSolutionState state,
            int remainingLimit,
            int nodeVisitLimit,
            ref int visitedNodes,
            ref bool hitNodeLimit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (remainingLimit <= 0)
            {
                return 0;
            }

            if (visitedNodes >= nodeVisitLimit)
            {
                hitNodeLimit = true;
                return 0;
            }

            visitedNodes++;
            if (!state.HasActiveVehicles)
            {
                return 1;
            }

            var count = 0;
            // Garage fronts unlock queued vehicles and eventually remove a large static
            // obstacle. Exploring them before ordinary cars avoids spending the bounded
            // node budget on equivalent permutations of unrelated exits.
            for (var garagePriorityPass = 0; garagePriorityPass < 2; garagePriorityPass++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requireGarageVehicle = garagePriorityPass == 0;
                for (var index = 0; index < state.Buses.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (state.IsGarageVehicle(index) != requireGarageVehicle ||
                        !state.Active[index] ||
                        !LevelVehicleExitPlanner.IsPathClear(
                            index,
                            state.Buses,
                            state.Active,
                            state.ActiveGarageObstacles,
                            out _,
                            cancellationToken))
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var nextState = state.Clone();
                    cancellationToken.ThrowIfCancellationRequested();
                    nextState.RemoveVehicle(index);
                    count += CountSolutions(
                        nextState,
                        remainingLimit - count,
                        nodeVisitLimit,
                        ref visitedNodes,
                        ref hitNodeLimit,
                        cancellationToken);
                    if (count >= remainingLimit)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return remainingLimit;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return count;
        }

        private enum MemoizedSearchOutcome
        {
            Solved,
            Dead,
            Incomplete
        }

        private static MemoizedSearchOutcome TryFindMemoizedWitness(
            MemoizedWitnessState state,
            int nodeVisitLimit,
            int memoizedStateLimit,
            HashSet<MemoizedStageSolutionStateKey> deadStates,
            ref int visitedNodes,
            ref int memoHits,
            ref bool hitNodeLimit,
            ref bool hitMemoLimit,
            List<StageSolutionWitnessStep> witness,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Count every entered state, including memo hits, against the
            // same bounded work budget as CountSolutions. Excluding cached
            // states allows thousands of additional path probes after the
            // legacy analyzer would already have stopped.
            if (visitedNodes >= nodeVisitLimit)
            {
                hitNodeLimit = true;
                return MemoizedSearchOutcome.Incomplete;
            }

            visitedNodes++;
            if (!state.HasActiveVehicles)
            {
                return MemoizedSearchOutcome.Solved;
            }

            var stateKey = state.CreateMemoizedKey(cancellationToken);
            if (deadStates.Contains(stateKey))
            {
                memoHits++;
                return MemoizedSearchOutcome.Dead;
            }
            for (var garagePriorityPass = 0; garagePriorityPass < 2; garagePriorityPass++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requireGarageVehicle = garagePriorityPass == 0;
                for (var index = 0; index < state.Buses.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (state.IsGarageVehicle(index) != requireGarageVehicle ||
                        !state.Active[index] ||
                        !LevelVehicleExitPlanner.IsPathClear(
                            index,
                            state.Buses,
                            state.Active,
                            state.ActiveGarageObstacles,
                            out _,
                            cancellationToken))
                    {
                        continue;
                    }

                    witness.Add(state.CreateWitnessStep(index));
                    state.ApplyVehicle(index, out var undo);
                    var childOutcome =
                        MemoizedSearchOutcome.Incomplete;
                    try
                    {
                        childOutcome = TryFindMemoizedWitness(
                            state,
                            nodeVisitLimit,
                            memoizedStateLimit,
                            deadStates,
                            ref visitedNodes,
                            ref memoHits,
                            ref hitNodeLimit,
                            ref hitMemoLimit,
                            witness,
                            cancellationToken);
                    }
                    finally
                    {
                        state.UndoVehicle(undo);
                        if (childOutcome !=
                            MemoizedSearchOutcome.Solved)
                        {
                            witness.RemoveAt(
                                witness.Count - 1);
                        }
                    }

                    if (childOutcome ==
                        MemoizedSearchOutcome.Solved)
                    {
                        return MemoizedSearchOutcome.Solved;
                    }

                    if (childOutcome ==
                        MemoizedSearchOutcome.Incomplete)
                    {
                        // The current state is not proven dead when even one
                        // child exhausts the global node budget.
                        return MemoizedSearchOutcome.Incomplete;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (deadStates.Count < memoizedStateLimit)
            {
                deadStates.Add(stateKey);
            }
            else
            {
                // Reaching the memo capacity does not make the proof
                // incomplete; search continues under the independent node
                // budget without retaining additional dead states.
                hitMemoLimit = true;
            }

            return MemoizedSearchOutcome.Dead;
        }

        private static bool ValidateMemoizedWitness(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            IReadOnlyList<StageSolutionWitnessStep> witness,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var replay = MemoizedWitnessState.Create(
                buses,
                garages);
            for (var stepIndex = 0; stepIndex < witness.Count; stepIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = witness[stepIndex];
                if (!replay.MatchesWitnessStep(step) ||
                    !LevelVehicleExitPlanner.IsPathClear(
                        step.VehicleIndex,
                        replay.Buses,
                        replay.Active,
                        replay.ActiveGarageObstacles,
                        out _,
                        cancellationToken))
                {
                    return false;
                }

                replay.ApplyVehicle(
                    step.VehicleIndex,
                    out _);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return !replay.HasActiveVehicles;
        }

        private readonly struct MemoizedStageSolutionStateKey :
            IEquatable<MemoizedStageSolutionStateKey>
        {
            private readonly bool isPacked;
            private readonly int slotCount;
            private readonly int garageCount;
            private readonly ulong activeLow;
            private readonly ulong activeHigh;
            private readonly uint garageProgressBits;
            private readonly byte activeGarageObstacleMask;
            private readonly WideMemoizedStageSolutionStateKey wideKey;
            private readonly int hashCode;

            private MemoizedStageSolutionStateKey(
                int slotCount,
                int garageCount,
                ulong activeLow,
                ulong activeHigh,
                uint garageProgressBits,
                byte activeGarageObstacleMask)
            {
                isPacked = true;
                this.slotCount = slotCount;
                this.garageCount = garageCount;
                this.activeLow = activeLow;
                this.activeHigh = activeHigh;
                this.garageProgressBits = garageProgressBits;
                this.activeGarageObstacleMask =
                    activeGarageObstacleMask;
                wideKey = null;
                hashCode = CalculatePackedHashCode(
                    slotCount,
                    garageCount,
                    activeLow,
                    activeHigh,
                    garageProgressBits,
                    activeGarageObstacleMask);
            }

            private MemoizedStageSolutionStateKey(
                WideMemoizedStageSolutionStateKey wideKey)
            {
                isPacked = false;
                slotCount = 0;
                garageCount = 0;
                activeLow = 0UL;
                activeHigh = 0UL;
                garageProgressBits = 0U;
                activeGarageObstacleMask = 0;
                this.wideKey = wideKey ??
                    throw new ArgumentNullException(nameof(wideKey));
                hashCode = wideKey.GetHashCode();
            }

            public static MemoizedStageSolutionStateKey CreatePacked(
                int slotCount,
                int garageCount,
                ulong activeLow,
                ulong activeHigh,
                uint garageProgressBits,
                byte activeGarageObstacleMask)
            {
                return new MemoizedStageSolutionStateKey(
                    slotCount,
                    garageCount,
                    activeLow,
                    activeHigh,
                    garageProgressBits,
                    activeGarageObstacleMask);
            }

            public static MemoizedStageSolutionStateKey CreateWide(
                ulong[] activeVehicleWords,
                int[] garageProgress,
                ulong[] activeGarageObstacleWords)
            {
                return new MemoizedStageSolutionStateKey(
                    new WideMemoizedStageSolutionStateKey(
                        activeVehicleWords,
                        garageProgress,
                        activeGarageObstacleWords));
            }

            public bool Equals(
                MemoizedStageSolutionStateKey other)
            {
                if (isPacked != other.isPacked)
                {
                    return false;
                }

                if (!isPacked)
                {
                    return wideKey != null &&
                        wideKey.Equals(other.wideKey);
                }

                return slotCount == other.slotCount &&
                    garageCount == other.garageCount &&
                    activeLow == other.activeLow &&
                    activeHigh == other.activeHigh &&
                    garageProgressBits ==
                        other.garageProgressBits &&
                    activeGarageObstacleMask ==
                        other.activeGarageObstacleMask;
            }

            public override bool Equals(object obj)
            {
                return obj is MemoizedStageSolutionStateKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            private static int CalculatePackedHashCode(
                int slotCount,
                int garageCount,
                ulong activeLow,
                ulong activeHigh,
                uint garageProgressBits,
                byte activeGarageObstacleMask)
            {
                unchecked
                {
                    var hash = 17;
                    hash = (hash * 31) + slotCount;
                    hash = (hash * 31) + garageCount;
                    hash = (hash * 31) + (int)activeLow;
                    hash = (hash * 31) +
                        (int)(activeLow >> 32);
                    hash = (hash * 31) + (int)activeHigh;
                    hash = (hash * 31) +
                        (int)(activeHigh >> 32);
                    hash = (hash * 31) +
                        (int)garageProgressBits;
                    hash = (hash * 31) +
                        activeGarageObstacleMask;
                    return hash;
                }
            }
        }

        private sealed class WideMemoizedStageSolutionStateKey :
            IEquatable<WideMemoizedStageSolutionStateKey>
        {
            private readonly ulong[] activeVehicleWords;
            private readonly int[] garageProgress;
            private readonly ulong[] activeGarageObstacleWords;
            private readonly int hashCode;

            public WideMemoizedStageSolutionStateKey(
                ulong[] activeVehicleWords,
                int[] garageProgress,
                ulong[] activeGarageObstacleWords)
            {
                this.activeVehicleWords =
                    activeVehicleWords ?? Array.Empty<ulong>();
                this.garageProgress =
                    garageProgress ?? Array.Empty<int>();
                this.activeGarageObstacleWords =
                    activeGarageObstacleWords ??
                    Array.Empty<ulong>();
                hashCode = CalculateHashCode();
            }

            public bool Equals(
                WideMemoizedStageSolutionStateKey other)
            {
                return other != null &&
                    SequenceEqual(
                        activeVehicleWords,
                        other.activeVehicleWords) &&
                    SequenceEqual(
                        garageProgress,
                        other.garageProgress) &&
                    SequenceEqual(
                        activeGarageObstacleWords,
                        other.activeGarageObstacleWords);
            }

            public override bool Equals(object obj)
            {
                return Equals(
                    obj as WideMemoizedStageSolutionStateKey);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            private int CalculateHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = AppendHash(
                        hash,
                        activeVehicleWords);
                    hash = AppendHash(hash, garageProgress);
                    hash = AppendHash(
                        hash,
                        activeGarageObstacleWords);
                    return hash;
                }
            }

            private static int AppendHash(
                int hash,
                IReadOnlyList<ulong> values)
            {
                unchecked
                {
                    hash = (hash * 31) + values.Count;
                    for (var index = 0;
                        index < values.Count;
                        index++)
                    {
                        var value = values[index];
                        hash = (hash * 31) + (int)value;
                        hash = (hash * 31) +
                            (int)(value >> 32);
                    }

                    return hash;
                }
            }

            private static int AppendHash(
                int hash,
                IReadOnlyList<int> values)
            {
                unchecked
                {
                    hash = (hash * 31) + values.Count;
                    for (var index = 0;
                        index < values.Count;
                        index++)
                    {
                        hash = (hash * 31) + values[index];
                    }

                    return hash;
                }
            }

            private static bool SequenceEqual(
                IReadOnlyList<ulong> first,
                IReadOnlyList<ulong> second)
            {
                if (first.Count != second.Count)
                {
                    return false;
                }

                for (var index = 0;
                    index < first.Count;
                    index++)
                {
                    if (first[index] != second[index])
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool SequenceEqual(
                IReadOnlyList<int> first,
                IReadOnlyList<int> second)
            {
                if (first.Count != second.Count)
                {
                    return false;
                }

                for (var index = 0;
                    index < first.Count;
                    index++)
                {
                    if (first[index] != second[index])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class MemoizedWitnessState
        {
            private const int PackedMaximumVehicleSlots = 128;
            private const int PackedMaximumGarageCount = 5;
            private const int PackedGarageProgressBits = 4;
            private const int PackedMaximumGarageProgress =
                (1 << PackedGarageProgressBits) - 1;

            public readonly List<BusDefinition> Buses;
            public readonly List<GarageDefinition>
                ActiveGarageObstacles;
            public readonly bool[] Active;

            private readonly int regularBusCount;
            private readonly GarageDefinition[] garages;
            private readonly BusDefinition[][] garageVehicleSequences;
            private readonly int[] garageProgress;
            private readonly List<int>
                activeGarageObstacleIndices;
            private bool usePackedKey;
            private int activeVehicleCount;
            private ulong activeLow;
            private ulong activeHigh;
            private ulong activeGarageObstacleMask;

            public bool HasActiveVehicles =>
                activeVehicleCount > 0;

            private MemoizedWitnessState(
                int regularBusCount,
                int garageCount)
            {
                this.regularBusCount = regularBusCount;
                var slotCount = regularBusCount + garageCount;
                Buses = new List<BusDefinition>(slotCount);
                ActiveGarageObstacles =
                    new List<GarageDefinition>(garageCount);
                Active = new bool[slotCount];
                garages = new GarageDefinition[garageCount];
                garageVehicleSequences =
                    new BusDefinition[garageCount][];
                garageProgress = new int[garageCount];
                activeGarageObstacleIndices =
                    new List<int>(garageCount);
                usePackedKey =
                    slotCount <= PackedMaximumVehicleSlots &&
                    garageCount <= PackedMaximumGarageCount;
            }

            public static MemoizedWitnessState Create(
                IReadOnlyList<BusDefinition> buses,
                IReadOnlyList<GarageDefinition> garages)
            {
                var regularBusCount =
                    buses != null ? buses.Count : 0;
                var garageCount =
                    garages != null ? garages.Count : 0;
                var state = new MemoizedWitnessState(
                    regularBusCount,
                    garageCount);

                if (buses != null)
                {
                    for (var index = 0;
                        index < buses.Count;
                        index++)
                    {
                        state.Buses.Add(buses[index]);
                        state.ActivateVehicle(index);
                    }
                }

                if (garages != null)
                {
                    for (var garageIndex = 0;
                        garageIndex < garages.Count;
                        garageIndex++)
                    {
                        var garage = garages[garageIndex];
                        var queuedVehicles =
                            garage.QueuedVehicles;
                        var sequence = new BusDefinition[
                            1 + queuedVehicles.Count];
                        sequence[0] = garage.FrontVehicle;
                        for (var queueIndex = 0;
                            queueIndex < queuedVehicles.Count;
                            queueIndex++)
                        {
                            sequence[queueIndex + 1] =
                                queuedVehicles[queueIndex];
                        }

                        state.garages[garageIndex] = garage;
                        state.garageVehicleSequences[
                            garageIndex] = sequence;
                        if (sequence.Length >
                            PackedMaximumGarageProgress)
                        {
                            state.usePackedKey = false;
                        }

                        var busIndex = state.Buses.Count;
                        state.Buses.Add(sequence[0]);
                        state.ActivateVehicle(busIndex);
                        state.ActiveGarageObstacles.Add(garage);
                        state.activeGarageObstacleIndices.Add(
                            garageIndex);
                        if (garageIndex < 64)
                        {
                            state.activeGarageObstacleMask |=
                                1UL << garageIndex;
                        }
                    }
                }

                return state;
            }

            public bool IsGarageVehicle(int busIndex)
            {
                return GetGarageIndex(busIndex) >= 0;
            }

            public StageSolutionWitnessStep CreateWitnessStep(
                int busIndex)
            {
                var garageIndex = GetGarageIndex(busIndex);
                return new StageSolutionWitnessStep(
                    busIndex,
                    garageIndex,
                    garageIndex >= 0
                        ? garageProgress[garageIndex]
                        : -1);
            }

            public bool MatchesWitnessStep(
                StageSolutionWitnessStep step)
            {
                if (step.VehicleIndex < 0 ||
                    step.VehicleIndex >= Active.Length ||
                    !Active[step.VehicleIndex])
                {
                    return false;
                }

                var garageIndex =
                    GetGarageIndex(step.VehicleIndex);
                return garageIndex == step.GarageIndex &&
                    (garageIndex < 0
                        ? step.GarageProgress == -1
                        : garageProgress[garageIndex] ==
                            step.GarageProgress);
            }

            public MemoizedStageSolutionStateKey
                CreateMemoizedKey(
                    CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (usePackedKey)
                {
                    uint progressBits = 0U;
                    for (var garageIndex = 0;
                        garageIndex < garageProgress.Length;
                        garageIndex++)
                    {
                        progressBits |=
                            (uint)garageProgress[garageIndex] <<
                            (garageIndex *
                                PackedGarageProgressBits);
                    }

                    return MemoizedStageSolutionStateKey
                        .CreatePacked(
                            Active.Length,
                            garageProgress.Length,
                            activeLow,
                            activeHigh,
                            progressBits,
                            (byte)activeGarageObstacleMask);
                }

                var activeWords =
                    new ulong[(Active.Length + 63) / 64];
                for (var index = 0;
                    index < Active.Length;
                    index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (Active[index])
                    {
                        activeWords[index / 64] |=
                            1UL << (index % 64);
                    }
                }

                var progress =
                    new int[garageProgress.Length];
                garageProgress.CopyTo(progress, 0);
                var obstacleWords = new ulong[
                    (garages.Length + 63) / 64];
                for (var index = 0;
                    index <
                        activeGarageObstacleIndices.Count;
                    index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var garageIndex =
                        activeGarageObstacleIndices[index];
                    obstacleWords[garageIndex / 64] |=
                        1UL << (garageIndex % 64);
                }

                return MemoizedStageSolutionStateKey.CreateWide(
                    activeWords,
                    progress,
                    obstacleWords);
            }

            public void ApplyVehicle(
                int busIndex,
                out VehicleUndo undo)
            {
                if (busIndex < 0 ||
                    busIndex >= Active.Length ||
                    !Active[busIndex])
                {
                    throw new InvalidOperationException(
                        "Cannot apply an inactive vehicle move.");
                }

                var garageIndex = GetGarageIndex(busIndex);
                var previousBus = Buses[busIndex];
                if (garageIndex < 0)
                {
                    undo = new VehicleUndo(
                        busIndex,
                        -1,
                        -1,
                        previousBus,
                        true,
                        -1,
                        -1);
                    DeactivateVehicle(busIndex);
                    return;
                }

                var previousProgress =
                    garageProgress[garageIndex];
                var sequence =
                    garageVehicleSequences[garageIndex];
                var remainingQueuedBefore =
                    sequence.Length - 1 - previousProgress;
                garageProgress[garageIndex] =
                    previousProgress + 1;

                var removedObstacleListIndex = -1;
                var removedObstacleGarageIndex = -1;
                if (remainingQueuedBefore <= 0)
                {
                    DeactivateVehicle(busIndex);
                    removedObstacleListIndex =
                        RemoveGarageObstacle(
                            garageIndex,
                            out removedObstacleGarageIndex);
                }
                else
                {
                    Buses[busIndex] =
                        sequence[previousProgress + 1];
                    if (remainingQueuedBefore == 1)
                    {
                        removedObstacleListIndex =
                            RemoveGarageObstacle(
                                garageIndex,
                                out removedObstacleGarageIndex);
                    }
                }

                undo = new VehicleUndo(
                    busIndex,
                    garageIndex,
                    previousProgress,
                    previousBus,
                    true,
                    removedObstacleListIndex,
                    removedObstacleGarageIndex);
            }

            public void UndoVehicle(VehicleUndo undo)
            {
                if (undo.RemovedObstacleListIndex >= 0)
                {
                    RestoreGarageObstacle(
                        undo.RemovedObstacleListIndex,
                        undo.RemovedObstacleGarageIndex);
                }

                Buses[undo.BusIndex] = undo.PreviousBus;
                if (undo.GarageIndex >= 0)
                {
                    garageProgress[undo.GarageIndex] =
                        undo.PreviousGarageProgress;
                }

                if (undo.PreviousActive &&
                    !Active[undo.BusIndex])
                {
                    ActivateVehicle(undo.BusIndex);
                }
            }

            private int GetGarageIndex(int busIndex)
            {
                var garageIndex =
                    busIndex - regularBusCount;
                return garageIndex >= 0 &&
                    garageIndex < garages.Length
                        ? garageIndex
                        : -1;
            }

            private void ActivateVehicle(int busIndex)
            {
                if (Active[busIndex])
                {
                    return;
                }

                Active[busIndex] = true;
                activeVehicleCount++;
                SetActiveBit(busIndex, true);
            }

            private void DeactivateVehicle(int busIndex)
            {
                if (!Active[busIndex])
                {
                    return;
                }

                Active[busIndex] = false;
                activeVehicleCount--;
                SetActiveBit(busIndex, false);
            }

            private void SetActiveBit(
                int busIndex,
                bool active)
            {
                if (busIndex < 64)
                {
                    var mask = 1UL << busIndex;
                    activeLow = active
                        ? activeLow | mask
                        : activeLow & ~mask;
                }
                else if (busIndex <
                    PackedMaximumVehicleSlots)
                {
                    var mask = 1UL << (busIndex - 64);
                    activeHigh = active
                        ? activeHigh | mask
                        : activeHigh & ~mask;
                }
            }

            private int RemoveGarageObstacle(
                int garageIndex,
                out int removedGarageIndex)
            {
                removedGarageIndex = -1;
                if (garageIndex < 0 ||
                    garageIndex >= garages.Length)
                {
                    return -1;
                }

                var garagePosition =
                    garages[garageIndex].GridPosition;
                for (var index =
                        ActiveGarageObstacles.Count - 1;
                    index >= 0;
                    index--)
                {
                    if (ActiveGarageObstacles[index]
                        .GridPosition != garagePosition)
                    {
                        continue;
                    }

                    removedGarageIndex =
                        activeGarageObstacleIndices[index];
                    ActiveGarageObstacles.RemoveAt(index);
                    activeGarageObstacleIndices.RemoveAt(index);
                    if (removedGarageIndex < 64)
                    {
                        activeGarageObstacleMask &=
                            ~(1UL << removedGarageIndex);
                    }

                    return index;
                }

                return -1;
            }

            private void RestoreGarageObstacle(
                int listIndex,
                int garageIndex)
            {
                if (garageIndex < 0 ||
                    garageIndex >= garages.Length)
                {
                    throw new InvalidOperationException(
                        "Cannot restore an unknown garage obstacle.");
                }

                ActiveGarageObstacles.Insert(
                    listIndex,
                    garages[garageIndex]);
                activeGarageObstacleIndices.Insert(
                    listIndex,
                    garageIndex);
                if (garageIndex < 64)
                {
                    activeGarageObstacleMask |=
                        1UL << garageIndex;
                }
            }

            public readonly struct VehicleUndo
            {
                public readonly int BusIndex;
                public readonly int GarageIndex;
                public readonly int PreviousGarageProgress;
                public readonly BusDefinition PreviousBus;
                public readonly bool PreviousActive;
                public readonly int RemovedObstacleListIndex;
                public readonly int RemovedObstacleGarageIndex;

                public VehicleUndo(
                    int busIndex,
                    int garageIndex,
                    int previousGarageProgress,
                    BusDefinition previousBus,
                    bool previousActive,
                    int removedObstacleListIndex,
                    int removedObstacleGarageIndex)
                {
                    BusIndex = busIndex;
                    GarageIndex = garageIndex;
                    PreviousGarageProgress =
                        previousGarageProgress;
                    PreviousBus = previousBus;
                    PreviousActive = previousActive;
                    RemovedObstacleListIndex =
                        removedObstacleListIndex;
                    RemovedObstacleGarageIndex =
                        removedObstacleGarageIndex;
                }
            }
        }

        private sealed class StageSolutionState
        {
            public readonly List<BusDefinition> Buses = new List<BusDefinition>();
            public readonly List<GarageDefinition> ActiveGarageObstacles = new List<GarageDefinition>();
            public bool[] Active;

            private readonly int[] garageIndexByBus;
            private readonly List<GarageDefinition> garages = new List<GarageDefinition>();
            private readonly List<Queue<BusDefinition>> garageQueues = new List<Queue<BusDefinition>>();

            public bool HasActiveVehicles
            {
                get
                {
                    for (var index = 0; index < Active.Length; index++)
                    {
                        if (Active[index])
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            private StageSolutionState(int busCount)
            {
                Active = new bool[busCount];
                garageIndexByBus = new int[busCount];
                for (var index = 0; index < garageIndexByBus.Length; index++)
                {
                    garageIndexByBus[index] = -1;
                }
            }

            public static StageSolutionState Create(IReadOnlyList<BusDefinition> buses, IReadOnlyList<GarageDefinition> garages)
            {
                var busCount = buses != null ? buses.Count : 0;
                var garageCount = garages != null ? garages.Count : 0;
                var state = new StageSolutionState(busCount + garageCount);

                if (buses != null)
                {
                    for (var index = 0; index < buses.Count; index++)
                    {
                        state.Buses.Add(buses[index]);
                        state.Active[index] = true;
                    }
                }

                if (garages != null)
                {
                    for (var garageIndex = 0; garageIndex < garages.Count; garageIndex++)
                    {
                        var garage = garages[garageIndex];
                        var busIndex = state.Buses.Count;
                        state.Buses.Add(garage.FrontVehicle);
                        state.Active[busIndex] = true;
                        state.garageIndexByBus[busIndex] = garageIndex;
                        state.garages.Add(garage);
                        state.garageQueues.Add(new Queue<BusDefinition>(garage.QueuedVehicles));
                        state.ActiveGarageObstacles.Add(garage);
                    }
                }

                return state;
            }

            public StageSolutionState Clone()
            {
                var clone = new StageSolutionState(Buses.Count);
                clone.Buses.AddRange(Buses);
                Active.CopyTo(clone.Active, 0);
                garageIndexByBus.CopyTo(clone.garageIndexByBus, 0);
                clone.garages.AddRange(garages);
                clone.ActiveGarageObstacles.AddRange(ActiveGarageObstacles);

                for (var index = 0; index < garageQueues.Count; index++)
                {
                    clone.garageQueues.Add(new Queue<BusDefinition>(garageQueues[index]));
                }

                return clone;
            }

            public bool IsGarageVehicle(int busIndex)
            {
                return busIndex >= 0 &&
                    busIndex < garageIndexByBus.Length &&
                    garageIndexByBus[busIndex] >= 0;
            }

            public void RemoveVehicle(int busIndex)
            {
                var garageIndex = garageIndexByBus[busIndex];
                if (garageIndex < 0)
                {
                    Active[busIndex] = false;
                    return;
                }

                var queue = garageQueues[garageIndex];
                if (queue.Count == 0)
                {
                    Active[busIndex] = false;
                    RemoveGarageObstacle(garageIndex);
                    return;
                }

                Buses[busIndex] = queue.Dequeue();
                if (queue.Count == 0)
                {
                    RemoveGarageObstacle(garageIndex);
                }
            }

            private void RemoveGarageObstacle(int garageIndex)
            {
                if (garageIndex < 0 || garageIndex >= garages.Count)
                {
                    return;
                }

                var garagePosition = garages[garageIndex].GridPosition;
                for (var index = ActiveGarageObstacles.Count - 1; index >= 0; index--)
                {
                    if (ActiveGarageObstacles[index].GridPosition == garagePosition)
                    {
                        ActiveGarageObstacles.RemoveAt(index);
                        return;
                    }
                }
            }
        }
    }
}
