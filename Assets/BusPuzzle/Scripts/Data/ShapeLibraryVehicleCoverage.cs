using UnityEngine;

namespace BusPuzzle
{
    public static class ShapeLibraryVehicleCoverage
    {
        // The authored Heart contour has a fixed visual capacity on the 10x12 board.
        // Requiring 78% of an ever-growing stage target eventually forces the generic
        // filler path to scatter vehicles outside the silhouette. Thirty-two is the bounded
        // dense capacity of this contour; difficulty beyond that belongs in blockers and
        // queues, not fake fill outside the named shape.
        internal const int HeartSilhouetteVehicleCapacity = 32;

        public static bool RequiresCoverage(int layoutVariantIndex)
        {
            return VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out _);
        }

        public static bool RequiresCoverage(
            LevelDifficultyProfile profile,
            int layoutVariantIndex)
        {
            return TryResolveLibraryId(profile, layoutVariantIndex, out _);
        }

        public static bool IsSatisfied(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            int actualVehicleCount)
        {
            if (!TryResolveLibraryId(profile, layoutVariantIndex, out _))
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
            if (!TryResolveLibraryId(profile, layoutVariantIndex, out var libraryId))
            {
                return 0;
            }

            var targetVehicleCount = profile.TargetVehicleCount;
            var ratio = GetMinimumCoverageRatio(libraryId);
            var minimumVehicleCount = Mathf.Clamp(
                Mathf.CeilToInt(targetVehicleCount * ratio),
                Mathf.Min(targetVehicleCount, 8),
                targetVehicleCount);
            if (libraryId == VehicleShapeLibraryId.Heart ||
                libraryId == VehicleShapeLibraryId.HeartArrow)
            {
                minimumVehicleCount = Mathf.Min(
                    minimumVehicleCount,
                    Mathf.Min(targetVehicleCount, HeartSilhouetteVehicleCapacity));
            }

            return minimumVehicleCount;
        }

        public static int GetMinimumOpeningExitCount(
            int actualVehicleCount,
            int layoutVariantIndex)
        {
            if (!VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                return actualVehicleCount;
            }

            return GetMinimumOpeningExitCount(actualVehicleCount, (VehicleShapeLibraryId)libraryIndex);
        }

        internal static int GetMinimumOpeningExitCount(
            int actualVehicleCount,
            VehicleShapeLibraryId libraryId)
        {
            var ratio = GetMinimumOpeningExitRatio(libraryId);
            return Mathf.Clamp(
                Mathf.CeilToInt(actualVehicleCount * ratio),
                Mathf.Min(actualVehicleCount, 3),
                actualVehicleCount);
        }

        private static bool TryResolveLibraryId(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            out VehicleShapeLibraryId libraryId)
        {
            if (VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                libraryId = (VehicleShapeLibraryId)libraryIndex;
                return true;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            if (VehicleLayoutPatternEngine.TryCreateTemplateQualityShapeDefinition(
                    profile,
                    Mathf.Max(1, profile.TargetVehicleCount),
                    layoutVariantIndex,
                    out var definition) &&
                definition.LibraryId != VehicleShapeLibraryId.None)
            {
                libraryId = definition.LibraryId;
                return true;
            }

            libraryId = VehicleShapeLibraryId.None;
            return false;
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
