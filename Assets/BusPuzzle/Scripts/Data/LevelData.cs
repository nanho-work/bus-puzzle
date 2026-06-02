using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Level Data", fileName = "LevelData")]
    public sealed class LevelData : ScriptableObject
    {
        public const int MinRotaryUnitCapacity = 18;
        public const int MaxRotaryUnitCapacity = 50;

        [SerializeField] private string levelName = "New Level";
        [SerializeField] private RotaryRoadPresetId roadPresetId = RotaryRoadPresetId.Large;
        [SerializeField] private RoadPresetAsset roadPresetAsset = null;
        [SerializeField] private int rotaryUnitCapacity = MaxRotaryUnitCapacity;
        [SerializeField] private List<PuzzleColor> passengerUnits = new List<PuzzleColor>();
        [SerializeField] private List<BusDefinition> buses = new List<BusDefinition>();

        public string LevelName => levelName;
        public RotaryRoadPresetId RoadPresetId => roadPresetId;
        public RoadPresetDefinition RoadPreset => roadPresetAsset != null ? roadPresetAsset.ToDefinition() : RoadPresetLibrary.Get(roadPresetId);
        public int RotaryStartCapacity => Mathf.Clamp(rotaryUnitCapacity, MinRotaryUnitCapacity, MaxRotaryUnitCapacity);
        public int RotaryUnitCapacity => RotaryStartCapacity;
        public IReadOnlyList<PuzzleColor> PassengerUnits => passengerUnits;
        public IReadOnlyList<BusDefinition> Buses => buses;
        public int PassengerUnitCount => passengerUnits.Count;
        public int PassengerPeopleCount => passengerUnits.Count * 4;

        public void Configure(
            string newLevelName,
            IEnumerable<PuzzleColor> units,
            IEnumerable<BusDefinition> busDefinitions,
            int newRotaryUnitCapacity = MaxRotaryUnitCapacity,
            RotaryRoadPresetId newRoadPresetId = RotaryRoadPresetId.Large)
        {
            levelName = newLevelName;
            roadPresetId = newRoadPresetId;
            rotaryUnitCapacity = Mathf.Clamp(newRotaryUnitCapacity, MinRotaryUnitCapacity, MaxRotaryUnitCapacity);
            passengerUnits = new List<PuzzleColor>(units);
            buses = new List<BusDefinition>(busDefinitions);
        }
    }
}
