using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Level Sequence", fileName = "LevelSequence")]
    public sealed class LevelSequence : ScriptableObject
    {
        [SerializeField] private List<LevelData> levels = new List<LevelData>();
        [SerializeField] private bool verifiedGeneratedSet;

        private StageGenerationConfig runtimeGenerationConfig;
        private LevelData[] runtimeGeneratedLevels;

        public int Count => runtimeGenerationConfig != null ? runtimeGenerationConfig.GeneratedStageCount : levels.Count;
        public bool UsesRuntimeGeneration => runtimeGenerationConfig != null;
        public bool IsVerifiedGeneratedSet => runtimeGenerationConfig == null && verifiedGeneratedSet && levels != null && levels.Count > 0;
        public int RuntimePreloadAheadCount => runtimeGenerationConfig != null ? runtimeGenerationConfig.RuntimePreloadAheadCount : 0;
        public IReadOnlyList<LevelData> StaticLevels => levels;

        public LevelData GetLevel(int index)
        {
            if (runtimeGenerationConfig != null)
            {
                if (TryGetStaticLevel(index, out var staticLevel))
                {
                    return staticLevel;
                }

                return GetRuntimeGeneratedLevel(index);
            }

            if (levels.Count == 0)
            {
                return null;
            }

            return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
        }

        public void Configure(IEnumerable<LevelData> newLevels)
        {
            levels = new List<LevelData>(newLevels);
            verifiedGeneratedSet = false;
            runtimeGenerationConfig = null;
            runtimeGeneratedLevels = null;
        }

        public void ConfigureVerifiedGeneratedSet(IEnumerable<LevelData> newLevels)
        {
            levels = new List<LevelData>(newLevels);
            verifiedGeneratedSet = true;
            runtimeGenerationConfig = null;
            runtimeGeneratedLevels = null;
        }

        public void ConfigureRuntimeGeneration(StageGenerationConfig config, IEnumerable<LevelData> seedLevels = null)
        {
            runtimeGenerationConfig = config;
            runtimeGeneratedLevels = new LevelData[config.GeneratedStageCount];
            levels = seedLevels != null ? new List<LevelData>(seedLevels) : new List<LevelData>();
            verifiedGeneratedSet = false;
        }

        public bool IsLevelCached(int index)
        {
            if (TryGetStaticLevel(index, out _))
            {
                return true;
            }

            return runtimeGenerationConfig == null ||
                runtimeGeneratedLevels != null &&
                index >= 0 &&
                index < runtimeGeneratedLevels.Length &&
                runtimeGeneratedLevels[index] != null;
        }

        public bool PreloadLevel(int index)
        {
            if (runtimeGenerationConfig == null || index < 0 || index >= Count || IsLevelCached(index))
            {
                return false;
            }

            GetRuntimeGeneratedLevel(index);
            return true;
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

        public static LevelSequence CreateRuntimeGenerated(StageGenerationConfig config, IEnumerable<LevelData> seedLevels = null)
        {
            var sequence = CreateInstance<LevelSequence>();
            sequence.hideFlags = HideFlags.DontSave;
            sequence.ConfigureRuntimeGeneration(config, seedLevels);
            return sequence;
        }

        private bool TryGetStaticLevel(int index, out LevelData level)
        {
            level = null;
            if (levels == null || index < 0 || index >= levels.Count)
            {
                return false;
            }

            level = levels[index];
            return level != null;
        }

        private LevelData GetRuntimeGeneratedLevel(int index)
        {
            if (runtimeGeneratedLevels == null || runtimeGeneratedLevels.Length != runtimeGenerationConfig.GeneratedStageCount)
            {
                runtimeGeneratedLevels = new LevelData[runtimeGenerationConfig.GeneratedStageCount];
            }

            var clampedIndex = Mathf.Clamp(index, 0, runtimeGeneratedLevels.Length - 1);
            if (runtimeGeneratedLevels[clampedIndex] == null)
            {
                var stageNumber = clampedIndex + 1;
                var request = StageGenerationPlanner.CreateRequest(runtimeGenerationConfig, stageNumber);
                if (!RuntimeGeneratedLevelCache.TryLoad(runtimeGenerationConfig, request, out var level))
                {
                    level = StageCandidateBuilder.BuildRuntimeStageCandidate(runtimeGenerationConfig, request);
                    RuntimeGeneratedLevelCache.Save(runtimeGenerationConfig, request, level);
                }

                runtimeGeneratedLevels[clampedIndex] = level;
            }

            return runtimeGeneratedLevels[clampedIndex];
        }
    }
}
