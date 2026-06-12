using UnityEngine;

namespace BusPuzzle
{
    internal static class BoardCameraFramer
    {
        private const float CameraPitchDegrees = 62f;
        private const float CameraDistance = 8.15f;
        private const float MinOrthographicSize = 4.20f;
        private const float MaxOrthographicSize = 5.95f;
        private const float TopUiInset = 0.060f;
        private const float BottomUiInset = 0.130f;
        private const float HorizontalUiInset = 0.018f;
        private const float SafeFitPaddingScale = 1.04f;

        public static void Apply(Camera camera, Bounds contentBounds)
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            Apply(camera, contentBounds, Screen.safeArea, screenSize);
        }

        public static void Apply(Camera camera, Bounds contentBounds, Rect safeArea, Vector2Int screenSize)
        {
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.transform.rotation = Quaternion.Euler(CameraPitchDegrees, 0f, 0f);

            var aspect = GetCameraAspect(camera);
            var safeAreaRatios = GetSafeAreaRatios(safeArea, screenSize);
            var safeWidthRatio = Mathf.Clamp(safeAreaRatios.width, 0.55f, 1f);
            var safeHeightRatio = Mathf.Clamp(safeAreaRatios.height, 0.55f, 1f);
            var usableHeight = Mathf.Clamp(1f - TopUiInset - BottomUiInset, 0.62f, 0.92f);
            var usableWidth = Mathf.Clamp(1f - HorizontalUiInset * 2f, 0.72f, 0.995f);
            var corners = GetBoundsCorners(contentBounds);
            var center = contentBounds.center;
            var right = camera.transform.right;
            var up = camera.transform.up;
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;

            for (var index = 0; index < corners.Length; index++)
            {
                var delta = corners[index] - center;
                var projectedX = Vector3.Dot(delta, right);
                var projectedY = Vector3.Dot(delta, up);
                minX = Mathf.Min(minX, projectedX);
                maxX = Mathf.Max(maxX, projectedX);
                minY = Mathf.Min(minY, projectedY);
                maxY = Mathf.Max(maxY, projectedY);
            }

            var requiredForHeight = (maxY - minY) / (2f * safeHeightRatio * usableHeight);
            var requiredForWidth = (maxX - minX) / (2f * aspect * safeWidthRatio * usableWidth);

            var requiredSize = Mathf.Max(requiredForHeight, requiredForWidth) * SafeFitPaddingScale;
            camera.orthographicSize = Mathf.Clamp(requiredSize, MinOrthographicSize, MaxOrthographicSize);

            var usableCenterX = safeAreaRatios.x + safeAreaRatios.width * 0.5f;
            var usableCenterY = safeAreaRatios.y + safeAreaRatios.height * (BottomUiInset + usableHeight * 0.5f);
            var targetLocalX = (usableCenterX - 0.5f) * 2f * camera.orthographicSize * aspect;
            var targetLocalY = (usableCenterY - 0.5f) * 2f * camera.orthographicSize;
            camera.transform.position =
                center -
                camera.transform.right * targetLocalX -
                camera.transform.up * targetLocalY -
                camera.transform.forward * CameraDistance;
        }

        private static float GetCameraAspect(Camera camera)
        {
            if (camera.aspect > 0.01f)
            {
                return camera.aspect;
            }

            return Screen.height > 0
                ? Mathf.Max(0.01f, Screen.width / (float)Screen.height)
                : 9f / 16f;
        }

        private static Rect GetSafeAreaRatios(Rect safeArea, Vector2Int screenSize)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0 || safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            var xMin = Mathf.Clamp01(safeArea.xMin / screenSize.x);
            var yMin = Mathf.Clamp01(safeArea.yMin / screenSize.y);
            var xMax = Mathf.Clamp01(safeArea.xMax / screenSize.x);
            var yMax = Mathf.Clamp01(safeArea.yMax / screenSize.y);

            return new Rect(xMin, yMin, Mathf.Max(0.01f, xMax - xMin), Mathf.Max(0.01f, yMax - yMin));
        }

        private static Vector3[] GetBoundsCorners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            return new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };
        }
    }
}
