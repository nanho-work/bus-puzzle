using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private const float StartupSplashSettleSeconds = 0.35f;
        private const float StartupSplashMaxSettleSeconds = 1f;
        private const float StartupSplashHoldSeconds = 1.65f;
        private const float StartupSplashFadeSeconds = 0.45f;

        private static bool ShouldShowRuntimeStartupSplash()
        {
#if UNITY_IOS
            return false;
#else
            return true;
#endif
        }

        private void BuildStartupSplashOverlay()
        {
            var splashSprite = LoadResourceSprite(StartupSplashResource);
            if (splashSprite == null)
            {
                return;
            }

            startupSplashRoot = CreateRectTransform("Startup Splash Overlay", transform);
            SetAnchors(startupSplashRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            startupSplashRoot.SetAsLastSibling();

            var blocker = startupSplashRoot.gameObject.AddComponent<Image>();
            blocker.color = Color.black;
            blocker.raycastTarget = true;

            startupSplashCanvasGroup = startupSplashRoot.gameObject.AddComponent<CanvasGroup>();
            startupSplashCanvasGroup.alpha = 1f;
            startupSplashCanvasGroup.blocksRaycasts = true;
            startupSplashCanvasGroup.interactable = true;

            var imageObject = new GameObject("Startup Splash Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            imageObject.transform.SetParent(startupSplashRoot, false);

            var imageRect = imageObject.GetComponent<RectTransform>();
            SetAnchors(imageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            startupSplashImageCanvasGroup = imageObject.AddComponent<CanvasGroup>();
            startupSplashImageCanvasGroup.alpha = 0f;
            startupSplashImageCanvasGroup.blocksRaycasts = false;
            startupSplashImageCanvasGroup.interactable = false;

            var image = imageObject.GetComponent<Image>();
            image.sprite = splashSprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var fitter = imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = Mathf.Max(0.01f, splashSprite.rect.width / splashSprite.rect.height);

            StartCoroutine(PlayStartupSplashRoutine());
        }

        private IEnumerator PlayStartupSplashRoutine()
        {
            yield return WaitForStartupOrientationSettle();

            if (startupSplashImageCanvasGroup != null)
            {
                startupSplashImageCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSecondsRealtime(StartupSplashHoldSeconds);

            var elapsed = 0f;
            while (elapsed < StartupSplashFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / StartupSplashFadeSeconds);
                if (startupSplashCanvasGroup != null)
                {
                    startupSplashCanvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
                }

                yield return null;
            }

            if (startupSplashRoot != null)
            {
                Destroy(startupSplashRoot.gameObject);
                startupSplashRoot = null;
                startupSplashCanvasGroup = null;
                startupSplashImageCanvasGroup = null;
            }
        }

        private static IEnumerator WaitForStartupOrientationSettle()
        {
            var elapsed = 0f;
            var portraitElapsed = 0f;

            while (elapsed < StartupSplashMaxSettleSeconds && portraitElapsed < StartupSplashSettleSeconds)
            {
                LockStartupPortraitOrientation();

                var delta = Time.unscaledDeltaTime;
                elapsed += delta;

                if (IsStartupPortraitFrame())
                {
                    portraitElapsed += delta;
                }
                else
                {
                    portraitElapsed = 0f;
                }

                yield return null;
            }
        }

        private static void LockStartupPortraitOrientation()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private static bool IsStartupPortraitFrame()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return false;
            }

            return Screen.height >= Screen.width &&
                Screen.orientation != ScreenOrientation.PortraitUpsideDown &&
                Screen.orientation != ScreenOrientation.LandscapeLeft &&
                Screen.orientation != ScreenOrientation.LandscapeRight;
        }
    }
}
