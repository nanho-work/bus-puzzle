using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public sealed partial class GameManager
    {
        private void LoadDailyChallengeLevel(int stepIndex)
        {
            var challengeLevel = DailyChallengeService.CreateRuntimeLevel(stepIndex);
            if (challengeLevel == null)
            {
                uiController?.ShowDailyChallengePrompt(DailyChallengeService.GetTodaySteps());
                return;
            }

            boardingFlowController.Reset();
            ResetVipTeleportState();
            ResetMixShuffleState();
            ResetDepartState();
            ResetClearRewardDoubleState();
            ResetTutorialState();
            StopDailyRewardPromptCheck();

            isDailyChallengeMode = true;
            activeDailyChallengeStepIndex = Mathf.Clamp(stepIndex, 1, 3);
            activeDailyChallengeDateKey = DailyChallengeService.CurrentDateKey;
            BackgroundMusicPlayer.PlayDailyChallengeEvent();
            currentLevel = challengeLevel;
            UpdateBannerAdState(false);

            var validationReport = LevelValidator.Validate(currentLevel, false);
            if (validationReport.HasIssues)
            {
                var validationMessage = validationReport.ToConsoleMessage(currentLevel.LevelName);
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

            boardView.BuildLevel(currentLevel, circulatingPassengerUnits, buses, GetDailyChallengeThemeStageNumber());
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
            uiController?.ShowDailyChallengeLoading();
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return new WaitForSecondsRealtime(DailyChallengeLoadingSettleSeconds);

            if (!DailyChallengeService.CanStartStep(stepIndex))
            {
                uiController?.HideDailyChallengeLoading();
                uiController?.ShowDailyChallengePrompt(DailyChallengeService.GetTodaySteps());
                dailyChallengeStartRoutine = null;
                yield break;
            }

            DailyChallengeEventMapBuilder.PreloadResources();
            DailyChallengeService.PreloadRuntimeLevel(stepIndex);
            yield return null;

            LoadDailyChallengeLevel(stepIndex);
            yield return null;

            uiController?.HideDailyChallengeLoading();
            dailyChallengeStartRoutine = null;
            Debug.Log($"Daily challenge step {stepIndex} started.");
        }

        private void ReturnFromDailyChallenge()
        {
            if (!isDailyChallengeMode)
            {
                return;
            }

            var returnLevelIndex = Mathf.Clamp(dailyChallengeReturnLevelIndex, 0, levelSequence.Count - 1);
            LoadLevel(returnLevelIndex);
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

            var returnLevelIndex = Mathf.Clamp(dailyChallengeReturnLevelIndex, 0, levelSequence.Count - 1);
            LoadLevel(returnLevelIndex);
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
