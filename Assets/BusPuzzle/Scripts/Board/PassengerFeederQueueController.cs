using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerFeederQueueController
    {
        private readonly RotaryLayout rotaryLayout;
        private readonly PassengerFlowController passengerFlow;
        private readonly PassengerTrafficSettings settings;
        private readonly int rotaryActiveTarget;
        private readonly Action<PassengerView, int> assignTraffic;
        private readonly Action<PassengerView> setTrafficPose;

        public PassengerFeederQueueController(
            RotaryLayout rotaryLayout,
            PassengerFlowController passengerFlow,
            PassengerTrafficSettings settings,
            int rotaryActiveTarget,
            Action<PassengerView, int> assignTraffic,
            Action<PassengerView> setTrafficPose)
        {
            this.rotaryLayout = rotaryLayout;
            this.passengerFlow = passengerFlow;
            this.settings = settings;
            this.rotaryActiveTarget = rotaryActiveTarget;
            this.assignTraffic = assignTraffic;
            this.setTrafficPose = setTrafficPose;
        }

        public void Assign(PassengerView passenger, int feederQueueIndex)
        {
            var side = feederQueueIndex % 2 == 0 ? -1 : 1;
            var slot = feederQueueIndex / 2;
            passenger.AssignFeeder(side, slot);
        }

        public void SetPose(PassengerView passenger)
        {
            var pose = GetFeederPose(passenger.FeederSide, passenger.FeederSlotIndex);
            passenger.SetPose(pose.Position, pose.Rotation);
        }

        public void Promote(IReadOnlyList<PassengerView> passengers)
        {
            var targetCount = Mathf.Min(rotaryActiveTarget, passengers.Count);
            if (CountRotaryReservedPassengers(passengers) >= targetCount)
            {
                return;
            }

            if (HasFeederPassengers(passengers, 1))
            {
                TryPromoteFeederPassenger(passengers, 1);
                return;
            }

            TryPromoteFeederPassenger(passengers, -1);
        }

        private bool TryPromoteFeederPassenger(IReadOnlyList<PassengerView> passengers, int side)
        {
            if (!TryFindFeederPassenger(passengers, side, out var passenger))
            {
                return false;
            }

            if (!TryFindOpenRotarySlotAtFeeder(passengers, side, out var slotIndex))
            {
                return false;
            }

            var feederSlotIndex = passenger.FeederSlotIndex;
            passenger.AssignMergingToRotary(side, feederSlotIndex, slotIndex);
            passenger.MoveAlongDynamicPose(
                t => GetFeederMergePose(side, feederSlotIndex, slotIndex, t),
                settings.FeederMergeDuration,
                () =>
                {
                    assignTraffic(passenger, slotIndex);
                    setTrafficPose(passenger);
                });
            AdvanceFeederQueue(passengers, side, feederSlotIndex);
            return true;
        }

        private static int CountRotaryReservedPassengers(IReadOnlyList<PassengerView> passengers)
        {
            var count = 0;
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger.IsRotarySlotReserved)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasFeederPassengers(IReadOnlyList<PassengerView> passengers, int side)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger.IsWaitingInFeeder && passenger.FeederSide == side)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindFeederPassenger(IReadOnlyList<PassengerView> passengers, int side, out PassengerView passenger)
        {
            passenger = null;
            var bestSlot = int.MaxValue;

            for (var index = 0; index < passengers.Count; index++)
            {
                var candidate = passengers[index];
                if (!candidate.IsWaitingInFeeder || candidate.IsMoving || candidate.FeederSide != side || candidate.FeederSlotIndex >= bestSlot)
                {
                    continue;
                }

                passenger = candidate;
                bestSlot = candidate.FeederSlotIndex;
            }

            return passenger != null;
        }

        private bool TryFindOpenRotarySlotAtFeeder(IReadOnlyList<PassengerView> passengers, int side, out int slotIndex)
        {
            slotIndex = -1;
            var feederDistance = passengerFlow.GetProgressDistance(GetFeederJoinProgress(side));
            var bestDistance = float.MaxValue;

            for (var slot = 0; slot < rotaryLayout.CapacityUnits; slot++)
            {
                if (IsRotarySlotOccupied(passengers, slot))
                {
                    continue;
                }

                var vacancyDistance = passengerFlow.GetSlotDistance(slot);
                var distanceToFeeder = passengerFlow.GetCircularDistance(vacancyDistance, feederDistance);
                if (distanceToFeeder > settings.FeederVacancyWindowDistance || distanceToFeeder >= bestDistance)
                {
                    continue;
                }

                bestDistance = distanceToFeeder;
                slotIndex = slot;
            }

            return slotIndex >= 0;
        }

        private static bool IsRotarySlotOccupied(IReadOnlyList<PassengerView> passengers, int slot)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger.IsRotarySlotReserved && passenger.RotarySlotIndex == slot)
                {
                    return true;
                }
            }

            return false;
        }

        private void AdvanceFeederQueue(IReadOnlyList<PassengerView> passengers, int side, int removedSlotIndex)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.IsWaitingInFeeder || passenger.FeederSide != side || passenger.FeederSlotIndex <= removedSlotIndex)
                {
                    continue;
                }

                passenger.AssignFeeder(side, passenger.FeederSlotIndex - 1);
                var pose = GetFeederPose(side, passenger.FeederSlotIndex);
                passenger.MoveToPose(pose.Position, pose.Rotation, settings.FeederQueueStepDuration);
            }
        }

        private float GetFeederJoinProgress(int side)
        {
            return side < 0 ? rotaryLayout.Preset.LeftFeederProgress : rotaryLayout.Preset.RightFeederProgress;
        }

        private PassengerUnitRoadPose GetFeederPose(int side, int slotIndex)
        {
            return rotaryLayout.GetFeederPose(side, slotIndex, settings.RotaryCenterZ, settings.PassengerUnitY);
        }

        private PassengerUnitRoadPose GetFeederMergePose(int side, int feederSlotIndex, int rotarySlotIndex, float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            var feederPath = rotaryLayout.GetFeederPath(side);
            var startDistance = rotaryLayout.GetFeederDistanceForSlot(side, feederSlotIndex);
            var feederDistance = Mathf.Lerp(startDistance, feederPath.Length, normalizedTime);
            var feederPose = rotaryLayout.GetFeederPoseByDistance(side, feederDistance, settings.RotaryCenterZ, settings.PassengerUnitY);
            var targetPose = GetRotaryPoseByDistance(passengerFlow.GetSlotDistance(rotarySlotIndex));
            var mergeBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, normalizedTime));
            return BlendPassengerRoadPose(feederPose, targetPose, mergeBlend);
        }

        private PassengerUnitRoadPose BlendPassengerRoadPose(PassengerUnitRoadPose from, PassengerUnitRoadPose to, float t)
        {
            t = Mathf.Clamp01(t);
            return PassengerUnitRoadPose.FromPersonWorldPositions(
                Vector3.Lerp(GetPosePersonWorldPosition(from, 0), GetPosePersonWorldPosition(to, 0), t),
                Vector3.Lerp(GetPosePersonWorldPosition(from, 1), GetPosePersonWorldPosition(to, 1), t),
                Vector3.Lerp(GetPosePersonWorldPosition(from, 2), GetPosePersonWorldPosition(to, 2), t),
                Vector3.Lerp(GetPosePersonWorldPosition(from, 3), GetPosePersonWorldPosition(to, 3), t));
        }

        private Vector3 GetPosePersonWorldPosition(PassengerUnitRoadPose pose, int personIndex)
        {
            return pose.Position + pose.Rotation * GetPosePersonLocalPosition(pose, personIndex);
        }

        private Vector3 GetPosePersonLocalPosition(PassengerUnitRoadPose pose, int personIndex)
        {
            if (pose.HasCustomPersonLocalPositions)
            {
                switch (personIndex)
                {
                    case 0:
                        return pose.Person1LocalPosition;
                    case 1:
                        return pose.Person2LocalPosition;
                    case 2:
                        return pose.Person3LocalPosition;
                    default:
                        return pose.Person4LocalPosition;
                }
            }

            switch (personIndex)
            {
                case 0:
                    return new Vector3(0f, 0f, settings.PassengerPersonLocalZ.x);
                case 1:
                    return new Vector3(0f, 0f, settings.PassengerPersonLocalZ.y);
                case 2:
                    return new Vector3(0f, 0f, settings.PassengerPersonLocalZ.z);
                default:
                    return new Vector3(0f, 0f, settings.PassengerPersonLocalZ.w);
            }
        }

        private PassengerUnitRoadPose GetRotaryPoseByDistance(float routeDistance)
        {
            return passengerFlow.GetPose(routeDistance, settings.RotaryCenterZ, settings.PassengerUnitY);
        }
    }
}
