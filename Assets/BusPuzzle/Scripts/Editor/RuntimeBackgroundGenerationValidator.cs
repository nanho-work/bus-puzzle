#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BusPuzzle.EditorTools
{
    public static class RuntimeBackgroundGenerationValidator
    {
        private const string GeneratedSequencePath =
            "Assets/BusPuzzle/Resources/Levels/Generated/GeneratedLevelSequence.asset";
        private const string StageGenerationConfigPath =
            "Assets/BusPuzzle/Resources/Levels/StageGenerationConfig.asset";
        private const int MaximumTotalWaitMilliseconds = 180000;
        private const long MaximumStartMilliseconds = 32;
        private const long MaximumFinalizeMilliseconds = 1500;

        private static readonly int[] RepresentativeStageNumbers =
        {
            201,
            202,
            206,
            223,
            500
        };

        [MenuItem("Bus Puzzle/Validation/Validate Background Runtime Generation")]
        private static void ValidateFromMenu()
        {
            ValidateFromCommandLine();
        }

        public static void ValidateFromCommandLine()
        {
            var releaseSequence =
                AssetDatabase.LoadAssetAtPath<LevelSequence>(
                    GeneratedSequencePath);
            var config =
                AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(
                    StageGenerationConfigPath);
            if (releaseSequence == null || config == null)
            {
                throw new BuildFailedException(
                    "Background generation validation requires the release sequence and StageGenerationConfig.");
            }

            var releaseFingerprints = new HashSet<string>(
                StringComparer.Ordinal);
            for (var index = 0;
                index < releaseSequence.StaticLevels.Count;
                index++)
            {
                var releaseLevel = releaseSequence.StaticLevels[index];
                if (releaseLevel != null)
                {
                    releaseFingerprints.Add(
                        CreateLayoutFingerprint(releaseLevel));
                }
            }

            var runtimeSequence = LevelSequence.CreateRuntimeGenerated(
                config,
                releaseSequence.StaticLevels);
            var pending = new HashSet<int>();
            var acceptedFingerprints = new HashSet<string>(
                StringComparer.Ordinal);
            var startedAt = Stopwatch.StartNew();

            try
            {
                for (var index = 0;
                    index < RepresentativeStageNumbers.Length;
                    index++)
                {
                    var stageNumber =
                        RepresentativeStageNumbers[index];
                    var levelIndex = stageNumber - 1;
                    var startWatch = Stopwatch.StartNew();
                    var started =
                        runtimeSequence.StartRuntimeLevelGeneration(
                            levelIndex);
                    startWatch.Stop();
                    if (!started)
                    {
                        throw new BuildFailedException(
                            $"Stage {stageNumber:000} background generation did not start.");
                    }

                    if (startWatch.ElapsedMilliseconds >
                        MaximumStartMilliseconds)
                    {
                        throw new BuildFailedException(
                            $"Stage {stageNumber:000} background start took " +
                            $"{startWatch.ElapsedMilliseconds} ms; main-thread setup must stay below " +
                            $"{MaximumStartMilliseconds} ms.");
                    }

                    pending.Add(levelIndex);
                }

                while (pending.Count > 0)
                {
                    if (startedAt.ElapsedMilliseconds >
                        MaximumTotalWaitMilliseconds)
                    {
                        throw new BuildFailedException(
                            $"Background generation timed out with {pending.Count} representative stages pending.");
                    }

                    var snapshot = new List<int>(pending);
                    for (var index = 0;
                        index < snapshot.Count;
                        index++)
                    {
                        var levelIndex = snapshot[index];
                        var finalizeWatch = Stopwatch.StartNew();
                        var committed =
                            runtimeSequence.TryFinalizeRuntimeLevelGeneration(
                                levelIndex,
                                -1,
                                out var finished,
                                out var diagnostic);
                        finalizeWatch.Stop();
                        if (!finished)
                        {
                            continue;
                        }

                        pending.Remove(levelIndex);
                        if (!committed)
                        {
                            throw new BuildFailedException(
                                string.IsNullOrWhiteSpace(diagnostic)
                                    ? $"Stage {levelIndex + 1:000} background generation was rejected."
                                    : diagnostic);
                        }

                        if (finalizeWatch.ElapsedMilliseconds >
                            MaximumFinalizeMilliseconds)
                        {
                            throw new BuildFailedException(
                                $"Stage {levelIndex + 1:000} main-thread finalize took " +
                                $"{finalizeWatch.ElapsedMilliseconds} ms, above the " +
                                $"{MaximumFinalizeMilliseconds} ms safety ceiling.");
                        }

                        ValidateAcceptedLevel(
                            runtimeSequence,
                            config,
                            levelIndex,
                            releaseFingerprints,
                            acceptedFingerprints);
                        Debug.Log(
                            $"{diagnostic} Main-thread finalize: " +
                            $"{finalizeWatch.ElapsedMilliseconds} ms.");
                    }

                    if (pending.Count > 0)
                    {
                        Thread.Sleep(2);
                    }
                }

                if (acceptedFingerprints.Count <
                    RepresentativeStageNumbers.Length)
                {
                    throw new BuildFailedException(
                        $"Representative background generation produced only " +
                        $"{acceptedFingerprints.Count}/{RepresentativeStageNumbers.Length} distinct layouts.");
                }

                ValidateWorkerHeartSnapshot(config);
                ValidateQueueCancellationDoesNotBlockFollower(
                    config);
                ValidatePinnedRuntimeCache(runtimeSequence);
                ValidateSolverCancellation();
                Debug.Log(
                    $"Background runtime generation passed: " +
                    $"{RepresentativeStageNumbers.Length} distinct, non-release layouts; " +
                    $"total worker time {startedAt.ElapsedMilliseconds} ms.");
            }
            finally
            {
                runtimeSequence.ReleaseRuntimeResources();
                UnityEngine.Object.DestroyImmediate(runtimeSequence);
            }
        }

        private static void ValidateAcceptedLevel(
            LevelSequence runtimeSequence,
            StageGenerationConfig config,
            int levelIndex,
            ISet<string> releaseFingerprints,
            ISet<string> acceptedFingerprints)
        {
            var stageNumber = levelIndex + 1;
            if (!runtimeSequence.IsProcedurallyGeneratedLevelCached(
                    levelIndex) ||
                !runtimeSequence.TryGetPreparedLevel(
                    levelIndex,
                    out var level) ||
                level == null)
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber:000} did not commit as a procedural level.");
            }

            if (!StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "runtimeProcedural",
                    out var runtimeProcedural) ||
                runtimeProcedural != 1 ||
                !StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "background",
                    out var background) ||
                background != 1 ||
                !StageGenerationSignature.TryGetInt(
                    level.GenerationSignature,
                    "stage",
                    out var signatureStageNumber) ||
                signatureStageNumber != stageNumber)
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber:000} has an invalid background procedural signature: " +
                    level.GenerationSignature);
            }

            var report = LevelValidator.Validate(level, false);
            if (report == null || report.HasErrors)
            {
                throw new BuildFailedException(
                    report != null
                        ? report.ToConsoleMessage(level.LevelName)
                        : $"Stage {stageNumber:000} returned no validation report.");
            }

            var request = StageGenerationPlanner.CreateRequest(
                config,
                stageNumber);
            var minimumVehicleCount = Mathf.CeilToInt(
                request.Profile.TargetVehicleCount * 0.75f);
            if (level.AllVehicles.Count < minimumVehicleCount)
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber:000} generated {level.AllVehicles.Count}/" +
                    $"{request.Profile.TargetVehicleCount} target vehicles.");
            }

            var solutionDistance =
                level.GenerationSolutionCount <
                    request.MinSolutionCount
                    ? request.MinSolutionCount -
                        level.GenerationSolutionCount
                    : level.GenerationSolutionCount >
                        request.MaxSolutionCount
                        ? level.GenerationSolutionCount -
                            request.MaxSolutionCount
                        : 0;
            if (solutionDistance > 2)
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber:000} recorded " +
                    $"{level.GenerationSolutionCount} bounded solutions; preferred " +
                    $"{request.MinSolutionCount}-{request.MaxSolutionCount} with a maximum " +
                    "near-range distance of 2.");
            }

            var solutionWatch = Stopwatch.StartNew();
            var difficultyAnalysis = StageSolutionAnalyzer.Analyze(
                level.Buses,
                level.Garages,
                request.MaxSolutionCount + 1,
                8192);
            solutionWatch.Stop();
            Debug.Log(
                $"Stage {stageNumber:000} difficulty solution probe: " +
                $"{difficultyAnalysis.SolutionCount} solution(s), " +
                $"preferred {request.MinSolutionCount}-{request.MaxSolutionCount}, " +
                $"hitLimit={difficultyAnalysis.HitLimit}, " +
                $"{solutionWatch.ElapsedMilliseconds} ms.");

            var fingerprint = CreateLayoutFingerprint(level);
            if (releaseFingerprints.Contains(fingerprint))
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber:000} repeated a locked 1-200 release topology.");
            }

            if (!acceptedFingerprints.Add(fingerprint))
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber:000} repeated another representative runtime topology.");
            }
        }

        private static void ValidateSolverCancellation()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            try
            {
                StageSolutionAnalyzer.Analyze(
                    Array.Empty<BusDefinition>(),
                    Array.Empty<GarageDefinition>(),
                    1,
                    2048,
                    cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            throw new BuildFailedException(
                "StageSolutionAnalyzer accepted a pre-cancelled runtime generation request.");
        }

        private static void ValidateQueueCancellationDoesNotBlockFollower(
            StageGenerationConfig config)
        {
            const int cancelledLevelIndex = 600;
            const int followerLevelIndex = 601;
            var sequence =
                LevelSequence.CreateRuntimeGenerated(config);
            var wait = Stopwatch.StartNew();
            try
            {
                if (!sequence.StartRuntimeLevelGeneration(
                        cancelledLevelIndex) ||
                    !sequence.StartRuntimeLevelGeneration(
                        followerLevelIndex))
                {
                    throw new BuildFailedException(
                        "Runtime generation cancellation regression jobs did not start.");
                }

                if (!sequence.CancelRuntimeLevelGeneration(
                        cancelledLevelIndex))
                {
                    throw new BuildFailedException(
                        "Runtime generation cancellation regression could not cancel its FIFO head.");
                }

                while (true)
                {
                    var committed =
                        sequence.TryFinalizeRuntimeLevelGeneration(
                            followerLevelIndex,
                            -1,
                            out var finished,
                            out var diagnostic);
                    if (finished)
                    {
                        if (!committed)
                        {
                            throw new BuildFailedException(
                                string.IsNullOrWhiteSpace(diagnostic)
                                    ? "The FIFO follower was rejected after its predecessor was cancelled."
                                    : diagnostic);
                        }

                        break;
                    }

                    if (wait.ElapsedMilliseconds >
                        MaximumTotalWaitMilliseconds)
                    {
                        throw new BuildFailedException(
                            "The FIFO follower remained blocked after its predecessor was cancelled.");
                    }

                    Thread.Sleep(2);
                }

                if (!sequence.StartRuntimeLevelGeneration(
                        cancelledLevelIndex))
                {
                    throw new BuildFailedException(
                        "A cancelled stage was incorrectly cached as a terminal generation failure.");
                }

                sequence.CancelRuntimeLevelGeneration(
                    cancelledLevelIndex);
                Debug.Log(
                    "Background FIFO cancellation passed: a cancelled head did not block its follower.");
            }
            finally
            {
                sequence.ReleaseRuntimeResources();
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        private static void ValidatePinnedRuntimeCache(
            LevelSequence sequence)
        {
            const int pinnedLevelIndex = 200;
            if (!sequence.TryGetPreparedLevel(
                    pinnedLevelIndex,
                    out var pinnedLevel) ||
                pinnedLevel == null)
            {
                throw new BuildFailedException(
                    "Pinned cache regression requires generated stage 201.");
            }

            sequence.PinActiveRuntimeLevel(
                pinnedLevelIndex);
            for (var levelIndex = 700;
                levelIndex < 712;
                levelIndex++)
            {
                if (!sequence.PrepareSafeGameplayLevel(
                        levelIndex,
                        "pinned cache regression",
                        false))
                {
                    throw new BuildFailedException(
                        $"Pinned cache regression could not prepare stage {levelIndex + 1:000}.");
                }
            }

            if (!sequence.TryGetPreparedLevel(
                    pinnedLevelIndex,
                    out var retainedLevel) ||
                !ReferenceEquals(
                    pinnedLevel,
                    retainedLevel))
            {
                throw new BuildFailedException(
                    "Runtime cache eviction released the pinned active stage.");
            }

            if (sequence.RuntimePreparedLevelCount > 8)
            {
                throw new BuildFailedException(
                    $"Runtime cache retained {sequence.RuntimePreparedLevelCount} levels; expected at most 8.");
            }

            sequence.PinActiveRuntimeLevel(-1);
            Debug.Log(
                "Runtime cache pinning passed: the active stage survived bounded-cache eviction.");
        }

        private static void ValidateWorkerHeartSnapshot(
            StageGenerationConfig config)
        {
            const int heartStageNumber = 8;
            const int expectedHeartLayoutVariant = 216;
            var sequence = LevelSequence.CreateRuntimeGenerated(config);
            var wait = Stopwatch.StartNew();
            try
            {
                if (!sequence.StartRuntimeLevelGeneration(
                        heartStageNumber - 1))
                {
                    throw new BuildFailedException(
                        "Automatic Heart worker snapshot validation did not start.");
                }

                while (true)
                {
                    var committed =
                        sequence.TryFinalizeRuntimeLevelGeneration(
                            heartStageNumber - 1,
                            -1,
                            out var finished,
                            out var diagnostic);
                    if (finished)
                    {
                        if (!committed ||
                            !sequence.TryGetPreparedLevel(
                                heartStageNumber - 1,
                                out var heartLevel) ||
                            heartLevel == null)
                        {
                            throw new BuildFailedException(
                                string.IsNullOrWhiteSpace(diagnostic)
                                    ? "Automatic Heart worker snapshot was rejected."
                                    : diagnostic);
                        }

                        if (!StageGenerationSignature.TryGetInt(
                                heartLevel.GenerationSignature,
                                "layoutVariant",
                                out var layoutVariant) ||
                            layoutVariant !=
                                expectedHeartLayoutVariant)
                        {
                            throw new BuildFailedException(
                                $"Automatic Heart worker snapshot expected layout variant " +
                                $"{expectedHeartLayoutVariant}, got {layoutVariant}.");
                        }

                        var report = LevelValidator.Validate(
                            heartLevel,
                            false);
                        if (report == null || report.HasErrors)
                        {
                            throw new BuildFailedException(
                                report != null
                                    ? report.ToConsoleMessage(
                                        heartLevel.LevelName)
                                    : "Automatic Heart worker snapshot returned no validation report.");
                        }

                        Debug.Log(
                            $"Background Heart template snapshot passed: " +
                            $"stage {heartStageNumber:000}, layoutVariant {layoutVariant}, " +
                            $"{heartLevel.Buses.Count} vehicles.");
                        return;
                    }

                    if (wait.ElapsedMilliseconds >
                        MaximumTotalWaitMilliseconds)
                    {
                        throw new BuildFailedException(
                            "Automatic Heart worker snapshot timed out.");
                    }

                    Thread.Sleep(2);
                }
            }
            finally
            {
                sequence.ReleaseRuntimeResources();
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        private static string CreateLayoutFingerprint(
            LevelData level)
        {
            var builder = new StringBuilder(4096);
            AppendVehicles(builder, level.Buses);
            builder.Append("|G:");
            var garages = level.Garages;
            for (var garageIndex = 0;
                garageIndex < garages.Count;
                garageIndex++)
            {
                var garage = garages[garageIndex];
                builder
                    .Append(garage.GridPosition.x).Append(',')
                    .Append(garage.GridPosition.y).Append(',')
                    .Append((int)garage.ExitDirection).Append(';');
                foreach (var vehicle in garage.EnumerateVehicles())
                {
                    AppendVehicle(builder, vehicle);
                }

                builder.Append('|');
            }

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes(builder.ToString()));
            var hex = new StringBuilder(bytes.Length * 2);
            for (var index = 0; index < bytes.Length; index++)
            {
                hex.Append(
                    bytes[index].ToString(
                        "x2",
                        CultureInfo.InvariantCulture));
            }

            return hex.ToString();
        }

        private static void AppendVehicles(
            StringBuilder builder,
            IReadOnlyList<BusDefinition> vehicles)
        {
            if (vehicles == null)
            {
                return;
            }

            for (var index = 0; index < vehicles.Count; index++)
            {
                AppendVehicle(builder, vehicles[index]);
            }
        }

        private static void AppendVehicle(
            StringBuilder builder,
            BusDefinition vehicle)
        {
            builder
                .Append((int)vehicle.Color).Append(',')
                .Append((int)vehicle.Size).Append(',')
                .Append((int)vehicle.Direction).Append(',')
                .Append(vehicle.GridPosition.x).Append(',')
                .Append(vehicle.GridPosition.y).Append(',')
                .Append(vehicle.AngleOffsetDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture)).Append(',')
                .Append(vehicle.PositionOffsetCells.x.ToString(
                    "R",
                    CultureInfo.InvariantCulture)).Append(',')
                .Append(vehicle.PositionOffsetCells.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture)).Append(',')
                .Append(vehicle.StartsConcealed ? 1 : 0)
                .Append(';');
        }
    }
}
#endif
