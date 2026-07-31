using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace BusPuzzle
{
    public static class RuntimeGeneratedLevelCache
    {
        private const int CacheVersion = 62;
        private const int RetainedStageWindow = 16;
        private const string CacheDirectoryName = "generated-stage-cache";

        public static bool TryLoad(StageGenerationConfig config, StageGenerationRequest request, out LevelData level)
        {
            level = null;
            if (config == null)
            {
                return false;
            }

            var signature = CreateSignature(config, request);
            var path = GetCachePath(signature, request.StageNumber);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                var payload = JsonUtility.FromJson<CachedLevelPayload>(File.ReadAllText(path));
                if (payload == null || !payload.Matches(signature))
                {
                    return false;
                }

                level = payload.ToLevel(request);
                if (!IsCachedLevelUsable(request, level))
                {
                    ReleaseCachedLevel(level);
                    level = null;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Generated stage cache load failed for stage {request.StageNumber}: {exception.Message}");
                return false;
            }
        }

        public static void Save(StageGenerationConfig config, StageGenerationRequest request, LevelData level)
        {
            if (config == null ||
                level == null ||
                !StageCandidateBuilder.ShouldCacheRuntimeStage(request, level) ||
                !IsCachedLevelUsable(request, level))
            {
                return;
            }

            var signature = CreateSignature(config, request);
            var path = GetCachePath(signature, request.StageNumber);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var payload = CachedLevelPayload.FromLevel(signature, level);
                File.WriteAllText(path, JsonUtility.ToJson(payload));
                PruneOldCacheFiles(path, request.StageNumber);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Generated stage cache save failed for stage {request.StageNumber}: {exception.Message}");
            }
        }

        private static string GetCachePath(string signature, int stageNumber)
        {
            return Path.Combine(
                Application.persistentDataPath,
                CacheDirectoryName,
                $"stage_{stageNumber:000}_{CreateStableHash(signature):x16}.json");
        }

        private static void PruneOldCacheFiles(string retainedPath, int currentStageNumber)
        {
            var directory = Path.GetDirectoryName(retainedPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var minimumRetainedStage = Mathf.Max(1, currentStageNumber - RetainedStageWindow + 1);
            var paths = Directory.GetFiles(directory, "stage_*.json", SearchOption.TopDirectoryOnly);
            for (var index = 0; index < paths.Length; index++)
            {
                var candidatePath = paths[index];
                if (string.Equals(candidatePath, retainedPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!TryParseStageNumber(candidatePath, out var cachedStageNumber))
                {
                    continue;
                }

                if (cachedStageNumber < minimumRetainedStage ||
                    cachedStageNumber == currentStageNumber)
                {
                    File.Delete(candidatePath);
                }
            }
        }

        private static bool TryParseStageNumber(string path, out int stageNumber)
        {
            stageNumber = 0;
            var fileName = Path.GetFileNameWithoutExtension(path);
            const string prefix = "stage_";
            if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var separatorIndex = fileName.IndexOf('_', prefix.Length);
            return separatorIndex > prefix.Length &&
                int.TryParse(
                    fileName.Substring(prefix.Length, separatorIndex - prefix.Length),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out stageNumber);
        }

        private static bool IsCachedLevelUsable(StageGenerationRequest request, LevelData level)
        {
            if (level == null)
            {
                return false;
            }

            if (!StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "runtimeProcedural",
                    out var runtimeProcedural) ||
                runtimeProcedural != 1 ||
                !StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "stage",
                    out var stageNumber) ||
                stageNumber != request.StageNumber ||
                !StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "seed",
                    out var seed) ||
                seed != request.Seed)
            {
                return false;
            }

            if (LevelValidator.Validate(level, false).HasErrors)
            {
                return false;
            }

            return StageCandidateBuilder.AnalyzeRuntimeSolution(level).IsSolvable;
        }

        private static void ReleaseCachedLevel(LevelData level)
        {
            if (level == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(level);
                return;
            }
#endif
            UnityEngine.Object.Destroy(level);
        }

        private static string CreateSignature(StageGenerationConfig config, StageGenerationRequest request)
        {
            var profile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var passengerRule = profile.PassengerFlowRule;
            var garageRule = config.SuperHardGarageRule;
            var builder = new StringBuilder(256);

            Append(builder, "cache", CacheVersion);
            Append(builder, "stage", request.StageNumber);
            Append(builder, "stageCount", config.GeneratedStageCount);
            Append(builder, "rampStart", config.DifficultyRampStartStage);
            Append(builder, "rampReference", config.DifficultyRampReferenceStage);
            Append(builder, "rampMax", config.DifficultyRampMaxStage);
            Append(builder, "postRampStart", config.Post50RampStartStage);
            Append(builder, "postRampMax", config.Post50RampMaxStage);
            Append(builder, "endlessVersion", StageGenerationConfig.EndlessScheduleVersion);
            Append(builder, "baseSeed", config.BaseSeed);
            Append(builder, "candidateAttempts", config.CandidateAttemptsPerStage);
            Append(builder, "runtimeCandidateAttempts", config.RuntimeCandidateAttemptsPerStage);
            Append(builder, "runtimeVehicleAttempts", config.RuntimeVehicleGenerationAttempts);
            Append(builder, "solutionLimit", config.SolutionCountLimit);
            Append(builder, "seed", request.Seed);
            Append(builder, "difficulty", (int)request.Difficulty);
            Append(builder, "modifiers", (int)request.Modifiers);
            Append(builder, "progress", request.Progress);
            Append(builder, "post50", request.Post50Pressure);
            Append(builder, "road", (int)request.RoadPresetId);
            Append(builder, "layoutVariant", request.VehicleLayoutVariantIndex);
            Append(builder, "layoutPool", request.VehicleLayoutVariantPoolSize);
            Append(builder, "garages", request.GarageCount);
            Append(builder, "garageQueueMin", request.MinGarageQueuedVehicles);
            Append(builder, "garageQueueMax", request.MaxGarageQueuedVehicles);
            Append(builder, "rotary", request.RotaryCapacity);
            Append(builder, "mysteryEnabled", request.MysteryVehicleProfile.Enabled ? 1 : 0);
            Append(builder, "mysteryMin", request.MysteryVehicleProfile.MinVehicles);
            Append(builder, "mysteryMax", request.MysteryVehicleProfile.MaxVehicles);
            Append(builder, "mysteryRatio", request.MysteryVehicleProfile.Ratio);
            Append(builder, "minSolutions", request.MinSolutionCount);
            Append(builder, "maxSolutions", request.MaxSolutionCount);
            Append(builder, "vehicles", profile.TargetVehicleCount);
            Append(builder, "colors", profile.TargetColorCount);
            Append(builder, "parking", profile.ParkingTension);
            Append(builder, "station", profile.StationPressure);
            Append(builder, "route", profile.RequireSolutionRoute ? 1 : 0);
            Append(builder, "flowMin", passengerRule.MinMainGroupRatio);
            Append(builder, "flowMax", passengerRule.MaxMainGroupRatio);
            Append(builder, "groupMin", passengerRule.MinGroupUnits);
            Append(builder, "groupMax", passengerRule.MaxGroupUnits);
            Append(builder, "interference", passengerRule.InterferenceRatio);
            Append(builder, "preserve", passengerRule.PreserveSolutionRoute ? 1 : 0);
            Append(builder, "garageEnabled", garageRule.Enabled ? 1 : 0);
            Append(builder, "garageRuleQueueMin", garageRule.MinQueuedVehiclesPerGarage);
            Append(builder, "garageRuleQueueMax", garageRule.MaxQueuedVehiclesPerGarage);
            Append(builder, "garageRulePostQueueMin", garageRule.Post50MinQueuedVehiclesPerGarage);
            Append(builder, "garageRulePostQueueMax", garageRule.Post50MaxQueuedVehiclesPerGarage);
            Append(builder, "modifierSystem", 1);

            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string key, int value)
        {
            builder.Append(key).Append('=').Append(value).Append(';');
        }

        private static void Append(StringBuilder builder, string key, float value)
        {
            builder.Append(key)
                .Append('=')
                .Append(value.ToString("0.####", CultureInfo.InvariantCulture))
                .Append(';');
        }

        private static ulong CreateStableHash(string value)
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            unchecked
            {
                var hash = offsetBasis;
                for (var index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= prime;
                }

                return hash;
            }
        }

        [Serializable]
        private sealed class CachedLevelPayload
        {
            [SerializeField] private int cacheVersion;
            [SerializeField] private string signature;
            [SerializeField] private string levelName;
            [SerializeField] private LevelDifficultyProfile difficultyProfile;
            [SerializeField] private RotaryRoadPresetId roadPresetId;
            [SerializeField] private int rotaryUnitCapacity;
            [SerializeField] private List<PuzzleColor> passengerUnits;
            [SerializeField] private PassengerFlowPlan passengerFlowPlan;
            [SerializeField] private List<BusDefinition> buses;
            [SerializeField] private List<GarageDefinition> garages;
            [SerializeField] private string generationSignature;
            [SerializeField] private int generationSolutionCount;

            public bool Matches(string expectedSignature)
            {
                return cacheVersion == CacheVersion && signature == expectedSignature;
            }

            public LevelData ToLevel(StageGenerationRequest request)
            {
                var level = ScriptableObject.CreateInstance<LevelData>();
                level.hideFlags = HideFlags.DontSave;
                level.ConfigureWithPassengerFlowPlan(
                    string.IsNullOrEmpty(levelName) ? $"Stage {request.StageNumber:000} {request.Difficulty}" : levelName,
                    difficultyProfile ?? request.Profile,
                    passengerFlowPlan ?? LevelGenerator.BuildPassengerFlowPlan(request.Profile, buses, garages, request.Seed),
                    buses ?? new List<BusDefinition>(),
                    rotaryUnitCapacity > 0 ? rotaryUnitCapacity : request.RotaryCapacity,
                    roadPresetId,
                    passengerUnits,
                    garages);
                level.SetGenerationMetadata(generationSignature, generationSolutionCount);
                return level;
            }

            public static CachedLevelPayload FromLevel(string newSignature, LevelData level)
            {
                return new CachedLevelPayload
                {
                    cacheVersion = CacheVersion,
                    signature = newSignature,
                    levelName = level.LevelName,
                    difficultyProfile = level.DifficultyProfile,
                    roadPresetId = level.RoadPresetId,
                    rotaryUnitCapacity = level.RotaryUnitCapacity,
                    passengerUnits = new List<PuzzleColor>(level.PassengerUnits),
                    passengerFlowPlan = level.PassengerFlowPlan,
                    buses = new List<BusDefinition>(level.Buses),
                    garages = new List<GarageDefinition>(level.Garages),
                    generationSignature = level.GenerationSignature,
                    generationSolutionCount = level.GenerationSolutionCount
                };
            }
        }
    }
}
