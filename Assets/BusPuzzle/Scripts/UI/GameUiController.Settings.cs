using System;
using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
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
