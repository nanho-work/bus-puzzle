using System.Collections.Generic;
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

        private static Button CreateOverlayDismissButton(string name, RectTransform overlay, System.Action dismissAction)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(overlay, false);

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            SetAnchors(buttonRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0f);
            buttonImage.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => dismissAction?.Invoke());
            buttonObject.transform.SetAsFirstSibling();
            return button;
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

        private static Button CreatePromptSkipButton(
            string name,
            Transform parent,
            string label,
            Color fallbackColor,
            out Text labelText)
        {
            return CreateImageTextButton(name, parent, PromptButtonBaseResource, SkipIconResource, label, fallbackColor, out labelText);
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

        private static RectTransform AddRewardClaimBadge(Button button, string namePrefix)
        {
            if (button == null)
            {
                return null;
            }

            var badge = CreateRoundedPanel($"{namePrefix} Reward Badge", button.transform, new Color(0.96f, 0.17f, 0.28f, 0.98f));
            SetAnchors(badge, new Vector2(0.74f, 0.68f), new Vector2(1.08f, 1.08f), Vector2.zero, Vector2.zero);

            var badgeImage = badge.GetComponent<Image>();
            badgeImage.raycastTarget = false;

            var badgeText = CreateText($"{namePrefix} Reward Badge Text", badge, TextAnchor.MiddleCenter, 28, FontStyle.Bold);
            badgeText.text = "1";
            badgeText.color = Color.white;
            badgeText.resizeTextMinSize = 18;
            badgeText.raycastTarget = false;
            SetAnchors(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(-2f, -2f));
            return badge;
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
            if (clearPromptTitleText != null)
            {
                clearPromptTitleText.fontSize += 1;
                clearPromptTitleText.resizeTextMaxSize = clearPromptTitleText.fontSize;
            }

            clearPromptText = CreateText("Clear Prompt Text", modal, TextAnchor.MiddleCenter, 42, FontStyle.Normal);
            SetAnchors(clearPromptText.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 0.80f), new Vector2(24f, 4f), new Vector2(-24f, -4f));

            var rewardPanel = CreateRoundedPanel("Clear Reward Panel", modal, new Color(0.16f, 0.33f, 0.38f, 0.90f));
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

            clearRewardText = CreateText("Clear Reward Text", rewardPanel, TextAnchor.MiddleLeft, 34, FontStyle.Normal);
            clearRewardText.color = new Color(1.00f, 0.78f, 0.16f);
            SetAnchors(clearRewardText.rectTransform, new Vector2(0.31f, 0f), Vector2.one, new Vector2(0f, 2f), new Vector2(-16f, -2f));

            clearRewardDoubleButton = CreatePromptAdButton("Clear Reward Double Button", modal, Localization.Text("reward_double_ad"), UiAdActionColor, out clearRewardDoubleButtonText);
            GameFontProvider.ApplyMediumToText(clearRewardDoubleButtonText);
            SetAnchors(clearRewardDoubleButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.01f), new Vector2(0.48f, 0.31f), new Vector2(6f, 16f), new Vector2(-6f, -10f));
            clearRewardDoubleButton.onClick.AddListener(() =>
            {
                ClearRewardDoubleRequested?.Invoke();
            });

            nextButton = CreateImageActionButton("Clear Next Button", modal, NextButtonIconResource, Localization.Text("next"), UiPrimaryActionColor);
            nextButtonText = GetButtonLabel(nextButton);
            GameFontProvider.ApplyMediumToText(nextButtonText);
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
            SetAnchors(failStationUnlockButton.GetComponent<RectTransform>(), new Vector2(0.02f, 0.12f), new Vector2(0.18f, 0.52f), Vector2.zero, Vector2.zero);
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
            SetAnchors(failVipButton.GetComponent<RectTransform>(), new Vector2(0.215f, 0.12f), new Vector2(0.375f, 0.52f), Vector2.zero, Vector2.zero);
            AddPromptAdBadge(failVipButton);
            failVipButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                VipTeleportRequested?.Invoke();
            });

            failMixButton = CreatePromptIconButton(
                "Fail Mix Button",
                modal,
                MixBoosterIconResource,
                Localization.Text("mix_title"),
                UiBoosterBlueColor,
                out failMixButtonText);
            SetAnchors(failMixButton.GetComponent<RectTransform>(), new Vector2(0.41f, 0.12f), new Vector2(0.57f, 0.52f), Vector2.zero, Vector2.zero);
            AddPromptAdBadge(failMixButton);
            failMixButton.onClick.AddListener(() =>
            {
                BeginFailRecoveryPrompt();
                MixShuffleRequested?.Invoke();
            });

            failDepartButton = CreatePromptIconButton(
                "Fail Depart Button",
                modal,
                DepartBoosterIconResource,
                Localization.Text("depart"),
                UiBoosterDepartColor,
                out failDepartButtonText);
            SetAnchors(failDepartButton.GetComponent<RectTransform>(), new Vector2(0.605f, 0.12f), new Vector2(0.765f, 0.52f), Vector2.zero, Vector2.zero);
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
            SetAnchors(retryButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0.12f), new Vector2(0.96f, 0.52f), Vector2.zero, Vector2.zero);
            retryButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideFailPrompt();
                RestartRequested?.Invoke();
            });

            HideFailPrompt();
        }

        private void ShowFailPrompt(bool canUnlockStationSlot, bool canVipTeleport, bool canMixShuffle, bool canDepart)
        {
            if (failPrompt == null)
            {
                return;
            }

            failPrompt.gameObject.SetActive(true);
            ApplyFailRecoveryState(canUnlockStationSlot, canVipTeleport, canMixShuffle, canDepart);
        }

        private void HideFailPrompt()
        {
            if (failPrompt != null)
            {
                failPrompt.gameObject.SetActive(false);
            }
        }

        private void ApplyFailRecoveryState(bool canUnlockStationSlot, bool canVipTeleport, bool canMixShuffle, bool canDepart)
        {
            if (failHintText != null)
            {
                failHintText.text = canUnlockStationSlot || canVipTeleport || canMixShuffle || canDepart
                    ? Localization.Text("recover_or_retry")
                    : Localization.Text("retry_stage");
            }

            SetFailRecoveryButtonState(failStationUnlockButton, failStationUnlockButtonText, Localization.Text("plus_slot"), canUnlockStationSlot);
            SetFailRecoveryButtonState(failVipButton, failVipButtonText, Localization.Text("vip_title"), canVipTeleport);
            SetFailRecoveryButtonState(failMixButton, failMixButtonText, Localization.Text("mix_title"), canMixShuffle);
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
            CreateOverlayDismissButton("Exit Outside Close Button", exitPrompt, HideExitPrompt);

            exitPromptText = CreateText("Exit Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            exitPromptText.text = Localization.Text("exit_game");
            SetAnchors(exitPromptText.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Exit Close Button", modal);
            closeButton.onClick.AddListener(HideExitPrompt);

            var exitButton = CreateImageActionButton("Exit Confirm Button", modal, ExitButtonIconResource, Localization.Text("exit"), UiDangerActionColor);
            exitButtonText = GetButtonLabel(exitButton);
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

        private void BuildDailyRewardPrompt()
        {
            dailyRewardPrompt = CreatePromptOverlay("Daily Reward Overlay");
            var modal = CreatePromptModal(
                dailyRewardPrompt,
                "Daily Reward Prompt",
                Localization.Text("daily_reward_title"),
                new Vector2(0.08f, 0.33f),
                new Vector2(0.92f, 0.66f),
                out dailyRewardPromptTitleText);
            CreateOverlayDismissButton("Daily Reward Outside Close Button", dailyRewardPrompt, HideDailyRewardPrompt);

            dailyRewardPromptMessageText = CreateText("Daily Reward Message", modal, TextAnchor.MiddleCenter, 28, FontStyle.Normal);
            ApplySettingsTextWeight(dailyRewardPromptMessageText);
            dailyRewardPromptMessageText.color = new Color(0.86f, 0.94f, 1f, 0.94f);
            dailyRewardPromptMessageText.text = Localization.Text("daily_reward_message");
            SetAnchors(dailyRewardPromptMessageText.rectTransform, new Vector2(0.06f, 0.61f), new Vector2(0.94f, 0.79f), new Vector2(8f, 0f), new Vector2(-8f, 0f));

            var rewardPanel = CreateRoundedPanel("Daily Reward Item Panel", modal, new Color(0.14f, 0.30f, 0.35f, 0.88f));
            var rewardShadow = rewardPanel.gameObject.AddComponent<Shadow>();
            rewardShadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
            rewardShadow.effectDistance = new Vector2(0f, -4f);
            SetAnchors(rewardPanel, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.58f), Vector2.zero, Vector2.zero);

            var iconObject = new GameObject("Daily Reward Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(rewardPanel, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            SetAnchors(iconRect, new Vector2(0.08f, 0.08f), new Vector2(0.28f, 0.92f), Vector2.zero, Vector2.zero);

            dailyRewardIconImage = iconObject.GetComponent<Image>();
            dailyRewardIconImage.color = Color.white;
            dailyRewardIconImage.preserveAspect = true;
            dailyRewardIconImage.raycastTarget = false;

            dailyRewardText = CreateText("Daily Reward Text", rewardPanel, TextAnchor.MiddleLeft, 34, FontStyle.Normal);
            ApplySettingsTextWeight(dailyRewardText);
            SetAnchors(dailyRewardText.rectTransform, new Vector2(0.32f, 0f), Vector2.one, new Vector2(0f, 2f), new Vector2(-16f, -2f));

            dailyRewardClaimButton = CreatePromptTextButton(
                "Daily Reward Claim Button",
                modal,
                Localization.Text("daily_reward_claim"),
                UiPrimaryActionColor,
                out dailyRewardClaimButtonText);
            ApplySettingsTextWeight(dailyRewardClaimButtonText);
            SetAnchors(dailyRewardClaimButton.GetComponent<RectTransform>(), new Vector2(0.22f, 0.02f), new Vector2(0.78f, 0.30f), new Vector2(0f, 12f), new Vector2(0f, -8f));
            dailyRewardClaimBadge = AddRewardClaimBadge(dailyRewardClaimButton, "Daily Reward Claim");
            if (dailyRewardClaimBadge != null)
            {
                dailyRewardClaimBadge.gameObject.SetActive(false);
            }

            dailyRewardClaimButton.onClick.AddListener(() =>
            {
                if (!dailyRewardPromptCanClaim)
                {
                    return;
                }

                HideDailyRewardPrompt();
                DailyRewardClaimRequested?.Invoke();
            });

            HideDailyRewardPrompt();
        }

        internal void ShowDailyRewardPrompt(DailyReward reward, bool canClaim)
        {
            if (dailyRewardPrompt == null)
            {
                return;
            }

            dailyRewardPromptCanClaim = canClaim;
            HideSettingsPanel();
            HideClearPrompt();
            HideFailPrompt();
            HideExitPrompt();
            HideDailyChallengePrompt();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            HideDepartPrompt();

            if (dailyRewardPromptTitleText != null)
            {
                dailyRewardPromptTitleText.text = Localization.Text("daily_reward_title");
            }

            if (dailyRewardPromptMessageText != null)
            {
                dailyRewardPromptMessageText.text = Localization.Text(
                    canClaim ? "daily_reward_message" : "daily_reward_claimed_message");
            }

            if (dailyRewardText != null)
            {
                dailyRewardText.text = GetDailyRewardText(reward);
                dailyRewardText.color = reward.Type == DailyRewardType.Gold
                    ? UiGoldTextColor
                    : new Color(0.88f, 0.96f, 1f, 0.98f);
            }

            if (dailyRewardIconImage != null)
            {
                var iconSprite = reward.Type == DailyRewardType.Gold
                    ? LoadGoldIconSprite()
                    : LoadResourceSprite(SkipIconResource);
                dailyRewardIconImage.sprite = iconSprite;
                dailyRewardIconImage.enabled = iconSprite != null;
            }

            if (dailyRewardClaimButton != null)
            {
                dailyRewardClaimButton.interactable = canClaim;
            }

            if (dailyRewardClaimBadge != null)
            {
                dailyRewardClaimBadge.gameObject.SetActive(canClaim);
            }

            if (dailyRewardClaimButtonText != null)
            {
                dailyRewardClaimButtonText.text = Localization.Text(
                    canClaim ? "daily_reward_claim" : "daily_reward_claimed");
            }

            dailyRewardPrompt.SetAsLastSibling();
            dailyRewardPrompt.gameObject.SetActive(true);
        }

        internal void HideDailyRewardPrompt()
        {
            if (dailyRewardPrompt != null)
            {
                dailyRewardPrompt.gameObject.SetActive(false);
            }
        }

        private void BuildDailyChallengePrompt()
        {
            dailyChallengePrompt = CreatePromptOverlay("Daily Challenge Overlay");
            var modal = CreatePromptModal(
                dailyChallengePrompt,
                "Daily Challenge Prompt",
                Localization.Text("daily_challenge_title"),
                new Vector2(0.05f, 0.20f),
                new Vector2(0.95f, 0.78f),
                out dailyChallengePromptTitleText);
            if (dailyChallengePromptTitleText != null)
            {
                ApplySettingsTextWeight(dailyChallengePromptTitleText);
                dailyChallengePromptTitleText.fontSize = 49;
                dailyChallengePromptTitleText.resizeTextMaxSize = 49;
                dailyChallengePromptTitleText.resizeTextMinSize = 35;
            }

            CreateOverlayDismissButton("Daily Challenge Outside Close Button", dailyChallengePrompt, HideDailyChallengePrompt);

            dailyChallengePromptMessageText = CreateText("Daily Challenge Message", modal, TextAnchor.MiddleCenter, 28, FontStyle.Normal);
            ApplySettingsTextWeight(dailyChallengePromptMessageText);
            dailyChallengePromptMessageText.color = new Color(0.86f, 0.94f, 1f, 0.94f);
            dailyChallengePromptMessageText.text = Localization.Text("daily_challenge_message");
            SetAnchors(dailyChallengePromptMessageText.rectTransform, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.84f), new Vector2(8f, 0f), new Vector2(-8f, 0f));

            dailyChallengeListContent = CreateRoundedPanel("Daily Challenge List", modal, new Color(0.08f, 0.19f, 0.24f, 0.78f));
            var listShadow = dailyChallengeListContent.gameObject.AddComponent<Shadow>();
            listShadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
            listShadow.effectDistance = new Vector2(0f, -4f);
            SetAnchors(dailyChallengeListContent, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.70f), Vector2.zero, Vector2.zero);

            var closeButton = CreatePromptCloseButton("Daily Challenge Close Button", modal);
            closeButton.onClick.AddListener(HideDailyChallengePrompt);

            HideDailyChallengePrompt();
        }

        internal void ShowDailyChallengePrompt(IReadOnlyList<DailyChallengeStepSnapshot> steps)
        {
            if (dailyChallengePrompt == null)
            {
                return;
            }

            HideSettingsPanel();
            HideClearPrompt();
            HideFailPrompt();
            HideExitPrompt();
            HideDailyRewardPrompt();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            HideDepartPrompt();

            if (dailyChallengePromptTitleText != null)
            {
                dailyChallengePromptTitleText.text = Localization.Text("daily_challenge_title");
            }

            if (dailyChallengePromptMessageText != null)
            {
                dailyChallengePromptMessageText.text = Localization.Text("daily_challenge_message");
            }

            RebuildDailyChallengeRows(steps);
            dailyChallengePrompt.SetAsLastSibling();
            dailyChallengePrompt.gameObject.SetActive(true);
        }

        internal void HideDailyChallengePrompt()
        {
            if (dailyChallengePrompt != null)
            {
                dailyChallengePrompt.gameObject.SetActive(false);
            }
        }

        private void BuildDailyChallengeLoadingOverlay()
        {
            dailyChallengeLoadingOverlay = CreatePromptOverlay("Daily Challenge Loading Overlay");
            dailyChallengeLoadingCanvasGroup = dailyChallengeLoadingOverlay.gameObject.AddComponent<CanvasGroup>();
            dailyChallengeLoadingCanvasGroup.alpha = 1f;
            dailyChallengeLoadingCanvasGroup.blocksRaycasts = true;
            dailyChallengeLoadingCanvasGroup.interactable = true;

            var modal = CreateGameDialog("Daily Challenge Loading Panel", dailyChallengeLoadingOverlay);
            SetAnchors(modal, new Vector2(0.18f, 0.40f), new Vector2(0.82f, 0.60f), Vector2.zero, Vector2.zero);

            var iconRoot = CreateCenteredSquare("Daily Challenge Loading Icon Root", modal, 112f);
            SetAnchors(iconRoot, new Vector2(0.10f, 0.18f), new Vector2(0.32f, 0.82f), Vector2.zero, Vector2.zero);

            var iconObject = new GameObject("Daily Challenge Loading Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(iconRoot, false);
            SetAnchors(iconObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            dailyChallengeLoadingIcon = iconObject.GetComponent<Image>();
            dailyChallengeLoadingIcon.sprite = LoadResourceSprite(DailyChallengeIconResource);
            dailyChallengeLoadingIcon.color = Color.white;
            dailyChallengeLoadingIcon.preserveAspect = true;
            dailyChallengeLoadingIcon.raycastTarget = false;

            dailyChallengeLoadingText = CreateText("Daily Challenge Loading Text", modal, TextAnchor.MiddleLeft, 36, FontStyle.Normal);
            ApplySettingsTextWeight(dailyChallengeLoadingText);
            dailyChallengeLoadingText.color = new Color(0.96f, 0.99f, 1f, 0.98f);
            dailyChallengeLoadingText.text = Localization.Text("daily_challenge_loading");
            SetAnchors(dailyChallengeLoadingText.rectTransform, new Vector2(0.34f, 0.36f), new Vector2(0.92f, 0.80f), new Vector2(0f, 0f), new Vector2(-12f, 0f));

            dailyChallengeLoadingSpinnerRoot = CreateRectTransform("Daily Challenge Loading Spinner", modal);
            SetAnchors(dailyChallengeLoadingSpinnerRoot, new Vector2(0.48f, 0.11f), new Vector2(0.76f, 0.35f), Vector2.zero, Vector2.zero);
            BuildDailyChallengeLoadingSpinnerDots(dailyChallengeLoadingSpinnerRoot);

            HideDailyChallengeLoading();
        }

        internal void ShowDailyChallengeLoading()
        {
            if (dailyChallengeLoadingOverlay == null)
            {
                return;
            }

            HideSettingsPanel();
            HideClearPrompt();
            HideFailPrompt();
            HideExitPrompt();
            HideDailyRewardPrompt();
            HideDailyChallengePrompt();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            HideDepartPrompt();

            if (dailyChallengeLoadingText != null)
            {
                dailyChallengeLoadingText.text = Localization.Text("daily_challenge_loading");
            }

            if (dailyChallengeLoadingCanvasGroup != null)
            {
                dailyChallengeLoadingCanvasGroup.alpha = 1f;
                dailyChallengeLoadingCanvasGroup.blocksRaycasts = true;
                dailyChallengeLoadingCanvasGroup.interactable = true;
            }

            ResetDailyChallengeLoadingSpinner();
            dailyChallengeLoadingOverlay.SetAsLastSibling();
            dailyChallengeLoadingOverlay.gameObject.SetActive(true);
        }

        internal void HideDailyChallengeLoading()
        {
            if (dailyChallengeLoadingOverlay != null)
            {
                dailyChallengeLoadingOverlay.gameObject.SetActive(false);
            }

            if (dailyChallengeLoadingIcon != null)
            {
                dailyChallengeLoadingIcon.transform.localScale = Vector3.one;
            }

            ResetDailyChallengeLoadingSpinner();
        }

        private void UpdateDailyChallengeLoadingOverlay()
        {
            if (dailyChallengeLoadingOverlay == null ||
                !dailyChallengeLoadingOverlay.gameObject.activeSelf ||
                dailyChallengeLoadingIcon == null)
            {
                return;
            }

            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f);
            dailyChallengeLoadingIcon.transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.08f, pulse);
            UpdateDailyChallengeLoadingSpinner();
        }

        private void BuildDailyChallengeLoadingSpinnerDots(Transform parent)
        {
            const float radius = 32f;
            const float dotSize = 12f;
            for (var index = 0; index < dailyChallengeLoadingSpinnerDots.Length; index++)
            {
                var angle = index * Mathf.PI * 2f / dailyChallengeLoadingSpinnerDots.Length;
                var center = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                var dot = CreateRoundedPanel($"Daily Challenge Loading Spinner Dot {index + 1}", parent, UiGoldTextColor);
                SetAnchors(
                    dot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    center - Vector2.one * (dotSize * 0.5f),
                    center + Vector2.one * (dotSize * 0.5f));
                dot.GetComponent<Image>().raycastTarget = false;
                dailyChallengeLoadingSpinnerDots[index] = dot;
            }
        }

        private void ResetDailyChallengeLoadingSpinner()
        {
            if (dailyChallengeLoadingSpinnerRoot != null)
            {
                dailyChallengeLoadingSpinnerRoot.localRotation = Quaternion.identity;
            }

            UpdateDailyChallengeLoadingSpinnerDots(0f);
        }

        private void UpdateDailyChallengeLoadingSpinner()
        {
            var time = Time.unscaledTime;
            if (dailyChallengeLoadingSpinnerRoot != null)
            {
                dailyChallengeLoadingSpinnerRoot.localRotation = Quaternion.Euler(0f, 0f, -time * 240f);
            }

            UpdateDailyChallengeLoadingSpinnerDots(time);
        }

        private void UpdateDailyChallengeLoadingSpinnerDots(float time)
        {
            for (var index = 0; index < dailyChallengeLoadingSpinnerDots.Length; index++)
            {
                var dot = dailyChallengeLoadingSpinnerDots[index];
                if (dot == null)
                {
                    continue;
                }

                var phase = Mathf.Repeat(index / (float)dailyChallengeLoadingSpinnerDots.Length + time * 1.4f, 1f);
                var strength = Mathf.Lerp(0.28f, 1f, phase);
                dot.localScale = Vector3.one * Mathf.Lerp(0.70f, 1.08f, phase);
                var image = dot.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Color.Lerp(new Color(0.40f, 0.82f, 1f, 0.46f), UiGoldTextColor, strength);
                }
            }
        }

        private void RebuildDailyChallengeRows(IReadOnlyList<DailyChallengeStepSnapshot> steps)
        {
            if (dailyChallengeListContent == null)
            {
                return;
            }

            for (var index = dailyChallengeListContent.childCount - 1; index >= 0; index--)
            {
                Destroy(dailyChallengeListContent.GetChild(index).gameObject);
            }

            if (steps == null || steps.Count == 0)
            {
                return;
            }

            var rowHeight = 1f / steps.Count;
            for (var index = 0; index < steps.Count; index++)
            {
                var yMax = 1f - index * rowHeight;
                var yMin = yMax - rowHeight;
                CreateDailyChallengeRow(steps[index], yMin, yMax);
            }
        }

        private void CreateDailyChallengeRow(DailyChallengeStepSnapshot step, float yMin, float yMax)
        {
            var row = CreateRoundedPanel(
                $"Daily Challenge Step {step.StepIndex:00}",
                dailyChallengeListContent,
                GetDailyChallengeStateColor(step.State));
            SetAnchors(row, new Vector2(0.03f, yMin), new Vector2(0.97f, yMax), new Vector2(0f, 7f), new Vector2(0f, -7f));

            var titleText = CreateText($"Daily Challenge Step {step.StepIndex:00} Title", row, TextAnchor.MiddleLeft, 31, FontStyle.Normal);
            ApplySettingsTextWeight(titleText);
            titleText.color = new Color(0.96f, 0.99f, 1f, step.State == DailyChallengeStepState.Locked ? 0.50f : 0.98f);
            titleText.text = Localization.Text($"daily_challenge_step_{step.StepIndex}");
            SetAnchors(titleText.rectTransform, new Vector2(0.05f, 0.56f), new Vector2(0.66f, 0.92f), Vector2.zero, Vector2.zero);

            var detailText = CreateText($"Daily Challenge Step {step.StepIndex:00} Detail", row, TextAnchor.MiddleLeft, 24, FontStyle.Normal);
            ApplySettingsTextWeight(detailText);
            detailText.color = new Color(0.78f, 0.88f, 0.94f, step.State == DailyChallengeStepState.Locked ? 0.44f : 0.86f);
            detailText.text = Localization.Text(
                "daily_challenge_step_detail",
                step.VehicleCount,
                step.ColorCount,
                step.PassengerBatchSize);
            SetAnchors(detailText.rectTransform, new Vector2(0.05f, 0.28f), new Vector2(0.66f, 0.56f), Vector2.zero, Vector2.zero);

            var rewardText = CreateText($"Daily Challenge Step {step.StepIndex:00} Reward", row, TextAnchor.MiddleLeft, 25, FontStyle.Normal);
            ApplySettingsTextWeight(rewardText);
            rewardText.color = step.State == DailyChallengeStepState.Locked
                ? new Color(0.86f, 0.94f, 1f, 0.42f)
                : UiGoldTextColor;
            rewardText.text = GetDailyChallengeRewardText(step.Reward);
            SetAnchors(rewardText.rectTransform, new Vector2(0.05f, 0.06f), new Vector2(0.66f, 0.31f), Vector2.zero, Vector2.zero);

            var button = CreatePromptTextButton(
                $"Daily Challenge Step {step.StepIndex:00} Button",
                row,
                GetDailyChallengeButtonLabel(step.State),
                step.State == DailyChallengeStepState.Cleared ? UiGoldActionColor : UiPrimaryActionColor,
                out var buttonText);
            ApplySettingsTextWeight(buttonText);
            button.interactable = step.State == DailyChallengeStepState.Available ||
                                  step.State == DailyChallengeStepState.Cleared;
            SetAnchors(button.GetComponent<RectTransform>(), new Vector2(0.68f, 0.13f), new Vector2(0.96f, 0.87f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            if (step.State == DailyChallengeStepState.Cleared)
            {
                AddRewardClaimBadge(button, $"Daily Challenge Step {step.StepIndex:00}");
            }

            var stepIndex = step.StepIndex;
            if (step.State == DailyChallengeStepState.Available)
            {
                button.onClick.AddListener(() => DailyChallengeStartRequested?.Invoke(stepIndex));
            }
            else if (step.State == DailyChallengeStepState.Cleared)
            {
                button.onClick.AddListener(() => DailyChallengeRewardClaimRequested?.Invoke(stepIndex));
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
            CreateOverlayDismissButton("Station Unlock Outside Close Button", stationUnlockPrompt, () => CancelRecoveryPrompt(stationUnlockPrompt));

            stationUnlockPromptText = CreateText("Station Unlock Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(stationUnlockPromptText.rectTransform, new Vector2(0f, 0.46f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Station Unlock Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(stationUnlockPrompt));

            stationUnlockConfirmButton = CreatePromptAdButton("Station Unlock Confirm Button", modal, Localization.Text("watch"), UiAdActionColor, out stationUnlockConfirmButtonText);
            SetAnchors(stationUnlockConfirmButton.GetComponent<RectTransform>(), new Vector2(0.09f, 0f), new Vector2(0.49f, 0.40f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            stationUnlockConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideStationUnlockPrompt();
                StationUnlockConfirmed?.Invoke();
            });

            stationUnlockSkipButton = CreatePromptSkipButton("Station Unlock Skip Button", modal, Localization.Text("ad_skip_ticket_none"), UiSecondaryActionColor, out stationUnlockSkipButtonText);
            SetAnchors(stationUnlockSkipButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0f), new Vector2(0.91f, 0.40f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            stationUnlockSkipButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideStationUnlockPrompt();
                StationUnlockSkipConfirmed?.Invoke();
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
            CreateOverlayDismissButton("VIP Teleport Outside Close Button", vipTeleportPrompt, () => CancelRecoveryPrompt(vipTeleportPrompt));

            vipTeleportPromptText = CreateText("VIP Teleport Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(vipTeleportPromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("VIP Teleport Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(vipTeleportPrompt));

            vipTeleportGoldConfirmButton = CreatePromptGoldButton("VIP Teleport Gold Button", modal, Localization.Text("cost_gold", 120), UiGoldActionColor, out vipTeleportGoldButtonText);
            SetAnchors(vipTeleportGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.04f, 0f), new Vector2(0.32f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            vipTeleportGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideVipTeleportPrompt();
                VipTeleportGoldConfirmed?.Invoke();
            });

            vipTeleportConfirmButton = CreatePromptAdButton("VIP Teleport Confirm Button", modal, Localization.Text("watch"), UiAdActionColor, out vipTeleportWatchButtonText);
            SetAnchors(vipTeleportConfirmButton.GetComponent<RectTransform>(), new Vector2(0.34f, 0f), new Vector2(0.62f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            vipTeleportConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideVipTeleportPrompt();
                VipTeleportConfirmed?.Invoke();
            });

            vipTeleportSkipConfirmButton = CreatePromptSkipButton("VIP Teleport Skip Button", modal, Localization.Text("ad_skip_ticket_none"), UiSecondaryActionColor, out vipTeleportSkipButtonText);
            SetAnchors(vipTeleportSkipConfirmButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0f), new Vector2(0.92f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            vipTeleportSkipConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideVipTeleportPrompt();
                VipTeleportSkipConfirmed?.Invoke();
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
            CreateOverlayDismissButton("Mix Shuffle Outside Close Button", mixShufflePrompt, () => CancelRecoveryPrompt(mixShufflePrompt));

            mixShufflePromptText = CreateText("Mix Shuffle Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(mixShufflePromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Mix Shuffle Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(mixShufflePrompt));

            mixShuffleGoldConfirmButton = CreatePromptGoldButton("Mix Shuffle Gold Button", modal, Localization.Text("cost_gold", 90), UiGoldActionColor, out mixShuffleGoldButtonText);
            SetAnchors(mixShuffleGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.04f, 0f), new Vector2(0.32f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            mixShuffleGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleGoldConfirmed?.Invoke();
            });

            mixShuffleConfirmButton = CreatePromptAdButton("Mix Shuffle Confirm Button", modal, Localization.Text("watch"), UiPrimaryActionColor, out mixShuffleWatchButtonText);
            SetAnchors(mixShuffleConfirmButton.GetComponent<RectTransform>(), new Vector2(0.34f, 0f), new Vector2(0.62f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            mixShuffleConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleConfirmed?.Invoke();
            });

            mixShuffleSkipConfirmButton = CreatePromptSkipButton("Mix Shuffle Skip Button", modal, Localization.Text("ad_skip_ticket_none"), UiSecondaryActionColor, out mixShuffleSkipButtonText);
            SetAnchors(mixShuffleSkipConfirmButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0f), new Vector2(0.92f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            mixShuffleSkipConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideMixShufflePrompt();
                MixShuffleSkipConfirmed?.Invoke();
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
            CreateOverlayDismissButton("Depart Outside Close Button", departPrompt, () => CancelRecoveryPrompt(departPrompt));

            departPromptText = CreateText("Depart Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            SetAnchors(departPromptText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.80f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var closeButton = CreatePromptCloseButton("Depart Close Button", modal);
            closeButton.onClick.AddListener(() => CancelRecoveryPrompt(departPrompt));

            departGoldConfirmButton = CreatePromptGoldButton("Depart Gold Button", modal, Localization.Text("cost_gold", 90), UiGoldActionColor, out departGoldButtonText);
            SetAnchors(departGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.04f, 0f), new Vector2(0.32f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            departGoldConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideDepartPrompt();
                DepartGoldConfirmed?.Invoke();
            });

            departConfirmButton = CreatePromptAdButton("Depart Confirm Button", modal, Localization.Text("watch"), UiAdActionColor, out departWatchButtonText);
            SetAnchors(departConfirmButton.GetComponent<RectTransform>(), new Vector2(0.34f, 0f), new Vector2(0.62f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            departConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideDepartPrompt();
                DepartConfirmed?.Invoke();
            });

            departSkipConfirmButton = CreatePromptSkipButton("Depart Skip Button", modal, Localization.Text("ad_skip_ticket_none"), UiSecondaryActionColor, out departSkipButtonText);
            SetAnchors(departSkipConfirmButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0f), new Vector2(0.92f, 0.42f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            departSkipConfirmButton.onClick.AddListener(() =>
            {
                shouldReturnToFailPromptOnRecoveryCancel = false;
                HideDepartPrompt();
                DepartSkipConfirmed?.Invoke();
            });

            HideDepartPrompt();
        }

        private void ApplyStationUnlockPromptState(int lockedSlotsRemaining, bool adReady, int adSkipTickets, bool adInProgress)
        {
            var adsEnabled = RemoteConfigService.AreRewardedAdsEnabled;
            var canUseSkipTicket = adSkipTickets > 0;

            if (stationUnlockPromptText != null)
            {
                if (adInProgress)
                {
                    stationUnlockPromptText.text = Localization.Text("loading_ad");
                }
                else if (adReady)
                {
                    stationUnlockPromptText.text = Localization.Text("watch_ad_stop", lockedSlotsRemaining);
                }
                else if (canUseSkipTicket)
                {
                    stationUnlockPromptText.text = Localization.Text("ad_skip_ticket_stop", lockedSlotsRemaining);
                }
                else
                {
                    stationUnlockPromptText.text = Localization.Text("ad_unavailable_try_later");
                }
            }

            if (stationUnlockConfirmButton != null)
            {
                stationUnlockConfirmButton.gameObject.SetActive(adsEnabled);
                stationUnlockConfirmButton.interactable = adsEnabled && adReady && !adInProgress;
            }

            if (stationUnlockConfirmButtonText != null)
            {
                stationUnlockConfirmButtonText.text = GetRewardedAdButtonLabel(adReady, adInProgress);
            }

            if (stationUnlockSkipButton != null)
            {
                stationUnlockSkipButton.gameObject.SetActive(canUseSkipTicket);
                stationUnlockSkipButton.interactable = canUseSkipTicket && !adInProgress;
            }

            if (stationUnlockSkipButtonText != null)
            {
                stationUnlockSkipButtonText.text = GetAdSkipTicketButtonLabel(adSkipTickets);
            }

            SetStationRecoveryButtonLayout(stationUnlockConfirmButton, stationUnlockSkipButton, adsEnabled, canUseSkipTicket);
        }

        private void ApplyVipTeleportPromptState(
            int usedCount,
            int maxUses,
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            int adSkipTickets,
            bool adReady,
            bool adInProgress)
        {
            var adsEnabled = RemoteConfigService.AreRewardedAdsEnabled;
            var canUseSkipTicket = adSkipTickets > 0;

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
                vipTeleportWatchButtonText.text = GetRewardedAdButtonLabel(adReady, adInProgress);
            }

            if (vipTeleportGoldConfirmButton != null)
            {
                vipTeleportGoldConfirmButton.interactable = canSpendGold && !adInProgress && usedCount < maxUses;
            }

            if (vipTeleportConfirmButton != null)
            {
                vipTeleportConfirmButton.gameObject.SetActive(adsEnabled);
                vipTeleportConfirmButton.interactable = adReady && !adInProgress && usedCount < maxUses;
            }

            if (vipTeleportSkipConfirmButton != null)
            {
                vipTeleportSkipConfirmButton.gameObject.SetActive(canUseSkipTicket);
                vipTeleportSkipConfirmButton.interactable = canUseSkipTicket && !adInProgress && usedCount < maxUses;
            }

            if (vipTeleportSkipButtonText != null)
            {
                vipTeleportSkipButtonText.text = GetAdSkipTicketButtonLabel(adSkipTickets);
            }

            SetRecoveryChoiceButtonLayout(vipTeleportGoldConfirmButton, vipTeleportConfirmButton, vipTeleportSkipConfirmButton, adsEnabled, canUseSkipTicket);
        }

        private void ApplyMixShufflePromptState(
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            int adSkipTickets,
            bool adReady,
            bool adInProgress)
        {
            var adsEnabled = RemoteConfigService.AreRewardedAdsEnabled;
            var canUseSkipTicket = adSkipTickets > 0;

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
                mixShuffleWatchButtonText.text = GetRewardedAdButtonLabel(adReady, adInProgress);
            }

            if (mixShuffleGoldConfirmButton != null)
            {
                mixShuffleGoldConfirmButton.interactable = canSpendGold && !adInProgress;
            }

            if (mixShuffleConfirmButton != null)
            {
                mixShuffleConfirmButton.gameObject.SetActive(adsEnabled);
                mixShuffleConfirmButton.interactable = adReady && !adInProgress;
            }

            if (mixShuffleSkipConfirmButton != null)
            {
                mixShuffleSkipConfirmButton.gameObject.SetActive(canUseSkipTicket);
                mixShuffleSkipConfirmButton.interactable = canUseSkipTicket && !adInProgress;
            }

            if (mixShuffleSkipButtonText != null)
            {
                mixShuffleSkipButtonText.text = GetAdSkipTicketButtonLabel(adSkipTickets);
            }

            SetRecoveryChoiceButtonLayout(mixShuffleGoldConfirmButton, mixShuffleConfirmButton, mixShuffleSkipConfirmButton, adsEnabled, canUseSkipTicket);
        }

        private void ApplyDepartPromptState(
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            int adSkipTickets,
            bool adReady,
            bool adInProgress)
        {
            var adsEnabled = RemoteConfigService.AreRewardedAdsEnabled;
            var canUseSkipTicket = adSkipTickets > 0;

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
                departWatchButtonText.text = GetRewardedAdButtonLabel(adReady, adInProgress);
            }

            if (departGoldConfirmButton != null)
            {
                departGoldConfirmButton.interactable = canSpendGold && !adInProgress;
            }

            if (departConfirmButton != null)
            {
                departConfirmButton.gameObject.SetActive(adsEnabled);
                departConfirmButton.interactable = adReady && !adInProgress;
            }

            if (departSkipConfirmButton != null)
            {
                departSkipConfirmButton.gameObject.SetActive(canUseSkipTicket);
                departSkipConfirmButton.interactable = canUseSkipTicket && !adInProgress;
            }

            if (departSkipButtonText != null)
            {
                departSkipButtonText.text = GetAdSkipTicketButtonLabel(adSkipTickets);
            }

            SetRecoveryChoiceButtonLayout(departGoldConfirmButton, departConfirmButton, departSkipConfirmButton, adsEnabled, canUseSkipTicket);
        }

        private static void SetStationRecoveryButtonLayout(Button adButton, Button skipButton, bool showAdButton, bool showSkipButton)
        {
            if (showAdButton && showSkipButton)
            {
                SetButtonAnchors(adButton, new Vector2(0.09f, 0f), new Vector2(0.49f, 0.40f));
                SetButtonAnchors(skipButton, new Vector2(0.51f, 0f), new Vector2(0.91f, 0.40f));
                return;
            }

            if (showAdButton)
            {
                SetButtonAnchors(adButton, new Vector2(0.18f, 0f), new Vector2(0.82f, 0.40f));
            }
            else if (showSkipButton)
            {
                SetButtonAnchors(skipButton, new Vector2(0.18f, 0f), new Vector2(0.82f, 0.40f));
            }
        }

        private static void SetRecoveryChoiceButtonLayout(Button goldButton, Button adButton, Button skipButton, bool showAdButton, bool showSkipButton)
        {
            if (showAdButton && showSkipButton)
            {
                SetButtonAnchors(goldButton, new Vector2(0.04f, 0f), new Vector2(0.32f, 0.42f));
                SetButtonAnchors(adButton, new Vector2(0.34f, 0f), new Vector2(0.62f, 0.42f));
                SetButtonAnchors(skipButton, new Vector2(0.64f, 0f), new Vector2(0.92f, 0.42f));
                return;
            }

            if (showAdButton)
            {
                SetButtonAnchors(goldButton, new Vector2(0.09f, 0f), new Vector2(0.49f, 0.42f));
                SetButtonAnchors(adButton, new Vector2(0.51f, 0f), new Vector2(0.91f, 0.42f));
                return;
            }

            if (showSkipButton)
            {
                SetButtonAnchors(goldButton, new Vector2(0.09f, 0f), new Vector2(0.49f, 0.42f));
                SetButtonAnchors(skipButton, new Vector2(0.51f, 0f), new Vector2(0.91f, 0.42f));
                return;
            }

            SetButtonAnchors(goldButton, new Vector2(0.18f, 0f), new Vector2(0.82f, 0.42f));
        }

        private static void SetButtonAnchors(Button button, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (button == null)
            {
                return;
            }

            SetAnchors(
                button.GetComponent<RectTransform>(),
                anchorMin,
                anchorMax,
                new Vector2(6f, 16f),
                new Vector2(-6f, -12f));
        }

        private static string GetRewardedAdButtonLabel(bool adReady, bool adInProgress)
        {
            if (adInProgress)
            {
                return Localization.Text("loading");
            }

            return adReady ? Localization.Text("watch") : Localization.Text("ad_unavailable");
        }

        private static string GetAdSkipTicketButtonLabel(int tickets)
        {
            return tickets > 0
                ? Localization.Text("ad_skip_ticket_button", tickets)
                : Localization.Text("ad_skip_ticket_none");
        }

        private static string GetDailyChallengeRewardText(DailyChallengeReward reward)
        {
            if (reward.Gold > 0 && reward.AdSkipTickets > 0)
            {
                return Localization.Text("daily_challenge_reward_gold_skip", reward.Gold, reward.AdSkipTickets);
            }

            if (reward.Gold > 0)
            {
                return Localization.Text("daily_reward_gold", reward.Gold);
            }

            if (reward.AdSkipTickets > 0)
            {
                return Localization.Text("daily_reward_skip_ticket", reward.AdSkipTickets);
            }

            return string.Empty;
        }

        private static string GetDailyChallengeButtonLabel(DailyChallengeStepState state)
        {
            switch (state)
            {
                case DailyChallengeStepState.Available:
                    return Localization.Text("daily_challenge_start");
                case DailyChallengeStepState.Cleared:
                    return Localization.Text("daily_challenge_claim_reward");
                case DailyChallengeStepState.RewardClaimed:
                    return Localization.Text("daily_reward_claimed");
                default:
                    return Localization.Text("locked");
            }
        }

        private static Color GetDailyChallengeStateColor(DailyChallengeStepState state)
        {
            switch (state)
            {
                case DailyChallengeStepState.Available:
                    return new Color(0.13f, 0.30f, 0.36f, 0.90f);
                case DailyChallengeStepState.Cleared:
                    return new Color(0.30f, 0.34f, 0.18f, 0.92f);
                case DailyChallengeStepState.RewardClaimed:
                    return new Color(0.12f, 0.25f, 0.24f, 0.78f);
                default:
                    return new Color(0.10f, 0.17f, 0.20f, 0.62f);
            }
        }

        private static string GetDailyRewardText(DailyReward reward)
        {
            return reward.Type == DailyRewardType.Gold
                ? Localization.Text("daily_reward_gold", reward.Amount)
                : Localization.Text("daily_reward_skip_ticket", reward.Amount);
        }
    }
}
