using System;
using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController : MonoBehaviour
    {
        private const string FeedbackEmailAddress = "support@buspuzzle.app";
        private const string PrivacyPolicyUrl = "https://buspuzzle.app/privacy";
        private const string VipBoosterIconResource = "UI/Boosters/booster_vip";
        private const string MixBoosterIconResource = "UI/Boosters/booster_mix";
        private const string NextButtonIconResource = "UI/Boosters/NEXT";
        private const string GoldIconResource = "UI/Boosters/gold";
        private const float HeaderIconSize = 117f;
        private const int HeaderStageFontSize = 60;
        private const int HeaderGoldFontSize = 34;
        private const float BoosterIconSize = 144f;
        private static readonly Color UiOverlayColor = new Color(0.03f, 0.05f, 0.07f, 0.56f);
        private static readonly Color UiPanelColor = new Color(0.11f, 0.15f, 0.18f, 0.96f);
        private static readonly Color UiPanelAccentColor = new Color(0.18f, 0.56f, 0.74f, 0.34f);
        private static readonly Color UiPanelStrokeColor = new Color(0.56f, 0.74f, 0.82f, 0.10f);
        private static readonly Color UiHeaderTextColor = new Color(0.09f, 0.13f, 0.16f, 0.96f);
        private static readonly Color UiPrimaryActionColor = new Color(0.12f, 0.55f, 0.75f);
        private static readonly Color UiGoldActionColor = new Color(0.88f, 0.57f, 0.10f);
        private static readonly Color UiDangerActionColor = new Color(0.72f, 0.27f, 0.20f);
        private static readonly Color UiSecondaryActionColor = new Color(0.27f, 0.35f, 0.41f);
        private static readonly Color UiAdActionColor = new Color(0.15f, 0.55f, 0.38f);
        private static readonly Color UiBoosterGoldColor = new Color(0.92f, 0.62f, 0.10f);
        private static readonly Color UiBoosterBlueColor = new Color(0.16f, 0.48f, 0.70f);

        private RectTransform safeAreaRoot;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private Text levelText;
        private Text goldText;
        private Text statusText;
        private Text remainingText;
        private Text stationText;
        private Text vipBadgeText;
        private Button menuButton;
        private Button settingsButton;
        private Button vipButton;
        private Button mixButton;
        private Button nextButton;
        private Text nextButtonText;
        private RectTransform settingsPanel;
        private RectTransform clearPrompt;
        private Text clearPromptText;
        private Text clearRewardText;
        private RectTransform failPrompt;
        private Text failPromptText;
        private Text failHintText;
        private Button failStationUnlockButton;
        private Text failStationUnlockButtonText;
        private Button failVipButton;
        private Text failVipButtonText;
        private Button failMixButton;
        private Text failMixButtonText;
        private RectTransform exitPrompt;
        private Text exitPromptText;
        private Toggle effectSoundToggle;
        private Toggle mainSoundToggle;
        private Toggle vibrationToggle;
        private RectTransform stationUnlockPrompt;
        private Text stationUnlockPromptText;
        private Button stationUnlockConfirmButton;
        private RectTransform vipTeleportPrompt;
        private Text vipTeleportPromptText;
        private Button vipTeleportConfirmButton;
        private RectTransform mixShufflePrompt;
        private Text mixShufflePromptText;
        private Button mixShuffleGoldConfirmButton;
        private Text mixShuffleGoldButtonText;
        private Button mixShuffleConfirmButton;
        private Text mixShuffleWatchButtonText;
        private static Sprite roundedPanelSprite;
        private static Sprite circleSprite;
        private static Sprite gearIconSprite;
        private bool shouldReturnToFailPromptOnRecoveryCancel;

        public event Action RestartRequested;
        public event Action NextLevelRequested;
        public event Action ExitConfirmed;
        public event Action StationUnlockRequested;
        public event Action StationUnlockConfirmed;
        public event Action VipTeleportRequested;
        public event Action VipTeleportConfirmed;
        public event Action MixShuffleRequested;
        public event Action MixShuffleGoldConfirmed;
        public event Action MixShuffleConfirmed;
        public event Action RecoveryPromptCancelled;

        public static GameUiController CreateDefault()
        {
            EnsureEventSystem();

            var uiObject = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameUiController));
            var canvas = uiObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = uiObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.55f;

            var controller = uiObject.GetComponent<GameUiController>();
            controller.BuildLayout();
            return controller;
        }

        private void LateUpdate()
        {
            UpdateSafeArea();
        }

        public void SetLevel(int levelNumber, int totalLevels)
        {
            if (levelText != null)
            {
                levelText.text = $"Stage {levelNumber:00}";
            }
        }

        public void SetGold(int gold)
        {
            if (goldText != null)
            {
                goldText.text = FormatCompactGold(gold);
            }
        }

        public void SetRemaining(int remainingCount)
        {
            if (remainingText != null)
            {
                remainingText.text = $"Units {remainingCount}";
            }
        }

        public void SetStationSlots(int occupiedSlots, int totalSlots)
        {
            if (stationText != null)
            {
                stationText.text = $"Stops {occupiedSlots}/{totalSlots}";
            }
        }

        public void SetStationUnlock(int lockedSlotsRemaining, bool canUnlock, bool adReady, bool adInProgress)
        {
            if (stationUnlockPrompt == null)
            {
                return;
            }

            if (!canUnlock)
            {
                HideStationUnlockPrompt();
                return;
            }

            if (!stationUnlockPrompt.gameObject.activeSelf)
            {
                return;
            }

            ApplyStationUnlockPromptState(lockedSlotsRemaining, adReady, adInProgress);
        }

        public void SetVipTeleport(
            int remainingAds,
            bool hasTicket,
            bool isSelectionMode,
            bool canRequest,
            bool adReady,
            bool adInProgress)
        {
            if (vipButton != null)
            {
                if (vipBadgeText != null)
                {
                    vipBadgeText.text = isSelectionMode
                        ? "Cancel"
                        : hasTicket
                            ? "Pick"
                            : adInProgress
                                ? "..."
                                : remainingAds.ToString();
                }

                vipButton.interactable = isSelectionMode || hasTicket || canRequest;
            }

            if (vipTeleportPrompt == null || !vipTeleportPrompt.gameObject.activeSelf)
            {
                return;
            }

            ApplyVipTeleportPromptState(remainingAds, adReady, adInProgress);
        }

        public void SetMixShuffle(
            bool canRequest,
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            bool adReady,
            bool adInProgress)
        {
            if (mixButton != null)
            {
                mixButton.interactable = canRequest;
            }

            if (mixShufflePrompt == null || !mixShufflePrompt.gameObject.activeSelf)
            {
                return;
            }

            ApplyMixShufflePromptState(goldBalance, goldCost, canSpendGold, adReady, adInProgress);
        }

        public void ShowStationUnlockPrompt(int lockedSlotsRemaining, bool adReady, bool adInProgress)
        {
            if (stationUnlockPrompt == null || lockedSlotsRemaining <= 0)
            {
                return;
            }

            HideSettingsPanel();
            HideFailPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            stationUnlockPrompt.gameObject.SetActive(true);
            ApplyStationUnlockPromptState(lockedSlotsRemaining, adReady, adInProgress);
        }

        public void HideStationUnlockPrompt()
        {
            if (stationUnlockPrompt != null)
            {
                stationUnlockPrompt.gameObject.SetActive(false);
            }
        }

        public void ShowVipTeleportPrompt(int remainingAds, bool adReady, bool adInProgress)
        {
            if (vipTeleportPrompt == null || remainingAds <= 0)
            {
                return;
            }

            HideSettingsPanel();
            HideFailPrompt();
            HideStationUnlockPrompt();
            HideMixShufflePrompt();
            vipTeleportPrompt.gameObject.SetActive(true);
            ApplyVipTeleportPromptState(remainingAds, adReady, adInProgress);
        }

        public void HideVipTeleportPrompt()
        {
            if (vipTeleportPrompt != null)
            {
                vipTeleportPrompt.gameObject.SetActive(false);
            }
        }

        public void ShowMixShufflePrompt(
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            bool adReady,
            bool adInProgress)
        {
            if (mixShufflePrompt == null)
            {
                return;
            }

            HideSettingsPanel();
            HideFailPrompt();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            mixShufflePrompt.gameObject.SetActive(true);
            ApplyMixShufflePromptState(goldBalance, goldCost, canSpendGold, adReady, adInProgress);
        }

        public void HideMixShufflePrompt()
        {
            if (mixShufflePrompt != null)
            {
                mixShufflePrompt.gameObject.SetActive(false);
            }
        }

        public void ShowPlaying(string levelName)
        {
            statusText.text = string.Empty;
            SetRestartButtonInteractable(true);
            shouldReturnToFailPromptOnRecoveryCancel = false;
            HideSettingsPanel();
            HideClearPrompt();
            HideFailPrompt();
            HideExitPrompt();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            if (nextButton != null)
            {
                nextButton.interactable = false;
            }
        }

        public void ShowInvalid(string message)
        {
            statusText.text = message;
        }

        public void ShowClear(int levelNumber, bool hasNextLevel, int goldReward)
        {
            statusText.text = hasNextLevel ? "Clear" : "All Clear";
            SetRestartButtonInteractable(false);
            shouldReturnToFailPromptOnRecoveryCancel = false;
            SetStationUnlock(0, false, false, false);
            SetVipTeleport(0, false, false, false, false, false);
            SetMixShuffle(false, 0, 0, false, false, false);
            HideSettingsPanel();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            HideFailPrompt();
            HideExitPrompt();
            ShowClearPrompt(levelNumber, hasNextLevel, goldReward);
        }

        public void ShowFailed(bool canUnlockStationSlot, bool canVipTeleport, bool canMixShuffle)
        {
            statusText.text = "Failed";
            SetRestartButtonInteractable(true);
            shouldReturnToFailPromptOnRecoveryCancel = false;
            HideSettingsPanel();
            HideClearPrompt();
            HideExitPrompt();
            if (nextButton != null)
            {
                nextButton.interactable = false;
            }

            SetStationUnlock(0, false, false, false);
            SetVipTeleport(0, false, false, false, false, false);
            SetMixShuffle(false, 0, 0, false, false, false);
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            ShowFailPrompt(canUnlockStationSlot, canVipTeleport, canMixShuffle);
        }

        public void ShowExitPrompt()
        {
            if (exitPrompt == null)
            {
                return;
            }

            HideSettingsPanel();
            HideFailPrompt();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            HideMixShufflePrompt();
            exitPrompt.gameObject.SetActive(true);
        }

        private void SetRestartButtonInteractable(bool isInteractable)
        {
            if (menuButton != null)
            {
                menuButton.interactable = isInteractable;
            }
        }
    }
}
