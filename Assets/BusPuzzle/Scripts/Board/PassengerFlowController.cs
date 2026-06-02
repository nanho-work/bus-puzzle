using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerFlowController
    {
        private const int PeoplePerUnit = 4;
        private const float CatchUpGapMultiplier = 1.35f;
        private const float CatchUpSpeedMultiplier = 3.2f;

        private struct LanePathCache
        {
            public float PathLength;
            public Vector2[] Points;
            public Vector2[] Tangents;
            public float[] Distances;
        }

        private RotaryLayout layout;
        private float centerPathLength = 1f;
        private float routePathLength = 1f;
        private float unitSpacing = 0.14f;
        private float outerPathLength = 1f;
        private float leadOuterDistance;
        private float outerDistanceSpeed;
        private float[] centerDistanceSamples = new float[0];
        private readonly LanePathCache[] personLanePaths = new LanePathCache[PeoplePerUnit];

        public float RoutePathLength => routePathLength;

        public void Configure(RotaryLayout newLayout)
        {
            layout = newLayout;
            centerPathLength = Mathf.Max(0.01f, layout.Path.Length);
            BuildPersonLaneMaps();
            unitSpacing = outerPathLength / Mathf.Max(1, layout.Preset.MaxCapacityUnits);
            outerDistanceSpeed = outerPathLength * layout.PassengerSpeed;
            routePathLength = outerPathLength;
            leadOuterDistance = 0f;
        }

        public void AssignTraffic(PassengerView passenger, int queueIndex)
        {
            passenger.AssignTrafficDistance(GetQueueDistance(queueIndex), routePathLength, outerDistanceSpeed, queueIndex);
        }

        public void Advance(IReadOnlyList<PassengerView> passengers, float deltaTime)
        {
            var baseDelta = Mathf.Max(0f, outerDistanceSpeed * deltaTime);
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
                    passenger.SetTrafficDistance(targetDistance, routePathLength);
                    continue;
                }

                var forwardGap = Mathf.Repeat(targetDistance - passenger.RouteDistance, routePathLength);
                var speedMultiplier = forwardGap > unitSpacing * CatchUpGapMultiplier
                    ? CatchUpSpeedMultiplier
                    : 1f;

                passenger.MoveTrafficToward(targetDistance, baseDelta * speedMultiplier, routePathLength);
            }
        }

        public PassengerUnitRoadPose GetPose(float routeDistance, float centerZ, float y)
        {
            return PassengerUnitRoadPose.FromPersonWorldPositions(
                GetLaneWorldPoint(0, routeDistance, centerZ, y),
                GetLaneWorldPoint(1, routeDistance, centerZ, y),
                GetLaneWorldPoint(2, routeDistance, centerZ, y),
                GetLaneWorldPoint(3, routeDistance, centerZ, y));
        }

        public float GetSlotDistance(int slotIndex)
        {
            return GetQueueDistance(slotIndex);
        }

        public float GetPredictedSlotDistance(int slotIndex, float secondsFromNow)
        {
            var futureOuterDistance = Mathf.Repeat(GetQueueOuterDistance(slotIndex) + Mathf.Max(0f, secondsFromNow) * outerDistanceSpeed, outerPathLength);
            return futureOuterDistance;
        }

        public float GetProgressDistance(float progress)
        {
            return MapCenterDistanceToOuterDistance(Mathf.Repeat(progress, 1f) * centerPathLength);
        }

        public float GetCircularDistance(float firstDistance, float secondDistance)
        {
            var forward = Mathf.Repeat(firstDistance - secondDistance, routePathLength);
            var backward = Mathf.Repeat(secondDistance - firstDistance, routePathLength);
            return Mathf.Min(forward, backward);
        }

        private float GetQueueDistance(int queueIndex)
        {
            return GetQueueOuterDistance(queueIndex);
        }

        private float GetQueueOuterDistance(int queueIndex)
        {
            return Mathf.Repeat(leadOuterDistance + queueIndex * unitSpacing, outerPathLength);
        }

        private void BuildPersonLaneMaps()
        {
            var sampleCount = Mathf.Max(64, layout.MeshSampleCount);
            centerDistanceSamples = new float[sampleCount + 1];

            for (var index = 0; index <= sampleCount; index++)
            {
                centerDistanceSamples[index] = centerPathLength * index / sampleCount;
            }

            for (var personIndex = 0; personIndex < PeoplePerUnit; personIndex++)
            {
                personLanePaths[personIndex] = BuildLanePathMap(layout.GetPersonLaneOffset(personIndex), sampleCount);
            }

            outerPathLength = Mathf.Max(0.01f, personLanePaths[PeoplePerUnit - 1].PathLength);
        }

        private LanePathCache BuildLanePathMap(float laneOffset, int sampleCount)
        {
            var points = new Vector2[sampleCount + 1];
            var tangents = new Vector2[sampleCount + 1];
            var distances = new float[sampleCount + 1];

            points[0] = GetLanePoint(0f, laneOffset);
            distances[0] = 0f;
            for (var index = 1; index <= sampleCount; index++)
            {
                var centerDistance = centerDistanceSamples[index];
                points[index] = GetLanePoint(centerDistance, laneOffset);
                distances[index] = distances[index - 1] + Vector2.Distance(points[index - 1], points[index]);
            }

            BuildLaneTangents(points, tangents);
            return new LanePathCache
            {
                PathLength = Mathf.Max(0.01f, distances[distances.Length - 1]),
                Points = points,
                Tangents = tangents,
                Distances = distances
            };
        }

        private Vector2 GetLanePoint(float centerDistance, float laneOffset)
        {
            var sample = layout.Path.SampleByDistance(centerDistance);
            return sample.Point + sample.Outward * laneOffset;
        }

        private static void BuildLaneTangents(Vector2[] points, Vector2[] tangents)
        {
            if (points.Length == 0)
            {
                return;
            }

            var duplicateIndex = points.Length - 1;
            var lastUniqueIndex = duplicateIndex - 1;
            for (var index = 0; index < points.Length; index++)
            {
                var previous = index == 0 ? lastUniqueIndex : index - 1;
                var next = index == duplicateIndex ? 1 : index + 1;
                var tangent = points[next] - points[previous];
                tangents[index] = tangent.sqrMagnitude > 0.0001f
                    ? tangent.normalized
                    : Vector2.right;
            }
        }

        private Vector3 GetLaneWorldPoint(int personIndex, float outerDistance, float centerZ, float y)
        {
            var sample = SamplePersonLanePath(personIndex, outerDistance);
            return layout.ToWorldPoint(sample.Point, centerZ, y);
        }

        private RotaryPathSample SamplePersonLanePath(int personIndex, float outerDistance)
        {
            outerDistance = Mathf.Repeat(outerDistance, outerPathLength);
            var cache = personLanePaths[Mathf.Clamp(personIndex, 0, PeoplePerUnit - 1)];
            var laneProgress = outerDistance / outerPathLength;
            var laneDistance = Mathf.Repeat(laneProgress * cache.PathLength, cache.PathLength);

            for (var index = 0; index < cache.Distances.Length - 1; index++)
            {
                if (laneDistance > cache.Distances[index + 1])
                {
                    continue;
                }

                var segmentLength = Mathf.Max(0.0001f, cache.Distances[index + 1] - cache.Distances[index]);
                var t = Mathf.Clamp01((laneDistance - cache.Distances[index]) / segmentLength);
                var point = Vector2.Lerp(cache.Points[index], cache.Points[index + 1], t);
                var tangent = Vector2.Lerp(cache.Tangents[index], cache.Tangents[index + 1], t);
                tangent = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.right;
                var outward = new Vector2(tangent.y, -tangent.x).normalized;
                return new RotaryPathSample(point, tangent, outward);
            }

            var fallbackTangent = cache.Tangents.Length > 0 ? cache.Tangents[0] : Vector2.right;
            return new RotaryPathSample(cache.Points[0], fallbackTangent, new Vector2(fallbackTangent.y, -fallbackTangent.x).normalized);
        }

        private float MapCenterDistanceToOuterDistance(float centerDistance)
        {
            centerDistance = Mathf.Repeat(centerDistance, centerPathLength);
            var outerDistances = personLanePaths[PeoplePerUnit - 1].Distances;
            for (var index = 0; index < centerDistanceSamples.Length - 1; index++)
            {
                if (centerDistance > centerDistanceSamples[index + 1])
                {
                    continue;
                }

                var centerSegmentLength = Mathf.Max(0.0001f, centerDistanceSamples[index + 1] - centerDistanceSamples[index]);
                var t = Mathf.Clamp01((centerDistance - centerDistanceSamples[index]) / centerSegmentLength);
                return Mathf.Lerp(outerDistances[index], outerDistances[index + 1], t);
            }

            return 0f;
        }
    }
}
