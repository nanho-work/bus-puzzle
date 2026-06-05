namespace BusPuzzle
{
    public static class StageCandidateBuilder
    {
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
            var attempts = UnityEngine.Mathf.Min(config.RuntimeCandidateAttemptsPerStage, config.CandidateAttemptsPerStage);

            for (var candidate = 0; candidate < attempts; candidate++)
            {
                var level = LevelGenerator.CreateRuntimeStage(
                    request,
                    config.SuperHardGarageRule,
                    candidate,
                    config.RuntimeVehicleGenerationAttempts,
                    false);
                var score = ScoreRuntimeCandidate(request, level);

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

            return bestLevel;
        }

        public static LevelData BuildBestStageCandidate(StageGenerationConfig config, StageGenerationRequest request)
        {
            return TryBuildVerifiedStageCandidate(config, request, out var level, out _, out _) ? level : null;
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
