using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private void BuildNicknamePrompt()
        {
            nicknamePrompt = CreatePromptOverlay("Nickname Overlay");
            var modal = CreateGameDialog("Nickname Modal", nicknamePrompt);
            SetAnchors(modal, new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);
            CreateOverlayDismissButton("Nickname Outside Close Button", nicknamePrompt, () =>
            {
                if (!nicknamePromptIsInitial)
                {
                    HideNicknamePrompt(true);
                }
            });

            var titlePlate = CreateDialogTitlePlate("Nickname Title Plate", modal, Localization.Text("nickname_title"));
            nicknamePromptTitleText = titlePlate.GetComponentInChildren<Text>();
            ApplySettingsTextWeight(nicknamePromptTitleText);
            SetAnchors(titlePlate, new Vector2(0.17f, 0.86f), new Vector2(0.83f, 1.12f), Vector2.zero, Vector2.zero);

            nicknameCloseButton = CreatePromptCloseButton("Nickname Close Button", modal);
            nicknameCloseButton.onClick.AddListener(() => HideNicknamePrompt(true));

            nicknamePromptMessageText = CreateText("Nickname Message", modal, TextAnchor.MiddleCenter, 24, FontStyle.Normal);
            ApplySettingsTextWeight(nicknamePromptMessageText);
            nicknamePromptMessageText.color = new Color(0.86f, 0.94f, 1f, 0.96f);
            SetAnchors(nicknamePromptMessageText.rectTransform, new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.76f), new Vector2(8f, 0f), new Vector2(-8f, 0f));

            nicknameInput = CreateNicknameInputField(modal);
            SetAnchors(nicknameInput.GetComponent<RectTransform>(), new Vector2(0.10f, 0.41f), new Vector2(0.90f, 0.58f), Vector2.zero, Vector2.zero);

            nicknameValidationText = CreateText("Nickname Validation", modal, TextAnchor.MiddleCenter, 23, FontStyle.Normal);
            ApplySettingsTextWeight(nicknameValidationText);
            nicknameValidationText.color = new Color(1f, 0.32f, 0.32f, 0.98f);
            SetAnchors(nicknameValidationText.rectTransform, new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.39f), new Vector2(8f, 0f), new Vector2(-8f, 0f));

            nicknameSaveButton = CreatePromptTextButton(
                "Nickname Save Button",
                modal,
                Localization.Text("nickname_save"),
                UiPrimaryActionColor,
                out nicknameSaveButtonText);
            ApplySettingsTextWeight(nicknameSaveButtonText);
            SetAnchors(nicknameSaveButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.04f), new Vector2(0.76f, 0.29f), new Vector2(0f, 10f), new Vector2(0f, -8f));
            nicknameSaveButton.onClick.AddListener(SubmitNicknamePrompt);

            HideNicknamePrompt(false);
        }

        private InputField CreateNicknameInputField(Transform parent)
        {
            var inputObject = new GameObject("Nickname Input Field", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);

            var image = inputObject.GetComponent<Image>();
            image.sprite = GetRoundedPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.11f, 0.24f, 0.30f, 0.88f);
            image.raycastTarget = true;

            var text = CreateText("Nickname Input Text", inputObject.transform, TextAnchor.MiddleLeft, 31, FontStyle.Normal);
            ApplySettingsTextWeight(text);
            text.color = new Color(0.96f, 0.98f, 1f, 0.98f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 4f), new Vector2(-22f, -4f));

            nicknameInputPlaceholderText = CreateText("Nickname Input Placeholder", inputObject.transform, TextAnchor.MiddleLeft, 29, FontStyle.Normal);
            ApplySettingsTextWeight(nicknameInputPlaceholderText);
            nicknameInputPlaceholderText.text = Localization.Text("nickname_placeholder");
            nicknameInputPlaceholderText.color = new Color(0.82f, 0.92f, 0.96f, 0.62f);
            SetAnchors(nicknameInputPlaceholderText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 4f), new Vector2(-22f, -4f));

            var inputField = inputObject.GetComponent<InputField>();
            inputField.targetGraphic = image;
            inputField.textComponent = text;
            inputField.placeholder = nicknameInputPlaceholderText;
            inputField.characterLimit = 16;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.contentType = InputField.ContentType.Standard;
            inputField.onValueChanged.AddListener(_ => RefreshNicknameValidation());
            return inputField;
        }

        private void TryShowInitialNicknamePrompt()
        {
            if (attemptedInitialNicknamePrompt || nicknamePrompt == null)
            {
                return;
            }

            if (!PlayerIdentityService.ShouldShowInitialNicknamePrompt)
            {
                attemptedInitialNicknamePrompt = true;
                return;
            }

            if (startupSplashRoot != null)
            {
                return;
            }

            attemptedInitialNicknamePrompt = true;
            ShowNicknamePrompt(true);
        }

        private void ShowNicknamePrompt(bool initialPrompt)
        {
            if (nicknamePrompt == null || nicknameInput == null)
            {
                return;
            }

            nicknamePromptIsInitial = initialPrompt;
            nicknamePromptReturnToSettings = !initialPrompt;

            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
            }

            if (nicknameCloseButton != null)
            {
                nicknameCloseButton.gameObject.SetActive(!initialPrompt);
            }

            nicknameInput.SetTextWithoutNotify(PlayerIdentityService.Nickname);
            SetNicknamePromptMessage(Localization.Text(initialPrompt ? "nickname_initial_hint" : "nickname_hint"));
            RefreshNicknameValidation();

            nicknamePrompt.SetAsLastSibling();
            nicknamePrompt.gameObject.SetActive(true);
            RefreshLocalizedTexts();
        }

        private void SubmitNicknamePrompt()
        {
            if (nicknameInput == null)
            {
                return;
            }

            var wasInitialPrompt = nicknamePromptIsInitial;
            if (!PlayerIdentityService.TrySetNickname(nicknameInput.text, out var normalizedNickname, out var validationMessage))
            {
                ApplyNicknameValidationState(false, validationMessage);
                return;
            }

            nicknameInput.SetTextWithoutNotify(normalizedNickname);
            if (wasInitialPrompt)
            {
                PlayerIdentityService.MarkInitialNicknamePromptSeen();
            }

            SetNicknamePromptMessage(Localization.Text("nickname_saved"));
            HideNicknamePrompt(nicknamePromptReturnToSettings);

            if (wasInitialPrompt)
            {
                InitialNicknamePromptCompleted?.Invoke();
            }
        }

        private void HideNicknamePrompt(bool returnToSettings)
        {
            if (nicknamePrompt != null)
            {
                nicknamePrompt.gameObject.SetActive(false);
            }

            nicknamePromptIsInitial = false;
            if (returnToSettings && settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(true);
                RefreshSettingsToggles();
                RefreshLocalizedTexts();
            }
        }

        private void SetNicknamePromptMessage(string message)
        {
            if (nicknamePromptMessageText != null)
            {
                nicknamePromptMessageText.text = message;
            }
        }

        private void RefreshNicknameValidation()
        {
            if (nicknameInput == null)
            {
                return;
            }

            var isValid = PlayerIdentityService.TryValidateNickname(nicknameInput.text, out _, out var validationMessage);
            ApplyNicknameValidationState(isValid, validationMessage);
            SetNicknamePromptMessage(Localization.Text(nicknamePromptIsInitial ? "nickname_initial_hint" : "nickname_hint"));
        }

        private void ApplyNicknameValidationState(bool isValid, string validationMessage)
        {
            if (nicknameSaveButton != null)
            {
                nicknameSaveButton.interactable = isValid;
            }

            if (nicknameValidationText == null)
            {
                return;
            }

            var hasMessage = !isValid && !string.IsNullOrWhiteSpace(validationMessage);
            nicknameValidationText.gameObject.SetActive(hasMessage);
            nicknameValidationText.text = hasMessage ? Localization.Text(validationMessage) : string.Empty;
        }
    }
}
