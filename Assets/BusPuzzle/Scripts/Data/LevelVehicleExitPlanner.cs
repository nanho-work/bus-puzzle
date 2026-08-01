using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BusPuzzle
{
    internal static class LevelVehicleExitPlanner
    {
        private const float ExitClearanceCells = 0.75f;
        private const float SweepStepCells = 0.16f;
        private const float CollisionClearanceCells = 0.035f;

        public static bool TryFindExitOrder(
            IReadOnlyList<BusDefinition> buses,
            out List<int> exitOrder,
            out List<int> stuckIndices)
        {
            return TryFindExitOrder(
                buses,
                out exitOrder,
                out stuckIndices,
                CancellationToken.None);
        }

        public static bool TryFindExitOrder(
            IReadOnlyList<BusDefinition> buses,
            out List<int> exitOrder,
            out List<int> stuckIndices,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            exitOrder = new List<int>();
            stuckIndices = new List<int>();

            if (buses == null || buses.Count == 0)
            {
                return true;
            }

            var active = new bool[buses.Count];
            for (var index = 0; index < active.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                active[index] = true;
            }

            var removedAny = true;
            while (removedAny)
            {
                cancellationToken.ThrowIfCancellationRequested();
                removedAny = false;

                for (var index = 0; index < buses.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!active[index] ||
                        !IsPathClear(
                            index,
                            buses,
                            active,
                            out _,
                            cancellationToken))
                    {
                        continue;
                    }

                    active[index] = false;
                    exitOrder.Add(index);
                    removedAny = true;
                }
            }

            for (var index = 0; index < active.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (active[index])
                {
                    stuckIndices.Add(index);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return stuckIndices.Count == 0;
        }

        public static bool IsPathClear(
            int movingIndex,
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<bool> active,
            out int blockingIndex)
        {
            return IsPathClear(
                movingIndex,
                buses,
                active,
                null,
                out blockingIndex,
                CancellationToken.None);
        }

        public static bool IsPathClear(
            int movingIndex,
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<bool> active,
            out int blockingIndex,
            CancellationToken cancellationToken)
        {
            return IsPathClear(
                movingIndex,
                buses,
                active,
                null,
                out blockingIndex,
                cancellationToken);
        }

        public static bool IsPathClear(
            int movingIndex,
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<bool> active,
            IReadOnlyList<GarageDefinition> activeGarages,
            out int blockingIndex)
        {
            return IsPathClear(
                movingIndex,
                buses,
                active,
                activeGarages,
                out blockingIndex,
                CancellationToken.None);
        }

        public static bool IsPathClear(
            int movingIndex,
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<bool> active,
            IReadOnlyList<GarageDefinition> activeGarages,
            out int blockingIndex,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            blockingIndex = -1;
            if (buses == null || movingIndex < 0 || movingIndex >= buses.Count)
            {
                return false;
            }

            var movingBus = buses[movingIndex];
            var worldDirection = movingBus.Rotation * Vector3.forward;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            worldDirection.Normalize();
            var sweepDistance = GetBoardExitSweepDistance(movingBus, worldDirection);
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(sweepDistance / SweepStepCells));
            var movingRotation = movingBus.Rotation;
            var movingRoot = GetRootPositionCells(movingBus);

            for (var sample = 1; sample <= sampleCount; sample++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var distance = Mathf.Min(sweepDistance, sample * SweepStepCells);
                var footprint = BoardLayoutConfig.GetVehicleFootprint(
                    movingRoot + worldDirection * distance,
                    movingRotation,
                    movingBus.Size,
                    1f);

                for (var index = 0; index < buses.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (index == movingIndex || !IsActive(active, index))
                    {
                        continue;
                    }

                    if (footprint.IsWithinPadding(BoardLayoutConfig.GetVehicleFootprintCells(buses[index]), CollisionClearanceCells))
                    {
                        blockingIndex = index;
                        return false;
                    }
                }

                if (IntersectsGarageObstacle(
                    footprint,
                    activeGarages,
                    cancellationToken))
                {
                    blockingIndex = -1;
                    return false;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        private static bool IntersectsGarageObstacle(
            VehicleFootprint footprint,
            IReadOnlyList<GarageDefinition> activeGarages,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (activeGarages == null)
            {
                return false;
            }

            for (var index = 0; index < activeGarages.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var garageFootprint = new VehicleFootprint(
                    new Vector3(activeGarages[index].GridPosition.x, 0f, activeGarages[index].GridPosition.y),
                    Vector3.right,
                    Vector3.forward,
                    0.45f,
                    0.45f);
                if (footprint.IsWithinPadding(garageFootprint, CollisionClearanceCells))
                {
                    return true;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }

        private static bool IsActive(IReadOnlyList<bool> active, int index)
        {
            return active == null || index < 0 || index >= active.Count || active[index];
        }

        private static Vector3 GetRootPositionCells(BusDefinition bus)
        {
            return new Vector3(
                bus.GridPosition.x + bus.PositionOffsetCells.x,
                0f,
                bus.GridPosition.y + bus.PositionOffsetCells.y);
        }

        private static float GetBoardExitSweepDistance(BusDefinition bus, Vector3 worldDirection)
        {
            var footprint = BoardLayoutConfig.GetVehicleFootprintCells(bus);
            var leftBoundary = -0.5f - ExitClearanceCells;
            var rightBoundary = BoardLayoutConfig.GridColumns - 0.5f + ExitClearanceCells;
            var bottomBoundary = -0.5f - ExitClearanceCells;
            var topBoundary = BoardLayoutConfig.GridRows - 0.5f + ExitClearanceCells;
            var bestDistance = float.PositiveInfinity;

            if (worldDirection.x > 0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (rightBoundary - footprint.ProjectMax(Vector2.right)) / worldDirection.x);
            }
            else if (worldDirection.x < -0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (footprint.ProjectMin(Vector2.right) - leftBoundary) / -worldDirection.x);
            }

            if (worldDirection.z > 0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (topBoundary - footprint.ProjectMax(Vector2.up)) / worldDirection.z);
            }
            else if (worldDirection.z < -0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (footprint.ProjectMin(Vector2.up) - bottomBoundary) / -worldDirection.z);
            }

            if (float.IsInfinity(bestDistance) || float.IsNaN(bestDistance))
            {
                return Mathf.Max(BoardLayoutConfig.GridColumns, BoardLayoutConfig.GridRows);
            }

            return Mathf.Max(0.5f, bestDistance);
        }
    }
}
