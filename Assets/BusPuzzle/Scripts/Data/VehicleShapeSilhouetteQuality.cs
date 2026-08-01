using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public readonly struct VehicleShapeSilhouetteMetrics
    {
        public VehicleShapeSilhouetteMetrics(
            string templateId,
            bool usesFilledTarget,
            int targetPixelCount,
            int outputPixelCount,
            float intersectionOverUnion,
            float targetCoverage,
            float outsideRatio,
            float boundaryErrorCells,
            float symmetryError,
            int targetComponentCount,
            int outputComponentCount,
            int targetHoleCount,
            int outputHoleCount,
            float targetHoleAreaCells,
            float outputHoleAreaCells,
            float featurePassRatio,
            string failedFeatureIds,
            float meanTangentErrorDegrees,
            int boundaryVehicleCount)
        {
            WasEvaluated = true;
            TemplateId = templateId ?? string.Empty;
            UsesFilledTarget = usesFilledTarget;
            TargetPixelCount = targetPixelCount;
            OutputPixelCount = outputPixelCount;
            IntersectionOverUnion = intersectionOverUnion;
            TargetCoverage = targetCoverage;
            OutsideRatio = outsideRatio;
            BoundaryErrorCells = boundaryErrorCells;
            SymmetryError = symmetryError;
            TargetComponentCount = targetComponentCount;
            OutputComponentCount = outputComponentCount;
            TargetHoleCount = targetHoleCount;
            OutputHoleCount = outputHoleCount;
            TargetHoleAreaCells = targetHoleAreaCells;
            OutputHoleAreaCells = outputHoleAreaCells;
            FeaturePassRatio = featurePassRatio;
            FailedFeatureIds = failedFeatureIds ?? string.Empty;
            MeanTangentErrorDegrees = meanTangentErrorDegrees;
            BoundaryVehicleCount = boundaryVehicleCount;
        }

        public bool WasEvaluated { get; }
        public string TemplateId { get; }
        public bool UsesFilledTarget { get; }
        public int TargetPixelCount { get; }
        public int OutputPixelCount { get; }
        public float IntersectionOverUnion { get; }
        public float TargetCoverage { get; }
        public float OutsideRatio { get; }
        public float BoundaryErrorCells { get; }
        public float SymmetryError { get; }
        public int TargetComponentCount { get; }
        public int OutputComponentCount { get; }
        public int TargetHoleCount { get; }
        public int OutputHoleCount { get; }
        public float TargetHoleAreaCells { get; }
        public float OutputHoleAreaCells { get; }
        public float FeaturePassRatio { get; }
        public string FailedFeatureIds { get; }
        public float MeanTangentErrorDegrees { get; }
        public int BoundaryVehicleCount { get; }
        public bool TopologyMatches =>
            TargetComponentCount == OutputComponentCount &&
            TargetHoleCount == OutputHoleCount;

        public string ToCompactString()
        {
            if (!WasEvaluated)
            {
                return "not evaluated";
            }

            return $"{TemplateId} {(UsesFilledTarget ? "filled" : "outline")}: " +
                $"IoU {IntersectionOverUnion:0.000}, coverage {TargetCoverage:0.000}, " +
                $"outside {OutsideRatio:0.000}, boundary {BoundaryErrorCells:0.00} cells, " +
                $"symmetry {SymmetryError:0.000}, topology " +
                $"{OutputComponentCount} component(s)/{OutputHoleCount} hole(s) " +
                $"({OutputHoleAreaCells:0.00} cells), " +
                $"features {FeaturePassRatio:0.00}" +
                (string.IsNullOrEmpty(FailedFeatureIds) ? string.Empty : $" ({FailedFeatureIds})") +
                $", tangent {MeanTangentErrorDegrees:0.0} degrees";
        }
    }

    public static class VehicleShapeSilhouetteQuality
    {
        private const float RasterBoardMinimumCells = -0.5f;

        public static bool TryEvaluate(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<BusDefinition> vehicles,
            out VehicleShapeSilhouetteMetrics metrics)
        {
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var targetVehicleCount = Mathf.Max(
                1,
                Mathf.Max(profile.TargetVehicleCount, vehicles != null ? vehicles.Count : 0));
            if (!VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                    out var definition))
            {
                metrics = default;
                return false;
            }

            return TryEvaluate(definition, vehicles, out metrics);
        }

        public static bool TryEvaluateHeart(
            IReadOnlyList<BusDefinition> vehicles,
            bool fillInterior,
            int thickness,
            float scale,
            out VehicleShapeSilhouetteMetrics metrics)
        {
            var definition = new VehicleShapeLayoutDefinition(
                VehicleShapeLayoutKind.Heart,
                VehicleShapeLibraryId.Heart,
                thickness,
                fillInterior,
                scale,
                true,
                0);
            return TryEvaluate(definition, vehicles, out metrics);
        }

        public static bool TryGetFailureMessage(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<BusDefinition> vehicles,
            out string message)
        {
            profile = profile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var targetVehicleCount = Mathf.Max(
                1,
                Mathf.Max(profile.TargetVehicleCount, vehicles != null ? vehicles.Count : 0));
            if (!VehicleLayoutPatternEngine.TryCreateShapeDefinition(
                    profile,
                    targetVehicleCount,
                    layoutVariantIndex,
                    out var definition) ||
                !TryEvaluate(definition, vehicles, out var metrics) ||
                !VehicleShapeTemplateCatalog.TryGetQualityTemplate(definition, out var template))
            {
                message = string.Empty;
                return false;
            }

            return TryGetFailureMessage(template, metrics, out message);
        }

        public static bool TryGetHeartFailureMessage(
            IReadOnlyList<BusDefinition> vehicles,
            bool fillInterior,
            int thickness,
            float scale,
            out string message)
        {
            var definition = new VehicleShapeLayoutDefinition(
                VehicleShapeLayoutKind.Heart,
                VehicleShapeLibraryId.Heart,
                thickness,
                fillInterior,
                scale,
                true,
                0);
            if (!TryEvaluate(definition, vehicles, out var metrics) ||
                !VehicleShapeTemplateCatalog.TryGetQualityTemplate(definition, out var template))
            {
                message = string.Empty;
                return false;
            }

            return TryGetFailureMessage(template, metrics, out message);
        }

        internal static bool TryEvaluate(
            VehicleShapeLayoutDefinition definition,
            IReadOnlyList<BusDefinition> vehicles,
            out VehicleShapeSilhouetteMetrics metrics)
        {
            metrics = default;
            if (!VehicleShapeTemplateCatalog.TryGetQualityTemplate(definition, out var template))
            {
                return false;
            }

            var constraints = template.Constraints;
            var pixelsPerCell = constraints.RasterPixelsPerCell;
            var width = BoardLayoutConfig.GridColumns * pixelsPerCell;
            var height = BoardLayoutConfig.GridRows * pixelsPerCell;
            var targetMask = new bool[width * height];
            var outputMask = new bool[width * height];
            var footprints = BuildVisualFootprints(vehicles);
            BuildTargetMask(template, definition, pixelsPerCell, width, height, targetMask);
            BuildOutputMask(
                footprints,
                constraints.PerceptionPaddingCells,
                pixelsPerCell,
                width,
                height,
                outputMask);
            ApplyPerceptionClosing(
                outputMask,
                width,
                height,
                Mathf.RoundToInt(constraints.PerceptionClosingCells * pixelsPerCell));

            var targetPixelCount = 0;
            var outputPixelCount = 0;
            var intersectionCount = 0;
            var unionCount = 0;
            var outsideCount = 0;
            for (var index = 0; index < targetMask.Length; index++)
            {
                var target = targetMask[index];
                var output = outputMask[index];
                if (target)
                {
                    targetPixelCount++;
                }

                if (output)
                {
                    outputPixelCount++;
                }

                if (target && output)
                {
                    intersectionCount++;
                }

                if (target || output)
                {
                    unionCount++;
                }

                if (output && !target)
                {
                    outsideCount++;
                }
            }

            var targetBoundary = BuildExternalBoundaryMask(targetMask, width, height);
            var outputBoundary = BuildExternalBoundaryMask(outputMask, width, height);
            var boundaryErrorCells = CalculateBoundaryErrorCells(
                targetBoundary,
                outputBoundary,
                width,
                height,
                pixelsPerCell);
            var symmetryError = CalculateSymmetryError(
                template,
                definition.Scale,
                outputMask,
                width,
                height,
                pixelsPerCell);
            var targetComponents = CountComponents(targetMask, width, height, true);
            var outputComponents = CountComponents(outputMask, width, height, true);
            var minimumHolePixelCount = Mathf.CeilToInt(
                constraints.MinimumTopologyHoleAreaCells * pixelsPerCell * pixelsPerCell);
            var targetHoles = CountEnclosedBackgroundComponents(
                targetMask,
                width,
                height,
                minimumHolePixelCount,
                out var targetHolePixels);
            var outputHoles = CountEnclosedBackgroundComponents(
                outputMask,
                width,
                height,
                minimumHolePixelCount,
                out var outputHolePixels);
            var pixelsPerCellSquared = pixelsPerCell * pixelsPerCell;
            var featurePassRatio = CalculateFeaturePassRatio(
                template,
                definition.Scale,
                outputMask,
                outputBoundary,
                width,
                height,
                pixelsPerCell,
                out var failedFeatureIds);
            CalculateMeanTangentError(
                template,
                definition,
                vehicles,
                constraints.TangentProbeDistanceCells,
                out var meanTangentError,
                out var boundaryVehicleCount);

            metrics = new VehicleShapeSilhouetteMetrics(
                template.ShapeId,
                definition.FillInterior,
                targetPixelCount,
                outputPixelCount,
                unionCount > 0 ? intersectionCount / (float)unionCount : 0f,
                targetPixelCount > 0 ? intersectionCount / (float)targetPixelCount : 0f,
                outputPixelCount > 0 ? outsideCount / (float)outputPixelCount : 1f,
                boundaryErrorCells,
                symmetryError,
                targetComponents,
                outputComponents,
                targetHoles,
                outputHoles,
                targetHolePixels / (float)pixelsPerCellSquared,
                outputHolePixels / (float)pixelsPerCellSquared,
                featurePassRatio,
                failedFeatureIds,
                meanTangentError,
                boundaryVehicleCount);
            return true;
        }

        internal static bool TryGetFailureMessage(
            IVehicleShapeTemplate template,
            VehicleShapeSilhouetteMetrics metrics,
            out string message)
        {
            message = string.Empty;
            if (template == null || !metrics.WasEvaluated || !template.Constraints.EnableSilhouetteGate)
            {
                return false;
            }

            var constraints = template.Constraints;
            var failures = new List<string>();
            if (metrics.TargetPixelCount <= 0 || metrics.OutputPixelCount <= 0)
            {
                failures.Add("target or output silhouette is empty");
            }

            if (metrics.IntersectionOverUnion < constraints.MinimumIntersectionOverUnion)
            {
                failures.Add(
                    $"IoU {metrics.IntersectionOverUnion:0.000} < {constraints.MinimumIntersectionOverUnion:0.000}");
            }

            if (metrics.TargetCoverage < constraints.MinimumTargetCoverage)
            {
                failures.Add(
                    $"coverage {metrics.TargetCoverage:0.000} < {constraints.MinimumTargetCoverage:0.000}");
            }

            if (metrics.OutsideRatio > constraints.MaximumOutsideRatio)
            {
                failures.Add(
                    $"outside {metrics.OutsideRatio:0.000} > {constraints.MaximumOutsideRatio:0.000}");
            }

            if (metrics.BoundaryErrorCells > constraints.MaximumBoundaryErrorCells)
            {
                failures.Add(
                    $"boundary P95 {metrics.BoundaryErrorCells:0.00} > {constraints.MaximumBoundaryErrorCells:0.00} cells");
            }

            if (metrics.SymmetryError > constraints.MaximumSymmetryError)
            {
                failures.Add(
                    $"symmetry {metrics.SymmetryError:0.000} > {constraints.MaximumSymmetryError:0.000}");
            }

            var componentMismatch = metrics.TargetComponentCount != metrics.OutputComponentCount;
            var outlineHoleMismatch = !metrics.UsesFilledTarget &&
                metrics.TargetHoleCount != metrics.OutputHoleCount;
            if (constraints.RequireTopologyMatch && (componentMismatch || outlineHoleMismatch))
            {
                failures.Add(
                    $"topology {metrics.OutputComponentCount} component(s)/{metrics.OutputHoleCount} hole(s), " +
                    $"expected {metrics.TargetComponentCount}/{metrics.TargetHoleCount}");
            }

            if (metrics.UsesFilledTarget &&
                constraints.MaximumFilledHoleCount >= 0 &&
                metrics.OutputHoleCount > constraints.MaximumFilledHoleCount)
            {
                failures.Add(
                    $"filled holes {metrics.OutputHoleCount} > {constraints.MaximumFilledHoleCount}");
            }

            if (metrics.UsesFilledTarget &&
                constraints.MaximumFilledHoleAreaCells >= 0f &&
                metrics.OutputHoleAreaCells > constraints.MaximumFilledHoleAreaCells)
            {
                failures.Add(
                    $"filled hole area {metrics.OutputHoleAreaCells:0.00} > " +
                    $"{constraints.MaximumFilledHoleAreaCells:0.00} cells");
            }

            if (metrics.FeaturePassRatio < constraints.MinimumFeaturePassRatio)
            {
                failures.Add(
                    $"features {metrics.FeaturePassRatio:0.00} < {constraints.MinimumFeaturePassRatio:0.00}" +
                    (string.IsNullOrEmpty(metrics.FailedFeatureIds)
                        ? string.Empty
                        : $" ({metrics.FailedFeatureIds})"));
            }

            if (metrics.BoundaryVehicleCount >= 3 &&
                metrics.MeanTangentErrorDegrees > constraints.MaximumMeanTangentErrorDegrees)
            {
                failures.Add(
                    $"outer tangent {metrics.MeanTangentErrorDegrees:0.0} > " +
                    $"{constraints.MaximumMeanTangentErrorDegrees:0.0} degrees");
            }

            if (failures.Count == 0)
            {
                return false;
            }

            message = $"Shape template {template.DisplayName} silhouette failed: {string.Join("; ", failures)}. " +
                metrics.ToCompactString();
            return true;
        }

        private static List<VehicleFootprint> BuildVisualFootprints(IReadOnlyList<BusDefinition> vehicles)
        {
            var footprints = new List<VehicleFootprint>(vehicles != null ? vehicles.Count : 0);
            if (vehicles == null)
            {
                return footprints;
            }

            for (var index = 0; index < vehicles.Count; index++)
            {
                footprints.Add(BoardLayoutConfig.GetVehicleVisualFootprintCells(vehicles[index]));
            }

            return footprints;
        }

        private static void BuildTargetMask(
            IVehicleShapeTemplate template,
            VehicleShapeLayoutDefinition definition,
            int pixelsPerCell,
            int width,
            int height,
            bool[] mask)
        {
            var outlineHalfWidth = GetOutlineBandHalfWidthCells(definition);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = PixelToBoard(x, y, pixelsPerCell);
                    if (definition.FillInterior)
                    {
                        mask[y * width + x] = template.ContainsBoardPoint(point, definition.Scale);
                        continue;
                    }

                    mask[y * width + x] = template.TryGetNearestBoundary(
                            point,
                            definition.Scale,
                            out _,
                            out _,
                            out var distanceCells) &&
                        distanceCells <= outlineHalfWidth;
                }
            }
        }

        private static float GetOutlineBandHalfWidthCells(VehicleShapeLayoutDefinition definition)
        {
            return 0.52f + (definition.Thickness - 1) * 0.42f;
        }

        private static void BuildOutputMask(
            IReadOnlyList<VehicleFootprint> footprints,
            float perceptionPaddingCells,
            int pixelsPerCell,
            int width,
            int height,
            bool[] mask)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = PixelToBoard(x, y, pixelsPerCell);
                    mask[y * width + x] = IsInsideAnyFootprint(point, footprints, perceptionPaddingCells);
                }
            }
        }

        private static bool IsInsideAnyFootprint(
            Vector2 point,
            IReadOnlyList<VehicleFootprint> footprints,
            float paddingCells)
        {
            for (var index = 0; index < footprints.Count; index++)
            {
                var footprint = footprints[index];
                var delta = point - footprint.Center;
                if (Mathf.Abs(Vector2.Dot(delta, footprint.Right)) <= footprint.HalfWidth + paddingCells &&
                    Mathf.Abs(Vector2.Dot(delta, footprint.Forward)) <= footprint.HalfLength + paddingCells)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyPerceptionClosing(
            bool[] mask,
            int width,
            int height,
            int radiusPixels)
        {
            radiusPixels = Mathf.Max(0, radiusPixels);
            if (mask == null || mask.Length == 0 || radiusPixels == 0)
            {
                return;
            }

            // Binary closing (dilate, then erode) joins only gaps smaller than the
            // configured perceptual radius. It deliberately does not enlarge the final
            // outer silhouette, so vehicles outside the authored contour still count as
            // outside pixels and cannot be hidden by this smoothing step.
            var dilated = new bool[mask.Length];
            var radiusSquared = radiusPixels * radiusPixels;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = false;
                    for (var offsetY = -radiusPixels; offsetY <= radiusPixels && !value; offsetY++)
                    {
                        var sampleY = y + offsetY;
                        if (sampleY < 0 || sampleY >= height)
                        {
                            continue;
                        }

                        for (var offsetX = -radiusPixels; offsetX <= radiusPixels; offsetX++)
                        {
                            if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
                            {
                                continue;
                            }

                            var sampleX = x + offsetX;
                            if (sampleX >= 0 && sampleX < width && mask[sampleY * width + sampleX])
                            {
                                value = true;
                                break;
                            }
                        }
                    }

                    dilated[y * width + x] = value;
                }
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = true;
                    for (var offsetY = -radiusPixels; offsetY <= radiusPixels && value; offsetY++)
                    {
                        var sampleY = y + offsetY;
                        for (var offsetX = -radiusPixels; offsetX <= radiusPixels; offsetX++)
                        {
                            if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
                            {
                                continue;
                            }

                            var sampleX = x + offsetX;
                            if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height ||
                                !dilated[sampleY * width + sampleX])
                            {
                                value = false;
                                break;
                            }
                        }
                    }

                    mask[y * width + x] = value;
                }
            }
        }

        private static bool[] BuildBoundaryMask(bool[] foreground, int width, int height)
        {
            var boundary = new bool[foreground.Length];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    if (!foreground[index])
                    {
                        continue;
                    }

                    boundary[index] = x == 0 || x == width - 1 || y == 0 || y == height - 1 ||
                        !foreground[index - 1] ||
                        !foreground[index + 1] ||
                        !foreground[index - width] ||
                        !foreground[index + width];
                }
            }

            return boundary;
        }

        private static bool[] BuildExternalBoundaryMask(bool[] foreground, int width, int height)
        {
            var exterior = new bool[foreground.Length];
            var queue = new int[foreground.Length];
            var read = 0;
            var write = 0;
            for (var x = 0; x < width; x++)
            {
                TryQueueExterior(x, foreground, exterior, queue, ref write);
                TryQueueExterior((height - 1) * width + x, foreground, exterior, queue, ref write);
            }

            for (var y = 0; y < height; y++)
            {
                TryQueueExterior(y * width, foreground, exterior, queue, ref write);
                TryQueueExterior(y * width + width - 1, foreground, exterior, queue, ref write);
            }

            while (read < write)
            {
                var index = queue[read++];
                var x = index % width;
                var y = index / width;
                if (x > 0)
                {
                    TryQueueExterior(index - 1, foreground, exterior, queue, ref write);
                }

                if (x < width - 1)
                {
                    TryQueueExterior(index + 1, foreground, exterior, queue, ref write);
                }

                if (y > 0)
                {
                    TryQueueExterior(index - width, foreground, exterior, queue, ref write);
                }

                if (y < height - 1)
                {
                    TryQueueExterior(index + width, foreground, exterior, queue, ref write);
                }
            }

            var boundary = new bool[foreground.Length];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    if (!foreground[index])
                    {
                        continue;
                    }

                    boundary[index] = x == 0 || x == width - 1 || y == 0 || y == height - 1 ||
                        exterior[index - 1] ||
                        exterior[index + 1] ||
                        exterior[index - width] ||
                        exterior[index + width];
                }
            }

            return boundary;
        }

        private static void TryQueueExterior(
            int index,
            bool[] foreground,
            bool[] exterior,
            int[] queue,
            ref int write)
        {
            if (foreground[index] || exterior[index])
            {
                return;
            }

            exterior[index] = true;
            queue[write++] = index;
        }

        private static float CalculateBoundaryErrorCells(
            bool[] targetBoundary,
            bool[] outputBoundary,
            int width,
            int height,
            int pixelsPerCell)
        {
            var targetPoints = CollectMaskPoints(targetBoundary, width, height);
            var outputPoints = CollectMaskPoints(outputBoundary, width, height);
            if (targetPoints.Count == 0 || outputPoints.Count == 0)
            {
                return Mathf.Max(BoardLayoutConfig.GridColumns, BoardLayoutConfig.GridRows);
            }

            var distances = new List<float>(targetPoints.Count + outputPoints.Count);
            AppendNearestDistances(targetPoints, outputPoints, pixelsPerCell, distances);
            AppendNearestDistances(outputPoints, targetPoints, pixelsPerCell, distances);
            distances.Sort();
            var percentileIndex = Mathf.Clamp(
                Mathf.CeilToInt(distances.Count * 0.95f) - 1,
                0,
                distances.Count - 1);
            return distances[percentileIndex];
        }

        private static List<Vector2Int> CollectMaskPoints(bool[] mask, int width, int height)
        {
            var points = new List<Vector2Int>();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (mask[y * width + x])
                    {
                        points.Add(new Vector2Int(x, y));
                    }
                }
            }

            return points;
        }

        private static void AppendNearestDistances(
            IReadOnlyList<Vector2Int> source,
            IReadOnlyList<Vector2Int> target,
            int pixelsPerCell,
            List<float> distances)
        {
            for (var sourceIndex = 0; sourceIndex < source.Count; sourceIndex++)
            {
                var bestSquared = float.MaxValue;
                for (var targetIndex = 0; targetIndex < target.Count; targetIndex++)
                {
                    var delta = source[sourceIndex] - target[targetIndex];
                    bestSquared = Mathf.Min(bestSquared, delta.sqrMagnitude);
                }

                distances.Add(Mathf.Sqrt(bestSquared) / pixelsPerCell);
            }
        }

        private static float CalculateSymmetryError(
            IVehicleShapeTemplate template,
            float shapeScale,
            bool[] mask,
            int width,
            int height,
            int pixelsPerCell)
        {
            var totalError = 0f;
            var symmetryCount = 0;
            if ((template.Symmetry & VehicleShapeTemplateSymmetry.MirrorX) != 0)
            {
                totalError += CalculateAxisSymmetryError(
                    mask,
                    width,
                    height,
                    pixelsPerCell,
                    template.GetProjectionCenterCells(),
                    true);
                symmetryCount++;
            }

            if ((template.Symmetry & VehicleShapeTemplateSymmetry.MirrorY) != 0)
            {
                totalError += CalculateAxisSymmetryError(
                    mask,
                    width,
                    height,
                    pixelsPerCell,
                    template.GetProjectionCenterCells(),
                    false);
                symmetryCount++;
            }

            return symmetryCount > 0 ? totalError / symmetryCount : 0f;
        }

        private static float CalculateAxisSymmetryError(
            bool[] mask,
            int width,
            int height,
            int pixelsPerCell,
            Vector2 center,
            bool mirrorX)
        {
            var mismatch = 0;
            var union = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = PixelToBoard(x, y, pixelsPerCell);
                    var mirrored = mirrorX
                        ? new Vector2(center.x * 2f - point.x, point.y)
                        : new Vector2(point.x, center.y * 2f - point.y);
                    var mirroredX = BoardToPixelIndex(mirrored.x, pixelsPerCell, width);
                    var mirroredY = BoardToPixelIndex(mirrored.y, pixelsPerCell, height);
                    var value = mask[y * width + x];
                    var mirrorValue = mirroredX >= 0 && mirroredY >= 0 &&
                        mask[mirroredY * width + mirroredX];
                    if (value || mirrorValue)
                    {
                        union++;
                    }

                    if (value != mirrorValue)
                    {
                        mismatch++;
                    }
                }
            }

            return union > 0 ? mismatch / (float)union : 0f;
        }

        private static int CountComponents(bool[] mask, int width, int height, bool targetValue)
        {
            var visited = new bool[mask.Length];
            var queue = new int[mask.Length];
            var componentCount = 0;
            for (var start = 0; start < mask.Length; start++)
            {
                if (visited[start] || mask[start] != targetValue)
                {
                    continue;
                }

                componentCount++;
                FloodFill(mask, width, height, targetValue, start, visited, queue, out _, out _);
            }

            return componentCount;
        }

        private static int CountEnclosedBackgroundComponents(
            bool[] foreground,
            int width,
            int height,
            int minimumPixelCount,
            out int qualifyingPixelCount)
        {
            var visited = new bool[foreground.Length];
            var queue = new int[foreground.Length];
            var holes = 0;
            qualifyingPixelCount = 0;
            for (var start = 0; start < foreground.Length; start++)
            {
                if (visited[start] || foreground[start])
                {
                    continue;
                }

                FloodFill(
                    foreground,
                    width,
                    height,
                    false,
                    start,
                    visited,
                    queue,
                    out var touchesBoundary,
                    out var pixelCount);
                if (!touchesBoundary && pixelCount >= Mathf.Max(1, minimumPixelCount))
                {
                    holes++;
                    qualifyingPixelCount += pixelCount;
                }
            }

            return holes;
        }

        private static void FloodFill(
            bool[] mask,
            int width,
            int height,
            bool targetValue,
            int start,
            bool[] visited,
            int[] queue,
            out bool touchesBoundary,
            out int pixelCount)
        {
            var read = 0;
            var write = 0;
            queue[write++] = start;
            visited[start] = true;
            touchesBoundary = false;
            pixelCount = 0;
            while (read < write)
            {
                var index = queue[read++];
                pixelCount++;
                var x = index % width;
                var y = index / width;
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    touchesBoundary = true;
                }

                TryQueueNeighbour(index - 1, x > 0, mask, targetValue, visited, queue, ref write);
                TryQueueNeighbour(index + 1, x < width - 1, mask, targetValue, visited, queue, ref write);
                TryQueueNeighbour(index - width, y > 0, mask, targetValue, visited, queue, ref write);
                TryQueueNeighbour(index + width, y < height - 1, mask, targetValue, visited, queue, ref write);
            }
        }

        private static void TryQueueNeighbour(
            int index,
            bool inBounds,
            bool[] mask,
            bool targetValue,
            bool[] visited,
            int[] queue,
            ref int write)
        {
            if (!inBounds || visited[index] || mask[index] != targetValue)
            {
                return;
            }

            visited[index] = true;
            queue[write++] = index;
        }

        private static float CalculateFeaturePassRatio(
            IVehicleShapeTemplate template,
            float shapeScale,
            bool[] outputMask,
            bool[] outputBoundary,
            int width,
            int height,
            int pixelsPerCell,
            out string failedFeatureIds)
        {
            var features = template.KeyFeatures;
            if (features == null || features.Count == 0)
            {
                failedFeatureIds = string.Empty;
                return 1f;
            }

            var halfExtents = template.GetProjectionHalfExtentsCells(shapeScale);
            var passed = 0;
            var failed = new List<string>();
            for (var featureIndex = 0; featureIndex < features.Count; featureIndex++)
            {
                var feature = features[featureIndex];
                if (feature == null)
                {
                    passed++;
                    continue;
                }

                var center = template.NormalizedToBoard(feature.NormalizedPosition, shapeScale);
                var radiusCells = Mathf.Max(
                    1f / pixelsPerCell,
                    feature.RadiusNormalized * Mathf.Min(halfExtents.x, halfExtents.y) * 2f);
                if (FeaturePasses(
                    feature,
                    center,
                    radiusCells,
                    outputMask,
                    outputBoundary,
                    width,
                    height,
                    pixelsPerCell))
                {
                    passed++;
                }
                else
                {
                    failed.Add(feature.Id);
                }
            }

            failedFeatureIds = string.Join(",", failed);
            return passed / (float)features.Count;
        }

        private static bool FeaturePasses(
            VehicleShapeKeyFeature feature,
            Vector2 center,
            float radiusCells,
            bool[] outputMask,
            bool[] outputBoundary,
            int width,
            int height,
            int pixelsPerCell)
        {
            if (feature.Expectation == VehicleShapeFeatureExpectation.Boundary)
            {
                var nearestDistanceCells = float.PositiveInfinity;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (!outputBoundary[y * width + x])
                        {
                            continue;
                        }

                        nearestDistanceCells = Mathf.Min(
                            nearestDistanceCells,
                            Vector2.Distance(PixelToBoard(x, y, pixelsPerCell), center));
                    }
                }

                if (float.IsPositiveInfinity(nearestDistanceCells))
                {
                    return false;
                }

                var pixelToleranceCells = 1f / pixelsPerCell;
                var excessDistanceCells = Mathf.Max(0f, nearestDistanceCells - pixelToleranceCells);
                var proximityScore = 1f - Mathf.Clamp01(
                    excessDistanceCells / Mathf.Max(radiusCells, pixelToleranceCells));
                return proximityScore >= feature.RequiredCoverage;
            }

            var sampleCount = 0;
            var matchingCount = 0;
            var radiusSquared = radiusCells * radiusCells;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = PixelToBoard(x, y, pixelsPerCell);
                    if ((point - center).sqrMagnitude > radiusSquared)
                    {
                        continue;
                    }

                    var index = y * width + x;
                    sampleCount++;
                    var matches = feature.Expectation == VehicleShapeFeatureExpectation.Foreground
                        ? outputMask[index]
                        : !outputMask[index];
                    if (matches)
                    {
                        matchingCount++;
                    }
                }
            }

            return sampleCount > 0 &&
                matchingCount / (float)sampleCount >= feature.RequiredCoverage;
        }

        private static void CalculateMeanTangentError(
            IVehicleShapeTemplate template,
            VehicleShapeLayoutDefinition definition,
            IReadOnlyList<BusDefinition> vehicles,
            float tangentProbeDistanceCells,
            out float meanErrorDegrees,
            out int boundaryVehicleCount)
        {
            var sum = 0f;
            boundaryVehicleCount = 0;
            if (vehicles == null)
            {
                meanErrorDegrees = 0f;
                return;
            }

            for (var index = 0; index < vehicles.Count; index++)
            {
                var vehicle = vehicles[index];
                var rootPosition = new Vector2(
                    vehicle.GridPosition.x + vehicle.PositionOffsetCells.x,
                    vehicle.GridPosition.y + vehicle.PositionOffsetCells.y);
                if (!VehicleShapeLayoutEngine.TryFindNearestShapeCell(
                        definition,
                        rootPosition,
                        out var nearestCell,
                        out var cellDistance) ||
                    nearestCell.Role != VehicleShapeCellRole.Outline ||
                    cellDistance > 0.85f ||
                    !template.TryGetNearestBoundary(
                        rootPosition,
                        definition.Scale,
                        out _,
                        out var tangent,
                        out var boundaryDistance) ||
                    boundaryDistance > tangentProbeDistanceCells)
                {
                    continue;
                }

                var yawRadians = vehicle.YawDegrees * Mathf.Deg2Rad;
                var vehicleAxis = new Vector2(Mathf.Sin(yawRadians), Mathf.Cos(yawRadians));
                var axisDot = Mathf.Clamp01(Mathf.Abs(Vector2.Dot(vehicleAxis.normalized, tangent.normalized)));
                sum += Mathf.Acos(axisDot) * Mathf.Rad2Deg;
                boundaryVehicleCount++;
            }

            meanErrorDegrees = boundaryVehicleCount > 0 ? sum / boundaryVehicleCount : 0f;
        }

        private static Vector2 PixelToBoard(int x, int y, int pixelsPerCell)
        {
            return new Vector2(
                RasterBoardMinimumCells + (x + 0.5f) / pixelsPerCell,
                RasterBoardMinimumCells + (y + 0.5f) / pixelsPerCell);
        }

        private static int BoardToPixelIndex(float value, int pixelsPerCell, int limit)
        {
            var index = Mathf.FloorToInt((value - RasterBoardMinimumCells) * pixelsPerCell);
            return index >= 0 && index < limit ? index : -1;
        }
    }
}
