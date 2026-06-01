using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Level Sequence", fileName = "LevelSequence")]
    public sealed class LevelSequence : ScriptableObject
    {
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

        public int Count => levels.Count;

        public LevelData GetLevel(int index)
        {
            if (levels.Count == 0)
            {
                return null;
            }

            return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
        }

        public void Configure(IEnumerable<LevelData> newLevels)
        {
            levels = new List<LevelData>(newLevels);
        }

        public static LevelSequence CreateRuntimeFallback()
        {
            var sequence = CreateInstance<LevelSequence>();
            sequence.hideFlags = HideFlags.DontSave;

            var levelOne = CreateLevel(
                "Downtown Warmup",
                new[] { PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Yellow, PuzzleColor.Yellow },
                new[] { new BusDefinition(PuzzleColor.Red, 2), new BusDefinition(PuzzleColor.Blue, 2), new BusDefinition(PuzzleColor.Yellow, 2) });

            var levelTwo = CreateLevel(
                "Crosswalk Mix",
                new[] { PuzzleColor.Red, PuzzleColor.Green, PuzzleColor.Blue, PuzzleColor.Green, PuzzleColor.Red, PuzzleColor.Yellow, PuzzleColor.Blue, PuzzleColor.Yellow },
                new[] { new BusDefinition(PuzzleColor.Red, 2), new BusDefinition(PuzzleColor.Blue, 2), new BusDefinition(PuzzleColor.Green, 2), new BusDefinition(PuzzleColor.Yellow, 2) });

            var levelThree = CreateLevel(
                "Terminal Shuffle",
                new[] { PuzzleColor.Purple, PuzzleColor.Red, PuzzleColor.Orange, PuzzleColor.Purple, PuzzleColor.Blue, PuzzleColor.Orange, PuzzleColor.Green, PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Green, PuzzleColor.Purple, PuzzleColor.Orange },
                new[] { new BusDefinition(PuzzleColor.Purple, 3), new BusDefinition(PuzzleColor.Orange, 3), new BusDefinition(PuzzleColor.Red, 2), new BusDefinition(PuzzleColor.Blue, 2), new BusDefinition(PuzzleColor.Green, 2) });

            sequence.Configure(new[] { levelOne, levelTwo, levelThree });
            return sequence;
        }

        private static LevelData CreateLevel(string name, IEnumerable<PuzzleColor> passengers, IEnumerable<BusDefinition> buses)
        {
            var level = CreateInstance<LevelData>();
            level.hideFlags = HideFlags.DontSave;
            level.Configure(name, passengers, buses);
            return level;
        }
    }
}
