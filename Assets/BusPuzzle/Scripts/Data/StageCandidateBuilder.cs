using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public static class StageCandidateBuilder
    {
        private const int MinimumRuntimeCandidateAttempts = 8;
        private const int MinimumReleaseCandidateProbeCount = 6;
        private const int MinimumRuntimeFallbackVehicleCount = 24;
        private const int EmergencyVehicleCount = 4;
        private const int RuntimeSolutionNodeVisitLimit = 2048;
        private static readonly float[] RelaxedRuntimeVehicleScales = { 0.90f, 0.78f, 0.66f };

        public static bool TryBuildVerifiedStageCandidate(
            StageGenerationConfig config,
            StageGenerationRequest request,
            out LevelData level,
            out LevelValidationReport report,
            out StageSolutionAnalysis analysis,
            System.Func<int, bool> shouldCancel = null)
        {
            config = config != null ? config : UnityEngine.ScriptableObject.CreateInstance<StageGenerationConfig>();
            level = null;
            report = null;
            analysis = default;
            LevelData fallbackLevel = null;
            LevelValidationReport fallbackReport = null;
            StageSolutionAnalysis fallbackAnalysis = default;
            var fallbackScore = int.MaxValue;
            var fallbackSolutionDistance = int.MaxValue;
            LevelData bestLevel = null;
            LevelValidationReport bestReport = null;
            StageSolutionAnalysis bestAnalysis = default;
            var bestScore = int.MaxValue;
            var minimumProbeCount = GetMinimumReleaseCandidateProbeCount(config, request);
            var isShapeLibraryRequest = IsShapeLibraryRequest(request);

            for (var candidate = 0; candidate < config.CandidateAttemptsPerStage; candidate++)
            {
                if (shouldCancel != null && shouldCancel(candidate))
                {
                    return false;
                }

                var candidateLevel = LevelGenerator.CreateRuntimeStage(
                    request,
                    config.SuperHardGarageRule,
                    candidate,
                    config.ReleaseVehicleGenerationAttempts,
                    false);
                if (!HasRequiredShapeLibraryVehicleCoverage(request, candidateLevel))
                {
#if UNITY_EDITOR
                    if (isShapeLibraryRequest && candidate + 1 >= config.CandidateAttemptsPerStage)
                    {
                        LogShapeLibraryCandidateRejection(request, candidateLevel, candidate, "coverage/quality");
                    }
#endif
                    continue;
                }

                var candidateReport = LevelValidator.Validate(candidateLevel, false);
                var candidateAnalysis = AnalyzeCandidateSolution(config, request, candidateLevel);

                if (candidateReport.HasErrors || !candidateAnalysis.IsSolvable)
                {
#if UNITY_EDITOR
                    if (isShapeLibraryRequest && candidate + 1 >= config.CandidateAttemptsPerStage)
                    {
                        LogShapeLibraryCandidateRejection(
                            request,
                            candidateLevel,
                            candidate,
                            candidateReport.HasErrors ? "validation" : "solution");
                    }
#endif
                    report = candidateReport;
                    analysis = candidateAnalysis;
                    continue;
                }

                var solutionDistance = GetSolutionRangeDistance(candidateAnalysis, request);
                var candidateScore = ScoreReleaseFallbackCandidate(request, candidateLevel, candidateReport, candidateAnalysis, solutionDistance);
                if (solutionDistance == 0)
                {
                    if (candidateScore < bestScore)
                    {
                        bestScore = candidateScore;
                        bestLevel = candidateLevel;
                        bestReport = candidateReport;
                        bestAnalysis = candidateAnalysis;
                    }

                    report = candidateReport;
                    analysis = candidateAnalysis;
                    if (candidate + 1 >= minimumProbeCount)
                    {
                        level = bestLevel;
                        report = bestReport;
                        analysis = bestAnalysis;
                        return true;
                    }

                    continue;
                }

                if (solutionDistance > 0)
                {
                    var acceptableFallback = IsAcceptableReleaseFallback(solutionDistance);
                    if (acceptableFallback)
                    {
                        if (fallbackLevel == null ||
                            !IsAcceptableReleaseFallback(fallbackSolutionDistance) ||
                            candidateScore < fallbackScore)
                        {
                            fallbackScore = candidateScore;
                            fallbackSolutionDistance = solutionDistance;
                            fallbackLevel = candidateLevel;
                            fallbackReport = candidateReport;
                            fallbackAnalysis = candidateAnalysis;
                        }

                        report = candidateReport;
                        analysis = candidateAnalysis;
                        if (candidate + 1 >= minimumProbeCount)
                        {
                            UnityEngine.Debug.LogWarning(
                                $"Stage {request.StageNumber:000} is using a near-range verified candidate with " +
                                $"{fallbackAnalysis.SolutionCount} solutions; preferred range is " +
                                $"{request.MinSolutionCount}-{request.MaxSolutionCount}.");
                            level = fallbackLevel;
                            report = fallbackReport;
                            analysis = fallbackAnalysis;
                            return true;
                        }

                        continue;
                    }

                    if (candidateScore < fallbackScore)
                    {
                        fallbackScore = candidateScore;
                        fallbackSolutionDistance = solutionDistance;
                        fallbackLevel = candidateLevel;
                        fallbackReport = candidateReport;
                        fallbackAnalysis = candidateAnalysis;
                    }

                    report = candidateReport;
                    analysis = candidateAnalysis;
                    continue;
                }
            }

            if (bestLevel != null)
            {
                level = bestLevel;
                report = bestReport;
                analysis = bestAnalysis;
                return true;
            }

            if (fallbackLevel != null)
            {
                UnityEngine.Debug.LogWarning(
                    $"No stage {request.StageNumber:000} candidate matched preferred solution range " +
                    $"{request.MinSolutionCount}-{request.MaxSolutionCount}. " +
                    $"Using closest verified candidate with {fallbackAnalysis.SolutionCount} solutions.");
                level = fallbackLevel;
                report = fallbackReport;
                analysis = fallbackAnalysis;
                return true;
            }

            return false;
        }

        public static LevelData BuildRuntimeStageCandidate(StageGenerationConfig config, StageGenerationRequest request)
        {
            config = config != null ? config : UnityEngine.ScriptableObject.CreateInstance<StageGenerationConfig>();

            LevelData bestLevel = null;
            var bestScore = int.MaxValue;
            var attempts = UnityEngine.Mathf.Min(
                UnityEngine.Mathf.Max(config.RuntimeCandidateAttemptsPerStage, MinimumRuntimeCandidateAttempts),
                config.CandidateAttemptsPerStage);

            for (var candidate = 0; candidate < attempts; candidate++)
            {
                var level = LevelGenerator.CreateRuntimeStage(
                    request,
                    config.SuperHardGarageRule,
                    candidate,
                    config.RuntimeVehicleGenerationAttempts,
                    false);
                if (!HasRequiredShapeLibraryVehicleCoverage(request, level))
                {
                    continue;
                }

                if (!TryScoreRuntimeCandidate(config, request, level, out var score))
                {
                    continue;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestLevel = level;
                }

                if (score == 0)
                {
                    return level;
                }
            }

            if (bestLevel != null)
            {
                return bestLevel;
            }

            if (TryBuildVerifiedStageCandidate(config, request, out var verifiedLevel, out _, out _))
            {
                UnityEngine.Debug.LogWarning(
                    $"Runtime stage {request.StageNumber:000} required full verification fallback.");
                return verifiedLevel;
            }

            var relaxedLevel = BuildRelaxedRuntimeStageCandidate(config, request);
            if (relaxedLevel != null)
            {
                return relaxedLevel;
            }

            UnityEngine.Debug.LogWarning(
                $"Failed to build a fast verified runtime candidate for stage {request.StageNumber}. Using emergency solvable stage.");
            return CreateEmergencySolvableStage(request);
        }

        public static bool ShouldCacheRuntimeStage(StageGenerationRequest request, LevelData level)
        {
            return level != null &&
                level.AllVehicles != null &&
                level.AllVehicles.Count > EmergencyVehicleCount;
        }

        public static LevelData BuildBestStageCandidate(StageGenerationConfig config, StageGenerationRequest request)
        {
            return TryBuildVerifiedStageCandidate(config, request, out var level, out _, out _) ? level : null;
        }

        public static bool IsSolutionCountAcceptable(StageGenerationRequest request, StageSolutionAnalysis analysis)
        {
            if (!analysis.IsSolvable)
            {
                return false;
            }

            var solutionDistance = GetSolutionRangeDistance(analysis, request);
            return solutionDistance == 0 || IsAcceptableReleaseFallback(solutionDistance);
        }

        private static bool TryScoreRuntimeCandidate(
            StageGenerationConfig config,
            StageGenerationRequest request,
            LevelData level,
            out int score)
        {
            score = int.MaxValue;
            if (level == null)
            {
                return false;
            }

            var report = LevelValidator.Validate(level, false);
            if (report.HasErrors)
            {
                return false;
            }

            if (!HasRequiredShapeLibraryVehicleCoverage(request, level))
            {
                return false;
            }

            var analysis = AnalyzeCandidateSolution(config, request, level);
            if (!analysis.IsSolvable)
            {
                return false;
            }

            score = ScoreRuntimeCandidate(request, level);
            return true;
        }

        private static LevelData BuildRelaxedRuntimeStageCandidate(StageGenerationConfig config, StageGenerationRequest request)
        {
            var sourceProfile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            for (var pass = 0; pass < RelaxedRuntimeVehicleScales.Length; pass++)
            {
                var relaxedRequest = CreateRelaxedRuntimeRequest(request, pass);
                if (!TryBuildVerifiedStageCandidate(config, relaxedRequest, out var level, out _, out _))
                {
                    continue;
                }

                UnityEngine.Debug.LogWarning(
                    $"Runtime stage {request.StageNumber:000} is using relaxed fallback pass {pass + 1}: " +
                    $"{level.AllVehicles.Count}/{sourceProfile.TargetVehicleCount} vehicles.");
                return level;
            }

            return null;
        }

        private static StageGenerationRequest CreateRelaxedRuntimeRequest(StageGenerationRequest request, int pass)
        {
            var sourceProfile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var scaleIndex = UnityEngine.Mathf.Clamp(pass, 0, RelaxedRuntimeVehicleScales.Length - 1);
            var vehicleScale = RelaxedRuntimeVehicleScales[scaleIndex];
            var minimumVehicleCount = UnityEngine.Mathf.Min(
                sourceProfile.TargetVehicleCount,
                GetMinimumRuntimeFallbackVehicleCount(request.Difficulty));
            var targetVehicleCount = UnityEngine.Mathf.Clamp(
                UnityEngine.Mathf.RoundToInt(sourceProfile.TargetVehicleCount * vehicleScale),
                minimumVehicleCount,
                sourceProfile.TargetVehicleCount);
            var targetColorCount = UnityEngine.Mathf.Clamp(
                sourceProfile.TargetColorCount - pass - 1,
                4,
                sourceProfile.TargetColorCount);
            var relaxation = (pass + 1f) / RelaxedRuntimeVehicleScales.Length;
            var parkingTension = UnityEngine.Mathf.Lerp(
                sourceProfile.ParkingTension,
                UnityEngine.Mathf.Min(sourceProfile.ParkingTension, 0.46f),
                relaxation);
            var stationPressure = UnityEngine.Mathf.Lerp(
                sourceProfile.StationPressure,
                UnityEngine.Mathf.Min(sourceProfile.StationPressure, 0.46f),
                relaxation);
            var relaxedProfile = LevelDifficultyProfile.CreateCustom(
                sourceProfile.Difficulty,
                sourceProfile.PassengerFlowRule,
                targetVehicleCount,
                targetColorCount,
                parkingTension,
                stationPressure,
                sourceProfile.RequireSolutionRoute);
            var layoutPoolSize = UnityEngine.Mathf.Max(1, request.VehicleLayoutVariantPoolSize);
            var layoutVariantIndex = UnityEngine.Mathf.Abs(
                request.VehicleLayoutVariantIndex + (pass + 1) * 31) % layoutPoolSize;
            var maxSolutionCount = UnityEngine.Mathf.Max(request.MaxSolutionCount, targetVehicleCount * 4);

            return new StageGenerationRequest(
                request.StageNumber,
                request.Seed + (pass + 1) * 104729,
                request.Difficulty,
                StageModifierFlags.None,
                relaxedProfile,
                request.Progress,
                request.Post50Pressure,
                RotaryRoadPresetId.LargeCircleTest,
                layoutVariantIndex,
                layoutPoolSize,
                0,
                1,
                1,
                request.RotaryCapacity,
                MysteryVehicleGenerationProfile.Disabled,
                1,
                maxSolutionCount);
        }

        private static int GetMinimumRuntimeFallbackVehicleCount(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 32;
                case LevelDifficulty.Hard:
                    return 28;
                default:
                    return MinimumRuntimeFallbackVehicleCount;
            }
        }

        private static int ScoreRuntimeCandidate(StageGenerationRequest request, LevelData level)
        {
            if (level == null)
            {
                return int.MaxValue;
            }

            var score = 0;
            if (level.Buses == null || level.Buses.Count == 0 || level.PassengerUnits == null || level.PassengerUnits.Count == 0)
            {
                score += 100000;
            }

            if (level.TryGetCapacityMismatchMessage(out _))
            {
                score += 50000;
            }

            score += UnityEngine.Mathf.Abs(level.AllVehicles.Count - request.Profile.TargetVehicleCount) * 100;
            score += UnityEngine.Mathf.Abs(CountUniqueVehicleColors(level.AllVehicles) - request.Profile.TargetColorCount) * 25;
            score += ScoreLayoutAesthetics(request, level);
            return score;
        }

        private static int ScoreLayoutAesthetics(StageGenerationRequest request, LevelData level)
        {
            var vehicles = CollectVisibleLayoutVehicles(level);
            if (vehicles.Count == 0)
            {
                return 5000;
            }

            var boardCenterX = (BoardLayoutConfig.GridColumns - 1) * 0.5f;
            var boardCenterY = (BoardLayoutConfig.GridRows - 1) * 0.5f;
            var rowCounts = new int[BoardLayoutConfig.GridRows];
            var columnCounts = new int[BoardLayoutConfig.GridColumns];
            var directionCounts = new int[4];
            var positions = new Vector2[vehicles.Count];
            var usesShapeLibraryLayout = VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(
                request.VehicleLayoutVariantIndex,
                out _);
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            var sumX = 0f;
            var sumY = 0f;
            var score = 0;

            for (var index = 0; index < vehicles.Count; index++)
            {
                var vehicle = vehicles[index];
                var position = new Vector2(
                    vehicle.GridPosition.x + vehicle.PositionOffsetCells.x,
                    vehicle.GridPosition.y + vehicle.PositionOffsetCells.y);
                positions[index] = position;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
                sumX += position.x;
                sumY += position.y;

                if (vehicle.GridPosition.x >= 0 && vehicle.GridPosition.x < columnCounts.Length)
                {
                    columnCounts[vehicle.GridPosition.x]++;
                }

                if (vehicle.GridPosition.y >= 0 && vehicle.GridPosition.y < rowCounts.Length)
                {
                    rowCounts[vehicle.GridPosition.y]++;
                }

                var directionIndex = Mathf.Clamp((int)vehicle.Direction, 0, directionCounts.Length - 1);
                directionCounts[directionIndex]++;

                if (!usesShapeLibraryLayout)
                {
                    var angle = Mathf.Abs(Mathf.DeltaAngle(0f, vehicle.AngleOffsetDegrees));
                    score += Mathf.RoundToInt(Mathf.Min(angle, 18f) * 2.5f);
                    var offsetOverage = Mathf.Max(0f, vehicle.PositionOffsetCells.magnitude - 0.08f);
                    score += Mathf.RoundToInt(offsetOverage * 180f);
                }
            }

            var count = Mathf.Max(1, vehicles.Count);
            var centerX = sumX / count;
            var centerY = sumY / count;
            score += Mathf.RoundToInt((Mathf.Abs(centerX - boardCenterX) + Mathf.Abs(centerY - boardCenterY)) * 55f);

            var width = Mathf.Max(1f, maxX - minX + 1f);
            var height = Mathf.Max(1f, maxY - minY + 1f);
            var density = count / Mathf.Max(1f, width * height);
            var profile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var targetDensity = usesShapeLibraryLayout
                ? Mathf.Lerp(0.38f, 0.62f, profile.ParkingTension)
                : Mathf.Lerp(0.22f, 0.46f, profile.ParkingTension);
            score += Mathf.RoundToInt(Mathf.Abs(density - targetDensity) * 260f);
            var regularTargetVehicleCount = Mathf.Max(1, profile.TargetVehicleCount - CountGarageVehicles(level.Garages));
            score += VehicleLayoutPatternEngine.ScoreShapeFidelity(
                profile,
                regularTargetVehicleCount,
                request.VehicleLayoutVariantIndex,
                level.Buses);
            if (usesShapeLibraryLayout)
            {
                score += ScoreShapeLibrarySizeMix(profile, request.VehicleLayoutVariantIndex, level.Buses);
            }

            if (width < 6f || height < 6f)
            {
                score += 120;
            }

            if (profile.ParkingTension >= 0.62f)
            {
                score += Mathf.RoundToInt((Mathf.Max(0f, 10f - width) + Mathf.Max(0f, 10f - height)) * 24f);
            }

            score += CountSparseLanePenalty(rowCounts, 12);
            score += CountSparseLanePenalty(columnCounts, 10);
            score += Mathf.Max(0, 5 - CountStrongLanes(rowCounts, 3) - CountStrongLanes(columnCounts, 3)) * 30;
            score += CountIsolationPenalty(positions);
            score += CountDirectionDominancePenalty(directionCounts, count);
            return score;
        }

        private static int ScoreShapeLibrarySizeMix(
            LevelDifficultyProfile profile,
            int layoutVariantIndex,
            IReadOnlyList<BusDefinition> vehicles)
        {
            if (!VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(layoutVariantIndex, out var libraryIndex))
            {
                return 0;
            }

            var libraryId = (VehicleShapeLibraryId)libraryIndex;
            var metrics = ShapeLibraryLayoutQuality.CalculateMetrics(profile, layoutVariantIndex, vehicles);
            var minimumMediumLarge = ShapeLibraryLayoutQuality.GetMinimumMediumLargeVehicleCount(
                profile,
                libraryId,
                metrics.VehicleCount);
            var minimumLarge = ShapeLibraryLayoutQuality.GetMinimumLargeVehicleCount(
                profile,
                libraryId,
                metrics.VehicleCount);
            var score = Mathf.Max(0, minimumMediumLarge - metrics.MediumLargeCount) * 45;
            score += Mathf.Max(0, minimumLarge - metrics.LargeCount) * 80;
            if (metrics.VehicleCount >= 34 && metrics.MediumLargeCount == 0)
            {
                score += 180;
            }

            return score;
        }

        private static bool HasRequiredShapeLibraryVehicleCoverage(StageGenerationRequest request, LevelData level)
        {
            if (level == null)
            {
                return false;
            }

            var profile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var actualVehicleCount = level.Buses != null ? level.Buses.Count : 0;
            return ShapeLibraryVehicleCoverage.IsSatisfied(
                    profile,
                    request.VehicleLayoutVariantIndex,
                    actualVehicleCount) &&
                ShapeLibraryLayoutQuality.IsSatisfied(
                    profile,
                    request.VehicleLayoutVariantIndex,
                level.Buses);
        }

        private static StageSolutionAnalysis AnalyzeCandidateSolution(
            StageGenerationConfig config,
            StageGenerationRequest request,
            LevelData level)
        {
            if (level == null)
            {
                return new StageSolutionAnalysis(false, 0, false);
            }

            if (IsShapeLibraryRequest(request) &&
                level.Buses != null &&
                (level.Garages == null || level.Garages.Count == 0))
            {
                return LevelVehicleExitPlanner.TryFindExitOrder(level.Buses, out var exitOrder, out _) &&
                    exitOrder.Count == level.Buses.Count
                    ? new StageSolutionAnalysis(true, 1, false)
                    : new StageSolutionAnalysis(false, 0, false);
            }

            return StageSolutionAnalyzer.Analyze(
                level.Buses,
                level.Garages,
                GetCandidateSolutionCountLimit(config, request),
                GetCandidateSolutionNodeVisitLimit(config, request));
        }

#if UNITY_EDITOR
        private static void LogShapeLibraryCandidateRejection(
            StageGenerationRequest request,
            LevelData level,
            int candidateIndex,
            string reason)
        {
            var profile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var vehicles = level != null ? level.Buses : null;
            var vehicleCount = vehicles != null ? vehicles.Count : 0;
            var qualityMessage = string.Empty;
            ShapeLibraryLayoutQuality.TryGetFailureMessage(
                profile,
                request.VehicleLayoutVariantIndex,
                vehicles,
                out qualityMessage);
            var metrics = ShapeLibraryLayoutQuality.CalculateMetrics(
                profile,
                request.VehicleLayoutVariantIndex,
                vehicles);
            UnityEngine.Debug.LogWarning(
                $"Shape candidate rejected at stage {request.StageNumber:000}, candidate {candidateIndex + 1}: " +
                $"{reason}. Vehicles {vehicleCount}/{profile.TargetVehicleCount}, " +
                $"shape {metrics.ShapeMatchedCount}, openings {metrics.OpeningExitCount}, " +
                $"outward {metrics.OutwardFacingCount}, medium+large {metrics.MediumLargeCount}. " +
                $"{qualityMessage}");
        }
#endif

        private static List<BusDefinition> CollectVisibleLayoutVehicles(LevelData level)
        {
            var vehicles = new List<BusDefinition>();
            if (level == null)
            {
                return vehicles;
            }

            if (level.Buses != null)
            {
                for (var index = 0; index < level.Buses.Count; index++)
                {
                    vehicles.Add(level.Buses[index]);
                }
            }

            var garages = level.Garages;
            if (garages != null)
            {
                for (var index = 0; index < garages.Count; index++)
                {
                    vehicles.Add(garages[index].FrontVehicle);
                }
            }

            return vehicles;
        }

        private static int CountGarageVehicles(IReadOnlyList<GarageDefinition> garages)
        {
            if (garages == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < garages.Count; index++)
            {
                count += garages[index].TotalVehicleCount;
            }

            return count;
        }

        private static int CountSparseLanePenalty(IReadOnlyList<int> laneCounts, int penalty)
        {
            var score = 0;
            for (var index = 0; index < laneCounts.Count; index++)
            {
                if (laneCounts[index] == 1)
                {
                    score += penalty;
                }
            }

            return score;
        }

        private static int CountStrongLanes(IReadOnlyList<int> laneCounts, int threshold)
        {
            var count = 0;
            for (var index = 0; index < laneCounts.Count; index++)
            {
                if (laneCounts[index] >= threshold)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountIsolationPenalty(IReadOnlyList<Vector2> positions)
        {
            var score = 0;
            for (var index = 0; index < positions.Count; index++)
            {
                var nearestDistanceSquared = float.MaxValue;
                for (var otherIndex = 0; otherIndex < positions.Count; otherIndex++)
                {
                    if (index == otherIndex)
                    {
                        continue;
                    }

                    nearestDistanceSquared = Mathf.Min(
                        nearestDistanceSquared,
                        (positions[index] - positions[otherIndex]).sqrMagnitude);
                }

                if (nearestDistanceSquared > 7.0f)
                {
                    score += 80;
                }
                else if (nearestDistanceSquared > 4.2f)
                {
                    score += 28;
                }
            }

            return score;
        }

        private static int CountDirectionDominancePenalty(IReadOnlyList<int> directionCounts, int vehicleCount)
        {
            var maxDirectionCount = 0;
            for (var index = 0; index < directionCounts.Count; index++)
            {
                maxDirectionCount = Mathf.Max(maxDirectionCount, directionCounts[index]);
            }

            var dominanceLimit = Mathf.CeilToInt(vehicleCount * 0.62f);
            return maxDirectionCount <= dominanceLimit ? 0 : (maxDirectionCount - dominanceLimit) * 18;
        }

        private static int ScoreReleaseFallbackCandidate(
            StageGenerationRequest request,
            LevelData level,
            LevelValidationReport report,
            StageSolutionAnalysis analysis,
            int solutionDistance)
        {
            var score = solutionDistance * 1000;
            score += analysis.HitLimit ? 10000 : 0;
            score += CountWarnings(report) * 250;
            score += ScoreRuntimeCandidate(request, level);
            score += ScoreShapeLibraryVehicleCoverage(request, level);
            return score;
        }

        private static int GetMinimumReleaseCandidateProbeCount(StageGenerationConfig config, StageGenerationRequest request)
        {
            var attempts = config != null ? config.CandidateAttemptsPerStage : MinimumReleaseCandidateProbeCount;
            if (VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(request.VehicleLayoutVariantIndex, out _))
            {
                return Mathf.Clamp(2, 1, attempts);
            }

            var lower = Mathf.Min(4, attempts);
            var upper = Mathf.Min(MinimumReleaseCandidateProbeCount, attempts);
            return Mathf.Clamp(Mathf.CeilToInt(attempts * 0.16f), lower, upper);
        }

        private static int ScoreShapeLibraryVehicleCoverage(StageGenerationRequest request, LevelData level)
        {
            if (!IsShapeLibraryRequest(request) || level == null)
            {
                return 0;
            }

            var profile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var vehicleCount = level.Buses != null ? level.Buses.Count : 0;
            var missing = Mathf.Max(0, profile.TargetVehicleCount - vehicleCount);
            return missing * 420;
        }

        private static int GetSolutionRangeDistance(StageSolutionAnalysis analysis, StageGenerationRequest request)
        {
            if (IsShapeLibraryRequest(request))
            {
                return analysis.IsSolvable ? 0 : int.MaxValue / 4;
            }

            if (analysis.SolutionCount < request.MinSolutionCount)
            {
                return request.MinSolutionCount - analysis.SolutionCount;
            }

            if (analysis.SolutionCount > request.MaxSolutionCount)
            {
                return analysis.SolutionCount - request.MaxSolutionCount;
            }

            return 0;
        }

        private static int GetCandidateSolutionCountLimit(StageGenerationConfig config, StageGenerationRequest request)
        {
            if (IsShapeLibraryRequest(request))
            {
                return 1;
            }

            var upperBoundProbe = UnityEngine.Mathf.Max(1, request.MaxSolutionCount + 1);
            return UnityEngine.Mathf.Clamp(
                UnityEngine.Mathf.Min(config.SolutionCountLimit, upperBoundProbe),
                1,
                config.SolutionCountLimit);
        }

        private static int GetCandidateSolutionNodeVisitLimit(StageGenerationConfig config, StageGenerationRequest request)
        {
            if (IsShapeLibraryRequest(request))
            {
                return 1024;
            }

            return config.ReleaseSolutionNodeVisitLimit;
        }

        private static bool IsShapeLibraryRequest(StageGenerationRequest request)
        {
            return VehicleLayoutPatternEngine.TryGetShapeLibraryIndex(request.VehicleLayoutVariantIndex, out _);
        }

        private static bool IsAcceptableReleaseFallback(int solutionDistance)
        {
            return solutionDistance <= 2;
        }

        private static int CountWarnings(LevelValidationReport report)
        {
            if (report == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < report.Issues.Count; index++)
            {
                if (report.Issues[index].Severity == LevelValidationSeverity.Warning)
                {
                    count++;
                }
            }

            return count;
        }

        private static LevelData CreateEmergencySolvableStage(StageGenerationRequest request)
        {
            var buses = new System.Collections.Generic.List<BusDefinition>
            {
                new BusDefinition(PuzzleColor.Red, BusSize.Small, GridDirection.Left, new UnityEngine.Vector2Int(2, 2)),
                new BusDefinition(PuzzleColor.Blue, BusSize.Small, GridDirection.Right, new UnityEngine.Vector2Int(11, 4)),
                new BusDefinition(PuzzleColor.Green, BusSize.Medium, GridDirection.Down, new UnityEngine.Vector2Int(5, 2)),
                new BusDefinition(PuzzleColor.Yellow, BusSize.Medium, GridDirection.Up, new UnityEngine.Vector2Int(8, 11))
            };

            var level = UnityEngine.ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = UnityEngine.HideFlags.DontSave;
            level.ConfigureWithPassengerFlowPlan(
                $"Stage {request.StageNumber:000} {request.Difficulty}",
                request.Profile,
                LevelGenerator.BuildPassengerFlowPlan(request.Profile, buses, request.Seed),
                buses,
                request.RotaryCapacity,
                request.RoadPresetId);
            return level;
        }

        private static int CountUniqueVehicleColors(System.Collections.Generic.IReadOnlyList<BusDefinition> vehicles)
        {
            var colors = new System.Collections.Generic.List<PuzzleColor>();
            if (vehicles == null)
            {
                return 0;
            }

            for (var index = 0; index < vehicles.Count; index++)
            {
                if (!colors.Contains(vehicles[index].Color))
                {
                    colors.Add(vehicles[index].Color);
                }
            }

            return colors.Count;
        }
    }
}
