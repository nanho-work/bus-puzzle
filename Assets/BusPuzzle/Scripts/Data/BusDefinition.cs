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
        [SerializeField] private float angleOffsetDegrees;
        [SerializeField] private Vector2 positionOffsetCells;
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private bool startsConcealed;

        public BusDefinition(PuzzleColor color, BusSize size, GridDirection direction, Vector2Int gridPosition)
            : this(color, size, direction, gridPosition, 0f, Vector2.zero)
        {
        }

        public BusDefinition(PuzzleColor color, BusSize size, GridDirection direction, Vector2Int gridPosition, float angleOffsetDegrees)
            : this(color, size, direction, gridPosition, angleOffsetDegrees, Vector2.zero)
        {
        }

        public BusDefinition(
            PuzzleColor color,
            BusSize size,
            GridDirection direction,
            Vector2Int gridPosition,
            float angleOffsetDegrees,
            Vector2 positionOffsetCells,
            bool startsConcealed = false)
        {
            this.color = color;
            this.size = size;
            this.direction = direction;
            this.angleOffsetDegrees = angleOffsetDegrees;
            this.positionOffsetCells = positionOffsetCells;
            this.gridPosition = gridPosition;
            this.startsConcealed = startsConcealed;
        }

        public PuzzleColor Color => color;
        public BusSize Size => size;
        public GridDirection Direction => direction;
        public float AngleOffsetDegrees => angleOffsetDegrees;
        public float YawDegrees => GridDirectionUtility.ToYawDegrees(direction) + angleOffsetDegrees;
        public Quaternion Rotation => GridDirectionUtility.ToRotation(direction, angleOffsetDegrees);
        public Vector2 PositionOffsetCells => positionOffsetCells;
        public Vector2Int GridPosition => gridPosition;
        public bool StartsConcealed => startsConcealed;
        public int CapacityUnits => BusSizeUtility.ToPassengerUnits(size);
        public int CapacityPeople => BusSizeUtility.ToPeopleCapacity(size);

        public BusDefinition WithStartsConcealed(bool value)
        {
            return new BusDefinition(color, size, direction, gridPosition, angleOffsetDegrees, positionOffsetCells, value);
        }
    }
}
