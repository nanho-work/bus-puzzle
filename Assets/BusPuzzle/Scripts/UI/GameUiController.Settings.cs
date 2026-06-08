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

            var titlePlate = CreateDialogTitlePlate("Settings Title Plate", modal, "OPTION");
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
                "Effect",
                UserPreferences.EffectSoundEnabled);
            SetAnchors(effectSoundToggle.GetComponent<RectTransform>(), new Vector2(0.08f, 0.39f), new Vector2(0.32f, 0.72f), Vector2.zero, Vector2.zero);
            effectSoundToggle.onValueChanged.AddListener(value => UserPreferences.EffectSoundEnabled = value);

            mainSoundToggle = CreateSettingIconToggle(
                "Main Sound Toggle",
                modal,
                MainSoundOnIconResource,
                MainSoundOffIconResource,
                "♪",
                "Music",
                UserPreferences.MainSoundEnabled);
            SetAnchors(mainSoundToggle.GetComponent<RectTransform>(), new Vector2(0.38f, 0.39f), new Vector2(0.62f, 0.72f), Vector2.zero, Vector2.zero);
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
                "Vibration",
                UserPreferences.VibrationEnabled);
            SetAnchors(vibrationToggle.GetComponent<RectTransform>(), new Vector2(0.68f, 0.39f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);
            vibrationToggle.onValueChanged.AddListener(value => UserPreferences.VibrationEnabled = value);

            var feedbackButton = CreateRoundIconButton("Feedback Button", modal, "i", 78f, 36, true);
            SetAnchors(feedbackButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.12f), new Vector2(0.45f, 0.32f), Vector2.zero, Vector2.zero);
            feedbackButton.onClick.AddListener(OpenFeedbackMail);

            var privacyButton = CreateRoundIconButton("Privacy Button", modal, "≡", 78f, 36, true);
            SetAnchors(privacyButton.GetComponent<RectTransform>(), new Vector2(0.55f, 0.12f), new Vector2(0.72f, 0.32f), Vector2.zero, Vector2.zero);
            privacyButton.onClick.AddListener(OpenPrivacyPolicy);

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
            var iconVisual = toggle.GetComponent<SettingIconToggleVisual>();
            if (iconVisual != null)
            {
                iconVisual.Apply(value);
            }
        }

        private static Toggle CreateSettingIconToggle(
            string name,
            Transform parent,
            string onIconResourcePath,
            string offIconResourcePath,
            string fallbackGlyph,
            string label,
            bool initialValue)
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

            var labelText = CreateText($"{name} Label", toggleObject.transform, TextAnchor.MiddleCenter, 24, FontStyle.Normal);
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
            var subject = Uri.EscapeDataString("Bus Puzzle Feedback");
            var body = Uri.EscapeDataString("Please write your feedback here.");
            Application.OpenURL($"mailto:{FeedbackEmailAddress}?subject={subject}&body={body}");
        }

        private static void OpenPrivacyPolicy()
        {
            Application.OpenURL(PrivacyPolicyUrl);
        }
    }
}
