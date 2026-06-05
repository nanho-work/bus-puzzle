using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [Serializable]
    public struct GarageDefinition
    {
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private GridDirection exitDirection;
        [SerializeField] private BusDefinition frontVehicle;
        [SerializeField] private List<BusDefinition> queuedVehicles;

        public GarageDefinition(
            Vector2Int gridPosition,
            GridDirection exitDirection,
            BusDefinition frontVehicle,
            IEnumerable<BusDefinition> queuedVehicles)
        {
            this.gridPosition = gridPosition;
            this.exitDirection = exitDirection;
            this.frontVehicle = frontVehicle;
            this.queuedVehicles = queuedVehicles != null
                ? new List<BusDefinition>(queuedVehicles)
                : new List<BusDefinition>();
        }

        public Vector2Int GridPosition => gridPosition;
        public GridDirection ExitDirection => exitDirection;
        public Vector2Int FrontVehicleGridPosition => gridPosition + GridDirectionUtility.ToGridVector(exitDirection);
        public BusDefinition FrontVehicle => frontVehicle;
        public IReadOnlyList<BusDefinition> QueuedVehicles => queuedVehicles ?? EmptyVehicles;
        public int QueuedVehicleCount => QueuedVehicles.Count;
        public int TotalVehicleCount => 1 + QueuedVehicleCount;

        private static readonly IReadOnlyList<BusDefinition> EmptyVehicles = Array.Empty<BusDefinition>();

        public IEnumerable<BusDefinition> EnumerateVehicles()
        {
            yield return frontVehicle;

            var queue = QueuedVehicles;
            for (var index = 0; index < queue.Count; index++)
            {
                yield return queue[index];
            }
        }
    }
}
