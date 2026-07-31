using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    /// <summary>
    /// Provides a bounded-cost runtime fallback by cloning already verified, prebuilt levels.
    /// No solution search or procedural generation is performed while selecting a level.
    /// </summary>
    internal sealed class RuntimeSafeLevelCatalog
    {
        private const int DifficultyBucketCount = 3;
        private const int GarageBucketCount = 2;
        private const int MysteryBucketCount = 2;
        private const int BucketCount = DifficultyBucketCount * GarageBucketCount * MysteryBucketCount;
        private const int MaximumCompatibleProfileVehicleDelta = 3;
        private const int MaximumCompatibleRotaryCapacityDelta = 8;
        private const float MinimumActualVehicleTargetRatio = 0.75f;

        private readonly List<CatalogEntry>[] entriesByRequirement;

        private RuntimeSafeLevelCatalog(List<CatalogEntry>[] entries, int count)
        {
            entriesByRequirement = entries;
            Count = count;
        }

        public int Count { get; }

        public static RuntimeSafeLevelCatalog Create(IReadOnlyList<LevelData> sourceLevels)
        {
            var buckets = new List<CatalogEntry>[BucketCount];
            for (var index = 0; index < buckets.Length; index++)
            {
                buckets[index] = new List<CatalogEntry>();
            }

            var acceptedCount = 0;
            if (sourceLevels != null)
            {
                for (var sourceIndex = 0; sourceIndex < sourceLevels.Count; sourceIndex++)
                {
                    var source = sourceLevels[sourceIndex];
                    if (!IsSafeCatalogSource(source))
                    {
                        continue;
                    }

                    var hasGarages = source.Garages != null && source.Garages.Count > 0;
                    var hasMysteryVehicles = HasMysteryVehicles(source);
                    var bucketIndex = GetBucketIndex(
                        source.DifficultyProfile.Difficulty,
                        hasGarages,
                        hasMysteryVehicles);
                    buckets[bucketIndex].Add(new CatalogEntry(source, sourceIndex));
                    acceptedCount++;
                }
            }

            return new RuntimeSafeLevelCatalog(buckets, acceptedCount);
        }

        public bool TryCreateLevel(
            StageGenerationRequest request,
            out LevelData level,
            out int sourceLevelIndex)
        {
            level = null;
            sourceLevelIndex = -1;

            var wantsGarages = request.GarageCount > 0 ||
                (request.Modifiers & StageModifierFlags.Garages) != 0;
            var wantsMysteryVehicles = request.MysteryVehicleProfile.Enabled ||
                (request.Modifiers & (StageModifierFlags.MysteryVehicles | StageModifierFlags.LightMysteryVehicles)) != 0;
            var bucketIndex = GetBucketIndex(request.Difficulty, wantsGarages, wantsMysteryVehicles);
            var candidates = entriesByRequirement[bucketIndex];
            if (candidates == null || candidates.Count == 0)
            {
                // A verified pack may end before a post-pack modifier starts (for example,
                // Normal + LightMystery immediately after the last prebuilt stage). The
                // output clone is normalized to the request below, so only the source's
                // mystery flag may be relaxed; difficulty and garage requirements stay exact.
                bucketIndex = GetBucketIndex(request.Difficulty, wantsGarages, !wantsMysteryVehicles);
                candidates = entriesByRequirement[bucketIndex];
            }

            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            var plannerVehicleCount = GetRequestedVehicleCount(request);
            var catalogVehicleCap = GetCatalogVehicleCap(request, candidates);
            var effectiveRequest = CapVehicleRequest(request, catalogVehicleCap);
            var selected = SelectClosestCandidate(effectiveRequest, candidates);
            level = CloneLevel(
                selected.Level,
                effectiveRequest,
                selected.SourceLevelIndex,
                plannerVehicleCount,
                catalogVehicleCap);
            sourceLevelIndex = selected.SourceLevelIndex;
            return level != null;
        }

        private static int GetCatalogVehicleCap(
            StageGenerationRequest request,
            IReadOnlyList<CatalogEntry> candidates)
        {
            var catalogVehicleCap = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                var source = candidates[index].Level;
                if (!MeetsStructuralRuntimeContract(request, source))
                {
                    continue;
                }

                var sourceProfile = source.DifficultyProfile ??
                    LevelDifficultyProfile.DefaultFor(request.Difficulty);
                var actualVehicleCount = source.AllVehicles != null ? source.AllVehicles.Count : 0;
                var profileVehicleCap = sourceProfile.TargetVehicleCount +
                    MaximumCompatibleProfileVehicleDelta;
                var actualVehicleCap = Mathf.FloorToInt(
                    actualVehicleCount / MinimumActualVehicleTargetRatio);
                catalogVehicleCap = Mathf.Max(
                    catalogVehicleCap,
                    Mathf.Min(profileVehicleCap, actualVehicleCap));
            }

            // A zero cap means the locked pack cannot satisfy the structural request at all.
            // Keep the planner request intact in that case so validation fails loudly instead
            // of disguising an incompatible release pack as a successful capped request.
            return catalogVehicleCap > 0
                ? catalogVehicleCap
                : GetRequestedVehicleCount(request);
        }

        private static bool MeetsStructuralRuntimeContract(
            StageGenerationRequest request,
            LevelData source)
        {
            if (source == null ||
                Mathf.Abs(source.RotaryUnitCapacity - request.RotaryCapacity) >
                MaximumCompatibleRotaryCapacityDelta)
            {
                return false;
            }

            var sourceGarages = source.Garages;
            var sourceGarageCount = sourceGarages != null ? sourceGarages.Count : 0;
            if (sourceGarageCount != request.GarageCount)
            {
                return false;
            }

            for (var garageIndex = 0; garageIndex < sourceGarageCount; garageIndex++)
            {
                var queuedVehicleCount = sourceGarages[garageIndex].QueuedVehicles.Count;
                if (queuedVehicleCount < request.MinGarageQueuedVehicles ||
                    queuedVehicleCount > request.MaxGarageQueuedVehicles)
                {
                    return false;
                }
            }

            return true;
        }

        private static StageGenerationRequest CapVehicleRequest(
            StageGenerationRequest request,
            int catalogVehicleCap)
        {
            var sourceProfile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var effectiveVehicleCount = Mathf.Min(
                sourceProfile.TargetVehicleCount,
                Mathf.Max(4, catalogVehicleCap));
            if (effectiveVehicleCount == sourceProfile.TargetVehicleCount)
            {
                return request;
            }

            var effectiveProfile = LevelDifficultyProfile.CreateCustom(
                sourceProfile.Difficulty,
                sourceProfile.PassengerFlowRule,
                effectiveVehicleCount,
                sourceProfile.TargetColorCount,
                sourceProfile.ParkingTension,
                sourceProfile.StationPressure,
                sourceProfile.RequireSolutionRoute);
            return new StageGenerationRequest(
                request.StageNumber,
                request.Seed,
                request.Difficulty,
                request.Modifiers,
                effectiveProfile,
                request.Progress,
                request.Post50Pressure,
                request.RoadPresetId,
                request.VehicleLayoutVariantIndex,
                request.VehicleLayoutVariantPoolSize,
                request.GarageCount,
                request.MinGarageQueuedVehicles,
                request.MaxGarageQueuedVehicles,
                request.RotaryCapacity,
                request.MysteryVehicleProfile,
                request.MinSolutionCount,
                request.MaxSolutionCount);
        }

        private static int GetRequestedVehicleCount(StageGenerationRequest request)
        {
            var profile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            return profile.TargetVehicleCount;
        }

        private static bool IsSafeCatalogSource(LevelData source)
        {
            if (source == null ||
                source.PresentationMode != LevelPresentationMode.Standard ||
                source.Buses == null ||
                source.Buses.Count == 0 ||
                source.PassengerUnits == null ||
                source.PassengerUnits.Count == 0 ||
                IsManualOrPreviewShape(source.GenerationSignature))
            {
                return false;
            }

            if (!StageGenerationSignature.TryGetInt(
                    source.GenerationSignature,
                    "layoutVariant",
                    out var layoutVariantIndex) ||
                layoutVariantIndex < 0)
            {
                return false;
            }

            // The release build validator already performs the expensive full validation
            // for every prebuilt level. Repeating it for all 200 assets on the first player
            // frame caused a noticeable startup hitch; the runtime catalog only needs a
            // cheap immutable-content sanity check here.
            return StageGenerationSignature.TryGetInt(
                    source.GenerationSignature,
                    "stage",
                    out var stageNumber) &&
                stageNumber > 0 &&
                StageGenerationSignature.TryGetInt(
                    source.GenerationSignature,
                    "stageCount",
                    out var stageCount) &&
                stageCount >= 200;
        }

        private static bool IsManualOrPreviewShape(string signature)
        {
            if (string.IsNullOrEmpty(signature))
            {
                return true;
            }

            return signature.IndexOf("manualShape=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("shapePreview=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("previewShape=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("templatePreview=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("previewOnly=shape", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasMysteryVehicles(LevelData source)
        {
            var buses = source.Buses;
            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index].StartsConcealed)
                {
                    return true;
                }
            }

            var garages = source.Garages;
            for (var garageIndex = 0; garageIndex < garages.Count; garageIndex++)
            {
                foreach (var vehicle in garages[garageIndex].EnumerateVehicles())
                {
                    if (vehicle.StartsConcealed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetBucketIndex(
            LevelDifficulty difficulty,
            bool hasGarages,
            bool hasMysteryVehicles)
        {
            var difficultyIndex = Mathf.Clamp((int)difficulty, 0, DifficultyBucketCount - 1);
            var garageIndex = hasGarages ? 1 : 0;
            var mysteryIndex = hasMysteryVehicles ? 1 : 0;
            return difficultyIndex * GarageBucketCount * MysteryBucketCount +
                garageIndex * MysteryBucketCount +
                mysteryIndex;
        }

        private static CatalogEntry SelectClosestCandidate(
            StageGenerationRequest request,
            IReadOnlyList<CatalogEntry> candidates)
        {
            var startIndex = GetStableSelectionIndex(request, candidates.Count);
            var best = candidates[startIndex];
            var bestRank = GetCandidateRank(request, best.Level);
            for (var offset = 1; offset < candidates.Count; offset++)
            {
                var candidate = candidates[(startIndex + offset) % candidates.Count];
                var rank = GetCandidateRank(request, candidate.Level);
                if (rank.IsBetterThan(bestRank))
                {
                    best = candidate;
                    bestRank = rank;
                }
            }

            return best;
        }

        private static CandidateRank GetCandidateRank(
            StageGenerationRequest request,
            LevelData source)
        {
            var requestProfile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var sourceProfile = source.DifficultyProfile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var profileVehicleDelta = Mathf.Abs(
                sourceProfile.TargetVehicleCount - requestProfile.TargetVehicleCount);

            // Garage shape and the already-verified profile band are compatibility
            // constraints. Once a compatible release source exists, actual playable
            // vehicle count ranks ahead of the softer profile/road balance score.
            var contractViolationDistance = Mathf.Max(
                0,
                profileVehicleDelta - MaximumCompatibleProfileVehicleDelta);
            var sourceGarageCount = source.Garages != null ? source.Garages.Count : 0;
            contractViolationDistance += Mathf.Abs(sourceGarageCount - request.GarageCount) * 100;
            if (sourceGarageCount > 0)
            {
                for (var index = 0; index < source.Garages.Count; index++)
                {
                    var queuedVehicleCount = source.Garages[index].QueuedVehicles.Count;
                    if (queuedVehicleCount < request.MinGarageQueuedVehicles)
                    {
                        contractViolationDistance +=
                            10 + request.MinGarageQueuedVehicles - queuedVehicleCount;
                    }
                    else if (queuedVehicleCount > request.MaxGarageQueuedVehicles)
                    {
                        contractViolationDistance +=
                            10 + queuedVehicleCount - request.MaxGarageQueuedVehicles;
                    }
                }
            }

            var actualVehicleCount = source.AllVehicles != null ? source.AllVehicles.Count : 0;
            var minimumActualVehicleCount = Mathf.CeilToInt(
                requestProfile.TargetVehicleCount * MinimumActualVehicleTargetRatio);
            contractViolationDistance += Mathf.Max(
                0,
                minimumActualVehicleCount - actualVehicleCount);
            contractViolationDistance += Mathf.Max(
                0,
                Mathf.Abs(source.RotaryUnitCapacity - request.RotaryCapacity) -
                MaximumCompatibleRotaryCapacityDelta);
            var actualVehicleDistance = Mathf.Abs(
                actualVehicleCount - requestProfile.TargetVehicleCount);
            return new CandidateRank(
                contractViolationDistance,
                actualVehicleDistance,
                GetBalanceDistance(request, source));
        }

        private static int GetBalanceDistance(StageGenerationRequest request, LevelData source)
        {
            var requestProfile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var sourceProfile = source.DifficultyProfile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var score = Mathf.Abs(sourceProfile.TargetVehicleCount - requestProfile.TargetVehicleCount) * 40;
            score += Mathf.Abs(sourceProfile.TargetColorCount - requestProfile.TargetColorCount) * 8;
            score += Mathf.Abs(source.RotaryUnitCapacity - request.RotaryCapacity) * 4;
            score += source.RoadPresetId == request.RoadPresetId ? 0 : 20;
            score += Mathf.RoundToInt(
                Mathf.Abs(sourceProfile.ParkingTension - requestProfile.ParkingTension) * 30f);
            score += Mathf.RoundToInt(
                Mathf.Abs(sourceProfile.StationPressure - requestProfile.StationPressure) * 30f);

            var sourceGarageCount = source.Garages != null ? source.Garages.Count : 0;
            score += Mathf.Abs(sourceGarageCount - request.GarageCount) * 120;
            if (sourceGarageCount > 0)
            {
                var sourceQueuedVehicleCount = 0;
                for (var index = 0; index < source.Garages.Count; index++)
                {
                    sourceQueuedVehicleCount += source.Garages[index].QueuedVehicles.Count;
                }

                var sourceQueueAverage = sourceQueuedVehicleCount / (float)sourceGarageCount;
                var requestedQueueAverage =
                    (request.MinGarageQueuedVehicles + request.MaxGarageQueuedVehicles) * 0.5f;
                score += Mathf.RoundToInt(Mathf.Abs(sourceQueueAverage - requestedQueueAverage) * 24f);
            }

            return score;
        }

        private static int GetStableSelectionIndex(StageGenerationRequest request, int candidateCount)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261u;
                const uint prime = 16777619u;
                var hash = offsetBasis;
                hash = (hash ^ (uint)request.StageNumber) * prime;
                hash = (hash ^ (uint)request.Seed) * prime;
                hash = (hash ^ (uint)request.Difficulty) * prime;
                hash = (hash ^ (uint)request.Modifiers) * prime;
                return (int)(hash % (uint)candidateCount);
            }
        }

        private static LevelData CloneLevel(
            LevelData source,
            StageGenerationRequest request,
            int sourceLevelIndex,
            int plannerVehicleCount,
            int catalogVehicleCap)
        {
            if (source == null)
            {
                return null;
            }

            var wantsMysteryVehicles = request.MysteryVehicleProfile.Enabled ||
                (request.Modifiers & (StageModifierFlags.MysteryVehicles | StageModifierFlags.LightMysteryVehicles)) != 0;
            var buses = CloneBuses(source.Buses, request, wantsMysteryVehicles);
            var garages = CloneGarages(source.Garages, false);
            var passengerUnits = new List<PuzzleColor>(source.PassengerUnits);
            var difficultyProfile = CloneDifficultyProfile(source.DifficultyProfile);
            var passengerFlowPlan = ClonePassengerFlowPlan(source.PassengerFlowPlan);
            var requestedVehicleCount = request.Profile != null
                ? request.Profile.TargetVehicleCount
                : 0;
            var actualVehicleCount = source.AllVehicles != null
                ? source.AllVehicles.Count
                : 0;
            var sourceStoredSolutions = source.GenerationSolutionCount;
            var usesSolutionFallback = sourceStoredSolutions < request.MinSolutionCount ||
                sourceStoredSolutions > request.MaxSolutionCount;
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.hideFlags = HideFlags.DontSave;
            level.ConfigureWithPassengerFlowPlan(
                $"Stage {request.StageNumber:000} {difficultyProfile.Difficulty}",
                difficultyProfile,
                passengerFlowPlan,
                buses,
                source.RotaryUnitCapacity,
                source.RoadPresetId,
                passengerUnits,
                garages,
                LevelPresentationMode.Standard);
            level.SetGenerationMetadata(
                $"runtimeSafeCatalog=1;stage={request.StageNumber};sourceStage={sourceLevelIndex + 1};" +
                $"difficulty={(int)difficultyProfile.Difficulty};modifiers={(int)request.Modifiers};" +
                $"garages={garages.Count};mysteryEnabled={(wantsMysteryVehicles ? 1 : 0)};" +
                $"plannerVehicles={plannerVehicleCount};catalogVehicleCap={catalogVehicleCap};" +
                $"requestedVehicles={requestedVehicleCount};actualVehicles={actualVehicleCount};" +
                $"sourceVehicles={difficultyProfile.TargetVehicleCount};" +
                $"requestedSolutionMin={request.MinSolutionCount};" +
                $"requestedSolutionMax={request.MaxSolutionCount};" +
                $"sourceStoredSolutions={sourceStoredSolutions};" +
                $"solutionFallback={(usesSolutionFallback ? 1 : 0)};",
                sourceStoredSolutions);
            return level;
        }

        private static List<BusDefinition> CloneBuses(
            IReadOnlyList<BusDefinition> sourceBuses,
            StageGenerationRequest request,
            bool wantsMysteryVehicles)
        {
            var buses = sourceBuses != null
                ? new List<BusDefinition>(sourceBuses)
                : new List<BusDefinition>();
            for (var index = 0; index < buses.Count; index++)
            {
                buses[index] = buses[index].WithStartsConcealed(false);
            }

            if (!wantsMysteryVehicles || buses.Count == 0)
            {
                return buses;
            }

            var active = new bool[buses.Count];
            for (var index = 0; index < active.Length; index++)
            {
                active[index] = true;
            }

            var candidates = new List<int>();
            for (var index = 0; index < buses.Count; index++)
            {
                if (!LevelVehicleExitPlanner.IsPathClear(index, buses, active, out var blockingIndex) &&
                    blockingIndex >= 0)
                {
                    candidates.Add(index);
                }
            }

            if (candidates.Count == 0)
            {
                // Keep the modifier contract even for an unusually open source layout.
                // Path-clear concealed vehicles are harmless because the board reveals
                // them immediately on load.
                for (var index = Mathf.Min(3, buses.Count - 1); index < buses.Count; index++)
                {
                    candidates.Add(index);
                }
            }

            Shuffle(candidates, new System.Random(request.Seed ^ 0x5f3759df));
            var profile = request.MysteryVehicleProfile;
            var targetCount = Mathf.RoundToInt(buses.Count * profile.Ratio);
            var minimumTarget = Mathf.Max(1, profile.Enabled ? profile.MinVehicles : 1);
            var maximumTarget = Mathf.Max(minimumTarget, profile.Enabled ? profile.MaxVehicles : minimumTarget);
            targetCount = Mathf.Clamp(
                Mathf.Max(targetCount, 1),
                Mathf.Min(minimumTarget, candidates.Count),
                Mathf.Min(maximumTarget, candidates.Count));
            for (var index = 0; index < targetCount; index++)
            {
                var vehicleIndex = candidates[index];
                buses[vehicleIndex] = buses[vehicleIndex].WithStartsConcealed(true);
            }

            return buses;
        }

        private static List<GarageDefinition> CloneGarages(
            IReadOnlyList<GarageDefinition> sourceGarages,
            bool preserveMysteryVehicles)
        {
            var garages = new List<GarageDefinition>();
            if (sourceGarages == null)
            {
                return garages;
            }

            for (var index = 0; index < sourceGarages.Count; index++)
            {
                var source = sourceGarages[index];
                var frontVehicle = preserveMysteryVehicles
                    ? source.FrontVehicle
                    : source.FrontVehicle.WithStartsConcealed(false);
                var queuedVehicles = new List<BusDefinition>(source.QueuedVehicles);
                if (!preserveMysteryVehicles)
                {
                    for (var queueIndex = 0; queueIndex < queuedVehicles.Count; queueIndex++)
                    {
                        queuedVehicles[queueIndex] = queuedVehicles[queueIndex].WithStartsConcealed(false);
                    }
                }

                garages.Add(new GarageDefinition(
                    source.GridPosition,
                    source.ExitDirection,
                    frontVehicle,
                    queuedVehicles));
            }

            return garages;
        }

        private static LevelDifficultyProfile CloneDifficultyProfile(LevelDifficultyProfile source)
        {
            source = source ?? LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            return LevelDifficultyProfile.CreateCustom(
                source.Difficulty,
                source.PassengerFlowRule,
                source.TargetVehicleCount,
                source.TargetColorCount,
                source.ParkingTension,
                source.StationPressure,
                source.RequireSolutionRoute);
        }

        private static PassengerFlowPlan ClonePassengerFlowPlan(PassengerFlowPlan source)
        {
            var clone = new PassengerFlowPlan();
            if (source == null || !source.Enabled)
            {
                return clone;
            }

            switch (source.Mode)
            {
                case PassengerFlowPlanMode.SolutionRoute:
                    clone.ConfigureSolutionRoute(
                        new List<SolutionBusStepDefinition>(source.SolutionRoute),
                        source.MinGroupUnits,
                        source.MaxGroupUnits,
                        source.AutoFillMissingCapacity,
                        source.Seed);
                    break;
                case PassengerFlowPlanMode.RatioByDifficulty:
                    if (source.SolutionRoute.Count > 0)
                    {
                        clone.ConfigureRatioByDifficultyWithSolutionRoute(
                            new List<SolutionBusStepDefinition>(source.SolutionRoute),
                            source.Seed,
                            source.AutoFillMissingCapacity);
                    }
                    else
                    {
                        clone.ConfigureRatioByDifficulty(source.Seed, source.AutoFillMissingCapacity);
                    }
                    break;
                default:
                    clone.ConfigureManualGroups(
                        new List<PassengerGroupDefinition>(source.Groups),
                        source.AutoFillMissingCapacity,
                        source.Seed);
                    break;
            }

            return clone;
        }

        private static void Shuffle(List<int> values, System.Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(0, index + 1);
                var value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private readonly struct CatalogEntry
        {
            public CatalogEntry(LevelData level, int sourceLevelIndex)
            {
                Level = level;
                SourceLevelIndex = sourceLevelIndex;
            }

            public LevelData Level { get; }
            public int SourceLevelIndex { get; }
        }

        private readonly struct CandidateRank
        {
            public CandidateRank(
                int contractViolationDistance,
                int actualVehicleDistance,
                int balanceDistance)
            {
                ContractViolationDistance = contractViolationDistance;
                ActualVehicleDistance = actualVehicleDistance;
                BalanceDistance = balanceDistance;
            }

            private int ContractViolationDistance { get; }
            private int ActualVehicleDistance { get; }
            private int BalanceDistance { get; }

            public bool IsBetterThan(CandidateRank other)
            {
                if (ContractViolationDistance != other.ContractViolationDistance)
                {
                    return ContractViolationDistance < other.ContractViolationDistance;
                }

                if (ActualVehicleDistance != other.ActualVehicleDistance)
                {
                    return ActualVehicleDistance < other.ActualVehicleDistance;
                }

                return BalanceDistance < other.BalanceDistance;
            }
        }
    }
}
