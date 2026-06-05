using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Level Data", fileName = "LevelData")]
    public sealed class LevelData : ScriptableObject
    {
        public const int MinRotaryUnitCapacity = 18;
        public const int MaxRotaryUnitCapacity = 40;

        [SerializeField] private string levelName = "New Level";
        [SerializeField] private LevelDifficultyProfile difficultyProfile = LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
        [SerializeField] private RotaryRoadPresetId roadPresetId = RotaryRoadPresetId.Large;
        [SerializeField] private RoadPresetAsset roadPresetAsset = null;
        [SerializeField] private int rotaryUnitCapacity = MaxRotaryUnitCapacity;
        [SerializeField] private List<PuzzleColor> passengerUnits = new List<PuzzleColor>();
        [SerializeField] private PassengerFlowPlan passengerFlowPlan = new PassengerFlowPlan();
        [SerializeField] private List<BusDefinition> buses = new List<BusDefinition>();
        [SerializeField] private List<GarageDefinition> garages = new List<GarageDefinition>();

        [NonSerialized] private List<PuzzleColor> resolvedPassengerUnitsCache;
        [NonSerialized] private List<BusDefinition> allVehiclesCache;

        public string LevelName => levelName;
        public LevelDifficultyProfile DifficultyProfile => difficultyProfile != null && difficultyProfile.HasUsableValues
            ? difficultyProfile
            : LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
        public RotaryRoadPresetId RoadPresetId => roadPresetId;
        public RoadPresetDefinition RoadPreset => roadPresetAsset != null ? roadPresetAsset.ToDefinition() : RoadPresetLibrary.Get(roadPresetId);
        public int RotaryStartCapacity => Mathf.Clamp(rotaryUnitCapacity, MinRotaryUnitCapacity, MaxRotaryUnitCapacity);
        public int RotaryUnitCapacity => RotaryStartCapacity;
        public IReadOnlyList<PuzzleColor> PassengerUnits => GetResolvedPassengerUnits();
        public PassengerFlowPlan PassengerFlowPlan => passengerFlowPlan;
        public IReadOnlyList<BusDefinition> Buses => buses;
        public IReadOnlyList<GarageDefinition> Garages => garages ?? EmptyGarages;
        public IReadOnlyList<BusDefinition> AllVehicles => GetAllVehicles();
        public int PassengerUnitCount => PassengerUnits.Count;
        public int PassengerPeopleCount => PassengerUnits.Count * 4;

        private static readonly IReadOnlyList<GarageDefinition> EmptyGarages = Array.Empty<GarageDefinition>();

        public bool TryGetCapacityMismatchMessage(out string message)
        {
            var passengerCounts = new Dictionary<PuzzleColor, int>();
            var capacityCounts = new Dictionary<PuzzleColor, int>();
            var colors = new List<PuzzleColor>();

            var resolvedPassengerUnits = PassengerUnits;
            for (var index = 0; index < resolvedPassengerUnits.Count; index++)
            {
                AddCount(passengerCounts, resolvedPassengerUnits[index], 1);
                AddColor(colors, resolvedPassengerUnits[index]);
            }

            for (var index = 0; index < buses.Count; index++)
            {
                AddCount(capacityCounts, buses[index].Color, buses[index].CapacityUnits);
                AddColor(colors, buses[index].Color);
            }

            var allGarages = Garages;
            for (var garageIndex = 0; garageIndex < allGarages.Count; garageIndex++)
            {
                foreach (var garageVehicle in allGarages[garageIndex].EnumerateVehicles())
                {
                    AddCount(capacityCounts, garageVehicle.Color, garageVehicle.CapacityUnits);
                    AddColor(colors, garageVehicle.Color);
                }
            }

            var mismatches = new List<string>();
            for (var index = 0; index < colors.Count; index++)
            {
                var color = colors[index];
                passengerCounts.TryGetValue(color, out var passengerUnitCount);
                capacityCounts.TryGetValue(color, out var capacityUnitCount);
                if (passengerUnitCount == capacityUnitCount)
                {
                    continue;
                }

                mismatches.Add(
                    $"{PuzzlePalette.DisplayName(color)} passengers {passengerUnitCount * 4}, capacity {capacityUnitCount * 4}");
            }

            if (mismatches.Count == 0)
            {
                message = string.Empty;
                return false;
            }

            message = $"{levelName} has passenger/capacity mismatches: {string.Join("; ", mismatches)}";
            return true;
        }

        public void Configure(
            string newLevelName,
            IEnumerable<PuzzleColor> units,
            IEnumerable<BusDefinition> busDefinitions,
            int newRotaryUnitCapacity = MaxRotaryUnitCapacity,
            RotaryRoadPresetId newRoadPresetId = RotaryRoadPresetId.Large,
            IEnumerable<GarageDefinition> garageDefinitions = null)
        {
            levelName = newLevelName;
            difficultyProfile = LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            roadPresetId = newRoadPresetId;
            rotaryUnitCapacity = Mathf.Clamp(newRotaryUnitCapacity, MinRotaryUnitCapacity, MaxRotaryUnitCapacity);
            passengerUnits = new List<PuzzleColor>(units);
            buses = new List<BusDefinition>(busDefinitions);
            garages = garageDefinitions != null ? new List<GarageDefinition>(garageDefinitions) : new List<GarageDefinition>();
            passengerFlowPlan = new PassengerFlowPlan();
            InvalidatePassengerCache();
        }

        public void ConfigureWithPassengerFlowPlan(
            string newLevelName,
            LevelDifficultyProfile newDifficultyProfile,
            PassengerFlowPlan newPassengerFlowPlan,
            IEnumerable<BusDefinition> busDefinitions,
            int newRotaryUnitCapacity = MaxRotaryUnitCapacity,
            RotaryRoadPresetId newRoadPresetId = RotaryRoadPresetId.Large,
            IEnumerable<PuzzleColor> fallbackUnits = null,
            IEnumerable<GarageDefinition> garageDefinitions = null)
        {
            levelName = newLevelName;
            difficultyProfile = newDifficultyProfile ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            roadPresetId = newRoadPresetId;
            rotaryUnitCapacity = Mathf.Clamp(newRotaryUnitCapacity, MinRotaryUnitCapacity, MaxRotaryUnitCapacity);
            passengerUnits = fallbackUnits != null ? new List<PuzzleColor>(fallbackUnits) : new List<PuzzleColor>();
            passengerFlowPlan = newPassengerFlowPlan ?? new PassengerFlowPlan();
            buses = new List<BusDefinition>(busDefinitions);
            garages = garageDefinitions != null ? new List<GarageDefinition>(garageDefinitions) : new List<GarageDefinition>();
            InvalidatePassengerCache();
        }

        private IReadOnlyList<PuzzleColor> GetResolvedPassengerUnits()
        {
            if (resolvedPassengerUnitsCache == null)
            {
                resolvedPassengerUnitsCache = LevelPassengerBuilder.BuildPassengerUnits(
                    DifficultyProfile,
                    passengerFlowPlan,
                    passengerUnits,
                    AllVehicles,
                    RotaryStartCapacity,
                    GetStartingVisibleVehicles());
            }

            return resolvedPassengerUnitsCache;
        }

        private IReadOnlyList<BusDefinition> GetStartingVisibleVehicles()
        {
            var vehicles = new List<BusDefinition>();
            if (buses != null)
            {
                vehicles.AddRange(buses);
            }

            var allGarages = Garages;
            for (var garageIndex = 0; garageIndex < allGarages.Count; garageIndex++)
            {
                vehicles.Add(allGarages[garageIndex].FrontVehicle);
            }

            return vehicles;
        }

        private IReadOnlyList<BusDefinition> GetAllVehicles()
        {
            if (allVehiclesCache == null)
            {
                allVehiclesCache = new List<BusDefinition>();
                if (buses != null)
                {
                    allVehiclesCache.AddRange(buses);
                }

                var allGarages = Garages;
                for (var garageIndex = 0; garageIndex < allGarages.Count; garageIndex++)
                {
                    foreach (var garageVehicle in allGarages[garageIndex].EnumerateVehicles())
                    {
                        allVehiclesCache.Add(garageVehicle);
                    }
                }
            }

            return allVehiclesCache;
        }

        private void OnValidate()
        {
            InvalidatePassengerCache();
        }

        private void InvalidatePassengerCache()
        {
            resolvedPassengerUnitsCache = null;
            allVehiclesCache = null;
        }

        private static void AddCount(Dictionary<PuzzleColor, int> counts, PuzzleColor color, int amount)
        {
            counts.TryGetValue(color, out var current);
            counts[color] = current + amount;
        }

        private static void AddColor(List<PuzzleColor> colors, PuzzleColor color)
        {
            for (var index = 0; index < colors.Count; index++)
            {
                if (colors[index] == color)
                {
                    return;
                }
            }

            colors.Add(color);
        }
    }
}
