using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerBoardingSelector
    {
        private readonly PassengerFlowController passengerFlow;
        private readonly RotaryLayout rotaryLayout;
        private readonly PassengerTrafficSettings settings;

        public PassengerBoardingSelector(PassengerFlowController passengerFlow, RotaryLayout rotaryLayout, PassengerTrafficSettings settings)
        {
            this.passengerFlow = passengerFlow;
            this.rotaryLayout = rotaryLayout;
            this.settings = settings;
        }

        public bool TryFindBoardingPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            var bestDistance = float.MaxValue;
            passengerIndex = -1;

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.CanCirculate || passenger.IsReservedForBoarding || passenger.Color != color || !IsPassengerAtBoardingGate(passenger))
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(passenger.transform.position - GetBoardingGatePosition());
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                passengerIndex = index;
            }

            return passengerIndex >= 0;
        }

        public bool TryFindBoardingReservationPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            var bestForwardDistance = float.MaxValue;
            passengerIndex = -1;

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.CanReserveForBoarding || passenger.Color != color)
                {
                    continue;
                }

                var forwardDistance = GetForwardDistanceToBoardingGate(passenger);
                if (forwardDistance > passengerFlow.RoutePathLength * settings.BoardingReservationProgressWindow || forwardDistance >= bestForwardDistance)
                {
                    continue;
                }

                bestForwardDistance = forwardDistance;
                passengerIndex = index;
            }

            return passengerIndex >= 0;
        }

        public bool IsPassengerReadyToBoard(PassengerView passenger)
        {
            return passenger != null && passenger.IsReservedForBoarding && IsPassengerAtBoardingGate(passenger);
        }

        public static bool HasRotaryPassengerColor(IReadOnlyList<PassengerView> passengers, PuzzleColor color)
        {
            if (passengers == null)
            {
                return false;
            }

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger != null &&
                    passenger.Color == color &&
                    passenger.gameObject.activeSelf &&
                    passenger.IsAssignedToRotary)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetBoardingGatePosition()
        {
            var gateDistance = passengerFlow.GetProgressDistance(rotaryLayout.Preset.BoardingGateProgress);
            return GetRotaryPoseByDistance(gateDistance).Position;
        }

        private bool IsPassengerAtBoardingGate(PassengerView passenger)
        {
            var gateDistance = passengerFlow.GetProgressDistance(rotaryLayout.Preset.BoardingGateProgress);
            var gateWindow = passengerFlow.RoutePathLength * settings.BoardingGateProgressWindow;
            return passengerFlow.GetForwardDistance(passenger.RouteDistance, gateDistance) <= gateWindow ||
                passengerFlow.GetCircularDistance(passenger.RouteDistance, gateDistance) <= gateWindow * 0.35f;
        }

        private float GetForwardDistanceToBoardingGate(PassengerView passenger)
        {
            var gateDistance = passengerFlow.GetProgressDistance(rotaryLayout.Preset.BoardingGateProgress);
            return passengerFlow.GetForwardDistance(passenger.RouteDistance, gateDistance);
        }

        private PassengerUnitRoadPose GetRotaryPoseByDistance(float routeDistance)
        {
            return passengerFlow.GetPose(routeDistance, settings.RotaryCenterZ, settings.PassengerUnitY);
        }
    }
}
