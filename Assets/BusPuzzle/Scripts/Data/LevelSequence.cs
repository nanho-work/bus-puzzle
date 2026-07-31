using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Level Sequence", fileName = "LevelSequence")]
    public sealed class LevelSequence : ScriptableObject
    {
        private const int MaximumRuntimeLevelsInMemory = 8;

        [SerializeField] private List<LevelData> levels = new List<LevelData>();
        [SerializeField] private bool verifiedGeneratedSet;

        private StageGenerationConfig runtimeGenerationConfig;
        private Dictionary<int, LevelData> runtimeGeneratedLevels;
        private RuntimeSafeLevelCatalog runtimeSafeLevelCatalog;

        public int Count => runtimeGenerationConfig != null ? int.MaxValue : levels.Count;
        public bool UsesRuntimeGeneration => runtimeGenerationConfig != null;
        public bool IsVerifiedGeneratedSet => runtimeGenerationConfig == null && verifiedGeneratedSet && levels != null && levels.Count > 0;
        public int RuntimePreloadAheadCount => runtimeGenerationConfig != null ? runtimeGenerationConfig.RuntimePreloadAheadCount : 0;
        public int RuntimeSafeCatalogCount => runtimeSafeLevelCatalog != null ? runtimeSafeLevelCatalog.Count : 0;
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
            runtimeSafeLevelCatalog = null;
        }

        public void ConfigureVerifiedGeneratedSet(IEnumerable<LevelData> newLevels)
        {
            levels = new List<LevelData>(newLevels);
            verifiedGeneratedSet = true;
            runtimeGenerationConfig = null;
            runtimeGeneratedLevels = null;
            runtimeSafeLevelCatalog = null;
        }

        public void ConfigureRuntimeGeneration(StageGenerationConfig config, IEnumerable<LevelData> seedLevels = null)
        {
            runtimeGenerationConfig = config;
            runtimeGeneratedLevels = new Dictionary<int, LevelData>();
            levels = seedLevels != null ? new List<LevelData>(seedLevels) : new List<LevelData>();
            runtimeSafeLevelCatalog = RuntimeSafeLevelCatalog.Create(levels);
            verifiedGeneratedSet = false;

            if (levels.Count > 0)
            {
                Debug.Log(
                    $"Runtime safe level catalog accepted {runtimeSafeLevelCatalog.Count}/{levels.Count} prebuilt stages.");
            }
        }

        public bool IsLevelCached(int index)
        {
            if (TryGetStaticLevel(index, out _))
            {
                return true;
            }

            return runtimeGenerationConfig != null &&
                runtimeGeneratedLevels != null &&
                index >= 0 &&
                runtimeGeneratedLevels.TryGetValue(index, out var cachedLevel) &&
                cachedLevel != null;
        }

        public bool PreloadLevel(int index)
        {
            if (runtimeGenerationConfig == null || index < 0 || IsLevelCached(index))
            {
                return false;
            }

            return GetRuntimeGeneratedLevel(index) != null;
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
            if (runtimeGeneratedLevels == null)
            {
                runtimeGeneratedLevels = new Dictionary<int, LevelData>();
            }

            var runtimeLevelIndex = Mathf.Max(0, index);
            if (!runtimeGeneratedLevels.TryGetValue(runtimeLevelIndex, out var level) || level == null)
            {
                var stageNumber = runtimeLevelIndex + 1;
                var request = StageGenerationPlanner.CreateRequest(runtimeGenerationConfig, stageNumber);
                var startedAt = Time.realtimeSinceStartup;
                if (RuntimeGeneratedLevelCache.TryLoad(runtimeGenerationConfig, request, out level))
                {
                    var elapsedMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                    Debug.Log(
                        $"Runtime stage {stageNumber:000} loaded its validated procedural cache " +
                        $"in {elapsedMilliseconds:0.0} ms.");
                }
                else if (StageCandidateBuilder.TryBuildRuntimeStageCandidate(
                    runtimeGenerationConfig,
                    request,
                    out level,
                    out _,
                    out var analysis,
                    out var candidateIndex))
                {
                    if (StageCandidateBuilder.ShouldCacheRuntimeStage(request, level))
                    {
                        RuntimeGeneratedLevelCache.Save(runtimeGenerationConfig, request, level);
                    }

                    var elapsedMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                    Debug.Log(
                        $"Runtime stage {stageNumber:000} procedurally generated candidate " +
                        $"{candidateIndex + 1} with {analysis.SolutionCount} verified solution(s) " +
                        $"in {elapsedMilliseconds:0.0} ms.");
                }
                else if (runtimeSafeLevelCatalog != null &&
                    runtimeSafeLevelCatalog.TryCreateLevel(request, out level, out var sourceLevelIndex))
                {
                    var elapsedMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                    Debug.LogWarning(
                        $"Runtime stage {stageNumber:000} exhausted every bounded procedural probe and " +
                        $"cloned safe catalog stage {sourceLevelIndex + 1:000} as its final content fallback " +
                        $"in {elapsedMilliseconds:0.0} ms.");
                }
                else
                {
                    level = StageCandidateBuilder.BuildEmergencyRuntimeStage(request);
                    var elapsedMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                    Debug.LogError(
                        $"Runtime stage {stageNumber:000} had neither a valid procedural candidate nor a " +
                        $"safe catalog match. An emergency board was created in {elapsedMilliseconds:0.0} ms.");
                }

                if (level != null)
                {
                    runtimeGeneratedLevels[runtimeLevelIndex] = level;
                    PruneRuntimeMemoryCache(runtimeLevelIndex);
                }
            }

            return level;
        }

        private void PruneRuntimeMemoryCache(int protectedLevelIndex)
        {
            if (runtimeGeneratedLevels == null ||
                runtimeGeneratedLevels.Count <= MaximumRuntimeLevelsInMemory)
            {
                return;
            }

            while (runtimeGeneratedLevels.Count > MaximumRuntimeLevelsInMemory)
            {
                var evictionIndex = -1;
                var greatestDistance = long.MinValue;
                foreach (var pair in runtimeGeneratedLevels)
                {
                    if (pair.Key == protectedLevelIndex)
                    {
                        continue;
                    }

                    var distance = System.Math.Abs((long)pair.Key - protectedLevelIndex);
                    if (distance > greatestDistance ||
                        (distance == greatestDistance && pair.Key < evictionIndex))
                    {
                        greatestDistance = distance;
                        evictionIndex = pair.Key;
                    }
                }

                if (evictionIndex < 0 ||
                    !runtimeGeneratedLevels.TryGetValue(evictionIndex, out var evictedLevel))
                {
                    return;
                }

                runtimeGeneratedLevels.Remove(evictionIndex);
                ReleaseRuntimeLevel(evictedLevel);
            }
        }

        private static void ReleaseRuntimeLevel(LevelData level)
        {
            if (level == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(level);
                return;
            }
#endif
            Destroy(level);
        }
    }
}
