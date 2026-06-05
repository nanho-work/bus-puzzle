using UnityEngine;

namespace BusPuzzle
{
    public readonly struct BusRouteStep
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public BusRouteStep(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    public readonly struct VehicleFootprint
    {
        private readonly Vector2 center;
        private readonly Vector2 right;
        private readonly Vector2 forward;
        private readonly float halfWidth;
        private readonly float halfLength;

        public VehicleFootprint(Vector3 center, Vector3 right, Vector3 forward, float halfWidth, float halfLength)
        {
            this.center = new Vector2(center.x, center.z);
            this.right = NormalizeFlat(right, Vector2.right);
            this.forward = NormalizeFlat(forward, Vector2.up);
            this.halfWidth = Mathf.Max(0.001f, halfWidth);
            this.halfLength = Mathf.Max(0.001f, halfLength);
        }

        public Vector2 Center => center;
        public Vector2 Right => right;
        public Vector2 Forward => forward;
        public float HalfWidth => halfWidth;
        public float HalfLength => halfLength;

        public bool Overlaps(VehicleFootprint other)
        {
            return Overlaps(other, 0f);
        }

        public bool Overlaps(VehicleFootprint other, float clearance)
        {
            clearance = Mathf.Max(0f, clearance);
            return OverlapsOnAxis(other, right, clearance)
                && OverlapsOnAxis(other, forward, clearance)
                && OverlapsOnAxis(other, other.right, clearance)
                && OverlapsOnAxis(other, other.forward, clearance);
        }

        public bool IsWithinPadding(VehicleFootprint other, float padding)
        {
            padding = Mathf.Max(0f, padding);
            return OverlapsOnAxis(other, right, -padding)
                && OverlapsOnAxis(other, forward, -padding)
                && OverlapsOnAxis(other, other.right, -padding)
                && OverlapsOnAxis(other, other.forward, -padding);
        }

        public Vector3 GetCorner(int index, float y = 0f)
        {
            var rightSign = index == 0 || index == 3 ? -1f : 1f;
            var forwardSign = index < 2 ? -1f : 1f;
            var corner = center + right * (halfWidth * rightSign) + forward * (halfLength * forwardSign);
            return new Vector3(corner.x, y, corner.y);
        }

        public Vector3 GetCenter(float y = 0f)
        {
            return new Vector3(center.x, y, center.y);
        }

        public float ProjectMax(Vector2 axis)
        {
            axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector2.right;
            return Vector2.Dot(center, axis) + ProjectRadius(axis);
        }

        public float ProjectMin(Vector2 axis)
        {
            axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector2.right;
            return Vector2.Dot(center, axis) - ProjectRadius(axis);
        }

        private bool OverlapsOnAxis(VehicleFootprint other, Vector2 axis, float clearance)
        {
            var distance = Mathf.Abs(Vector2.Dot(other.center - center, axis));
            return distance <= ProjectRadius(axis) + other.ProjectRadius(axis) - clearance;
        }

        private float ProjectRadius(Vector2 axis)
        {
            return halfWidth * Mathf.Abs(Vector2.Dot(axis, right))
                + halfLength * Mathf.Abs(Vector2.Dot(axis, forward));
        }

        private static Vector2 NormalizeFlat(Vector3 value, Vector2 fallback)
        {
            var flat = new Vector2(value.x, value.z);
            return flat.sqrMagnitude > 0.0001f ? flat.normalized : fallback;
        }
    }
}
