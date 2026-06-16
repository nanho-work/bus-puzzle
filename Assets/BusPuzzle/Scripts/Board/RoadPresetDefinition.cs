using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public readonly struct RoadPresetDefinition
    {
        private const float PassengerSpeedMultiplier = 1.70f;

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
            PassengerSpeed = Mathf.Max(0.001f, passengerSpeed) * PassengerSpeedMultiplier;
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
                        40,
                        0.0525f,
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
                        0.022f,
                        0f,
                        0.73f,
                        0.27f,
                        0.14f,
                        10);

                case RotaryRoadPresetId.Medium:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.Medium,
                        40,
                        0.048f,
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
                        0.024f,
                        0f,
                        0.72f,
                        0.28f,
                        0.14f,
                        11);

                case RotaryRoadPresetId.CompactOval:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.CompactOval,
                        40,
                        0.0505f,
                        new Vector2(0f, -0.72f),
                        new Vector2(0.54f, -0.72f),
                        new Vector2(0.82f, 0.46f),
                        new Vector2(-0.82f, 0.46f),
                        new Vector2(-0.54f, -0.72f),
                        new Vector2(1.12f, -0.22f),
                        new Vector2(0f, 0.72f),
                        new Vector2(-1.12f, -0.22f),
                        18,
                        48,
                        58,
                        0.024f,
                        0f,
                        0.75f,
                        0.25f,
                        0.14f,
                        11);

                case RotaryRoadPresetId.WideTerminal:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.WideTerminal,
                        40,
                        0.0470f,
                        new Vector2(0f, -0.70f),
                        new Vector2(0.86f, -0.70f),
                        new Vector2(1.12f, 0.40f),
                        new Vector2(-1.12f, 0.40f),
                        new Vector2(-0.86f, -0.70f),
                        new Vector2(1.58f, -0.18f),
                        new Vector2(0f, 0.78f),
                        new Vector2(-1.58f, -0.18f),
                        22,
                        48,
                        70,
                        0.025f,
                        0f,
                        0.67f,
                        0.33f,
                        0.14f,
                        12);

                case RotaryRoadPresetId.TallTerminal:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.TallTerminal,
                        40,
                        0.0455f,
                        new Vector2(0f, -0.88f),
                        new Vector2(0.46f, -0.88f),
                        new Vector2(0.70f, 0.96f),
                        new Vector2(-0.70f, 0.96f),
                        new Vector2(-0.46f, -0.88f),
                        new Vector2(1.10f, 0.08f),
                        new Vector2(0f, 1.52f),
                        new Vector2(-1.10f, 0.08f),
                        16,
                        64,
                        58,
                        0.026f,
                        0f,
                        0.78f,
                        0.22f,
                        0.14f,
                        12);

                case RotaryRoadPresetId.LeftHook:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.LeftHook,
                        40,
                        0.0475f,
                        new Vector2(0f, -0.78f),
                        new Vector2(0.50f, -0.76f),
                        new Vector2(0.72f, 0.56f),
                        new Vector2(-1.02f, 0.66f),
                        new Vector2(-0.76f, -0.70f),
                        new Vector2(1.12f, -0.10f),
                        new Vector2(-0.22f, 1.12f),
                        new Vector2(-1.52f, -0.04f),
                        18,
                        52,
                        68,
                        0.025f,
                        0f,
                        0.76f,
                        0.31f,
                        0.14f,
                        12);

                case RotaryRoadPresetId.RightHook:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.RightHook,
                        40,
                        0.0475f,
                        new Vector2(0f, -0.78f),
                        new Vector2(0.76f, -0.70f),
                        new Vector2(1.02f, 0.66f),
                        new Vector2(-0.72f, 0.56f),
                        new Vector2(-0.50f, -0.76f),
                        new Vector2(1.52f, -0.04f),
                        new Vector2(0.22f, 1.12f),
                        new Vector2(-1.12f, -0.10f),
                        18,
                        52,
                        68,
                        0.025f,
                        0f,
                        0.69f,
                        0.24f,
                        0.14f,
                        12);

                case RotaryRoadPresetId.Roundabout:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.Roundabout,
                        40,
                        0.0485f,
                        new Vector2(0f, -0.76f),
                        new Vector2(0.62f, -0.76f),
                        new Vector2(0.92f, 0.54f),
                        new Vector2(-0.92f, 0.54f),
                        new Vector2(-0.62f, -0.76f),
                        new Vector2(1.34f, -0.18f),
                        new Vector2(0f, 0.92f),
                        new Vector2(-1.34f, -0.18f),
                        20,
                        54,
                        64,
                        0.024f,
                        0f,
                        0.70f,
                        0.30f,
                        0.14f,
                        12);

                case RotaryRoadPresetId.SnakeTest:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.SnakeTest,
                        40,
                        0.0465f,
                        new Vector2(0f, -1.10f),
                        new Vector2(0.70f, -1.02f),
                        new Vector2(0.80f, 0.78f),
                        new Vector2(-0.80f, 0.78f),
                        new Vector2(-0.70f, -1.02f),
                        new Vector2(1.18f, -0.54f),
                        new Vector2(0f, 1.18f),
                        new Vector2(-1.18f, -0.54f),
                        18,
                        54,
                        64,
                        0.026f,
                        0f,
                        0.61f,
                        0.35f,
                        0.14f,
                        12);

                case RotaryRoadPresetId.HeartTest:
                    return CreateShapePreset(RotaryRoadPresetId.HeartTest, 40, 0.66f, 0.34f);

                case RotaryRoadPresetId.SmallCircleTest:
                    return CreateShapePreset(RotaryRoadPresetId.SmallCircleTest, 32, 0.66f, 0.34f);

                case RotaryRoadPresetId.LargeCircleTest:
                    return CreateShapePreset(RotaryRoadPresetId.LargeCircleTest, 40, 0.66f, 0.34f);

                case RotaryRoadPresetId.OvalTest:
                    return CreateShapePreset(RotaryRoadPresetId.OvalTest, 40, 0.66f, 0.34f);

                case RotaryRoadPresetId.RoundedSquareTest:
                    return CreateShapePreset(RotaryRoadPresetId.RoundedSquareTest, 40, 0.66f, 0.34f);

                case RotaryRoadPresetId.CloverTest:
                    return CreateShapePreset(RotaryRoadPresetId.CloverTest, 40, 0.63f, 0.37f);

                case RotaryRoadPresetId.DropTest:
                    return CreateShapePreset(RotaryRoadPresetId.DropTest, 40, 0.66f, 0.34f);

                case RotaryRoadPresetId.CloudTest:
                    return CreateShapePreset(RotaryRoadPresetId.CloudTest, 40, 0.64f, 0.36f);

                case RotaryRoadPresetId.LoopTest:
                    return CreateShapePreset(RotaryRoadPresetId.LoopTest, 40, 0.64f, 0.36f);

                case RotaryRoadPresetId.ArrowTest:
                    return CreateShapePreset(RotaryRoadPresetId.ArrowTest, 40, 0.62f, 0.38f);

                case RotaryRoadPresetId.RibbonTest:
                    return CreateShapePreset(RotaryRoadPresetId.RibbonTest, 40, 0.64f, 0.36f);

                default:
                    return new RoadPresetDefinition(
                        RotaryRoadPresetId.Large,
                        40,
                        0.0435f,
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
                        0.026f,
                        0f,
                        0.71f,
                        0.29f,
                        0.14f,
                        12);
            }
        }

        public static RotaryPath CreatePath(RoadPresetDefinition preset, float targetPathLength)
        {
            if (preset.Id == RotaryRoadPresetId.SnakeTest)
            {
                return CreateSnakeTestPath(targetPathLength);
            }

            if (TryCreateShapePath(preset.Id, targetPathLength, out var shapePath))
            {
                return shapePath;
            }

            var basePoints = new List<Vector2> { preset.Start };
            AddLinePoints(basePoints, preset.Start, preset.RightBottom, preset.BottomSegments);
            AddQuadraticPoints(basePoints, preset.RightBottom, preset.RightControl, preset.RightTop, preset.SideSegments);
            AddQuadraticPoints(basePoints, preset.RightTop, preset.TopControl, preset.LeftTop, preset.TopSegments);
            AddQuadraticPoints(basePoints, preset.LeftTop, preset.LeftControl, preset.LeftBottom, preset.SideSegments);
            AddLinePoints(basePoints, preset.LeftBottom, preset.Start, preset.BottomSegments);
            basePoints.RemoveAt(basePoints.Count - 1);

            return CreateScaledPath(basePoints, targetPathLength);
        }

        private static RoadPresetDefinition CreateShapePreset(
            RotaryRoadPresetId id,
            int maxCapacityUnits,
            float leftFeederProgress,
            float rightFeederProgress)
        {
            return new RoadPresetDefinition(
                id,
                maxCapacityUnits,
                0.0465f,
                new Vector2(0f, -1.00f),
                new Vector2(0.68f, -0.82f),
                new Vector2(0.88f, 0.68f),
                new Vector2(-0.88f, 0.68f),
                new Vector2(-0.68f, -0.82f),
                new Vector2(1.24f, -0.20f),
                new Vector2(0f, 1.10f),
                new Vector2(-1.24f, -0.20f),
                18,
                56,
                58,
                0.026f,
                0f,
                leftFeederProgress,
                rightFeederProgress,
                0.14f,
                12);
        }

        private static bool TryCreateShapePath(RotaryRoadPresetId id, float targetPathLength, out RotaryPath path)
        {
            switch (id)
            {
                case RotaryRoadPresetId.HeartTest:
                    path = CreateHeartTestPath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.SmallCircleTest:
                case RotaryRoadPresetId.LargeCircleTest:
                    path = CreateCirclePath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.OvalTest:
                    path = CreateOvalPath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.RoundedSquareTest:
                    path = CreateRoundedSquarePath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.CloverTest:
                    path = CreateCloverPath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.DropTest:
                    path = CreateDropPath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.CloudTest:
                    path = CreateCloudPath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.LoopTest:
                    path = CreateLoopPath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.ArrowTest:
                    path = CreateArrowPath(targetPathLength);
                    return true;
                case RotaryRoadPresetId.RibbonTest:
                    path = CreateRibbonPath(targetPathLength);
                    return true;
                default:
                    path = null;
                    return false;
            }
        }

        private static RotaryPath CreateSnakeTestPath(float targetPathLength)
        {
            var basePoints = new List<Vector2>
            {
                new Vector2(0.00f, -1.10f),
                new Vector2(0.95f, -0.90f),
                new Vector2(0.95f, -0.45f),
                new Vector2(-0.95f, -0.20f),
                new Vector2(-0.95f, 0.30f),
                new Vector2(0.95f, 0.55f),
                new Vector2(0.95f, 1.00f),
                new Vector2(0.00f, 1.15f),
                new Vector2(-1.20f, 1.05f),
                new Vector2(-1.20f, -1.05f)
            };

            return CreateScaledPath(basePoints, targetPathLength);
        }

        private static RotaryPath CreateHeartTestPath(float targetPathLength)
        {
            var basePoints = new List<Vector2>
            {
                new Vector2(0.00f, -1.18f),
                new Vector2(0.48f, -0.90f),
                new Vector2(0.92f, -0.46f),
                new Vector2(1.12f, 0.14f),
                new Vector2(1.02f, 0.58f),
                new Vector2(0.76f, 0.84f),
                new Vector2(0.48f, 0.92f),
                new Vector2(0.24f, 0.82f),
                new Vector2(0.08f, 0.72f),
                new Vector2(0.00f, 0.70f),
                new Vector2(-0.08f, 0.72f),
                new Vector2(-0.24f, 0.82f),
                new Vector2(-0.48f, 0.92f),
                new Vector2(-0.76f, 0.84f),
                new Vector2(-1.02f, 0.58f),
                new Vector2(-1.12f, 0.14f),
                new Vector2(-0.92f, -0.46f),
                new Vector2(-0.48f, -0.90f)
            };

            return CreateScaledPath(basePoints, targetPathLength);
        }

        private static RotaryPath CreateCirclePath(float targetPathLength)
        {
            return CreateScaledPath(CreateRadialPoints(48, angle => 1f), targetPathLength);
        }

        private static RotaryPath CreateOvalPath(float targetPathLength)
        {
            return CreateParametricPath(56, angle => new Vector2(Mathf.Sin(angle) * 1.30f, -Mathf.Cos(angle) * 0.88f), targetPathLength);
        }

        private static RotaryPath CreateRoundedSquarePath(float targetPathLength)
        {
            var basePoints = new List<Vector2>
            {
                new Vector2(0.00f, -1.04f),
                new Vector2(0.72f, -1.02f),
                new Vector2(1.04f, -0.72f),
                new Vector2(1.04f, 0.54f),
                new Vector2(0.72f, 0.86f),
                new Vector2(0.00f, 0.92f),
                new Vector2(-0.72f, 0.86f),
                new Vector2(-1.04f, 0.54f),
                new Vector2(-1.04f, -0.72f),
                new Vector2(-0.72f, -1.02f)
            };

            return CreateScaledPath(basePoints, targetPathLength);
        }

        private static RotaryPath CreateCloverPath(float targetPathLength)
        {
            return CreateParametricPath(
                72,
                angle =>
                {
                    var radius = 0.90f + 0.22f * Mathf.Cos(4f * angle);
                    return new Vector2(Mathf.Sin(angle) * radius, -Mathf.Cos(angle) * radius);
                },
                targetPathLength);
        }

        private static RotaryPath CreateDropPath(float targetPathLength)
        {
            var basePoints = new List<Vector2>
            {
                new Vector2(0.00f, -1.18f),
                new Vector2(0.42f, -0.88f),
                new Vector2(0.86f, -0.30f),
                new Vector2(0.82f, 0.34f),
                new Vector2(0.46f, 0.82f),
                new Vector2(0.00f, 1.02f),
                new Vector2(-0.46f, 0.82f),
                new Vector2(-0.82f, 0.34f),
                new Vector2(-0.86f, -0.30f),
                new Vector2(-0.42f, -0.88f)
            };

            return CreateScaledPath(basePoints, targetPathLength);
        }

        private static RotaryPath CreateCloudPath(float targetPathLength)
        {
            return CreateParametricPath(
                72,
                angle =>
                {
                    var radius =
                        0.96f +
                        0.12f * Mathf.Cos(5f * angle) +
                        0.05f * Mathf.Cos(2f * angle + 0.40f);
                    return new Vector2(Mathf.Sin(angle) * radius * 1.06f, -Mathf.Cos(angle) * radius * 0.92f);
                },
                targetPathLength);
        }

        private static RotaryPath CreateLoopPath(float targetPathLength)
        {
            return CreateParametricPath(
                72,
                angle =>
                {
                    var x = Mathf.Sin(angle) * (1.02f + 0.16f * Mathf.Cos(2f * angle));
                    var y = -Mathf.Cos(angle) * (0.92f - 0.18f * Mathf.Cos(2f * angle));
                    return new Vector2(x, y);
                },
                targetPathLength);
        }

        private static RotaryPath CreateArrowPath(float targetPathLength)
        {
            var basePoints = new List<Vector2>
            {
                new Vector2(0.00f, -1.15f),
                new Vector2(0.72f, -0.82f),
                new Vector2(0.72f, -0.16f),
                new Vector2(1.08f, -0.16f),
                new Vector2(0.00f, 1.06f),
                new Vector2(-1.08f, -0.16f),
                new Vector2(-0.72f, -0.16f),
                new Vector2(-0.72f, -0.82f)
            };

            return CreateScaledPath(basePoints, targetPathLength);
        }

        private static RotaryPath CreateRibbonPath(float targetPathLength)
        {
            var basePoints = new List<Vector2>
            {
                new Vector2(0.00f, -1.06f),
                new Vector2(0.62f, -0.88f),
                new Vector2(1.06f, -0.40f),
                new Vector2(0.88f, 0.18f),
                new Vector2(0.36f, 0.38f),
                new Vector2(0.96f, 0.68f),
                new Vector2(0.78f, 1.06f),
                new Vector2(0.18f, 0.92f),
                new Vector2(0.00f, 0.56f),
                new Vector2(-0.18f, 0.92f),
                new Vector2(-0.78f, 1.06f),
                new Vector2(-0.96f, 0.68f),
                new Vector2(-0.36f, 0.38f),
                new Vector2(-0.88f, 0.18f),
                new Vector2(-1.06f, -0.40f),
                new Vector2(-0.62f, -0.88f)
            };

            return CreateScaledPath(basePoints, targetPathLength);
        }

        private static List<Vector2> CreateRadialPoints(int sampleCount, System.Func<float, float> radiusAtAngle)
        {
            var points = new List<Vector2>(sampleCount);
            for (var index = 0; index < sampleCount; index++)
            {
                var angle = index / (float)sampleCount * Mathf.PI * 2f;
                var radius = Mathf.Max(0.05f, radiusAtAngle(angle));
                points.Add(new Vector2(Mathf.Sin(angle) * radius, -Mathf.Cos(angle) * radius));
            }

            return points;
        }

        private static RotaryPath CreateParametricPath(int sampleCount, System.Func<float, Vector2> pointAtAngle, float targetPathLength)
        {
            var points = new List<Vector2>(sampleCount);
            for (var index = 0; index < sampleCount; index++)
            {
                var angle = index / (float)sampleCount * Mathf.PI * 2f;
                points.Add(pointAtAngle(angle));
            }

            return CreateScaledPath(points, targetPathLength);
        }

        private static RotaryPath CreateScaledPath(IReadOnlyList<Vector2> basePoints, float targetPathLength)
        {
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
