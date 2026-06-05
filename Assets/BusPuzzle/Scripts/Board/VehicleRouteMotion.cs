using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct VehicleSmoothRoute
    {
        public readonly List<Vector3> Points;
        public readonly List<float> Distances;
        public readonly float Length;
        public readonly Quaternion FinalRotation;

        public VehicleSmoothRoute(List<Vector3> points, List<float> distances, float length, Quaternion finalRotation)
        {
            Points = points;
            Distances = distances;
            Length = length;
            FinalRotation = finalRotation;
        }
    }

    internal static class VehicleRouteMotion
    {
        private const float CornerRadiusFactor = 0.88f;
        private const float CornerMaxSegmentRatio = 0.46f;
        private const float PointMergeDistance = 0.015f;
        private const float SampleSpacingFactor = 0.10f;
        private const float LookAheadFactor = 0.64f;
        private const float FinalRotationBlendStart = 0.88f;

        public static VehicleSmoothRoute Build(BusRouteStep[] route, Vector3 startPosition, Quaternion fallbackRotation, float cellSize)
        {
            var finalRotation = route != null && route.Length > 0
                ? route[route.Length - 1].Rotation
                : fallbackRotation;
            var rawPoints = new List<Vector3>();
            AppendRoutePoint(rawPoints, startPosition);

            if (route != null)
            {
                for (var index = 0; index < route.Length; index++)
                {
                    AppendRoutePoint(rawPoints, route[index].Position);
                }
            }

            if (rawPoints.Count <= 1)
            {
                return BuildRouteFromPoints(rawPoints, finalRotation);
            }

            var samples = new List<Vector3> { rawPoints[0] };
            var sampleSpacing = Mathf.Max(0.018f, cellSize * SampleSpacingFactor);
            var cornerRadius = Mathf.Max(0.02f, cellSize * CornerRadiusFactor);

            for (var index = 1; index < rawPoints.Count - 1; index++)
            {
                var previous = rawPoints[index - 1];
                var corner = rawPoints[index];
                var next = rawPoints[index + 1];
                var incoming = corner - previous;
                var outgoing = next - corner;
                incoming.y = 0f;
                outgoing.y = 0f;

                var incomingLength = incoming.magnitude;
                var outgoingLength = outgoing.magnitude;
                if (incomingLength < PointMergeDistance || outgoingLength < PointMergeDistance)
                {
                    AddLineSamples(samples, corner, sampleSpacing);
                    continue;
                }

                var incomingDirection = incoming / incomingLength;
                var outgoingDirection = outgoing / outgoingLength;
                var angle = Vector3.Angle(incomingDirection, outgoingDirection);
                if (angle < 8f || angle > 172f)
                {
                    AddLineSamples(samples, corner, sampleSpacing);
                    continue;
                }

                var radius = Mathf.Min(cornerRadius, incomingLength * CornerMaxSegmentRatio, outgoingLength * CornerMaxSegmentRatio);
                var turnEntry = corner - incomingDirection * radius;
                var turnExit = corner + outgoingDirection * radius;
                turnEntry.y = corner.y;
                turnExit.y = corner.y;

                AddLineSamples(samples, turnEntry, sampleSpacing);
                AddQuadraticSamples(samples, turnEntry, corner, turnExit, sampleSpacing);
            }

            AddLineSamples(samples, rawPoints[rawPoints.Count - 1], sampleSpacing);
            return BuildRouteFromPoints(samples, finalRotation);
        }

        public static Vector3 EvaluatePosition(VehicleSmoothRoute route, float distance)
        {
            if (route.Points.Count == 0)
            {
                return Vector3.zero;
            }

            if (distance <= 0f)
            {
                return route.Points[0];
            }

            if (distance >= route.Length)
            {
                return route.Points[route.Points.Count - 1];
            }

            for (var index = 1; index < route.Points.Count; index++)
            {
                var segmentEndDistance = route.Distances[index];
                if (distance > segmentEndDistance)
                {
                    continue;
                }

                var segmentStartDistance = route.Distances[index - 1];
                var segmentLength = segmentEndDistance - segmentStartDistance;
                var t = segmentLength <= 0.001f ? 1f : (distance - segmentStartDistance) / segmentLength;
                return Vector3.Lerp(route.Points[index - 1], route.Points[index], t);
            }

            return route.Points[route.Points.Count - 1];
        }

        public static Quaternion EvaluateRotation(
            VehicleSmoothRoute route,
            float distance,
            float progress,
            Quaternion fallbackRotation,
            float cellSize)
        {
            var lookAhead = Mathf.Max(0.035f, cellSize * LookAheadFactor);
            var position = EvaluatePosition(route, distance);
            var forwardPosition = EvaluatePosition(route, Mathf.Min(route.Length, distance + lookAhead));
            var tangent = forwardPosition - position;
            tangent.y = 0f;

            if (tangent.sqrMagnitude < 0.0001f)
            {
                var backPosition = EvaluatePosition(route, Mathf.Max(0f, distance - lookAhead));
                tangent = position - backPosition;
                tangent.y = 0f;
            }

            var rotation = tangent.sqrMagnitude < 0.0001f
                ? fallbackRotation
                : Quaternion.LookRotation(tangent.normalized, Vector3.up);

            if (progress <= FinalRotationBlendStart)
            {
                return rotation;
            }

            var blend = Mathf.InverseLerp(FinalRotationBlendStart, 1f, progress);
            blend = Mathf.SmoothStep(0f, 1f, blend);
            return Quaternion.Slerp(rotation, route.FinalRotation, blend);
        }

        public static float EaseDriveProgress(float t)
        {
            return 0.5f - Mathf.Cos(t * Mathf.PI) * 0.5f;
        }

        private static void AppendRoutePoint(List<Vector3> points, Vector3 point)
        {
            if (points.Count == 0)
            {
                points.Add(point);
                return;
            }

            if (Vector3.Distance(points[points.Count - 1], point) >= PointMergeDistance)
            {
                points.Add(point);
            }
        }

        private static void AddLineSamples(List<Vector3> points, Vector3 target, float sampleSpacing)
        {
            if (points.Count == 0)
            {
                points.Add(target);
                return;
            }

            var start = points[points.Count - 1];
            var distance = Vector3.Distance(start, target);
            if (distance < PointMergeDistance)
            {
                points[points.Count - 1] = target;
                return;
            }

            var steps = Mathf.Max(1, Mathf.CeilToInt(distance / sampleSpacing));
            for (var step = 1; step <= steps; step++)
            {
                points.Add(Vector3.Lerp(start, target, step / (float)steps));
            }
        }

        private static void AddQuadraticSamples(List<Vector3> points, Vector3 start, Vector3 control, Vector3 end, float sampleSpacing)
        {
            var estimatedLength = Vector3.Distance(start, control) + Vector3.Distance(control, end);
            var steps = Mathf.Max(5, Mathf.CeilToInt(estimatedLength / sampleSpacing));

            for (var step = 1; step <= steps; step++)
            {
                var t = step / (float)steps;
                var oneMinusT = 1f - t;
                var point =
                    oneMinusT * oneMinusT * start +
                    2f * oneMinusT * t * control +
                    t * t * end;
                AppendRoutePoint(points, point);
            }
        }

        private static VehicleSmoothRoute BuildRouteFromPoints(List<Vector3> points, Quaternion finalRotation)
        {
            var distances = new List<float>(points.Count);
            var length = 0f;

            for (var index = 0; index < points.Count; index++)
            {
                if (index > 0)
                {
                    length += Vector3.Distance(points[index - 1], points[index]);
                }

                distances.Add(length);
            }

            return new VehicleSmoothRoute(points, distances, length, finalRotation);
        }
    }
}
