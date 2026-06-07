using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private void BuildClearPrompt()
        {
            clearPrompt = CreateGameDialog("Clear Prompt", safeAreaRoot);
            SetAnchors(clearPrompt, new Vector2(0.12f, 0.33f), new Vector2(0.88f, 0.64f), Vector2.zero, Vector2.zero);

            clearPromptText = CreateText("Clear Prompt Text", clearPrompt, TextAnchor.MiddleCenter, 42, FontStyle.Bold);
            SetAnchors(clearPromptText.rectTransform, new Vector2(0f, 0.64f), new Vector2(1f, 1f), new Vector2(24f, 8f), new Vector2(-24f, -4f));

            var rewardPanel = CreateRoundedPanel("Clear Reward Panel", clearPrompt, new Color(0.15f, 0.20f, 0.23f, 0.94f));
            var rewardShadow = rewardPanel.gameObject.AddComponent<Shadow>();
            rewardShadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
            rewardShadow.effectDistance = new Vector2(0f, -4f);
            SetAnchors(rewardPanel, new Vector2(0.12f, 0.39f), new Vector2(0.88f, 0.62f), Vector2.zero, Vector2.zero);

            var goldIconObject = new GameObject("Clear Reward Gold Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            goldIconObject.transform.SetParent(rewardPanel, false);
            var goldIconRect = goldIconObject.GetComponent<RectTransform>();
            SetAnchors(goldIconRect, new Vector2(0.08f, 0.08f), new Vector2(0.28f, 0.92f), Vector2.zero, Vector2.zero);
            var goldIconImage = goldIconObject.GetComponent<Image>();
            goldIconImage.sprite = LoadGoldIconSprite();
            goldIconImage.color = Color.white;
            goldIconImage.preserveAspect = true;
            goldIconImage.raycastTarget = false;

            clearRewardText = CreateText("Clear Reward Text", rewardPanel, TextAnchor.MiddleLeft, 34, FontStyle.Bold);
            clearRewardText.color = new Color(1.00f, 0.78f, 0.16f);
            SetAnchors(clearRewardText.rectTransform, new Vector2(0.31f, 0f), Vector2.one, new Vector2(0f, 2f), new Vector2(-16f, -2f));

            nextButton = CreateImageActionButton("Clear Next Button", clearPrompt, NextButtonIconResource, "Next", UiPrimaryActionColor);
            nextButtonText = GetButtonLabel(nextButton);
            SetAnchors(nextButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.01f), new Vector2(0.82f, 0.38f), new Vector2(0f, 16f), new Vector2(0f, -10f));
            nextButton.onClick.AddListener(() =>
            {
                HideClearPrompt();
                NextLevelRequested?.Invoke();
            });

            HideClearPrompt();
        }

        private void ShowClearPrompt(int levelNumber, bool hasNextLevel, int goldReward)
        {
            if (clearPrompt == null)
            {
                return;
            }

            clearPrompt.gameObject.SetActive(true);
            if (clearPromptText != null)
            {
                clearPromptText.text = $"Stage {levelNumber:00} Clear";
            }

            if (clearRewardText != null)
            {
                clearRewardText.text = goldReward > 0 ? $"+{goldReward} Gold" : "Reward Claimed";
            }

            if (nextButton != null)
            {
                nextButton.interactable = hasNextLevel;
            }

            if (nextButtonText != null)
            {
                nextButtonText.text = hasNextLevel ? "Next" : "Done";
            }
        }

        private void HideClearPrompt()
        {
            if (clearPrompt != null)
            {
                clearPrompt.gameObject.SetActive(false);
            }
        }

        private void BuildFailPrompt()
        {
            failPrompt = CreateGameDialog("Fail Prompt", safeAreaRoot);
            SetAnchors(failPrompt, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);

            failPromptText = CreateText("Fail Prompt Text", failPrompt, TextAnchor.MiddleCenter, 40, FontStyle.Bold);
            failPromptText.text = "Stage Failed";
            SetAnchors(failPromptText.rectTransform, new Vector2(0f, 0.74f), new Vector2(1f, 1f), new Vector2(24f, 6f), new Vector2(-24f, -4f));

            failHintText = CreateText("Fail Hint Text", failPrompt, TextAnchor.MiddleCenter, 26, FontStyle.Bold);
            failHintText.text = "Recover or Retry";
            failHintText.color = new Color(0.78f, 0.90f, 0.96f, 0.92f);
            SetAnchors(failHintText.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.76f), new Vector2(24f, 0f), new Vector2(-24f, 0f));

            failStationUnlockButton = CreateButton("Fail Station Unlock Button", failPrompt, "+ Slot", UiAdActionColor);
            failStationUnlockButtonText = GetButtonLabel(failStationUnlockButton);
            SetAnchors(failStationUnlockButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.34f), new Vector2(0.36f, 0.58f), Vector2.zero, Vector2.zero);
            failStationUnlockButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                StationUnlockRequested?.Invoke();
            });

            failVipButton = CreateButton("Fail VIP Button", failPrompt, "VIP", UiGoldActionColor);
            failVipButtonText = GetButtonLabel(failVipButton);
            SetAnchors(failVipButton.GetComponent<RectTransform>(), new Vector2(0.38f, 0.34f), new Vector2(0.68f, 0.58f), Vector2.zero, Vector2.zero);
            failVipButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                VipTeleportRequested?.Invoke();
            });

            failMixButton = CreateButton("Fail Mix Button", failPrompt, "Mix", UiPrimaryActionColor);
            failMixButtonText = GetButtonLabel(failMixButton);
            SetAnchors(failMixButton.GetComponent<RectTransform>(), new Vector2(0.70f, 0.34f), new Vector2(0.94f, 0.58f), Vector2.zero, Vector2.zero);
            failMixButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                MixShuffleRequested?.Invoke();
            });

            var retryButton = CreateButton("Fail Retry Button", failPrompt, "Retry", UiDangerActionColor);
            SetAnchors(retryButton.GetComponent<RectTransform>(), new Vector2(0.16f, 0f), new Vector2(0.84f, 0.26f), new Vector2(0f, 16f), new Vector2(0f, -12f));
            retryButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideFailPrompt();
                RestartRequested?.Invoke();
            });

            HideFailPrompt();
        }

        private void ShowFailPrompt(bool canUnlockStationSlot, bool canVipTeleport, bool canMixShuffle)
        {
            if (failPrompt == null)
            {
                return;
            }

            failPrompt.gameObject.SetActive(true);
            ApplyFailRecoveryState(canUnlockStationSlot, canVipTeleport, canMixShuffle);
        }

        private void HideFailPrompt()
        {
            if (failPrompt != null)
            {
                failPrompt.gameObject.SetActive(false);
            }
        }

        private void ApplyFailRecoveryState(bool canUnlockStationSlot, bool canVipTeleport, bool canMixShuffle)
        {
            if (failHintText != null)
            {
                failHintText.text = canUnlockStationSlot || canVipTeleport || canMixShuffle
                    ? "Recover or Retry"
                    : "Retry Stage";
            }

            SetFailRecoveryButtonState(failStationUnlockButton, failStationUnlockButtonText, "+ Slot", canUnlockStationSlot);
            SetFailRecoveryButtonState(failVipButton, failVipButtonText, "VIP", canVipTeleport);
            SetFailRecoveryButtonState(failMixButton, failMixButtonText, "Mix", canMixShuffle);
        }

        private static void SetFailRecoveryButtonState(Button button, Text label, string activeLabel, bool isAvailable)
        {
            if (button != null)
            {
                button.interactable = isAvailable;
            }

            if (label != null)
            {
                label.text = isAvailable ? activeLabel : "Locked";
            }
        }

        private void BeginFailRecoveryPrompt()
        {
            shouldReturnToFailPromptOnRecoveryCancel = true;
            HideFailPrompt();
        }

        private void CancelRecoveryPrompt(RectTransform prompt)
        {
            if (prompt != null)
            {
                prompt.gameObject.SetActive(false);
            }

            if (shouldReturnToFailPromptOnRecoveryCancel)
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                if (failPrompt != null)
                {
                    failPrompt.gameObject.SetActive(true);
                }
            }

            RecoveryPromptCancelled?.Invoke();
        }

        private void BuildExitPrompt()
        {
            exitPrompt = CreateGameDialog("Exit Prompt", safeAreaRoot);
            SetAnchors(exitPrompt, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.56f), Vector2.zero, Vector2.zero);

            exitPromptText = CreateText("Exit Prompt Text", exitPrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            exitPromptText.text = "Exit Game?";
            SetAnchors(exitPromptText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Exit Cancel Button", exitPrompt, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(HideExitPrompt);

            var exitButton = CreateButton("Exit Confirm Button", exitPrompt, "Exit", UiDangerActionColor);
            SetAnchors(exitButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            exitButton.onClick.AddListener(() =>
            {
                HideExitPrompt();
                ExitConfirmed?.Invoke();
            });

            HideExitPrompt();
        }

        private void HideExitPrompt()
        {
            if (exitPrompt != null)
            {
                exitPrompt.gameObject.SetActive(false);
            }
        }

        private void BuildStationUnlockPrompt()
        {
            stationUnlockPrompt = CreateGameDialog("Station Unlock Prompt", safeAreaRoot);
            SetAnchors(stationUnlockPrompt, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.56f), Vector2.zero, Vector2.zero);

            stationUnlockPromptText = CreateText("Station Unlock Prompt Text", stationUnlockPrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(stationUnlockPromptText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Station Unlock Cancel Button", stationUnlockPrompt, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(() => CancelRecoveryPrompt(stationUnlockPrompt));

            stationUnlockConfirmButton = CreateButton("Station Unlock Confirm Button", stationUnlockPrompt, "Watch", UiAdActionColor);
            SetAnchors(stationUnlockConfirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            stationUnlockConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideStationUnlockPrompt();
                StationUnlockConfirmed?.Invoke();
            });

            HideStationUnlockPrompt();
        }

        private void BuildVipTeleportPrompt()
        {
            vipTeleportPrompt = CreateGameDialog("VIP Teleport Prompt", safeAreaRoot);
            SetAnchors(vipTeleportPrompt, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.56f), Vector2.zero, Vector2.zero);

            vipTeleportPromptText = CreateText("VIP Teleport Prompt Text", vipTeleportPrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(vipTeleportPromptText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("VIP Teleport Cancel Button", vipTeleportPrompt, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(() => CancelRecoveryPrompt(vipTeleportPrompt));

            vipTeleportConfirmButton = CreateButton("VIP Teleport Confirm Button", vipTeleportPrompt, "Watch", UiGoldActionColor);
            SetAnchors(vipTeleportConfirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            vipTeleportConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideVipTeleportPrompt();
                VipTeleportConfirmed?.Invoke();
            });

            HideVipTeleportPrompt();
        }

        private void BuildMixShufflePrompt()
        {
            mixShufflePrompt = CreateGameDialog("Mix Shuffle Prompt", safeAreaRoot);
            SetAnchors(mixShufflePrompt, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);

            mixShufflePromptText = CreateText("Mix Shuffle Prompt Text", mixShufflePrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(mixShufflePromptText.rectTransform, new Vector2(0f, 0.46f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Mix Shuffle Cancel Button", mixShufflePrompt, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.31f, 0.46f), new Vector2(18f, 16f), new Vector2(-6f, -12f));
            cancelButton.onClick.AddListener(() => CancelRecoveryPrompt(mixShufflePrompt));

            mixShuffleGoldConfirmButton = CreateButton("Mix Shuffle Gold Button", mixShufflePrompt, "90 Gold", UiGoldActionColor);
            SetAnchors(mixShuffleGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.345f, 0f), new Vector2(0.655f, 0.46f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            mixShuffleGoldButtonText = GetButtonLabel(mixShuffleGoldConfirmButton);
            mixShuffleGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleGoldConfirmed?.Invoke();
            });

            mixShuffleConfirmButton = CreateButton("Mix Shuffle Confirm Button", mixShufflePrompt, "Watch", UiPrimaryActionColor);
            SetAnchors(mixShuffleConfirmButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0f), new Vector2(1f, 0.46f), new Vector2(6f, 16f), new Vector2(-18f, -12f));
            mixShuffleWatchButtonText = GetButtonLabel(mixShuffleConfirmButton);
            mixShuffleConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleConfirmed?.Invoke();
            });

            HideMixShufflePrompt();
        }

        private void ApplyStationUnlockPromptState(int lockedSlotsRemaining, bool adReady, bool adInProgress)
        {
            if (stationUnlockPromptText != null)
            {
                stationUnlockPromptText.text = adInProgress || !adReady
                    ? "Loading Ad"
                    : $"Watch Ad?\n+1 Stop ({lockedSlotsRemaining})";
            }

            if (stationUnlockConfirmButton != null)
            {
                stationUnlockConfirmButton.interactable = adReady && !adInProgress;
            }
        }

        private void ApplyVipTeleportPromptState(int remainingAds, bool adReady, bool adInProgress)
        {
            if (vipTeleportPromptText != null)
            {
                vipTeleportPromptText.text = adInProgress || !adReady
                    ? "Loading Ad"
                    : $"Watch Ad?\nVIP Bus ({remainingAds})";
            }

            if (vipTeleportConfirmButton != null)
            {
                vipTeleportConfirmButton.interactable = adReady && !adInProgress && remainingAds > 0;
            }
        }

        private void ApplyMixShufflePromptState(
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            bool adReady,
            bool adInProgress)
        {
            if (mixShufflePromptText != null)
            {
                mixShufflePromptText.text = canSpendGold
                    ? "Mix Buses\nUse Gold or Watch Ad"
                    : $"Mix Buses\nGold {Mathf.Max(0, goldBalance)}/{Mathf.Max(0, goldCost)}";
            }

            if (mixShuffleGoldButtonText != null)
            {
                mixShuffleGoldButtonText.text = canSpendGold
                    ? $"{Mathf.Max(0, goldCost)} Gold"
                    : "Need Gold";
            }

            if (mixShuffleWatchButtonText != null)
            {
                mixShuffleWatchButtonText.text = adInProgress || !adReady ? "Loading" : "Watch";
            }

            if (mixShuffleGoldConfirmButton != null)
            {
                mixShuffleGoldConfirmButton.interactable = canSpendGold && !adInProgress;
            }

            if (mixShuffleConfirmButton != null)
            {
                mixShuffleConfirmButton.interactable = adReady && !adInProgress;
            }
        }
    }
}
