using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct PassengerUnitRoadPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        private PassengerUnitRoadPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public static PassengerUnitRoadPose FromPathSample(Vector3 position, RotaryPathSample sample)
        {
            var widthAxis = new Vector3(sample.Outward.x, 0f, sample.Outward.y);
            widthAxis = widthAxis.sqrMagnitude > 0.0001f ? widthAxis.normalized : Vector3.forward;

            // PassengerView places person 1 on local -Z and person 4 on local +Z.
            // This rotation keeps person 1 near the inner guardrail and person 4 near the opposite guardrail.
            return new PassengerUnitRoadPose(
                position,
                Quaternion.LookRotation(widthAxis, Vector3.up));
        }
    }

    internal readonly struct RotaryLayout
    {
        private readonly float passengerPivotOffset;

        public RotaryLayout(
            RoadPresetDefinition preset,
            int capacityUnits,
            int laneCount,
            int meshSampleCount,
            float laneSpacing,
            float roadShoulder,
            float passengerPivotOffset,
            float outerSpacingOffset,
            RotaryPath path,
            FeederRoadPath leftFeederPath,
            FeederRoadPath rightFeederPath)
        {
            Preset = preset;
            CapacityUnits = capacityUnits;
            LaneCount = laneCount;
            MeshSampleCount = meshSampleCount;
            LaneSpacing = laneSpacing;
            RoadShoulder = roadShoulder;
            this.passengerPivotOffset = passengerPivotOffset;
            OuterSpacingOffset = outerSpacingOffset;
            Path = path;
            LeftFeederPath = leftFeederPath;
            RightFeederPath = rightFeederPath;
        }

        public RoadPresetDefinition Preset { get; }
        public int CapacityUnits { get; }
        public int LaneCount { get; }
        public int MeshSampleCount { get; }
        public float LaneSpacing { get; }
        public float RoadShoulder { get; }
        public float OuterSpacingOffset { get; }
        public RotaryPath Path { get; }
        public FeederRoadPath LeftFeederPath { get; }
        public FeederRoadPath RightFeederPath { get; }
        public float PassengerSpeed => Preset.PassengerSpeed;
        public float RoadWidth => LaneCount * LaneSpacing + RoadShoulder * 2f;
        public float OuterRadiusX => Path.RadiusX + RoadWidth;
        public float OuterRadiusZ => Path.RadiusZ + RoadWidth;

        public static RotaryLayout Create(
            RoadPresetDefinition preset,
            int rotaryUnitCapacity,
            float passengerTangentialSlotSpacing,
            float passengerSetRoadWidth,
            float passengerSetPivotOffset,
            float passengerOuterSpacingOffset)
        {
            var capacity = Mathf.Clamp(rotaryUnitCapacity, LevelData.MinRotaryUnitCapacity, preset.MaxCapacityUnits);
            var targetPathLength = preset.MaxCapacityUnits * passengerTangentialSlotSpacing;
            var path = RoadPresetLibrary.CreatePath(preset, targetPathLength);
            var meshSampleCount = Mathf.Clamp(preset.MaxCapacityUnits * 6, 128, 256);
            var leftFeederPath = CreateFeederPath(path, preset, -1, passengerSetRoadWidth, passengerSetPivotOffset);
            var rightFeederPath = CreateFeederPath(path, preset, 1, passengerSetRoadWidth, passengerSetPivotOffset);

            return new RotaryLayout(
                preset,
                capacity,
                1,
                meshSampleCount,
                passengerSetRoadWidth,
                preset.RoadShoulder,
                passengerSetPivotOffset,
                passengerOuterSpacingOffset,
                path,
                leftFeederPath,
                rightFeederPath);
        }

        public Vector3 ToWorldPoint(Vector2 point, float centerZ, float y)
        {
            return new Vector3(point.x, y, centerZ + point.y);
        }

        public PassengerUnitRoadPose GetFeederPose(int side, int slotIndex, float centerZ, float y)
        {
            return GetFeederPoseByDistance(side, GetFeederDistanceForSlot(side, slotIndex), centerZ, y);
        }

        public PassengerUnitRoadPose GetFeederPoseByDistance(int side, float distance, float centerZ, float y)
        {
            var feederPath = GetFeederPath(side);
            var sample = feederPath.SampleByDistance(distance);
            var localPoint = sample.Point + sample.Outward * passengerPivotOffset;
            return PassengerUnitRoadPose.FromPathSample(ToWorldPoint(localPoint, centerZ, y), sample);
        }

        public float GetFeederDistanceForSlot(int side, int slotIndex)
        {
            var feederPath = GetFeederPath(side);
            var distanceFromJoin = (Mathf.Max(0, slotIndex) + 2) * Preset.FeederRowSpacing;
            return Mathf.Clamp(feederPath.Length - distanceFromJoin, 0f, feederPath.Length);
        }

        public FeederRoadPath GetFeederPath(int side)
        {
            return side < 0 ? LeftFeederPath : RightFeederPath;
        }

        public PassengerUnitRoadPose GetRotaryPose(float progress, float laneOffset, float centerZ, float y)
        {
            var sample = Path.Sample(progress);
            var localPoint = sample.Point + sample.Outward * laneOffset;
            return PassengerUnitRoadPose.FromPathSample(ToWorldPoint(localPoint, centerZ, y), sample);
        }

        public PassengerUnitRoadPose GetRotaryPoseByDistance(float routeDistance, float laneOffset, float centerZ, float y)
        {
            var sample = Path.SampleByDistance(routeDistance);
            var localPoint = sample.Point + sample.Outward * laneOffset;
            return PassengerUnitRoadPose.FromPathSample(ToWorldPoint(localPoint, centerZ, y), sample);
        }

        private static FeederRoadPath CreateFeederPath(
            RotaryPath rotaryPath,
            RoadPresetDefinition preset,
            int side,
            float passengerSetRoadWidth,
            float passengerSetPivotOffset)
        {
            var progress = side < 0 ? preset.LeftFeederProgress : preset.RightFeederProgress;
            var joinSample = rotaryPath.Sample(progress);
            var roadWidth = passengerSetRoadWidth + preset.RoadShoulder * 2f;
            var outerRoadOffset = roadWidth - passengerSetPivotOffset;
            var joinPoint = joinSample.Point + joinSample.Outward * Mathf.Max(0.05f, outerRoadOffset - 0.015f);
            var laneLength = Mathf.Max(1.05f, preset.FeederRowsPerStack * preset.FeederRowSpacing + passengerSetRoadWidth);
            var start = joinPoint + joinSample.Outward * laneLength;
            var mid = joinPoint + joinSample.Outward * (laneLength * 0.48f);

            return new FeederRoadPath(new[]
            {
                start,
                mid,
                joinPoint
            });
        }
    }
}
