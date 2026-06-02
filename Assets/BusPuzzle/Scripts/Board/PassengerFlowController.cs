using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerFlowController
    {
        private const float CatchUpGapMultiplier = 1.35f;
        private const float CatchUpSpeedMultiplier = 3.2f;

        private RotaryLayout layout;
        private float pathLength = 1f;
        private float unitSpacing = 0.14f;
        private float outerPathLength = 1f;
        private float leadOuterDistance;
        private float distanceSpeed;
        private float outerDistanceSpeed;
        private float[] centerDistanceSamples = new float[0];
        private float[] outerDistanceSamples = new float[0];

        public void Configure(RotaryLayout newLayout)
        {
            layout = newLayout;
            pathLength = Mathf.Max(0.01f, layout.Path.Length);
            BuildOuterDistanceMap();
            unitSpacing = outerPathLength / Mathf.Max(1, layout.Preset.MaxCapacityUnits);
            distanceSpeed = pathLength * layout.PassengerSpeed;
            outerDistanceSpeed = outerPathLength * layout.PassengerSpeed;
            leadOuterDistance = 0f;
        }

        public void AssignTraffic(PassengerView passenger, int queueIndex, float laneOffset)
        {
            passenger.AssignTrafficDistance(GetQueueDistance(queueIndex), pathLength, distanceSpeed, laneOffset, queueIndex);
        }

        public void Advance(IReadOnlyList<PassengerView> passengers, float deltaTime)
        {
            var baseDelta = Mathf.Max(0f, distanceSpeed * deltaTime);
            leadOuterDistance = Mathf.Repeat(leadOuterDistance + Mathf.Max(0f, outerDistanceSpeed * deltaTime), outerPathLength);

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.IsAssignedToRotary)
                {
                    continue;
                }

                var targetDistance = GetQueueDistance(passenger.RotarySlotIndex);
                if (!passenger.CanCirculate)
                {
                    passenger.SetTrafficDistance(targetDistance, pathLength);
                    continue;
                }

                var forwardGap = Mathf.Repeat(targetDistance - passenger.RouteDistance, pathLength);
                var speedMultiplier = forwardGap > unitSpacing * CatchUpGapMultiplier
                    ? CatchUpSpeedMultiplier
                    : 1f;

                passenger.MoveTrafficToward(targetDistance, baseDelta * speedMultiplier, pathLength);
            }
        }

        public PassengerUnitRoadPose GetPose(float routeDistance, float laneOffset, float centerZ, float y)
        {
            return layout.GetRotaryPoseByDistance(routeDistance, laneOffset, centerZ, y);
        }

        public float GetSlotDistance(int slotIndex)
        {
            return GetQueueDistance(slotIndex);
        }

        public float GetPredictedSlotDistance(int slotIndex, float secondsFromNow)
        {
            var futureOuterDistance = Mathf.Repeat(GetQueueOuterDistance(slotIndex) + Mathf.Max(0f, secondsFromNow) * outerDistanceSpeed, outerPathLength);
            return MapOuterDistanceToCenterDistance(futureOuterDistance);
        }

        public float GetProgressDistance(float progress)
        {
            return Mathf.Repeat(progress, 1f) * pathLength;
        }

        public float GetCircularDistance(float firstDistance, float secondDistance)
        {
            var forward = Mathf.Repeat(firstDistance - secondDistance, pathLength);
            var backward = Mathf.Repeat(secondDistance - firstDistance, pathLength);
            return Mathf.Min(forward, backward);
        }

        private float GetQueueDistance(int queueIndex)
        {
            return MapOuterDistanceToCenterDistance(GetQueueOuterDistance(queueIndex));
        }

        private float GetQueueOuterDistance(int queueIndex)
        {
            return Mathf.Repeat(leadOuterDistance + queueIndex * unitSpacing, outerPathLength);
        }

        private void BuildOuterDistanceMap()
        {
            var sampleCount = Mathf.Max(64, layout.MeshSampleCount);
            centerDistanceSamples = new float[sampleCount + 1];
            outerDistanceSamples = new float[sampleCount + 1];

            var previousPoint = GetOuterSpacingPoint(0f);
            centerDistanceSamples[0] = 0f;
            outerDistanceSamples[0] = 0f;

            for (var index = 1; index <= sampleCount; index++)
            {
                var centerDistance = pathLength * index / sampleCount;
                var point = GetOuterSpacingPoint(centerDistance);
                centerDistanceSamples[index] = centerDistance;
                outerDistanceSamples[index] = outerDistanceSamples[index - 1] + Vector2.Distance(previousPoint, point);
                previousPoint = point;
            }

            outerPathLength = Mathf.Max(0.01f, outerDistanceSamples[outerDistanceSamples.Length - 1]);
        }

        private Vector2 GetOuterSpacingPoint(float centerDistance)
        {
            var sample = layout.Path.SampleByDistance(centerDistance);
            return sample.Point + sample.Outward * layout.OuterSpacingOffset;
        }

        private float MapOuterDistanceToCenterDistance(float outerDistance)
        {
            outerDistance = Mathf.Repeat(outerDistance, outerPathLength);
            for (var index = 0; index < outerDistanceSamples.Length - 1; index++)
            {
                if (outerDistance > outerDistanceSamples[index + 1])
                {
                    continue;
                }

                var outerSegmentLength = Mathf.Max(0.0001f, outerDistanceSamples[index + 1] - outerDistanceSamples[index]);
                var t = Mathf.Clamp01((outerDistance - outerDistanceSamples[index]) / outerSegmentLength);
                return Mathf.Lerp(centerDistanceSamples[index], centerDistanceSamples[index + 1], t);
            }

            return 0f;
        }
    }
}
