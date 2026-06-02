using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed class GameUiController : MonoBehaviour
    {
        private Text levelText;
        private Text statusText;
        private Text remainingText;
        private Text stationText;
        private Button restartButton;
        private Button nextButton;

        public event Action RestartRequested;
        public event Action NextLevelRequested;

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
            levelText.text = $"Level {levelNumber}/{totalLevels}";
        }

        public void SetRemaining(int remainingCount)
        {
            remainingText.text = $"Units {remainingCount}";
        }

        public void SetStationSlots(int occupiedSlots, int totalSlots)
        {
            stationText.text = $"Stops {occupiedSlots}/{totalSlots}";
        }

        public void ShowPlaying(string levelName)
        {
            statusText.text = levelName;
            nextButton.interactable = false;
        }

        public void ShowInvalid(string message)
        {
            statusText.text = message;
        }

        public void ShowClear(bool hasNextLevel)
        {
            statusText.text = hasNextLevel ? "Clear" : "All Clear";
            nextButton.interactable = hasNextLevel;
        }

        public void ShowFailed()
        {
            statusText.text = "Failed";
            nextButton.interactable = false;
        }

        private void BuildLayout()
        {
            var topPanel = CreatePanel("Top Bar", transform, new Color(0.08f, 0.10f, 0.13f, 0.82f));
            SetAnchors(topPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -108f), new Vector2(-20f, -18f));

            levelText = CreateText("Level Text", topPanel, TextAnchor.MiddleLeft, 34, FontStyle.Bold);
            SetAnchors(levelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.32f, 1f), new Vector2(20f, -6f), new Vector2(-6f, -4f));

            stationText = CreateText("Station Text", topPanel, TextAnchor.MiddleCenter, 28, FontStyle.Bold);
            SetAnchors(stationText.rectTransform, new Vector2(0.34f, 0.5f), new Vector2(0.66f, 1f), new Vector2(4f, -6f), new Vector2(-4f, -4f));

            remainingText = CreateText("Remaining Text", topPanel, TextAnchor.MiddleRight, 30, FontStyle.Normal);
            SetAnchors(remainingText.rectTransform, new Vector2(0.68f, 0.5f), new Vector2(1f, 1f), new Vector2(8f, -6f), new Vector2(-20f, -4f));

            statusText = CreateText("Status Text", topPanel, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.52f), new Vector2(20f, 6f), new Vector2(-20f, -4f));

            var bottomPanel = CreatePanel("Bottom Bar", transform, new Color(0.08f, 0.10f, 0.13f, 0.88f));
            SetAnchors(bottomPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 18f), new Vector2(-20f, 108f));

            restartButton = CreateButton("Restart Button", bottomPanel, "Restart", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(restartButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(14f, 16f), new Vector2(-8f, -16f));
            restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

            nextButton = CreateButton("Next Button", bottomPanel, "Next", new Color(0.12f, 0.42f, 0.78f));
            SetAnchors(nextButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 1f), new Vector2(8f, 16f), new Vector2(-14f, -16f));
            nextButton.onClick.AddListener(() => NextLevelRequested?.Invoke());
            nextButton.interactable = false;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = color;
            return panelObject.GetComponent<RectTransform>();
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
