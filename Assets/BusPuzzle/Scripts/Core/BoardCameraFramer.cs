using UnityEngine;

namespace BusPuzzle
{
    internal static class BoardCameraFramer
    {
        private const float CameraPitchDegrees = 62f;
        private const float CameraDistance = 8.15f;
        private const float MinOrthographicSize = 4.46f;
        private const float MaxOrthographicSize = 5.90f;
        private const float TopUiInset = 0.055f;
        private const float BottomUiInset = 0.105f;
        private const float HorizontalUiInset = 0.045f;

        public static void Apply(Camera camera, Bounds contentBounds)
        {
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.transform.rotation = Quaternion.Euler(CameraPitchDegrees, 0f, 0f);

            var aspect = GetCameraAspect(camera);
            var usableHeight = Mathf.Clamp(1f - TopUiInset - BottomUiInset, 0.62f, 0.92f);
            var usableWidth = Mathf.Clamp(1f - HorizontalUiInset * 2f, 0.72f, 0.96f);
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

            var requiredForHeight = (maxY - minY) / (2f * usableHeight);
            var requiredForWidth = (maxX - minX) / (2f * aspect * usableWidth);
            camera.orthographicSize = Mathf.Clamp(
                Mathf.Max(requiredForHeight, requiredForWidth),
                MinOrthographicSize,
                MaxOrthographicSize);

            var usableCenter = BottomUiInset + usableHeight * 0.5f;
            var targetLocalY = (usableCenter - 0.5f) * 2f * camera.orthographicSize;
            camera.transform.position =
                center -
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
