using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private RectTransform CreatePromptOverlay(string name)
        {
            var overlay = CreatePanel(name, safeAreaRoot, UiOverlayColor);
            SetAnchors(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return overlay;
        }

        private RectTransform CreatePromptModal(
            RectTransform overlay,
            string name,
            string title,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var modal = CreateGameDialog(name, overlay);
            SetAnchors(modal, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            var titlePlate = CreateDialogTitlePlate($"{name} Title Plate", modal, title);
            SetAnchors(titlePlate, new Vector2(0.17f, 0.88f), new Vector2(0.83f, 1.14f), Vector2.zero, Vector2.zero);
            return modal;
        }

        private static Button CreatePromptIconButton(
            string name,
            Transform parent,
            string iconResourcePath,
            string fallbackLabel,
            Color fallbackColor,
            out Text labelText)
        {
            var button = CreateBoosterButton(name, parent, iconResourcePath, fallbackLabel, fallbackColor, false, out _);
            labelText = CreateText($"{name} Label", button.transform, TextAnchor.MiddleCenter, 24, FontStyle.Bold);
            labelText.text = fallbackLabel;
            labelText.color = new Color(0.88f, 0.96f, 1f, 0.96f);
            labelText.resizeTextMinSize = 16;
            SetAnchors(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.24f), new Vector2(4f, 0f), new Vector2(-4f, -2f));
            return button;
        }

        private void BuildClearPrompt()
        {
            clearPrompt = CreatePromptOverlay("Clear Overlay");
            var modal = CreatePromptModal(
                clearPrompt,
                "Clear Prompt",
                "CLEAR",
                new Vector2(0.10f, 0.32f),
                new Vector2(0.90f, 0.66f));

            clearPromptText = CreateText("Clear Prompt Text", modal, TextAnchor.MiddleCenter, 42, FontStyle.Bold);
            SetAnchors(clearPromptText.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 0.80f), new Vector2(24f, 4f), new Vector2(-24f, -4f));

            var rewardPanel = CreateRoundedPanel("Clear Reward Panel", modal, new Color(0.15f, 0.20f, 0.23f, 0.94f));
            var rewardShadow = rewardPanel.gameObject.AddComponent<Shadow>();
            rewardShadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
            rewardShadow.effectDistance = new Vector2(0f, -4f);
            SetAnchors(rewardPanel, new Vector2(0.12f, 0.35f), new Vector2(0.88f, 0.55f), Vector2.zero, Vector2.zero);

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

            nextButton = CreateImageActionButton("Clear Next Button", modal, NextButtonIconResource, "Next", UiPrimaryActionColor);
            nextButtonText = GetButtonLabel(nextButton);
            SetAnchors(nextButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.01f), new Vector2(0.82f, 0.31f), new Vector2(0f, 16f), new Vector2(0f, -10f));
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
            failPrompt = CreatePromptOverlay("Fail Overlay");
            var modal = CreatePromptModal(
                failPrompt,
                "Fail Prompt",
                "FAILED",
                new Vector2(0.06f, 0.24f),
                new Vector2(0.94f, 0.69f));

            failPromptText = CreateText("Fail Prompt Text", modal, TextAnchor.MiddleCenter, 40, FontStyle.Bold);
            failPromptText.text = "Stage Failed";
            SetAnchors(failPromptText.rectTransform, new Vector2(0f, 0.66f), new Vector2(1f, 0.82f), new Vector2(24f, 4f), new Vector2(-24f, -4f));

            failHintText = CreateText("Fail Hint Text", modal, TextAnchor.MiddleCenter, 26, FontStyle.Bold);
            failHintText.text = "Recover or Retry";
            failHintText.color = new Color(0.78f, 0.90f, 0.96f, 0.92f);
            SetAnchors(failHintText.rectTransform, new Vector2(0f, 0.52f), new Vector2(1f, 0.66f), new Vector2(24f, 0f), new Vector2(-24f, 0f));

            failStationUnlockButton = CreatePromptIconButton(
                "Fail Station Unlock Button",
                modal,
                StationSlotBoosterIconResource,
                "+Slot",
                UiAdActionColor,
                out failStationUnlockButtonText);
            SetAnchors(failStationUnlockButton.GetComponent<RectTransform>(), new Vector2(0.03f, 0.12f), new Vector2(0.25f, 0.52f), Vector2.zero, Vector2.zero);
            failStationUnlockButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                StationUnlockRequested?.Invoke();
            });

            failVipButton = CreatePromptIconButton(
                "Fail VIP Button",
                modal,
                VipBoosterIconResource,
                "VIP",
                UiGoldActionColor,
                out failVipButtonText);
            SetAnchors(failVipButton.GetComponent<RectTransform>(), new Vector2(0.27f, 0.12f), new Vector2(0.49f, 0.52f), Vector2.zero, Vector2.zero);
            failVipButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                VipTeleportRequested?.Invoke();
            });

            failDepartButton = CreatePromptIconButton(
                "Fail Depart Button",
                modal,
                DepartBoosterIconResource,
                "Depart",
                UiBoosterDepartColor,
                out failDepartButtonText);
            SetAnchors(failDepartButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.12f), new Vector2(0.73f, 0.52f), Vector2.zero, Vector2.zero);
            failDepartButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                DepartRequested?.Invoke();
            });

            var retryButton = CreatePromptIconButton(
                "Fail Retry Button",
                modal,
                RetryButtonIconResource,
                "Retry",
                UiDangerActionColor,
                out _);
            SetAnchors(retryButton.GetComponent<RectTransform>(), new Vector2(0.75f, 0.12f), new Vector2(0.97f, 0.52f), Vector2.zero, Vector2.zero);
            retryButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideFailPrompt();
                RestartRequested?.Invoke();
            });

            HideFailPrompt();
        }

        private void ShowFailPrompt(bool canUnlockStationSlot, bool canVipTeleport, bool canDepart)
        {
            if (failPrompt == null)
            {
                return;
            }

            failPrompt.gameObject.SetActive(true);
            ApplyFailRecoveryState(canUnlockStationSlot, canVipTeleport, canDepart);
        }

        private void HideFailPrompt()
        {
            if (failPrompt != null)
            {
                failPrompt.gameObject.SetActive(false);
            }
        }

        private void ApplyFailRecoveryState(bool canUnlockStationSlot, bool canVipTeleport, bool canDepart)
        {
            if (failHintText != null)
            {
                failHintText.text = canUnlockStationSlot || canVipTeleport || canDepart
                    ? "Recover or Retry"
                    : "Retry Stage";
            }

            SetFailRecoveryButtonState(failStationUnlockButton, failStationUnlockButtonText, "+ Slot", canUnlockStationSlot);
            SetFailRecoveryButtonState(failVipButton, failVipButtonText, "VIP", canVipTeleport);
            SetFailRecoveryButtonState(failDepartButton, failDepartButtonText, "Depart", canDepart);
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
            exitPrompt = CreatePromptOverlay("Exit Overlay");
            var modal = CreatePromptModal(
                exitPrompt,
                "Exit Prompt",
                "EXIT",
                new Vector2(0.12f, 0.36f),
                new Vector2(0.88f, 0.62f));

            exitPromptText = CreateText("Exit Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            exitPromptText.text = "Exit Game?";
            SetAnchors(exitPromptText.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Exit Cancel Button", modal, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.40f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(HideExitPrompt);

            var exitButton = CreateButton("Exit Confirm Button", modal, "Exit", UiDangerActionColor);
            SetAnchors(exitButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.40f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
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
            stationUnlockPrompt = CreatePromptOverlay("Station Unlock Overlay");
            var modal = CreatePromptModal(
                stationUnlockPrompt,
                "Station Unlock Prompt",
                "SLOT",
                new Vector2(0.10f, 0.35f),
                new Vector2(0.90f, 0.62f));

            stationUnlockPromptText = CreateText("Station Unlock Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(stationUnlockPromptText.rectTransform, new Vector2(0f, 0.46f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Station Unlock Cancel Button", modal, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.40f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(() => CancelRecoveryPrompt(stationUnlockPrompt));

            stationUnlockConfirmButton = CreateButton("Station Unlock Confirm Button", modal, "Watch", UiAdActionColor);
            SetAnchors(stationUnlockConfirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.40f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
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
            vipTeleportPrompt = CreatePromptOverlay("VIP Teleport Overlay");
            var modal = CreatePromptModal(
                vipTeleportPrompt,
                "VIP Teleport Prompt",
                "VIP",
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.63f));

            vipTeleportPromptText = CreateText("VIP Teleport Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(vipTeleportPromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("VIP Teleport Cancel Button", modal, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.31f, 0.42f), new Vector2(18f, 16f), new Vector2(-6f, -12f));
            cancelButton.onClick.AddListener(() => CancelRecoveryPrompt(vipTeleportPrompt));

            vipTeleportGoldConfirmButton = CreateButton("VIP Teleport Gold Button", modal, "120 Gold", UiGoldActionColor);
            SetAnchors(vipTeleportGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.345f, 0f), new Vector2(0.655f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            vipTeleportGoldButtonText = GetButtonLabel(vipTeleportGoldConfirmButton);
            vipTeleportGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideVipTeleportPrompt();
                VipTeleportGoldConfirmed?.Invoke();
            });

            vipTeleportConfirmButton = CreateButton("VIP Teleport Confirm Button", modal, "Watch", UiAdActionColor);
            SetAnchors(vipTeleportConfirmButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0f), new Vector2(1f, 0.42f), new Vector2(6f, 16f), new Vector2(-18f, -12f));
            vipTeleportWatchButtonText = GetButtonLabel(vipTeleportConfirmButton);
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
            mixShufflePrompt = CreatePromptOverlay("Mix Shuffle Overlay");
            var modal = CreatePromptModal(
                mixShufflePrompt,
                "Mix Shuffle Prompt",
                "MIX",
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.63f));

            mixShufflePromptText = CreateText("Mix Shuffle Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(mixShufflePromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Mix Shuffle Cancel Button", modal, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.31f, 0.42f), new Vector2(18f, 16f), new Vector2(-6f, -12f));
            cancelButton.onClick.AddListener(() => CancelRecoveryPrompt(mixShufflePrompt));

            mixShuffleGoldConfirmButton = CreateButton("Mix Shuffle Gold Button", modal, "90 Gold", UiGoldActionColor);
            SetAnchors(mixShuffleGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.345f, 0f), new Vector2(0.655f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            mixShuffleGoldButtonText = GetButtonLabel(mixShuffleGoldConfirmButton);
            mixShuffleGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleGoldConfirmed?.Invoke();
            });

            mixShuffleConfirmButton = CreateButton("Mix Shuffle Confirm Button", modal, "Watch", UiPrimaryActionColor);
            SetAnchors(mixShuffleConfirmButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0f), new Vector2(1f, 0.42f), new Vector2(6f, 16f), new Vector2(-18f, -12f));
            mixShuffleWatchButtonText = GetButtonLabel(mixShuffleConfirmButton);
            mixShuffleConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleConfirmed?.Invoke();
            });

            HideMixShufflePrompt();
        }

        private void BuildDepartPrompt()
        {
            departPrompt = CreatePromptOverlay("Depart Overlay");
            var modal = CreatePromptModal(
                departPrompt,
                "Depart Prompt",
                "DEPART",
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.63f));

            departPromptText = CreateText("Depart Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(departPromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Depart Cancel Button", modal, "Cancel", UiSecondaryActionColor);
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.31f, 0.42f), new Vector2(18f, 16f), new Vector2(-6f, -12f));
            cancelButton.onClick.AddListener(() => CancelRecoveryPrompt(departPrompt));

            departGoldConfirmButton = CreateButton("Depart Gold Button", modal, "90 Gold", UiGoldActionColor);
            SetAnchors(departGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.345f, 0f), new Vector2(0.655f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            departGoldButtonText = GetButtonLabel(departGoldConfirmButton);
            departGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideDepartPrompt();
                DepartGoldConfirmed?.Invoke();
            });

            departConfirmButton = CreateButton("Depart Confirm Button", modal, "Watch", UiAdActionColor);
            SetAnchors(departConfirmButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0f), new Vector2(1f, 0.42f), new Vector2(6f, 16f), new Vector2(-18f, -12f));
            departWatchButtonText = GetButtonLabel(departConfirmButton);
            departConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideDepartPrompt();
                DepartConfirmed?.Invoke();
            });

            HideDepartPrompt();
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

        private void ApplyVipTeleportPromptState(
            int usedCount,
            int maxUses,
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            bool adReady,
            bool adInProgress)
        {
            if (vipTeleportPromptText != null)
            {
                var remainingUses = Mathf.Max(0, maxUses - usedCount);
                vipTeleportPromptText.text = canSpendGold
                    ? $"VIP Bus ({remainingUses})\nUse Gold or Watch Ad"
                    : $"VIP Bus ({remainingUses})\nGold {Mathf.Max(0, goldBalance)}/{Mathf.Max(0, goldCost)}";
            }

            if (vipTeleportGoldButtonText != null)
            {
                vipTeleportGoldButtonText.text = canSpendGold
                    ? $"{Mathf.Max(0, goldCost)} Gold"
                    : "Need Gold";
            }

            if (vipTeleportWatchButtonText != null)
            {
                vipTeleportWatchButtonText.text = adInProgress || !adReady ? "Loading" : "Watch";
            }

            if (vipTeleportGoldConfirmButton != null)
            {
                vipTeleportGoldConfirmButton.interactable = canSpendGold && !adInProgress && usedCount < maxUses;
            }

            if (vipTeleportConfirmButton != null)
            {
                vipTeleportConfirmButton.interactable = adReady && !adInProgress && usedCount < maxUses;
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

        private void ApplyDepartPromptState(
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            bool adReady,
            bool adInProgress)
        {
            if (departPromptText != null)
            {
                departPromptText.text = canSpendGold
                    ? "Depart Buses\nUse Gold or Watch Ad"
                    : $"Depart Buses\nGold {Mathf.Max(0, goldBalance)}/{Mathf.Max(0, goldCost)}";
            }

            if (departGoldButtonText != null)
            {
                departGoldButtonText.text = canSpendGold
                    ? $"{Mathf.Max(0, goldCost)} Gold"
                    : "Need Gold";
            }

            if (departWatchButtonText != null)
            {
                departWatchButtonText.text = adInProgress || !adReady ? "Loading" : "Watch";
            }

            if (departGoldConfirmButton != null)
            {
                departGoldConfirmButton.interactable = canSpendGold && !adInProgress;
            }

            if (departConfirmButton != null)
            {
                departConfirmButton.interactable = adReady && !adInProgress;
            }
        }
    }
}
