using System;
using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleBoardingSequence
    {
        private const float BoardingSpeedMultiplier = 2f;
        private const float BoardingWalkDuration = 0.58f / BoardingSpeedMultiplier;
        private const float BoardingPersonEnterDuration = 0.24f / BoardingSpeedMultiplier;
        private const float BoardingPersonEnterInterval = 0.075f / BoardingSpeedMultiplier;
        private const float BoardingPassengerY = 0.08f;

        public static void BoardPassenger(
            PassengerView passenger,
            Transform vehicle,
            PuzzleColor color,
            float cellSize,
            float visualFrontZ,
            float visualCharacterLength,
            PassengerUnitRoadPose? boardingGatePose,
            Action onComplete)
        {
            if (passenger == null || vehicle == null)
            {
                onComplete?.Invoke();
                return;
            }

            var approachPosition = vehicle.TransformPoint(new Vector3(0f, BoardingPassengerY, visualFrontZ + cellSize * 0.16f));
            var doorPosition = vehicle.TransformPoint(new Vector3(0f, BoardingPassengerY, visualFrontZ + cellSize * 0.02f));
            var entryPosition = vehicle.TransformPoint(new Vector3(0f, BoardingPassengerY, visualFrontZ - visualCharacterLength * 0.36f));

            passenger.BeginBoarding();
            passenger.WalkToBoard(
                approachPosition,
                doorPosition,
                entryPosition,
                BoardingWalkDuration,
                BoardingPersonEnterDuration,
                BoardingPersonEnterInterval,
                boardingGatePose,
                personPosition => EffectFactory.PlayBoardingAbsorb(personPosition, color, cellSize * 0.62f),
                () =>
                {
                    passenger.MarkBoarded();
                    EffectAudioPlayer.PlayBoarding(entryPosition);
                    UnityEngine.Object.Destroy(passenger.gameObject);
                    onComplete?.Invoke();
                });
        }
    }
}
