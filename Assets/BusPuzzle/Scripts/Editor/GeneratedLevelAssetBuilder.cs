#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BusPuzzle.EditorTools
{
    public static class GeneratedLevelAssetBuilder
    {
        private const string LevelDirectory = "Assets/BusPuzzle/Resources/Levels";
        private const string LevelSequencePath = LevelDirectory + "/LevelSequence.asset";

        [MenuItem("Bus Puzzle/Levels/Rebuild Generated Difficulty Levels")]
        public static void RebuildGeneratedDifficultyLevels()
        {
            Directory.CreateDirectory(LevelDirectory);

            var levelOne = SaveLevel(
                "Level01",
                LevelGenerator.CreateRuntimeLevel("Downtown Warmup", LevelDifficulty.Normal, 1001, LevelGenerator.GetRoadPreset(LevelDifficulty.Normal)));
            var levelTwo = SaveLevel(
                "Level02",
                LevelGenerator.CreateRuntimeLevel("Crosswalk Mix", LevelDifficulty.Hard, 2001, LevelGenerator.GetRoadPreset(LevelDifficulty.Hard)));
            var levelThree = SaveLevel(
                "Level03",
                LevelGenerator.CreateRuntimeLevel("Terminal Shuffle", LevelDifficulty.SuperHard, 3001, LevelGenerator.GetRoadPreset(LevelDifficulty.SuperHard)));

            SaveSequence(new[] { levelOne, levelTwo, levelThree });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Bus Puzzle difficulty levels rebuilt.");
        }

        private static LevelData SaveLevel(string assetName, LevelData generatedLevel)
        {
            var path = $"{LevelDirectory}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generatedLevel, path);
                return generatedLevel;
            }

            EditorUtility.CopySerialized(generatedLevel, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void SaveSequence(LevelData[] levels)
        {
            var sequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(LevelSequencePath);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<LevelSequence>();
                AssetDatabase.CreateAsset(sequence, LevelSequencePath);
            }

            sequence.Configure(levels);
            EditorUtility.SetDirty(sequence);
        }
    }
}
#endif
