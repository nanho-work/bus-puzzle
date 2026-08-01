using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct ShapeLibraryLayoutMetrics
    {
        public readonly int VehicleCount;
        public readonly int ShapeMatchedCount;
        public readonly int OutlineMatchedCount;
        public readonly int SmallCount;
        public readonly int MediumCount;
        public readonly int LargeCount;
        public readonly int OutwardFacingCount;
        public readonly int OpeningExitCount;
        public readonly int ShapeFidelityScore;
        public readonly VehicleShapeSilhouetteMetrics SilhouetteMetrics;

        public ShapeLibraryLayoutMetrics(
            int vehicleCount,
            int shapeMatchedCount,
            int outlineMatchedCount,
            int smallCount,
            int mediumCount,
            int largeCount,
            int outwardFacingCount,
            int openingExitCount,
            int shapeFidelityScore,
            VehicleShapeSilhouetteMetrics silhouetteMetrics)
        {
            VehicleCount = vehicleCount;
            ShapeMatchedCount = shapeMatchedCount;
            OutlineMatchedCount = outlineMatchedCount;
            SmallCount = smallCount;
            MediumCount = mediumCount;
            LargeCount = largeCount;
            OutwardFacingCount = outwardFacingCount;
            OpeningExitCount = openingExitCount;
            ShapeFidelityScore = shapeFidelityScore;
            SilhouetteMetrics = silhouetteMetrics;
        }

        public int MediumLargeCount => MediumCount + LargeCount;
    }

    internal static class ShapeLibraryLayoutQuality
    {
        private const float ShapeCellMatchDistanceCells = 0.82f;
        private const float OutwardFacingDotThreshold = 0.707f;
        private const float BoardCenterX = (BoardLayoutConfig.GridColumns - 1) * 0.5f;
        private const float BoardCenterY = (BoardLayoutConfig.GridRows - 1) * 0.5f;

        public static bool RequiresQuality(int layoutVariantIndex)
        {
            return VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out _);
        }

        public static bool IsSatisfied(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<BusDefinition> vehicles,
            bool enforceOpeningMoves = true)
        {
            return !TryGetFailureMessage(
                profile,
                layoutVariantIndex,
                vehicles,
                out _,
                true,
                enforceOpeningMoves);
        }

        public static bool TryGetFailureMessage(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<BusDefinition> vehicles,
            out string message,
            bool enforceTemplateSilhouette = true,
            bool enforceOpeningMoves = true)
        {
            message = string.Empty;
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var vehicleCount = vehicles != null ? vehicles.Count : 0;
            if (!TryResolveQualityLibraryId(
                    profile,
                    Mathf.Max(vehicleCount, profile.TargetVehicleCount),
                    layoutVariantIndex,
                    out var libraryId))
            {
                return false;
            }

            var targetVehicleCount = Mathf.Max(vehicleCount, profile.TargetVehicleCount);
            IVehicleShapeTemplate qualityTemplate = null;
            var hasTemplateDefinition =
                VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                    out var templateDefinition);
            var hasUsableQualityTemplate =
                hasTemplateDefinition &&
                VehicleShapeTemplateCatalog.TryGetQualityTemplate(
                    templateDefinition,
                    out qualityTemplate);
            var hasEnabledQualityTemplate =
                hasUsableQualityTemplate && qualityTemplate.Constraints.EnableSilhouetteGate;
            if (enforceTemplateSilhouette &&
                libraryId == VehicleShapeLibraryId.Heart &&
                !hasEnabledQualityTemplate)
            {
                message =
                    "Required Heart quality template Resources/VehicleShapeTemplates/Heart.asset " +
                    "is missing, unusable, or has its silhouette gate disabled.";
                return true;
            }

            var metrics = CalculateMetrics(profile, layoutVariantIndex, vehicles);
            var hasTemplateQualityProfile = enforceTemplateSilhouette && hasEnabledQualityTemplate;
            var usesLockedTemplateQualityProfile = UsesLockedTemplateQualityProfile(libraryId);
            var minimumCoverage = ShapeLibraryVehicleCoverage.GetMinimumVehicleCount(profile, layoutVariantIndex);
            if (metrics.VehicleCount < minimumCoverage)
            {
                message = $"Shape library coverage {metrics.VehicleCount}/{profile.TargetVehicleCount} is below required minimum {minimumCoverage}.";
                return true;
            }

            if (usesLockedTemplateQualityProfile)
            {
                var minimumMediumLarge = GetMinimumMediumLargeVehicleCount(profile, libraryId, metrics.VehicleCount);
                if (metrics.MediumLargeCount < minimumMediumLarge)
                {
                    message = $"Shape library medium/large mix {metrics.MediumLargeCount}/{metrics.VehicleCount} is below required minimum {minimumMediumLarge}.";
                    return true;
                }

                var minimumLarge = GetMinimumLargeVehicleCount(profile, libraryId, metrics.VehicleCount);
                if (metrics.LargeCount < minimumLarge)
                {
                    message = $"Shape library large vehicles {metrics.LargeCount}/{metrics.VehicleCount} are below required minimum {minimumLarge}.";
                    return true;
                }
            }

            if (!IsRelaxedCircularLibrary(libraryId))
            {
                var minimumMatched = GetMinimumShapeMatchedCount(
                    libraryId,
                    layoutVariantIndex,
                    metrics.VehicleCount,
                    metrics.MediumLargeCount,
                    usesLockedTemplateQualityProfile);
                if (metrics.ShapeMatchedCount < minimumMatched)
                {
                    message = $"Shape library silhouette match {metrics.ShapeMatchedCount}/{metrics.VehicleCount} is below required minimum {minimumMatched}.";
                    return true;
                }
            }

            if (RestrictsOutwardFacing(libraryId))
            {
                var maximumOutward = GetMaximumOutwardFacingCount(profile, libraryId, metrics.VehicleCount);
                if (metrics.OutwardFacingCount > maximumOutward)
                {
                    message = $"Shape library outward-facing vehicles {metrics.OutwardFacingCount}/{metrics.VehicleCount} exceed maximum {maximumOutward}.";
                    return true;
                }
            }

            if (enforceOpeningMoves)
            {
                var minimumOpening = ShapeLibraryVehicleCoverage.GetMinimumOpeningExitCount(
                    metrics.VehicleCount,
                    libraryId);
                if (metrics.OpeningExitCount < minimumOpening)
                {
                    message = $"Shape library opening exits {metrics.OpeningExitCount}/{metrics.VehicleCount} are below required minimum {minimumOpening}.";
                    return true;
                }

                var maximumOpening = GetMaximumOpeningExitCount(profile, libraryId, metrics.VehicleCount);
                if (metrics.OpeningExitCount > maximumOpening)
                {
                    message = $"Shape library opening exits {metrics.OpeningExitCount}/{metrics.VehicleCount} exceed maximum {maximumOpening}.";
                    return true;
                }
            }

            if (!IsRelaxedCircularLibrary(libraryId) && !hasTemplateQualityProfile)
            {
                // The legacy fidelity score grades root-cell positions and authored
                // directions. A template-backed silhouette is graded from the vehicles'
                // actual rendered footprints instead, including boundary distance,
                // symmetry, topology, key features and tangent alignment. Applying both
                // rejects valid geometry-preserving opening rotations, so the stronger
                // visual contract supersedes the legacy score for opted-in templates.
                var maximumFidelityScore = GetMaximumShapeFidelityScore(
                    profile,
                    libraryId,
                    layoutVariantIndex,
                    metrics.VehicleCount,
                    usesLockedTemplateQualityProfile);
                if (metrics.ShapeFidelityScore > maximumFidelityScore)
                {
                    message = $"Shape library fidelity score {metrics.ShapeFidelityScore} exceeds maximum {maximumFidelityScore}.";
                    return true;
                }
            }

            // Root-cell fidelity alone can approve a recognizable-but-ugly silhouette.
            // Template-backed libraries therefore get a second, visual-footprint gate.
            if (hasTemplateQualityProfile)
            {
                if (!metrics.SilhouetteMetrics.WasEvaluated)
                {
                    message = $"Shape template {qualityTemplate.DisplayName} silhouette could not be evaluated.";
                    return true;
                }

                if (VehicleShapeSilhouetteQuality.TryGetFailureMessage(
                        qualityTemplate,
                        metrics.SilhouetteMetrics,
                        out message))
                {
                    return true;
                }
            }

            return false;
        }

        public static ShapeLibraryLayoutMetrics CalculateMetrics(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<BusDefinition> vehicles)
        {
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var vehicleCount = vehicles != null ? vehicles.Count : 0;
            var shapeMatchedCount = 0;
            var outlineMatchedCount = 0;
            var smallCount = 0;
            var mediumCount = 0;
            var largeCount = 0;
            var outwardFacingCount = 0;
            var openingExitCount = CountOpeningExits(vehicles);
            var targetVehicleCount = Mathf.Max(vehicleCount, profile.TargetVehicleCount);
            VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                profile,
                targetVehicleCount,
                layoutVariantIndex,
                out var definition);

            for (var index = 0; index < vehicleCount; index++)
            {
                var vehicle = vehicles[index];
                switch (vehicle.Size)
                {
                    case BusSize.Medium:
                        mediumCount++;
                        break;
                    case BusSize.Large:
                        largeCount++;
                        break;
                    default:
                        smallCount++;
                        break;
                }

                if (IsOutwardFacing(vehicle))
                {
                    outwardFacingCount++;
                }

                if (TryGetShapeMatchedCell(definition, vehicle, out var nearestCell))
                {
                    shapeMatchedCount++;
                    if (nearestCell.Role == VehicleShapeCellRole.Outline)
                    {
                        outlineMatchedCount++;
                    }
                }
            }

            var shapeFidelityScore = VehicleLayoutPatternEngine.ScoreShapeFidelity(
                profile,
                targetVehicleCount,
                layoutVariantIndex,
                vehicles);
            var silhouetteMetrics = default(VehicleShapeSilhouetteMetrics);
            VehicleShapeSilhouetteQuality.TryEvaluate(definition, vehicles, out silhouetteMetrics);

            return new ShapeLibraryLayoutMetrics(
                vehicleCount,
                shapeMatchedCount,
                outlineMatchedCount,
                smallCount,
                mediumCount,
                largeCount,
                outwardFacingCount,
                openingExitCount,
                shapeFidelityScore,
                silhouetteMetrics);
        }

        private static bool TryResolveQualityLibraryId(
            LevelDifficultyProfile profile,
            int targetVehicleCount,
            int layoutVariantIndex,
            out VehicleShapeLibraryId libraryId)
        {
            if (VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                libraryId = (VehicleShapeLibraryId)libraryIndex;
                return true;
            }

            if (VehicleLayoutPatternEngine.TryCreateTemplateQualityShapeDefinition(
                    profile,
                    targetVehicleCount,
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

        private static bool TryGetShapeMatchedCell(
            VehicleShapeLayoutDefinition definition,
            BusDefinition vehicle,
            out VehicleShapeCell matchedCell)
        {
            matchedCell = default;
            if (definition.Kind == VehicleShapeLayoutKind.None)
            {
                return false;
            }

            var position = new Vector2(
                vehicle.GridPosition.x + vehicle.PositionOffsetCells.x,
                vehicle.GridPosition.y + vehicle.PositionOffsetCells.y);
            if (TryGetShapeMatchedCell(definition, position, ShapeCellMatchDistanceCells, out matchedCell))
            {
                return true;
            }

            if (!UsesSizeAwareShapeMatching(definition) || vehicle.Size == BusSize.Small)
            {
                return false;
            }

            var footprint = BoardLayoutConfig.GetVehicleVisualFootprintCells(vehicle);
            var forwardReach = footprint.Forward * footprint.HalfLength;
            var rightReach = footprint.Right * footprint.HalfWidth * 0.35f;
            const float sizeAwareMatchDistanceCells = 0.98f;
            return TryGetShapeMatchedCell(definition, footprint.Center, sizeAwareMatchDistanceCells, out matchedCell) ||
                TryGetShapeMatchedCell(definition, footprint.Center + forwardReach * 0.45f, sizeAwareMatchDistanceCells, out matchedCell) ||
                TryGetShapeMatchedCell(definition, footprint.Center - forwardReach * 0.45f, sizeAwareMatchDistanceCells, out matchedCell) ||
                TryGetShapeMatchedCell(definition, footprint.Center + forwardReach * 0.80f + rightReach, sizeAwareMatchDistanceCells, out matchedCell) ||
                TryGetShapeMatchedCell(definition, footprint.Center + forwardReach * 0.80f - rightReach, sizeAwareMatchDistanceCells, out matchedCell) ||
                TryGetShapeMatchedCell(definition, footprint.Center - forwardReach * 0.80f + rightReach, sizeAwareMatchDistanceCells, out matchedCell) ||
                TryGetShapeMatchedCell(definition, footprint.Center - forwardReach * 0.80f - rightReach, sizeAwareMatchDistanceCells, out matchedCell);
        }

        private static bool TryGetShapeMatchedCell(
            VehicleShapeLayoutDefinition definition,
            Vector2 position,
            float maxDistanceCells,
            out VehicleShapeCell matchedCell)
        {
            matchedCell = default;
            return VehicleShapeLayoutEngine.TryFindNearestShapeCell(
                    definition,
                    position,
                    out matchedCell,
                    out var distanceCells) &&
                distanceCells <= maxDistanceCells;
        }

        private static bool UsesSizeAwareShapeMatching(VehicleShapeLayoutDefinition definition)
        {
            return definition.LibraryId == VehicleShapeLibraryId.Star &&
                definition.VariantSeed == StageGenerationPlanner.StarSizeMixVariantSeed;
        }

        private static bool UsesStarSizeMixLayoutVariant(int layoutVariantIndex)
        {
            return VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex) &&
                (VehicleShapeLibraryId)libraryIndex == VehicleShapeLibraryId.Star &&
                VehicleLayoutPatternEngine.TryGetShapeLibraryVariantSeed(layoutVariantIndex, out var variantSeed) &&
                variantSeed == StageGenerationPlanner.StarSizeMixVariantSeed;
        }

        public static int GetMinimumMediumLargeVehicleCount(
            LevelDifficultyProfile profile,
            VehicleShapeLibraryId libraryId,
            int vehicleCount)
        {
            if (vehicleCount < 28)
            {
                return 0;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var minimum = vehicleCount >= 50
                ? 8
                : vehicleCount >= 42
                    ? 6
                    : vehicleCount >= 34
                        ? 4
                        : 2;
            if (profile.Difficulty == LevelDifficulty.Normal)
            {
                minimum = Mathf.Max(2, minimum - 1);
            }

            if (IsNarrowLibrary(libraryId))
            {
                minimum = Mathf.Min(minimum, vehicleCount >= 42 ? 5 : 3);
            }

            return Mathf.Clamp(minimum, 0, Mathf.Max(0, vehicleCount / 2));
        }

        public static int GetMinimumLargeVehicleCount(
            LevelDifficultyProfile profile,
            VehicleShapeLibraryId libraryId,
            int vehicleCount)
        {
            if (libraryId == VehicleShapeLibraryId.Star && vehicleCount >= 30)
            {
                return 1;
            }

            if (!SupportsLargeVehicle(libraryId) || vehicleCount < 46)
            {
                return 0;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            if (profile.Difficulty == LevelDifficulty.SuperHard && vehicleCount >= 54)
            {
                return 2;
            }

            return 1;
        }

        public static int GetMaximumOpeningExitCount(
            LevelDifficultyProfile profile,
            VehicleShapeLibraryId libraryId,
            int vehicleCount)
        {
            if (vehicleCount <= 0)
            {
                return 0;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var ratio = libraryId == VehicleShapeLibraryId.Sunburst
                ? 0.96f
                : libraryId == VehicleShapeLibraryId.Star
                    ? 0.70f
                : libraryId == VehicleShapeLibraryId.Cross || libraryId == VehicleShapeLibraryId.X
                    ? 0.92f
                : libraryId == VehicleShapeLibraryId.HollowSquare
                    ? 0.86f
                : IsRadialPetalLibrary(libraryId)
                    ? 0.78f
                    : IsLinearLibrary(libraryId)
                        ? 0.72f
                        : IsClosedGeometricLibrary(libraryId)
                            ? 0.62f
                            : 0.58f;
            if (profile.Difficulty == LevelDifficulty.Normal)
            {
                ratio += 0.06f;
            }
            else if (profile.Difficulty == LevelDifficulty.SuperHard)
            {
                ratio -= 0.04f;
            }

            return Mathf.Clamp(Mathf.FloorToInt(vehicleCount * ratio), 4, vehicleCount);
        }

        public static int GetMaximumOutwardFacingCount(
            LevelDifficultyProfile profile,
            VehicleShapeLibraryId libraryId,
            int vehicleCount)
        {
            if (vehicleCount <= 0)
            {
                return 0;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var ratio = GetMaximumOutwardFacingRatio(libraryId);
            if (profile.Difficulty == LevelDifficulty.Normal)
            {
                ratio += 0.04f;
            }
            else if (profile.Difficulty == LevelDifficulty.SuperHard)
            {
                ratio -= 0.03f;
            }

            return Mathf.Clamp(Mathf.FloorToInt(vehicleCount * ratio), 4, vehicleCount);
        }

        public static bool SupportsLargeVehicle(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.Smile:
                    return false;
                default:
                    return true;
            }
        }

        private static int CountOpeningExits(IReadOnlyList<BusDefinition> vehicles)
        {
            if (vehicles == null || vehicles.Count == 0)
            {
                return 0;
            }

            var active = new bool[vehicles.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
            }

            var count = 0;
            for (var index = 0; index < vehicles.Count; index++)
            {
                if (LevelVehicleExitPlanner.IsPathClear(index, vehicles, active, out _))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsOutwardFacing(BusDefinition vehicle)
        {
            var position = new Vector2(
                vehicle.GridPosition.x + vehicle.PositionOffsetCells.x,
                vehicle.GridPosition.y + vehicle.PositionOffsetCells.y);
            var fromCenter = position - new Vector2(BoardCenterX, BoardCenterY);
            if (fromCenter.sqrMagnitude < 0.001f)
            {
                return false;
            }

            fromCenter.Normalize();
            var yawRadians = vehicle.YawDegrees * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(yawRadians), Mathf.Cos(yawRadians));
            return Vector2.Dot(forward, fromCenter) >= OutwardFacingDotThreshold;
        }

        private static float GetMinimumShapeMatchRatio(VehicleShapeLibraryId libraryId)
        {
            if (libraryId == VehicleShapeLibraryId.Cross || libraryId == VehicleShapeLibraryId.X)
            {
                return 0.74f;
            }

            if (IsSparsePathLibrary(libraryId))
            {
                if (libraryId == VehicleShapeLibraryId.Stairs)
                {
                    return 0.55f;
                }

                return 0.62f;
            }

            if (IsRadialPetalLibrary(libraryId))
            {
                return 0.68f;
            }

            if (libraryId == VehicleShapeLibraryId.Star)
            {
                return 0.78f;
            }

            if (libraryId == VehicleShapeLibraryId.Clover ||
                libraryId == VehicleShapeLibraryId.Eight)
            {
                return 0.72f;
            }

            return IsLinearLibrary(libraryId) ? 0.76f : 0.82f;
        }

        private static int GetMinimumShapeMatchedCount(
            VehicleShapeLibraryId libraryId,
            int layoutVariantIndex,
            int vehicleCount,
            int mediumLargeCount,
            bool usesLockedTemplateQualityProfile)
        {
            if (vehicleCount <= 0)
            {
                return 0;
            }

            var minimum = Mathf.CeilToInt(vehicleCount * GetMinimumShapeMatchRatio(libraryId));
            if (usesLockedTemplateQualityProfile && libraryId == VehicleShapeLibraryId.Star)
            {
                var mixedSizeCoverageCredit = Mathf.Max(0, mediumLargeCount - 3);
                var mixedSizeMinimumRatio = UsesStarSizeMixLayoutVariant(layoutVariantIndex) ? 0.50f : 0.55f;
                var mixedSizeMinimum = Mathf.CeilToInt(vehicleCount * mixedSizeMinimumRatio);
                minimum = Mathf.Max(mixedSizeMinimum, minimum - mixedSizeCoverageCredit);
            }

            return IsSparsePathLibrary(libraryId)
                ? Mathf.Clamp(minimum, Mathf.Min(vehicleCount, 5), vehicleCount)
                : Mathf.Clamp(Mathf.Max(8, minimum), 0, vehicleCount);
        }

        private static int GetMaximumShapeFidelityScore(
            LevelDifficultyProfile profile,
            VehicleShapeLibraryId libraryId,
            int layoutVariantIndex,
            int vehicleCount,
            bool usesLockedTemplateQualityProfile)
        {
            if (vehicleCount <= 0)
            {
                return 0;
            }

            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var perVehicle = usesLockedTemplateQualityProfile && libraryId == VehicleShapeLibraryId.Star
                ? UsesStarSizeMixLayoutVariant(layoutVariantIndex)
                    ? 40f
                    : 31f
                : IsLinearLibrary(libraryId)
                ? 15f
                : IsRelaxedCircularLibrary(libraryId)
                    ? 17f
                    : IsClosedGeometricLibrary(libraryId)
                        ? 13.5f
                        : 12.5f;
            if (profile.Difficulty == LevelDifficulty.Normal)
            {
                perVehicle += 1.5f;
            }
            else if (profile.Difficulty == LevelDifficulty.SuperHard)
            {
                perVehicle -= 1.0f;
            }

            return Mathf.RoundToInt(vehicleCount * perVehicle + 90f);
        }

        private static bool UsesLockedTemplateQualityProfile(VehicleShapeLibraryId libraryId)
        {
            return libraryId == VehicleShapeLibraryId.Star;
        }

        private static bool RestrictsOutwardFacing(VehicleShapeLibraryId libraryId)
        {
            return !IsLinearLibrary(libraryId) &&
                !IsRelaxedCircularLibrary(libraryId) &&
                libraryId != VehicleShapeLibraryId.Cross &&
                libraryId != VehicleShapeLibraryId.X;
        }

        private static float GetMaximumOutwardFacingRatio(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Smile:
                    return 0.82f;
                case VehicleShapeLibraryId.Sunburst:
                    return 0.96f;
                case VehicleShapeLibraryId.Star:
                    return 0.60f;
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Fan:
                    return 0.74f;
                case VehicleShapeLibraryId.Heart:
                case VehicleShapeLibraryId.HeartArrow:
                case VehicleShapeLibraryId.Shield:
                case VehicleShapeLibraryId.Clover:
                case VehicleShapeLibraryId.Eight:
                    return 0.54f;
                case VehicleShapeLibraryId.HollowSquare:
                    return 0.86f;
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Triangle:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                    return 0.64f;
                default:
                    return 0.62f;
            }
        }

        private static bool IsNarrowLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                case VehicleShapeLibraryId.Smile:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsRadialPetalLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Flower:
                case VehicleShapeLibraryId.Sunburst:
                case VehicleShapeLibraryId.Fan:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSparsePathLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsRelaxedCircularLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Circle:
                case VehicleShapeLibraryId.Ring:
                case VehicleShapeLibraryId.SemiCircle:
                case VehicleShapeLibraryId.DoubleRing:
                case VehicleShapeLibraryId.Spiral:
                case VehicleShapeLibraryId.Smile:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsLinearLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Arrow:
                case VehicleShapeLibraryId.DoubleArrow:
                case VehicleShapeLibraryId.Lightning:
                case VehicleShapeLibraryId.S:
                case VehicleShapeLibraryId.Wave:
                case VehicleShapeLibraryId.Stairs:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsClosedGeometricLibrary(VehicleShapeLibraryId libraryId)
        {
            switch (libraryId)
            {
                case VehicleShapeLibraryId.Square:
                case VehicleShapeLibraryId.HollowSquare:
                case VehicleShapeLibraryId.Diamond:
                case VehicleShapeLibraryId.Triangle:
                case VehicleShapeLibraryId.Grid:
                case VehicleShapeLibraryId.MazeBox:
                case VehicleShapeLibraryId.Crown:
                case VehicleShapeLibraryId.Shield:
                    return true;
                default:
                    return false;
            }
        }
    }
}
