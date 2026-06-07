using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerFeederQueueController
    {
        private const float MinScaledFeederMergeDuration = 0.20f;
        private const float MinScaledFeederQueueStepDuration = 0.12f;
        private const float FeederInsertionLeadDuration = 0.055f;
        private const float FeederMergeBlendStart = 0.00f;
        private const float FeederMergeBlendEnd = 0.66f;

        private readonly RotaryLayout rotaryLayout;
        private readonly PassengerFlowController passengerFlow;
        private readonly PassengerTrafficSettings settings;
        private readonly int rotaryActiveTarget;
        private readonly Action<PassengerView, int> assignTraffic;
        private readonly Action<PassengerView> setTrafficPose;
        private readonly List<PassengerView> compactScratch = new List<PassengerView>();
        private int preferredFeederSide = 1;

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

        public void Promote(IReadOnlyList<PassengerView> passengers, float trafficTimeScale)
        {
            var targetCount = Mathf.Min(rotaryActiveTarget, passengers.Count);
            if (CountRotaryReservedPassengers(passengers) >= targetCount)
            {
                return;
            }

            var effectiveTrafficScale = Mathf.Max(0.01f, trafficTimeScale);
            var mergeDuration = GetScaledFeederMoveDuration(settings.FeederMergeDuration, effectiveTrafficScale, MinScaledFeederMergeDuration);
            var queueStepDuration = GetScaledFeederMoveDuration(settings.FeederQueueStepDuration, effectiveTrafficScale, MinScaledFeederQueueStepDuration);
            var predictionDuration = GetInsertionPredictionDuration(mergeDuration, effectiveTrafficScale);
            if (TryFindPromotionCandidate(passengers, predictionDuration, out var passenger, out var side, out var slotIndex))
            {
                PromoteFeederPassenger(passengers, passenger, side, slotIndex, mergeDuration, queueStepDuration);
            }
        }

        public bool HasPendingRotaryFill(IReadOnlyList<PassengerView> passengers)
        {
            if (HasMergingPassenger(passengers))
            {
                return true;
            }

            var targetCount = Mathf.Min(rotaryActiveTarget, passengers.Count);
            if (CountRotaryReservedPassengers(passengers) >= targetCount)
            {
                return false;
            }

            return HasFeederPassengers(passengers);
        }

        public void Compact(IReadOnlyList<PassengerView> passengers)
        {
            CompactSide(passengers, -1);
            CompactSide(passengers, 1);
        }

        private bool TryFindPromotionCandidate(
            IReadOnlyList<PassengerView> passengers,
            float predictionDuration,
            out PassengerView passenger,
            out int side,
            out int slotIndex)
        {
            passenger = null;
            side = preferredFeederSide;
            slotIndex = -1;
            var bestDistance = float.MaxValue;

            TryEvaluatePromotionSide(passengers, preferredFeederSide, predictionDuration, ref passenger, ref side, ref slotIndex, ref bestDistance);
            TryEvaluatePromotionSide(passengers, -preferredFeederSide, predictionDuration, ref passenger, ref side, ref slotIndex, ref bestDistance);
            return passenger != null;
        }

        private void TryEvaluatePromotionSide(
            IReadOnlyList<PassengerView> passengers,
            int candidateSide,
            float predictionDuration,
            ref PassengerView bestPassenger,
            ref int bestSide,
            ref int bestSlotIndex,
            ref float bestDistance)
        {
            if (!TryFindFeederPassenger(passengers, candidateSide, out var passenger))
            {
                return;
            }

            if (!TryFindOpenRotarySlotAtFeeder(passengers, candidateSide, predictionDuration, out var slotIndex, out var distanceToFeeder))
            {
                return;
            }

            if (distanceToFeeder >= bestDistance)
            {
                return;
            }

            bestPassenger = passenger;
            bestSide = candidateSide;
            bestSlotIndex = slotIndex;
            bestDistance = distanceToFeeder;
        }

        private void PromoteFeederPassenger(
            IReadOnlyList<PassengerView> passengers,
            PassengerView passenger,
            int side,
            int slotIndex,
            float mergeDuration,
            float queueStepDuration)
        {
            var feederSlotIndex = passenger.FeederSlotIndex;
            passenger.AssignMergingToRotary(side, feederSlotIndex, slotIndex);
            passenger.MoveAlongDynamicPose(
                t => GetFeederMergePose(side, feederSlotIndex, slotIndex, t),
                mergeDuration,
                () =>
                {
                    assignTraffic(passenger, slotIndex);
                    setTrafficPose(passenger);
                });
            AdvanceFeederQueue(passengers, side, feederSlotIndex, queueStepDuration);
            preferredFeederSide = -side;
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

        private static bool HasFeederPassengers(IReadOnlyList<PassengerView> passengers)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                if (passengers[index].IsWaitingInFeeder)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMergingPassenger(IReadOnlyList<PassengerView> passengers)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                if (passengers[index].IsMergingToRotary)
                {
                    return true;
                }
            }

            return false;
        }

        private void CompactSide(IReadOnlyList<PassengerView> passengers, int side)
        {
            compactScratch.Clear();
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger != null && passenger.IsWaitingInFeeder && passenger.FeederSide == side)
                {
                    compactScratch.Add(passenger);
                }
            }

            compactScratch.Sort(CompareFeederSlots);
            for (var slotIndex = 0; slotIndex < compactScratch.Count; slotIndex++)
            {
                var passenger = compactScratch[slotIndex];
                if (passenger.FeederSlotIndex == slotIndex)
                {
                    continue;
                }

                passenger.AssignFeeder(side, slotIndex);
                var pose = GetFeederPose(side, slotIndex);
                passenger.MoveToPose(pose.Position, pose.Rotation, settings.FeederQueueStepDuration);
            }

            compactScratch.Clear();
        }

        private static int CompareFeederSlots(PassengerView first, PassengerView second)
        {
            var slotCompare = first.FeederSlotIndex.CompareTo(second.FeederSlotIndex);
            return slotCompare != 0 ? slotCompare : first.GetInstanceID().CompareTo(second.GetInstanceID());
        }

        private static bool TryFindFeederPassenger(IReadOnlyList<PassengerView> passengers, int side, out PassengerView passenger)
        {
            passenger = null;
            var bestSlot = int.MaxValue;

            for (var index = 0; index < passengers.Count; index++)
            {
                var candidate = passengers[index];
                // Feeder step animations are interruptible; skipping them creates alternating rotary gaps.
                if (!candidate.IsWaitingInFeeder || candidate.FeederSide != side || candidate.FeederSlotIndex >= bestSlot)
                {
                    continue;
                }

                passenger = candidate;
                bestSlot = candidate.FeederSlotIndex;
            }

            return passenger != null;
        }

        private bool TryFindOpenRotarySlotAtFeeder(
            IReadOnlyList<PassengerView> passengers,
            int side,
            float predictionDuration,
            out int slotIndex,
            out float bestDistance)
        {
            slotIndex = -1;
            bestDistance = float.MaxValue;
            var feederDistance = passengerFlow.GetProgressDistance(GetFeederJoinProgress(side));
            predictionDuration = Mathf.Max(0f, predictionDuration);

            for (var slot = 0; slot < rotaryLayout.CapacityUnits; slot++)
            {
                if (IsRotarySlotOccupied(passengers, slot))
                {
                    continue;
                }

                var vacancyDistance = passengerFlow.GetPredictedSlotDistance(slot, predictionDuration);
                var distanceToFeeder = passengerFlow.GetForwardDistance(vacancyDistance, feederDistance);
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

        private void AdvanceFeederQueue(IReadOnlyList<PassengerView> passengers, int side, int removedSlotIndex, float queueStepDuration)
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
                passenger.MoveToPose(pose.Position, pose.Rotation, queueStepDuration);
            }
        }

        private static float GetScaledFeederMoveDuration(float baseDuration, float trafficTimeScale, float minDuration)
        {
            return Mathf.Max(minDuration, baseDuration / Mathf.Max(0.01f, trafficTimeScale));
        }

        private static float GetInsertionPredictionDuration(float mergeDuration, float trafficTimeScale)
        {
            var visualLeadDuration = FeederInsertionLeadDuration * Mathf.Max(0.01f, trafficTimeScale);
            return Mathf.Min(mergeDuration * trafficTimeScale, visualLeadDuration);
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
            var mergeBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FeederMergeBlendStart, FeederMergeBlendEnd, normalizedTime));
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
