using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
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

        private static Button CreateRoundIconButton(
            string name,
            Transform parent,
            string glyph,
            float size,
            int glyphFontSize)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var hitArea = buttonObject.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            CreateRoundIconVisual($"{name} Icon", buttonObject.transform, glyph, size, glyphFontSize, out var targetImage);

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
            out Image targetImage)
        {
            var iconRoot = CreateCenteredSquare($"{name} Root", parent, size);

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
            outerImage.color = new Color(0.82f, 0.42f, 0.11f);
            outerImage.raycastTarget = false;

            var ringObject = new GameObject($"{name} Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringObject.transform.SetParent(iconRoot, false);
            var ringRect = ringObject.GetComponent<RectTransform>();
            SetAnchors(ringRect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            var ringImage = ringObject.GetComponent<Image>();
            ringImage.sprite = GetCircleSprite();
            ringImage.color = new Color(1.00f, 0.70f, 0.24f);
            ringImage.raycastTarget = false;

            var bevelObject = new GameObject($"{name} Bevel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bevelObject.transform.SetParent(iconRoot, false);
            var bevelRect = bevelObject.GetComponent<RectTransform>();
            SetAnchors(bevelRect, new Vector2(0.11f, 0.12f), new Vector2(0.89f, 0.90f), Vector2.zero, Vector2.zero);
            var bevelImage = bevelObject.GetComponent<Image>();
            bevelImage.sprite = GetCircleSprite();
            bevelImage.color = new Color(0.49f, 0.24f, 0.08f, 0.34f);
            bevelImage.raycastTarget = false;

            var innerObject = new GameObject($"{name} Inner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            innerObject.transform.SetParent(iconRoot, false);
            var innerRect = innerObject.GetComponent<RectTransform>();
            SetAnchors(innerRect, new Vector2(0.17f, 0.17f), new Vector2(0.83f, 0.83f), Vector2.zero, Vector2.zero);
            var innerImage = innerObject.GetComponent<Image>();
            innerImage.sprite = GetCircleSprite();
            innerImage.color = new Color(0.10f, 0.66f, 0.90f);
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
            glyphText.color = new Color(1f, 0.82f, 0.20f);
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
            var sprite = Resources.Load<Sprite>(GoldIconResource);
            if (sprite != null)
            {
                return sprite;
            }

            if (runtimeGoldIconSprite != null)
            {
                return runtimeGoldIconSprite;
            }

            var texture = Resources.Load<Texture2D>(GoldIconResource);
            if (texture == null)
            {
                return null;
            }

            runtimeGoldIconSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeGoldIconSprite.name = "Runtime Gold Icon Sprite";
            runtimeGoldIconSprite.hideFlags = HideFlags.HideAndDontSave;
            return runtimeGoldIconSprite;
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
            var iconSprite = Resources.Load<Sprite>(iconResourcePath);
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
            var iconSprite = Resources.Load<Sprite>(iconResourcePath);
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
            colors.disabledColor = Color.white;
            button.colors = colors;

            badgeText = null;
            if (!createBadge)
            {
                return button;
            }

            var badgeObject = new GameObject($"{name} Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(iconRoot, false);
            var badgeRect = badgeObject.GetComponent<RectTransform>();
            SetAnchors(badgeRect, new Vector2(0.48f, 0.02f), new Vector2(0.98f, 0.31f), Vector2.zero, Vector2.zero);

            var badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.color = new Color(0.06f, 0.08f, 0.10f, 0.82f);
            badgeImage.raycastTarget = false;

            badgeText = CreateText($"{name} Badge Text", badgeObject.transform, TextAnchor.MiddleCenter, 20, FontStyle.Bold);
            badgeText.text = string.Empty;
            badgeText.resizeTextMinSize = 12;
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

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
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
