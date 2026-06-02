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
                RotaryRoadPresetId.Large,
                50,
                new[]
                {
                    PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Yellow, PuzzleColor.Green,
                    PuzzleColor.Purple, PuzzleColor.Orange, PuzzleColor.Red, PuzzleColor.Blue,
                    PuzzleColor.Yellow, PuzzleColor.Green, PuzzleColor.Purple, PuzzleColor.Orange,
                    PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Yellow, PuzzleColor.Green,
                    PuzzleColor.Purple, PuzzleColor.Orange, PuzzleColor.Red, PuzzleColor.Blue,
                    PuzzleColor.Yellow, PuzzleColor.Green, PuzzleColor.Purple, PuzzleColor.Orange,
                    PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Yellow, PuzzleColor.Green,
                    PuzzleColor.Purple, PuzzleColor.Orange, PuzzleColor.Red, PuzzleColor.Blue,
                    PuzzleColor.Yellow, PuzzleColor.Green, PuzzleColor.Purple, PuzzleColor.Orange,
                    PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Yellow, PuzzleColor.Green,
                    PuzzleColor.Purple, PuzzleColor.Orange, PuzzleColor.Red, PuzzleColor.Blue,
                    PuzzleColor.Yellow, PuzzleColor.Green, PuzzleColor.Purple, PuzzleColor.Orange,
                    PuzzleColor.Red, PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Blue,
                    PuzzleColor.Red, PuzzleColor.Red, PuzzleColor.Blue, PuzzleColor.Blue
                },
                new[]
                {
                    new BusDefinition(PuzzleColor.Red, BusSize.Small, GridDirection.Up, new Vector2Int(5, 10)),
                    new BusDefinition(PuzzleColor.Blue, BusSize.Small, GridDirection.Up, new Vector2Int(9, 10)),
                    new BusDefinition(PuzzleColor.Yellow, BusSize.Small, GridDirection.Left, new Vector2Int(2, 8)),
                    new BusDefinition(PuzzleColor.Green, BusSize.Small, GridDirection.Right, new Vector2Int(11, 8)),
                    new BusDefinition(PuzzleColor.Purple, BusSize.Small, GridDirection.Down, new Vector2Int(5, 2)),
                    new BusDefinition(PuzzleColor.Orange, BusSize.Small, GridDirection.Down, new Vector2Int(9, 2)),
                    new BusDefinition(PuzzleColor.Red, BusSize.Small, GridDirection.Right, new Vector2Int(6, 4)),
                    new BusDefinition(PuzzleColor.Blue, BusSize.Small, GridDirection.Left, new Vector2Int(7, 6)),
                    new BusDefinition(PuzzleColor.Yellow, BusSize.Small, GridDirection.Up, new Vector2Int(12, 5)),
                    new BusDefinition(PuzzleColor.Green, BusSize.Small, GridDirection.Down, new Vector2Int(1, 6)),
                    new BusDefinition(PuzzleColor.Purple, BusSize.Small, GridDirection.Right, new Vector2Int(3, 3)),
                    new BusDefinition(PuzzleColor.Red, BusSize.Small, GridDirection.Up, new Vector2Int(3, 0)),
                    new BusDefinition(PuzzleColor.Blue, BusSize.Small, GridDirection.Up, new Vector2Int(11, 0)),
                    new BusDefinition(PuzzleColor.Orange, BusSize.Small, GridDirection.Left, new Vector2Int(10, 7))
                });

            var levelTwo = CreateLevel(
                "Crosswalk Mix",
                RotaryRoadPresetId.Small,
                24,
                new[]
                {
                    PuzzleColor.Red, PuzzleColor.Green, PuzzleColor.Blue, PuzzleColor.Yellow,
                    PuzzleColor.Red, PuzzleColor.Green, PuzzleColor.Red, PuzzleColor.Blue,
                    PuzzleColor.Yellow, PuzzleColor.Red, PuzzleColor.Green, PuzzleColor.Red,
                    PuzzleColor.Blue, PuzzleColor.Yellow, PuzzleColor.Green, PuzzleColor.Red,
                    PuzzleColor.Blue, PuzzleColor.Yellow
                },
                new[]
                {
                    new BusDefinition(PuzzleColor.Red, BusSize.Medium, GridDirection.Up, new Vector2Int(6, 0)),
                    new BusDefinition(PuzzleColor.Green, BusSize.Small, GridDirection.Right, new Vector2Int(5, 5)),
                    new BusDefinition(PuzzleColor.Blue, BusSize.Small, GridDirection.Up, new Vector2Int(3, 1)),
                    new BusDefinition(PuzzleColor.Yellow, BusSize.Small, GridDirection.Right, new Vector2Int(2, 8))
                });

            var levelThree = CreateLevel(
                "Terminal Shuffle",
                RotaryRoadPresetId.Medium,
                32,
                new[]
                {
                    PuzzleColor.Purple, PuzzleColor.Red, PuzzleColor.Orange, PuzzleColor.Blue,
                    PuzzleColor.Green, PuzzleColor.Purple, PuzzleColor.Orange, PuzzleColor.Red,
                    PuzzleColor.Purple, PuzzleColor.Blue, PuzzleColor.Orange, PuzzleColor.Green,
                    PuzzleColor.Purple, PuzzleColor.Orange, PuzzleColor.Purple, PuzzleColor.Red,
                    PuzzleColor.Orange, PuzzleColor.Blue, PuzzleColor.Purple, PuzzleColor.Green,
                    PuzzleColor.Orange, PuzzleColor.Purple, PuzzleColor.Red, PuzzleColor.Orange,
                    PuzzleColor.Blue, PuzzleColor.Green, PuzzleColor.Purple, PuzzleColor.Purple,
                    PuzzleColor.Purple
                },
                new[]
                {
                    new BusDefinition(PuzzleColor.Purple, BusSize.Large, GridDirection.Up, new Vector2Int(6, 0)),
                    new BusDefinition(PuzzleColor.Orange, BusSize.Medium, GridDirection.Right, new Vector2Int(1, 8)),
                    new BusDefinition(PuzzleColor.Red, BusSize.Small, GridDirection.Left, new Vector2Int(12, 5)),
                    new BusDefinition(PuzzleColor.Blue, BusSize.Small, GridDirection.Up, new Vector2Int(9, 1)),
                    new BusDefinition(PuzzleColor.Green, BusSize.Small, GridDirection.Down, new Vector2Int(4, 12))
                });

            sequence.Configure(new[] { levelOne, levelTwo, levelThree });
            return sequence;
        }

        private static LevelData CreateLevel(
            string name,
            RotaryRoadPresetId roadPresetId,
            int rotaryUnitCapacity,
            IEnumerable<PuzzleColor> passengers,
            IEnumerable<BusDefinition> buses)
        {
            var level = CreateInstance<LevelData>();
            level.hideFlags = HideFlags.DontSave;
            level.Configure(name, passengers, buses, rotaryUnitCapacity, roadPresetId);
            return level;
        }
    }
}
