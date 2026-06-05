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
        private const int CacheVersion = 4;
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
                return level != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Generated stage cache load failed for stage {request.StageNumber}: {exception.Message}");
                return false;
            }
        }

        public static void Save(StageGenerationConfig config, StageGenerationRequest request, LevelData level)
        {
            if (config == null || level == null)
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

        private static string CreateSignature(StageGenerationConfig config, StageGenerationRequest request)
        {
            var profile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var passengerRule = profile.PassengerFlowRule;
            var garageRule = config.SuperHardGarageRule;
            var builder = new StringBuilder(256);

            Append(builder, "cache", CacheVersion);
            Append(builder, "stage", request.StageNumber);
            Append(builder, "stageCount", config.GeneratedStageCount);
            Append(builder, "baseSeed", config.BaseSeed);
            Append(builder, "candidateAttempts", config.CandidateAttemptsPerStage);
            Append(builder, "runtimeCandidateAttempts", config.RuntimeCandidateAttemptsPerStage);
            Append(builder, "runtimeVehicleAttempts", config.RuntimeVehicleGenerationAttempts);
            Append(builder, "solutionLimit", config.SolutionCountLimit);
            Append(builder, "seed", request.Seed);
            Append(builder, "difficulty", (int)request.Difficulty);
            Append(builder, "road", (int)request.RoadPresetId);
            Append(builder, "garages", request.GarageCount);
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
            Append(builder, "garageQueueMin", garageRule.MinQueuedVehiclesPerGarage);
            Append(builder, "garageQueueMax", garageRule.MaxQueuedVehiclesPerGarage);

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
                    rotaryUnitCapacity > 0 ? rotaryUnitCapacity : LevelGenerator.GetRotaryCapacity(request.Difficulty),
                    roadPresetId,
                    passengerUnits,
                    garages);
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
                    garages = new List<GarageDefinition>(level.Garages)
                };
            }
        }
    }
}
