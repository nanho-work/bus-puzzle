using UnityEngine;

namespace BusPuzzle
{
    internal static class FeederQueueLayout
    {
        public const int PeoplePerUnit = 4;

        public static RotaryPathSample Sample(int side, FeederRoadPath path, float progress)
        {
            return ResolveSideAwareSample(side, path.Sample(progress));
        }

        public static RotaryPathSample SampleByDistance(int side, FeederRoadPath path, float distance)
        {
            return ResolveSideAwareSample(side, path.SampleByDistance(distance));
        }

        public static Vector2 GetPersonLanePoint(RotaryPathSample sample, PassengerRoadProfile roadProfile, int personIndex)
        {
            return sample.Point + sample.Outward * roadProfile.GetPersonLaneOffset(personIndex);
        }

        private static RotaryPathSample ResolveSideAwareSample(int side, RotaryPathSample sample)
        {
            var tangent = Normalize(sample.Tangent, Vector2.down);
            var outward = Normalize(sample.Outward, new Vector2(tangent.y, -tangent.x));
            var desiredSide = new Vector2(side < 0 ? -1f : 1f, 0f);
            var desiredOutward = desiredSide - tangent * Vector2.Dot(desiredSide, tangent);

            if (desiredOutward.sqrMagnitude > 0.0001f)
            {
                desiredOutward.Normalize();
                if (Vector2.Dot(outward, desiredOutward) < 0f)
                {
                    outward = -outward;
                }
            }
            else if (Vector2.Dot(outward, desiredSide) < 0f)
            {
                outward = -outward;
            }

            return new RotaryPathSample(sample.Point, tangent, outward);
        }

        private static Vector2 Normalize(Vector2 value, Vector2 fallback)
        {
            if (value.sqrMagnitude > 0.0001f)
            {
                return value.normalized;
            }

            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector2.right;
        }
    }

    internal sealed class FeederQueueLaneSampler
    {
        private struct LanePathCache
        {
            public float PathLength;
            public Vector2[] Points;
            public Vector2[] Tangents;
            public float[] Distances;
        }

        private readonly int side;
        private readonly FeederRoadPath path;
        private readonly PassengerRoadProfile roadProfile;
        private readonly LanePathCache[] personLanePaths = new LanePathCache[FeederQueueLayout.PeoplePerUnit];

        public FeederQueueLaneSampler(int side, FeederRoadPath path, PassengerRoadProfile roadProfile, int sampleCount)
        {
            this.side = side;
            this.path = path;
            this.roadProfile = roadProfile;
            sampleCount = Mathf.Clamp(sampleCount, 32, 192);

            for (var personIndex = 0; personIndex < personLanePaths.Length; personIndex++)
            {
                personLanePaths[personIndex] = BuildLanePathMap(roadProfile.GetPersonLaneOffset(personIndex), sampleCount);
            }

            ReferencePathLength = Mathf.Max(0.01f, personLanePaths[FeederQueueLayout.PeoplePerUnit - 1].PathLength);
        }

        public float ReferencePathLength { get; }

        public PassengerUnitRoadPose GetPose(float referenceDistance, RotaryLayout layout, float centerZ, float y)
        {
            var person1Sample = SamplePersonLanePath(0, referenceDistance);
            var person2Sample = SamplePersonLanePath(1, referenceDistance);
            var person3Sample = SamplePersonLanePath(2, referenceDistance);
            var person4Sample = SamplePersonLanePath(3, referenceDistance);
            var tangent = person1Sample.Tangent + person2Sample.Tangent + person3Sample.Tangent + person4Sample.Tangent;
            tangent = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : person4Sample.Tangent;

            return PassengerUnitRoadPose.FromPersonWorldPositions(
                layout.ToWorldPoint(person1Sample.Point, centerZ, y),
                layout.ToWorldPoint(person2Sample.Point, centerZ, y),
                layout.ToWorldPoint(person3Sample.Point, centerZ, y),
                layout.ToWorldPoint(person4Sample.Point, centerZ, y),
                new Vector3(tangent.x, 0f, tangent.y));
        }

        private LanePathCache BuildLanePathMap(float laneOffset, int sampleCount)
        {
            var points = new Vector2[sampleCount + 1];
            var tangents = new Vector2[sampleCount + 1];
            var distances = new float[sampleCount + 1];

            for (var index = 0; index <= sampleCount; index++)
            {
                var progress = index / (float)sampleCount;
                var sample = FeederQueueLayout.Sample(side, path, progress);
                points[index] = sample.Point + sample.Outward * laneOffset;
                if (index > 0)
                {
                    distances[index] = distances[index - 1] + Vector2.Distance(points[index - 1], points[index]);
                }
            }

            BuildOpenLaneTangents(points, tangents);
            return new LanePathCache
            {
                PathLength = Mathf.Max(0.01f, distances[distances.Length - 1]),
                Points = points,
                Tangents = tangents,
                Distances = distances
            };
        }

        private RotaryPathSample SamplePersonLanePath(int personIndex, float referenceDistance)
        {
            var cache = personLanePaths[Mathf.Clamp(personIndex, 0, personLanePaths.Length - 1)];
            var laneProgress = Mathf.Clamp01(referenceDistance / ReferencePathLength);
            var laneDistance = Mathf.Clamp(laneProgress * cache.PathLength, 0f, cache.PathLength);

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
                tangent = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.down;
                var outward = new Vector2(tangent.y, -tangent.x).normalized;
                return new RotaryPathSample(point, tangent, outward);
            }

            var lastIndex = cache.Points.Length - 1;
            var fallbackTangent = cache.Tangents.Length > 0 ? cache.Tangents[lastIndex] : Vector2.down;
            return new RotaryPathSample(cache.Points[lastIndex], fallbackTangent, new Vector2(fallbackTangent.y, -fallbackTangent.x).normalized);
        }

        private static void BuildOpenLaneTangents(Vector2[] points, Vector2[] tangents)
        {
            if (points.Length == 0)
            {
                return;
            }

            if (points.Length == 1)
            {
                tangents[0] = Vector2.down;
                return;
            }

            for (var index = 0; index < points.Length; index++)
            {
                Vector2 tangent;
                if (index == 0)
                {
                    tangent = points[1] - points[0];
                }
                else if (index == points.Length - 1)
                {
                    tangent = points[index] - points[index - 1];
                }
                else
                {
                    tangent = points[index + 1] - points[index - 1];
                }

                tangents[index] = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.down;
            }
        }
    }
}
