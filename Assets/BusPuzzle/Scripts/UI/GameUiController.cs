using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed class GameUiController : MonoBehaviour
    {
        private const string FeedbackEmailAddress = "support@buspuzzle.app";
        private const string PrivacyPolicyUrl = "https://buspuzzle.app/privacy";
        private const string HeaderMenuIconResource = "UI/Boosters/re";
        private const string HeaderSettingsIconResource = "UI/Boosters/set";
        private const string VipBoosterIconResource = "UI/Boosters/booster_vip";
        private const string MixBoosterIconResource = "UI/Boosters/booster_mix";
        private const float HeaderIconSize = 78f;
        private const float BoosterIconSize = 144f;

        private Text levelText;
        private Text statusText;
        private Text remainingText;
        private Text stationText;
        private Text vipBadgeText;
        private Button menuButton;
        private Button settingsButton;
        private Button restartButton;
        private Button vipButton;
        private Button mixButton;
        private Button nextButton;
        private RectTransform menuPanel;
        private RectTransform settingsPanel;
        private RectTransform clearPrompt;
        private Text clearPromptText;
        private Toggle effectSoundToggle;
        private Toggle mainSoundToggle;
        private Toggle vibrationToggle;
        private RectTransform stationUnlockPrompt;
        private Text stationUnlockPromptText;
        private Button stationUnlockConfirmButton;
        private RectTransform vipTeleportPrompt;
        private Text vipTeleportPromptText;
        private Button vipTeleportConfirmButton;

        public event Action RestartRequested;
        public event Action HomeRequested;
        public event Action NextLevelRequested;
        public event Action StationUnlockConfirmed;
        public event Action VipTeleportRequested;
        public event Action VipTeleportConfirmed;

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

        public void SetLevel(int levelNumber, int totalLevels)
        {
            if (levelText != null)
            {
                levelText.text = $"Stage {levelNumber:00}";
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

        public void ShowStationUnlockPrompt(int lockedSlotsRemaining, bool adReady, bool adInProgress)
        {
            if (stationUnlockPrompt == null || lockedSlotsRemaining <= 0)
            {
                return;
            }

            HideMenuPanel();
            HideSettingsPanel();
            HideVipTeleportPrompt();
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

            HideMenuPanel();
            HideSettingsPanel();
            HideStationUnlockPrompt();
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

        public void ShowPlaying(string levelName)
        {
            statusText.text = string.Empty;
            HideMenuPanel();
            HideSettingsPanel();
            HideClearPrompt();
            if (nextButton != null)
            {
                nextButton.interactable = false;
            }
        }

        public void ShowInvalid(string message)
        {
            statusText.text = message;
        }

        public void ShowClear(int levelNumber, bool hasNextLevel)
        {
            statusText.text = hasNextLevel ? "Clear" : "All Clear";
            SetStationUnlock(0, false, false, false);
            SetVipTeleport(0, false, false, false, false, false);
            HideMenuPanel();
            HideSettingsPanel();
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
            ShowClearPrompt(levelNumber, hasNextLevel);
        }

        public void ShowFailed()
        {
            statusText.text = "Failed";
            HideMenuPanel();
            HideSettingsPanel();
            HideClearPrompt();
            if (nextButton != null)
            {
                nextButton.interactable = false;
            }

            SetStationUnlock(0, false, false, false);
            SetVipTeleport(0, false, false, false, false, false);
            HideStationUnlockPrompt();
            HideVipTeleportPrompt();
        }

        private void BuildLayout()
        {
            var topPanel = CreatePanel("Top Bar", transform, new Color(1f, 1f, 1f, 0f));
            topPanel.GetComponent<Image>().raycastTarget = false;
            SetAnchors(topPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -96f), new Vector2(-16f, -8f));

            var headerIconColor = new Color(0.07f, 0.10f, 0.14f, 0.90f);
            menuButton = CreateHeaderIconButton("Header Menu Button", topPanel, HeaderMenuIconResource, "↩", headerIconColor);
            SetAnchors(menuButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.22f, 1f), new Vector2(0f, 0f), new Vector2(-8f, 0f));
            menuButton.onClick.AddListener(ToggleMenuPanel);

            levelText = CreateText("Stage Text", topPanel, TextAnchor.MiddleCenter, 40, FontStyle.Bold);
            levelText.color = headerIconColor;
            SetAnchors(levelText.rectTransform, new Vector2(0.24f, 0f), new Vector2(0.76f, 1f), new Vector2(8f, 2f), new Vector2(-8f, -2f));

            settingsButton = CreateHeaderIconButton("Header Settings Button", topPanel, HeaderSettingsIconResource, "⚙", headerIconColor);
            SetAnchors(settingsButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(0f, 0f));
            settingsButton.onClick.AddListener(ToggleSettingsPanel);

            statusText = CreateText("Status Text", transform, TextAnchor.MiddleCenter, 30, FontStyle.Bold);
            SetAnchors(statusText.rectTransform, new Vector2(0.18f, 1f), new Vector2(0.82f, 1f), new Vector2(0f, -126f), new Vector2(0f, -84f));

            var boosterRow = CreateRectTransform("Booster Row", transform);
            SetAnchors(boosterRow, new Vector2(0.24f, 0f), new Vector2(0.76f, 0f), new Vector2(0f, 18f), new Vector2(0f, 170f));

            vipButton = CreateBoosterButton("VIP Button", boosterRow, VipBoosterIconResource, "VIP", new Color(0.82f, 0.58f, 0.08f), true, out vipBadgeText);
            SetAnchors(vipButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.50f, 1f), new Vector2(0f, 0f), new Vector2(-18f, 0f));
            vipButton.onClick.AddListener(() => VipTeleportRequested?.Invoke());

            mixButton = CreateBoosterButton("Mix Button", boosterRow, MixBoosterIconResource, "Mix", new Color(0.21f, 0.46f, 0.66f), false, out _);
            SetAnchors(mixButton.GetComponent<RectTransform>(), new Vector2(0.50f, 0f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(0f, 0f));
            mixButton.interactable = false;

            BuildMenuPanel();
            BuildSettingsPanel();
            BuildClearPrompt();
            BuildStationUnlockPrompt();
            BuildVipTeleportPrompt();
        }

        private void BuildMenuPanel()
        {
            menuPanel = CreatePanel("Header Menu Panel", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(menuPanel, new Vector2(0.04f, 0.78f), new Vector2(0.34f, 0.92f), Vector2.zero, Vector2.zero);

            restartButton = CreateButton("Menu Restart Button", menuPanel, "Restart", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(restartButton.GetComponent<RectTransform>(), new Vector2(0f, 0.52f), new Vector2(1f, 1f), new Vector2(14f, 8f), new Vector2(-14f, -10f));
            restartButton.onClick.AddListener(() =>
            {
                HideMenuPanel();
                RestartRequested?.Invoke();
            });

            var homeButton = CreateButton("Menu Home Button", menuPanel, "Home", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(homeButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(14f, 10f), new Vector2(-14f, -8f));
            homeButton.onClick.AddListener(() =>
            {
                HideMenuPanel();
                HomeRequested?.Invoke();
            });

            HideMenuPanel();
        }

        private void BuildSettingsPanel()
        {
            settingsPanel = CreatePanel("Settings Panel", transform, new Color(0.08f, 0.10f, 0.13f, 0.96f));
            SetAnchors(settingsPanel, new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.70f), Vector2.zero, Vector2.zero);

            var title = CreateText("Settings Title", settingsPanel, TextAnchor.MiddleCenter, 38, FontStyle.Bold);
            title.text = "Settings";
            SetAnchors(title.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            effectSoundToggle = CreateToggle("Effect Sound Toggle", settingsPanel, "Effect Sound", UserPreferences.EffectSoundEnabled);
            SetAnchors(effectSoundToggle.GetComponent<RectTransform>(), new Vector2(0f, 0.68f), new Vector2(1f, 0.82f), new Vector2(28f, 4f), new Vector2(-28f, -4f));
            effectSoundToggle.onValueChanged.AddListener(value => UserPreferences.EffectSoundEnabled = value);

            mainSoundToggle = CreateToggle("Main Sound Toggle", settingsPanel, "Main Sound", UserPreferences.MainSoundEnabled);
            SetAnchors(mainSoundToggle.GetComponent<RectTransform>(), new Vector2(0f, 0.52f), new Vector2(1f, 0.66f), new Vector2(28f, 4f), new Vector2(-28f, -4f));
            mainSoundToggle.onValueChanged.AddListener(value => UserPreferences.MainSoundEnabled = value);

            vibrationToggle = CreateToggle("Vibration Toggle", settingsPanel, "Vibration", UserPreferences.VibrationEnabled);
            SetAnchors(vibrationToggle.GetComponent<RectTransform>(), new Vector2(0f, 0.36f), new Vector2(1f, 0.50f), new Vector2(28f, 4f), new Vector2(-28f, -4f));
            vibrationToggle.onValueChanged.AddListener(value => UserPreferences.VibrationEnabled = value);

            var feedbackButton = CreateButton("Feedback Button", settingsPanel, "Feedback", new Color(0.21f, 0.46f, 0.66f));
            SetAnchors(feedbackButton.GetComponent<RectTransform>(), new Vector2(0f, 0.18f), new Vector2(0.48f, 0.34f), new Vector2(24f, 6f), new Vector2(-8f, -4f));
            feedbackButton.onClick.AddListener(OpenFeedbackMail);

            var privacyButton = CreateButton("Privacy Button", settingsPanel, "Privacy", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(privacyButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.18f), new Vector2(1f, 0.34f), new Vector2(8f, 6f), new Vector2(-24f, -4f));
            privacyButton.onClick.AddListener(OpenPrivacyPolicy);

            var closeButton = CreateButton("Settings Close Button", settingsPanel, "Close", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.16f), new Vector2(24f, 10f), new Vector2(-24f, -8f));
            closeButton.onClick.AddListener(HideSettingsPanel);

            HideSettingsPanel();
        }

        private void ToggleMenuPanel()
        {
            if (menuPanel == null)
            {
                return;
            }

            var shouldShow = !menuPanel.gameObject.activeSelf;
            HideSettingsPanel();
            menuPanel.gameObject.SetActive(shouldShow);
        }

        private void HideMenuPanel()
        {
            if (menuPanel != null)
            {
                menuPanel.gameObject.SetActive(false);
            }
        }

        private void ToggleSettingsPanel()
        {
            if (settingsPanel == null)
            {
                return;
            }

            var shouldShow = !settingsPanel.gameObject.activeSelf;
            HideMenuPanel();
            settingsPanel.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                RefreshSettingsToggles();
            }
        }

        private void HideSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
            }
        }

        private void RefreshSettingsToggles()
        {
            SetToggleValueWithoutNotify(effectSoundToggle, UserPreferences.EffectSoundEnabled);
            SetToggleValueWithoutNotify(mainSoundToggle, UserPreferences.MainSoundEnabled);
            SetToggleValueWithoutNotify(vibrationToggle, UserPreferences.VibrationEnabled);
        }

        private static void SetToggleValueWithoutNotify(Toggle toggle, bool value)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.SetIsOnWithoutNotify(value);
        }

        private static void OpenFeedbackMail()
        {
            var subject = Uri.EscapeDataString("Bus Puzzle Feedback");
            var body = Uri.EscapeDataString("Please write your feedback here.");
            Application.OpenURL($"mailto:{FeedbackEmailAddress}?subject={subject}&body={body}");
        }

        private static void OpenPrivacyPolicy()
        {
            Application.OpenURL(PrivacyPolicyUrl);
        }

        private void BuildClearPrompt()
        {
            clearPrompt = CreatePanel("Clear Prompt", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(clearPrompt, new Vector2(0.12f, 0.37f), new Vector2(0.88f, 0.58f), Vector2.zero, Vector2.zero);

            clearPromptText = CreateText("Clear Prompt Text", clearPrompt, TextAnchor.MiddleCenter, 36, FontStyle.Bold);
            SetAnchors(clearPromptText.rectTransform, new Vector2(0f, 0.43f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var homeButton = CreateButton("Clear Home Button", clearPrompt, "Home", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(homeButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.43f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            homeButton.onClick.AddListener(() =>
            {
                HideClearPrompt();
                HomeRequested?.Invoke();
            });

            nextButton = CreateButton("Clear Next Button", clearPrompt, "Next", new Color(0.12f, 0.42f, 0.78f));
            SetAnchors(nextButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.43f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            nextButton.onClick.AddListener(() =>
            {
                HideClearPrompt();
                NextLevelRequested?.Invoke();
            });

            HideClearPrompt();
        }

        private void ShowClearPrompt(int levelNumber, bool hasNextLevel)
        {
            if (clearPrompt == null)
            {
                return;
            }

            clearPrompt.gameObject.SetActive(true);
            if (clearPromptText != null)
            {
                clearPromptText.text = $"Level {levelNumber} Clear";
            }

            if (nextButton != null)
            {
                nextButton.interactable = hasNextLevel;
            }
        }

        private void HideClearPrompt()
        {
            if (clearPrompt != null)
            {
                clearPrompt.gameObject.SetActive(false);
            }
        }

        private void BuildStationUnlockPrompt()
        {
            stationUnlockPrompt = CreatePanel("Station Unlock Prompt", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(stationUnlockPrompt, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.56f), Vector2.zero, Vector2.zero);

            stationUnlockPromptText = CreateText("Station Unlock Prompt Text", stationUnlockPrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(stationUnlockPromptText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Station Unlock Cancel Button", stationUnlockPrompt, "Cancel", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(HideStationUnlockPrompt);

            stationUnlockConfirmButton = CreateButton("Station Unlock Confirm Button", stationUnlockPrompt, "Watch", new Color(0.10f, 0.48f, 0.30f));
            SetAnchors(stationUnlockConfirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            stationUnlockConfirmButton.onClick.AddListener(() =>
            {
                HideStationUnlockPrompt();
                StationUnlockConfirmed?.Invoke();
            });

            HideStationUnlockPrompt();
        }

        private void BuildVipTeleportPrompt()
        {
            vipTeleportPrompt = CreatePanel("VIP Teleport Prompt", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(vipTeleportPrompt, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.56f), Vector2.zero, Vector2.zero);

            vipTeleportPromptText = CreateText("VIP Teleport Prompt Text", vipTeleportPrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(vipTeleportPromptText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("VIP Teleport Cancel Button", vipTeleportPrompt, "Cancel", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(HideVipTeleportPrompt);

            vipTeleportConfirmButton = CreateButton("VIP Teleport Confirm Button", vipTeleportPrompt, "Watch", new Color(0.82f, 0.58f, 0.08f));
            SetAnchors(vipTeleportConfirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            vipTeleportConfirmButton.onClick.AddListener(() =>
            {
                HideVipTeleportPrompt();
                VipTeleportConfirmed?.Invoke();
            });

            HideVipTeleportPrompt();
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

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = color;
            return panelObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateRectTransform(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static Text CreateText(string name, Transform parent, TextAnchor alignment, int fontSize, FontStyle fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.alignment = alignment;
            text.color = Color.white;
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, fontSize - 12);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color baseColor)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = baseColor;

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
            colors.disabledColor = new Color(0.20f, 0.22f, 0.25f, 0.55f);
            button.colors = colors;

            var labelText = CreateText($"{name} Label", buttonObject.transform, TextAnchor.MiddleCenter, 30, FontStyle.Bold);
            labelText.text = label;
            SetAnchors(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f));

            return button;
        }

        private static Button CreateHeaderIconButton(
            string name,
            Transform parent,
            string iconResourcePath,
            string fallbackLabel,
            Color fallbackColor)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var hitArea = buttonObject.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            var iconRoot = CreateCenteredSquare($"{name} Icon Root", buttonObject.transform, HeaderIconSize);
            var iconObject = new GameObject($"{name} Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(iconRoot, false);

            var iconRect = iconObject.GetComponent<RectTransform>();
            SetAnchors(iconRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var iconImage = iconObject.GetComponent<Image>();
            var iconSprite = Resources.Load<Sprite>(iconResourcePath);
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            else
            {
                iconImage.color = new Color(1f, 1f, 1f, 0f);
                iconImage.raycastTarget = false;

                var fallbackText = CreateText($"{name} Fallback Label", iconRoot, TextAnchor.MiddleCenter, 48, FontStyle.Bold);
                fallbackText.text = fallbackLabel;
                fallbackText.color = fallbackColor;
                SetAnchors(fallbackText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = iconSprite != null ? iconImage : hitArea;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.48f);
            button.colors = colors;
            return button;
        }

        private static Button CreateBoosterButton(
            string name,
            Transform parent,
            string iconResourcePath,
            string fallbackLabel,
            Color fallbackColor,
            bool createBadge,
            out Text badgeText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var hitArea = buttonObject.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            var iconRoot = CreateCenteredSquare($"{name} Icon Root", buttonObject.transform, BoosterIconSize);
            var iconSprite = Resources.Load<Sprite>(iconResourcePath);
            if (iconSprite != null)
            {
                var shadowObject = new GameObject($"{name} Icon Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                shadowObject.transform.SetParent(iconRoot, false);

                var shadowRect = shadowObject.GetComponent<RectTransform>();
                SetAnchors(shadowRect, Vector2.zero, Vector2.one, new Vector2(0f, -6f), new Vector2(0f, -6f));

                var shadowImage = shadowObject.GetComponent<Image>();
                shadowImage.sprite = iconSprite;
                shadowImage.color = new Color(0f, 0f, 0f, 0.22f);
                shadowImage.preserveAspect = true;
                shadowImage.raycastTarget = false;
            }

            var iconObject = new GameObject($"{name} Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(iconRoot, false);

            var iconRect = iconObject.GetComponent<RectTransform>();
            SetAnchors(iconRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var iconImage = iconObject.GetComponent<Image>();
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
            }
            else
            {
                iconImage.color = fallbackColor;

                var fallbackText = CreateText($"{name} Fallback Label", iconRoot, TextAnchor.MiddleCenter, 30, FontStyle.Bold);
                fallbackText.text = fallbackLabel;
                SetAnchors(fallbackText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            }

            iconImage.raycastTarget = false;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = iconImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;

            badgeText = null;
            if (!createBadge)
            {
                return button;
            }

            var badgeObject = new GameObject($"{name} Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(iconRoot, false);
            var badgeRect = badgeObject.GetComponent<RectTransform>();
            SetAnchors(badgeRect, new Vector2(0.48f, 0.02f), new Vector2(0.98f, 0.31f), Vector2.zero, Vector2.zero);

            var badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.color = new Color(0.06f, 0.08f, 0.10f, 0.82f);
            badgeImage.raycastTarget = false;

            badgeText = CreateText($"{name} Badge Text", badgeObject.transform, TextAnchor.MiddleCenter, 20, FontStyle.Bold);
            badgeText.text = string.Empty;
            badgeText.resizeTextMinSize = 12;
            SetAnchors(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 1f), new Vector2(-4f, -1f));

            return button;
        }

        private static RectTransform CreateCenteredSquare(string name, Transform parent, float size)
        {
            var rectTransform = CreateRectTransform(name, parent);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(size, size);
            return rectTransform;
        }

        private static Toggle CreateToggle(string name, Transform parent, string label, bool initialValue)
        {
            var toggleObject = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            var toggle = toggleObject.GetComponent<Toggle>();

            var backgroundObject = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(toggleObject.transform, false);
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            SetAnchors(backgroundRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -24f), new Vector2(48f, 24f));
            var backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = new Color(0.18f, 0.21f, 0.26f);

            var checkObject = new GameObject("Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkObject.transform.SetParent(backgroundObject.transform, false);
            var checkRect = checkObject.GetComponent<RectTransform>();
            SetAnchors(checkRect, Vector2.zero, Vector2.one, new Vector2(9f, 9f), new Vector2(-9f, -9f));
            var checkImage = checkObject.GetComponent<Image>();
            checkImage.color = new Color(0.82f, 0.58f, 0.08f);

            var labelText = CreateText($"{name} Label", toggleObject.transform, TextAnchor.MiddleLeft, 30, FontStyle.Bold);
            labelText.text = label;
            SetAnchors(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(64f, 2f), new Vector2(-12f, -2f));

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkImage;
            toggle.SetIsOnWithoutNotify(initialValue);
            return toggle;
        }

        private static void SetAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
