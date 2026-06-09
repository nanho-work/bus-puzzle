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

        public static PassengerUnitRoadPose FromPathSample(Vector3 position, RotaryPathSample sample, Vector4 personLocalOffsets)
        {
            var forwardAxis = ToWorldDirection(sample.Tangent, Vector3.forward);

            // Local X follows the road width, so 1/4 stay on the guardrail sides while the unit faces forward.
            return new PassengerUnitRoadPose(
                position,
                Quaternion.LookRotation(forwardAxis, Vector3.up),
                new Vector3(personLocalOffsets.x, 0f, 0f),
                new Vector3(personLocalOffsets.y, 0f, 0f),
                new Vector3(personLocalOffsets.z, 0f, 0f),
                new Vector3(personLocalOffsets.w, 0f, 0f));
        }

        public static PassengerUnitRoadPose FromPersonWorldPositions(
            Vector3 person1Position,
            Vector3 person2Position,
            Vector3 person3Position,
            Vector3 person4Position,
            Vector3 forwardDirection)
        {
            var position = (person1Position + person2Position + person3Position + person4Position) * 0.25f;
            var rotation = Quaternion.LookRotation(NormalizeFlat(forwardDirection, Vector3.forward), Vector3.up);
            var inverseRotation = Quaternion.Inverse(rotation);

            return new PassengerUnitRoadPose(
                position,
                rotation,
                inverseRotation * (person1Position - position),
                inverseRotation * (person2Position - position),
                inverseRotation * (person3Position - position),
                inverseRotation * (person4Position - position));
        }

        private static Vector3 ToWorldDirection(Vector2 direction, Vector3 fallback)
        {
            return NormalizeFlat(new Vector3(direction.x, 0f, direction.y), fallback);
        }

        private static Vector3 NormalizeFlat(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }
    }

    internal readonly struct RotaryLayout
    {
        private const float FeederMergeClearanceRows = 2.35f;
        private const float FeederQueueSpacingMultiplier = 1.17f;
        private const float FeederStartPaddingRows = 0.35f;
        private const float FeederHiddenTailRows = 14f;

        public RotaryLayout(
            RoadPresetDefinition preset,
            int capacityUnits,
            int laneCount,
            int meshSampleCount,
            PassengerRoadProfile roadProfile,
            RotaryPath path,
            FeederRoadPath leftFeederPath,
            FeederRoadPath rightFeederPath,
            float visibleFeederTopY)
        {
            Preset = preset;
            CapacityUnits = capacityUnits;
            LaneCount = laneCount;
            MeshSampleCount = meshSampleCount;
            RoadProfile = roadProfile;
            Path = path;
            LeftFeederPath = leftFeederPath;
            RightFeederPath = rightFeederPath;
            VisibleFeederTopY = visibleFeederTopY;
        }

        public RoadPresetDefinition Preset { get; }
        public int CapacityUnits { get; }
        public int LaneCount { get; }
        public int MeshSampleCount { get; }
        public PassengerRoadProfile RoadProfile { get; }
        public float LaneSpacing => RoadProfile.LaneWidth;
        public float RoadShoulder => RoadProfile.RoadShoulder;
        public float PassengerPivotOffset => RoadProfile.PivotOffset;
        public Vector4 PassengerPersonLocalZ => RoadProfile.PersonLocalOffsets;
        public float RoadInnerOffset => RoadProfile.InnerRoadOffset;
        public float RoadOuterOffset => RoadProfile.OuterRoadOffset;
        public float OuterSpacingOffset => RoadProfile.GetPersonLaneOffset(3);
        public RotaryPath Path { get; }
        public FeederRoadPath LeftFeederPath { get; }
        public FeederRoadPath RightFeederPath { get; }
        public float VisibleFeederTopY { get; }
        public float PassengerSpeed => Preset.PassengerSpeed;
        public float RoadWidth => RoadProfile.RoadWidth;
        public float OuterRadiusX => Path.RadiusX + RoadWidth;
        public float OuterRadiusZ => Path.RadiusZ + RoadWidth;

        public static RotaryLayout Create(
            RoadPresetDefinition preset,
            int rotaryUnitCapacity,
            float passengerTangentialSlotSpacing,
            PassengerRoadProfile roadProfile)
        {
            var capacity = Mathf.Clamp(rotaryUnitCapacity, LevelData.MinRotaryUnitCapacity, preset.MaxCapacityUnits);
            var targetPathLength = capacity * passengerTangentialSlotSpacing;
            var path = RoadPresetLibrary.CreatePath(preset, targetPathLength);
            var meshSampleCount = Mathf.Clamp(capacity * 6, 128, 256);
            var leftFeederPath = CreateFeederPath(path, preset, -1, roadProfile, out var leftVisibleTopY);
            var rightFeederPath = CreateFeederPath(path, preset, 1, roadProfile, out var rightVisibleTopY);

            return new RotaryLayout(
                preset,
                capacity,
                1,
                meshSampleCount,
                roadProfile,
                path,
                leftFeederPath,
                rightFeederPath,
                Mathf.Max(leftVisibleTopY, rightVisibleTopY));
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
            var localPoint = sample.Point + sample.Outward * RoadProfile.PivotOffset;
            return PassengerUnitRoadPose.FromPathSample(ToWorldPoint(localPoint, centerZ, y), sample, PassengerPersonLocalZ);
        }

        public float GetFeederDistanceForSlot(int side, int slotIndex)
        {
            var feederPath = GetFeederPath(side);
            var distanceFromJoin =
                (FeederMergeClearanceRows + Mathf.Max(0, slotIndex) * FeederQueueSpacingMultiplier) *
                Preset.FeederRowSpacing;
            var maxDistanceFromJoin = Mathf.Max(Preset.FeederRowSpacing, feederPath.Length - Preset.FeederRowSpacing * FeederStartPaddingRows);
            distanceFromJoin = Mathf.Min(distanceFromJoin, maxDistanceFromJoin);
            return Mathf.Clamp(feederPath.Length - distanceFromJoin, 0f, feederPath.Length);
        }

        public FeederRoadPath GetFeederPath(int side)
        {
            return side < 0 ? LeftFeederPath : RightFeederPath;
        }

        public float GetPersonLaneOffset(int personIndex)
        {
            return RoadProfile.GetPersonLaneOffset(personIndex);
        }

        private static FeederRoadPath CreateFeederPath(
            RotaryPath rotaryPath,
            RoadPresetDefinition preset,
            int side,
            PassengerRoadProfile roadProfile,
            out float visibleTopY)
        {
            var progress = side < 0 ? preset.LeftFeederProgress : preset.RightFeederProgress;
            var joinSample = rotaryPath.Sample(progress);
            var roadWidth = roadProfile.RoadWidth;
            var outerRoadOffset = roadProfile.OuterRoadOffset;
            var joinPoint = joinSample.Point + joinSample.Outward * Mathf.Max(0.05f, outerRoadOffset - 0.015f);
            var laneLength = Mathf.Max(1.05f, preset.FeederRowsPerStack * preset.FeederRowSpacing + roadProfile.LaneWidth);
            var sideSign = side < 0 ? -1f : 1f;
            var laneX = sideSign * (rotaryPath.RadiusX + roadWidth * 1.05f + 0.16f);
            var start = new Vector2(laneX, joinPoint.y + laneLength * 0.82f);
            var verticalEnd = new Vector2(laneX, joinPoint.y + laneLength * 0.24f);
            visibleTopY = start.y;
            var hiddenTailDirection = start - verticalEnd;
            if (hiddenTailDirection.sqrMagnitude < 0.0001f)
            {
                hiddenTailDirection = Vector2.up;
            }

            hiddenTailDirection.Normalize();
            var hiddenStart = start + hiddenTailDirection * preset.FeederRowSpacing * FeederHiddenTailRows;
            var joinOverlap = joinPoint - joinSample.Outward * Mathf.Min(0.085f, roadWidth * 0.18f);
            var approach = new Vector2(
                Mathf.Lerp(laneX, joinPoint.x, 0.68f),
                joinPoint.y + laneLength * 0.06f);

            return new FeederRoadPath(new[]
            {
                hiddenStart,
                start,
                verticalEnd,
                approach,
                joinOverlap
            });
        }
    }
}
