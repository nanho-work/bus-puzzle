using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public sealed partial class GameManager
    {
        private bool LoadDailyChallengeLevel(int stepIndex)
        {
            LevelData challengeLevel;
            try
            {
                challengeLevel =
                    DailyChallengeService.CreateRuntimeLevel(stepIndex);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Daily challenge step {stepIndex} creation failed: " +
                    $"{exception}");
                return false;
            }

            if (challengeLevel == null)
            {
                Debug.LogError(
                    $"Daily challenge step {stepIndex} could not be created.");
                return false;
            }

            LevelValidationReport validationReport;
            try
            {
                validationReport =
                    LevelValidator.Validate(challengeLevel, false);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Daily challenge step {stepIndex} validation failed: " +
                    $"{exception}");
                return false;
            }

            if (validationReport == null)
            {
                Debug.LogError(
                    $"Daily challenge step {stepIndex} validation returned no report.");
                return false;
            }

            if (validationReport.HasIssues)
            {
                var validationMessage =
                    validationReport.ToConsoleMessage(
                        challengeLevel.LevelName);
                if (validationReport.HasErrors)
                {
                    Debug.LogError(validationMessage);
                }
                else
                {
                    Debug.LogWarning(validationMessage);
                }
            }

            if (validationReport.HasErrors)
            {
                return false;
            }

            var previousState = CaptureLevelLoadState();
            try
            {
                StopNextLevelLoad();
                StopClearNextStagePreload();
                StopRuntimeAheadPreload();
                boardingFlowController?.Reset();

                if (!TryBuildDailyChallengeBoard(
                        challengeLevel,
                        out var buildException))
                {
                    throw new System.InvalidOperationException(
                        $"Daily challenge step {stepIndex} board activation failed.",
                        buildException);
                }

                ResetVipTeleportState();
                ResetMixShuffleState();
                ResetDepartState();
                ResetClearRewardDoubleState();
                ResetTutorialState();
                StopDailyRewardPromptCheck();

                isDailyChallengeMode = true;
                activeDailyChallengeStepIndex =
                    Mathf.Clamp(stepIndex, 1, 3);
                activeDailyChallengeDateKey =
                    DailyChallengeService.CurrentDateKey;
                currentLevel = challengeLevel;
                gameState = GameState.Playing;
                ClearPendingFailureRecoveryState();

                BackgroundMusicPlayer.PlayDailyChallengeEvent();
                UpdateBannerAdState(false);
                RevealReadyConcealedBuses();
                ReframeBoardCamera(true);
                UpdateCounters();
                UpdateGoldUi();
                UpdateRewardedAdUi();

                uiController.SetDailyChallengeLevel(stepIndex);
                uiController.SetDailyChallengeReturnButtonState(true);
                uiController.ShowPlaying(currentLevel.LevelName);
                RefreshDailyRewardButtonState();
                CheckBlocked();
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Daily challenge step {stepIndex} activation was " +
                    $"rolled back: {exception}");
                if (!TryRestorePreviousBoard(previousState))
                {
                    gameState = GameState.Failed;
                }

                return false;
            }
        }

        private bool TryBuildDailyChallengeBoard(
            LevelData challengeLevel,
            out System.Exception exception)
        {
            exception = null;
            try
            {
                boardView.BuildLevel(
                    challengeLevel,
                    circulatingPassengerUnits,
                    buses,
                    GetDailyChallengeThemeStageNumber());
                return true;
            }
            catch (System.Exception buildException)
            {
                exception = buildException;
                return false;
            }
        }

        private static bool TryPreloadDailyChallengeStep(int stepIndex)
        {
            try
            {
                DailyChallengeEventMapBuilder.PreloadResources();
                DailyChallengeService.PreloadRuntimeLevel(stepIndex);
                return DailyChallengeService.CreateRuntimeLevel(stepIndex) !=
                    null;
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Daily challenge step {stepIndex} preload failed: " +
                    $"{exception}");
                return false;
            }
        }

        private int GetDailyChallengeThemeStageNumber()
        {
            return Mathf.Max(1, currentLevelIndex + 1);
        }

        private void ScheduleDailyRewardPromptCheck()
        {
            StopDailyRewardPromptCheck();
            if (uiController == null)
            {
                return;
            }

            RefreshDailyRewardButtonState();
            dailyRewardPromptRoutine = StartCoroutine(DailyRewardPromptCheckRoutine());
        }

        private void StopDailyRewardPromptCheck()
        {
            if (dailyRewardPromptRoutine == null)
            {
                return;
            }

            StopCoroutine(dailyRewardPromptRoutine);
            dailyRewardPromptRoutine = null;
        }

        private IEnumerator DailyRewardPromptCheckRoutine()
        {
            yield return null;

            while (uiController != null &&
                   gameState == GameState.Playing &&
                   (uiController.IsStartupSplashActive ||
                    uiController.IsInitialNicknamePromptBlocking ||
                    IsTutorialActive))
            {
                yield return null;
            }

            dailyRewardPromptRoutine = null;
            RefreshDailyRewardButtonState();
        }

        private void RefreshDailyRewardButtonState()
        {
            if (uiController == null)
            {
                return;
            }

            var visible =
                !remoteConfigBlocksGameplay &&
                gameState == GameState.Playing &&
                !isDailyChallengeMode &&
                !uiController.IsStartupSplashActive &&
                !uiController.IsInitialNicknamePromptBlocking &&
                !IsTutorialActive;

            uiController.SetDailyRewardButtonState(visible, visible && DailyRewardService.CanClaimToday);
            uiController.SetDailyChallengeButtonState(visible, visible && DailyChallengeService.HasPendingNotification);
        }

        private void ShowDailyRewardPrompt()
        {
            if (uiController == null ||
                remoteConfigBlocksGameplay ||
                gameState != GameState.Playing ||
                isDailyChallengeMode ||
                uiController.IsStartupSplashActive ||
                uiController.IsInitialNicknamePromptBlocking ||
                IsTutorialActive)
            {
                return;
            }

            uiController.ShowDailyRewardPrompt(
                DailyRewardService.GetDisplayReward(),
                DailyRewardService.CanClaimToday);
        }

        private void ClaimDailyReward()
        {
            if (!DailyRewardService.TryClaimToday(out var reward))
            {
                return;
            }

            UpdateGoldUi();
            UpdateRewardedAdUi();
            RefreshDailyRewardButtonState();
            Debug.Log($"Daily reward claimed: {reward.Type} x{reward.Amount}");
        }

        private void ShowDailyChallengePrompt()
        {
            if (uiController == null ||
                remoteConfigBlocksGameplay ||
                gameState != GameState.Playing ||
                isDailyChallengeMode ||
                uiController.IsStartupSplashActive ||
                uiController.IsInitialNicknamePromptBlocking ||
                IsTutorialActive)
            {
                return;
            }

            DailyChallengeService.MarkAvailableNotificationSeen();
            var steps = DailyChallengeService.GetTodaySteps();
            uiController.ShowDailyChallengePrompt(steps);
            RefreshDailyRewardButtonState();
        }

        private void StartDailyChallengeStep(int stepIndex)
        {
            if (dailyChallengeStartRoutine != null)
            {
                return;
            }

            if (!DailyChallengeService.CanStartStep(stepIndex))
            {
                uiController?.ShowDailyChallengePrompt(DailyChallengeService.GetTodaySteps());
                return;
            }

            dailyChallengeReturnLevelIndex = currentLevelIndex;
            dailyChallengeStartRoutine = StartCoroutine(StartDailyChallengeStepRoutine(stepIndex));
        }

        private IEnumerator StartDailyChallengeStepRoutine(int stepIndex)
        {
            var loaded = false;
            try
            {
                uiController?.ShowDailyChallengeLoading();
                Canvas.ForceUpdateCanvases();
                yield return null;
                yield return new WaitForSecondsRealtime(
                    DailyChallengeLoadingSettleSeconds);

                if (!DailyChallengeService.CanStartStep(stepIndex))
                {
                    uiController?.ShowDailyChallengePrompt(
                        DailyChallengeService.GetTodaySteps());
                    yield break;
                }

                if (!TryPreloadDailyChallengeStep(stepIndex))
                {
                    uiController?.ShowInvalid(
                        Localization.Text("stage_failed"));
                    uiController?.ShowDailyChallengePrompt(
                        DailyChallengeService.GetTodaySteps());
                    yield break;
                }

                yield return null;

                loaded = LoadDailyChallengeLevel(stepIndex);
                if (!loaded)
                {
                    uiController?.ShowInvalid(
                        Localization.Text("stage_failed"));
                    uiController?.ShowDailyChallengePrompt(
                        DailyChallengeService.GetTodaySteps());
                    yield break;
                }

                yield return null;
                Debug.Log(
                    $"Daily challenge step {stepIndex} started.");
            }
            finally
            {
                dailyChallengeStartRoutine = null;
                if (!isShuttingDown)
                {
                    uiController?.HideDailyChallengeLoading();
                }
            }
        }

        private void ReturnFromDailyChallenge()
        {
            if (!isDailyChallengeMode)
            {
                return;
            }

            if (levelSequence == null || levelSequence.Count <= 0)
            {
                uiController?.ShowInvalid(
                    Localization.Text("stage_failed"));
                return;
            }

            var returnLevelIndex = Mathf.Clamp(
                dailyChallengeReturnLevelIndex,
                0,
                levelSequence.Count - 1);
            if (!TryLoadLevelWithoutThrow(returnLevelIndex))
            {
                uiController?.SetDailyChallengeReturnButtonState(true);
                uiController?.ShowInvalid(
                    Localization.Text("stage_failed"));
                return;
            }

            RefreshDailyRewardButtonState();
            Debug.Log($"Returned from daily challenge to stage {returnLevelIndex + 1}.");
        }

        private void ClaimDailyChallengeReward(int stepIndex)
        {
            if (!DailyChallengeService.TryClaimReward(stepIndex, out var step))
            {
                uiController?.ShowDailyChallengePrompt(DailyChallengeService.GetTodaySteps());
                return;
            }

            UpdateGoldUi();
            UpdateRewardedAdUi();
            DailyChallengeService.MarkAvailableNotificationSeen();
            RefreshDailyRewardButtonState();
            uiController?.ShowDailyChallengePrompt(DailyChallengeService.GetTodaySteps());
            Debug.Log($"Daily challenge reward claimed: step={step.StepIndex}, gold={step.Reward.Gold}, skip={step.Reward.AdSkipTickets}");
        }

        private void StopDailyChallengeStart()
        {
            if (dailyChallengeStartRoutine == null)
            {
                return;
            }

            StopCoroutine(dailyChallengeStartRoutine);
            dailyChallengeStartRoutine = null;
            uiController?.HideDailyChallengeLoading();
        }

        private void CompleteDailyChallengeLevel()
        {
            var completedStepIndex = Mathf.Clamp(activeDailyChallengeStepIndex, 1, 3);
            var startedDateKey = activeDailyChallengeDateKey;
            var didRecordClear = DailyChallengeService.IsCurrentDateKey(startedDateKey) &&
                                  DailyChallengeService.MarkStepCleared(completedStepIndex);
            Debug.Log(didRecordClear
                ? $"Daily challenge step {completedStepIndex} cleared."
                : $"Daily challenge step {completedStepIndex} clear skipped because the challenge state changed.");

            if (levelSequence == null || levelSequence.Count <= 0)
            {
                uiController?.SetDailyChallengeReturnButtonState(true);
                uiController?.ShowInvalid(
                    Localization.Text("stage_failed"));
                return;
            }

            var returnLevelIndex = Mathf.Clamp(
                dailyChallengeReturnLevelIndex,
                0,
                levelSequence.Count - 1);
            if (!TryLoadLevelWithoutThrow(returnLevelIndex))
            {
                uiController?.SetDailyChallengeReturnButtonState(true);
                uiController?.ShowInvalid(
                    Localization.Text("stage_failed"));
                return;
            }

            StopDailyRewardPromptCheck();
            DailyChallengeService.MarkAvailableNotificationSeen();
            RefreshDailyRewardButtonState();
            uiController?.ShowInvalid(Localization.Text(didRecordClear
                ? "daily_challenge_clear"
                : "daily_challenge_refreshed"));
            uiController?.ShowDailyChallengePrompt(DailyChallengeService.GetTodaySteps());
            EffectAudioPlayer.PlayVictory();
        }
    }
}
