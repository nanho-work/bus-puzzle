using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private static readonly Dictionary<string, Sprite> runtimeResourceSprites = new Dictionary<string, Sprite>();

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = color;
            return panelObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateRoundedPanel(string name, Transform parent, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            var image = panelObject.GetComponent<Image>();
            image.sprite = GetRoundedPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return panelObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateGameDialog(string name, Transform parent)
        {
            var panel = CreateRoundedPanel(name, parent, UiPanelColor);
            var panelImage = panel.GetComponent<Image>();
            panelImage.raycastTarget = true;

            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.useGraphicAlpha = true;

            var accent = CreateRoundedPanel($"{name} Accent", panel, UiPanelAccentColor);
            SetAnchors(accent, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);

            var stroke = CreateRoundedPanel($"{name} Stroke", panel, UiPanelStrokeColor);
            SetAnchors(stroke, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
            return panel;
        }

        private static RectTransform CreateDialogTitlePlate(string name, Transform parent, string label)
        {
            var titleRoot = CreateRectTransform(name, parent);

            var shadow = CreateRoundedPanel($"{name} Shadow", titleRoot, new Color(0f, 0f, 0f, 0.28f));
            SetAnchors(shadow, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.86f), new Vector2(0f, -8f), new Vector2(0f, -8f));

            var leftCap = CreateTitlePlateCap($"{name} Left Cap", titleRoot, new Color(0.12f, 0.18f, 0.26f, 0.98f));
            SetAnchors(leftCap, new Vector2(0.00f, 0.16f), new Vector2(0.18f, 0.86f), Vector2.zero, Vector2.zero);

            var rightCap = CreateTitlePlateCap($"{name} Right Cap", titleRoot, new Color(0.12f, 0.18f, 0.26f, 0.98f));
            SetAnchors(rightCap, new Vector2(0.82f, 0.16f), new Vector2(1.00f, 0.86f), Vector2.zero, Vector2.zero);

            var plate = CreateRoundedPanel($"{name} Plate", titleRoot, new Color(0.25f, 0.30f, 0.45f, 0.98f));
            SetAnchors(plate, new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.92f), Vector2.zero, Vector2.zero);

            var inner = CreateRoundedPanel($"{name} Inner", titleRoot, new Color(0.19f, 0.23f, 0.36f, 0.94f));
            SetAnchors(inner, new Vector2(0.13f, 0.18f), new Vector2(0.87f, 0.82f), Vector2.zero, Vector2.zero);

            var topHighlight = CreateRoundedPanel($"{name} Top Highlight", titleRoot, new Color(0.66f, 0.74f, 0.94f, 0.20f));
            SetAnchors(topHighlight, new Vector2(0.16f, 0.74f), new Vector2(0.84f, 0.86f), Vector2.zero, Vector2.zero);

            var bottomShadow = CreateRoundedPanel($"{name} Bottom Shadow", titleRoot, new Color(0.03f, 0.05f, 0.09f, 0.28f));
            SetAnchors(bottomShadow, new Vector2(0.16f, 0.18f), new Vector2(0.84f, 0.28f), Vector2.zero, Vector2.zero);

            var leftLight = CreateTitlePlateCap($"{name} Left Light", titleRoot, new Color(0.22f, 0.84f, 0.86f, 0.95f));
            SetAnchors(leftLight, new Vector2(0.055f, 0.40f), new Vector2(0.105f, 0.60f), Vector2.zero, Vector2.zero);

            var rightLight = CreateTitlePlateCap($"{name} Right Light", titleRoot, new Color(0.96f, 0.32f, 0.45f, 0.95f));
            SetAnchors(rightLight, new Vector2(0.895f, 0.40f), new Vector2(0.945f, 0.60f), Vector2.zero, Vector2.zero);

            var title = CreateText($"{name} Text", titleRoot, TextAnchor.MiddleCenter, 50, FontStyle.Bold);
            title.text = label;
            title.color = new Color(0.98f, 0.99f, 1f);
            title.resizeTextMinSize = 34;
            SetAnchors(title.rectTransform, new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.88f), new Vector2(8f, 2f), new Vector2(-8f, -2f));

            return titleRoot;
        }

        private static RectTransform CreateTitlePlateCap(string name, Transform parent, Color color)
        {
            var capObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            capObject.transform.SetParent(parent, false);

            var capImage = capObject.GetComponent<Image>();
            capImage.sprite = GetCircleSprite();
            capImage.color = color;
            capImage.raycastTarget = false;
            return capObject.GetComponent<RectTransform>();
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
            text.font = GameFontProvider.GetFont(fontStyle);
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
            image.sprite = GetRoundedPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = baseColor;

            var shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
            shadow.effectDistance = new Vector2(0f, -5f);
            shadow.useGraphicAlpha = true;

            var shineObject = new GameObject($"{name} Shine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shineObject.transform.SetParent(buttonObject.transform, false);
            var shineRect = shineObject.GetComponent<RectTransform>();
            SetAnchors(shineRect, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            var shineImage = shineObject.GetComponent<Image>();
            shineImage.sprite = GetRoundedPanelSprite();
            shineImage.type = Image.Type.Sliced;
            shineImage.color = new Color(1f, 1f, 1f, 0.16f);
            shineImage.raycastTarget = false;

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.20f);
            colors.disabledColor = new Color(0.20f, 0.22f, 0.25f, 0.55f);
            button.colors = colors;

            var labelText = CreateText($"{name} Label", buttonObject.transform, TextAnchor.MiddleCenter, 30, FontStyle.Bold);
            labelText.text = label;
            labelText.color = new Color(0.96f, 0.98f, 1f);
            SetAnchors(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f));

            return button;
        }

        private static Button CreateImageActionButton(
            string name,
            Transform parent,
            string iconResourcePath,
            string fallbackLabel,
            Color fallbackColor)
        {
            var iconSprite = LoadResourceSprite(iconResourcePath);
            if (iconSprite == null)
            {
                return CreateButton(name, parent, fallbackLabel, fallbackColor);
            }

            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var hitArea = buttonObject.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            var shadowObject = new GameObject($"{name} Icon Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shadowObject.transform.SetParent(buttonObject.transform, false);
            var shadowRect = shadowObject.GetComponent<RectTransform>();
            SetAnchors(shadowRect, Vector2.zero, Vector2.one, new Vector2(0f, -7f), new Vector2(0f, -7f));

            var shadowImage = shadowObject.GetComponent<Image>();
            shadowImage.sprite = iconSprite;
            shadowImage.color = new Color(0f, 0f, 0f, 0.24f);
            shadowImage.preserveAspect = true;
            shadowImage.raycastTarget = false;

            var iconObject = new GameObject($"{name} Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(buttonObject.transform, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            SetAnchors(iconRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = iconImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
            return button;
        }

        private static Button CreateRoundIconButton(
            string name,
            Transform parent,
            string glyph,
            float size,
            int glyphFontSize,
            bool subdued = false)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var hitArea = buttonObject.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            CreateRoundIconVisual($"{name} Icon", buttonObject.transform, glyph, size, glyphFontSize, out var targetImage, subdued);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = targetImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
            return button;
        }

        private static Button CreateGearIconButton(string name, Transform parent, float size, bool subdued = false)
        {
            return CreateRoundSpriteIconButton(name, parent, GetGearIconSprite(), "G", size, 44, subdued, 0.58f);
        }

        private static Button CreateRoundSpriteIconButton(
            string name,
            Transform parent,
            Sprite iconSprite,
            string fallbackGlyph,
            float size,
            int fallbackGlyphFontSize,
            bool subdued = false,
            float iconScale = 0.54f)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var hitArea = buttonObject.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            var glyph = iconSprite != null ? string.Empty : fallbackGlyph;
            var iconRoot = CreateRoundIconVisual($"{name} Icon", buttonObject.transform, glyph, size, fallbackGlyphFontSize, out var targetImage, subdued);
            if (iconSprite != null)
            {
                var inset = Mathf.Clamp01((1f - iconScale) * 0.5f);
                var shadowObject = new GameObject($"{name} Gear Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                shadowObject.transform.SetParent(iconRoot, false);
                var shadowRect = shadowObject.GetComponent<RectTransform>();
                SetAnchors(
                    shadowRect,
                    new Vector2(inset, inset),
                    new Vector2(1f - inset, 1f - inset),
                    new Vector2(2f, -3f),
                    new Vector2(2f, -3f));
                var shadowImage = shadowObject.GetComponent<Image>();
                shadowImage.sprite = iconSprite;
                shadowImage.color = new Color(0.04f, 0.17f, 0.24f, 0.42f);
                shadowImage.preserveAspect = true;
                shadowImage.raycastTarget = false;

                var iconObject = new GameObject($"{name} Gear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(iconRoot, false);
                var iconRect = iconObject.GetComponent<RectTransform>();
                SetAnchors(
                    iconRect,
                    new Vector2(inset, inset),
                    new Vector2(1f - inset, 1f - inset),
                    Vector2.zero,
                    Vector2.zero);
                var iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.color = subdued ? new Color(0.95f, 0.98f, 1f) : new Color(1f, 0.82f, 0.20f);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = targetImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
            return button;
        }

        private static RectTransform CreateRoundIconVisual(
            string name,
            Transform parent,
            string glyph,
            float size,
            int glyphFontSize,
            out Image targetImage,
            bool subdued = false)
        {
            var iconRoot = CreateCenteredSquare($"{name} Root", parent, size);
            var outerColor = subdued ? new Color(0.58f, 0.68f, 0.72f) : new Color(0.82f, 0.42f, 0.11f);
            var ringColor = subdued ? new Color(0.78f, 0.86f, 0.88f) : new Color(1.00f, 0.70f, 0.24f);
            var bevelColor = subdued ? new Color(0.13f, 0.20f, 0.24f, 0.30f) : new Color(0.49f, 0.24f, 0.08f, 0.34f);
            var innerColor = subdued ? new Color(0.16f, 0.45f, 0.58f) : new Color(0.10f, 0.66f, 0.90f);
            var glyphColor = subdued ? new Color(0.95f, 0.98f, 1f) : new Color(1f, 0.82f, 0.20f);

            var shadowObject = new GameObject($"{name} Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shadowObject.transform.SetParent(iconRoot, false);
            var shadowRect = shadowObject.GetComponent<RectTransform>();
            SetAnchors(shadowRect, Vector2.zero, Vector2.one, new Vector2(0f, -5f), new Vector2(0f, -5f));
            var shadowImage = shadowObject.GetComponent<Image>();
            shadowImage.sprite = GetCircleSprite();
            shadowImage.color = new Color(0f, 0f, 0f, 0.22f);
            shadowImage.raycastTarget = false;

            var outerObject = new GameObject($"{name} Outer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            outerObject.transform.SetParent(iconRoot, false);
            var outerRect = outerObject.GetComponent<RectTransform>();
            SetAnchors(outerRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var outerImage = outerObject.GetComponent<Image>();
            outerImage.sprite = GetCircleSprite();
            outerImage.color = outerColor;
            outerImage.raycastTarget = false;

            var ringObject = new GameObject($"{name} Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringObject.transform.SetParent(iconRoot, false);
            var ringRect = ringObject.GetComponent<RectTransform>();
            SetAnchors(ringRect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            var ringImage = ringObject.GetComponent<Image>();
            ringImage.sprite = GetCircleSprite();
            ringImage.color = ringColor;
            ringImage.raycastTarget = false;

            var bevelObject = new GameObject($"{name} Bevel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bevelObject.transform.SetParent(iconRoot, false);
            var bevelRect = bevelObject.GetComponent<RectTransform>();
            SetAnchors(bevelRect, new Vector2(0.11f, 0.12f), new Vector2(0.89f, 0.90f), Vector2.zero, Vector2.zero);
            var bevelImage = bevelObject.GetComponent<Image>();
            bevelImage.sprite = GetCircleSprite();
            bevelImage.color = bevelColor;
            bevelImage.raycastTarget = false;

            var innerObject = new GameObject($"{name} Inner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            innerObject.transform.SetParent(iconRoot, false);
            var innerRect = innerObject.GetComponent<RectTransform>();
            SetAnchors(innerRect, new Vector2(0.17f, 0.17f), new Vector2(0.83f, 0.83f), Vector2.zero, Vector2.zero);
            var innerImage = innerObject.GetComponent<Image>();
            innerImage.sprite = GetCircleSprite();
            innerImage.color = innerColor;
            innerImage.raycastTarget = false;
            targetImage = innerImage;

            var shineObject = new GameObject($"{name} Shine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shineObject.transform.SetParent(iconRoot, false);
            var shineRect = shineObject.GetComponent<RectTransform>();
            SetAnchors(shineRect, new Vector2(0.28f, 0.58f), new Vector2(0.60f, 0.80f), Vector2.zero, Vector2.zero);
            var shineImage = shineObject.GetComponent<Image>();
            shineImage.sprite = GetCircleSprite();
            shineImage.color = new Color(1f, 1f, 1f, 0.34f);
            shineImage.raycastTarget = false;

            var glyphShadowText = CreateText($"{name} Glyph Shadow", iconRoot, TextAnchor.MiddleCenter, glyphFontSize, FontStyle.Bold);
            glyphShadowText.text = glyph;
            glyphShadowText.color = new Color(0.03f, 0.17f, 0.27f, 0.42f);
            glyphShadowText.resizeTextMinSize = Mathf.Max(12, glyphFontSize - 16);
            SetAnchors(glyphShadowText.rectTransform, new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.82f), new Vector2(2f, -3f), new Vector2(2f, -3f));

            var glyphText = CreateText($"{name} Glyph", iconRoot, TextAnchor.MiddleCenter, glyphFontSize, FontStyle.Bold);
            glyphText.text = glyph;
            glyphText.color = glyphColor;
            glyphText.resizeTextMinSize = Mathf.Max(12, glyphFontSize - 16);
            SetAnchors(glyphText.rectTransform, new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);

            return iconRoot;
        }

        private static Text GetButtonLabel(Button button)
        {
            return button != null ? button.GetComponentInChildren<Text>() : null;
        }

        private static string FormatCompactGold(int gold)
        {
            var value = Mathf.Max(0, gold);
            if (value < 1000)
            {
                return value.ToString();
            }

            if (value < 1000000)
            {
                return FormatCompactAmount(value / 1000f, "K");
            }

            if (value < 1000000000)
            {
                return FormatCompactAmount(value / 1000000f, "M");
            }

            return FormatCompactAmount(value / 1000000000f, "B");
        }

        private static string FormatCompactAmount(float value, string suffix)
        {
            var rounded = value >= 100f
                ? Mathf.Floor(value)
                : value >= 10f
                    ? Mathf.Floor(value * 10f) / 10f
                    : Mathf.Floor(value * 10f) / 10f;

            return Mathf.Approximately(rounded % 1f, 0f)
                ? $"{rounded:0}{suffix}"
                : $"{rounded:0.#}{suffix}";
        }

        private static Sprite LoadGoldIconSprite()
        {
            return LoadResourceSprite(GoldIconResource);
        }

        private static Sprite LoadResourceSprite(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            if (runtimeResourceSprites.TryGetValue(resourcePath, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var runtimeSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeSprite.name = $"{resourcePath} Runtime Sprite";
            runtimeSprite.hideFlags = HideFlags.HideAndDontSave;
            runtimeResourceSprites[resourcePath] = runtimeSprite;
            return runtimeSprite;
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
            var iconSprite = LoadResourceSprite(iconResourcePath);
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
            var iconSprite = LoadResourceSprite(iconResourcePath);
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
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;

            badgeText = null;
            if (!createBadge)
            {
                return button;
            }

            var badgeObject = new GameObject($"{name} Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(iconRoot, false);
            var badgeRect = badgeObject.GetComponent<RectTransform>();
            SetAnchors(badgeRect, new Vector2(0.44f, 0.02f), new Vector2(0.98f, 0.31f), Vector2.zero, Vector2.zero);

            var badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.sprite = GetRoundedPanelSprite();
            badgeImage.type = Image.Type.Sliced;
            badgeImage.color = new Color(0.02f, 0.03f, 0.04f, 0.10f);
            badgeImage.raycastTarget = false;

            badgeText = CreateText($"{name} Badge Text", badgeObject.transform, TextAnchor.MiddleCenter, 20, FontStyle.Bold);
            badgeText.text = string.Empty;
            badgeText.resizeTextMinSize = 12;
            badgeText.color = new Color(1f, 1f, 1f, 0.96f);
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

        private static Sprite GetRoundedPanelSprite()
        {
            if (roundedPanelSprite != null)
            {
                return roundedPanelSprite;
            }

            const int width = 64;
            const int height = 32;
            const int radius = 12;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "UI Rounded Panel Texture",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cornerX = x < radius ? radius - x : x >= width - radius ? x - (width - radius - 1) : 0;
                    var cornerY = y < radius ? radius - y : y >= height - radius ? y - (height - radius - 1) : 0;
                    var inside = cornerX == 0 || cornerY == 0 || cornerX * cornerX + cornerY * cornerY <= radius * radius;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            roundedPanelSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedPanelSprite.name = "UI Rounded Panel Sprite";
            roundedPanelSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedPanelSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 96;
            const float radius = size * 0.48f;
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UI Circle Texture",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            circleSprite.name = "UI Circle Sprite";
            circleSprite.hideFlags = HideFlags.HideAndDontSave;
            return circleSprite;
        }

        private static Sprite GetGearIconSprite()
        {
            if (gearIconSprite != null)
            {
                return gearIconSprite;
            }

            const int size = 128;
            const float center = (size - 1) * 0.5f;
            const float radius = size * 0.46f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UI Gear Icon Texture",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x - center) / radius;
                    var ny = (y - center) / radius;
                    var r = Mathf.Sqrt(nx * nx + ny * ny);
                    var angle = Mathf.Atan2(ny, nx);
                    var toothWave = Mathf.Cos(angle * 8f);
                    var outerRadius = toothWave > 0.10f ? 0.94f : 0.74f;
                    var rootRadius = toothWave > 0.10f ? 0.66f : 0.70f;
                    var innerRadius = 0.34f;

                    var alpha = 0f;
                    if (r <= outerRadius && r >= innerRadius)
                    {
                        alpha = 1f;
                    }

                    if (r < innerRadius)
                    {
                        alpha = 0f;
                    }

                    if (r > rootRadius && toothWave <= 0.10f)
                    {
                        alpha = 0f;
                    }

                    var outerFade = Mathf.Clamp01((outerRadius - r) * 16f);
                    var innerFade = Mathf.Clamp01((r - innerRadius) * 16f);
                    alpha *= Mathf.Min(outerFade, innerFade);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            gearIconSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            gearIconSprite.name = "UI Gear Icon Sprite";
            gearIconSprite.hideFlags = HideFlags.HideAndDontSave;
            return gearIconSprite;
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
