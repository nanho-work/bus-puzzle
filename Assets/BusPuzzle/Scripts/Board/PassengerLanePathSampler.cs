using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerLanePathSampler
    {
        private struct LanePathCache
        {
            public float PathLength;
            public Vector2[] Points;
            public Vector2[] Tangents;
            public float[] Distances;
        }

        private RotaryLayout layout;
        private float centerPathLength = 1f;
        private float outerPathLength = 1f;
        private float[] centerDistanceSamples = new float[0];
        private readonly LanePathCache[] personLanePaths = new LanePathCache[PassengerUnitLayout.PeoplePerUnit];

        public float RoutePathLength => outerPathLength;

        public void Configure(RotaryLayout newLayout)
        {
            layout = newLayout;
            centerPathLength = Mathf.Max(0.01f, layout.Path.Length);
            BuildPersonLaneMaps();
        }

        public PassengerUnitRoadPose GetPose(float routeDistance, float centerZ, float y)
        {
            var person1Sample = SamplePersonLanePath(0, routeDistance);
            var person2Sample = SamplePersonLanePath(1, routeDistance);
            var person3Sample = SamplePersonLanePath(2, routeDistance);
            var person4Sample = SamplePersonLanePath(3, routeDistance);
            var tangent = person1Sample.Tangent + person2Sample.Tangent + person3Sample.Tangent + person4Sample.Tangent;
            tangent = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : person4Sample.Tangent;

            return PassengerUnitRoadPose.FromPersonWorldPositions(
                layout.ToWorldPoint(person1Sample.Point, centerZ, y),
                layout.ToWorldPoint(person2Sample.Point, centerZ, y),
                layout.ToWorldPoint(person3Sample.Point, centerZ, y),
                layout.ToWorldPoint(person4Sample.Point, centerZ, y),
                new Vector3(tangent.x, 0f, tangent.y));
        }

        public float GetProgressDistance(float progress)
        {
            return MapCenterDistanceToOuterDistance(Mathf.Repeat(progress, 1f) * centerPathLength);
        }

        private void BuildPersonLaneMaps()
        {
            var sampleCount = Mathf.Max(64, layout.MeshSampleCount);
            centerDistanceSamples = new float[sampleCount + 1];

            for (var index = 0; index <= sampleCount; index++)
            {
                centerDistanceSamples[index] = centerPathLength * index / sampleCount;
            }

            for (var personIndex = 0; personIndex < PassengerUnitLayout.PeoplePerUnit; personIndex++)
            {
                personLanePaths[personIndex] = BuildLanePathMap(layout.GetPersonLaneOffset(personIndex), sampleCount);
            }

            outerPathLength = Mathf.Max(0.01f, personLanePaths[PassengerUnitLayout.PeoplePerUnit - 1].PathLength);
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

        private RotaryPathSample SamplePersonLanePath(int personIndex, float outerDistance)
        {
            outerDistance = Mathf.Repeat(outerDistance, outerPathLength);
            var cache = personLanePaths[Mathf.Clamp(personIndex, 0, PassengerUnitLayout.PeoplePerUnit - 1)];
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
            var outerDistances = personLanePaths[PassengerUnitLayout.PeoplePerUnit - 1].Distances;
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
