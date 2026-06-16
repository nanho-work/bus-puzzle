using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerFlowController
    {
        private readonly PassengerLanePathSampler lanePathSampler = new PassengerLanePathSampler();
        private readonly PassengerSlotFlow slotFlow = new PassengerSlotFlow();

        public float RoutePathLength => slotFlow.RoutePathLength;

        public void Configure(RotaryLayout newLayout, int visibleCapacityUnits)
        {
            lanePathSampler.Configure(newLayout);
            slotFlow.Configure(
                lanePathSampler.RoutePathLength,
                Mathf.Clamp(visibleCapacityUnits, 1, newLayout.CapacityUnits),
                newLayout.PassengerSpeed);
        }

        public void AssignTraffic(PassengerView passenger, int queueIndex)
        {
            passenger.AssignTrafficDistance(GetSlotDistance(queueIndex), RoutePathLength, slotFlow.DistanceSpeed, queueIndex);
        }

        public void Advance(IReadOnlyList<PassengerView> passengers, float deltaTime)
        {
            slotFlow.AdvanceLead(deltaTime);

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.IsAssignedToRotary)
                {
                    continue;
                }

                var targetDistance = GetSlotDistance(passenger.RotarySlotIndex);
                if (!passenger.CanCirculate)
                {
                    passenger.SetTrafficDistance(targetDistance, RoutePathLength);
                    continue;
                }

                passenger.MoveTrafficToward(targetDistance, slotFlow.GetTravelDelta(passenger.RouteDistance, targetDistance, deltaTime), RoutePathLength);
            }
        }

        public PassengerUnitRoadPose GetPose(float routeDistance, float centerZ, float y)
        {
            return lanePathSampler.GetPose(routeDistance, centerZ, y);
        }

        public float GetSlotDistance(int slotIndex)
        {
            return slotFlow.GetSlotDistance(slotIndex);
        }

        public float GetPredictedSlotDistance(int slotIndex, float secondsFromNow)
        {
            return slotFlow.GetPredictedSlotDistance(slotIndex, secondsFromNow);
        }

        public float GetProgressDistance(float progress)
        {
            return lanePathSampler.GetProgressDistance(progress);
        }

        public float GetCircularDistance(float firstDistance, float secondDistance)
        {
            return slotFlow.GetCircularDistance(firstDistance, secondDistance);
        }

        public float GetForwardDistance(float fromDistance, float toDistance)
        {
            return slotFlow.GetForwardDistance(fromDistance, toDistance);
        }
    }
}
