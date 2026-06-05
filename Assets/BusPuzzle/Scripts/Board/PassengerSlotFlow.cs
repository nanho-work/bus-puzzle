using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerSlotFlow
    {
        private const float CatchUpGapMultiplier = 1.35f;
        private const float CatchUpSpeedMultiplier = 3.2f;

        private float routePathLength = 1f;
        private float unitSpacing = 0.14f;
        private float leadDistance;
        private float distanceSpeed;

        public float RoutePathLength => routePathLength;
        public float DistanceSpeed => distanceSpeed;

        public void Configure(float newRoutePathLength, int maxCapacityUnits, float passengerSpeed)
        {
            routePathLength = Mathf.Max(0.01f, newRoutePathLength);
            unitSpacing = routePathLength / Mathf.Max(1, maxCapacityUnits);
            distanceSpeed = routePathLength * Mathf.Max(0f, passengerSpeed);
            leadDistance = 0f;
        }

        public void AdvanceLead(float deltaTime)
        {
            leadDistance = Mathf.Repeat(leadDistance + GetBaseDelta(deltaTime), routePathLength);
        }

        public float GetSlotDistance(int slotIndex)
        {
            return Mathf.Repeat(leadDistance + slotIndex * unitSpacing, routePathLength);
        }

        public float GetPredictedSlotDistance(int slotIndex, float secondsFromNow)
        {
            return Mathf.Repeat(GetSlotDistance(slotIndex) + Mathf.Max(0f, secondsFromNow) * distanceSpeed, routePathLength);
        }

        public float GetTravelDelta(float currentDistance, float targetDistance, float deltaTime)
        {
            var forwardGap = Mathf.Repeat(targetDistance - currentDistance, routePathLength);
            var speedMultiplier = forwardGap > unitSpacing * CatchUpGapMultiplier
                ? CatchUpSpeedMultiplier
                : 1f;
            return GetBaseDelta(deltaTime) * speedMultiplier;
        }

        public float GetCircularDistance(float firstDistance, float secondDistance)
        {
            var forward = Mathf.Repeat(firstDistance - secondDistance, routePathLength);
            var backward = Mathf.Repeat(secondDistance - firstDistance, routePathLength);
            return Mathf.Min(forward, backward);
        }

        public float GetForwardDistance(float fromDistance, float toDistance)
        {
            return Mathf.Repeat(toDistance - fromDistance, routePathLength);
        }

        private float GetBaseDelta(float deltaTime)
        {
            return Mathf.Max(0f, distanceSpeed * deltaTime);
        }
    }
}
