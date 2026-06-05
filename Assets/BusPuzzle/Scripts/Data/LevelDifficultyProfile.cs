using System;
using UnityEngine;

namespace BusPuzzle
{
    public enum LevelDifficulty
    {
        Normal = 0,
        Hard = 1,
        SuperHard = 2
    }

    [Serializable]
    public sealed class LevelDifficultyProfile
    {
        [SerializeField] private LevelDifficulty difficulty = LevelDifficulty.Normal;
        [SerializeField] private PassengerFlowDifficultyRule passengerFlowRule = PassengerFlowDifficultyRule.DefaultFor(LevelDifficulty.Normal);
        [SerializeField, Range(4, 48)] private int targetVehicleCount = 12;
        [SerializeField, Range(2, 10)] private int targetColorCount = 6;
        [SerializeField, Range(0f, 1f)] private float parkingTension = 0.35f;
        [SerializeField, Range(0f, 1f)] private float stationPressure = 0.30f;
        [SerializeField] private bool requireSolutionRoute;

        public LevelDifficulty Difficulty => difficulty;
        public PassengerFlowDifficultyRule PassengerFlowRule => passengerFlowRule.HasUsableValues
            ? passengerFlowRule
            : PassengerFlowDifficultyRule.DefaultFor(difficulty);
        public int TargetVehicleCount => Mathf.Clamp(targetVehicleCount, 4, 48);
        public int TargetColorCount => Mathf.Clamp(targetColorCount, 2, 10);
        public float ParkingTension => Mathf.Clamp01(parkingTension);
        public float StationPressure => Mathf.Clamp01(stationPressure);
        public bool RequireSolutionRoute => requireSolutionRoute;
        public bool HasUsableValues => passengerFlowRule.HasUsableValues && targetVehicleCount > 0 && targetColorCount > 0;

        public static LevelDifficultyProfile DefaultFor(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:
                    return new LevelDifficultyProfile
                    {
                        difficulty = difficulty,
                        passengerFlowRule = PassengerFlowDifficultyRule.DefaultFor(difficulty),
                        targetVehicleCount = 18,
                        targetColorCount = 8,
                        parkingTension = 0.58f,
                        stationPressure = 0.55f,
                        requireSolutionRoute = true
                    };
                case LevelDifficulty.SuperHard:
                    return new LevelDifficultyProfile
                    {
                        difficulty = difficulty,
                        passengerFlowRule = PassengerFlowDifficultyRule.DefaultFor(difficulty),
                        targetVehicleCount = 26,
                        targetColorCount = 10,
                        parkingTension = 0.78f,
                        stationPressure = 0.78f,
                        requireSolutionRoute = true
                    };
                default:
                    return new LevelDifficultyProfile
                    {
                        difficulty = LevelDifficulty.Normal,
                        passengerFlowRule = PassengerFlowDifficultyRule.DefaultFor(LevelDifficulty.Normal),
                        targetVehicleCount = 12,
                        targetColorCount = 6,
                        parkingTension = 0.35f,
                        stationPressure = 0.30f,
                        requireSolutionRoute = false
                    };
            }
        }
    }
}
