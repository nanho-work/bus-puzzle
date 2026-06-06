namespace BusPuzzle
{
    public static class StageCandidateBuilder
    {
        private const int MinimumRuntimeCandidateAttempts = 8;

        public static bool TryBuildVerifiedStageCandidate(
            StageGenerationConfig config,
            StageGenerationRequest request,
            out LevelData level,
            out LevelValidationReport report,
            out StageSolutionAnalysis analysis)
        {
            config = config != null ? config : UnityEngine.ScriptableObject.CreateInstance<StageGenerationConfig>();
            level = null;
            report = null;
            analysis = default;

            for (var candidate = 0; candidate < config.CandidateAttemptsPerStage; candidate++)
            {
                var candidateLevel = LevelGenerator.CreateRuntimeStage(request, config.SuperHardGarageRule, candidate);
                var candidateReport = LevelValidator.Validate(candidateLevel, false);
                var candidateAnalysis = StageSolutionAnalyzer.Analyze(candidateLevel.Buses, candidateLevel.Garages, config.SolutionCountLimit);

                if (candidateReport.HasErrors ||
                    !candidateAnalysis.IsSolvable ||
                    candidateAnalysis.SolutionCount < request.MinSolutionCount ||
                    candidateAnalysis.SolutionCount > request.MaxSolutionCount)
                {
                    report = candidateReport;
                    analysis = candidateAnalysis;
                    continue;
                }

                level = candidateLevel;
                report = candidateReport;
                analysis = candidateAnalysis;
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
                    true);
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
                return verifiedLevel;
            }

            UnityEngine.Debug.LogWarning(
                $"Failed to build a verified runtime candidate for stage {request.StageNumber}. Falling back to the best generated candidate.");
            var fallbackLevel = LevelGenerator.CreateRuntimeStage(
                request,
                config.SuperHardGarageRule,
                attempts,
                config.CandidateAttemptsPerStage,
                true);
            return TryScoreRuntimeCandidate(config, request, fallbackLevel, out _)
                ? fallbackLevel
                : CreateEmergencySolvableStage(request);
        }

        public static LevelData BuildBestStageCandidate(StageGenerationConfig config, StageGenerationRequest request)
        {
            return TryBuildVerifiedStageCandidate(config, request, out var level, out _, out _) ? level : null;
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

            var solutionLimit = UnityEngine.Mathf.Clamp(request.MaxSolutionCount + 1, 1, config.SolutionCountLimit);
            var analysis = StageSolutionAnalyzer.Analyze(level.Buses, level.Garages, solutionLimit);
            if (!analysis.IsSolvable)
            {
                return false;
            }

            score = ScoreRuntimeCandidate(request, level) + ScoreSolutionCount(request, analysis);
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

        private static int ScoreSolutionCount(StageGenerationRequest request, StageSolutionAnalysis analysis)
        {
            if (!analysis.IsSolvable)
            {
                return 1000000;
            }

            if (analysis.SolutionCount < request.MinSolutionCount)
            {
                return (request.MinSolutionCount - analysis.SolutionCount) * 45;
            }

            if (analysis.SolutionCount > request.MaxSolutionCount || analysis.HitLimit)
            {
                return UnityEngine.Mathf.Max(0, analysis.SolutionCount - request.MaxSolutionCount) * 45 + 180;
            }

            return 0;
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
