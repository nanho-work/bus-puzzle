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
        private const int MaximumCatalogVehicleSelectionSlack = 2;
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
            if (!TrySelectClosestCandidate(
                    effectiveRequest,
                    candidates,
                    out var selected))
            {
                return false;
            }

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

        private static bool TrySelectClosestCandidate(
            StageGenerationRequest request,
            IReadOnlyList<CatalogEntry> candidates,
            out CatalogEntry selected)
        {
            var startIndex = GetStableSelectionIndex(request, candidates.Count);
            selected = default;
            var requestedVehicleCount = GetRequestedVehicleCount(request);
            var bestActualVehicleDistance = int.MaxValue;
            for (var offset = 0; offset < candidates.Count; offset++)
            {
                var candidate = candidates[(startIndex + offset) % candidates.Count];
                if (!MeetsStructuralRuntimeContract(request, candidate.Level))
                {
                    continue;
                }

                var actualVehicleCount = candidate.Level.AllVehicles != null
                    ? candidate.Level.AllVehicles.Count
                    : 0;
                bestActualVehicleDistance = Mathf.Min(
                    bestActualVehicleDistance,
                    Mathf.Abs(actualVehicleCount - requestedVehicleCount));
            }

            if (bestActualVehicleDistance == int.MaxValue)
            {
                return false;
            }

            var maximumVehicleDistance =
                bestActualVehicleDistance +
                MaximumCatalogVehicleSelectionSlack;
            for (var offset = 0; offset < candidates.Count; offset++)
            {
                var candidate = candidates[(startIndex + offset) % candidates.Count];
                if (!MeetsStructuralRuntimeContract(request, candidate.Level))
                {
                    continue;
                }

                var actualVehicleCount = candidate.Level.AllVehicles != null
                    ? candidate.Level.AllVehicles.Count
                    : 0;
                if (Mathf.Abs(actualVehicleCount - requestedVehicleCount) >
                    maximumVehicleDistance)
                {
                    continue;
                }

                // The hashed start index spreads otherwise equivalent, verified
                // sources across the endless stream. Every eligible candidate already
                // satisfies the hard garage/queue/rotary contract above.
                selected = candidate;
                return true;
            }

            return false;
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
            var mirrorX = ShouldMirrorHorizontally(request);
            if (mirrorX)
            {
                buses = MirrorBusesHorizontally(buses);
                garages = MirrorGaragesHorizontally(garages);
            }

            var passengerUnits = new List<PuzzleColor>(source.PassengerUnits);
            var difficultyProfile = CloneDifficultyProfile(
                request.Profile ??
                    source.DifficultyProfile);
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
                request.RotaryCapacity,
                request.RoadPresetId,
                passengerUnits,
                garages,
                LevelPresentationMode.Standard);
            level.SetGenerationMetadata(
                $"runtimeSafeCatalog=1;stage={request.StageNumber};sourceStage={sourceLevelIndex + 1};" +
                $"difficulty={(int)difficultyProfile.Difficulty};modifiers={(int)request.Modifiers};" +
                $"requestedRoad={(int)request.RoadPresetId};" +
                $"garages={garages.Count};mysteryEnabled={(wantsMysteryVehicles ? 1 : 0)};" +
                $"mirrorX={(mirrorX ? 1 : 0)};" +
                $"plannerVehicles={plannerVehicleCount};catalogVehicleCap={catalogVehicleCap};" +
                $"requestedVehicles={requestedVehicleCount};actualVehicles={actualVehicleCount};" +
                $"effectiveProfileVehicles={difficultyProfile.TargetVehicleCount};" +
                $"sourceVehicles={source.DifficultyProfile.TargetVehicleCount};" +
                $"requestedSolutionMin={request.MinSolutionCount};" +
                $"requestedSolutionMax={request.MaxSolutionCount};" +
                $"sourceStoredSolutions={sourceStoredSolutions};" +
                $"solutionFallback={(usesSolutionFallback ? 1 : 0)};",
                sourceStoredSolutions);
            return level;
        }

        private static bool ShouldMirrorHorizontally(
            StageGenerationRequest request)
        {
            unchecked
            {
                // Seed is derived linearly from StageNumber, so using only the raw
                // low bit makes both values cancel for most stages. Avalanche the
                // independent fields before choosing the mirror bit.
                var hash = (uint)request.Seed;
                hash ^= (uint)request.StageNumber * 0x9e3779b9u;
                hash ^= (uint)request.Modifiers * 0x85ebca6bu;
                hash ^= (uint)request.Difficulty * 0xc2b2ae35u;
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                hash *= 0x846ca68bu;
                hash ^= hash >> 16;
                return (hash & 1u) != 0u;
            }
        }

        private static List<BusDefinition> MirrorBusesHorizontally(
            IReadOnlyList<BusDefinition> sourceBuses)
        {
            var mirrored = new List<BusDefinition>(
                sourceBuses != null ? sourceBuses.Count : 0);
            if (sourceBuses == null)
            {
                return mirrored;
            }

            for (var index = 0; index < sourceBuses.Count; index++)
            {
                mirrored.Add(MirrorBusHorizontally(sourceBuses[index]));
            }

            return mirrored;
        }

        private static List<GarageDefinition> MirrorGaragesHorizontally(
            IReadOnlyList<GarageDefinition> sourceGarages)
        {
            var mirrored = new List<GarageDefinition>(
                sourceGarages != null ? sourceGarages.Count : 0);
            if (sourceGarages == null)
            {
                return mirrored;
            }

            for (var garageIndex = 0;
                garageIndex < sourceGarages.Count;
                garageIndex++)
            {
                var source = sourceGarages[garageIndex];
                var queuedVehicles = new List<BusDefinition>(
                    source.QueuedVehicleCount);
                for (var queueIndex = 0;
                    queueIndex < source.QueuedVehicleCount;
                    queueIndex++)
                {
                    queuedVehicles.Add(
                        MirrorBusHorizontally(
                            source.QueuedVehicles[queueIndex]));
                }

                mirrored.Add(
                    new GarageDefinition(
                        MirrorGridPositionHorizontally(
                            source.GridPosition),
                        MirrorDirectionHorizontally(
                            source.ExitDirection),
                        MirrorBusHorizontally(
                            source.FrontVehicle),
                        queuedVehicles));
            }

            return mirrored;
        }

        private static BusDefinition MirrorBusHorizontally(
            BusDefinition source)
        {
            return new BusDefinition(
                source.Color,
                source.Size,
                MirrorDirectionHorizontally(source.Direction),
                MirrorGridPositionHorizontally(source.GridPosition),
                -source.AngleOffsetDegrees,
                new Vector2(
                    -source.PositionOffsetCells.x,
                    source.PositionOffsetCells.y),
                source.StartsConcealed);
        }

        private static Vector2Int MirrorGridPositionHorizontally(
            Vector2Int position)
        {
            return new Vector2Int(
                BoardLayoutConfig.GridColumns - 1 - position.x,
                position.y);
        }

        private static GridDirection MirrorDirectionHorizontally(
            GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Right:
                    return GridDirection.Left;
                case GridDirection.Left:
                    return GridDirection.Right;
                default:
                    return direction;
            }
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

    }
}
