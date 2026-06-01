using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Level Data", fileName = "LevelData")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] private string levelName = "New Level";
        [SerializeField] private List<PuzzleColor> passengerQueue = new List<PuzzleColor>();
        [SerializeField] private List<BusDefinition> buses = new List<BusDefinition>();

        public string LevelName => levelName;
        public IReadOnlyList<PuzzleColor> PassengerQueue => passengerQueue;
        public IReadOnlyList<BusDefinition> Buses => buses;
        public int PassengerCount => passengerQueue.Count;

        public void Configure(string newLevelName, IEnumerable<PuzzleColor> passengers, IEnumerable<BusDefinition> busDefinitions)
        {
            levelName = newLevelName;
            passengerQueue = new List<PuzzleColor>(passengers);
            buses = new List<BusDefinition>(busDefinitions);
        }
    }
}
