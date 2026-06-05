using System.Collections.Generic;
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

    public static class StageSolutionAnalyzer
    {
        public static StageSolutionAnalysis Analyze(
            IReadOnlyList<BusDefinition> buses,
            IReadOnlyList<GarageDefinition> garages,
            int solutionCountLimit)
        {
            solutionCountLimit = Mathf.Max(1, solutionCountLimit);
            var state = StageSolutionState.Create(buses, garages);
            var count = CountSolutions(state, solutionCountLimit);
            return new StageSolutionAnalysis(count > 0, count, count >= solutionCountLimit);
        }

        private static int CountSolutions(StageSolutionState state, int remainingLimit)
        {
            if (remainingLimit <= 0)
            {
                return 0;
            }

            if (!state.HasActiveVehicles)
            {
                return 1;
            }

            var count = 0;
            for (var index = 0; index < state.Buses.Count; index++)
            {
                if (!state.Active[index] || !LevelVehicleExitPlanner.IsPathClear(index, state.Buses, state.Active, state.ActiveGarageObstacles, out _))
                {
                    continue;
                }

                var nextState = state.Clone();
                nextState.RemoveVehicle(index);
                count += CountSolutions(nextState, remainingLimit - count);
                if (count >= remainingLimit)
                {
                    return remainingLimit;
                }
            }

            return count;
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
