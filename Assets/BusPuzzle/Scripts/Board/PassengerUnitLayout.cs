using UnityEngine;

namespace BusPuzzle
{
    internal static class PassengerUnitLayout
    {
        public const bool UseAssetPassengerPrefabs = false;
        public const int PeoplePerUnit = 4;

        public const float VisualScale = 1.2f;
        public const float FootprintScale = 2.65f;
        public const float PersonRadius = 0.065f * FootprintScale;
        public const float RailClearance = 0.032f;
        public const float UnitY = 0.08f;

        public const float PersonSpacingScale = 3.05f;
        public const float RotaryUnitSpacing = 0.108f;
        public const float FeederVacancyWindowDistance = RotaryUnitSpacing * 0.62f;

        public const float FeederMergeClearanceRows = 2.35f;
        public const float FeederQueueSpacingMultiplier = 1.17f;
        public const float FeederStartPaddingRows = 0.35f;
        public const float FeederHiddenTailRows = 14f;

        public static readonly Vector4 PersonLocalZOffsets = new Vector4(
            -0.155f * PersonSpacingScale,
            -0.052f * PersonSpacingScale,
            0.052f * PersonSpacingScale,
            0.155f * PersonSpacingScale);

        public static Vector3[] CreateDefaultPersonLocalPositions()
        {
            return new[]
            {
                GetDefaultPersonLocalPosition(0),
                GetDefaultPersonLocalPosition(1),
                GetDefaultPersonLocalPosition(2),
                GetDefaultPersonLocalPosition(3)
            };
        }

        public static Vector3 GetDefaultPersonLocalPosition(int personIndex)
        {
            return new Vector3(0f, 0f, GetPersonLocalZOffset(personIndex));
        }

        public static float GetPersonLocalZOffset(int personIndex)
        {
            switch (personIndex)
            {
                case 0:
                    return PersonLocalZOffsets.x;
                case 1:
                    return PersonLocalZOffsets.y;
                case 2:
                    return PersonLocalZOffsets.z;
                default:
                    return PersonLocalZOffsets.w;
            }
        }

        public static PassengerRoadProfile CreateRoadProfile(RoadPresetDefinition preset)
        {
            return PassengerRoadProfile.Create(
                PersonLocalZOffsets,
                PersonRadius,
                RailClearance,
                preset.RoadShoulder);
        }
    }
}
