using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private void BuildLayout()
        {
            var root = EnsureSafeAreaRoot();

            var topPanel = CreatePanel("Top Bar", root, new Color(1f, 1f, 1f, 0f));
            topPanel.GetComponent<Image>().raycastTarget = false;
            SetAnchors(topPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -132f), new Vector2(-16f, -8f));

            menuButton = CreateRoundIconButton("Header Menu Button", topPanel, "↻", HeaderIconSize, 48, true);
            SetAnchors(menuButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.20f, 1f), new Vector2(0f, 0f), new Vector2(-8f, 0f));
            menuButton.onClick.AddListener(() =>
            {
                HideSettingsPanel();
                RestartRequested?.Invoke();
            });

            levelText = CreateText("Stage Text", topPanel, TextAnchor.MiddleCenter, HeaderStageFontSize, FontStyle.Bold);
            levelText.color = UiHeaderTextColor;
            SetAnchors(levelText.rectTransform, new Vector2(0.21f, 0f), new Vector2(0.60f, 1f), new Vector2(6f, 2f), new Vector2(-6f, -2f));

            var goldCounter = CreateRoundedPanel("Gold Counter", topPanel, new Color(0.11f, 0.16f, 0.19f, 0.84f));
            var goldShadow = goldCounter.gameObject.AddComponent<Shadow>();
            goldShadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
            goldShadow.effectDistance = new Vector2(0f, -4f);
            SetAnchors(goldCounter, new Vector2(0.61f, 0.18f), new Vector2(0.78f, 0.82f), new Vector2(4f, 0f), new Vector2(-4f, 0f));

            var goldIconObject = new GameObject("Gold Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            goldIconObject.transform.SetParent(goldCounter, false);
            var goldIconRect = goldIconObject.GetComponent<RectTransform>();
            SetAnchors(goldIconRect, new Vector2(0f, 0f), new Vector2(0.34f, 1f), new Vector2(8f, 5f), new Vector2(-1f, -5f));

            var goldIconImage = goldIconObject.GetComponent<Image>();
            goldIconImage.sprite = LoadGoldIconSprite();
            goldIconImage.color = Color.white;
            goldIconImage.preserveAspect = true;
            goldIconImage.raycastTarget = false;

            goldText = CreateText("Gold Text", goldCounter, TextAnchor.MiddleLeft, HeaderGoldFontSize, FontStyle.Bold);
            goldText.color = new Color(1.00f, 0.78f, 0.16f);
            SetAnchors(goldText.rectTransform, new Vector2(0.33f, 0f), Vector2.one, new Vector2(2f, 1f), new Vector2(-8f, -1f));

            settingsButton = CreateGearIconButton("Header Settings Button", topPanel, HeaderIconSize);
            SetAnchors(settingsButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(0f, 0f));
            settingsButton.onClick.AddListener(ToggleSettingsPanel);

            statusText = CreateText("Status Text", root, TextAnchor.MiddleCenter, 30, FontStyle.Bold);
            SetAnchors(statusText.rectTransform, new Vector2(0.18f, 1f), new Vector2(0.82f, 1f), new Vector2(0f, -162f), new Vector2(0f, -120f));

            var boosterRow = CreateRectTransform("Booster Row", root);
            SetAnchors(boosterRow, new Vector2(0.24f, 0f), new Vector2(0.76f, 0f), new Vector2(0f, 18f), new Vector2(0f, 170f));

            vipButton = CreateBoosterButton("VIP Button", boosterRow, VipBoosterIconResource, "VIP", UiBoosterGoldColor, true, out vipBadgeText);
            SetAnchors(vipButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.50f, 1f), new Vector2(0f, 0f), new Vector2(-18f, 0f));
            vipButton.onClick.AddListener(() => VipTeleportRequested?.Invoke());

            mixButton = CreateBoosterButton("Mix Button", boosterRow, MixBoosterIconResource, "Mix", UiBoosterBlueColor, false, out _);
            SetAnchors(mixButton.GetComponent<RectTransform>(), new Vector2(0.50f, 0f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(0f, 0f));
            mixButton.onClick.AddListener(() => MixShuffleRequested?.Invoke());
            mixButton.interactable = false;

            BuildSettingsPanel();
            BuildClearPrompt();
            BuildFailPrompt();
            BuildExitPrompt();
            BuildStationUnlockPrompt();
            BuildVipTeleportPrompt();
            BuildMixShufflePrompt();
        }

        private RectTransform EnsureSafeAreaRoot()
        {
            if (safeAreaRoot != null)
            {
                return safeAreaRoot;
            }

            safeAreaRoot = CreateRectTransform("Safe Area", transform);
            UpdateSafeArea(true);
            return safeAreaRoot;
        }

        private void UpdateSafeArea(bool force = false)
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (!force && lastScreenSize == screenSize && lastSafeArea == safeArea)
            {
                return;
            }

            lastScreenSize = screenSize;
            lastSafeArea = safeArea;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }
    }
}
