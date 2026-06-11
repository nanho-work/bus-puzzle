using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private const string SuperHardBannerResource = "UI/Boosters/Super Hard";
        private const float DifficultyBannerFadeInDuration = 0.22f;
        private const float DifficultyBannerHoldDuration = 0.62f;
        private const float DifficultyBannerFadeOutDuration = 0.36f;
        private const float DifficultyBannerPeakAlpha = 0.94f;

        private RectTransform difficultyBannerRoot;
        private CanvasGroup difficultyBannerCanvasGroup;
        private Coroutine difficultyBannerRoutine;
        private bool difficultyBannerAvailable;

        public void ShowSuperHardBanner()
        {
            if (!difficultyBannerAvailable || difficultyBannerRoot == null || difficultyBannerCanvasGroup == null)
            {
                return;
            }

            if (difficultyBannerRoutine != null)
            {
                StopCoroutine(difficultyBannerRoutine);
            }

            difficultyBannerRoutine = StartCoroutine(PlayDifficultyBannerRoutine());
        }

        private void BuildDifficultyBanner()
        {
            difficultyBannerRoot = CreateRectTransform("Difficulty Banner", safeAreaRoot);
            SetAnchors(difficultyBannerRoot, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);

            difficultyBannerCanvasGroup = difficultyBannerRoot.gameObject.AddComponent<CanvasGroup>();
            difficultyBannerCanvasGroup.alpha = 0f;
            difficultyBannerCanvasGroup.blocksRaycasts = false;
            difficultyBannerCanvasGroup.interactable = false;

            var bannerSprite = LoadResourceSprite(SuperHardBannerResource);
            difficultyBannerAvailable = bannerSprite != null;
            var bannerObject = new GameObject("Super Hard Banner Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            bannerObject.transform.SetParent(difficultyBannerRoot, false);
            var bannerRect = bannerObject.GetComponent<RectTransform>();
            SetAnchors(bannerRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var bannerImage = bannerObject.GetComponent<Image>();
            bannerImage.sprite = bannerSprite;
            bannerImage.color = Color.white;
            bannerImage.preserveAspect = true;
            bannerImage.raycastTarget = false;

            var aspectFitter = bannerObject.GetComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = bannerSprite != null && bannerSprite.rect.height > 0f
                ? bannerSprite.rect.width / bannerSprite.rect.height
                : 4.4f;

            difficultyBannerRoot.gameObject.SetActive(false);
        }

        private void HideDifficultyBanner()
        {
            if (difficultyBannerRoutine != null)
            {
                StopCoroutine(difficultyBannerRoutine);
                difficultyBannerRoutine = null;
            }

            if (difficultyBannerCanvasGroup != null)
            {
                difficultyBannerCanvasGroup.alpha = 0f;
            }

            if (difficultyBannerRoot != null)
            {
                difficultyBannerRoot.gameObject.SetActive(false);
            }
        }

        private IEnumerator PlayDifficultyBannerRoutine()
        {
            difficultyBannerRoot.gameObject.SetActive(true);
            difficultyBannerRoot.SetAsLastSibling();

            yield return FadeDifficultyBanner(0f, DifficultyBannerPeakAlpha, DifficultyBannerFadeInDuration);
            yield return new WaitForSecondsRealtime(DifficultyBannerHoldDuration);
            yield return FadeDifficultyBanner(DifficultyBannerPeakAlpha, 0f, DifficultyBannerFadeOutDuration);

            difficultyBannerRoot.gameObject.SetActive(false);
            difficultyBannerRoutine = null;
        }

        private IEnumerator FadeDifficultyBanner(float startAlpha, float endAlpha, float duration)
        {
            duration = Mathf.Max(0.01f, duration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                difficultyBannerCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            difficultyBannerCanvasGroup.alpha = endAlpha;
        }
    }
}
