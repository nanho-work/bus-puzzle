using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BusPuzzle
{
    public interface IVehicleShapeTemplate
    {
        string ShapeId { get; }
        string DisplayName { get; }
        VehicleShapeTemplateSymmetry Symmetry { get; }
        IReadOnlyList<VehicleShapeContour> Contours { get; }
        IReadOnlyList<VehicleShapeKeyFeature> KeyFeatures { get; }
        VehicleShapeTemplateConstraints Constraints { get; }
        bool IsUsable { get; }

        Vector2 GetProjectionCenterCells();
        Vector2 GetProjectionHalfExtentsCells(float shapeScale);
        Vector2 NormalizedToBoard(Vector2 normalizedPosition, float shapeScale);
        Vector2 BoardToNormalized(Vector2 boardPosition, float shapeScale);
        bool ContainsBoardPoint(Vector2 boardPosition, float shapeScale);
        bool ContainsNormalizedPoint(Vector2 normalizedPosition);
        bool TryGetNearestBoundary(
            Vector2 boardPosition,
            float shapeScale,
            out Vector2 nearestPoint,
            out Vector2 tangent,
            out float distanceCells);
    }

    [Flags]
    public enum VehicleShapeTemplateSymmetry
    {
        None = 0,
        MirrorX = 1,
        MirrorY = 2
    }

    public enum VehicleShapeContourOperation
    {
        Additive = 0,
        Subtractive = 1
    }

    public enum VehicleShapeFeatureExpectation
    {
        Foreground = 0,
        Background = 1,
        Boundary = 2
    }

    [Serializable]
    public sealed class VehicleShapeContour
    {
        [SerializeField] private VehicleShapeContourOperation operation = VehicleShapeContourOperation.Additive;
        [SerializeField] private List<Vector2> points = new List<Vector2>();
        private IReadOnlyList<Vector2> readOnlyPoints;

        public VehicleShapeContour()
        {
        }

        internal VehicleShapeContour(VehicleShapeContour source)
        {
            operation = source != null
                ? source.Operation
                : VehicleShapeContourOperation.Additive;
            points = source != null && source.Points != null
                ? new List<Vector2>(source.Points)
                : new List<Vector2>();
            readOnlyPoints = points.AsReadOnly();
        }

        public VehicleShapeContourOperation Operation => operation;
        public IReadOnlyList<Vector2> Points => readOnlyPoints ?? points;
        public bool IsUsable => points != null && points.Count >= 3;
    }

    [Serializable]
    public sealed class VehicleShapeKeyFeature
    {
        [SerializeField] private string id = "feature";
        [SerializeField] private Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);
        [SerializeField, Min(0.005f)] private float radiusNormalized = 0.06f;
        [SerializeField] private VehicleShapeFeatureExpectation expectation = VehicleShapeFeatureExpectation.Boundary;
        [SerializeField, Range(0f, 1f)] private float requiredCoverage = 0.8f;

        public VehicleShapeKeyFeature()
        {
        }

        internal VehicleShapeKeyFeature(VehicleShapeKeyFeature source)
        {
            id = source != null ? source.Id : "feature";
            normalizedPosition = source != null
                ? source.NormalizedPosition
                : new Vector2(0.5f, 0.5f);
            radiusNormalized = source != null
                ? source.RadiusNormalized
                : 0.06f;
            expectation = source != null
                ? source.Expectation
                : VehicleShapeFeatureExpectation.Boundary;
            requiredCoverage = source != null
                ? source.RequiredCoverage
                : 0.8f;
        }

        public string Id => string.IsNullOrWhiteSpace(id) ? "feature" : id;
        public Vector2 NormalizedPosition => normalizedPosition;
        public float RadiusNormalized => Mathf.Max(0.005f, radiusNormalized);
        public VehicleShapeFeatureExpectation Expectation => expectation;
        public float RequiredCoverage => Mathf.Clamp01(requiredCoverage);
    }

    [Serializable]
    public sealed class VehicleShapeTemplateConstraints
    {
        [SerializeField] private bool enableSilhouetteGate = true;
        [SerializeField, Range(2, 12)] private int rasterPixelsPerCell = 4;
        [SerializeField, Range(0f, 0.5f)] private float perceptionPaddingCells = 0.12f;
        [Tooltip("Closes small gaps between neighboring vehicle silhouettes before perceptual grading. " +
            "This models how players read a packed group of cars as one shape without changing collisions.")]
        [SerializeField, Range(0f, 1f)] private float perceptionClosingCells;
        [SerializeField, Range(0f, 1f)] private float minimumIntersectionOverUnion = 0.68f;
        [SerializeField, Range(0f, 1f)] private float minimumTargetCoverage = 0.78f;
        [SerializeField, Range(0f, 1f)] private float maximumOutsideRatio = 0.18f;
        [SerializeField, Min(0f)] private float maximumBoundaryErrorCells = 0.90f;
        [SerializeField, Range(0f, 1f)] private float maximumSymmetryError = 0.18f;
        [SerializeField] private bool requireTopologyMatch = true;
        [Tooltip("Enclosed gaps smaller than this area are treated as normal spacing between vehicles.")]
        [SerializeField, Range(0f, 4f)] private float minimumTopologyHoleAreaCells = 1.5f;
        [Tooltip("For filled silhouettes, reject layouts with more enclosed gaps than this. Use -1 to disable.")]
        [SerializeField, Min(-1)] private int maximumFilledHoleCount = -1;
        [Tooltip("For filled silhouettes, reject layouts whose qualifying enclosed gaps exceed this combined area. Use -1 to disable.")]
        [SerializeField, Min(-1f)] private float maximumFilledHoleAreaCells = -1f;
        [SerializeField, Range(0f, 1f)] private float minimumFeaturePassRatio = 1f;
        [SerializeField, Range(0f, 90f)] private float maximumMeanTangentErrorDegrees = 38f;
        [SerializeField, Range(0.1f, 3f)] private float tangentProbeDistanceCells = 1.15f;

        public VehicleShapeTemplateConstraints()
        {
        }

        internal VehicleShapeTemplateConstraints(
            VehicleShapeTemplateConstraints source)
        {
            if (source == null)
            {
                return;
            }

            enableSilhouetteGate = source.EnableSilhouetteGate;
            rasterPixelsPerCell = source.RasterPixelsPerCell;
            perceptionPaddingCells = source.PerceptionPaddingCells;
            perceptionClosingCells = source.PerceptionClosingCells;
            minimumIntersectionOverUnion =
                source.MinimumIntersectionOverUnion;
            minimumTargetCoverage = source.MinimumTargetCoverage;
            maximumOutsideRatio = source.MaximumOutsideRatio;
            maximumBoundaryErrorCells = source.MaximumBoundaryErrorCells;
            maximumSymmetryError = source.MaximumSymmetryError;
            requireTopologyMatch = source.RequireTopologyMatch;
            minimumTopologyHoleAreaCells =
                source.MinimumTopologyHoleAreaCells;
            maximumFilledHoleCount = source.MaximumFilledHoleCount;
            maximumFilledHoleAreaCells =
                source.MaximumFilledHoleAreaCells;
            minimumFeaturePassRatio = source.MinimumFeaturePassRatio;
            maximumMeanTangentErrorDegrees =
                source.MaximumMeanTangentErrorDegrees;
            tangentProbeDistanceCells =
                source.TangentProbeDistanceCells;
        }

        public bool EnableSilhouetteGate => enableSilhouetteGate;
        public int RasterPixelsPerCell => Mathf.Clamp(rasterPixelsPerCell, 2, 12);
        public float PerceptionPaddingCells => Mathf.Clamp(perceptionPaddingCells, 0f, 0.5f);
        public float PerceptionClosingCells => Mathf.Clamp(perceptionClosingCells, 0f, 1f);
        public float MinimumIntersectionOverUnion => Mathf.Clamp01(minimumIntersectionOverUnion);
        public float MinimumTargetCoverage => Mathf.Clamp01(minimumTargetCoverage);
        public float MaximumOutsideRatio => Mathf.Clamp01(maximumOutsideRatio);
        public float MaximumBoundaryErrorCells => Mathf.Max(0f, maximumBoundaryErrorCells);
        public float MaximumSymmetryError => Mathf.Clamp01(maximumSymmetryError);
        public bool RequireTopologyMatch => requireTopologyMatch;
        public float MinimumTopologyHoleAreaCells => Mathf.Clamp(minimumTopologyHoleAreaCells, 0f, 4f);
        public int MaximumFilledHoleCount => Mathf.Max(-1, maximumFilledHoleCount);
        public float MaximumFilledHoleAreaCells => Mathf.Max(-1f, maximumFilledHoleAreaCells);
        public float MinimumFeaturePassRatio => Mathf.Clamp01(minimumFeaturePassRatio);
        public float MaximumMeanTangentErrorDegrees => Mathf.Clamp(maximumMeanTangentErrorDegrees, 0f, 90f);
        public float TangentProbeDistanceCells => Mathf.Clamp(tangentProbeDistanceCells, 0.1f, 3f);
    }

    [CreateAssetMenu(menuName = "Bus Puzzle/Vehicle Shape Template", fileName = "VehicleShapeTemplate")]
    public sealed class VehicleShapeTemplate :
        ScriptableObject,
        IVehicleShapeTemplate
    {
        [SerializeField] private string shapeId = "shape";
        [SerializeField] private string displayName = "Shape";
        [SerializeField] private VehicleShapeTemplateSymmetry symmetry = VehicleShapeTemplateSymmetry.None;
        [Tooltip("Offset from the board center, expressed as a fraction of the safe board half-extents.")]
        [SerializeField] private Vector2 normalizedCenterOffset = Vector2.zero;
        [Tooltip("Contour size relative to the safe board area before the generated shape scale is applied.")]
        [SerializeField] private Vector2 normalizedScale = new Vector2(0.92f, 0.92f);
        [SerializeField] private List<VehicleShapeContour> contours = new List<VehicleShapeContour>();
        [SerializeField] private List<VehicleShapeKeyFeature> keyFeatures = new List<VehicleShapeKeyFeature>();
        [SerializeField] private VehicleShapeTemplateConstraints constraints = new VehicleShapeTemplateConstraints();

        public string ShapeId => string.IsNullOrWhiteSpace(shapeId) ? name : shapeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ShapeId : displayName;
        public VehicleShapeTemplateSymmetry Symmetry => symmetry;
        public IReadOnlyList<VehicleShapeContour> Contours => contours;
        public IReadOnlyList<VehicleShapeKeyFeature> KeyFeatures => keyFeatures;
        public VehicleShapeTemplateConstraints Constraints => constraints ?? new VehicleShapeTemplateConstraints();

        internal IVehicleShapeTemplate CreateSnapshot()
        {
            return new VehicleShapeTemplateSnapshot(
                ShapeId,
                DisplayName,
                symmetry,
                normalizedCenterOffset,
                normalizedScale,
                contours,
                keyFeatures,
                Constraints);
        }

        public bool IsUsable
        {
            get
            {
                if (contours == null)
                {
                    return false;
                }

                for (var index = 0; index < contours.Count; index++)
                {
                    if (contours[index] != null &&
                        contours[index].Operation == VehicleShapeContourOperation.Additive &&
                        contours[index].IsUsable)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Vector2 GetProjectionCenterCells()
        {
            var boardCenter = new Vector2(
                (BoardLayoutConfig.GridColumns - 1) * 0.5f,
                (BoardLayoutConfig.GridRows - 1) * 0.5f);
            return boardCenter + Vector2.Scale(normalizedCenterOffset, GetSafeBoardHalfExtentsCells());
        }

        public Vector2 GetProjectionHalfExtentsCells(float shapeScale)
        {
            var safeScale = new Vector2(
                Mathf.Clamp(normalizedScale.x, 0.1f, 1.25f),
                Mathf.Clamp(normalizedScale.y, 0.1f, 1.25f));
            return Vector2.Scale(GetSafeBoardHalfExtentsCells(), safeScale) * Mathf.Clamp(shapeScale, 0.5f, 1.25f);
        }

        public Vector2 NormalizedToBoard(Vector2 normalizedPosition, float shapeScale)
        {
            var centered = (normalizedPosition - Vector2.one * 0.5f) * 2f;
            return GetProjectionCenterCells() + Vector2.Scale(centered, GetProjectionHalfExtentsCells(shapeScale));
        }

        public Vector2 BoardToNormalized(Vector2 boardPosition, float shapeScale)
        {
            var halfExtents = GetProjectionHalfExtentsCells(shapeScale);
            var centered = boardPosition - GetProjectionCenterCells();
            return new Vector2(
                centered.x / Mathf.Max(0.001f, halfExtents.x) * 0.5f + 0.5f,
                centered.y / Mathf.Max(0.001f, halfExtents.y) * 0.5f + 0.5f);
        }

        public bool ContainsBoardPoint(Vector2 boardPosition, float shapeScale)
        {
            return ContainsNormalizedPoint(BoardToNormalized(boardPosition, shapeScale));
        }

        public bool ContainsNormalizedPoint(Vector2 normalizedPosition)
        {
            if (!IsUsable)
            {
                return false;
            }

            var insideAdditive = false;
            for (var index = 0; index < contours.Count; index++)
            {
                var contour = contours[index];
                if (contour == null || !contour.IsUsable ||
                    contour.Operation != VehicleShapeContourOperation.Additive)
                {
                    continue;
                }

                if (IsInsidePolygon(normalizedPosition, contour.Points))
                {
                    insideAdditive = true;
                    break;
                }
            }

            if (!insideAdditive)
            {
                return false;
            }

            for (var index = 0; index < contours.Count; index++)
            {
                var contour = contours[index];
                if (contour != null && contour.IsUsable &&
                    contour.Operation == VehicleShapeContourOperation.Subtractive &&
                    IsInsidePolygon(normalizedPosition, contour.Points))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryGetNearestBoundary(
            Vector2 boardPosition,
            float shapeScale,
            out Vector2 nearestPoint,
            out Vector2 tangent,
            out float distanceCells)
        {
            nearestPoint = default;
            tangent = Vector2.right;
            distanceCells = float.MaxValue;
            if (!IsUsable)
            {
                return false;
            }

            var found = false;
            for (var contourIndex = 0; contourIndex < contours.Count; contourIndex++)
            {
                var contour = contours[contourIndex];
                if (contour == null || !contour.IsUsable)
                {
                    continue;
                }

                var points = contour.Points;
                var previous = NormalizedToBoard(points[points.Count - 1], shapeScale);
                for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
                {
                    var current = NormalizedToBoard(points[pointIndex], shapeScale);
                    var segment = current - previous;
                    var lengthSquared = segment.sqrMagnitude;
                    if (lengthSquared <= 0.000001f)
                    {
                        previous = current;
                        continue;
                    }

                    var progress = Mathf.Clamp01(Vector2.Dot(boardPosition - previous, segment) / lengthSquared);
                    var candidate = previous + segment * progress;
                    var candidateDistance = Vector2.Distance(boardPosition, candidate);
                    if (candidateDistance < distanceCells)
                    {
                        nearestPoint = candidate;
                        tangent = segment.normalized;
                        distanceCells = candidateDistance;
                        found = true;
                    }

                    previous = current;
                }
            }

            return found;
        }

        private static Vector2 GetSafeBoardHalfExtentsCells()
        {
            return new Vector2(
                Mathf.Max(1f, (BoardLayoutConfig.GridColumns - 2) * 0.5f),
                Mathf.Max(1f, (BoardLayoutConfig.GridRows - 2) * 0.5f));
        }

        private static bool IsInsidePolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            var inside = false;
            var previous = polygon[polygon.Count - 1];
            for (var index = 0; index < polygon.Count; index++)
            {
                var current = polygon[index];
                if ((current.y > point.y) != (previous.y > point.y))
                {
                    var denominator = previous.y - current.y;
                    if (Mathf.Abs(denominator) > 0.000001f)
                    {
                        var crossingX = (previous.x - current.x) * (point.y - current.y) / denominator + current.x;
                        if (point.x < crossingX)
                        {
                            inside = !inside;
                        }
                    }
                }

                previous = current;
            }

            return inside;
        }
    }

    internal sealed class VehicleShapeTemplateSnapshot :
        IVehicleShapeTemplate
    {
        private readonly Vector2 normalizedCenterOffset;
        private readonly Vector2 normalizedScale;
        private readonly IReadOnlyList<VehicleShapeContour> contours;
        private readonly IReadOnlyList<VehicleShapeKeyFeature> keyFeatures;

        public VehicleShapeTemplateSnapshot(
            string shapeId,
            string displayName,
            VehicleShapeTemplateSymmetry symmetry,
            Vector2 normalizedCenterOffset,
            Vector2 normalizedScale,
            IReadOnlyList<VehicleShapeContour> sourceContours,
            IReadOnlyList<VehicleShapeKeyFeature> sourceKeyFeatures,
            VehicleShapeTemplateConstraints sourceConstraints)
        {
            ShapeId = string.IsNullOrWhiteSpace(shapeId)
                ? "shape"
                : shapeId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? ShapeId
                : displayName;
            Symmetry = symmetry;
            this.normalizedCenterOffset = normalizedCenterOffset;
            this.normalizedScale = normalizedScale;

            var contourCopies = new List<VehicleShapeContour>(
                sourceContours != null ? sourceContours.Count : 0);
            if (sourceContours != null)
            {
                for (var index = 0;
                    index < sourceContours.Count;
                    index++)
                {
                    contourCopies.Add(
                        sourceContours[index] != null
                            ? new VehicleShapeContour(
                                sourceContours[index])
                            : null);
                }
            }

            contours = contourCopies.AsReadOnly();

            var featureCopies = new List<VehicleShapeKeyFeature>(
                sourceKeyFeatures != null
                    ? sourceKeyFeatures.Count
                    : 0);
            if (sourceKeyFeatures != null)
            {
                for (var index = 0;
                    index < sourceKeyFeatures.Count;
                    index++)
                {
                    featureCopies.Add(
                        sourceKeyFeatures[index] != null
                            ? new VehicleShapeKeyFeature(
                                sourceKeyFeatures[index])
                            : null);
                }
            }

            keyFeatures = featureCopies.AsReadOnly();
            Constraints =
                new VehicleShapeTemplateConstraints(
                    sourceConstraints);
        }

        public string ShapeId { get; }
        public string DisplayName { get; }
        public VehicleShapeTemplateSymmetry Symmetry { get; }
        public IReadOnlyList<VehicleShapeContour> Contours =>
            contours;
        public IReadOnlyList<VehicleShapeKeyFeature> KeyFeatures =>
            keyFeatures;
        public VehicleShapeTemplateConstraints Constraints { get; }

        public bool IsUsable
        {
            get
            {
                for (var index = 0;
                    index < contours.Count;
                    index++)
                {
                    if (contours[index] != null &&
                        contours[index].Operation ==
                            VehicleShapeContourOperation.Additive &&
                        contours[index].IsUsable)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Vector2 GetProjectionCenterCells()
        {
            var boardCenter = new Vector2(
                (BoardLayoutConfig.GridColumns - 1) * 0.5f,
                (BoardLayoutConfig.GridRows - 1) * 0.5f);
            return boardCenter +
                Vector2.Scale(
                    normalizedCenterOffset,
                    GetSafeBoardHalfExtentsCells());
        }

        public Vector2 GetProjectionHalfExtentsCells(
            float shapeScale)
        {
            var safeScale = new Vector2(
                Mathf.Clamp(
                    normalizedScale.x,
                    0.1f,
                    1.25f),
                Mathf.Clamp(
                    normalizedScale.y,
                    0.1f,
                    1.25f));
            return Vector2.Scale(
                    GetSafeBoardHalfExtentsCells(),
                    safeScale) *
                Mathf.Clamp(shapeScale, 0.5f, 1.25f);
        }

        public Vector2 NormalizedToBoard(
            Vector2 normalizedPosition,
            float shapeScale)
        {
            var centered =
                (normalizedPosition - Vector2.one * 0.5f) * 2f;
            return GetProjectionCenterCells() +
                Vector2.Scale(
                    centered,
                    GetProjectionHalfExtentsCells(shapeScale));
        }

        public Vector2 BoardToNormalized(
            Vector2 boardPosition,
            float shapeScale)
        {
            var halfExtents =
                GetProjectionHalfExtentsCells(shapeScale);
            var centered =
                boardPosition - GetProjectionCenterCells();
            return new Vector2(
                centered.x /
                    Mathf.Max(0.001f, halfExtents.x) *
                    0.5f +
                    0.5f,
                centered.y /
                    Mathf.Max(0.001f, halfExtents.y) *
                    0.5f +
                    0.5f);
        }

        public bool ContainsBoardPoint(
            Vector2 boardPosition,
            float shapeScale)
        {
            return ContainsNormalizedPoint(
                BoardToNormalized(
                    boardPosition,
                    shapeScale));
        }

        public bool ContainsNormalizedPoint(
            Vector2 normalizedPosition)
        {
            if (!IsUsable)
            {
                return false;
            }

            var insideAdditive = false;
            for (var index = 0;
                index < contours.Count;
                index++)
            {
                var contour = contours[index];
                if (contour == null ||
                    !contour.IsUsable ||
                    contour.Operation !=
                        VehicleShapeContourOperation.Additive)
                {
                    continue;
                }

                if (IsInsidePolygon(
                        normalizedPosition,
                        contour.Points))
                {
                    insideAdditive = true;
                    break;
                }
            }

            if (!insideAdditive)
            {
                return false;
            }

            for (var index = 0;
                index < contours.Count;
                index++)
            {
                var contour = contours[index];
                if (contour != null &&
                    contour.IsUsable &&
                    contour.Operation ==
                        VehicleShapeContourOperation.Subtractive &&
                    IsInsidePolygon(
                        normalizedPosition,
                        contour.Points))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryGetNearestBoundary(
            Vector2 boardPosition,
            float shapeScale,
            out Vector2 nearestPoint,
            out Vector2 tangent,
            out float distanceCells)
        {
            nearestPoint = default;
            tangent = Vector2.right;
            distanceCells = float.MaxValue;
            if (!IsUsable)
            {
                return false;
            }

            var found = false;
            for (var contourIndex = 0;
                contourIndex < contours.Count;
                contourIndex++)
            {
                var contour = contours[contourIndex];
                if (contour == null || !contour.IsUsable)
                {
                    continue;
                }

                var points = contour.Points;
                var previous = NormalizedToBoard(
                    points[points.Count - 1],
                    shapeScale);
                for (var pointIndex = 0;
                    pointIndex < points.Count;
                    pointIndex++)
                {
                    var current = NormalizedToBoard(
                        points[pointIndex],
                        shapeScale);
                    var segment = current - previous;
                    var lengthSquared = segment.sqrMagnitude;
                    if (lengthSquared <= 0.000001f)
                    {
                        previous = current;
                        continue;
                    }

                    var progress = Mathf.Clamp01(
                        Vector2.Dot(
                            boardPosition - previous,
                            segment) /
                        lengthSquared);
                    var candidate =
                        previous + segment * progress;
                    var candidateDistance =
                        Vector2.Distance(
                            boardPosition,
                            candidate);
                    if (candidateDistance < distanceCells)
                    {
                        nearestPoint = candidate;
                        tangent = segment.normalized;
                        distanceCells = candidateDistance;
                        found = true;
                    }

                    previous = current;
                }
            }

            return found;
        }

        private static Vector2 GetSafeBoardHalfExtentsCells()
        {
            return new Vector2(
                Mathf.Max(
                    1f,
                    (BoardLayoutConfig.GridColumns - 2) *
                    0.5f),
                Mathf.Max(
                    1f,
                    (BoardLayoutConfig.GridRows - 2) *
                    0.5f));
        }

        private static bool IsInsidePolygon(
            Vector2 point,
            IReadOnlyList<Vector2> polygon)
        {
            var inside = false;
            var previous = polygon[polygon.Count - 1];
            for (var index = 0;
                index < polygon.Count;
                index++)
            {
                var current = polygon[index];
                if ((current.y > point.y) !=
                    (previous.y > point.y))
                {
                    var denominator =
                        previous.y - current.y;
                    if (Mathf.Abs(denominator) >
                        0.000001f)
                    {
                        var crossingX =
                            (previous.x - current.x) *
                            (point.y - current.y) /
                            denominator +
                            current.x;
                        if (point.x < crossingX)
                        {
                            inside = !inside;
                        }
                    }
                }

                previous = current;
            }

            return inside;
        }
    }

    internal static class VehicleShapeTemplateCatalog
    {
        private const string ResourceDirectory = "VehicleShapeTemplates/";
        private static readonly object CacheLock = new object();
        private static readonly Dictionary<string, IVehicleShapeTemplate>
            TemplatesByPath =
                new Dictionary<string, IVehicleShapeTemplate>(
                    StringComparer.Ordinal);
        private static int mainThreadId;
        private static bool runtimeTemplatesPrimed;

        /// <summary>
        /// Loads every conventionally named template while execution is still on
        /// Unity's main thread. Runtime generation workers can then read only the
        /// detached snapshots held by this catalog.
        /// </summary>
        public static void PrimeRuntimeGenerationTemplates()
        {
            RegisterMainThreadOrThrow();
            lock (CacheLock)
            {
                if (runtimeTemplatesPrimed)
                {
                    return;
                }
            }

            var assets =
                Resources.LoadAll<VehicleShapeTemplate>(
                    ResourceDirectory.TrimEnd('/'));
            for (var index = 0;
                index < assets.Length;
                index++)
            {
                var asset = assets[index];
                if (asset == null)
                {
                    continue;
                }

                var template = asset.CreateSnapshot();
                if (template == null || !template.IsUsable)
                {
                    continue;
                }

                lock (CacheLock)
                {
                    TemplatesByPath[
                        ResourceDirectory + asset.name] =
                        template;
                }
            }

            lock (CacheLock)
            {
                runtimeTemplatesPrimed = true;
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PrimeBeforeSceneLoad()
        {
            PrimeRuntimeGenerationTemplates();
        }

        public static bool TryGetTemplate(
            VehicleShapeLayoutDefinition definition,
            out IVehicleShapeTemplate template)
        {
            // A library-specific asset wins (for example HollowSquare.asset). When one is
            // absent, the primitive kind is the canonical fallback (Square.asset). This
            // makes new shapes data-driven: adding a correctly named Resources asset is
            // enough to enable contour classification and the raster gate.
            if (definition.LibraryId != VehicleShapeLibraryId.None &&
                TryLoad(definition.LibraryId.ToString(), out template))
            {
                return true;
            }

            if (definition.Kind != VehicleShapeLayoutKind.None)
            {
                return TryLoad(definition.Kind.ToString(), out template);
            }

            template = null;
            return false;
        }

        public static bool TryGetQualityTemplate(
            VehicleShapeLayoutDefinition definition,
            out IVehicleShapeTemplate template)
        {
            // Distinct library silhouettes must opt in with their own asset. For example,
            // HeartArrow must not be graded as a plain Heart and HollowSquare must not be
            // graded as a solid Square. Primitive/positive variants have no library id and
            // therefore use their kind asset directly.
            if (definition.LibraryId != VehicleShapeLibraryId.None)
            {
                return TryLoad(definition.LibraryId.ToString(), out template);
            }

            if (definition.Kind != VehicleShapeLayoutKind.None)
            {
                return TryLoad(definition.Kind.ToString(), out template);
            }

            template = null;
            return false;
        }

        private static bool TryLoad(
            string resourceName,
            out IVehicleShapeTemplate template)
        {
            var path = ResourceDirectory + resourceName;
            lock (CacheLock)
            {
                if (TemplatesByPath.TryGetValue(
                        path,
                        out template))
                {
                    return template != null &&
                        template.IsUsable;
                }
            }

            // A worker may consume a snapshot prepared by Prime..., but it must
            // never fall through to Resources.Load or touch a ScriptableObject.
            if (RuntimeGenerationThreadGuard.IsWorkerThread ||
                !IsCurrentThreadRegisteredMainThread())
            {
                template = null;
                return false;
            }

            var asset =
                Resources.Load<VehicleShapeTemplate>(path);
            template = asset != null
                ? asset.CreateSnapshot()
                : null;
#if UNITY_EDITOR
            // Do not make an editor miss permanent: artists can add the conventionally
            // named asset and use it immediately without a script/domain reload.
            lock (CacheLock)
            {
                if (template != null)
                {
                    TemplatesByPath[path] = template;
                }
                else
                {
                    TemplatesByPath.Remove(path);
                }
            }
#else
            // Player content is immutable, so negative caching avoids repeated missing
            // Resources lookups for library ids that intentionally have no template yet.
            lock (CacheLock)
            {
                TemplatesByPath[path] = template;
            }
#endif

            return template != null && template.IsUsable;
        }

        private static void RegisterMainThreadOrThrow()
        {
            if (RuntimeGenerationThreadGuard.IsWorkerThread)
            {
                throw new InvalidOperationException(
                    "Vehicle shape templates must be primed on the Unity main thread.");
            }

            var currentThreadId =
                Thread.CurrentThread.ManagedThreadId;
            lock (CacheLock)
            {
                if (mainThreadId == 0)
                {
                    mainThreadId = currentThreadId;
                    return;
                }

                if (mainThreadId != currentThreadId)
                {
                    throw new InvalidOperationException(
                        "Vehicle shape templates were primed from a different thread.");
                }
            }
        }

        private static bool IsCurrentThreadRegisteredMainThread()
        {
            var currentThreadId =
                Thread.CurrentThread.ManagedThreadId;
            lock (CacheLock)
            {
                // Preserve synchronous editor/runtime callers that predate the
                // explicit prime API. The worker scope above is the hard guard.
                if (mainThreadId == 0)
                {
                    mainThreadId = currentThreadId;
                }

                return mainThreadId == currentThreadId;
            }
        }
    }
}
