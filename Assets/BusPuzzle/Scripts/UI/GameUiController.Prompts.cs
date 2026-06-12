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
            return CreatePromptModal(overlay, name, title, anchorMin, anchorMax, out _);
        }

        private RectTransform CreatePromptModal(
            RectTransform overlay,
            string name,
            string title,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out Text titleText)
        {
            var modal = CreateGameDialog(name, overlay);
            SetAnchors(modal, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            var titlePlate = CreateDialogTitlePlate($"{name} Title Plate", modal, title);
            titleText = titlePlate.GetComponentInChildren<Text>();
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

        private static Button CreatePromptTextButton(
            string name,
            Transform parent,
            string label,
            Color fallbackColor,
            out Text labelText)
        {
            return CreateImageTextButton(name, parent, PromptButtonBaseResource, label, fallbackColor, out labelText);
        }

        private static Button CreatePromptAdButton(
            string name,
            Transform parent,
            string label,
            Color fallbackColor,
            out Text labelText)
        {
            return CreateImageTextButton(name, parent, PromptButtonBaseResource, AdIconResource, label, fallbackColor, out labelText);
        }

        private static Button CreatePromptGoldButton(
            string name,
            Transform parent,
            string label,
            Color fallbackColor,
            out Text labelText)
        {
            return CreateImageTextButton(name, parent, PromptButtonBaseResource, GoldIconResource, label, fallbackColor, out labelText);
        }

        private static void AddPromptAdBadge(Button button)
        {
            if (button == null)
            {
                return;
            }

            var adSprite = LoadResourceSprite(AdIconResource);
            if (adSprite == null)
            {
                return;
            }

            var badgeObject = new GameObject($"{button.name} Ad Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(button.transform, false);
            var badgeRect = badgeObject.GetComponent<RectTransform>();
            SetAnchors(badgeRect, new Vector2(0.68f, 0.64f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);

            var badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.sprite = adSprite;
            badgeImage.color = Color.white;
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;
        }

        private Button CreatePromptCloseButton(string name, RectTransform modal)
        {
            var closeButton = CreateRoundIconButton(name, modal, "×", 86f, 42, true);
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0.82f, 0.76f), new Vector2(1.00f, 0.96f), Vector2.zero, Vector2.zero);
            return closeButton;
        }

        private void BuildClearPrompt()
        {
            clearPrompt = CreatePromptOverlay("Clear Overlay");
            var modal = CreatePromptModal(
                clearPrompt,
                "Clear Prompt",
                Localization.Text("clear_title"),
                new Vector2(0.10f, 0.32f),
                new Vector2(0.90f, 0.66f),
                out clearPromptTitleText);

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

            clearRewardDoubleButton = CreatePromptAdButton("Clear Reward Double Button", modal, Localization.Text("reward_double_ad"), UiAdActionColor, out clearRewardDoubleButtonText);
            SetAnchors(clearRewardDoubleButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.01f), new Vector2(0.48f, 0.31f), new Vector2(6f, 16f), new Vector2(-6f, -10f));
            clearRewardDoubleButton.onClick.AddListener(() =>
            {
                ClearRewardDoubleRequested?.Invoke();
            });

            nextButton = CreateImageActionButton("Clear Next Button", modal, NextButtonIconResource, Localization.Text("next"), UiPrimaryActionColor);
            nextButtonText = GetButtonLabel(nextButton);
            SetAnchors(nextButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.01f), new Vector2(0.92f, 0.31f), new Vector2(6f, 16f), new Vector2(-6f, -10f));
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
                clearPromptText.text = Localization.Text("clear_stage", levelNumber);
            }

            if (clearRewardText != null)
            {
                clearRewardText.text = goldReward > 0
                    ? Localization.Text("reward_gold", goldReward)
                    : Localization.Text("reward_claimed");
            }

            if (nextButton != null)
            {
                nextButton.interactable = hasNextLevel;
            }

            if (nextButtonText != null)
            {
                nextButtonText.text = hasNextLevel ? Localization.Text("next") : Localization.Text("done");
            }

            SetClearRewardDouble(goldReward, false, false, false, false);
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
                Localization.Text("failed_title"),
                new Vector2(0.06f, 0.24f),
                new Vector2(0.94f, 0.69f),
                out failPromptTitleText);

            failPromptText = CreateText("Fail Prompt Text", modal, TextAnchor.MiddleCenter, 40, FontStyle.Bold);
            failPromptText.text = Localization.Text("stage_failed");
            SetAnchors(failPromptText.rectTransform, new Vector2(0f, 0.66f), new Vector2(1f, 0.82f), new Vector2(24f, 4f), new Vector2(-24f, -4f));

            failHintText = CreateText("Fail Hint Text", modal, TextAnchor.MiddleCenter, 26, FontStyle.Normal);
            failHintText.text = Localization.Text("recover_or_retry");
            failHintText.color = new Color(0.78f, 0.90f, 0.96f, 0.92f);
            SetAnchors(failHintText.rectTransform, new Vector2(0f, 0.52f), new Vector2(1f, 0.66f), new Vector2(24f, 0f), new Vector2(-24f, 0f));

            failStationUnlockButton = CreatePromptIconButton(
                "Fail Station Unlock Button",
                modal,
                StationSlotBoosterIconResource,
                Localization.Text("plus_slot"),
                UiAdActionColor,
                out failStationUnlockButtonText);
            SetAnchors(failStationUnlockButton.GetComponent<RectTransform>(), new Vector2(0.03f, 0.12f), new Vector2(0.25f, 0.52f), Vector2.zero, Vector2.zero);
            AddPromptAdBadge(failStationUnlockButton);
            failStationUnlockButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                StationUnlockRequested?.Invoke();
            });

            failVipButton = CreatePromptIconButton(
                "Fail VIP Button",
                modal,
                VipBoosterIconResource,
                Localization.Text("vip_title"),
                UiGoldActionColor,
                out failVipButtonText);
            SetAnchors(failVipButton.GetComponent<RectTransform>(), new Vector2(0.27f, 0.12f), new Vector2(0.49f, 0.52f), Vector2.zero, Vector2.zero);
            AddPromptAdBadge(failVipButton);
            failVipButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                VipTeleportRequested?.Invoke();
            });

            failDepartButton = CreatePromptIconButton(
                "Fail Depart Button",
                modal,
                DepartBoosterIconResource,
                Localization.Text("depart"),
                UiBoosterDepartColor,
                out failDepartButtonText);
            SetAnchors(failDepartButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.12f), new Vector2(0.73f, 0.52f), Vector2.zero, Vector2.zero);
            AddPromptAdBadge(failDepartButton);
            failDepartButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                DepartRequested?.Invoke();
            });

            var retryButton = CreatePromptIconButton(
                "Fail Retry Button",
                modal,
                RetryButtonIconResource,
                Localization.Text("retry"),
                UiDangerActionColor,
                out failRetryButtonText);
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
                    ? Localization.Text("recover_or_retry")
                    : Localization.Text("retry_stage");
            }

            SetFailRecoveryButtonState(failStationUnlockButton, failStationUnlockButtonText, Localization.Text("plus_slot"), canUnlockStationSlot);
            SetFailRecoveryButtonState(failVipButton, failVipButtonText, Localization.Text("vip_title"), canVipTeleport);
            SetFailRecoveryButtonState(failDepartButton, failDepartButtonText, Localization.Text("depart"), canDepart);
        }

        private static void SetFailRecoveryButtonState(Button button, Text label, string activeLabel, bool isAvailable)
        {
            if (button != null)
            {
                button.interactable = isAvailable;
            }

            if (label != null)
            {
                label.text = isAvailable ? activeLabel : Localization.Text("locked");
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
                Localization.Text("exit_title"),
                new Vector2(0.12f, 0.36f),
                new Vector2(0.88f, 0.62f),
                out exitPromptTitleText);

            exitPromptText = CreateText("Exit Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            exitPromptText.text = Localization.Text("exit_game");
            SetAnchors(exitPromptText.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Exit Close Button", modal);
            closeButton.onClick.AddListener(HideExitPrompt);

            var exitButton = CreatePromptTextButton("Exit Confirm Button", modal, Localization.Text("exit"), UiDangerActionColor, out exitButtonText);
            SetAnchors(exitButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0f), new Vector2(0.82f, 0.40f), new Vector2(0f, 16f), new Vector2(0f, -12f));
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
                Localization.Text("slot_title"),
                new Vector2(0.10f, 0.35f),
                new Vector2(0.90f, 0.62f),
                out stationUnlockPromptTitleText);

            stationUnlockPromptText = CreateText("Station Unlock Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(stationUnlockPromptText.rectTransform, new Vector2(0f, 0.46f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Station Unlock Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(stationUnlockPrompt));

            stationUnlockConfirmButton = CreatePromptAdButton("Station Unlock Confirm Button", modal, Localization.Text("watch"), UiAdActionColor, out stationUnlockConfirmButtonText);
            SetAnchors(stationUnlockConfirmButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0f), new Vector2(0.82f, 0.40f), new Vector2(0f, 16f), new Vector2(0f, -12f));
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
                Localization.Text("vip_title"),
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.63f),
                out vipTeleportPromptTitleText);

            vipTeleportPromptText = CreateText("VIP Teleport Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(vipTeleportPromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("VIP Teleport Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(vipTeleportPrompt));

            vipTeleportGoldConfirmButton = CreatePromptGoldButton("VIP Teleport Gold Button", modal, Localization.Text("cost_gold", 120), UiGoldActionColor, out vipTeleportGoldButtonText);
            SetAnchors(vipTeleportGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.09f, 0f), new Vector2(0.49f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            vipTeleportGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideVipTeleportPrompt();
                VipTeleportGoldConfirmed?.Invoke();
            });

            vipTeleportConfirmButton = CreatePromptAdButton("VIP Teleport Confirm Button", modal, Localization.Text("watch"), UiAdActionColor, out vipTeleportWatchButtonText);
            SetAnchors(vipTeleportConfirmButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0f), new Vector2(0.91f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
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
                Localization.Text("mix_title"),
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.63f),
                out mixShufflePromptTitleText);

            mixShufflePromptText = CreateText("Mix Shuffle Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(mixShufflePromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Mix Shuffle Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(mixShufflePrompt));

            mixShuffleGoldConfirmButton = CreatePromptGoldButton("Mix Shuffle Gold Button", modal, Localization.Text("cost_gold", 90), UiGoldActionColor, out mixShuffleGoldButtonText);
            SetAnchors(mixShuffleGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.09f, 0f), new Vector2(0.49f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            mixShuffleGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleGoldConfirmed?.Invoke();
            });

            mixShuffleConfirmButton = CreatePromptAdButton("Mix Shuffle Confirm Button", modal, Localization.Text("watch"), UiPrimaryActionColor, out mixShuffleWatchButtonText);
            SetAnchors(mixShuffleConfirmButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0f), new Vector2(0.91f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
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
                Localization.Text("depart"),
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.63f),
                out departPromptTitleText);

            departPromptText = CreateText("Depart Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(departPromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Depart Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(departPrompt));

            departGoldConfirmButton = CreatePromptGoldButton("Depart Gold Button", modal, Localization.Text("cost_gold", 90), UiGoldActionColor, out departGoldButtonText);
            SetAnchors(departGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.09f, 0f), new Vector2(0.49f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            departGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideDepartPrompt();
                DepartGoldConfirmed?.Invoke();
            });

            departConfirmButton = CreatePromptAdButton("Depart Confirm Button", modal, Localization.Text("watch"), UiAdActionColor, out departWatchButtonText);
            SetAnchors(departConfirmButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0f), new Vector2(0.91f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
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
                    ? Localization.Text("loading_ad")
                    : Localization.Text("watch_ad_stop", lockedSlotsRemaining);
            }

            if (stationUnlockConfirmButton != null)
            {
                stationUnlockConfirmButton.interactable = adReady && !adInProgress;
            }

            if (stationUnlockConfirmButtonText != null)
            {
                stationUnlockConfirmButtonText.text = adInProgress || !adReady ? Localization.Text("loading") : Localization.Text("watch");
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
                    ? Localization.Text("vip_bus_gold_or_ad", remainingUses)
                    : Localization.Text("vip_bus_gold_balance", remainingUses, Mathf.Max(0, goldBalance), Mathf.Max(0, goldCost));
            }

            if (vipTeleportGoldButtonText != null)
            {
                vipTeleportGoldButtonText.text = canSpendGold
                    ? Localization.Text("cost_gold", Mathf.Max(0, goldCost))
                    : Localization.Text("need_gold");
            }

            if (vipTeleportWatchButtonText != null)
            {
                vipTeleportWatchButtonText.text = adInProgress || !adReady ? Localization.Text("loading") : Localization.Text("watch");
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
                    ? Localization.Text("mix_buses_gold_or_ad")
                    : Localization.Text("mix_buses_gold_balance", Mathf.Max(0, goldBalance), Mathf.Max(0, goldCost));
            }

            if (mixShuffleGoldButtonText != null)
            {
                mixShuffleGoldButtonText.text = canSpendGold
                    ? Localization.Text("cost_gold", Mathf.Max(0, goldCost))
                    : Localization.Text("need_gold");
            }

            if (mixShuffleWatchButtonText != null)
            {
                mixShuffleWatchButtonText.text = adInProgress || !adReady ? Localization.Text("loading") : Localization.Text("watch");
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
                    ? Localization.Text("depart_buses_gold_or_ad")
                    : Localization.Text("depart_buses_gold_balance", Mathf.Max(0, goldBalance), Mathf.Max(0, goldCost));
            }

            if (departGoldButtonText != null)
            {
                departGoldButtonText.text = canSpendGold
                    ? Localization.Text("cost_gold", Mathf.Max(0, goldCost))
                    : Localization.Text("need_gold");
            }

            if (departWatchButtonText != null)
            {
                departWatchButtonText.text = adInProgress || !adReady ? Localization.Text("loading") : Localization.Text("watch");
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
