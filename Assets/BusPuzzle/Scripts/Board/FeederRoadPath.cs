using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class FeederRoadPath
    {
        public FeederRoadPath(Vector2[] points)
        {
            Points = points;
            Distances = BuildPathDistances(points, out var length);
            Length = length;
        }

        public Vector2[] Points { get; }
        public float[] Distances { get; }
        public float Length { get; }

        public RotaryPathSample Sample(float progress)
        {
            return SampleByDistance(Mathf.Clamp01(progress) * Length);
        }

        public RotaryPathSample SampleByDistance(float distance)
        {
            if (Points.Length == 0)
            {
                return new RotaryPathSample(Vector2.zero, Vector2.right, Vector2.down);
            }

            if (Points.Length == 1)
            {
                return CreateSample(Points[0], Vector2.right);
            }

            distance = Mathf.Clamp(distance, 0f, Length);
            for (var index = 0; index < Points.Length - 1; index++)
            {
                if (distance > Distances[index + 1] && index + 1 < Points.Length - 1)
                {
                    continue;
                }

                var start = Points[index];
                var end = Points[index + 1];
                var segmentLength = Mathf.Max(0.0001f, Distances[index + 1] - Distances[index]);
                var t = Mathf.Clamp01((distance - Distances[index]) / segmentLength);
                var previous = Points[Mathf.Max(0, index - 1)];
                var afterNext = Points[Mathf.Min(Points.Length - 1, index + 2)];
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

            return CreateSample(Points[Points.Length - 1], (Points[Points.Length - 1] - Points[Points.Length - 2]).normalized);
        }

        private static RotaryPathSample CreateSample(Vector2 point, Vector2 tangent)
        {
            var outward = new Vector2(tangent.y, -tangent.x).normalized;
            return new RotaryPathSample(point, tangent, outward);
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

        private static float[] BuildPathDistances(IReadOnlyList<Vector2> points, out float pathLength)
        {
            var distances = new float[points.Count];
            pathLength = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                pathLength += Vector2.Distance(points[index - 1], points[index]);
                distances[index] = pathLength;
            }

            return distances;
        }
    }
}
