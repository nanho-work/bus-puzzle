using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct PassengerRoadProfile
    {
        public readonly Vector4 PersonLocalOffsets;
        public readonly float PersonRadius;
        public readonly float RailClearance;
        public readonly float RoadShoulder;
        public readonly float LaneWidth;
        public readonly float RoadWidth;
        public readonly float PivotOffset;

        private PassengerRoadProfile(
            Vector4 personLocalOffsets,
            float personRadius,
            float railClearance,
            float roadShoulder,
            float laneWidth,
            float roadWidth,
            float pivotOffset)
        {
            PersonLocalOffsets = personLocalOffsets;
            PersonRadius = personRadius;
            RailClearance = railClearance;
            RoadShoulder = roadShoulder;
            LaneWidth = laneWidth;
            RoadWidth = roadWidth;
            PivotOffset = pivotOffset;
        }

        public float InnerRoadOffset => -PivotOffset;
        public float OuterRoadOffset => RoadWidth - PivotOffset;
        public float InnerPassengerEdge => PivotOffset + MinPersonOffset - PersonRadius;
        public float OuterPassengerEdge => PivotOffset + MaxPersonOffset + PersonRadius;

        public static PassengerRoadProfile Create(
            Vector4 personLocalOffsets,
            float personRadius,
            float railClearance,
            float roadShoulder)
        {
            personRadius = Mathf.Max(0.001f, personRadius);
            railClearance = Mathf.Max(0f, railClearance);
            roadShoulder = Mathf.Max(0f, roadShoulder);

            var minOffset = MinOffset(personLocalOffsets);
            var maxOffset = MaxOffset(personLocalOffsets);
            var laneWidth = maxOffset - minOffset + personRadius * 2f + railClearance * 2f;
            var roadWidth = laneWidth + roadShoulder * 2f;
            var clearanceToRoadEdge = railClearance + roadShoulder;
            var pivotOffset = (clearanceToRoadEdge - minOffset + personRadius) * 0.5f;

            return new PassengerRoadProfile(
                personLocalOffsets,
                personRadius,
                railClearance,
                roadShoulder,
                laneWidth,
                roadWidth,
                pivotOffset);
        }

        public float GetPersonLaneOffset(int personIndex)
        {
            return PivotOffset + GetPersonLocalOffset(personIndex);
        }

        public float GetPersonLocalOffset(int personIndex)
        {
            switch (personIndex)
            {
                case 0:
                    return PersonLocalOffsets.x;
                case 1:
                    return PersonLocalOffsets.y;
                case 2:
                    return PersonLocalOffsets.z;
                default:
                    return PersonLocalOffsets.w;
            }
        }

        private float MinPersonOffset => MinOffset(PersonLocalOffsets);
        private float MaxPersonOffset => MaxOffset(PersonLocalOffsets);

        private static float MinOffset(Vector4 offsets)
        {
            return Mathf.Min(Mathf.Min(offsets.x, offsets.y), Mathf.Min(offsets.z, offsets.w));
        }

        private static float MaxOffset(Vector4 offsets)
        {
            return Mathf.Max(Mathf.Max(offsets.x, offsets.y), Mathf.Max(offsets.z, offsets.w));
        }
    }
}
