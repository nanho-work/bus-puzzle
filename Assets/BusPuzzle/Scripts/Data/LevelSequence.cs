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

            var levelOne = LevelGenerator.CreateRuntimeLevel("Downtown Warmup", LevelDifficulty.Normal, 1001, LevelGenerator.GetRoadPreset(LevelDifficulty.Normal));
            var levelTwo = LevelGenerator.CreateRuntimeLevel("Crosswalk Mix", LevelDifficulty.Hard, 2001, LevelGenerator.GetRoadPreset(LevelDifficulty.Hard));
            var levelThree = LevelGenerator.CreateRuntimeLevel("Terminal Shuffle", LevelDifficulty.SuperHard, 3001, LevelGenerator.GetRoadPreset(LevelDifficulty.SuperHard));
            sequence.Configure(new[] { levelOne, levelTwo, levelThree });
            return sequence;
        }
    }
}
