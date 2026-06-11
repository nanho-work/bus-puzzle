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

            menuButton = CreateHeaderIconButton("Header Menu Button", topPanel, RetryButtonIconResource, "↻", UiPrimaryActionColor);
            SetAnchors(menuButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.16f, 1f), new Vector2(0f, 0f), new Vector2(-8f, 0f));
            menuButton.onClick.AddListener(() =>
            {
                HideSettingsPanel();
                RestartRequested?.Invoke();
            });

            levelText = CreateText("Stage Text", topPanel, TextAnchor.MiddleCenter, HeaderStageFontSize, FontStyle.Bold);
            levelText.color = UiStageTextColor;
            var levelTextOutline = levelText.gameObject.AddComponent<Outline>();
            levelTextOutline.effectColor = UiStageTextOutlineColor;
            levelTextOutline.effectDistance = new Vector2(1f, -1f);
            levelTextOutline.useGraphicAlpha = true;
            SetAnchors(levelText.rectTransform, new Vector2(0.30f, 0f), new Vector2(0.70f, 1f), new Vector2(6f, 2f), new Vector2(-6f, -2f));

            var goldCounter = CreateRoundedPanel("Gold Counter", topPanel, new Color(0.11f, 0.16f, 0.19f, 0.20f));
            var goldShadow = goldCounter.gameObject.AddComponent<Shadow>();
            goldShadow.effectColor = new Color(0f, 0f, 0f, 0.10f);
            goldShadow.effectDistance = new Vector2(0f, -3f);
            SetAnchors(goldCounter, new Vector2(0.68f, 0.18f), new Vector2(0.85f, 0.82f), new Vector2(2f, 0f), new Vector2(-2f, 0f));

            var goldIconObject = new GameObject("Gold Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            goldIconObject.transform.SetParent(goldCounter, false);
            var goldIconRect = goldIconObject.GetComponent<RectTransform>();
            SetAnchors(goldIconRect, new Vector2(0f, 0f), new Vector2(0.30f, 1f), new Vector2(8f, 5f), new Vector2(-5f, -5f));

            var goldIconImage = goldIconObject.GetComponent<Image>();
            goldIconImage.sprite = LoadGoldIconSprite();
            goldIconImage.color = Color.white;
            goldIconImage.preserveAspect = true;
            goldIconImage.raycastTarget = false;

            goldText = CreateText("Gold Text", goldCounter, TextAnchor.MiddleLeft, HeaderGoldFontSize, FontStyle.Bold);
            goldText.color = UiGoldTextColor;
            var goldTextOutline = goldText.gameObject.AddComponent<Outline>();
            goldTextOutline.effectColor = UiGoldTextOutlineColor;
            goldTextOutline.effectDistance = new Vector2(1.25f, -1.25f);
            goldTextOutline.useGraphicAlpha = true;
            SetAnchors(goldText.rectTransform, new Vector2(0.41f, 0f), Vector2.one, new Vector2(0f, 1f), new Vector2(-8f, -1f));

            settingsButton = CreateHeaderIconButton("Header Settings Button", topPanel, SettingsButtonIconResource, "⚙", UiPrimaryActionColor);
            SetAnchors(settingsButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(0f, 0f));
            settingsButton.onClick.AddListener(ToggleSettingsPanel);

            statusText = CreateText("Status Text", root, TextAnchor.MiddleCenter, 30, FontStyle.Normal);
            SetAnchors(statusText.rectTransform, new Vector2(0.18f, 1f), new Vector2(0.82f, 1f), new Vector2(0f, -162f), new Vector2(0f, -120f));

            var boosterRow = CreateRectTransform("Booster Row", root);
            SetAnchors(boosterRow, new Vector2(0.14f, 0f), new Vector2(0.86f, 0f), new Vector2(0f, 18f), new Vector2(0f, 170f));

            vipButton = CreateBoosterButton("VIP Button", boosterRow, VipBoosterIconResource, Localization.Text("vip_title"), UiBoosterGoldColor, true, out vipBadgeText);
            SetAnchors(vipButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.333f, 1f), new Vector2(0f, 0f), new Vector2(-12f, 0f));
            vipButton.onClick.AddListener(() => VipTeleportRequested?.Invoke());

            mixButton = CreateBoosterButton("Mix Button", boosterRow, MixBoosterIconResource, Localization.Text("mix_title"), UiBoosterBlueColor, false, out _);
            SetAnchors(mixButton.GetComponent<RectTransform>(), new Vector2(0.333f, 0f), new Vector2(0.667f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f));
            mixButton.onClick.AddListener(() => MixShuffleRequested?.Invoke());
            mixButton.interactable = false;

            departButton = CreateBoosterButton("Depart Button", boosterRow, DepartBoosterIconResource, Localization.Text("depart"), UiBoosterDepartColor, false, out _);
            SetAnchors(departButton.GetComponent<RectTransform>(), new Vector2(0.667f, 0f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(0f, 0f));
            departButton.onClick.AddListener(() => DepartRequested?.Invoke());
            departButton.interactable = false;

            BuildSettingsPanel();
            BuildClearPrompt();
            BuildFailPrompt();
            BuildExitPrompt();
            BuildStationUnlockPrompt();
            BuildVipTeleportPrompt();
            BuildMixShufflePrompt();
            BuildDepartPrompt();
            BuildDifficultyBanner();
            BuildRemoteConfigPrompt();
            BuildStartupSplashOverlay();
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
