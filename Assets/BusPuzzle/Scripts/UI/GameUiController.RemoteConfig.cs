using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private void BuildRemoteConfigPrompt()
        {
            remoteConfigPrompt = CreatePromptOverlay("Remote Config Overlay");
            var modal = CreatePromptModal(
                remoteConfigPrompt,
                "Remote Config Prompt",
                string.Empty,
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.64f),
                out remoteConfigPromptTitleText);

            remoteConfigPromptText = CreateText("Remote Config Prompt Text", modal, TextAnchor.MiddleCenter, 32, FontStyle.Normal);
            remoteConfigPromptText.color = new Color(0.88f, 0.96f, 1f, 0.96f);
            SetAnchors(remoteConfigPromptText.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.78f), new Vector2(24f, 4f), new Vector2(-24f, -4f));

            remoteConfigActionButton = CreatePromptTextButton("Remote Config Action Button", modal, Localization.Text("update"), UiPrimaryActionColor, out remoteConfigActionButtonText);
            SetAnchors(remoteConfigActionButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0f), new Vector2(0.82f, 0.36f), new Vector2(0f, 16f), new Vector2(0f, -12f));
            remoteConfigActionButton.onClick.AddListener(() => RemoteConfigActionRequested?.Invoke());

            HideRemoteConfigPrompt();
        }

        public void ShowRemoteConfigPrompt(string title, string message, string actionLabel, bool showAction)
        {
            if (remoteConfigPrompt == null)
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

            remoteConfigPrompt.gameObject.SetActive(true);
            if (remoteConfigPromptTitleText != null)
            {
                remoteConfigPromptTitleText.text = title ?? string.Empty;
            }

            if (remoteConfigPromptText != null)
            {
                remoteConfigPromptText.text = message ?? string.Empty;
            }

            if (remoteConfigActionButton != null)
            {
                remoteConfigActionButton.gameObject.SetActive(showAction);
                remoteConfigActionButton.interactable = showAction;
            }

            if (remoteConfigActionButtonText != null)
            {
                remoteConfigActionButtonText.text = actionLabel ?? string.Empty;
            }
        }

        public void HideRemoteConfigPrompt()
        {
            if (remoteConfigPrompt != null)
            {
                remoteConfigPrompt.gameObject.SetActive(false);
            }
        }
    }
}
