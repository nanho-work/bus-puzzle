using System;
using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private const string EffectSoundOnIconResource = "UI/Boosters/Effect_on";
        private const string EffectSoundOffIconResource = "UI/Boosters/Effect_off";
        private const string MainSoundOnIconResource = "UI/Boosters/Music_on";
        private const string MainSoundOffIconResource = "UI/Boosters/Music_off";
        private const string VibrationOnIconResource = "UI/Boosters/Vibration_on";
        private const string VibrationOffIconResource = "UI/Boosters/Vibration_off";

        private void BuildSettingsPanel()
        {
            settingsPanel = CreatePanel("Settings Overlay", safeAreaRoot, UiOverlayColor);
            SetAnchors(settingsPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var modal = CreateGameDialog("Settings Modal", settingsPanel);
            SetAnchors(modal, new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.70f), Vector2.zero, Vector2.zero);
            CreateOverlayDismissButton("Settings Outside Close Button", settingsPanel, HideSettingsPanel);

            var titlePlate = CreateDialogTitlePlate("Settings Title Plate", modal, Localization.Text("settings_title"));
            settingsTitleText = titlePlate.GetComponentInChildren<Text>();
            ApplySettingsTextWeight(settingsTitleText);
            SetAnchors(titlePlate, new Vector2(0.17f, 0.88f), new Vector2(0.83f, 1.14f), Vector2.zero, Vector2.zero);

            var closeButton = CreateRoundIconButton("Settings Close Button", modal, "×", 86f, 42, true);
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0.82f, 0.76f), new Vector2(1.00f, 0.96f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(HideSettingsPanel);

            effectSoundToggle = CreateSettingIconToggle(
                "Effect Sound Toggle",
                modal,
                EffectSoundOnIconResource,
                EffectSoundOffIconResource,
                "SFX",
                Localization.Text("effect_sound"),
                UserPreferences.EffectSoundEnabled,
                out effectSoundLabelText);
            SetAnchors(effectSoundToggle.GetComponent<RectTransform>(), new Vector2(0.04f, 0.39f), new Vector2(0.25f, 0.72f), Vector2.zero, Vector2.zero);
            effectSoundToggle.onValueChanged.AddListener(value => UserPreferences.EffectSoundEnabled = value);

            mainSoundToggle = CreateSettingIconToggle(
                "Main Sound Toggle",
                modal,
                MainSoundOnIconResource,
                MainSoundOffIconResource,
                "♪",
                Localization.Text("music"),
                UserPreferences.MainSoundEnabled,
                out mainSoundLabelText);
            SetAnchors(mainSoundToggle.GetComponent<RectTransform>(), new Vector2(0.28f, 0.39f), new Vector2(0.49f, 0.72f), Vector2.zero, Vector2.zero);
            mainSoundToggle.onValueChanged.AddListener(value =>
            {
                UserPreferences.MainSoundEnabled = value;
                BackgroundMusicPlayer.ApplyPreferences();
            });

            vibrationToggle = CreateSettingIconToggle(
                "Vibration Toggle",
                modal,
                VibrationOnIconResource,
                VibrationOffIconResource,
                "≋",
                Localization.Text("vibration"),
                UserPreferences.VibrationEnabled,
                out vibrationLabelText);
            SetAnchors(vibrationToggle.GetComponent<RectTransform>(), new Vector2(0.52f, 0.39f), new Vector2(0.73f, 0.72f), Vector2.zero, Vector2.zero);
            vibrationToggle.onValueChanged.AddListener(value =>
            {
                UserPreferences.VibrationEnabled = value;
                if (value)
                {
                    HapticFeedback.PlayUiConfirm();
                }
            });

            languageButton = CreateSettingIconButton(
                "Language Button",
                modal,
                LanguageIconResource,
                "A",
                Localization.Text("language"),
                out languageLabelText);
            SetAnchors(languageButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.39f), new Vector2(0.97f, 0.72f), Vector2.zero, Vector2.zero);
            languageButton.onClick.AddListener(ShowLanguagePrompt);

            var nicknameButton = CreatePromptTextButton(
                "Nickname Button",
                modal,
                Localization.Text("nickname_short"),
                UiSecondaryActionColor,
                out nicknameButtonText);
            ApplySettingsTextWeight(nicknameButtonText);
            SetAnchors(nicknameButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.36f), Vector2.zero, Vector2.zero);
            nicknameButton.onClick.AddListener(() => ShowNicknamePrompt(false));

            var feedbackButton = CreatePromptTextButton(
                "Feedback Button",
                modal,
                Localization.Text("contact_short"),
                UiSecondaryActionColor,
                out feedbackButtonText);
            ApplySettingsTextWeight(feedbackButtonText);
            SetAnchors(feedbackButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.06f), new Vector2(0.48f, 0.20f), Vector2.zero, Vector2.zero);
            feedbackButton.onClick.AddListener(OpenFeedbackMail);

            var privacyButton = CreatePromptTextButton(
                "Legal Button",
                modal,
                Localization.Text("legal_short"),
                UiSecondaryActionColor,
                out legalButtonText);
            ApplySettingsTextWeight(legalButtonText);
            SetAnchors(privacyButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.06f), new Vector2(0.94f, 0.20f), Vector2.zero, Vector2.zero);
            privacyButton.onClick.AddListener(OpenPrivacyPolicy);

            BuildLanguagePrompt();
            BuildLeaderboardPrompt();
            BuildNicknamePrompt();
            HideSettingsPanel();
        }

        private void ToggleSettingsPanel()
        {
            if (settingsPanel == null)
            {
                return;
            }

            var shouldShow = !settingsPanel.gameObject.activeSelf;
            settingsPanel.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                HideDailyRewardPrompt();
                RefreshSettingsToggles();
                RefreshLocalizedTexts();
            }
        }

        private void HideSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
            }

            if (languagePrompt != null)
            {
                languagePrompt.gameObject.SetActive(false);
            }

            if (leaderboardPrompt != null)
            {
                leaderboardPrompt.gameObject.SetActive(false);
            }

            if (nicknamePrompt != null)
            {
                nicknamePrompt.gameObject.SetActive(false);
            }
        }

        private void RefreshSettingsToggles()
        {
            SetToggleValueWithoutNotify(effectSoundToggle, UserPreferences.EffectSoundEnabled);
            SetToggleValueWithoutNotify(mainSoundToggle, UserPreferences.MainSoundEnabled);
            SetToggleValueWithoutNotify(vibrationToggle, UserPreferences.VibrationEnabled);
        }

        private void BuildLanguagePrompt()
        {
            languagePrompt = CreatePromptOverlay("Language Overlay");
            var modal = CreateGameDialog("Language Modal", languagePrompt);
            SetAnchors(modal, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);
            CreateOverlayDismissButton("Language Outside Close Button", languagePrompt, () => HideLanguagePrompt(true));

            var titlePlate = CreateDialogTitlePlate("Language Title Plate", modal, Localization.Text("language_title"));
            languagePromptTitleText = titlePlate.GetComponentInChildren<Text>();
            ApplySettingsTextWeight(languagePromptTitleText);
            SetAnchors(titlePlate, new Vector2(0.17f, 0.88f), new Vector2(0.83f, 1.14f), Vector2.zero, Vector2.zero);

            var closeButton = CreatePromptCloseButton("Language Close Button", modal);
            closeButton.onClick.AddListener(() => HideLanguagePrompt(true));

            languageOptionButtons.Clear();
            languageOptionButtonTexts.Clear();
            languageOptionCodes.Clear();

            var options = Localization.LanguageOptions;
            const int columnCount = 2;
            var rowCount = Mathf.CeilToInt(options.Count / (float)columnCount);
            const float top = 0.78f;
            const float bottom = 0.09f;
            const float rowGap = 0.010f;
            var rowHeight = (top - bottom) / rowCount;

            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                var row = index / columnCount;
                var column = index % columnCount;
                var xMin = column == 0 ? 0.08f : 0.53f;
                var xMax = column == 0 ? 0.47f : 0.92f;
                var yMax = top - row * rowHeight;
                var yMin = yMax - rowHeight + rowGap;

                var optionButton = CreatePromptTextButton(
                    $"Language Option {index:00}",
                    modal,
                    Localization.GetLanguageOptionLabel(option),
                    UiSecondaryActionColor,
                    out var optionButtonText);
                SetAnchors(
                    optionButton.GetComponent<RectTransform>(),
                    new Vector2(xMin, yMin),
                    new Vector2(xMax, yMax),
                    new Vector2(0f, 2f),
                    new Vector2(0f, -2f));

                var optionCode = option.Code;
                optionButton.onClick.AddListener(() => SelectLanguage(optionCode));
                languageOptionButtons.Add(optionButton);
                languageOptionButtonTexts.Add(optionButtonText);
                languageOptionCodes.Add(option.Code);
            }

            HideLanguagePrompt(false);
        }

        private void ShowLanguagePrompt()
        {
            if (languagePrompt == null)
            {
                return;
            }

            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
            }

            languagePrompt.gameObject.SetActive(true);
            RefreshLocalizedTexts();
            RefreshLanguageOptionButtons();
        }

        private void HideLanguagePrompt(bool returnToSettings)
        {
            if (languagePrompt != null)
            {
                languagePrompt.gameObject.SetActive(false);
            }

            if (returnToSettings && settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(true);
                RefreshSettingsToggles();
                RefreshLocalizedTexts();
            }
        }

        private void SelectLanguage(string languageCode)
        {
            Localization.SelectedLanguageCode = languageCode;
            RefreshLocalizedTexts();
            RefreshLanguageOptionButtons();
            HideLanguagePrompt(true);
        }

        private void RefreshLanguageOptionButtons()
        {
            var options = Localization.LanguageOptions;
            var selectedLanguageCode = Localization.SelectedLanguageCode;
            for (var index = 0; index < languageOptionButtons.Count && index < options.Count; index++)
            {
                var option = options[index];
                var isSelected = languageOptionCodes[index] == selectedLanguageCode;
                if (languageOptionButtonTexts[index] != null)
                {
                    var optionLabel = Localization.GetLanguageOptionLabel(option);
                    languageOptionButtonTexts[index].text = isSelected ? $"> {optionLabel}" : optionLabel;
                    languageOptionButtonTexts[index].fontSize = 24;
                    languageOptionButtonTexts[index].resizeTextMinSize = 16;
                }

                if (languageOptionButtons[index] != null)
                {
                    languageOptionButtons[index].interactable = !isSelected;
                }
            }
        }

        private void RefreshLocalizedTexts()
        {
            if (settingsTitleText != null)
            {
                settingsTitleText.text = Localization.Text("settings_title");
            }

            if (effectSoundLabelText != null)
            {
                effectSoundLabelText.text = Localization.Text("effect_sound");
            }

            if (mainSoundLabelText != null)
            {
                mainSoundLabelText.text = Localization.Text("music");
            }

            if (vibrationLabelText != null)
            {
                vibrationLabelText.text = Localization.Text("vibration");
            }

            if (languageLabelText != null)
            {
                languageLabelText.text = Localization.Text("language");
            }

            if (nicknameButtonText != null)
            {
                nicknameButtonText.text = Localization.Text("nickname_short");
            }

            if (feedbackButtonText != null)
            {
                feedbackButtonText.text = Localization.Text("contact_short");
            }

            if (legalButtonText != null)
            {
                legalButtonText.text = Localization.Text("legal_short");
            }

            if (languagePromptTitleText != null)
            {
                languagePromptTitleText.text = Localization.Text("language_title");
            }

            if (leaderboardPromptTitleText != null)
            {
                leaderboardPromptTitleText.text = Localization.Text("leaderboard_title");
            }

            if (leaderboardRefreshButtonText != null)
            {
                leaderboardRefreshButtonText.text = Localization.Text("leaderboard_refresh");
            }

            if (nicknamePromptTitleText != null)
            {
                nicknamePromptTitleText.text = Localization.Text("nickname_title");
            }

            if (nicknameInputPlaceholderText != null)
            {
                nicknameInputPlaceholderText.text = Localization.Text("nickname_placeholder");
            }

            if (nicknameSaveButtonText != null)
            {
                nicknameSaveButtonText.text = Localization.Text("nickname_save");
            }

            if (nicknamePrompt != null && nicknamePrompt.gameObject.activeSelf)
            {
                RefreshNicknameValidation();
            }

            if (clearPromptTitleText != null)
            {
                clearPromptTitleText.text = Localization.Text("clear_title");
            }

            if (failPromptTitleText != null)
            {
                failPromptTitleText.text = Localization.Text("failed_title");
            }

            if (failPromptText != null)
            {
                failPromptText.text = Localization.Text("stage_failed");
            }

            if (failRetryButtonText != null)
            {
                failRetryButtonText.text = Localization.Text("retry");
            }

            if (exitPromptTitleText != null)
            {
                exitPromptTitleText.text = Localization.Text("exit_title");
            }

            if (exitPromptText != null)
            {
                exitPromptText.text = Localization.Text("exit_game");
            }

            if (exitButtonText != null)
            {
                exitButtonText.text = Localization.Text("exit");
            }

            if (stationUnlockPromptTitleText != null)
            {
                stationUnlockPromptTitleText.text = Localization.Text("slot_title");
            }

            if (stationUnlockConfirmButtonText != null)
            {
                stationUnlockConfirmButtonText.text = Localization.Text("watch");
            }

            if (vipTeleportPromptTitleText != null)
            {
                vipTeleportPromptTitleText.text = Localization.Text("vip_title");
            }

            if (mixShufflePromptTitleText != null)
            {
                mixShufflePromptTitleText.text = Localization.Text("mix_title");
            }

            if (departPromptTitleText != null)
            {
                departPromptTitleText.text = Localization.Text("depart");
            }

            if (dailyRewardPromptTitleText != null)
            {
                dailyRewardPromptTitleText.text = Localization.Text("daily_reward_title");
            }

            if (dailyRewardPromptMessageText != null)
            {
                dailyRewardPromptMessageText.text = Localization.Text(
                    dailyRewardPromptCanClaim ? "daily_reward_message" : "daily_reward_claimed_message");
            }

            if (dailyRewardClaimButtonText != null)
            {
                dailyRewardClaimButtonText.text = Localization.Text(
                    dailyRewardPromptCanClaim ? "daily_reward_claim" : "daily_reward_claimed");
            }

            RefreshLanguageOptionButtons();
        }

        private static void SetToggleValueWithoutNotify(Toggle toggle, bool value)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.SetIsOnWithoutNotify(value);
            var iconVisual = toggle.GetComponent<SettingIconToggleVisual>();
            if (iconVisual != null)
            {
                iconVisual.Apply(value);
            }
        }

        private static void ApplySettingsTextWeight(Text text)
        {
            GameFontProvider.ApplyMediumToText(text);
        }

        private static Toggle CreateSettingIconToggle(
            string name,
            Transform parent,
            string onIconResourcePath,
            string offIconResourcePath,
            string fallbackGlyph,
            string label,
            bool initialValue,
            out Text labelText)
        {
            var toggleObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            var cardImage = toggleObject.GetComponent<Image>();
            cardImage.color = new Color(1f, 1f, 1f, 0f);

            var onIconSprite = LoadResourceSprite(onIconResourcePath);
            var offIconSprite = LoadResourceSprite(offIconResourcePath);
            var iconRoot = CreateCenteredSquare($"{name} Icon Root", toggleObject.transform, 116f);
            iconRoot.anchoredPosition = new Vector2(0f, 22f);

            var iconObject = new GameObject($"{name} Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(iconRoot, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            SetAnchors(iconRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = GetSettingToggleSprite(initialValue, onIconSprite, offIconSprite);
            iconImage.color = iconImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            Text fallbackText = null;
            if (onIconSprite == null && offIconSprite == null)
            {
                fallbackText = CreateText($"{name} Fallback", iconRoot, TextAnchor.MiddleCenter, 36, FontStyle.Bold);
                fallbackText.text = fallbackGlyph;
                SetAnchors(fallbackText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            labelText = CreateText($"{name} Label", toggleObject.transform, TextAnchor.MiddleCenter, 24, FontStyle.Normal);
            labelText.text = label;
            SetAnchors(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.25f), new Vector2(0f, 0f), new Vector2(0f, -2f));

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = cardImage;
            toggle.graphic = null;

            var iconVisual = toggleObject.AddComponent<SettingIconToggleVisual>();
            iconVisual.Initialize(iconImage, onIconSprite, offIconSprite, labelText, fallbackText);
            toggle.onValueChanged.AddListener(iconVisual.Apply);
            iconVisual.Apply(initialValue);
            toggle.SetIsOnWithoutNotify(initialValue);

            var colors = toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.98f, 1f);
            colors.pressedColor = new Color(0.90f, 0.93f, 0.96f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.48f);
            toggle.colors = colors;
            return toggle;
        }

        private static Button CreateSettingIconButton(
            string name,
            Transform parent,
            string iconResourcePath,
            string fallbackGlyph,
            string label,
            out Text labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var cardImage = buttonObject.GetComponent<Image>();
            cardImage.color = new Color(1f, 1f, 1f, 0f);

            var iconSprite = LoadResourceSprite(iconResourcePath);
            var iconRoot = CreateCenteredSquare($"{name} Icon Root", buttonObject.transform, 116f);
            iconRoot.anchoredPosition = new Vector2(0f, 22f);

            var iconObject = new GameObject($"{name} Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(iconRoot, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            SetAnchors(iconRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.color = iconSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            if (iconSprite == null)
            {
                var fallbackText = CreateText($"{name} Fallback", iconRoot, TextAnchor.MiddleCenter, 36, FontStyle.Bold);
                fallbackText.text = fallbackGlyph;
                SetAnchors(fallbackText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            labelText = CreateText($"{name} Label", buttonObject.transform, TextAnchor.MiddleCenter, 24, FontStyle.Normal);
            labelText.text = label;
            labelText.color = new Color(0.96f, 0.98f, 1f);
            SetAnchors(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.25f), new Vector2(0f, 0f), new Vector2(0f, -2f));

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = iconSprite != null ? iconImage : cardImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.98f, 1f);
            colors.pressedColor = new Color(0.90f, 0.93f, 0.96f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.48f);
            button.colors = colors;
            return button;
        }

        private static Sprite GetSettingToggleSprite(bool isOn, Sprite onSprite, Sprite offSprite)
        {
            var preferredSprite = isOn ? onSprite : offSprite;
            if (preferredSprite != null)
            {
                return preferredSprite;
            }

            return isOn ? offSprite : onSprite;
        }

        private sealed class SettingIconToggleVisual : MonoBehaviour
        {
            private static readonly Color OnLabelColor = new Color(0.96f, 0.98f, 1f);
            private static readonly Color OffLabelColor = new Color(0.52f, 0.58f, 0.62f);

            private Image iconImage;
            private Sprite onSprite;
            private Sprite offSprite;
            private Text labelText;
            private Text fallbackText;

            public void Initialize(
                Image newIconImage,
                Sprite newOnSprite,
                Sprite newOffSprite,
                Text newLabelText,
                Text newFallbackText)
            {
                iconImage = newIconImage;
                onSprite = newOnSprite;
                offSprite = newOffSprite;
                labelText = newLabelText;
                fallbackText = newFallbackText;
            }

            public void Apply(bool isOn)
            {
                if (iconImage != null)
                {
                    var nextSprite = GameUiController.GetSettingToggleSprite(isOn, onSprite, offSprite);
                    iconImage.sprite = nextSprite;
                    iconImage.color = nextSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (labelText != null)
                {
                    labelText.color = isOn ? OnLabelColor : OffLabelColor;
                }

                if (fallbackText != null)
                {
                    fallbackText.color = isOn ? OnLabelColor : OffLabelColor;
                }
            }
        }

        private static void OpenFeedbackMail()
        {
            var subject = Uri.EscapeDataString(Localization.Text("feedback_subject"));
            var body = Uri.EscapeDataString(Localization.Text("feedback_body"));
            Application.OpenURL($"mailto:{FeedbackEmailAddress}?subject={subject}&body={body}");
        }

        private static void OpenPrivacyPolicy()
        {
            Application.OpenURL(PrivacyPolicyUrl);
        }
    }
}
