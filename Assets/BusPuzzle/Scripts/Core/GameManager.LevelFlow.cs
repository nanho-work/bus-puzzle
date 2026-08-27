using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed partial class GameManager
    {
        private LevelSequence ResolveLevelSequence()
        {
            var stageGenerationConfig = Resources.Load<StageGenerationConfig>(StageGenerationConfigResourcePath);
            var generatedSequence = Resources.Load<LevelSequence>(GeneratedLevelSequenceResourcePath);
            if (TryResolveVerifiedSequence(generatedSequence, stageGenerationConfig, "Generated level sequence", out var resolvedSequence))
            {
                return resolvedSequence;
            }

            if (stageGenerationConfig != null)
            {
                if (generatedSequence == null || generatedSequence.Count == 0)
                {
                    Debug.LogWarning("Using runtime generated stage sequence from StageGenerationConfig.");
                }
                else
                {
                    Debug.LogWarning("Generated level sequence is not verified. Using runtime generated stage sequence from StageGenerationConfig.");
                }

                return LevelSequence.CreateRuntimeGenerated(stageGenerationConfig);
            }

            var activeSequence = Resources.Load<LevelSequence>(ActiveLevelSequenceResourcePath);
            if (TryResolveVerifiedSequence(activeSequence, stageGenerationConfig, "Active level sequence", out resolvedSequence))
            {
                return resolvedSequence;
            }

            if (levelSequence != null && levelSequence.Count > 0)
            {
                Debug.LogWarning("Using inspector LevelSequence fallback. This sequence is not marked as a verified generated release set.");
                return levelSequence;
            }

            if (activeSequence != null && activeSequence.Count > 0)
            {
                Debug.LogWarning("Using active LevelSequence fallback. This sequence is not marked as a verified generated release set.");
                return activeSequence;
            }

            return null;
        }

        private static bool TryResolveVerifiedSequence(
            LevelSequence sequence,
            StageGenerationConfig config,
            string sourceName,
            out LevelSequence resolvedSequence)
        {
            resolvedSequence = null;
            if (sequence == null || sequence.Count <= 0 || !sequence.IsVerifiedGeneratedSet)
            {
                return false;
            }

            if (config == null)
            {
                resolvedSequence = sequence;
                return true;
            }

            if (sequence.Count != config.GeneratedStageCount)
            {
                Debug.LogWarning(
                    $"{sourceName} contains {sequence.Count} verified prebuilt stages, while StageGenerationConfig expects " +
                    $"{config.GeneratedStageCount}. Existing stages will be used first, and later stages will be generated at runtime.");
            }

            resolvedSequence = LevelSequence.CreateRuntimeGenerated(config, sequence.StaticLevels);
            return true;
        }

        private void ConfigureControllers()
        {
            inputController = new GameInputController(gameCamera);
            vehicleDispatchController = new VehicleDispatchController(
                boardView,
                uiController,
                buses,
                UpdateCounters,
                RevealReadyConcealedBuses,
                StartBoardingResolver,
                CheckBlocked,
                GetCurrentLevelName);
            boardingFlowController = new BoardingFlowController(
                this,
                boardView,
                buses,
                circulatingPassengerUnits,
                UpdateCounters,
                TryCompleteLevelIfReady,
                CheckBlocked,
                () => gameState == GameState.Playing);
        }

        private bool LoadLevel(int levelIndex)
        {
            if (levelSequence == null || levelSequence.Count <= 0)
            {
                Debug.LogError("Cannot load a stage because the level sequence is missing or empty.");
                return false;
            }

            var preparedLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
            if (!TryPrepareGameplayLevel(preparedLevelIndex, "foreground load", out var preparedLevel))
            {
                Debug.LogError($"Stage {preparedLevelIndex + 1:000} could not be prepared. Keeping the current saved stage unchanged.");
                return false;
            }

            LevelData transientEmergencyLevel = null;
            if (!TryValidatePreparedLevel(preparedLevel, out _) &&
                !TryCreateValidatedEmergencyLevel(
                    preparedLevelIndex,
                    "prepared-level validation failure",
                    out preparedLevel,
                    out transientEmergencyLevel))
            {
                Debug.LogError(
                    $"Stage {preparedLevelIndex + 1:000} failed validation even after emergency recovery.");
                return false;
            }

            var previousState = CaptureLevelLoadState();
            try
            {
                StopNextLevelLoad();
                StopClearNextStagePreload();
                StopRuntimeAheadPreload();
                boardingFlowController?.Reset();

                if (!TryBuildPreparedBoard(
                        preparedLevel,
                        preparedLevelIndex,
                        out var buildException))
                {
                    Debug.LogError(
                        $"Stage {preparedLevelIndex + 1:000} board activation failed: " +
                        $"{buildException}");
                    if (transientEmergencyLevel != null ||
                        !TryCreateValidatedEmergencyLevel(
                            preparedLevelIndex,
                            "board activation failure",
                            out preparedLevel,
                            out transientEmergencyLevel) ||
                        !TryBuildPreparedBoard(
                            preparedLevel,
                            preparedLevelIndex,
                            out buildException))
                    {
                        throw new System.InvalidOperationException(
                            $"Stage {preparedLevelIndex + 1:000} emergency board activation failed.",
                            buildException);
                    }
                }

                ResetVipTeleportState();
                ResetMixShuffleState();
                ResetDepartState();
                ResetClearRewardDoubleState();
                ResetTutorialState();
                isDailyChallengeMode = false;
                activeDailyChallengeStepIndex = 0;
                activeDailyChallengeDateKey = string.Empty;
                BackgroundMusicPlayer.PlayDefault();
                currentLevelIndex = preparedLevelIndex;
                dailyChallengeReturnLevelIndex = currentLevelIndex;
                currentLevel = preparedLevel;
                UpdateBannerAdState(false);
                gameState = GameState.Playing;
                ClearPendingFailureRecoveryState();

                RevealReadyConcealedBuses();
                ReframeBoardCamera(true);
                UpdateCounters();
                UpdateGoldUi();
                UpdateRewardedAdUi();

                uiController.SetLevel(currentLevelIndex + 1, levelSequence.Count);
                uiController.SetDailyChallengeReturnButtonState(false);
                uiController.ShowPlaying(currentLevel.LevelName);
                if (currentLevel.DifficultyProfile.Difficulty == LevelDifficulty.SuperHard)
                {
                    uiController.ShowSuperHardBanner();
                }

                if (transientEmergencyLevel != null)
                {
                    if (!levelSequence.CommitPreparedRuntimeLevel(
                            preparedLevelIndex,
                            transientEmergencyLevel))
                    {
                        throw new System.InvalidOperationException(
                            $"Stage {preparedLevelIndex + 1:000} emergency board could not be committed.");
                    }

                    transientEmergencyLevel = null;
                }

                levelSequence.PinActiveRuntimeLevel(
                    currentLevelIndex);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Stage {preparedLevelIndex + 1:000} activation was rolled back: {exception}");
                TryRestorePreviousBoard(previousState);
                if (transientEmergencyLevel != null)
                {
                    levelSequence.ReleaseTransientRuntimeLevel(transientEmergencyLevel);
                }

                return false;
            }

            SaveActivatedStageProgressSafely();
            CompleteLevelActivationSafely();
            return true;
        }

        private bool TryCreateValidatedEmergencyLevel(
            int levelIndex,
            string reason,
            out LevelData preparedLevel,
            out LevelData transientEmergencyLevel)
        {
            preparedLevel = null;
            transientEmergencyLevel = null;
            if (!levelSequence.TryCreateEmergencyRuntimeLevel(
                    levelIndex,
                    reason,
                    out var emergencyLevel))
            {
                return false;
            }

            if (!TryValidatePreparedLevel(emergencyLevel, out _))
            {
                levelSequence.ReleaseTransientRuntimeLevel(emergencyLevel);
                return false;
            }

            preparedLevel = emergencyLevel;
            transientEmergencyLevel = emergencyLevel;
            return true;
        }

        private void SaveActivatedStageProgressSafely()
        {
            try
            {
                UserProgress.SavePreparedStageIndex(currentLevelIndex, levelSequence.Count);
                UserProgress.SaveActivatedStageIndex(currentLevelIndex, levelSequence.Count);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    $"Stage {currentLevelIndex + 1:000} loaded, but progress could not be saved: " +
                    $"{exception.Message}");
            }
        }

        private LevelLoadState CaptureLevelLoadState()
        {
            return new LevelLoadState(
                currentLevel,
                currentLevelIndex,
                dailyChallengeReturnLevelIndex,
                gameState,
                isDailyChallengeMode,
                activeDailyChallengeStepIndex,
                activeDailyChallengeDateKey,
                currentClearGoldReward,
                clearRewardDoubled,
                isClearRewardDoubleAdInProgress);
        }

        private void CompleteLevelActivationSafely()
        {
            try
            {
                CheckBlocked();
                StartTutorialIfNeeded();
                ScheduleDailyRewardPromptCheck();
                ScheduleRuntimeAheadPreload(currentLevelIndex);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Stage {currentLevelIndex + 1:000} loaded, but post-load setup failed: " +
                    $"{exception}");
            }
        }

        private bool TryPrepareGameplayLevel(
            int levelIndex,
            string reason,
            out LevelData preparedLevel)
        {
            preparedLevel = null;
            try
            {
                if (levelSequence != null &&
                    levelSequence.UsesRuntimeGeneration &&
                    levelIndex != currentLevelIndex &&
                    !levelSequence.IsProcedurallyGeneratedLevelCached(levelIndex))
                {
                    var committed = levelSequence.TryFinalizeRuntimeLevelGeneration(
                        levelIndex,
                        currentLevelIndex,
                        out var finished,
                        out var diagnostic);
                    if (finished && !string.IsNullOrEmpty(diagnostic))
                    {
                        if (committed)
                        {
                            Debug.Log(diagnostic);
                        }
                        else
                        {
                            Debug.LogWarning(diagnostic);
                        }
                    }
                }

                if (levelSequence.TryGetPreparedLevel(levelIndex, out preparedLevel))
                {
                    return preparedLevel != null;
                }

                return levelSequence.PrepareSafeGameplayLevel(levelIndex, reason) &&
                    levelSequence.TryGetPreparedLevel(levelIndex, out preparedLevel) &&
                    preparedLevel != null;
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Stage {levelIndex + 1:000} preparation threw unexpectedly: {exception}");
                preparedLevel = null;
                return false;
            }
        }

        private bool TryValidatePreparedLevel(
            LevelData preparedLevel,
            out LevelValidationReport validationReport)
        {
            validationReport = null;
            try
            {
                var shouldValidateExitSequence = levelSequence == null ||
                    !levelSequence.UsesRuntimeGeneration &&
                    !levelSequence.IsVerifiedGeneratedSet;
                validationReport = LevelValidator.Validate(
                    preparedLevel,
                    shouldValidateExitSequence);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Prepared stage validation threw unexpectedly: {exception}");
                return false;
            }

            if (validationReport == null)
            {
                Debug.LogError("Prepared stage validation returned no report.");
                return false;
            }

            if (validationReport.HasIssues)
            {
                var validationMessage = validationReport.ToConsoleMessage(
                    preparedLevel != null ? preparedLevel.LevelName : "Missing Level");
                if (validationReport.HasErrors)
                {
                    Debug.LogError(validationMessage);
                }
                else
                {
                    Debug.LogWarning(validationMessage);
                }
            }

            return preparedLevel != null && !validationReport.HasErrors;
        }

        private bool TryBuildPreparedBoard(
            LevelData preparedLevel,
            int preparedLevelIndex,
            out System.Exception exception)
        {
            exception = null;
            try
            {
                boardView.BuildLevel(
                    preparedLevel,
                    circulatingPassengerUnits,
                    buses,
                    preparedLevelIndex + 1);
                return true;
            }
            catch (System.Exception buildException)
            {
                exception = buildException;
                return false;
            }
        }

        private bool TryRestorePreviousBoard(LevelLoadState previousState)
        {
            currentLevel = previousState.Level;
            currentLevelIndex = previousState.LevelIndex;
            dailyChallengeReturnLevelIndex = previousState.DailyChallengeReturnLevelIndex;
            gameState = previousState.GameState;
            isDailyChallengeMode = previousState.IsDailyChallengeMode;
            activeDailyChallengeStepIndex = previousState.ActiveDailyChallengeStepIndex;
            activeDailyChallengeDateKey = previousState.ActiveDailyChallengeDateKey;
            currentClearGoldReward = previousState.ClearGoldReward;
            clearRewardDoubled = previousState.ClearRewardDoubled;
            isClearRewardDoubleAdInProgress =
                previousState.IsClearRewardDoubleAdInProgress;
            levelSequence?.PinActiveRuntimeLevel(
                previousState.LevelIndex);

            if (previousState.Level == null)
            {
                return false;
            }

            try
            {
                boardView.BuildLevel(
                    previousState.Level,
                    circulatingPassengerUnits,
                    buses,
                    previousState.LevelIndex + 1);
                RevealReadyConcealedBuses();
                ReframeBoardCamera(true);
                UpdateCounters();
                UpdateGoldUi();
                UpdateRewardedAdUi();
                UpdateBannerAdState(false);

                if (previousState.IsDailyChallengeMode)
                {
                    BackgroundMusicPlayer.PlayDailyChallengeEvent();
                    uiController.SetDailyChallengeLevel(
                        previousState.ActiveDailyChallengeStepIndex);
                    uiController.SetDailyChallengeReturnButtonState(true);
                    if (previousState.GameState == GameState.Cleared)
                    {
                        uiController.ShowDailyChallengePrompt(
                            DailyChallengeService.GetTodaySteps());
                    }
                    else
                    {
                        uiController.ShowPlaying(
                            previousState.Level.LevelName);
                    }
                }
                else if (previousState.GameState == GameState.Cleared)
                {
                    BackgroundMusicPlayer.PlayDefault();
                    RestoreClearUiAfterStageLoadFailure(
                        previousState.LevelIndex + 1);
                }
                else if (previousState.GameState == GameState.Failed)
                {
                    BackgroundMusicPlayer.PlayDefault();
                    uiController.ShowFailed(
                        boardView.CanUnlockStationSlot &&
                            HasStationUnlockRecoveryOption(),
                        HasVipTeleportTarget() && HasVipTeleportRecoveryOption(),
                        HasMixShuffleTarget() && HasMixShuffleRecoveryOption(),
                        HasPotentialDepartTarget() && HasDepartRecoveryOption());
                }
                else
                {
                    BackgroundMusicPlayer.PlayDefault();
                    uiController.SetLevel(
                        previousState.LevelIndex + 1,
                        levelSequence.Count);
                    uiController.SetDailyChallengeReturnButtonState(false);
                    uiController.ShowPlaying(previousState.Level.LevelName);
                }

                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Previous stage board restoration failed: {exception}");
                gameState = GameState.Failed;
                return false;
            }
        }

        private readonly struct LevelLoadState
        {
            public LevelLoadState(
                LevelData level,
                int levelIndex,
                int dailyChallengeReturnLevelIndex,
                GameState gameState,
                bool isDailyChallengeMode,
                int activeDailyChallengeStepIndex,
                string activeDailyChallengeDateKey,
                int clearGoldReward,
                bool clearRewardDoubled,
                bool isClearRewardDoubleAdInProgress)
            {
                Level = level;
                LevelIndex = levelIndex;
                DailyChallengeReturnLevelIndex = dailyChallengeReturnLevelIndex;
                GameState = gameState;
                IsDailyChallengeMode = isDailyChallengeMode;
                ActiveDailyChallengeStepIndex = activeDailyChallengeStepIndex;
                ActiveDailyChallengeDateKey = activeDailyChallengeDateKey;
                ClearGoldReward = clearGoldReward;
                ClearRewardDoubled = clearRewardDoubled;
                IsClearRewardDoubleAdInProgress =
                    isClearRewardDoubleAdInProgress;
            }

            public LevelData Level { get; }
            public int LevelIndex { get; }
            public int DailyChallengeReturnLevelIndex { get; }
            public GameState GameState { get; }
            public bool IsDailyChallengeMode { get; }
            public int ActiveDailyChallengeStepIndex { get; }
            public string ActiveDailyChallengeDateKey { get; }
            public int ClearGoldReward { get; }
            public bool ClearRewardDoubled { get; }
            public bool IsClearRewardDoubleAdInProgress { get; }
        }

        private IEnumerator LoadInitialLevelRoutine(int desiredLevelIndex)
        {
            var loaded = false;
            try
            {
                uiController?.ShowStageTransitionLoading();
                yield return null;

                loaded = TryLoadLevelWithoutThrow(desiredLevelIndex);
                if (!loaded && levelSequence != null)
                {
                    var lastActivatedLevelIndex =
                        UserProgress.GetLastActivatedStageIndex(
                            levelSequence.Count);
                    if (lastActivatedLevelIndex >= 0 &&
                        lastActivatedLevelIndex != desiredLevelIndex)
                    {
                        Debug.LogError(
                            $"Stage {desiredLevelIndex + 1:000} could not be restored. " +
                            $"Trying the last activated stage " +
                            $"{lastActivatedLevelIndex + 1:000}.");
                        loaded = TryLoadLevelWithoutThrow(
                            lastActivatedLevelIndex);
                    }
                }

                if (!loaded &&
                    levelSequence != null &&
                    levelSequence.StaticLevels != null &&
                    levelSequence.StaticLevels.Count > 0)
                {
                    var staticRecoveryIndex = Mathf.Clamp(
                        levelSequence.StaticLevels.Count - 1,
                        0,
                        levelSequence.Count - 1);
                    Debug.LogError(
                        $"Stage {desiredLevelIndex + 1:000} could not be restored. " +
                        $"Showing prebuilt stage {staticRecoveryIndex + 1:000} without lowering saved progress.");
                    loaded = TryLoadLevelWithoutThrow(staticRecoveryIndex);
                }
            }
            finally
            {
                initialLevelLoadRoutine = null;
                if (!isShuttingDown)
                {
                    uiController?.HideStageTransitionLoading();
                    if (!loaded)
                    {
                        gameState = GameState.Failed;
                        uiController?.ShowInvalid(Localization.Text("stage_failed"));
                    }
                }
            }
        }

        private bool TryLoadLevelWithoutThrow(int levelIndex)
        {
            try
            {
                return LoadLevel(levelIndex);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Stage {levelIndex + 1:000} load threw unexpectedly: {exception}");
                return false;
            }
        }

        private void StopInitialLevelLoad()
        {
            if (initialLevelLoadRoutine == null)
            {
                return;
            }

            StopCoroutine(initialLevelLoadRoutine);
            initialLevelLoadRoutine = null;
            if (!isShuttingDown)
            {
                uiController?.HideStageTransitionLoading();
            }
        }

        private void RestartLevel()
        {
            if (isDailyChallengeMode && activeDailyChallengeStepIndex > 0)
            {
                if (!LoadDailyChallengeLevel(
                        activeDailyChallengeStepIndex))
                {
                    uiController?.SetDailyChallengeReturnButtonState(true);
                    uiController?.ShowInvalid(
                        Localization.Text("stage_failed"));
                }

                return;
            }

            LoadLevel(currentLevelIndex);
        }

        private void LoadNextLevel()
        {
            if (gameState != GameState.Cleared || currentLevelIndex + 1 >= levelSequence.Count)
            {
                return;
            }

            var nextLevelIndex = currentLevelIndex + 1;
            if (levelSequence.IsLevelCached(nextLevelIndex))
            {
                if (!TryLoadLevelWithoutThrow(nextLevelIndex))
                {
                    RestoreClearUiAfterStageLoadFailure(nextLevelIndex);
                }

                return;
            }

            if (nextLevelLoadRoutine == null)
            {
                nextLevelLoadRoutine = StartCoroutine(LoadNextLevelRoutine(nextLevelIndex));
            }
        }

        private void ScheduleClearNextStagePreload(int nextLevelIndex)
        {
            StopClearNextStagePreload();
            var hasNextLevel = levelSequence != null &&
                nextLevelIndex >= 0 &&
                nextLevelIndex < levelSequence.Count;
            if (!hasNextLevel)
            {
                uiController?.SetClearNextPreparing(false, false);
                return;
            }

            if (levelSequence.IsLevelCached(nextLevelIndex))
            {
                CommitPreparedStageProgress(nextLevelIndex);
                uiController?.SetClearNextPreparing(false, true);
                return;
            }

            if (!levelSequence.UsesRuntimeGeneration)
            {
                uiController?.SetClearNextPreparing(false, false);
                return;
            }

            // Let the clear overlay render before the bounded safe-catalog copy begins.
            // Gold claims are already idempotent by stage number, while stage progress is
            // committed only after the prepared level has passed validation below.
            uiController?.SetClearNextPreparing(true, true);
            clearNextStagePreloadRoutine = StartCoroutine(PreloadClearNextStageRoutine(nextLevelIndex));
        }

        private void StopClearNextStagePreload()
        {
            if (clearNextStagePreloadRoutine == null)
            {
                return;
            }

            StopCoroutine(clearNextStagePreloadRoutine);
            clearNextStagePreloadRoutine = null;
            if (!isShuttingDown)
            {
                uiController?.SetClearNextPreparing(
                    false,
                    levelSequence != null &&
                        currentLevelIndex + 1 < levelSequence.Count);
            }
        }

        private void StopNextLevelLoad()
        {
            if (nextLevelLoadRoutine != null)
            {
                StopCoroutine(nextLevelLoadRoutine);
                nextLevelLoadRoutine = null;
            }

            if (!isShuttingDown)
            {
                uiController?.HideStageTransitionLoading();
            }
        }

        private void ScheduleRuntimeAheadPreload(int loadedLevelIndex)
        {
            StopRuntimeAheadPreload();
            if (levelSequence == null ||
                !levelSequence.UsesRuntimeGeneration ||
                levelSequence.RuntimePreloadAheadCount <= 0)
            {
                levelSequence?.CancelRuntimeLevelGenerationsOutsideRange(
                    1,
                    0);
                return;
            }

            levelSequence.CancelRuntimeLevelGenerationsOutsideRange(
                loadedLevelIndex + 1,
                loadedLevelIndex +
                    levelSequence.RuntimePreloadAheadCount);
            runtimeAheadPreloadRoutine = StartCoroutine(
                PreloadRuntimeAheadRoutine(
                    loadedLevelIndex,
                    levelSequence.RuntimePreloadAheadCount));
        }

        private void StopRuntimeAheadPreload()
        {
            if (runtimeAheadPreloadRoutine == null)
            {
                return;
            }

            StopCoroutine(runtimeAheadPreloadRoutine);
            runtimeAheadPreloadRoutine = null;
        }

        private IEnumerator PreloadRuntimeAheadRoutine(
            int loadedLevelIndex,
            int preloadAheadCount)
        {
            try
            {
                yield return null;
                var preloadIndices = new List<int>(preloadAheadCount);
                for (var offset = 1; offset <= preloadAheadCount; offset++)
                {
                    if (levelSequence == null ||
                        currentLevelIndex != loadedLevelIndex ||
                        isDailyChallengeMode)
                    {
                        yield break;
                    }

                    var preloadLevelIndex = loadedLevelIndex + offset;
                    if (preloadLevelIndex < 0 ||
                        preloadLevelIndex >= levelSequence.Count)
                    {
                        yield break;
                    }

                    if (!levelSequence.IsProcedurallyGeneratedLevelCached(preloadLevelIndex))
                    {
                        levelSequence.StartRuntimeLevelGeneration(preloadLevelIndex);
                    }

                    preloadIndices.Add(preloadLevelIndex);
                    yield return null;
                }

                while (levelSequence != null &&
                    currentLevelIndex == loadedLevelIndex &&
                    !isDailyChallengeMode)
                {
                    var hasPendingWork = false;
                    for (var index = 0; index < preloadIndices.Count; index++)
                    {
                        var preloadLevelIndex = preloadIndices[index];
                        if (levelSequence.IsProcedurallyGeneratedLevelCached(preloadLevelIndex))
                        {
                            continue;
                        }

                        var committed = levelSequence.TryFinalizeRuntimeLevelGeneration(
                            preloadLevelIndex,
                            currentLevelIndex,
                            out var finished,
                            out var diagnostic);
                        if (finished && !string.IsNullOrEmpty(diagnostic))
                        {
                            if (committed)
                            {
                                Debug.Log(diagnostic);
                            }
                            else
                            {
                                Debug.LogWarning(diagnostic);
                            }
                        }

                        if (finished)
                        {
                            // Materialize and validate at most one completed
                            // payload per frame on the Unity main thread.
                            hasPendingWork = true;
                            break;
                        }

                        if (!finished &&
                            levelSequence.IsRuntimeLevelGenerationPending(preloadLevelIndex))
                        {
                            hasPendingWork = true;
                        }
                    }

                    if (!hasPendingWork)
                    {
                        yield break;
                    }

                    yield return null;
                }
            }
            finally
            {
                runtimeAheadPreloadRoutine = null;
            }
        }

        private IEnumerator LoadNextLevelRoutine(int nextLevelIndex)
        {
            var loaded = false;
            try
            {
                StopClearNextStagePreload();
                uiController?.ShowStageTransitionLoading();
                yield return null;
                yield return new WaitForSecondsRealtime(StageTransitionLoadingSettleSeconds);

                if (gameState != GameState.Cleared ||
                    levelSequence == null ||
                    nextLevelIndex != currentLevelIndex + 1 ||
                    nextLevelIndex >= levelSequence.Count)
                {
                    yield break;
                }

                if (!levelSequence.IsLevelCached(nextLevelIndex) &&
                    !TryPrepareGameplayLevel(
                        nextLevelIndex,
                        "next-stage transition",
                        out _))
                {
                    Debug.LogError(
                        $"Stage {nextLevelIndex + 1:000} could not be prepared for loading.");
                    yield break;
                }

                yield return null;
                nextLevelLoadRoutine = null;
                loaded = TryLoadLevelWithoutThrow(nextLevelIndex);
            }
            finally
            {
                nextLevelLoadRoutine = null;
                if (!isShuttingDown)
                {
                    uiController?.HideStageTransitionLoading();
                    if (!loaded)
                    {
                        RestoreClearUiAfterStageLoadFailure(nextLevelIndex);
                    }
                }
            }
        }

        private IEnumerator PreloadClearNextStageRoutine(int nextLevelIndex)
        {
            var startedAt = Time.unscaledTime;
            var prepared = false;
            try
            {
                if (levelSequence != null &&
                    levelSequence.UsesRuntimeGeneration &&
                    !levelSequence.IsProcedurallyGeneratedLevelCached(nextLevelIndex))
                {
                    levelSequence.StartRuntimeLevelGeneration(nextLevelIndex);
                }

                yield return null;
                yield return new WaitForSecondsRealtime(ClearNextStagePreloadSettleSeconds);

                if (gameState == GameState.Cleared &&
                    !isDailyChallengeMode &&
                    levelSequence != null &&
                    levelSequence.UsesRuntimeGeneration &&
                    nextLevelIndex == currentLevelIndex + 1 &&
                    nextLevelIndex < levelSequence.Count)
                {
                    var generationDeadline = startedAt + RuntimeGenerationClearWaitSeconds;
                    while (!levelSequence.IsProcedurallyGeneratedLevelCached(nextLevelIndex) &&
                        Time.unscaledTime < generationDeadline)
                    {
                        var committed = levelSequence.TryFinalizeRuntimeLevelGeneration(
                            nextLevelIndex,
                            currentLevelIndex,
                            out var finished,
                            out var diagnostic);
                        if (finished)
                        {
                            if (!string.IsNullOrEmpty(diagnostic))
                            {
                                if (committed)
                                {
                                    Debug.Log(diagnostic);
                                }
                                else
                                {
                                    Debug.LogWarning(diagnostic);
                                }
                            }

                            break;
                        }

                        if (!levelSequence.IsRuntimeLevelGenerationPending(nextLevelIndex))
                        {
                            break;
                        }

                        yield return null;
                    }

                    if (!levelSequence.IsLevelCached(nextLevelIndex))
                    {
                        levelSequence.CancelRuntimeLevelGeneration(
                            nextLevelIndex);
                        TryPrepareGameplayLevel(
                            nextLevelIndex,
                            "clear-screen generation timeout fallback",
                            out _);
                    }

                    prepared =
                        levelSequence.IsLevelCached(nextLevelIndex) &&
                        CommitPreparedStageProgress(nextLevelIndex);
                }

                var remainingSettleSeconds =
                    ClearNextStageMinimumPreparingSeconds -
                    (Time.unscaledTime - startedAt);
                if (remainingSettleSeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(remainingSettleSeconds);
                }
            }
            finally
            {
                clearNextStagePreloadRoutine = null;
                if (!isShuttingDown &&
                    gameState == GameState.Cleared &&
                    !isDailyChallengeMode &&
                    levelSequence != null &&
                    nextLevelIndex == currentLevelIndex + 1 &&
                    nextLevelIndex < levelSequence.Count)
                {
                    uiController?.SetClearNextPreparing(
                        false,
                        prepared || levelSequence.IsLevelCached(nextLevelIndex));
                }
            }
        }

        private void RestoreClearUiAfterStageLoadFailure(int expectedNextLevelIndex)
        {
            if (isShuttingDown ||
                gameState != GameState.Cleared ||
                levelSequence == null ||
                expectedNextLevelIndex != currentLevelIndex + 1)
            {
                return;
            }

            var hasNextLevel = expectedNextLevelIndex < levelSequence.Count;
            uiController?.ShowClear(
                currentLevelIndex + 1,
                hasNextLevel,
                currentClearGoldReward);
            uiController?.SetClearNextPreparing(false, hasNextLevel);
            UpdateRewardedAdUi();
            UpdateClearRewardDoubleUi();
        }

        private bool CommitPreparedStageProgress(int levelIndex)
        {
            if (levelSequence == null ||
                levelIndex < 0 ||
                levelIndex >= levelSequence.Count ||
                !levelSequence.TryGetPreparedLevel(levelIndex, out var preparedLevel) ||
                preparedLevel == null)
            {
                return false;
            }

            try
            {
                var validationReport = LevelValidator.Validate(preparedLevel, false);
                if (validationReport == null || validationReport.HasErrors)
                {
                    Debug.LogError(
                        $"Stage {levelIndex + 1:000} preload was not committed because the prepared level is invalid.");
                    return false;
                }

                return UserProgress.SavePreparedStageIndex(
                    levelIndex,
                    levelSequence.Count);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Stage {levelIndex + 1:000} preload commit failed: {exception}");
                return false;
            }
        }
    }
}
