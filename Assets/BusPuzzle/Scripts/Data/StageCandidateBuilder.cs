namespace BusPuzzle
{
    public static class StageCandidateBuilder
    {
        private const int MinimumRuntimeCandidateAttempts = 8;
        private const int RuntimeSolutionNodeVisitLimit = 2048;

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
                var candidateReport = LevelValidator.Validate(candidateLevel, false);
                var candidateAnalysis = StageSolutionAnalyzer.Analyze(
                    candidateLevel.Buses,
                    candidateLevel.Garages,
                    GetCandidateSolutionCountLimit(config, request),
                    config.ReleaseSolutionNodeVisitLimit);

                if (candidateReport.HasErrors || !candidateAnalysis.IsSolvable)
                {
                    report = candidateReport;
                    analysis = candidateAnalysis;
                    continue;
                }

                var solutionDistance = GetSolutionRangeDistance(candidateAnalysis, request);
                if (solutionDistance > 0)
                {
                    var candidateScore = ScoreReleaseFallbackCandidate(request, candidateLevel, candidateReport, candidateAnalysis, solutionDistance);
                    if (candidateScore < fallbackScore)
                    {
                        fallbackScore = candidateScore;
                        fallbackLevel = candidateLevel;
                        fallbackReport = candidateReport;
                        fallbackAnalysis = candidateAnalysis;
                    }

                    report = candidateReport;
                    analysis = candidateAnalysis;
                    if (IsAcceptableReleaseFallback(solutionDistance))
                    {
                        UnityEngine.Debug.LogWarning(
                            $"Stage {request.StageNumber:000} is using a near-range verified candidate with " +
                            $"{candidateAnalysis.SolutionCount} solutions; preferred range is " +
                            $"{request.MinSolutionCount}-{request.MaxSolutionCount}.");
                        level = candidateLevel;
                        return true;
                    }

                    continue;
                }

                level = candidateLevel;
                report = candidateReport;
                analysis = candidateAnalysis;
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

            UnityEngine.Debug.LogWarning(
                $"Failed to build a fast verified runtime candidate for stage {request.StageNumber}. Using emergency solvable stage.");
            return CreateEmergencySolvableStage(request);
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

            var analysis = StageSolutionAnalyzer.Analyze(level.Buses, level.Garages, 1, RuntimeSolutionNodeVisitLimit);
            if (!analysis.IsSolvable)
            {
                return false;
            }

            score = ScoreRuntimeCandidate(request, level);
            return true;
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
            return score;
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
            return score;
        }

        private static int GetSolutionRangeDistance(StageSolutionAnalysis analysis, StageGenerationRequest request)
        {
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
            var upperBoundProbe = UnityEngine.Mathf.Max(1, request.MaxSolutionCount + 1);
            return UnityEngine.Mathf.Clamp(
                UnityEngine.Mathf.Min(config.SolutionCountLimit, upperBoundProbe),
                1,
                config.SolutionCountLimit);
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
                LevelGenerator.GetRotaryCapacity(request.Difficulty),
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
