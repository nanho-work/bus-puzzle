using UnityEngine;

namespace BusPuzzle
{
    internal static class PassengerUnitLayout
    {
        public const bool UseAssetPassengerPrefabs = true;
        public const int PeoplePerUnit = 4;

        private const float BaseVisualScale = 2.8f;
        private const float BaseFootprintScale = 2.65f;
        private const float BasePersonSpacingScale = 3.05f;
        private const float BaseRailClearance = 0.032f;
        private const float BaseRotaryUnitSpacing = 0.108f;
        private const float BaseFeederQueueSpacingMultiplier = 1.17f;

        public const float VisualScale = 2.0f;
        public const float LayoutVisualScale = 1.3f;
        public const float LayoutScale = LayoutVisualScale / BaseVisualScale;
        public const float FootprintScale = BaseFootprintScale * LayoutScale;
        public const float PersonRadius = 0.065f * FootprintScale;
        public const float RailClearance = BaseRailClearance * LayoutScale;
        public const float UnitY = 0.08f;

        public const float PersonSpacingScale = BasePersonSpacingScale * LayoutScale;
        public const float RotaryUnitSpacingScale = 1.2f;
        public const float RotaryUnitSpacing = BaseRotaryUnitSpacing * RotaryUnitSpacingScale;
        public const float FeederVacancyWindowDistance = RotaryUnitSpacing * 0.62f;

        public const float FeederMergeClearanceRows = 2.35f;
        public const float FeederQueueSpacingScale = 2f;
        public const float FeederQueueSpacingMultiplier = BaseFeederQueueSpacingMultiplier * FeederQueueSpacingScale;
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
            return CreateRoadProfile(preset, 1f);
        }

        public static PassengerRoadProfile CreateRoadProfile(RoadPresetDefinition preset, float roadScale)
        {
            roadScale = Mathf.Max(0.10f, roadScale);
            return PassengerRoadProfile.Create(
                PersonLocalZOffsets * roadScale,
                PersonRadius * roadScale,
                RailClearance * roadScale,
                preset.RoadShoulder * LayoutScale * roadScale);
        }
    }
}
