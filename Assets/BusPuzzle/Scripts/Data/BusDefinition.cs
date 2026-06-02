using System;
using UnityEngine;

namespace BusPuzzle
{
    [Serializable]
    public struct BusDefinition
    {
        [SerializeField] private PuzzleColor color;
        [SerializeField] private BusSize size;
        [SerializeField] private GridDirection direction;
        [SerializeField] private Vector2Int gridPosition;

        public BusDefinition(PuzzleColor color, BusSize size, GridDirection direction, Vector2Int gridPosition)
        {
            this.color = color;
            this.size = size;
            this.direction = direction;
            this.gridPosition = gridPosition;
        }

        public PuzzleColor Color => color;
        public BusSize Size => size;
        public GridDirection Direction => direction;
        public Vector2Int GridPosition => gridPosition;
        public int SizeCells => BusSizeUtility.ToBoardCells(size);
        public int CapacityUnits => BusSizeUtility.ToPassengerUnits(size);
        public int CapacityPeople => BusSizeUtility.ToPeopleCapacity(size);
    }
}
