using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct RotaryPathSample
    {
        public readonly Vector2 Point;
        public readonly Vector2 Tangent;
        public readonly Vector2 Outward;

        public RotaryPathSample(Vector2 point, Vector2 tangent, Vector2 outward)
        {
            Point = point;
            Tangent = tangent;
            Outward = outward;
        }
    }

    internal sealed class RotaryPath
    {
        public RotaryPath(Vector2[] points)
        {
            Points = points;
            Distances = BuildPathDistances(points, out var length);
            Length = length;

            for (var index = 0; index < points.Length; index++)
            {
                RadiusX = Mathf.Max(RadiusX, Mathf.Abs(points[index].x));
                RadiusZ = Mathf.Max(RadiusZ, Mathf.Abs(points[index].y));
            }
        }

        public Vector2[] Points { get; }
        public float[] Distances { get; }
        public float Length { get; }
        public float RadiusX { get; }
        public float RadiusZ { get; }

        public RotaryPathSample SampleByPointIndex(int pointIndex)
        {
            var previous = Points[(pointIndex - 1 + Points.Length) % Points.Length];
            var next = Points[(pointIndex + 1) % Points.Length];
            var tangent = (next - previous).normalized;
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector2.right;
            }

            return CreateSample(Points[pointIndex], tangent);
        }

        public RotaryPathSample Sample(float progress)
        {
            return SampleByDistance(Mathf.Repeat(progress, 1f) * Length);
        }

        public RotaryPathSample SampleByDistance(float distance)
        {
            distance = Mathf.Repeat(distance, Length);

            for (var index = 0; index < Points.Length; index++)
            {
                if (distance > Distances[index + 1] && index + 1 < Points.Length)
                {
                    continue;
                }

                var start = Points[index];
                var end = Points[(index + 1) % Points.Length];
                var segmentLength = Mathf.Max(0.0001f, Distances[index + 1] - Distances[index]);
                var t = Mathf.Clamp01((distance - Distances[index]) / segmentLength);
                var previous = Points[(index - 1 + Points.Length) % Points.Length];
                var afterNext = Points[(index + 2) % Points.Length];
                var point = CatmullRom(previous, start, end, afterNext, t);
                var tangent = CatmullRomTangent(previous, start, end, afterNext, t).normalized;
                if (tangent.sqrMagnitude < 0.0001f)
                {
                    tangent = (end - start).normalized;
                    if (tangent.sqrMagnitude < 0.0001f)
                    {
                        tangent = Vector2.right;
                    }
                }

                return CreateSample(point, tangent);
            }

            return SampleByPointIndex(0);
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static Vector2 CatmullRomTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var t2 = t * t;
            return 0.5f * (
                -p0 + p2 +
                2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
                3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
        }

        private static RotaryPathSample CreateSample(Vector2 point, Vector2 tangent)
        {
            var outward = new Vector2(tangent.y, -tangent.x).normalized;
            return new RotaryPathSample(point, tangent, outward);
        }

        private static float[] BuildPathDistances(IReadOnlyList<Vector2> points, out float pathLength)
        {
            var distances = new float[points.Count + 1];
            distances[0] = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                distances[index + 1] = distances[index] + Vector2.Distance(points[index], points[(index + 1) % points.Count]);
            }

            pathLength = distances[distances.Length - 1];
            return distances;
        }
    }
}
