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

        public PassengerUnitRoadPose WithForwardDirection(Vector3 forwardDirection)
        {
            if (!HasCustomPersonLocalPositions)
            {
                return new PassengerUnitRoadPose(
                    Position,
                    Quaternion.LookRotation(NormalizeFlat(forwardDirection, Vector3.forward), Vector3.up));
            }

            return FromPersonWorldPositions(
                Position + Rotation * Person1LocalPosition,
                Position + Rotation * Person2LocalPosition,
                Position + Rotation * Person3LocalPosition,
                Position + Rotation * Person4LocalPosition,
                forwardDirection);
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
        public RotaryLayout(
            RoadPresetDefinition preset,
            int capacityUnits,
            int laneCount,
            int meshSampleCount,
            PassengerRoadProfile roadProfile,
            RotaryPath path,
            FeederRoadPath leftFeederPath,
            FeederRoadPath rightFeederPath,
            FeederQueueLaneSampler leftFeederLaneSampler,
            FeederQueueLaneSampler rightFeederLaneSampler,
            float leftFeederJoinProgress,
            float rightFeederJoinProgress,
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
            LeftFeederLaneSampler = leftFeederLaneSampler;
            RightFeederLaneSampler = rightFeederLaneSampler;
            LeftFeederJoinProgress = leftFeederJoinProgress;
            RightFeederJoinProgress = rightFeederJoinProgress;
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
        public FeederQueueLaneSampler LeftFeederLaneSampler { get; }
        public FeederQueueLaneSampler RightFeederLaneSampler { get; }
        public float LeftFeederJoinProgress { get; }
        public float RightFeederJoinProgress { get; }
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
            var leftJoin = FeederJoinSnapEngine.Resolve(path, preset, -1, roadProfile);
            var rightJoin = FeederJoinSnapEngine.Resolve(path, preset, 1, roadProfile);
            var leftFeederPath = CreateFeederPath(preset, roadProfile, leftJoin, out var leftVisibleTopY);
            var rightFeederPath = CreateFeederPath(preset, roadProfile, rightJoin, out var rightVisibleTopY);
            var leftFeederLaneSampler = new FeederQueueLaneSampler(-1, leftFeederPath, roadProfile, meshSampleCount);
            var rightFeederLaneSampler = new FeederQueueLaneSampler(1, rightFeederPath, roadProfile, meshSampleCount);

            return new RotaryLayout(
                preset,
                capacity,
                1,
                meshSampleCount,
                roadProfile,
                path,
                leftFeederPath,
                rightFeederPath,
                leftFeederLaneSampler,
                rightFeederLaneSampler,
                leftJoin.Progress,
                rightJoin.Progress,
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
            return GetFeederLaneSampler(side).GetPose(distance, this, centerZ, y);
        }

        public float GetFeederDistanceForSlot(int side, int slotIndex)
        {
            var queueLength = GetFeederQueueLength(side);
            var distanceFromJoin =
                (PassengerUnitLayout.FeederMergeClearanceRows + Mathf.Max(0, slotIndex) * PassengerUnitLayout.FeederQueueSpacingMultiplier) *
                Preset.FeederRowSpacing;
            var maxDistanceFromJoin = Mathf.Max(Preset.FeederRowSpacing, queueLength - Preset.FeederRowSpacing * PassengerUnitLayout.FeederStartPaddingRows);
            distanceFromJoin = Mathf.Min(distanceFromJoin, maxDistanceFromJoin);
            return Mathf.Clamp(queueLength - distanceFromJoin, 0f, queueLength);
        }

        public FeederRoadPath GetFeederPath(int side)
        {
            return side < 0 ? LeftFeederPath : RightFeederPath;
        }

        public FeederQueueLaneSampler GetFeederLaneSampler(int side)
        {
            return side < 0 ? LeftFeederLaneSampler : RightFeederLaneSampler;
        }

        public float GetFeederQueueLength(int side)
        {
            return GetFeederLaneSampler(side).ReferencePathLength;
        }

        public RotaryPathSample SampleFeederPath(int side, FeederRoadPath path, float progress)
        {
            return FeederQueueLayout.Sample(side, path, progress);
        }

        public RotaryPathSample SampleFeederPathByDistance(int side, FeederRoadPath path, float distance)
        {
            return FeederQueueLayout.SampleByDistance(side, path, distance);
        }

        public float GetFeederJoinProgress(int side)
        {
            return side < 0 ? LeftFeederJoinProgress : RightFeederJoinProgress;
        }

        public float GetPersonLaneOffset(int personIndex)
        {
            return RoadProfile.GetPersonLaneOffset(personIndex);
        }

        private static FeederRoadPath CreateFeederPath(
            RoadPresetDefinition preset,
            PassengerRoadProfile roadProfile,
            FeederJoinResolution join,
            out float visibleTopY)
        {
            var joinSample = join.JoinSample;
            var joinPoint = join.JoinPoint;
            var laneLength = join.LaneLength;
            var laneX = join.LaneX;
            var start = new Vector2(laneX, joinPoint.y + laneLength * 0.82f);
            var verticalEnd = new Vector2(laneX, joinPoint.y + laneLength * 0.24f);
            visibleTopY = start.y;
            var hiddenTailDirection = start - verticalEnd;
            if (hiddenTailDirection.sqrMagnitude < 0.0001f)
            {
                hiddenTailDirection = Vector2.up;
            }

            hiddenTailDirection.Normalize();
            var hiddenStart = start + hiddenTailDirection * preset.FeederRowSpacing * PassengerUnitLayout.FeederHiddenTailRows;
            var joinOverlap = joinPoint - joinSample.Outward * Mathf.Min(0.085f, roadProfile.RoadWidth * 0.18f);
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
