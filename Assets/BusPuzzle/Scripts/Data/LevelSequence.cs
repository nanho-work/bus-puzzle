using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Level Sequence", fileName = "LevelSequence")]
    public sealed class LevelSequence : ScriptableObject
    {
        private enum RuntimeLevelOrigin
        {
            Unknown,
            Procedural,
            SafeCatalog,
            Emergency
        }

        private const int MaximumRuntimeLevelsInMemory = 8;

        [SerializeField] private List<LevelData> levels = new List<LevelData>();
        [SerializeField] private bool verifiedGeneratedSet;

        private StageGenerationConfig runtimeGenerationConfig;
        private Dictionary<int, LevelData> runtimeGeneratedLevels;
        private Dictionary<int, RuntimeLevelOrigin> runtimeLevelOrigins;
        private HashSet<int> runtimeGenerationTerminalFailures;
        private RuntimeSafeLevelCatalog runtimeSafeLevelCatalog;
        private RuntimeStageGenerationService runtimeGenerationService;
        private int runtimePinnedLevelIndex = -1;

        public int Count => runtimeGenerationConfig != null ? int.MaxValue : levels.Count;
        public bool UsesRuntimeGeneration => runtimeGenerationConfig != null;
        public bool IsVerifiedGeneratedSet => runtimeGenerationConfig == null && verifiedGeneratedSet && levels != null && levels.Count > 0;
        public int RuntimePreloadAheadCount => runtimeGenerationConfig != null ? runtimeGenerationConfig.RuntimePreloadAheadCount : 0;
        public int RuntimeSafeCatalogCount => runtimeSafeLevelCatalog != null ? runtimeSafeLevelCatalog.Count : 0;
        public int RuntimePreparedLevelCount =>
            runtimeGeneratedLevels != null ? runtimeGeneratedLevels.Count : 0;
        public bool IsTransientRuntimeSequence =>
            (hideFlags & HideFlags.DontSave) != 0;
        public IReadOnlyList<LevelData> StaticLevels => levels;

        public LevelData GetLevel(int index)
        {
            if (runtimeGenerationConfig != null)
            {
                if (TryGetStaticLevel(index, out var staticLevel))
                {
                    return staticLevel;
                }

                if (Application.isPlaying)
                {
                    Debug.LogError(
                        $"Stage {index + 1:000} requested the synchronous procedural " +
                        "generator during gameplay. Use PrepareSafeGameplayLevel instead.");
                    return null;
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
            runtimeLevelOrigins = null;
            runtimeGenerationTerminalFailures = null;
            runtimeSafeLevelCatalog = null;
            runtimePinnedLevelIndex = -1;
            runtimeGenerationService?.Dispose();
            runtimeGenerationService = null;
        }

        public void ConfigureVerifiedGeneratedSet(IEnumerable<LevelData> newLevels)
        {
            levels = new List<LevelData>(newLevels);
            verifiedGeneratedSet = true;
            runtimeGenerationConfig = null;
            runtimeGeneratedLevels = null;
            runtimeLevelOrigins = null;
            runtimeGenerationTerminalFailures = null;
            runtimeSafeLevelCatalog = null;
            runtimePinnedLevelIndex = -1;
            runtimeGenerationService?.Dispose();
            runtimeGenerationService = null;
        }

        public void ConfigureRuntimeGeneration(StageGenerationConfig config, IEnumerable<LevelData> seedLevels = null)
        {
            runtimeGenerationConfig = config;
            runtimeGeneratedLevels = new Dictionary<int, LevelData>();
            runtimeLevelOrigins = new Dictionary<int, RuntimeLevelOrigin>();
            runtimeGenerationTerminalFailures = new HashSet<int>();
            runtimePinnedLevelIndex = -1;
            levels = seedLevels != null ? new List<LevelData>(seedLevels) : new List<LevelData>();
            runtimeSafeLevelCatalog = RuntimeSafeLevelCatalog.Create(levels);
            VehicleShapeTemplateCatalog.PrimeRuntimeGenerationTemplates();
            runtimeGenerationService?.Dispose();
            runtimeGenerationService = new RuntimeStageGenerationService();
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

        public bool IsProcedurallyGeneratedLevelCached(int index)
        {
            return runtimeGenerationConfig != null &&
                runtimeGeneratedLevels != null &&
                runtimeLevelOrigins != null &&
                index >= 0 &&
                runtimeGeneratedLevels.TryGetValue(index, out var level) &&
                level != null &&
                runtimeLevelOrigins.TryGetValue(index, out var origin) &&
                origin == RuntimeLevelOrigin.Procedural;
        }

        public bool IsRuntimeLevelGenerationPending(int index)
        {
            return runtimeGenerationConfig != null &&
                runtimeGenerationService != null &&
                index >= 0 &&
                runtimeGenerationService.IsPending(index);
        }

        public bool StartRuntimeLevelGeneration(int index)
        {
            if (runtimeGenerationConfig == null ||
                runtimeGenerationService == null ||
                index < 0 ||
                TryGetStaticLevel(index, out _) ||
                runtimeGenerationTerminalFailures != null &&
                    runtimeGenerationTerminalFailures.Contains(index) ||
                IsProcedurallyGeneratedLevelCached(index))
            {
                return false;
            }

            try
            {
                var request = StageGenerationPlanner.CreateRequest(
                    runtimeGenerationConfig,
                    index + 1);
                var options = RuntimeStageGenerationOptions.Create(
                    runtimeGenerationConfig,
                    request);
                return runtimeGenerationService.Start(index, options, request);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Stage {index + 1:000} background generation could not start: {exception}");
                return false;
            }
        }

        public bool CancelRuntimeLevelGeneration(int index)
        {
            return runtimeGenerationService != null &&
                index >= 0 &&
                runtimeGenerationService.Cancel(index);
        }

        public int CancelRuntimeLevelGenerationsOutsideRange(
            int minimumLevelIndex,
            int maximumLevelIndex)
        {
            return runtimeGenerationService != null
                ? runtimeGenerationService.CancelOutsideRange(
                    minimumLevelIndex,
                    maximumLevelIndex)
                : 0;
        }

        public void PinActiveRuntimeLevel(int index)
        {
            runtimePinnedLevelIndex =
                runtimeGenerationConfig != null && index >= 0
                    ? index
                    : -1;
        }

        /// <summary>
        /// Polls a completed calculation and atomically replaces a future safe
        /// fallback only after main-thread materialization and validation succeed.
        /// </summary>
        public bool TryFinalizeRuntimeLevelGeneration(
            int index,
            int protectedLevelIndex,
            out bool finished,
            out string diagnostic)
        {
            finished = false;
            diagnostic = string.Empty;
            if (runtimeGenerationConfig == null ||
                runtimeGenerationService == null ||
                index < 0 ||
                index == protectedLevelIndex ||
                TryGetStaticLevel(index, out _))
            {
                return false;
            }

            if (!runtimeGenerationService.TryTakeCompleted(index, out var result))
            {
                return false;
            }

            finished = true;
            if (result == null || !result.Succeeded)
            {
                if (result == null ||
                    result.Outcome != RuntimeStageGenerationOutcome.Cancelled)
                {
                    runtimeGenerationTerminalFailures?.Add(index);
                }

                diagnostic = result != null
                    ? result.Diagnostic
                    : $"Stage {index + 1:000} background generation returned no result.";
                return false;
            }

            LevelData level = null;
            try
            {
                level = result.Data.Materialize();
                var report = LevelValidator.Validate(level, false);
                if (report == null || report.HasErrors)
                {
                    diagnostic = report != null
                        ? report.ToConsoleMessage(level.LevelName)
                        : $"Stage {index + 1:000} generated validation returned no report.";
                    runtimeGenerationTerminalFailures?.Add(index);
                    ReleaseRuntimeLevel(level);
                    return false;
                }

                CommitRuntimeLevel(index, level, RuntimeLevelOrigin.Procedural);
                runtimeGenerationTerminalFailures?.Remove(index);
                diagnostic =
                    $"Runtime stage {index + 1:000} accepted background candidate " +
                    $"{result.CandidateIndex + 1} with {result.Analysis.SolutionCount} verified solution(s) " +
                    $"and {result.Data.TotalVehicleCount} vehicles.";
                return true;
            }
            catch (System.Exception exception)
            {
                if (level != null)
                {
                    ReleaseRuntimeLevel(level);
                }

                diagnostic =
                    $"Stage {index + 1:000} generated result could not be committed: {exception}";
                runtimeGenerationTerminalFailures?.Add(index);
                return false;
            }
        }

        public bool TryGetPreparedLevel(int index, out LevelData level)
        {
            if (TryGetStaticLevel(index, out level))
            {
                return true;
            }

            level = null;
            return runtimeGenerationConfig != null &&
                runtimeGeneratedLevels != null &&
                index >= 0 &&
                runtimeGeneratedLevels.TryGetValue(index, out level) &&
                level != null;
        }

        /// <summary>
        /// Resolves a gameplay stage without entering the procedural generator.
        /// Foreground loads and cold-start recovery must use this path so a slow
        /// device can never spend multiple seconds inside one Unity frame.
        /// </summary>
        public bool PrepareSafeGameplayLevel(int index, string reason)
        {
            return PrepareSafeGameplayLevel(index, reason, true);
        }

        public bool PrepareSafeGameplayLevel(
            int index,
            string reason,
            bool logResolution)
        {
            if (index < 0)
            {
                return false;
            }

            if (TryGetPreparedLevel(index, out _))
            {
                return true;
            }

            if (runtimeGenerationConfig == null)
            {
                return false;
            }

            var runtimeLevelIndex = Mathf.Max(0, index);
            var stageNumber = runtimeLevelIndex + 1;
            StageGenerationRequest request;
            try
            {
                request = StageGenerationPlanner.CreateRequest(
                    runtimeGenerationConfig,
                    stageNumber);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Safe request planning failed for stage {stageNumber:000}: {exception}");
                return false;
            }

            LevelData level = null;
            var sourceLevelIndex = -1;

            try
            {
                if (runtimeSafeLevelCatalog != null)
                {
                    runtimeSafeLevelCatalog.TryCreateLevel(
                        request,
                        out level,
                        out sourceLevelIndex);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    $"Safe catalog preparation failed for stage {stageNumber:000}: {exception.Message}");
                level = null;
            }

            if (level == null)
            {
                try
                {
                    level = StageCandidateBuilder.BuildEmergencyRuntimeStage(request);
                }
                catch (System.Exception exception)
                {
                    Debug.LogError(
                        $"Emergency preparation failed for stage {stageNumber:000}: {exception}");
                    return false;
                }
            }

            if (level == null)
            {
                return false;
            }

            CommitRuntimeLevel(
                runtimeLevelIndex,
                level,
                sourceLevelIndex >= 0
                    ? RuntimeLevelOrigin.SafeCatalog
                    : RuntimeLevelOrigin.Emergency);
            if (!logResolution)
            {
                return true;
            }

            if (sourceLevelIndex >= 0)
            {
                Debug.Log(
                    $"Runtime stage {stageNumber:000} used safe catalog stage " +
                    $"{sourceLevelIndex + 1:000} for {reason}.");
            }
            else
            {
                Debug.LogError(
                    $"Runtime stage {stageNumber:000} used its emergency board for {reason}.");
            }

            return true;
        }

        public bool TryCreateEmergencyRuntimeLevel(
            int index,
            string reason,
            out LevelData level)
        {
            level = null;
            if (runtimeGenerationConfig == null ||
                index < 0 ||
                TryGetStaticLevel(index, out _))
            {
                return false;
            }

            var runtimeLevelIndex = Mathf.Max(0, index);
            var stageNumber = runtimeLevelIndex + 1;
            try
            {
                var request = StageGenerationPlanner.CreateRequest(
                    runtimeGenerationConfig,
                    stageNumber);
                level = StageCandidateBuilder.BuildEmergencyRuntimeStage(request);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Emergency replacement failed for stage {stageNumber:000}: {exception}");
                level = null;
                return false;
            }

            if (level == null)
            {
                return false;
            }

            Debug.LogWarning(
                $"Runtime stage {stageNumber:000} prepared an emergency board after {reason}. " +
                "It will replace the cached level only after board activation succeeds.");
            return true;
        }

        public bool CommitPreparedRuntimeLevel(int index, LevelData level)
        {
            if (runtimeGenerationConfig == null ||
                index < 0 ||
                level == null ||
                TryGetStaticLevel(index, out _))
            {
                return false;
            }

            var origin = StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "runtimeProcedural",
                    out var runtimeProcedural) &&
                runtimeProcedural == 1
                    ? RuntimeLevelOrigin.Procedural
                    : StageGenerationSignature.TryGetInt(
                            level.GenerationSignature,
                            "runtimeEmergency",
                            out var runtimeEmergency) &&
                        runtimeEmergency == 1
                        ? RuntimeLevelOrigin.Emergency
                        : RuntimeLevelOrigin.Unknown;
            CommitRuntimeLevel(index, level, origin);
            return true;
        }

        public void ReleaseTransientRuntimeLevel(LevelData level)
        {
            if (level == null)
            {
                return;
            }

            if (runtimeGeneratedLevels != null)
            {
                foreach (var cachedLevel in runtimeGeneratedLevels.Values)
                {
                    if (ReferenceEquals(cachedLevel, level))
                    {
                        return;
                    }
                }
            }

            ReleaseRuntimeLevel(level);
        }

        public void ReleaseRuntimeResources()
        {
            runtimeGenerationService?.Dispose();
            runtimeGenerationService = null;
            runtimePinnedLevelIndex = -1;
            runtimeGenerationTerminalFailures?.Clear();

            if (runtimeGeneratedLevels != null)
            {
                foreach (var level in runtimeGeneratedLevels.Values)
                {
                    ReleaseRuntimeLevel(level);
                }

                runtimeGeneratedLevels.Clear();
            }

            runtimeLevelOrigins?.Clear();

            // Runtime-generated sequences borrow their static seed levels from the
            // release asset, so those must never be destroyed here. The three-level
            // runtime fallback owns its DontSave levels and must release them.
            if (IsTransientRuntimeSequence &&
                runtimeGenerationConfig == null &&
                levels != null)
            {
                for (var index = 0; index < levels.Count; index++)
                {
                    var level = levels[index];
                    if (level != null &&
                        (level.hideFlags & HideFlags.DontSave) != 0)
                    {
                        ReleaseRuntimeLevel(level);
                    }
                }

                levels.Clear();
            }

            runtimeSafeLevelCatalog = null;
        }

        public bool PreloadLevel(int index)
        {
            if (runtimeGenerationConfig == null ||
                index < 0 ||
                IsLevelCached(index))
            {
                return false;
            }

            if (Application.isPlaying)
            {
                Debug.LogError(
                    $"Stage {index + 1:000} attempted synchronous procedural preload " +
                    "during gameplay. Use PrepareSafeGameplayLevel instead.");
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
                var origin = RuntimeLevelOrigin.Procedural;
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
                    origin = RuntimeLevelOrigin.SafeCatalog;
                    var elapsedMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                    Debug.LogWarning(
                        $"Runtime stage {stageNumber:000} exhausted every bounded procedural probe and " +
                        $"cloned safe catalog stage {sourceLevelIndex + 1:000} as its final content fallback " +
                        $"in {elapsedMilliseconds:0.0} ms.");
                }
                else
                {
                    origin = RuntimeLevelOrigin.Emergency;
                    level = StageCandidateBuilder.BuildEmergencyRuntimeStage(request);
                    var elapsedMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                    Debug.LogError(
                        $"Runtime stage {stageNumber:000} had neither a valid procedural candidate nor a " +
                        $"safe catalog match. An emergency board was created in {elapsedMilliseconds:0.0} ms.");
                }

                if (level != null)
                {
                    CommitRuntimeLevel(runtimeLevelIndex, level, origin);
                }
            }

            return level;
        }

        private void CommitRuntimeLevel(
            int runtimeLevelIndex,
            LevelData level,
            RuntimeLevelOrigin origin)
        {
            if (runtimeGeneratedLevels == null)
            {
                runtimeGeneratedLevels = new Dictionary<int, LevelData>();
            }

            if (runtimeLevelOrigins == null)
            {
                runtimeLevelOrigins = new Dictionary<int, RuntimeLevelOrigin>();
            }

            if (runtimeGeneratedLevels.TryGetValue(runtimeLevelIndex, out var previousLevel) &&
                previousLevel != null &&
                !ReferenceEquals(previousLevel, level))
            {
                ReleaseRuntimeLevel(previousLevel);
            }

            runtimeGeneratedLevels[runtimeLevelIndex] = level;
            runtimeLevelOrigins[runtimeLevelIndex] = origin;
            PruneRuntimeMemoryCache(runtimeLevelIndex);
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
                    if (pair.Key == protectedLevelIndex ||
                        pair.Key == runtimePinnedLevelIndex)
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
                runtimeLevelOrigins?.Remove(evictionIndex);
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
