using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct PassengerUnitRoadPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly bool HasCustomPersonLocalPositions;
        public readonly Vector3 Person1LocalPosition;
        public readonly Vector3 Person2LocalPosition;
        public readonly Vector3 Person3LocalPosition;
        public readonly Vector3 Person4LocalPosition;

        private PassengerUnitRoadPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            HasCustomPersonLocalPositions = false;
            Person1LocalPosition = Vector3.zero;
            Person2LocalPosition = Vector3.zero;
            Person3LocalPosition = Vector3.zero;
            Person4LocalPosition = Vector3.zero;
        }

        private PassengerUnitRoadPose(
            Vector3 position,
            Quaternion rotation,
            Vector3 person1LocalPosition,
            Vector3 person2LocalPosition,
            Vector3 person3LocalPosition,
            Vector3 person4LocalPosition)
        {
            Position = position;
            Rotation = rotation;
            HasCustomPersonLocalPositions = true;
            Person1LocalPosition = person1LocalPosition;
            Person2LocalPosition = person2LocalPosition;
            Person3LocalPosition = person3LocalPosition;
            Person4LocalPosition = person4LocalPosition;
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

        public static PassengerUnitRoadPose FromPersonWorldPositions(
            Vector3 person1Position,
            Vector3 person2Position,
            Vector3 person3Position,
            Vector3 person4Position)
        {
            var position = (person1Position + person2Position + person3Position + person4Position) * 0.25f;
            var widthAxis = person4Position - person1Position;
            widthAxis.y = 0f;
            widthAxis = widthAxis.sqrMagnitude > 0.0001f ? widthAxis.normalized : Vector3.forward;
            var rotation = Quaternion.LookRotation(widthAxis, Vector3.up);
            var inverseRotation = Quaternion.Inverse(rotation);

            return new PassengerUnitRoadPose(
                position,
                rotation,
                inverseRotation * (person1Position - position),
                inverseRotation * (person2Position - position),
                inverseRotation * (person3Position - position),
                inverseRotation * (person4Position - position));
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
            Vector4 passengerPersonLocalZ,
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
            PassengerPersonLocalZ = passengerPersonLocalZ;
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
        public float PassengerPivotOffset => passengerPivotOffset;
        public Vector4 PassengerPersonLocalZ { get; }
        public float OuterSpacingOffset => passengerPivotOffset + PassengerPersonLocalZ.w;
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
            Vector4 passengerPersonLocalZ)
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
                passengerPersonLocalZ,
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

        public float GetPersonLaneOffset(int personIndex)
        {
            return passengerPivotOffset + GetPersonLocalZ(personIndex);
        }

        private float GetPersonLocalZ(int personIndex)
        {
            switch (personIndex)
            {
                case 0:
                    return PassengerPersonLocalZ.x;
                case 1:
                    return PassengerPersonLocalZ.y;
                case 2:
                    return PassengerPersonLocalZ.z;
                default:
                    return PassengerPersonLocalZ.w;
            }
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
