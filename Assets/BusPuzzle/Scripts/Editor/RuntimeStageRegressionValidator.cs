#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BusPuzzle.EditorTools
{
    public static class RuntimeStageRegressionValidator
    {
        private const string GeneratedSequencePath =
            "Assets/BusPuzzle/Resources/Levels/Generated/GeneratedLevelSequence.asset";
        private const string StageGenerationConfigPath =
            "Assets/BusPuzzle/Resources/Levels/StageGenerationConfig.asset";
        private const int MaximumValidatedStageNumber = 10000;
        private const long MaximumSequenceSetupMilliseconds = 500;
        private const long MaximumStageResolveMilliseconds = 5000;
        private const long MaximumIndependentValidationMilliseconds = 500;
        private const long MaximumGameplaySafeResolveMilliseconds = 50;
        private const int MinimumGameplaySafeDistinctSources = 20;
        private const int MinimumGameplaySafeNormalVariants = 8;
        private const int MinimumGameplaySafeHardVariants = 8;
        private const int MinimumGameplaySafeSuperHardVariants = 4;
        private const int BoardRenderResourceSoakCycles = 3;
        private const int MaximumBoardRenderObjectCountDrift = 4;
        private const int SolvabilityNodeVisitLimit = 20000;
        private const float MinimumActualVehicleTargetRatio = 0.75f;
        private const int ExpectedBoardGridColumns = 14;
        private const int ExpectedLockedReleaseStageCount = 200;
        private const int ExpectedLockedReleaseLayoutVariantPoolSize = 220;
        private const int ExpectedEndlessDifficultyPatternLength = 23;
        private const int ExpectedEndlessIntensityPatternLength = 29;
        private const int EndlessCompositeWindowLength =
            ExpectedEndlessDifficultyPatternLength * ExpectedEndlessIntensityPatternLength;
        private const int LateMasteryWindowOffset = EndlessCompositeWindowLength * 6;
        private static readonly int[] BoardRenderResourceThemeStageNumbers =
        {
            1,
            11,
            21,
            31,
            41,
            51,
            61
        };
        private const float MinimumCompositeDifficultyIncrease = 0.75f;
        private const float DifficultyComparisonEpsilon = 0.0001f;
        private static readonly int[] RepresentativeRegressionStages =
        {
            201, 202, 203, 204, 205, 206, 207, 208, 209, 210,
            215, 220, 225, 230, 250, 287, 296, 300, 400,
            449, 450, 451, 452,
            500, 750,
            995, 996, 997, 998, 999, 1000, 1001,
            1998, 1999, 2000, 2001, 2002,
            4998, 4999, 5000, 5001, 5002,
            9995, 9996, 9997, 9998, 9999, 10000
        };
        private static readonly int[] GameplaySafetyRegressionStages =
            RepresentativeRegressionStages;

        [MenuItem("Bus Puzzle/Levels/Validate Procedural Engine (201-10000 Samples)")]
        public static void ValidateRuntimeStageContinuity()
        {
            var config = AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(StageGenerationConfigPath);
            var releaseSequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(GeneratedSequencePath);
            ValidateRequiredAssets(config, releaseSequence);

            var releaseSnapshot = CaptureReleaseSnapshot(releaseSequence);
            ValidateLockedReleaseLayoutMapping(config, releaseSequence);
            ValidateDifficultyProgressionContract(config);
            ValidatePlannerRange(config);

            var runtimeSequence = CreateRuntimeSequence(config, releaseSequence, out var setupMilliseconds);
            var firstRunLevels = new List<LevelData>();
            var firstRunRecords = new Dictionary<int, StageRecord>();
            try
            {
                ValidateSequenceSetup(runtimeSequence, releaseSequence, releaseSnapshot, setupMilliseconds);

                var generatedGeometryOwners = new Dictionary<string, int>(StringComparer.Ordinal);
                for (var stageIndex = 0; stageIndex < RepresentativeRegressionStages.Length; stageIndex++)
                {
                    var stageNumber = RepresentativeRegressionStages[stageIndex];
                    var level = GenerateAndValidateProceduralStage(
                        config,
                        stageNumber,
                        out var resolveMilliseconds,
                        out var validationMilliseconds);
                    firstRunLevels.Add(level);

                    var record = StageRecord.Create(level);
                    RejectReleaseClone(stageNumber, record, releaseSnapshot);
                    RejectGeneratedDuplicate(stageNumber, record, generatedGeometryOwners);
                    firstRunRecords.Add(stageNumber, record);
                    StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "candidate",
                        out var selectedCandidateIndex);
                    StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "layoutVariant",
                        out var layoutVariantIndex);
                    StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "difficulty",
                        out var difficultyIndex);
                    StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "garages",
                        out var garageCount);

                    Debug.Log(
                        $"Runtime procedural stage {stageNumber:00000} passed: " +
                        $"resolve {resolveMilliseconds} ms, validate {validationMilliseconds} ms, " +
                        $"candidate {selectedCandidateIndex + 1}, difficulty {difficultyIndex}, " +
                        $"layout {layoutVariantIndex}, garages {garageCount}, " +
                        $"vehicles {level.AllVehicles.Count}, " +
                        $"geometry {ShortHash(record.GeometryFingerprint)}.");
                }

                ValidateReleaseLevelsRemainUntouched(runtimeSequence, releaseSequence, releaseSnapshot);
                var productionResolvedLevels = new List<LevelData>();
                try
                {
                    ValidateProductionSequenceResolution(
                        runtimeSequence,
                        config,
                        releaseSnapshot,
                        firstRunRecords,
                        productionResolvedLevels);
                }
                finally
                {
                    DestroyRuntimeObjects(productionResolvedLevels, null);
                }

                var secondRunLevels = new List<LevelData>();
                try
                {
                    for (var stageIndex = 0; stageIndex < RepresentativeRegressionStages.Length; stageIndex++)
                    {
                        var stageNumber = RepresentativeRegressionStages[stageIndex];
                        var level = GenerateAndValidateProceduralStage(
                            config,
                            stageNumber,
                            out _,
                            out _);
                        secondRunLevels.Add(level);

                        var actual = StageRecord.Create(level);
                        var expected = firstRunRecords[stageNumber];
                        if (!expected.Equals(actual))
                        {
                            throw new BuildFailedException(
                                $"Runtime stage {stageNumber:00000} is not deterministic. " +
                                $"First signature '{expected.GenerationSignature}', second " +
                                $"'{actual.GenerationSignature}'; first content " +
                                $"{ShortHash(expected.StructuralFingerprint)}, second " +
                                $"{ShortHash(actual.StructuralFingerprint)}.");
                        }
                    }

                    ValidateReleaseLevelsRemainUntouched(runtimeSequence, releaseSequence, releaseSnapshot);
                }
                finally
                {
                    DestroyRuntimeObjects(secondRunLevels, null);
                }

                ValidateCatalogFallbackInIsolation(config, releaseSequence, releaseSnapshot);
                ValidateGameplaySafeResolution(config, releaseSequence);
            }
            finally
            {
                DestroyRuntimeObjects(firstRunLevels, runtimeSequence);
            }

            Debug.Log(
                $"Offline procedural-engine regression passed: planner stages 201-{MaximumValidatedStageNumber} " +
                $"and {RepresentativeRegressionStages.Length} generated boards were deterministic, unique, " +
                "not release-catalog clones, valid, greedy-exitable, independently solvable, and bounded. " +
                "Production sequence stages 207/10000 matched the direct generator without persistent cache changes. " +
                $"Gameplay-safe catalog resolution was validated separately. Sequence setup " +
                $"{setupMilliseconds} ms; generation limit {MaximumStageResolveMilliseconds} ms/stage.");
        }

        public static void ValidateRuntimeStageContinuityFromCommandLine()
        {
            ValidateRuntimeStageContinuity();
        }

        [MenuItem("Bus Puzzle/Levels/Validate Runtime Catalog Fallback In Isolation")]
        public static void ValidateRuntimeCatalogFallbackFromCommandLine()
        {
            var config = AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(StageGenerationConfigPath);
            var releaseSequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(GeneratedSequencePath);
            ValidateRequiredAssets(config, releaseSequence);
            ValidateCatalogFallbackInIsolation(config, releaseSequence, CaptureReleaseSnapshot(releaseSequence));
        }

        [MenuItem("Bus Puzzle/Levels/Validate Gameplay-Safe Runtime Resolution")]
        public static void ValidateGameplaySafeResolutionFromCommandLine()
        {
            var config = AssetDatabase.LoadAssetAtPath<StageGenerationConfig>(StageGenerationConfigPath);
            var releaseSequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(GeneratedSequencePath);
            ValidateRequiredAssets(config, releaseSequence);
            ValidateGameplaySafeResolution(config, releaseSequence);
        }

        [MenuItem("Bus Puzzle/Release/Validate Board Render Resource Lifetime")]
        public static void ValidateBoardRenderResourceLifetimeFromCommandLine()
        {
            var releaseSequence =
                AssetDatabase.LoadAssetAtPath<LevelSequence>(
                    GeneratedSequencePath);
            if (releaseSequence == null ||
                !releaseSequence.IsVerifiedGeneratedSet ||
                releaseSequence.Count <= 0)
            {
                throw new BuildFailedException(
                    $"A verified generated release sequence is required: " +
                    $"{GeneratedSequencePath}");
            }

            var level = releaseSequence.GetLevel(
                releaseSequence.Count - 1);
            if (level == null)
            {
                throw new BuildFailedException(
                    "Board render resource soak level is missing.");
            }

            var baselineOwnedMeshCount =
                BoardView.RuntimeOwnedMeshCount;
            GameObject boardObject = null;
            BoardView boardView = null;
            var fullyWarmedMaterialCacheCount = -1;
            var referenceNativeMaterialCounts =
                new int[BoardRenderResourceThemeStageNumbers.Length];
            var referenceNativeMeshCounts =
                new int[BoardRenderResourceThemeStageNumbers.Length];
            var referenceOwnedMeshCounts =
                new int[BoardRenderResourceThemeStageNumbers.Length];
            var maximumNativeMaterialCount = 0;
            var maximumNativeMeshCount = 0;
            var minimumOwnedMeshCount = int.MaxValue;
            var maximumOwnedMeshCount = 0;
            try
            {
                boardObject =
                    new GameObject("Board Render Resource Soak");
                boardView = boardObject.AddComponent<BoardView>();
                var passengers = new List<PassengerView>();
                var buses = new List<BusView>();

                var iterationCount =
                    BoardRenderResourceThemeStageNumbers.Length *
                    BoardRenderResourceSoakCycles;
                for (var iteration = 0;
                    iteration < iterationCount;
                    iteration++)
                {
                    var themeIndex =
                        iteration %
                        BoardRenderResourceThemeStageNumbers.Length;
                    var cycleIndex =
                        iteration /
                        BoardRenderResourceThemeStageNumbers.Length;
                    var themeStageNumber =
                        BoardRenderResourceThemeStageNumbers[themeIndex];
                    boardView.BuildLevel(
                        level,
                        passengers,
                        buses,
                        themeStageNumber);
                    for (var busIndex = 0;
                        busIndex < buses.Count;
                        busIndex++)
                    {
                        buses[busIndex]?.RevealConcealed();
                    }

                    var materialCacheCount =
                        PuzzlePalette.RuntimeMaterialCount;
                    var nativeMaterialCount =
                        Resources.FindObjectsOfTypeAll<Material>()
                            .Length;
                    var nativeMeshCount =
                        Resources.FindObjectsOfTypeAll<Mesh>()
                            .Length;
                    var ownedMeshCount =
                        BoardView.RuntimeOwnedMeshCount;

                    if (cycleIndex == 0)
                    {
                        if (themeIndex ==
                            BoardRenderResourceThemeStageNumbers.Length -
                            1)
                        {
                            fullyWarmedMaterialCacheCount =
                                materialCacheCount;
                        }
                    }
                    else
                    {
                        if (materialCacheCount !=
                            fullyWarmedMaterialCacheCount)
                        {
                            throw new BuildFailedException(
                                $"Board render material cache changed after " +
                                $"all themes were warmed: " +
                                $"{fullyWarmedMaterialCacheCount} -> " +
                                $"{materialCacheCount} on cycle " +
                                $"{cycleIndex + 1}, theme stage " +
                                $"{themeStageNumber}.");
                        }

                        if (cycleIndex == 1)
                        {
                            referenceNativeMaterialCounts[themeIndex] =
                                nativeMaterialCount;
                            referenceNativeMeshCounts[themeIndex] =
                                nativeMeshCount;
                            referenceOwnedMeshCounts[themeIndex] =
                                ownedMeshCount;
                        }
                        else
                        {
                            if (ownedMeshCount !=
                                referenceOwnedMeshCounts[themeIndex])
                            {
                                throw new BuildFailedException(
                                    $"Board-owned mesh count drifted for " +
                                    $"theme stage {themeStageNumber}: " +
                                    $"{referenceOwnedMeshCounts[themeIndex]} " +
                                    $"-> {ownedMeshCount}.");
                            }

                            if (nativeMaterialCount >
                                referenceNativeMaterialCounts[themeIndex] +
                                MaximumBoardRenderObjectCountDrift ||
                                nativeMeshCount >
                                referenceNativeMeshCounts[themeIndex] +
                                MaximumBoardRenderObjectCountDrift)
                            {
                                throw new BuildFailedException(
                                    $"Board native render resources did not " +
                                    $"plateau for theme stage " +
                                    $"{themeStageNumber}: materials " +
                                    $"{referenceNativeMaterialCounts[themeIndex]} " +
                                    $"-> {nativeMaterialCount}, meshes " +
                                    $"{referenceNativeMeshCounts[themeIndex]} " +
                                    $"-> {nativeMeshCount}.");
                            }
                        }
                    }

                    maximumNativeMaterialCount = Math.Max(
                        maximumNativeMaterialCount,
                        nativeMaterialCount);
                    maximumNativeMeshCount = Math.Max(
                        maximumNativeMeshCount,
                        nativeMeshCount);
                    minimumOwnedMeshCount = Math.Min(
                        minimumOwnedMeshCount,
                        ownedMeshCount);
                    maximumOwnedMeshCount = Math.Max(
                        maximumOwnedMeshCount,
                        ownedMeshCount);
                }
            }
            finally
            {
                if (boardObject != null)
                {
                    boardView?.ReleaseRuntimeRenderResources();
                    UnityEngine.Object.DestroyImmediate(boardObject);
                }
            }

            if (BoardView.RuntimeOwnedMeshCount !=
                baselineOwnedMeshCount)
            {
                throw new BuildFailedException(
                    $"Board render resource cleanup retained " +
                    $"{BoardView.RuntimeOwnedMeshCount - baselineOwnedMeshCount} " +
                    "owned meshes after the soak object was destroyed.");
            }

            Debug.Log(
                $"Board render resource lifetime passed " +
                $"{BoardRenderResourceThemeStageNumbers.Length} themes x " +
                $"{BoardRenderResourceSoakCycles} rebuild/reveal cycles: " +
                $"material cache {fullyWarmedMaterialCacheCount}, owned meshes " +
                $"{minimumOwnedMeshCount}-{maximumOwnedMeshCount}, native material max " +
                $"{maximumNativeMaterialCount}, native mesh max " +
                $"{maximumNativeMeshCount}; owned meshes returned to baseline.");
        }

        private static void ValidateRequiredAssets(
            StageGenerationConfig config,
            LevelSequence releaseSequence)
        {
            if (config == null)
            {
                throw new BuildFailedException($"Stage generation config is missing: {StageGenerationConfigPath}");
            }

            if (releaseSequence == null || !releaseSequence.IsVerifiedGeneratedSet)
            {
                throw new BuildFailedException(
                    $"A verified generated release sequence is required: {GeneratedSequencePath}");
            }

            if (releaseSequence.Count != config.GeneratedStageCount)
            {
                throw new BuildFailedException(
                    $"Release sequence/config mismatch: {releaseSequence.Count}/{config.GeneratedStageCount} stages.");
            }

            if (config.GeneratedStageCount >= MaximumValidatedStageNumber)
            {
                throw new BuildFailedException(
                    $"The procedural regression range must begin after the locked release set. Release count " +
                    $"{config.GeneratedStageCount} unexpectedly reaches {MaximumValidatedStageNumber}.");
            }
        }

        private static LevelSequence CreateRuntimeSequence(
            StageGenerationConfig config,
            LevelSequence releaseSequence,
            out long elapsedMilliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            var sequence = LevelSequence.CreateRuntimeGenerated(config, releaseSequence.StaticLevels);
            stopwatch.Stop();
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return sequence;
        }

        private static void ValidateSequenceSetup(
            LevelSequence sequence,
            LevelSequence releaseSequence,
            ReleaseSnapshot releaseSnapshot,
            long elapsedMilliseconds)
        {
            if (sequence == null || !sequence.UsesRuntimeGeneration || sequence.Count != int.MaxValue)
            {
                throw new BuildFailedException(
                    "Runtime sequence is not configured for unbounded procedural stage resolution.");
            }

            if (elapsedMilliseconds > MaximumSequenceSetupMilliseconds)
            {
                throw new BuildFailedException(
                    $"Runtime sequence setup took {elapsedMilliseconds} ms; limit is " +
                    $"{MaximumSequenceSetupMilliseconds} ms.");
            }

            ValidateReleaseLevelsRemainUntouched(sequence, releaseSequence, releaseSnapshot);
        }

        private static LevelData GenerateAndValidateProceduralStage(
            StageGenerationConfig config,
            int stageNumber,
            out long resolveMilliseconds,
            out long validationMilliseconds)
        {
            if (stageNumber <= config.GeneratedStageCount)
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber} is inside the locked release range and is not a runtime-generation sample.");
            }

            var stopwatch = Stopwatch.StartNew();
            var request = StageGenerationPlanner.CreateRequest(config, stageNumber);
            var built = StageCandidateBuilder.TryBuildRuntimeStageCandidate(
                config,
                request,
                out var level,
                out var builderReport,
                out var builderAnalysis,
                out var selectedCandidateIndex);
            stopwatch.Stop();
            resolveMilliseconds = stopwatch.ElapsedMilliseconds;

            if (!built || level == null)
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} exhausted every direct procedural candidate.");
            }

            if (resolveMilliseconds > MaximumStageResolveMilliseconds)
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} took {resolveMilliseconds} ms to resolve; " +
                    $"limit is {MaximumStageResolveMilliseconds} ms. Difficulty {request.Difficulty}, " +
                    $"target vehicles {request.Profile.TargetVehicleCount}, colors " +
                    $"{request.Profile.TargetColorCount}, layout {request.VehicleLayoutVariantIndex}, " +
                    $"candidate {selectedCandidateIndex + 1}.");
            }

            if (builderReport == null || builderReport.HasErrors ||
                !builderAnalysis.IsSolvable || builderAnalysis.SolutionCount <= 0 ||
                selectedCandidateIndex < 0)
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} builder returned an unverified candidate.");
            }

            if (level.GenerationSolutionCount != builderAnalysis.SolutionCount)
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} stored {level.GenerationSolutionCount} solutions, " +
                    $"but its production builder proved {builderAnalysis.SolutionCount}.");
            }

            ValidateProceduralSignature(config, request, level, selectedCandidateIndex);
            ValidateRequestContract(request, level);

            stopwatch.Restart();
            var validationReport = LevelValidator.Validate(level, false);
            if (validationReport.HasErrors)
            {
                throw new BuildFailedException(validationReport.ToConsoleMessage(level.LevelName));
            }

            if (!LevelGenerator.HasGreedyExitOrder(level.Buses))
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} has no greedy exit order for its visible parking layout.");
            }

            var solution = StageSolutionAnalyzer.Analyze(
                level.Buses,
                level.Garages,
                1,
                SolvabilityNodeVisitLimit);
            stopwatch.Stop();
            validationMilliseconds = stopwatch.ElapsedMilliseconds;

            if (!solution.IsSolvable || solution.SolutionCount != 1)
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} failed independent solution analysis.");
            }

            if (level.GenerationSolutionCount <= 0)
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} has invalid stored solution metadata: " +
                    $"stored {level.GenerationSolutionCount}, independently proven solvable.");
            }

            if (validationMilliseconds > MaximumIndependentValidationMilliseconds)
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} independent validation took " +
                    $"{validationMilliseconds} ms; limit is {MaximumIndependentValidationMilliseconds} ms.");
            }

            return level;
        }

        private static void ValidateProceduralSignature(
            StageGenerationConfig config,
            StageGenerationRequest request,
            LevelData level,
            int selectedCandidateIndex)
        {
            var signature = level.GenerationSignature;
            if (string.IsNullOrEmpty(signature) ||
                !StageGenerationSignature.TryGetInt(signature, "runtimeProcedural", out var proceduralFlag) ||
                proceduralFlag != 1)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} was not procedurally generated: {signature}");
            }

            if (StageGenerationSignature.TryGetInt(signature, "runtimeSafeCatalog", out var catalogFlag) &&
                catalogFlag != 0)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} silently used a release-catalog clone: {signature}");
            }

            if (StageGenerationSignature.TryGetInt(signature, "sourceStage", out var sourceStage))
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} exposes catalog sourceStage={sourceStage}; " +
                    "representative infinite-generation stages must be original procedural boards.");
            }

            if (!StageGenerationSignature.TryGetInt(signature, "stage", out var signatureStage) ||
                signatureStage != request.StageNumber ||
                !StageGenerationSignature.TryGetInt(signature, "seed", out var signatureSeed) ||
                signatureSeed != request.Seed ||
                !StageGenerationSignature.TryGetInt(signature, "candidate", out var candidateOffset) ||
                candidateOffset != selectedCandidateIndex ||
                candidateOffset < 0 ||
                candidateOffset >= config.RuntimeCandidateAttemptsPerStage)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} has invalid procedural metadata: {signature}");
            }
        }

        private static void ValidateRequestContract(StageGenerationRequest request, LevelData level)
        {
            var actualProfile = level.DifficultyProfile ??
                LevelDifficultyProfile.DefaultFor(LevelDifficulty.Normal);
            var requestedProfile = request.Profile ??
                LevelDifficultyProfile.DefaultFor(request.Difficulty);
            if (actualProfile.Difficulty != request.Difficulty ||
                actualProfile.TargetVehicleCount != requestedProfile.TargetVehicleCount ||
                actualProfile.TargetColorCount != requestedProfile.TargetColorCount)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} difficulty profile differs from its planner request.");
            }

            var actualVehicleCount = level.AllVehicles != null ? level.AllVehicles.Count : 0;
            var minimumVehicleCount = Mathf.CeilToInt(
                requestedProfile.TargetVehicleCount * MinimumActualVehicleTargetRatio);
            if (actualVehicleCount < minimumVehicleCount)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} has only {actualVehicleCount} vehicles; " +
                    $"planner target {requestedProfile.TargetVehicleCount}, required minimum {minimumVehicleCount}.");
            }

            if (level.RotaryUnitCapacity != request.RotaryCapacity ||
                level.RoadPresetId != request.RoadPresetId)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} road contract mismatch: requested " +
                    $"{request.RoadPresetId}/{request.RotaryCapacity}, got " +
                    $"{level.RoadPresetId}/{level.RotaryUnitCapacity}.");
            }

            var expectsGarages = request.GarageCount > 0 ||
                (request.Modifiers & StageModifierFlags.Garages) != 0;
            var actualGarageCount = level.Garages != null ? level.Garages.Count : 0;
            if ((actualGarageCount > 0) != expectsGarages || actualGarageCount != request.GarageCount)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} garage mismatch: requested " +
                    $"{request.GarageCount}, got {actualGarageCount}.");
            }

            for (var garageIndex = 0; garageIndex < actualGarageCount; garageIndex++)
            {
                var queuedVehicleCount = level.Garages[garageIndex].QueuedVehicles.Count;
                if (queuedVehicleCount < request.MinGarageQueuedVehicles ||
                    queuedVehicleCount > request.MaxGarageQueuedVehicles)
                {
                    throw new BuildFailedException(
                        $"Runtime stage {request.StageNumber:00000} garage {garageIndex + 1} queue " +
                        $"{queuedVehicleCount} is outside requested range " +
                        $"{request.MinGarageQueuedVehicles}-{request.MaxGarageQueuedVehicles}.");
                }
            }

            var expectsMystery = request.MysteryVehicleProfile.Enabled ||
                (request.Modifiers &
                    (StageModifierFlags.MysteryVehicles | StageModifierFlags.LightMysteryVehicles)) != 0;
            var hasMystery = HasMysteryVehicles(level);
            if (hasMystery != expectsMystery)
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} mystery modifier mismatch: " +
                    $"expected {expectsMystery}, got {hasMystery}.");
            }

            if (expectsMystery && !HasBlockedMysteryVehicle(level))
            {
                throw new BuildFailedException(
                    $"Runtime stage {request.StageNumber:00000} has only immediately revealed mystery vehicles.");
            }
        }

        private static void ValidatePlannerRange(StageGenerationConfig config)
        {
            var seeds = new HashSet<int>();
            for (var stageNumber = config.GeneratedStageCount + 1;
                 stageNumber <= MaximumValidatedStageNumber;
                 stageNumber++)
            {
                var request = StageGenerationPlanner.CreateRequest(config, stageNumber);
                var repeatedRequest = StageGenerationPlanner.CreateRequest(config, stageNumber);
                var signature = StageGenerationSignature.Create(config, request);
                var repeatedSignature = StageGenerationSignature.Create(config, repeatedRequest);
                if (!string.Equals(signature, repeatedSignature, StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        $"Stage planner is not deterministic at stage {stageNumber:00000}.");
                }

                if (request.StageNumber != stageNumber || request.Profile == null ||
                    request.Profile.TargetVehicleCount < 4 || request.Profile.TargetVehicleCount > 80 ||
                    request.Profile.TargetColorCount < 2 || request.Profile.TargetColorCount > 12 ||
                    request.RotaryCapacity < LevelData.MinRotaryUnitCapacity ||
                    request.RotaryCapacity > LevelData.MaxRotaryUnitCapacity ||
                    request.GarageCount < 0 ||
                    request.MinGarageQueuedVehicles < 1 ||
                    request.MaxGarageQueuedVehicles < request.MinGarageQueuedVehicles ||
                    request.MinSolutionCount < 1 ||
                    request.MaxSolutionCount < request.MinSolutionCount)
                {
                    throw new BuildFailedException(
                        $"Stage planner produced an invalid request at stage {stageNumber:00000}: {signature}");
                }

                if (!seeds.Add(request.Seed))
                {
                    throw new BuildFailedException(
                        $"Stage planner repeated seed {request.Seed} by stage {stageNumber:00000}; " +
                        "long-run procedural diversity cannot be guaranteed.");
                }
            }

            Debug.Log(
                $"Runtime planner sweep passed for every stage {config.GeneratedStageCount + 1}-" +
                $"{MaximumValidatedStageNumber}: deterministic requests and unique seeds.");
        }

        private static void ValidateLockedReleaseLayoutMapping(
            StageGenerationConfig config,
            LevelSequence releaseSequence)
        {
            for (var stageNumber = 1; stageNumber <= ExpectedLockedReleaseStageCount; stageNumber++)
            {
                var request = StageGenerationPlanner.CreateRequest(config, stageNumber);
                var level = releaseSequence.GetLevel(stageNumber - 1);
                var lockedPoolSize = -1;
                var lockedVariantIndex = -1;
                if (request.VehicleLayoutVariantPoolSize != ExpectedLockedReleaseLayoutVariantPoolSize ||
                    !StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "layoutPool",
                        out lockedPoolSize) ||
                    lockedPoolSize != ExpectedLockedReleaseLayoutVariantPoolSize ||
                    !StageGenerationSignature.TryGetInt(
                        level.GenerationSignature,
                        "layoutVariant",
                        out lockedVariantIndex) ||
                    lockedVariantIndex != request.VehicleLayoutVariantIndex)
                {
                    throw new BuildFailedException(
                        $"Locked release layout mapping changed at stage {stageNumber:000}. " +
                        $"Asset {lockedVariantIndex}/{lockedPoolSize}, planner " +
                        $"{request.VehicleLayoutVariantIndex}/{request.VehicleLayoutVariantPoolSize}.");
                }
            }

            Debug.Log(
                $"Locked release layout regression passed for stages 001-" +
                $"{ExpectedLockedReleaseStageCount:000}: all variants retain the shipped " +
                $"{ExpectedLockedReleaseLayoutVariantPoolSize}-entry mapping.");
        }

        private static void ValidateDifficultyProgressionContract(StageGenerationConfig config)
        {
            if (config.GeneratedStageCount != ExpectedLockedReleaseStageCount)
            {
                throw new BuildFailedException(
                    $"Difficulty regression expects the locked release boundary at stage " +
                    $"{ExpectedLockedReleaseStageCount}, got {config.GeneratedStageCount}.");
            }

            ValidatePost50DifficultyCurve(config);
            ValidateEndlessSchedule(config);
            ValidateLongRunPlannerExtremes(config);
        }

        private static void ValidatePost50DifficultyCurve(StageGenerationConfig config)
        {
            if (config.Post50RampStartStage != 50 || config.Post50RampMaxStage != 100)
            {
                throw new BuildFailedException(
                    $"Post-50 difficulty ramp must cover stages 50-100, got " +
                    $"{config.Post50RampStartStage}-{config.Post50RampMaxStage}.");
            }

            var previousPressure = config.GetPost50Pressure(config.Post50RampStartStage);
            if (!Mathf.Approximately(previousPressure, 0f) ||
                !Mathf.Approximately(config.GetPost50Pressure(config.Post50RampMaxStage), 1f) ||
                !Mathf.Approximately(config.GetPost50Pressure(config.Post50RampMaxStage + 1), 1f))
            {
                throw new BuildFailedException(
                    "Post-50 difficulty pressure does not preserve its 0-to-1 endpoints.");
            }

            for (var stageNumber = config.Post50RampStartStage + 1;
                 stageNumber <= config.Post50RampMaxStage;
                 stageNumber++)
            {
                var pressure = config.GetPost50Pressure(stageNumber);
                if (pressure <= previousPressure || pressure < 0f || pressure > 1f)
                {
                    throw new BuildFailedException(
                        $"Post-50 difficulty pressure is not strictly increasing at stage {stageNumber}: " +
                        $"{previousPressure:0.0000} -> {pressure:0.0000}.");
                }

                previousPressure = pressure;
            }
        }

        private static void ValidateEndlessSchedule(StageGenerationConfig config)
        {
            if (config.EndlessPatternLength != ExpectedEndlessDifficultyPatternLength ||
                config.EndlessIntensityPatternLength != ExpectedEndlessIntensityPatternLength)
            {
                throw new BuildFailedException(
                    $"Endless schedule must retain the 23x29 (667-stage) rhythm, got " +
                    $"{config.EndlessPatternLength}x{config.EndlessIntensityPatternLength}.");
            }

            var endlessStartStage = config.GeneratedStageCount + 1;
            var tierCounts = CreateTierCounts();
            for (var index = 0; index < ExpectedEndlessDifficultyPatternLength; index++)
            {
                var difficulty = config.GetDifficultyForStage(endlessStartStage + index);
                if (!tierCounts.ContainsKey(difficulty))
                {
                    throw new BuildFailedException(
                        $"Endless pattern contains unsupported difficulty {difficulty}.");
                }

                tierCounts[difficulty]++;
            }

            if (tierCounts[LevelDifficulty.Normal] != 9 ||
                tierCounts[LevelDifficulty.Hard] != 9 ||
                tierCounts[LevelDifficulty.SuperHard] != 5)
            {
                throw new BuildFailedException(
                    $"Endless 23-beat tier mix must be Normal/Hard/SuperHard 9/9/5, got " +
                    $"{tierCounts[LevelDifficulty.Normal]}/{tierCounts[LevelDifficulty.Hard]}/" +
                    $"{tierCounts[LevelDifficulty.SuperHard]}.");
            }

            var firstWindowPressure = config.GetEndlessMasteryPressure(endlessStartStage);
            var nextWindowPressure =
                config.GetEndlessMasteryPressure(endlessStartStage + EndlessCompositeWindowLength);
            var lateWindowPressure =
                config.GetEndlessMasteryPressure(endlessStartStage + LateMasteryWindowOffset);
            if (!Mathf.Approximately(firstWindowPressure, 0f) ||
                nextWindowPressure <= firstWindowPressure ||
                lateWindowPressure <= nextWindowPressure ||
                lateWindowPressure > 1f)
            {
                throw new BuildFailedException(
                    $"Endless mastery pressure must rise over long play: " +
                    $"{firstWindowPressure:0.0000} -> {nextWindowPressure:0.0000} -> " +
                    $"{lateWindowPressure:0.0000}.");
            }

            var earlyScores = CreateTierScoreLists();
            var lateScores = CreateTierScoreLists();
            var recoveryCounts = CreateTierCounts();
            for (var offset = 0; offset < EndlessCompositeWindowLength; offset++)
            {
                var earlyStage = endlessStartStage + offset;
                var lateStage = earlyStage + LateMasteryWindowOffset;
                var earlyRequest = StageGenerationPlanner.CreateRequest(config, earlyStage);
                var lateRequest = StageGenerationPlanner.CreateRequest(config, lateStage);
                ValidateEndlessRequestSafeBounds(config, earlyRequest);
                ValidateEndlessRequestSafeBounds(config, lateRequest);

                var earlyIntensity = config.GetEndlessIntensity(earlyStage);
                var lateIntensity = config.GetEndlessIntensity(lateStage);
                if (earlyRequest.Difficulty != lateRequest.Difficulty ||
                    earlyIntensity != lateIntensity)
                {
                    throw new BuildFailedException(
                        $"The paired 667-stage mastery windows lost their schedule alignment at " +
                        $"{earlyStage}/{lateStage}.");
                }

                var difficulty = earlyRequest.Difficulty;
                var earlyScore = GetCompositeDifficultyScore(earlyRequest);
                var lateScore = GetCompositeDifficultyScore(lateRequest);
                earlyScores[difficulty].Add(earlyScore);
                lateScores[difficulty].Add(lateScore);

                if (earlyIntensity == 0)
                {
                    recoveryCounts[difficulty]++;
                    if (!Mathf.Approximately(config.GetEndlessChallengeProgress(earlyStage), 0f) ||
                        !Mathf.Approximately(config.GetEndlessChallengeProgress(lateStage), 0f) ||
                        Mathf.Abs(lateScore - earlyScore) > DifficultyComparisonEpsilon)
                    {
                        throw new BuildFailedException(
                            $"Intensity-0 recovery beat became harder at stages {earlyStage}/{lateStage}.");
                    }
                }
            }

            var tiers = new[]
            {
                LevelDifficulty.Normal,
                LevelDifficulty.Hard,
                LevelDifficulty.SuperHard
            };
            for (var tierIndex = 0; tierIndex < tiers.Length; tierIndex++)
            {
                var difficulty = tiers[tierIndex];
                if (recoveryCounts[difficulty] <= 0)
                {
                    throw new BuildFailedException(
                        $"The 667-stage schedule has no intensity-0 recovery beat for {difficulty}.");
                }

                var earlyMean = GetMean(earlyScores[difficulty]);
                var lateMean = GetMean(lateScores[difficulty]);
                var earlyMedian = GetMedian(earlyScores[difficulty]);
                var lateMedian = GetMedian(lateScores[difficulty]);
                if (lateMean - earlyMean < MinimumCompositeDifficultyIncrease ||
                    lateMedian - earlyMedian < MinimumCompositeDifficultyIncrease)
                {
                    throw new BuildFailedException(
                        $"{difficulty} mastery progression is too flat across paired 667-stage windows: " +
                        $"mean {earlyMean:0.000}->{lateMean:0.000}, " +
                        $"median {earlyMedian:0.000}->{lateMedian:0.000}; required increase " +
                        $"{MinimumCompositeDifficultyIncrease:0.000}.");
                }
            }

            Debug.Log(
                "Difficulty progression regression passed: post-50 ramp 50-100, endless mix 9/9/5, " +
                "667-stage tier medians/means rise with mastery, and intensity-0 recovery beats stay light.");
        }

        private static void ValidateLongRunPlannerExtremes(StageGenerationConfig config)
        {
            var stages = new[] { 1000000, int.MaxValue };
            for (var index = 0; index < stages.Length; index++)
            {
                var stageNumber = stages[index];
                var request = StageGenerationPlanner.CreateRequest(config, stageNumber);
                var repeatedRequest = StageGenerationPlanner.CreateRequest(config, stageNumber);
                var signature = StageGenerationSignature.Create(config, request);
                var repeatedSignature = StageGenerationSignature.Create(config, repeatedRequest);
                if (!string.Equals(signature, repeatedSignature, StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        $"Long-run planner is not deterministic at stage {stageNumber}.");
                }

                ValidateEndlessRequestSafeBounds(config, request);
                if (request.StageNumber != stageNumber ||
                    config.GetEndlessMasteryPressure(stageNumber) + DifficultyComparisonEpsilon < 1f)
                {
                    throw new BuildFailedException(
                        $"Long-run planner produced invalid mastery metadata at stage {stageNumber}: " +
                        $"{signature}");
                }
            }
        }

        private static void ValidateEndlessRequestSafeBounds(
            StageGenerationConfig config,
            StageGenerationRequest request)
        {
            var profile = request.Profile;
            var knownModifiers =
                StageModifierFlags.Garages |
                StageModifierFlags.MysteryVehicles |
                StageModifierFlags.LightMysteryVehicles;
            if (request.StageNumber <= config.GeneratedStageCount ||
                profile == null ||
                profile.Difficulty != request.Difficulty ||
                profile.TargetVehicleCount < 4 ||
                profile.TargetVehicleCount > 50 ||
                profile.TargetColorCount < 2 ||
                profile.TargetColorCount > 12 ||
                !IsUnitInterval(profile.ParkingTension) ||
                !IsUnitInterval(profile.StationPressure) ||
                !IsUnitInterval(request.Progress) ||
                !IsUnitInterval(request.Post50Pressure) ||
                (request.Modifiers & ~knownModifiers) != 0 ||
                request.VehicleLayoutVariantPoolSize <= 0 ||
                request.VehicleLayoutVariantIndex < 0 ||
                request.VehicleLayoutVariantIndex >= request.VehicleLayoutVariantPoolSize ||
                request.GarageCount < 0 ||
                request.GarageCount > 5 ||
                request.MinGarageQueuedVehicles < 1 ||
                request.MaxGarageQueuedVehicles < request.MinGarageQueuedVehicles ||
                request.MaxGarageQueuedVehicles > 8 ||
                request.RotaryCapacity < LevelData.MinRotaryUnitCapacity ||
                request.RotaryCapacity > LevelData.MaxRotaryUnitCapacity ||
                request.MinSolutionCount < 1 ||
                request.MaxSolutionCount < request.MinSolutionCount ||
                request.MaxSolutionCount > config.SolutionCountLimit ||
                request.MysteryVehicleProfile.MinVehicles < 0 ||
                request.MysteryVehicleProfile.MaxVehicles < request.MysteryVehicleProfile.MinVehicles ||
                request.MysteryVehicleProfile.MaxVehicles > profile.TargetVehicleCount ||
                !IsUnitInterval(request.MysteryVehicleProfile.Ratio))
            {
                throw new BuildFailedException(
                    $"Endless planner request escaped safe bounds at stage {request.StageNumber}: " +
                    $"{StageGenerationSignature.Create(config, request)}");
            }
        }

        private static Dictionary<LevelDifficulty, int> CreateTierCounts()
        {
            return new Dictionary<LevelDifficulty, int>
            {
                { LevelDifficulty.Normal, 0 },
                { LevelDifficulty.Hard, 0 },
                { LevelDifficulty.SuperHard, 0 }
            };
        }

        private static Dictionary<LevelDifficulty, List<float>> CreateTierScoreLists()
        {
            return new Dictionary<LevelDifficulty, List<float>>
            {
                { LevelDifficulty.Normal, new List<float>() },
                { LevelDifficulty.Hard, new List<float>() },
                { LevelDifficulty.SuperHard, new List<float>() }
            };
        }

        private static float GetCompositeDifficultyScore(StageGenerationRequest request)
        {
            var profile = request.Profile;
            return profile.TargetVehicleCount +
                profile.TargetColorCount * 2f +
                profile.ParkingTension * 10f +
                profile.StationPressure * 10f;
        }

        private static float GetMean(List<float> values)
        {
            var sum = 0f;
            for (var index = 0; index < values.Count; index++)
            {
                sum += values[index];
            }

            return values.Count > 0 ? sum / values.Count : 0f;
        }

        private static float GetMedian(List<float> values)
        {
            if (values.Count == 0)
            {
                return 0f;
            }

            values.Sort();
            var midpoint = values.Count / 2;
            return values.Count % 2 != 0
                ? values[midpoint]
                : (values[midpoint - 1] + values[midpoint]) * 0.5f;
        }

        private static bool IsUnitInterval(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
        }

        private static void ValidateProductionSequenceResolution(
            LevelSequence runtimeSequence,
            StageGenerationConfig config,
            ReleaseSnapshot releaseSnapshot,
            IReadOnlyDictionary<int, StageRecord> expectedRecords,
            ICollection<LevelData> resolvedLevels)
        {
            var cacheSnapshot = CacheArtifactSnapshot.Capture();
            try
            {
                var productionStages = new[] { config.GeneratedStageCount + 7, MaximumValidatedStageNumber };
                for (var index = 0; index < productionStages.Length; index++)
                {
                    var stageNumber = productionStages[index];
                    if (!expectedRecords.TryGetValue(stageNumber, out var expected))
                    {
                        throw new BuildFailedException(
                            $"Production-path stage {stageNumber:00000} is missing its direct-generator baseline.");
                    }

                    var levelIndex = stageNumber - 1;
                    var stopwatch = Stopwatch.StartNew();
                    var level = runtimeSequence.GetLevel(levelIndex);
                    stopwatch.Stop();
                    if (level == null)
                    {
                        throw new BuildFailedException(
                            $"Production runtime sequence returned null for stage {stageNumber:00000}.");
                    }

                    resolvedLevels.Add(level);
                    if (stopwatch.ElapsedMilliseconds > MaximumStageResolveMilliseconds)
                    {
                        throw new BuildFailedException(
                            $"Production runtime sequence stage {stageNumber:00000} took " +
                            $"{stopwatch.ElapsedMilliseconds} ms; limit is {MaximumStageResolveMilliseconds} ms.");
                    }

                    if (!runtimeSequence.IsLevelCached(levelIndex) ||
                        !ReferenceEquals(level, runtimeSequence.GetLevel(levelIndex)))
                    {
                        throw new BuildFailedException(
                            $"Production runtime sequence did not retain stage {stageNumber:00000} in memory.");
                    }

                    var actual = StageRecord.Create(level);
                    if (!expected.Equals(actual))
                    {
                        throw new BuildFailedException(
                            $"Production runtime sequence stage {stageNumber:00000} differs from the direct " +
                            $"procedural builder. Expected {ShortHash(expected.StructuralFingerprint)}, " +
                            $"got {ShortHash(actual.StructuralFingerprint)} with signature " +
                            $"'{actual.GenerationSignature}'.");
                    }

                    RejectReleaseClone(stageNumber, actual, releaseSnapshot);
                    Debug.Log(
                        $"Production runtime sequence stage {stageNumber:00000} resolved procedural content " +
                        $"in {stopwatch.ElapsedMilliseconds} ms and matched the direct-generator baseline.");
                }
            }
            finally
            {
                cacheSnapshot.Restore();
            }
        }

        private static ReleaseSnapshot CaptureReleaseSnapshot(LevelSequence releaseSequence)
        {
            var structuralFingerprints = new List<string>(releaseSequence.StaticLevels.Count);
            var geometryOwners = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < releaseSequence.StaticLevels.Count; index++)
            {
                var level = releaseSequence.StaticLevels[index];
                if (level == null)
                {
                    throw new BuildFailedException($"Release level {index + 1:000} is null.");
                }

                structuralFingerprints.Add(CreateStructuralFingerprint(level));
                var geometryFingerprint = CreateGeometryFingerprint(level);
                if (!geometryOwners.ContainsKey(geometryFingerprint))
                {
                    geometryOwners.Add(geometryFingerprint, index + 1);
                }
            }

            return new ReleaseSnapshot(structuralFingerprints, geometryOwners);
        }

        private static void ValidateReleaseLevelsRemainUntouched(
            LevelSequence runtimeSequence,
            LevelSequence releaseSequence,
            ReleaseSnapshot releaseSnapshot)
        {
            for (var index = 0; index < releaseSequence.StaticLevels.Count; index++)
            {
                var expected = releaseSequence.StaticLevels[index];
                var actual = runtimeSequence.GetLevel(index);
                if (!ReferenceEquals(expected, actual))
                {
                    throw new BuildFailedException(
                        $"Locked release stage {index + 1:000} was replaced by runtime generation.");
                }

                var fingerprint = CreateStructuralFingerprint(actual);
                if (!string.Equals(
                        fingerprint,
                        releaseSnapshot.StructuralFingerprints[index],
                        StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        $"Locked release stage {index + 1:000} was mutated during runtime generation.");
                }
            }
        }

        private static void RejectReleaseClone(
            int stageNumber,
            StageRecord record,
            ReleaseSnapshot releaseSnapshot)
        {
            if (releaseSnapshot.GeometryOwners.TryGetValue(record.GeometryFingerprint, out var sourceStage))
            {
                throw new BuildFailedException(
                    $"Runtime stage {stageNumber:00000} is a geometry clone of locked release stage " +
                    $"{sourceStage:000}; procedural stages must build a new board.");
            }
        }

        private static void RejectGeneratedDuplicate(
            int stageNumber,
            StageRecord record,
            IDictionary<string, int> geometryOwners)
        {
            if (geometryOwners.TryGetValue(record.GeometryFingerprint, out var earlierStage))
            {
                throw new BuildFailedException(
                    $"Runtime stages {earlierStage:00000} and {stageNumber:00000} have identical board geometry; " +
                    "changing only metadata, colors, or passenger order is not sufficient diversity.");
            }

            geometryOwners.Add(record.GeometryFingerprint, stageNumber);
        }

        private static void ValidateCatalogFallbackInIsolation(
            StageGenerationConfig config,
            LevelSequence releaseSequence,
            ReleaseSnapshot releaseSnapshot)
        {
            LevelData fallbackLevel = null;
            try
            {
                var runtimeAssembly = typeof(LevelSequence).Assembly;
                var catalogType = runtimeAssembly.GetType("BusPuzzle.RuntimeSafeLevelCatalog", true);
                var createMethod = catalogType.GetMethod(
                    "Create",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var tryCreateMethod = catalogType.GetMethod(
                    "TryCreateLevel",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var countProperty = catalogType.GetProperty(
                    "Count",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (createMethod == null || tryCreateMethod == null || countProperty == null)
                {
                    throw new BuildFailedException(
                        "Runtime catalog fallback no longer exposes its isolated validation contract.");
                }

                var catalog = createMethod.Invoke(null, new object[] { releaseSequence.StaticLevels });
                var catalogCount = (int)countProperty.GetValue(catalog);
                if (catalogCount != releaseSequence.StaticLevels.Count)
                {
                    throw new BuildFailedException(
                        $"Runtime catalog fallback accepted {catalogCount}/{releaseSequence.StaticLevels.Count} " +
                        "locked release stages.");
                }

                var fallbackStageNumber = config.GeneratedStageCount + 7;
                var request = StageGenerationPlanner.CreateRequest(config, fallbackStageNumber);
                var arguments = new object[] { request, null, -1 };
                var created = (bool)tryCreateMethod.Invoke(catalog, arguments);
                fallbackLevel = arguments[1] as LevelData;
                var sourceLevelIndex = (int)arguments[2];
                if (!created || fallbackLevel == null || sourceLevelIndex < 0 ||
                    sourceLevelIndex >= releaseSequence.StaticLevels.Count)
                {
                    throw new BuildFailedException(
                        "Isolated runtime catalog fallback could not clone a verified release stage.");
                }

                if (!StageGenerationSignature.TryGetInt(
                        fallbackLevel.GenerationSignature,
                        "runtimeSafeCatalog",
                        out var catalogFlag) ||
                    catalogFlag != 1 ||
                    StageGenerationSignature.TryGetInt(
                        fallbackLevel.GenerationSignature,
                        "runtimeProcedural",
                        out _))
                {
                    throw new BuildFailedException(
                        $"Isolated fallback has ambiguous generation metadata: " +
                        fallbackLevel.GenerationSignature);
                }

                if (!StageGenerationSignature.TryGetInt(
                        fallbackLevel.GenerationSignature,
                        "mirrorX",
                        out var mirrorX) ||
                    (mirrorX != 0 && mirrorX != 1))
                {
                    throw new BuildFailedException(
                        $"Isolated fallback has invalid mirror metadata: " +
                        fallbackLevel.GenerationSignature);
                }

                var sourceGeometry = CreateGeometryFingerprint(
                    releaseSequence.StaticLevels[sourceLevelIndex]);
                var expectedFallbackGeometry =
                    CreateGeometryFingerprint(
                        releaseSequence.StaticLevels[sourceLevelIndex],
                        mirrorX == 1);
                var fallbackGeometry = CreateGeometryFingerprint(fallbackLevel);
                if (!releaseSnapshot.GeometryOwners.ContainsKey(sourceGeometry) ||
                    !string.Equals(
                        expectedFallbackGeometry,
                        fallbackGeometry,
                        StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        $"Isolated catalog fallback did not preserve its " +
                        $"declared source geometry with mirrorX={mirrorX}.");
                }

                var report = LevelValidator.Validate(fallbackLevel, false);
                var solution = StageSolutionAnalyzer.Analyze(
                    fallbackLevel.Buses,
                    fallbackLevel.Garages,
                    1,
                    SolvabilityNodeVisitLimit);
                if (report.HasErrors || !solution.IsSolvable)
                {
                    throw new BuildFailedException(
                        "Isolated runtime catalog fallback produced an invalid or unsolvable clone.");
                }

                Debug.Log(
                    $"Runtime catalog fallback passed in isolation: stage {fallbackStageNumber:000} -> " +
                    $"release source {sourceLevelIndex + 1:000}. It remains forbidden for normal regression samples.");
            }
            catch (TargetInvocationException exception)
            {
                throw new BuildFailedException(
                    $"Runtime catalog fallback validation failed: " +
                    (exception.InnerException != null ? exception.InnerException.Message : exception.Message));
            }
            finally
            {
                if (fallbackLevel != null)
                {
                    UnityEngine.Object.DestroyImmediate(fallbackLevel);
                }
            }
        }

        private static void ValidateGameplaySafeResolution(
            StageGenerationConfig config,
            LevelSequence releaseSequence)
        {
            ValidateGameplaySafePlannerCoverage(
                config,
                releaseSequence);

            var runtimeSequence = LevelSequence.CreateRuntimeGenerated(
                config,
                releaseSequence.StaticLevels);
            var preparedLevels = new List<LevelData>();
            var safeSourceStages = new HashSet<int>();
            var safeVariantsByDifficulty =
                new Dictionary<LevelDifficulty, HashSet<string>>();
            try
            {
                if (runtimeSequence.RuntimeSafeCatalogCount != releaseSequence.StaticLevels.Count)
                {
                    throw new BuildFailedException(
                        $"Gameplay-safe runtime sequence accepted " +
                        $"{runtimeSequence.RuntimeSafeCatalogCount}/" +
                        $"{releaseSequence.StaticLevels.Count} release stages.");
                }

                for (var index = 0; index < GameplaySafetyRegressionStages.Length; index++)
                {
                    var stageNumber = GameplaySafetyRegressionStages[index];
                    var levelIndex = stageNumber - 1;
                    var stopwatch = Stopwatch.StartNew();
                    var prepared = runtimeSequence.PrepareSafeGameplayLevel(
                        levelIndex,
                        "release safety validation");
                    stopwatch.Stop();

                    if (!prepared ||
                        !runtimeSequence.TryGetPreparedLevel(levelIndex, out var level) ||
                        level == null)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe runtime stage {stageNumber:00000} could not be prepared.");
                    }

                    preparedLevels.Add(level);
                    if (stopwatch.ElapsedMilliseconds > MaximumGameplaySafeResolveMilliseconds)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe runtime stage {stageNumber:00000} took " +
                            $"{stopwatch.ElapsedMilliseconds} ms; limit is " +
                            $"{MaximumGameplaySafeResolveMilliseconds} ms.");
                    }

                    var signature = level.GenerationSignature;
                    var usedCatalog =
                        StageGenerationSignature.TryGetInt(
                            signature,
                            "runtimeSafeCatalog",
                            out var catalogFlag) &&
                        catalogFlag == 1;
                    var usedEmergency =
                        StageGenerationSignature.TryGetInt(
                            signature,
                            "runtimeEmergency",
                            out var emergencyFlag) &&
                        emergencyFlag == 1;
                    if (!usedCatalog || usedEmergency)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe runtime stage {stageNumber:00000} did not use the " +
                            $"verified release catalog: {signature}");
                    }

                    if (!StageGenerationSignature.TryGetInt(
                            signature,
                            "sourceStage",
                            out var sourceStageNumber) ||
                        sourceStageNumber <= 0)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe runtime stage {stageNumber:00000} is missing " +
                            $"its verified source stage: {signature}");
                    }

                    safeSourceStages.Add(sourceStageNumber);
                    if (!StageGenerationSignature.TryGetInt(
                            signature,
                            "mirrorX",
                            out var mirrorX) ||
                        (mirrorX != 0 && mirrorX != 1))
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe runtime stage {stageNumber:00000} is missing " +
                            $"its deterministic mirror variant: {signature}");
                    }

                    var difficulty = level.DifficultyProfile.Difficulty;
                    if (!safeVariantsByDifficulty.TryGetValue(
                            difficulty,
                            out var difficultyVariants))
                    {
                        difficultyVariants = new HashSet<string>(
                            StringComparer.Ordinal);
                        safeVariantsByDifficulty.Add(
                            difficulty,
                            difficultyVariants);
                    }

                    difficultyVariants.Add(
                        $"{sourceStageNumber}:{mirrorX}");

                    if (StageGenerationSignature.TryGetInt(
                            signature,
                            "runtimeProcedural",
                            out _))
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe runtime stage {stageNumber:00000} entered the " +
                            $"foreground procedural generator: {signature}");
                    }

                    ValidateGameplaySafeContract(
                        config,
                        stageNumber,
                        level,
                        signature);

                    var report = LevelValidator.Validate(level, false);
                    var solution = StageSolutionAnalyzer.Analyze(
                        level.Buses,
                        level.Garages,
                        1,
                        SolvabilityNodeVisitLimit);
                    if (report.HasErrors || !solution.IsSolvable)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe runtime stage {stageNumber:00000} is invalid or unsolvable.");
                    }

                    Debug.Log(
                        $"Gameplay-safe runtime stage {stageNumber:00000} passed in " +
                        $"{stopwatch.ElapsedMilliseconds} ms using " +
                        $"{(usedCatalog ? "the release catalog" : "the emergency board")}.");
                }

                if (safeSourceStages.Count <
                    MinimumGameplaySafeDistinctSources)
                {
                    throw new BuildFailedException(
                        $"Gameplay-safe representative stages used only " +
                        $"{safeSourceStages.Count} distinct verified sources; minimum is " +
                        $"{MinimumGameplaySafeDistinctSources}.");
                }

                ValidateGameplaySafeVariantDiversity(
                    safeVariantsByDifficulty);
                ValidateEmergencyCommitSemantics(
                    runtimeSequence,
                    preparedLevels);
                if (runtimeSequence.RuntimePreparedLevelCount > 8)
                {
                    throw new BuildFailedException(
                        $"Gameplay-safe runtime cache retained " +
                        $"{runtimeSequence.RuntimePreparedLevelCount} levels; limit is 8.");
                }

                runtimeSequence.ReleaseRuntimeResources();
                if (runtimeSequence.RuntimePreparedLevelCount != 0)
                {
                    throw new BuildFailedException(
                        "Gameplay-safe runtime cache did not release all transient levels.");
                }

                ValidateRuntimeFallbackReleaseSemantics();
            }
            finally
            {
                DestroyRuntimeObjects(preparedLevels, runtimeSequence);
            }
        }

        private static void ValidateGameplaySafePlannerCoverage(
            StageGenerationConfig config,
            LevelSequence releaseSequence)
        {
            var runtimeSequence = LevelSequence.CreateRuntimeGenerated(
                config,
                releaseSequence.StaticLevels);
            var totalStopwatch = Stopwatch.StartNew();
            long maximumResolveMilliseconds = 0;
            try
            {
                for (var stageNumber = config.GeneratedStageCount + 1;
                    stageNumber <= MaximumValidatedStageNumber;
                    stageNumber++)
                {
                    var levelIndex = stageNumber - 1;
                    var resolveStopwatch = Stopwatch.StartNew();
                    var prepared = runtimeSequence.PrepareSafeGameplayLevel(
                        levelIndex,
                        "full planner coverage validation",
                        false);
                    resolveStopwatch.Stop();
                    maximumResolveMilliseconds = Math.Max(
                        maximumResolveMilliseconds,
                        resolveStopwatch.ElapsedMilliseconds);

                    if (!prepared ||
                        !runtimeSequence.TryGetPreparedLevel(
                            levelIndex,
                            out var level) ||
                        level == null)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe full sweep could not prepare stage " +
                            $"{stageNumber:00000}.");
                    }

                    var signature = level.GenerationSignature;
                    var usedVerifiedCatalog =
                        StageGenerationSignature.TryGetInt(
                            signature,
                            "runtimeSafeCatalog",
                            out var catalogFlag) &&
                        catalogFlag == 1;
                    var usedEmergency =
                        StageGenerationSignature.TryGetInt(
                            signature,
                            "runtimeEmergency",
                            out var emergencyFlag) &&
                        emergencyFlag == 1;
                    if (!usedVerifiedCatalog ||
                        usedEmergency ||
                        !StageGenerationSignature.TryGetInt(
                            signature,
                            "sourceStage",
                            out var sourceStageNumber) ||
                        sourceStageNumber <= 0)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe full sweep stage {stageNumber:00000} " +
                            $"did not resolve through a verified catalog source: " +
                            $"{signature}");
                    }

                    if (resolveStopwatch.ElapsedMilliseconds >
                        MaximumGameplaySafeResolveMilliseconds)
                    {
                        throw new BuildFailedException(
                            $"Gameplay-safe full sweep stage {stageNumber:00000} took " +
                            $"{resolveStopwatch.ElapsedMilliseconds} ms; limit is " +
                            $"{MaximumGameplaySafeResolveMilliseconds} ms.");
                    }

                    ValidateGameplaySafeContract(
                        config,
                        stageNumber,
                        level,
                        signature);
                }
            }
            finally
            {
                runtimeSequence.ReleaseRuntimeResources();
                UnityEngine.Object.DestroyImmediate(runtimeSequence);
            }

            totalStopwatch.Stop();
            Debug.Log(
                $"Gameplay-safe full sweep passed for stages " +
                $"{config.GeneratedStageCount + 1}-{MaximumValidatedStageNumber}: " +
                "every request used a verified catalog source without emergency or " +
                $"procedural generation; slowest resolve {maximumResolveMilliseconds} ms, " +
                $"total {totalStopwatch.ElapsedMilliseconds} ms.");
        }

        private static void ValidateGameplaySafeVariantDiversity(
            IReadOnlyDictionary<LevelDifficulty, HashSet<string>>
                variantsByDifficulty)
        {
            ValidateGameplaySafeVariantDiversity(
                variantsByDifficulty,
                LevelDifficulty.Normal,
                MinimumGameplaySafeNormalVariants);
            ValidateGameplaySafeVariantDiversity(
                variantsByDifficulty,
                LevelDifficulty.Hard,
                MinimumGameplaySafeHardVariants);
            ValidateGameplaySafeVariantDiversity(
                variantsByDifficulty,
                LevelDifficulty.SuperHard,
                MinimumGameplaySafeSuperHardVariants);
        }

        private static void ValidateGameplaySafeVariantDiversity(
            IReadOnlyDictionary<LevelDifficulty, HashSet<string>>
                variantsByDifficulty,
            LevelDifficulty difficulty,
            int minimumCount)
        {
            var actualCount =
                variantsByDifficulty.TryGetValue(
                    difficulty,
                    out var variants)
                    ? variants.Count
                    : 0;
            if (actualCount < minimumCount)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe {difficulty} representative stages used only " +
                    $"{actualCount} source/mirror variants; minimum is {minimumCount}.");
            }

            var mirrorVariants = new HashSet<int>();
            foreach (var variant in variants)
            {
                if (variant.EndsWith(
                        ":0",
                        StringComparison.Ordinal))
                {
                    mirrorVariants.Add(0);
                }
                else if (variant.EndsWith(
                    ":1",
                    StringComparison.Ordinal))
                {
                    mirrorVariants.Add(1);
                }
            }

            if (mirrorVariants.Count != 2)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe {difficulty} stages did not exercise both " +
                    "horizontal mirror orientations.");
            }

            Debug.Log(
                $"Gameplay-safe {difficulty} diversity passed with " +
                $"{actualCount} source/mirror variants across both orientations.");
        }

        private static void ValidateRuntimeFallbackReleaseSemantics()
        {
            LevelSequence fallbackSequence = null;
            try
            {
                fallbackSequence = LevelSequence.CreateRuntimeFallback();
                if (fallbackSequence == null ||
                    !fallbackSequence.IsTransientRuntimeSequence ||
                    fallbackSequence.Count != 3)
                {
                    throw new BuildFailedException(
                        "Runtime fallback did not create three transient owned levels.");
                }

                fallbackSequence.ReleaseRuntimeResources();
                if (fallbackSequence.Count != 0)
                {
                    throw new BuildFailedException(
                        "Runtime fallback did not release its owned transient levels.");
                }

                Debug.Log(
                    "Runtime fallback released all three owned transient levels.");
            }
            finally
            {
                if (fallbackSequence != null)
                {
                    fallbackSequence.ReleaseRuntimeResources();
                    UnityEngine.Object.DestroyImmediate(fallbackSequence);
                }
            }
        }

        private static void ValidateEmergencyCommitSemantics(
            LevelSequence runtimeSequence,
            ICollection<LevelData> preparedLevels)
        {
            const int stageNumber = 206;
            const int levelIndex = stageNumber - 1;
            if (!runtimeSequence.PrepareSafeGameplayLevel(
                    levelIndex,
                    "release atomic-commit validation") ||
                !runtimeSequence.TryGetPreparedLevel(levelIndex, out var originalLevel) ||
                originalLevel == null)
            {
                throw new BuildFailedException(
                    $"Stage {stageNumber:000} is missing before emergency commit validation.");
            }

            LevelData emergencyLevel = null;
            var committed = false;
            try
            {
                if (!runtimeSequence.TryCreateEmergencyRuntimeLevel(
                        levelIndex,
                        "release atomic-commit validation",
                        out emergencyLevel) ||
                    emergencyLevel == null)
                {
                    throw new BuildFailedException(
                        $"Stage {stageNumber:000} could not create an emergency candidate.");
                }

                if (!runtimeSequence.TryGetPreparedLevel(levelIndex, out var stillPrepared) ||
                    !ReferenceEquals(stillPrepared, originalLevel))
                {
                    throw new BuildFailedException(
                        $"Stage {stageNumber:000} replaced its cache before activation commit.");
                }

                var report = LevelValidator.Validate(emergencyLevel, false);
                var solution = StageSolutionAnalyzer.Analyze(
                    emergencyLevel.Buses,
                    emergencyLevel.Garages,
                    1,
                    SolvabilityNodeVisitLimit);
                if (report.HasErrors || !solution.IsSolvable)
                {
                    throw new BuildFailedException(
                        $"Stage {stageNumber:000} emergency candidate is invalid or unsolvable.");
                }

                if (!runtimeSequence.CommitPreparedRuntimeLevel(
                        levelIndex,
                        emergencyLevel) ||
                    !runtimeSequence.TryGetPreparedLevel(levelIndex, out var committedLevel) ||
                    !ReferenceEquals(committedLevel, emergencyLevel))
                {
                    throw new BuildFailedException(
                        $"Stage {stageNumber:000} emergency candidate did not commit atomically.");
                }

                committed = true;
                preparedLevels.Add(emergencyLevel);
                Debug.Log(
                    $"Gameplay-safe runtime stage {stageNumber:00000} emergency replacement " +
                    "preserved the original cache until explicit activation commit.");
            }
            finally
            {
                if (!committed && emergencyLevel != null)
                {
                    runtimeSequence.ReleaseTransientRuntimeLevel(emergencyLevel);
                }
            }
        }

        private static void ValidateGameplaySafeContract(
            StageGenerationConfig config,
            int stageNumber,
            LevelData level,
            string signature)
        {
            var request = StageGenerationPlanner.CreateRequest(
                config,
                stageNumber);
            if (level.DifficultyProfile.Difficulty != request.Difficulty)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} difficulty mismatch: " +
                    $"{level.DifficultyProfile.Difficulty}/{request.Difficulty}.");
            }

            var wantsGarages = request.GarageCount > 0 ||
                (request.Modifiers & StageModifierFlags.Garages) != 0;
            var garageCount = level.Garages != null ? level.Garages.Count : 0;
            if ((garageCount > 0) != wantsGarages ||
                garageCount != request.GarageCount)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} garage mismatch: " +
                    $"{garageCount}/{request.GarageCount}.");
            }

            for (var garageIndex = 0; garageIndex < garageCount; garageIndex++)
            {
                var queueCount = level.Garages[garageIndex].QueuedVehicleCount;
                if (queueCount < request.MinGarageQueuedVehicles ||
                    queueCount > request.MaxGarageQueuedVehicles)
                {
                    throw new BuildFailedException(
                        $"Gameplay-safe stage {stageNumber:00000} garage {garageIndex + 1} " +
                        $"queue {queueCount} is outside " +
                        $"{request.MinGarageQueuedVehicles}-{request.MaxGarageQueuedVehicles}.");
                }
            }

            var wantsMysteryVehicles = request.MysteryVehicleProfile.Enabled ||
                (request.Modifiers &
                    (StageModifierFlags.MysteryVehicles |
                        StageModifierFlags.LightMysteryVehicles)) != 0;
            if (HasMysteryVehicles(level) != wantsMysteryVehicles)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} mystery-vehicle contract mismatch.");
            }

            if (!StageGenerationSignature.TryGetInt(
                    signature,
                    "requestedVehicles",
                    out var requestedVehicleCount))
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} is missing its capped vehicle contract.");
            }

            var profile = level.DifficultyProfile;
            if (profile.TargetVehicleCount != requestedVehicleCount ||
                profile.TargetColorCount != request.Profile.TargetColorCount ||
                Mathf.Abs(
                    profile.ParkingTension -
                    request.Profile.ParkingTension) >
                    DifficultyComparisonEpsilon ||
                Mathf.Abs(
                    profile.StationPressure -
                    request.Profile.StationPressure) >
                    DifficultyComparisonEpsilon)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} profile contract mismatch.");
            }

            var actualVehicleCount =
                level.AllVehicles != null ? level.AllVehicles.Count : 0;
            if (actualVehicleCount <
                    Mathf.CeilToInt(
                        requestedVehicleCount *
                        MinimumActualVehicleTargetRatio) ||
                Mathf.Abs(actualVehicleCount - requestedVehicleCount) > 8)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} vehicle contract mismatch: " +
                    $"profile {level.DifficultyProfile.TargetVehicleCount}, " +
                    $"actual {actualVehicleCount}, request {requestedVehicleCount}.");
            }

            if (level.RoadPresetId != request.RoadPresetId ||
                !StageGenerationSignature.TryGetInt(
                    signature,
                    "requestedRoad",
                    out var requestedRoad) ||
                requestedRoad != (int)request.RoadPresetId)
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} road mismatch: " +
                    $"{level.RoadPresetId}/{request.RoadPresetId}.");
            }

            var effectiveRotaryCapacity =
                RotaryCapacityPolicy.Resolve(
                    level,
                    level.RoadPreset);
            if (level.RotaryUnitCapacity != request.RotaryCapacity ||
                effectiveRotaryCapacity != request.RotaryCapacity ||
                !RotaryCapacityPolicy.UsesExactRuntimeCapacity(level))
            {
                throw new BuildFailedException(
                    $"Gameplay-safe stage {stageNumber:00000} rotary mismatch: " +
                    $"stored {level.RotaryUnitCapacity}, effective " +
                    $"{effectiveRotaryCapacity}, requested " +
                    $"{request.RotaryCapacity}.");
            }
        }

        private static bool HasMysteryVehicles(LevelData level)
        {
            if (level == null)
            {
                return false;
            }

            for (var index = 0; index < level.Buses.Count; index++)
            {
                if (level.Buses[index].StartsConcealed)
                {
                    return true;
                }
            }

            for (var garageIndex = 0; garageIndex < level.Garages.Count; garageIndex++)
            {
                foreach (var vehicle in level.Garages[garageIndex].EnumerateVehicles())
                {
                    if (vehicle.StartsConcealed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasBlockedMysteryVehicle(LevelData level)
        {
            if (level == null || level.Buses == null || level.Buses.Count == 0)
            {
                return false;
            }

            for (var index = 0; index < level.Buses.Count; index++)
            {
                if (level.Buses[index].StartsConcealed &&
                    !LevelGenerator.IsVehiclePathClearForValidation(level.Buses, index))
                {
                    return true;
                }
            }

            return false;
        }

        private static string CreateStructuralFingerprint(LevelData level)
        {
            var builder = new StringBuilder(4096);
            builder.Append("road=").Append((int)level.RoadPresetId)
                .Append(";rotary=").Append(level.RotaryUnitCapacity)
                .Append(";presentation=").Append((int)level.PresentationMode);

            var profile = level.DifficultyProfile;
            builder.Append(";difficulty=").Append((int)profile.Difficulty)
                .Append(";targetVehicles=").Append(profile.TargetVehicleCount)
                .Append(";targetColors=").Append(profile.TargetColorCount)
                .Append(";parking=");
            AppendFloat(builder, profile.ParkingTension);
            builder.Append(";station=");
            AppendFloat(builder, profile.StationPressure);
            builder.Append(";route=").Append(profile.RequireSolutionRoute ? 1 : 0);

            builder.Append(";passengers=");
            for (var index = 0; index < level.PassengerUnits.Count; index++)
            {
                builder.Append((int)level.PassengerUnits[index]).Append(',');
            }

            AppendPassengerFlow(builder, level.PassengerFlowPlan);
            builder.Append(";buses=");
            for (var index = 0; index < level.Buses.Count; index++)
            {
                AppendBus(builder, level.Buses[index], true);
            }

            builder.Append(";garages=");
            for (var garageIndex = 0; garageIndex < level.Garages.Count; garageIndex++)
            {
                AppendGarage(builder, level.Garages[garageIndex], true);
            }

            return Hash(builder.ToString());
        }

        private static string CreateGeometryFingerprint(LevelData level)
        {
            return CreateGeometryFingerprint(level, false);
        }

        private static string CreateGeometryFingerprint(
            LevelData level,
            bool mirrorHorizontally)
        {
            var vehicleTokens = new List<string>(level.Buses.Count);
            for (var index = 0; index < level.Buses.Count; index++)
            {
                var token = new StringBuilder(96);
                AppendBus(
                    token,
                    mirrorHorizontally
                        ? MirrorBusForGeometryFingerprint(
                            level.Buses[index])
                        : level.Buses[index],
                    false);
                vehicleTokens.Add(token.ToString());
            }

            vehicleTokens.Sort(StringComparer.Ordinal);
            var garageTokens = new List<string>(level.Garages.Count);
            for (var garageIndex = 0; garageIndex < level.Garages.Count; garageIndex++)
            {
                var token = new StringBuilder(192);
                AppendGeometryGarage(
                    token,
                    level.Garages[garageIndex],
                    mirrorHorizontally);
                garageTokens.Add(token.ToString());
            }

            garageTokens.Sort(StringComparer.Ordinal);
            var builder = new StringBuilder(4096);
            builder.Append("vehicles=");
            for (var index = 0; index < vehicleTokens.Count; index++)
            {
                builder.Append(vehicleTokens[index]);
            }

            builder.Append(";garages=");
            for (var index = 0; index < garageTokens.Count; index++)
            {
                builder.Append(garageTokens[index]);
            }

            return Hash(builder.ToString());
        }

        private static void AppendGeometryGarage(
            StringBuilder builder,
            GarageDefinition garage,
            bool mirrorHorizontally)
        {
            if (!mirrorHorizontally)
            {
                AppendGarage(builder, garage, false);
                return;
            }

            var mirroredPosition =
                MirrorGridPositionForGeometryFingerprint(
                    garage.GridPosition);
            builder.Append('[')
                .Append(mirroredPosition.x)
                .Append(',')
                .Append(mirroredPosition.y)
                .Append(',')
                .Append((int)MirrorDirectionForGeometryFingerprint(
                    garage.ExitDirection))
                .Append('|');
            AppendBus(
                builder,
                MirrorBusForGeometryFingerprint(
                    garage.FrontVehicle),
                false);
            builder.Append('|');
            for (var index = 0;
                index < garage.QueuedVehicles.Count;
                index++)
            {
                AppendBus(
                    builder,
                    MirrorBusForGeometryFingerprint(
                        garage.QueuedVehicles[index]),
                    false);
            }

            builder.Append(']');
        }

        private static BusDefinition MirrorBusForGeometryFingerprint(
            BusDefinition source)
        {
            return new BusDefinition(
                source.Color,
                source.Size,
                MirrorDirectionForGeometryFingerprint(
                    source.Direction),
                MirrorGridPositionForGeometryFingerprint(
                    source.GridPosition),
                -source.AngleOffsetDegrees,
                new Vector2(
                    -source.PositionOffsetCells.x,
                    source.PositionOffsetCells.y),
                source.StartsConcealed);
        }

        private static Vector2Int
            MirrorGridPositionForGeometryFingerprint(
                Vector2Int position)
        {
            return new Vector2Int(
                ExpectedBoardGridColumns - 1 - position.x,
                position.y);
        }

        private static GridDirection
            MirrorDirectionForGeometryFingerprint(
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

        private static void AppendPassengerFlow(StringBuilder builder, PassengerFlowPlan plan)
        {
            if (plan == null)
            {
                builder.Append(";flow=null");
                return;
            }

            builder.Append(";flow=").Append(plan.Enabled ? 1 : 0)
                .Append(',').Append((int)plan.Mode)
                .Append(',').Append(plan.Seed)
                .Append(',').Append(plan.MinGroupUnits)
                .Append(',').Append(plan.MaxGroupUnits)
                .Append(',').Append(plan.AutoFillMissingCapacity)
                .Append(";groups=");
            for (var index = 0; index < plan.Groups.Count; index++)
            {
                builder.Append((int)plan.Groups[index].Color)
                    .Append(':').Append(plan.Groups[index].UnitCount).Append(',');
            }

            builder.Append(";solutionRoute=");
            for (var index = 0; index < plan.SolutionRoute.Count; index++)
            {
                var step = plan.SolutionRoute[index];
                builder.Append((int)step.Color)
                    .Append(':').Append((int)step.Size)
                    .Append(':').Append(step.OverrideUnitCount)
                    .Append(':').Append(step.PreferredGroupUnitCount).Append(',');
            }
        }

        private static void AppendGarage(StringBuilder builder, GarageDefinition garage, bool includeColor)
        {
            builder.Append('[').Append(garage.GridPosition.x).Append(',').Append(garage.GridPosition.y)
                .Append(',').Append((int)garage.ExitDirection).Append('|');
            AppendBus(builder, garage.FrontVehicle, includeColor);
            builder.Append('|');
            for (var index = 0; index < garage.QueuedVehicles.Count; index++)
            {
                AppendBus(builder, garage.QueuedVehicles[index], includeColor);
            }

            builder.Append(']');
        }

        private static void AppendBus(StringBuilder builder, BusDefinition bus, bool includeColor)
        {
            builder.Append('{');
            if (includeColor)
            {
                builder.Append((int)bus.Color).Append(',');
            }

            builder.Append((int)bus.Size).Append(',').Append((int)bus.Direction)
                .Append(',').Append(bus.GridPosition.x).Append(',').Append(bus.GridPosition.y).Append(',');
            AppendFloat(builder, bus.AngleOffsetDegrees);
            builder.Append(',');
            AppendFloat(builder, bus.PositionOffsetCells.x);
            builder.Append(',');
            AppendFloat(builder, bus.PositionOffsetCells.y);
            if (includeColor)
            {
                builder.Append(',').Append(bus.StartsConcealed ? 1 : 0);
            }

            builder.Append('}');
        }

        private static void AppendFloat(StringBuilder builder, float value)
        {
            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static string Hash(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
                var hash = sha256.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var index = 0; index < hash.Length; index++)
                {
                    builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static string ShortHash(string hash)
        {
            return string.IsNullOrEmpty(hash) || hash.Length <= 12 ? hash : hash.Substring(0, 12);
        }

        private static void DestroyRuntimeObjects(IReadOnlyList<LevelData> levels, LevelSequence sequence)
        {
            if (levels != null)
            {
                for (var index = 0; index < levels.Count; index++)
                {
                    if (levels[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(levels[index]);
                    }
                }
            }

            if (sequence != null)
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        private sealed class ReleaseSnapshot
        {
            public ReleaseSnapshot(
                IReadOnlyList<string> structuralFingerprints,
                IReadOnlyDictionary<string, int> geometryOwners)
            {
                StructuralFingerprints = structuralFingerprints;
                GeometryOwners = geometryOwners;
            }

            public IReadOnlyList<string> StructuralFingerprints { get; }
            public IReadOnlyDictionary<string, int> GeometryOwners { get; }
        }

        private sealed class CacheArtifactSnapshot
        {
            private const string CacheDirectoryName = "generated-stage-cache";

            private CacheArtifactSnapshot(
                string directoryPath,
                bool directoryExisted,
                IDictionary<string, byte[]> files)
            {
                DirectoryPath = directoryPath;
                DirectoryExisted = directoryExisted;
                Files = files;
            }

            private string DirectoryPath { get; }
            private bool DirectoryExisted { get; }
            private IDictionary<string, byte[]> Files { get; }

            public static CacheArtifactSnapshot Capture()
            {
                var directoryPath = Path.Combine(Application.persistentDataPath, CacheDirectoryName);
                var directoryExisted = Directory.Exists(directoryPath);
                var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                if (directoryExisted)
                {
                    var paths = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                    for (var index = 0; index < paths.Length; index++)
                    {
                        files.Add(paths[index], File.ReadAllBytes(paths[index]));
                    }
                }

                return new CacheArtifactSnapshot(directoryPath, directoryExisted, files);
            }

            public void Restore()
            {
                if (Directory.Exists(DirectoryPath))
                {
                    var currentPaths = Directory.GetFiles(DirectoryPath, "*", SearchOption.AllDirectories);
                    for (var index = 0; index < currentPaths.Length; index++)
                    {
                        if (!Files.ContainsKey(currentPaths[index]))
                        {
                            File.Delete(currentPaths[index]);
                        }
                    }
                }

                foreach (var pair in Files)
                {
                    var parentDirectory = Path.GetDirectoryName(pair.Key);
                    if (!string.IsNullOrEmpty(parentDirectory))
                    {
                        Directory.CreateDirectory(parentDirectory);
                    }

                    File.WriteAllBytes(pair.Key, pair.Value);
                }

                if (!DirectoryExisted && Directory.Exists(DirectoryPath) &&
                    Directory.GetFileSystemEntries(DirectoryPath).Length == 0)
                {
                    Directory.Delete(DirectoryPath);
                }
            }
        }

        private readonly struct StageRecord : IEquatable<StageRecord>
        {
            private StageRecord(
                string generationSignature,
                string structuralFingerprint,
                string geometryFingerprint)
            {
                GenerationSignature = generationSignature;
                StructuralFingerprint = structuralFingerprint;
                GeometryFingerprint = geometryFingerprint;
            }

            public string GenerationSignature { get; }
            public string StructuralFingerprint { get; }
            public string GeometryFingerprint { get; }

            public static StageRecord Create(LevelData level)
            {
                return new StageRecord(
                    level.GenerationSignature,
                    CreateStructuralFingerprint(level),
                    CreateGeometryFingerprint(level));
            }

            public bool Equals(StageRecord other)
            {
                return string.Equals(GenerationSignature, other.GenerationSignature, StringComparison.Ordinal) &&
                    string.Equals(StructuralFingerprint, other.StructuralFingerprint, StringComparison.Ordinal) &&
                    string.Equals(GeometryFingerprint, other.GeometryFingerprint, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is StageRecord other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = GenerationSignature != null ? GenerationSignature.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^
                        (StructuralFingerprint != null ? StructuralFingerprint.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^
                        (GeometryFingerprint != null ? GeometryFingerprint.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
#endif
