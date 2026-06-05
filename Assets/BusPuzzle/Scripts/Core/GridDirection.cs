using UnityEngine;

namespace BusPuzzle
{
    public enum GridDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public static class GridDirectionUtility
    {
        public static Vector2Int ToGridVector(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Up:
                    return Vector2Int.up;
                case GridDirection.Right:
                    return Vector2Int.right;
                case GridDirection.Down:
                    return Vector2Int.down;
                case GridDirection.Left:
                    return Vector2Int.left;
                default:
                    return Vector2Int.up;
            }
        }

        public static Vector3 ToWorldVector(GridDirection direction)
        {
            var gridVector = ToGridVector(direction);
            return new Vector3(gridVector.x, 0f, gridVector.y);
        }

        public static float ToYawDegrees(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Up:
                    return 0f;
                case GridDirection.Right:
                    return 90f;
                case GridDirection.Down:
                    return 180f;
                case GridDirection.Left:
                    return -90f;
                default:
                    return 0f;
            }
        }

        public static Quaternion ToRotation(GridDirection direction)
        {
            return Quaternion.Euler(0f, ToYawDegrees(direction), 0f);
        }

        public static Quaternion ToRotation(GridDirection direction, float angleOffsetDegrees)
        {
            return Quaternion.Euler(0f, ToYawDegrees(direction) + angleOffsetDegrees, 0f);
        }

        public static string DisplayName(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Up:
                    return "Up";
                case GridDirection.Right:
                    return "Right";
                case GridDirection.Down:
                    return "Down";
                case GridDirection.Left:
                    return "Left";
                default:
                    return "Unknown";
            }
        }
    }
}
