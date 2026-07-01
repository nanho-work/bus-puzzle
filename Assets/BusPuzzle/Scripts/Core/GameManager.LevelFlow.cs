using System.Collections;
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

        private void LoadLevel(int levelIndex)
        {
            StopNextLevelLoad();
            StopClearNextStagePreload();
            boardingFlowController.Reset();
            ResetVipTeleportState();
            ResetMixShuffleState();
            ResetDepartState();
            ResetClearRewardDoubleState();
            ResetTutorialState();
            isDailyChallengeMode = false;
            activeDailyChallengeStepIndex = 0;
            activeDailyChallengeDateKey = string.Empty;
            BackgroundMusicPlayer.PlayDefault();
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
            dailyChallengeReturnLevelIndex = currentLevelIndex;
            UserProgress.SaveLastStageIndex(currentLevelIndex, levelSequence.Count);
            currentLevel = levelSequence.GetLevel(currentLevelIndex);
            UpdateBannerAdState(false);
            var shouldValidateExitSequence = levelSequence == null ||
                !levelSequence.UsesRuntimeGeneration &&
                !levelSequence.IsVerifiedGeneratedSet;
            var validationReport = LevelValidator.Validate(
                currentLevel,
                shouldValidateExitSequence);
            if (validationReport.HasIssues)
            {
                var validationMessage = validationReport.ToConsoleMessage(currentLevel != null ? currentLevel.LevelName : "Missing Level");
                if (validationReport.HasErrors)
                {
                    Debug.LogError(validationMessage);
                }
                else
                {
                    Debug.LogWarning(validationMessage);
                }
            }

            gameState = GameState.Playing;
            ClearPendingFailureRecoveryState();

            boardView.BuildLevel(currentLevel, circulatingPassengerUnits, buses, currentLevelIndex + 1);
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

            CheckBlocked();
            StartTutorialIfNeeded();
            ScheduleDailyRewardPromptCheck();
        }

        private void RestartLevel()
        {
            if (isDailyChallengeMode && activeDailyChallengeStepIndex > 0)
            {
                LoadDailyChallengeLevel(activeDailyChallengeStepIndex);
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
                LoadLevel(nextLevelIndex);
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
            if (!hasNextLevel ||
                !levelSequence.UsesRuntimeGeneration ||
                levelSequence.IsLevelCached(nextLevelIndex))
            {
                uiController?.SetClearNextPreparing(false, hasNextLevel);
                return;
            }

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
            uiController?.SetClearNextPreparing(false, levelSequence != null && currentLevelIndex + 1 < levelSequence.Count);
        }

        private void StopNextLevelLoad()
        {
            if (nextLevelLoadRoutine != null)
            {
                StopCoroutine(nextLevelLoadRoutine);
                nextLevelLoadRoutine = null;
            }

            uiController?.HideStageTransitionLoading();
        }

        private IEnumerator LoadNextLevelRoutine(int nextLevelIndex)
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
                nextLevelLoadRoutine = null;
                uiController?.HideStageTransitionLoading();
                yield break;
            }

            if (!levelSequence.IsLevelCached(nextLevelIndex))
            {
                levelSequence.PreloadLevel(nextLevelIndex);
                yield return null;
            }

            nextLevelLoadRoutine = null;
            LoadLevel(nextLevelIndex);
        }

        private IEnumerator PreloadClearNextStageRoutine(int nextLevelIndex)
        {
            var startedAt = Time.unscaledTime;
            yield return null;
            yield return new WaitForSecondsRealtime(ClearNextStagePreloadSettleSeconds);

            if (gameState == GameState.Cleared &&
                !isDailyChallengeMode &&
                levelSequence != null &&
                levelSequence.UsesRuntimeGeneration &&
                nextLevelIndex == currentLevelIndex + 1 &&
                nextLevelIndex < levelSequence.Count &&
                !levelSequence.IsLevelCached(nextLevelIndex))
            {
                levelSequence.PreloadLevel(nextLevelIndex);
            }

            var remainingSettleSeconds = ClearNextStageMinimumPreparingSeconds - (Time.unscaledTime - startedAt);
            if (remainingSettleSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingSettleSeconds);
            }

            clearNextStagePreloadRoutine = null;
            if (gameState == GameState.Cleared &&
                !isDailyChallengeMode &&
                levelSequence != null &&
                nextLevelIndex == currentLevelIndex + 1 &&
                nextLevelIndex < levelSequence.Count)
            {
                uiController?.SetClearNextPreparing(false, true);
            }
        }
    }
}
