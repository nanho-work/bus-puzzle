using UnityEngine;

namespace BusPuzzle
{
    public static class ShapeLibraryVehicleCoverage
    {
        public static bool RequiresCoverage(int layoutVariantIndex)
        {
            return VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out _);
        }

        public static bool IsSatisfied(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            int actualVehicleCount)
        {
            if (!RequiresCoverage(layoutVariantIndex))
            {
                return true;
            }

            return actualVehicleCount >= GetMinimumVehicleCount(profile, layoutVariantIndex);
        }

        public static int GetMinimumVehicleCount(
            LevelDifficultyProfile profile,
            int layoutVariantIndex)
        {
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            if (!VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                return 0;
            }

            var targetVehicleCount = profile.TargetVehicleCount;
            var ratio = GetMinimumCoverageRatio((VehicleShapeLibraryId)libraryIndex);
            return Mathf.Clamp(
                Mathf.CeilToInt(targetVehicleCount * ratio),
                Mathf.Min(targetVehicleCount, 8),
                targetVehicleCount);
        }

        public static int GetMinimumOpeningExitCount(
            int actualVehicleCount,
            int layoutVariantIndex)
        {
            if (!VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                return actualVehicleCount;
            }

            var ratio = GetMinimumOpeningExitRatio((VehicleShapeLibraryId)libraryIndex);
            return Mathf.Clamp(
                Mathf.CeilToInt(actualVehicleCount * ratio),
                Mathf.Min(actualVehicleCount, 3),
                actualVehicleCount);
        }

        private static float GetMinimumCoverageRatio(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                    return 0.68f;
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                    return 0.60f;
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                case VehicleShapeLibraryId.Shield:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                    return 0.78f;
                case VehicleShapeLibraryId.Star:
                    return 0.92f;
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Fan:
                    return 0.76f;
                default:
                    return 0.74f;
            }
        }

        private static float GetMinimumOpeningExitRatio(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                case VehicleShapeLibraryId.Star:
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                case VehicleShapeLibraryId.Fan:
                    return 0.22f;
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Triangle:
                case VehicleShapeLibraryId.Cross:
                case VehicleShapeLibraryId.X:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                case VehicleShapeLibraryId.Shield:
                case VehicleShapeLibraryId.Smile:
                    return 0.20f;
                default:
                    return 0.18f;
            }
        }
    }
}
