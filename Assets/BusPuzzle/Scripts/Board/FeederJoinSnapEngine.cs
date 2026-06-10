using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct FeederJoinResolution
    {
        public readonly float Progress;
        public readonly RotaryPathSample JoinSample;
        public readonly Vector2 JoinPoint;
        public readonly float LaneX;
        public readonly float LaneLength;

        public FeederJoinResolution(
            float progress,
            RotaryPathSample joinSample,
            Vector2 joinPoint,
            float laneX,
            float laneLength)
        {
            Progress = progress;
            JoinSample = joinSample;
            JoinPoint = joinPoint;
            LaneX = laneX;
            LaneLength = laneLength;
        }
    }

    internal static class FeederJoinSnapEngine
    {
        private const int CandidateStepsPerSide = 14;
        private const float CandidateSearchRadius = 0.085f;
        private const float MinimumSideOutward = 0.08f;
        private const float PreferredSideOutward = 0.34f;
        private const float ProgressWeight = 2.15f;
        private const float LateralGapWeight = 1.85f;
        private const float OutwardWeight = 1.35f;
        private const float ReverseJoinPenaltyWeight = 0.70f;

        public static FeederJoinResolution Resolve(
            RotaryPath rotaryPath,
            RoadPresetDefinition preset,
            int side,
            PassengerRoadProfile roadProfile)
        {
            var baseProgress = side < 0 ? preset.LeftFeederProgress : preset.RightFeederProgress;
            var fallback = CreateResolution(rotaryPath, preset, side, roadProfile, baseProgress);
            var best = fallback;
            var bestScore = ScoreCandidate(rotaryPath, side, baseProgress, best);

            for (var step = -CandidateStepsPerSide; step <= CandidateStepsPerSide; step++)
            {
                var offset = step / (float)CandidateStepsPerSide * CandidateSearchRadius;
                var candidate = CreateResolution(rotaryPath, preset, side, roadProfile, Mathf.Repeat(baseProgress + offset, 1f));
                var score = ScoreCandidate(rotaryPath, side, baseProgress, candidate);
                if (score >= bestScore)
                {
                    continue;
                }

                best = candidate;
                bestScore = score;
            }

            return best;
        }

        private static FeederJoinResolution CreateResolution(
            RotaryPath rotaryPath,
            RoadPresetDefinition preset,
            int side,
            PassengerRoadProfile roadProfile,
            float progress)
        {
            var joinSample = rotaryPath.Sample(progress);
            var roadWidth = roadProfile.RoadWidth;
            var outerRoadOffset = roadProfile.OuterRoadOffset;
            var joinPoint = joinSample.Point + joinSample.Outward * Mathf.Max(0.05f, outerRoadOffset - 0.015f);
            var laneLength = Mathf.Max(1.05f, preset.FeederRowsPerStack * preset.FeederRowSpacing + roadProfile.LaneWidth);
            var laneX = ResolveLaneX(rotaryPath, side, roadWidth, joinPoint.x);

            return new FeederJoinResolution(progress, joinSample, joinPoint, laneX, laneLength);
        }

        private static float ResolveLaneX(RotaryPath rotaryPath, int side, float roadWidth, float joinPointX)
        {
            var sideSign = side < 0 ? -1f : 1f;
            var minimumLaneAbs = rotaryPath.RadiusX + roadWidth * 0.52f;
            var maximumLaneAbs = rotaryPath.RadiusX + roadWidth * 1.05f + 0.16f;
            var targetLaneAbs = Mathf.Abs(joinPointX) + Mathf.Max(0.12f, roadWidth * 0.34f);
            return sideSign * Mathf.Clamp(targetLaneAbs, minimumLaneAbs, maximumLaneAbs);
        }

        private static float ScoreCandidate(
            RotaryPath rotaryPath,
            int side,
            float baseProgress,
            FeederJoinResolution candidate)
        {
            var sideSign = side < 0 ? -1f : 1f;
            var sideOutward = Mathf.Max(0f, sideSign * candidate.JoinSample.Outward.x);
            var progressDelta = Mathf.Abs(Mathf.DeltaAngle(baseProgress * 360f, candidate.Progress * 360f)) / 360f;
            var lateralGap = Mathf.Abs(candidate.LaneX - candidate.JoinPoint.x) / Mathf.Max(0.01f, rotaryPath.RadiusX);
            var outwardPenalty = Mathf.Max(0f, PreferredSideOutward - sideOutward) * OutwardWeight;
            var weakSidePenalty = Mathf.Max(0f, MinimumSideOutward - sideOutward) * 10f;

            var laneLength = Mathf.Max(1.05f, candidate.LaneLength);
            var approach = new Vector2(
                Mathf.Lerp(candidate.LaneX, candidate.JoinPoint.x, 0.68f),
                candidate.JoinPoint.y + laneLength * 0.06f);
            var intoJoin = candidate.JoinPoint - approach;
            intoJoin = intoJoin.sqrMagnitude > 0.0001f ? intoJoin.normalized : candidate.JoinSample.Tangent;
            var reverseJoinPenalty = Mathf.Max(0f, -Vector2.Dot(intoJoin, candidate.JoinSample.Tangent)) * ReverseJoinPenaltyWeight;

            return progressDelta * ProgressWeight +
                lateralGap * LateralGapWeight +
                outwardPenalty +
                weakSidePenalty +
                reverseJoinPenalty;
        }
    }
}
