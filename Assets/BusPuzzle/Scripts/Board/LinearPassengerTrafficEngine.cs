using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class LinearPassengerTrafficEngine : IPassengerTrafficEngine
    {
        private const float UnitSpacing = 0.300f;
        private const float QueueAdvanceDuration = 0.20f;
        private const float RoutePathLength = 999f;

        private readonly PassengerTrafficSettings settings;
        private readonly PassengerUnitRoadPose boardingGatePose;
        private readonly Vector3[] queuePath;
        private readonly float[] queuePathDistances;
        private readonly float queuePathLength;

        public LinearPassengerTrafficEngine(PassengerTrafficSettings settings)
        {
            this.settings = settings;
            queuePath = CreateQueuePath(settings.PassengerUnitY);
            queuePathDistances = CreateQueuePathDistances(queuePath, out queuePathLength);
            boardingGatePose = CreatePose(
                new Vector3(0f, settings.PassengerUnitY, BoardLayoutConfig.StationZ + 0.48f),
                Vector3.back);
        }

        public void PlacePassenger(PassengerView passenger, int passengerIndex)
        {
            if (passenger == null)
            {
                return;
            }

            passenger.AssignTrafficDistance(passengerIndex * UnitSpacing, RoutePathLength, 0f, passengerIndex);
            passenger.SetPose(GetQueuePose(passengerIndex));
        }

        public void Advance(IReadOnlyList<PassengerView> passengers, float deltaTime, float trafficTimeScale)
        {
            if (passengers == null)
            {
                return;
            }

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger == null || !passenger.CanCirculate)
                {
                    continue;
                }

                var targetDistance = index * UnitSpacing;
                if (passenger.RotarySlotIndex == index)
                {
                    passenger.SetTrafficDistance(targetDistance, RoutePathLength);
                    passenger.SetPose(GetQueuePose(index));
                    continue;
                }

                passenger.AssignTrafficDistance(targetDistance, RoutePathLength, 0f, index);
                passenger.MoveToPoseFlat(GetQueuePose(index), QueueAdvanceDuration);
            }
        }

        public bool TryFindBoardingPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            return TryFindFrontPassenger(passengers, color, out passengerIndex);
        }

        public bool TryFindBoardingReservationPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            return TryFindFrontPassenger(passengers, color, out passengerIndex);
        }

        public bool IsPassengerReadyToBoard(PassengerView passenger)
        {
            return passenger != null && passenger.IsReservedForBoarding;
        }

        public PassengerUnitRoadPose GetBoardingGatePose()
        {
            return boardingGatePose;
        }

        public bool HasPendingRotaryFill(IReadOnlyList<PassengerView> passengers)
        {
            return false;
        }

        public void CompactFeederQueues(IReadOnlyList<PassengerView> passengers)
        {
        }

        private static bool TryFindFrontPassenger(
            IReadOnlyList<PassengerView> passengers,
            PuzzleColor color,
            out int passengerIndex)
        {
            passengerIndex = -1;
            if (passengers == null)
            {
                return false;
            }

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger == null || !passenger.gameObject.activeSelf)
                {
                    continue;
                }

                if (!passenger.CanReserveForBoarding || passenger.Color != color)
                {
                    return false;
                }

                passengerIndex = index;
                return true;
            }

            return false;
        }

        private PassengerUnitRoadPose GetQueuePose(int slotIndex)
        {
            slotIndex = Mathf.Max(0, slotIndex);
            return SampleQueuePose(Mathf.Min(slotIndex * UnitSpacing, queuePathLength));
        }

        private PassengerUnitRoadPose SampleQueuePose(float distance)
        {
            if (queuePath.Length < 2)
            {
                return CreatePose(Vector3.zero, Vector3.forward);
            }

            distance = Mathf.Clamp(distance, 0f, queuePathLength);
            for (var index = 0; index < queuePath.Length - 1; index++)
            {
                var segmentStartDistance = queuePathDistances[index];
                var segmentEndDistance = queuePathDistances[index + 1];
                if (distance > segmentEndDistance)
                {
                    continue;
                }

                var start = queuePath[index];
                var end = queuePath[index + 1];
                var segmentLength = Mathf.Max(0.001f, segmentEndDistance - segmentStartDistance);
                var t = Mathf.Clamp01((distance - segmentStartDistance) / segmentLength);
                return CreatePose(Vector3.Lerp(start, end, t), end - start);
            }

            return CreatePose(queuePath[queuePath.Length - 1], queuePath[queuePath.Length - 1] - queuePath[queuePath.Length - 2]);
        }

        private static Vector3[] CreateQueuePath(float y)
        {
            var stationZ = BoardLayoutConfig.StationZ;
            return new[]
            {
                new Vector3(-1.72f, y, stationZ + 1.48f),
                new Vector3(1.76f, y, stationZ + 1.48f),
                new Vector3(1.76f, y, stationZ + 1.96f),
                new Vector3(1.76f, y, stationZ + 4.08f),
                new Vector3(-2.05f, y, stationZ + 4.08f),
                new Vector3(-2.05f, y, stationZ + 4.42f),
                new Vector3(2.05f, y, stationZ + 4.42f),
                new Vector3(2.05f, y, stationZ + 4.76f),
                new Vector3(-2.05f, y, stationZ + 4.76f),
                new Vector3(-2.05f, y, stationZ + 5.10f),
                new Vector3(2.05f, y, stationZ + 5.10f),
                new Vector3(2.05f, y, stationZ + 5.44f),
                new Vector3(-2.05f, y, stationZ + 5.44f),
                new Vector3(-2.05f, y, stationZ + 5.78f),
                new Vector3(2.05f, y, stationZ + 5.78f),
                new Vector3(2.05f, y, stationZ + 6.12f),
                new Vector3(-2.05f, y, stationZ + 6.12f),
                new Vector3(-2.05f, y, stationZ + 6.46f),
                new Vector3(2.05f, y, stationZ + 6.46f),
                new Vector3(2.05f, y, stationZ + 6.80f),
                new Vector3(-2.05f, y, stationZ + 6.80f),
                new Vector3(-2.05f, y, stationZ + 7.14f),
                new Vector3(2.05f, y, stationZ + 7.14f),
                new Vector3(2.05f, y, stationZ + 7.48f),
                new Vector3(-2.05f, y, stationZ + 7.48f),
                new Vector3(-2.05f, y, stationZ + 7.82f),
                new Vector3(2.05f, y, stationZ + 7.82f),
                new Vector3(2.05f, y, stationZ + 8.16f),
                new Vector3(-2.05f, y, stationZ + 8.16f)
            };
        }

        private static float[] CreateQueuePathDistances(Vector3[] path, out float totalLength)
        {
            var distances = new float[path.Length];
            totalLength = 0f;
            for (var index = 1; index < path.Length; index++)
            {
                totalLength += Vector3.Distance(path[index - 1], path[index]);
                distances[index] = totalLength;
            }

            return distances;
        }

        private static PassengerUnitRoadPose CreatePose(Vector3 center, Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            var step = forward.normalized * (PassengerUnitLayout.GetPersonLocalZOffset(3) - PassengerUnitLayout.GetPersonLocalZOffset(2));
            return PassengerUnitRoadPose.FromPersonWorldPositions(
                center - step * 1.5f,
                center - step * 0.5f,
                center + step * 0.5f,
                center + step * 1.5f,
                forward);
        }
    }
}
