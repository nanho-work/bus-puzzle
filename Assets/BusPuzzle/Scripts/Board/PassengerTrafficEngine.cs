using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct PassengerTrafficSettings
    {
        public readonly float RotaryCenterZ;
        public readonly float PassengerUnitY;
        public readonly float FeederMergeDuration;
        public readonly float FeederQueueStepDuration;
        public readonly float FeederVacancyWindowDistance;
        public readonly float BoardingGateProgressWindow;
        public readonly float BoardingReservationProgressWindow;
        public readonly Vector4 PassengerPersonLocalZ;

        public PassengerTrafficSettings(
            float rotaryCenterZ,
            float passengerUnitY,
            float feederMergeDuration,
            float feederQueueStepDuration,
            float feederVacancyWindowDistance,
            float boardingGateProgressWindow,
            float boardingReservationProgressWindow,
            Vector4 passengerPersonLocalZ)
        {
            RotaryCenterZ = rotaryCenterZ;
            PassengerUnitY = passengerUnitY;
            FeederMergeDuration = feederMergeDuration;
            FeederQueueStepDuration = feederQueueStepDuration;
            FeederVacancyWindowDistance = feederVacancyWindowDistance;
            BoardingGateProgressWindow = boardingGateProgressWindow;
            BoardingReservationProgressWindow = boardingReservationProgressWindow;
            PassengerPersonLocalZ = passengerPersonLocalZ;
        }
    }

    internal interface IPassengerTrafficEngine
    {
        void PlacePassenger(PassengerView passenger, int passengerIndex);
        void Advance(IReadOnlyList<PassengerView> passengers, float deltaTime, float trafficTimeScale);
        bool TryFindBoardingPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex);
        bool TryFindBoardingReservationPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex);
        bool IsPassengerReadyToBoard(PassengerView passenger);
        PassengerUnitRoadPose GetBoardingGatePose();
        bool HasPendingRotaryFill(IReadOnlyList<PassengerView> passengers);
        void CompactFeederQueues(IReadOnlyList<PassengerView> passengers);
    }

    internal sealed class PassengerTrafficEngine : IPassengerTrafficEngine
    {
        private readonly PassengerFlowController passengerFlow = new PassengerFlowController();
        private readonly RotaryLayout rotaryLayout;
        private readonly PassengerTrafficSettings settings;
        private readonly int rotaryActiveTarget;
        private readonly PassengerBoardingSelector boardingSelector;
        private readonly PassengerFeederQueueController feederQueue;

        public PassengerTrafficEngine(RotaryLayout rotaryLayout, PassengerTrafficSettings settings, int rotaryActiveTarget)
        {
            this.rotaryLayout = rotaryLayout;
            this.settings = settings;
            this.rotaryActiveTarget = Mathf.Clamp(rotaryActiveTarget, 0, rotaryLayout.CapacityUnits);
            passengerFlow.Configure(rotaryLayout, this.rotaryActiveTarget);
            boardingSelector = new PassengerBoardingSelector(passengerFlow, rotaryLayout, settings);
            feederQueue = new PassengerFeederQueueController(
                rotaryLayout,
                passengerFlow,
                settings,
                this.rotaryActiveTarget,
                AssignPassengerTraffic,
                SetPassengerTrafficPose);
        }

        public void PlacePassenger(PassengerView passenger, int passengerIndex)
        {
            if (passengerIndex < rotaryActiveTarget)
            {
                AssignPassengerTraffic(passenger, passengerIndex);
                SetPassengerTrafficPose(passenger);
                return;
            }

            feederQueue.Assign(passenger, passengerIndex - rotaryActiveTarget);
            feederQueue.SetPose(passenger);
        }

        public void Advance(IReadOnlyList<PassengerView> passengers, float deltaTime, float trafficTimeScale)
        {
            passengerFlow.Advance(passengers, deltaTime);

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.CanCirculate)
                {
                    continue;
                }

                SetPassengerTrafficPose(passenger);
            }

            feederQueue.Promote(passengers, trafficTimeScale);
        }

        public bool TryFindBoardingPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            return boardingSelector.TryFindBoardingPassenger(passengers, color, out passengerIndex);
        }

        public bool TryFindBoardingReservationPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            return boardingSelector.TryFindBoardingReservationPassenger(passengers, color, out passengerIndex);
        }

        public bool IsPassengerReadyToBoard(PassengerView passenger)
        {
            return boardingSelector.IsPassengerReadyToBoard(passenger);
        }

        public PassengerUnitRoadPose GetBoardingGatePose()
        {
            var gateDistance = passengerFlow.GetProgressDistance(rotaryLayout.Preset.BoardingGateProgress);
            return GetRotaryPoseByDistance(gateDistance);
        }

        public static bool HasRotaryPassengerColor(IReadOnlyList<PassengerView> passengers, PuzzleColor color)
        {
            return PassengerBoardingSelector.HasRotaryPassengerColor(passengers, color);
        }

        public bool HasPendingRotaryFill(IReadOnlyList<PassengerView> passengers)
        {
            return feederQueue.HasPendingRotaryFill(passengers);
        }

        public void CompactFeederQueues(IReadOnlyList<PassengerView> passengers)
        {
            feederQueue.Compact(passengers);
        }

        private void AssignPassengerTraffic(PassengerView passenger, int rotarySlotIndex)
        {
            var clampedSlotIndex = Mathf.Clamp(rotarySlotIndex, 0, Mathf.Max(0, rotaryActiveTarget - 1));
            passengerFlow.AssignTraffic(passenger, clampedSlotIndex);
        }

        private PassengerUnitRoadPose GetRotaryPoseByDistance(float routeDistance)
        {
            return passengerFlow.GetPose(routeDistance, settings.RotaryCenterZ, settings.PassengerUnitY);
        }

        private void SetPassengerTrafficPose(PassengerView passenger)
        {
            var pose = GetRotaryPoseByDistance(passenger.RouteDistance);
            passenger.SetPose(pose);
        }
    }
}
