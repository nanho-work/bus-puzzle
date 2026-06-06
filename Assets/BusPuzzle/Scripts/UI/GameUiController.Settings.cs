using System;
using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private void BuildSettingsPanel()
        {
            settingsPanel = CreatePanel("Settings Overlay", safeAreaRoot, new Color(0.04f, 0.06f, 0.08f, 0.48f));
            SetAnchors(settingsPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var modal = CreateRoundedPanel("Settings Modal", settingsPanel, new Color(0.10f, 0.12f, 0.14f, 0.96f));
            SetAnchors(modal, new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.70f), Vector2.zero, Vector2.zero);

            var title = CreateText("Settings Title", modal, TextAnchor.MiddleLeft, 42, FontStyle.Bold);
            title.text = "Settings";
            title.color = new Color(0.96f, 0.98f, 1f);
            SetAnchors(title.rectTransform, new Vector2(0.10f, 0.80f), new Vector2(0.70f, 0.97f), new Vector2(0f, 4f), new Vector2(0f, -4f));

            var closeButton = CreateRoundIconButton("Settings Close Button", modal, "×", 86f, 42);
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.78f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(HideSettingsPanel);

            effectSoundToggle = CreateSettingIconToggle("Effect Sound Toggle", modal, "SFX", "Effect", UserPreferences.EffectSoundEnabled);
            SetAnchors(effectSoundToggle.GetComponent<RectTransform>(), new Vector2(0.08f, 0.39f), new Vector2(0.32f, 0.72f), Vector2.zero, Vector2.zero);
            effectSoundToggle.onValueChanged.AddListener(value => UserPreferences.EffectSoundEnabled = value);

            mainSoundToggle = CreateSettingIconToggle("Main Sound Toggle", modal, "♪", "Music", UserPreferences.MainSoundEnabled);
            SetAnchors(mainSoundToggle.GetComponent<RectTransform>(), new Vector2(0.38f, 0.39f), new Vector2(0.62f, 0.72f), Vector2.zero, Vector2.zero);
            mainSoundToggle.onValueChanged.AddListener(value =>
            {
                UserPreferences.MainSoundEnabled = value;
                BackgroundMusicPlayer.ApplyPreferences();
            });

            vibrationToggle = CreateSettingIconToggle("Vibration Toggle", modal, "≋", "Vibration", UserPreferences.VibrationEnabled);
            SetAnchors(vibrationToggle.GetComponent<RectTransform>(), new Vector2(0.68f, 0.39f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);
            vibrationToggle.onValueChanged.AddListener(value => UserPreferences.VibrationEnabled = value);

            var feedbackButton = CreateRoundIconButton("Feedback Button", modal, "i", 78f, 36);
            SetAnchors(feedbackButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.12f), new Vector2(0.45f, 0.32f), Vector2.zero, Vector2.zero);
            feedbackButton.onClick.AddListener(OpenFeedbackMail);

            var privacyButton = CreateRoundIconButton("Privacy Button", modal, "≡", 78f, 36);
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
            string glyph,
            string label,
            bool initialValue)
        {
            var toggleObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            var cardImage = toggleObject.GetComponent<Image>();
            cardImage.color = new Color(1f, 1f, 1f, 0f);

            var iconRoot = CreateRoundIconVisual($"{name} Icon", toggleObject.transform, glyph, 104f, 42, out var innerImage);
            iconRoot.anchoredPosition = new Vector2(0f, 22f);
            var iconGroup = iconRoot.gameObject.AddComponent<CanvasGroup>();

            var labelText = CreateText($"{name} Label", toggleObject.transform, TextAnchor.MiddleCenter, 24, FontStyle.Bold);
            labelText.text = label;
            SetAnchors(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.25f), new Vector2(0f, 0f), new Vector2(0f, -2f));

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = cardImage;
            toggle.graphic = null;

            var iconVisual = toggleObject.AddComponent<SettingIconToggleVisual>();
            iconVisual.Initialize(iconGroup, innerImage, labelText);
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

        private sealed class SettingIconToggleVisual : MonoBehaviour
        {
            private static readonly Color OnInnerColor = new Color(0.12f, 0.66f, 0.89f);
            private static readonly Color OffInnerColor = new Color(0.40f, 0.47f, 0.52f);
            private static readonly Color OnLabelColor = new Color(0.96f, 0.98f, 1f);
            private static readonly Color OffLabelColor = new Color(0.52f, 0.58f, 0.62f);

            private CanvasGroup iconGroup;
            private Image innerImage;
            private Text labelText;

            public void Initialize(CanvasGroup newIconGroup, Image newInnerImage, Text newLabelText)
            {
                iconGroup = newIconGroup;
                innerImage = newInnerImage;
                labelText = newLabelText;
            }

            public void Apply(bool isOn)
            {
                if (iconGroup != null)
                {
                    iconGroup.alpha = isOn ? 1f : 0.48f;
                }

                if (innerImage != null)
                {
                    innerImage.color = isOn ? OnInnerColor : OffInnerColor;
                }

                if (labelText != null)
                {
                    labelText.color = isOn ? OnLabelColor : OffLabelColor;
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
