using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public readonly struct RoadPresetDefinition
    {
        public readonly RotaryRoadPresetId Id;
        public readonly int MaxCapacityUnits;
        public readonly float PassengerSpeed;
        public readonly Vector2 Start;
        public readonly Vector2 RightBottom;
        public readonly Vector2 RightTop;
        public readonly Vector2 LeftTop;
        public readonly Vector2 LeftBottom;
        public readonly Vector2 RightControl;
        public readonly Vector2 TopControl;
        public readonly Vector2 LeftControl;
        public readonly int BottomSegments;
        public readonly int SideSegments;
        public readonly int TopSegments;
        public readonly float RoadShoulder;
        public readonly float StationConnectProgress;
        public readonly float LeftFeederProgress;
        public readonly float RightFeederProgress;
        public readonly float FeederRowSpacing;
        public readonly int FeederRowsPerStack;
        public float BoardingGateProgress => StationConnectProgress;

        public RoadPresetDefinition(
            RotaryRoadPresetId id,
            int maxCapacityUnits,
            float passengerSpeed,
            Vector2 start,
            Vector2 rightBottom,
            Vector2 rightTop,
            Vector2 leftTop,
            Vector2 leftBottom,
            Vector2 rightControl,
            Vector2 topControl,
            Vector2 leftControl,
            int bottomSegments,
            int sideSegments,
            int topSegments,
            float roadShoulder,
            float stationConnectProgress,
            float leftFeederProgress,
            float rightFeederProgress,
            float feederRowSpacing,
            int feederRowsPerStack)
        {
            Id = id;
            MaxCapacityUnits = maxCapacityUnits;
            PassengerSpeed = passengerSpeed;
            Start = start;
            RightBottom = rightBottom;
            RightTop = rightTop;
            LeftTop = leftTop;
            LeftBottom = leftBottom;
            RightControl = rightControl;
            TopControl = topControl;
            LeftControl = leftControl;
            BottomSegments = bottomSegments;
            SideSegments = sideSegments;
            TopSegments = topSegments;
            RoadShoulder = roadShoulder;
            StationConnectProgress = stationConnectProgress;
            LeftFeederProgress = leftFeederProgress;
            RightFeederProgress = rightFeederProgress;
            FeederRowSpacing = feederRowSpacing;
            FeederRowsPerStack = feederRowsPerStack;
        }
    }

    internal static class RoadPresetLibrary
    {
        public static RoadPresetDefinition Get(RotaryRoadPresetId roadPresetId)
        {
            switch (roadPresetId)
            {
                case RotaryRoadPresetId.Small:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.Small,
                        35,
                        0.035f,
                        new Vector2(0f, -0.70f),
                        new Vector2(0.42f, -0.70f),
                        new Vector2(0.70f, 0.48f),
                        new Vector2(-0.70f, 0.48f),
                        new Vector2(-0.42f, -0.70f),
                        new Vector2(1.02f, -0.10f),
                        new Vector2(0f, 0.84f),
                        new Vector2(-1.02f, -0.10f),
                        16,
                        44,
                        52,
                        0.035f,
                        0f,
                        0.73f,
                        0.27f,
                        0.18f,
                        10);

                case RotaryRoadPresetId.Medium:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.Medium,
                        42,
                        0.032f,
                        new Vector2(0f, -0.76f),
                        new Vector2(0.55f, -0.76f),
                        new Vector2(0.80f, 0.58f),
                        new Vector2(-0.80f, 0.58f),
                        new Vector2(-0.55f, -0.76f),
                        new Vector2(1.18f, -0.08f),
                        new Vector2(0f, 1.00f),
                        new Vector2(-1.18f, -0.08f),
                        18,
                        50,
                        58,
                        0.040f,
                        0f,
                        0.72f,
                        0.28f,
                        0.18f,
                        11);

                default:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.Large,
                        LevelData.MaxRotaryUnitCapacity,
                        0.029f,
                        new Vector2(0f, -0.82f),
                        new Vector2(0.66f, -0.82f),
                        new Vector2(0.88f, 0.72f),
                        new Vector2(-0.88f, 0.72f),
                        new Vector2(-0.66f, -0.82f),
                        new Vector2(1.38f, -0.10f),
                        new Vector2(0f, 1.22f),
                        new Vector2(-1.38f, -0.10f),
                        20,
                        56,
                        66,
                        0.045f,
                        0f,
                        0.71f,
                        0.29f,
                        0.18f,
                        12);
            }
        }

        public static RotaryPath CreatePath(RoadPresetDefinition preset, float targetPathLength)
        {
            var basePoints = new List<Vector2> { preset.Start };
            AddLinePoints(basePoints, preset.Start, preset.RightBottom, preset.BottomSegments);
            AddQuadraticPoints(basePoints, preset.RightBottom, preset.RightControl, preset.RightTop, preset.SideSegments);
            AddQuadraticPoints(basePoints, preset.RightTop, preset.TopControl, preset.LeftTop, preset.TopSegments);
            AddQuadraticPoints(basePoints, preset.LeftTop, preset.LeftControl, preset.LeftBottom, preset.SideSegments);
            AddLinePoints(basePoints, preset.LeftBottom, preset.Start, preset.BottomSegments);
            basePoints.RemoveAt(basePoints.Count - 1);

            var baseLength = CalculateClosedPathLength(basePoints);
            var scale = targetPathLength / Mathf.Max(0.01f, baseLength);
            var points = new Vector2[basePoints.Count];
            for (var index = 0; index < basePoints.Count; index++)
            {
                points[index] = basePoints[index] * scale;
            }

            return new RotaryPath(points);
        }

        private static void AddLinePoints(List<Vector2> points, Vector2 start, Vector2 end, int segments)
        {
            for (var index = 1; index <= segments; index++)
            {
                var t = index / (float)segments;
                points.Add(Vector2.Lerp(start, end, t));
            }
        }

        private static void AddQuadraticPoints(List<Vector2> points, Vector2 start, Vector2 control, Vector2 end, int segments)
        {
            for (var index = 1; index <= segments; index++)
            {
                var t = index / (float)segments;
                points.Add(Quadratic(start, control, end, t));
            }
        }

        private static Vector2 Quadratic(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            var a = Vector2.Lerp(start, control, t);
            var b = Vector2.Lerp(control, end, t);
            return Vector2.Lerp(a, b, t);
        }

        private static float CalculateClosedPathLength(IReadOnlyList<Vector2> points)
        {
            var length = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                length += Vector2.Distance(points[index], points[(index + 1) % points.Count]);
            }

            return length;
        }
    }
}
