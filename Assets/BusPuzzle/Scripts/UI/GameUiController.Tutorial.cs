using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private const float TutorialCalloutWidth = 760f;
        private const float TutorialCalloutHeight = 154f;
        private const float TutorialUiHighlightBorderThickness = 7f;

        private RectTransform tutorialOverlay;
        private RectTransform tutorialDimBottom;
        private RectTransform tutorialDimTop;
        private RectTransform tutorialDimLeft;
        private RectTransform tutorialDimRight;
        private RectTransform tutorialUiHighlight;
        private Image tutorialUiHighlightTop;
        private Image tutorialUiHighlightBottom;
        private Image tutorialUiHighlightLeft;
        private Image tutorialUiHighlightRight;
        private RectTransform tutorialCallout;
        private Text tutorialText;
        private Camera tutorialWorldCamera;
        private RectTransform tutorialRectTarget;
        private Vector3[] tutorialWorldCorners;
        private Vector3 tutorialWorldTarget;
        private Vector2 tutorialScreenTarget;
        private string tutorialMessage;
        private float tutorialRadiusPixels;
        private float tutorialRectPaddingPixels;
        private bool tutorialUsesWorldRect;
        private bool tutorialUsesWorldTarget;
        private bool tutorialUsesRectTarget;
        private float tutorialPulseTime;

        public bool IsTutorialVisible => tutorialOverlay != null && tutorialOverlay.gameObject.activeSelf;

        public void ShowTutorialForWorld(Camera camera, Vector3 worldPosition, float radiusPixels, string message)
        {
            tutorialWorldCamera = camera;
            tutorialWorldTarget = worldPosition;
            tutorialRadiusPixels = Mathf.Max(40f, radiusPixels);
            tutorialMessage = message ?? string.Empty;
            tutorialUsesWorldRect = false;
            tutorialUsesWorldTarget = true;
            tutorialUsesRectTarget = false;
            tutorialRectTarget = null;
            tutorialWorldCorners = null;
            EnsureTutorialOverlay();
            tutorialOverlay.gameObject.SetActive(true);
            UpdateTutorialOverlay();
        }

        public void ShowTutorialForWorldRect(Camera camera, Vector3[] worldCorners, float paddingPixels, string message)
        {
            if (camera == null || worldCorners == null || worldCorners.Length == 0)
            {
                HideTutorial();
                return;
            }

            tutorialWorldCamera = camera;
            tutorialWorldCorners = new Vector3[worldCorners.Length];
            for (var index = 0; index < worldCorners.Length; index++)
            {
                tutorialWorldCorners[index] = worldCorners[index];
            }

            tutorialRectPaddingPixels = Mathf.Max(0f, paddingPixels);
            tutorialMessage = message ?? string.Empty;
            tutorialUsesWorldRect = true;
            tutorialUsesWorldTarget = false;
            tutorialUsesRectTarget = false;
            tutorialRectTarget = null;
            EnsureTutorialOverlay();
            tutorialOverlay.gameObject.SetActive(true);
            UpdateTutorialOverlay();
        }

        public void ShowTutorialForScreen(Vector2 screenPosition, float radiusPixels, string message)
        {
            tutorialScreenTarget = screenPosition;
            tutorialRadiusPixels = Mathf.Max(40f, radiusPixels);
            tutorialMessage = message ?? string.Empty;
            tutorialUsesWorldRect = false;
            tutorialUsesWorldTarget = false;
            tutorialUsesRectTarget = false;
            tutorialRectTarget = null;
            tutorialWorldCorners = null;
            EnsureTutorialOverlay();
            tutorialOverlay.gameObject.SetActive(true);
            UpdateTutorialOverlay();
        }

        public void ShowTutorialForVipButton(string message)
        {
            ShowTutorialForButton(vipButton, message);
        }

        public void ShowTutorialForMixButton(string message)
        {
            ShowTutorialForButton(mixButton, message);
        }

        public void ShowTutorialForDepartButton(string message)
        {
            ShowTutorialForButton(departButton, message);
        }

        public void HideTutorial()
        {
            tutorialWorldCamera = null;
            tutorialRectTarget = null;
            tutorialWorldCorners = null;
            tutorialUsesWorldRect = false;
            tutorialUsesWorldTarget = false;
            tutorialUsesRectTarget = false;
            HideTutorialUiHighlight();
            if (tutorialOverlay != null)
            {
                tutorialOverlay.gameObject.SetActive(false);
            }
        }

        private void ShowTutorialForButton(Button button, string message)
        {
            if (button == null)
            {
                HideTutorial();
                return;
            }

            var iconRoot = button.transform.Find($"{button.name} Icon Root") as RectTransform;
            tutorialRectTarget = iconRoot != null ? iconRoot : button.GetComponent<RectTransform>();
            tutorialRadiusPixels = 72f;
            tutorialMessage = message ?? string.Empty;
            tutorialUsesWorldRect = false;
            tutorialUsesWorldTarget = false;
            tutorialUsesRectTarget = true;
            tutorialWorldCorners = null;
            EnsureTutorialOverlay();
            tutorialOverlay.gameObject.SetActive(true);
            UpdateTutorialOverlay();
        }

        private void EnsureTutorialOverlay()
        {
            if (tutorialOverlay != null)
            {
                return;
            }

            var root = EnsureSafeAreaRoot();
            tutorialOverlay = CreateRectTransform("Tutorial Overlay", root);
            SetAnchors(tutorialOverlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tutorialOverlay.gameObject.SetActive(false);

            tutorialDimBottom = CreateTutorialDimPanel("Tutorial Dim Bottom", tutorialOverlay);
            tutorialDimTop = CreateTutorialDimPanel("Tutorial Dim Top", tutorialOverlay);
            tutorialDimLeft = CreateTutorialDimPanel("Tutorial Dim Left", tutorialOverlay);
            tutorialDimRight = CreateTutorialDimPanel("Tutorial Dim Right", tutorialOverlay);

            tutorialUiHighlight = CreateRectTransform("Tutorial UI Icon Highlight", tutorialOverlay);
            tutorialUiHighlight.anchorMin = new Vector2(0.5f, 0.5f);
            tutorialUiHighlight.anchorMax = new Vector2(0.5f, 0.5f);
            tutorialUiHighlight.pivot = new Vector2(0.5f, 0.5f);
            tutorialUiHighlight.gameObject.SetActive(false);
            tutorialUiHighlightTop = CreateTutorialUiHighlightEdge("Tutorial UI Icon Highlight Top", tutorialUiHighlight);
            tutorialUiHighlightBottom = CreateTutorialUiHighlightEdge("Tutorial UI Icon Highlight Bottom", tutorialUiHighlight);
            tutorialUiHighlightLeft = CreateTutorialUiHighlightEdge("Tutorial UI Icon Highlight Left", tutorialUiHighlight);
            tutorialUiHighlightRight = CreateTutorialUiHighlightEdge("Tutorial UI Icon Highlight Right", tutorialUiHighlight);

            tutorialCallout = CreateRoundedPanel("Tutorial Callout", tutorialOverlay, new Color(0.08f, 0.12f, 0.15f, 0.96f));
            tutorialCallout.anchorMin = new Vector2(0.5f, 0.5f);
            tutorialCallout.anchorMax = new Vector2(0.5f, 0.5f);
            tutorialCallout.pivot = new Vector2(0.5f, 0.5f);
            tutorialCallout.sizeDelta = new Vector2(TutorialCalloutWidth, TutorialCalloutHeight);
            var calloutShadow = tutorialCallout.gameObject.AddComponent<Shadow>();
            calloutShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            calloutShadow.effectDistance = new Vector2(0f, -7f);

            tutorialText = CreateText("Tutorial Text", tutorialCallout, TextAnchor.MiddleCenter, 36, FontStyle.Normal);
            tutorialText.color = new Color(0.98f, 1f, 1f, 0.98f);
            tutorialText.resizeTextMinSize = 22;
            SetAnchors(tutorialText.rectTransform, Vector2.zero, Vector2.one, new Vector2(30f, 10f), new Vector2(-30f, -10f));
        }

        private static RectTransform CreateTutorialDimPanel(string name, Transform parent)
        {
            var panel = CreatePanel(name, parent, new Color(0.02f, 0.03f, 0.04f, 0.66f));
            panel.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        private static Image CreateTutorialUiHighlightEdge(string name, Transform parent)
        {
            var edge = CreatePanel(name, parent, new Color(1.00f, 0.87f, 0.12f, 0.94f));
            edge.GetComponent<Image>().raycastTarget = false;
            return edge.GetComponent<Image>();
        }

        private void UpdateTutorialOverlay()
        {
            if (tutorialOverlay == null || !tutorialOverlay.gameObject.activeSelf || safeAreaRoot == null)
            {
                return;
            }

            if (!TryGetTutorialHole(out var hole))
            {
                HideTutorial();
                return;
            }

            var rootRect = safeAreaRoot.rect;
            var width = rootRect.width;
            var height = rootRect.height;
            if (width <= 1f || height <= 1f)
            {
                return;
            }

            var left = Mathf.Clamp(hole.xMin - rootRect.xMin, 0f, width);
            var right = Mathf.Clamp(hole.xMax - rootRect.xMin, 0f, width);
            var bottom = Mathf.Clamp(hole.yMin - rootRect.yMin, 0f, height);
            var top = Mathf.Clamp(hole.yMax - rootRect.yMin, 0f, height);
            if (right < left)
            {
                var swap = right;
                right = left;
                left = swap;
            }

            if (top < bottom)
            {
                var swap = top;
                top = bottom;
                bottom = swap;
            }

            SetAbsoluteRect(tutorialDimBottom, 0f, 0f, width, bottom);
            SetAbsoluteRect(tutorialDimTop, 0f, top, width, height);
            SetAbsoluteRect(tutorialDimLeft, 0f, bottom, left, top);
            SetAbsoluteRect(tutorialDimRight, right, bottom, width, top);

            var center = hole.center;
            var diameter = Mathf.Max(hole.width, hole.height);
            tutorialPulseTime += Time.unscaledDeltaTime;
            if (tutorialUsesRectTarget)
            {
                ShowTutorialUiHighlight(hole);
            }
            else
            {
                HideTutorialUiHighlight();
            }

            var calloutY = center.y + diameter * 0.5f + TutorialCalloutHeight * 0.60f;
            if (calloutY + TutorialCalloutHeight * 0.5f > rootRect.yMax - 16f)
            {
                calloutY = center.y - diameter * 0.5f - TutorialCalloutHeight * 0.60f;
            }

            var calloutX = Mathf.Clamp(
                center.x,
                rootRect.xMin + TutorialCalloutWidth * 0.5f + 18f,
                rootRect.xMax - TutorialCalloutWidth * 0.5f - 18f);
            tutorialCallout.anchoredPosition = new Vector2(calloutX, calloutY);
            tutorialText.text = tutorialMessage;
        }

        private bool TryGetTutorialHole(out Rect hole)
        {
            if (tutorialUsesRectTarget && tutorialRectTarget != null)
            {
                return TryGetRectTargetHole(tutorialRectTarget, out hole);
            }

            if (tutorialUsesWorldRect && tutorialWorldCorners != null)
            {
                return TryGetWorldRectTargetHole(tutorialWorldCorners, tutorialRectPaddingPixels, out hole);
            }

            var screenPosition = tutorialScreenTarget;
            if (tutorialUsesWorldTarget)
            {
                if (tutorialWorldCamera == null)
                {
                    hole = default;
                    return false;
                }

                screenPosition = tutorialWorldCamera.WorldToScreenPoint(tutorialWorldTarget);
            }

            return TryGetScreenTargetHole(screenPosition, tutorialRadiusPixels, out hole);
        }

        private bool TryGetWorldRectTargetHole(Vector3[] worldCorners, float paddingPixels, out Rect hole)
        {
            if (tutorialWorldCamera == null || worldCorners == null || worldCorners.Length == 0)
            {
                hole = default;
                return false;
            }

            var hasPoint = false;
            var screenMin = new Vector2(float.MaxValue, float.MaxValue);
            var screenMax = new Vector2(float.MinValue, float.MinValue);
            for (var index = 0; index < worldCorners.Length; index++)
            {
                var screenPoint3 = tutorialWorldCamera.WorldToScreenPoint(worldCorners[index]);
                if (screenPoint3.z < 0f)
                {
                    continue;
                }

                var screenPoint = new Vector2(screenPoint3.x, screenPoint3.y);
                hasPoint = true;
                screenMin = Vector2.Min(screenMin, screenPoint);
                screenMax = Vector2.Max(screenMax, screenPoint);
            }

            if (!hasPoint)
            {
                hole = default;
                return false;
            }

            var padding = Vector2.one * Mathf.Max(0f, paddingPixels);
            screenMin -= padding;
            screenMax += padding;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(safeAreaRoot, screenMin, null, out var localMin) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(safeAreaRoot, screenMax, null, out var localMax))
            {
                hole = default;
                return false;
            }

            hole = Rect.MinMaxRect(
                Mathf.Min(localMin.x, localMax.x),
                Mathf.Min(localMin.y, localMax.y),
                Mathf.Max(localMin.x, localMax.x),
                Mathf.Max(localMin.y, localMax.y));
            return true;
        }

        private bool TryGetScreenTargetHole(Vector2 screenPosition, float radiusPixels, out Rect hole)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(safeAreaRoot, screenPosition, null, out var localCenter) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(safeAreaRoot, screenPosition + Vector2.right * radiusPixels, null, out var localRight) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(safeAreaRoot, screenPosition + Vector2.up * radiusPixels, null, out var localTop))
            {
                hole = default;
                return false;
            }

            var radius = Mathf.Max(
                42f,
                Mathf.Max(Mathf.Abs(localRight.x - localCenter.x), Mathf.Abs(localTop.y - localCenter.y)));
            hole = new Rect(
                localCenter.x - radius,
                localCenter.y - radius,
                radius * 2f,
                radius * 2f);
            return true;
        }

        private bool TryGetRectTargetHole(RectTransform target, out Rect hole)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var hasPoint = false;
            var localMin = new Vector2(float.MaxValue, float.MaxValue);
            var localMax = new Vector2(float.MinValue, float.MinValue);
            for (var index = 0; index < corners.Length; index++)
            {
                var screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[index]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(safeAreaRoot, screenPoint, null, out var localPoint))
                {
                    continue;
                }

                hasPoint = true;
                localMin = Vector2.Min(localMin, localPoint);
                localMax = Vector2.Max(localMax, localPoint);
            }

            if (!hasPoint)
            {
                hole = default;
                return false;
            }

            const float padding = 12f;
            localMin -= Vector2.one * padding;
            localMax += Vector2.one * padding;
            hole = Rect.MinMaxRect(localMin.x, localMin.y, localMax.x, localMax.y);
            return true;
        }

        private static void SetAbsoluteRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(xMin, yMin);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }

        private void ShowTutorialUiHighlight(Rect hole)
        {
            if (tutorialUiHighlight == null)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin(tutorialPulseTime * 6.2f) * 0.045f;
            tutorialUiHighlight.gameObject.SetActive(true);
            tutorialUiHighlight.anchoredPosition = hole.center;
            tutorialUiHighlight.sizeDelta = new Vector2(hole.width, hole.height);
            tutorialUiHighlight.localScale = Vector3.one * pulse;
            LayoutTutorialUiHighlightFrame();
        }

        private void HideTutorialUiHighlight()
        {
            if (tutorialUiHighlight == null)
            {
                return;
            }

            tutorialUiHighlight.gameObject.SetActive(false);
            tutorialUiHighlight.localScale = Vector3.one;
        }

        private void LayoutTutorialUiHighlightFrame()
        {
            var thickness = TutorialUiHighlightBorderThickness;
            SetHighlightEdge(tutorialUiHighlightTop, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness), new Vector2(0f, -thickness * 0.5f));
            SetHighlightEdge(tutorialUiHighlightBottom, Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, thickness), new Vector2(0f, thickness * 0.5f));
            SetHighlightEdge(tutorialUiHighlightLeft, Vector2.zero, new Vector2(0f, 1f), new Vector2(thickness, 0f), new Vector2(thickness * 0.5f, 0f));
            SetHighlightEdge(tutorialUiHighlightRight, new Vector2(1f, 0f), Vector2.one, new Vector2(thickness, 0f), new Vector2(-thickness * 0.5f, 0f));
        }

        private static void SetHighlightEdge(Image edge, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            if (edge == null)
            {
                return;
            }

            var rect = edge.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
