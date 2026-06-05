using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    internal static class PassengerPoseAnimator
    {
        private const float MoveLift = 0.18f;

        public static IEnumerator MoveTo(Transform target, Vector3 targetPosition, float duration, Action onComplete)
        {
            var startPosition = target.position;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                var position = Vector3.Lerp(startPosition, targetPosition, easedTime);
                position.y += Mathf.Sin(easedTime * Mathf.PI) * MoveLift;
                target.position = position;
                yield return null;
            }

            target.position = targetPosition;
            onComplete?.Invoke();
        }

        public static IEnumerator MoveToPose(
            Transform target,
            PassengerModel model,
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration,
            Action onComplete)
        {
            var startPosition = target.position;
            var startRotation = target.rotation;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                var position = Vector3.Lerp(startPosition, targetPosition, easedTime);
                position.y += Mathf.Sin(easedTime * Mathf.PI) * MoveLift;
                target.SetPositionAndRotation(position, Quaternion.Slerp(startRotation, targetRotation, easedTime));
                yield return null;
            }

            target.SetPositionAndRotation(targetPosition, targetRotation);
            model.ApplyDefaultPersonLocalPositions();
            onComplete?.Invoke();
        }

        public static IEnumerator MoveAlongPoses(
            Transform target,
            PassengerModel model,
            PassengerUnitRoadPose[] poses,
            float duration,
            Action onComplete)
        {
            var elapsed = 0f;
            var segmentCount = poses.Length - 1;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var scaledTime = normalizedTime * segmentCount;
                var segmentIndex = Mathf.Min(segmentCount - 1, Mathf.FloorToInt(scaledTime));
                var segmentTime = Mathf.SmoothStep(0f, 1f, scaledTime - segmentIndex);
                var startPose = poses[segmentIndex];
                var endPose = poses[segmentIndex + 1];
                ApplyInterpolatedPose(target, model, startPose, endPose, segmentTime, Mathf.Sin(normalizedTime * Mathf.PI) * MoveLift * 0.25f);
                yield return null;
            }

            ApplyPose(target, model, poses[poses.Length - 1]);
            onComplete?.Invoke();
        }

        public static IEnumerator MoveAlongDynamicPose(
            Transform target,
            PassengerModel model,
            Func<float, PassengerUnitRoadPose> getPose,
            float duration,
            Action onComplete)
        {
            var elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                ApplyPose(target, model, getPose(Mathf.SmoothStep(0f, 1f, normalizedTime)));
                yield return null;
            }

            ApplyPose(target, model, getPose(1f));
            onComplete?.Invoke();
        }

        public static void ApplyPose(Transform target, PassengerModel model, PassengerUnitRoadPose pose)
        {
            target.SetPositionAndRotation(pose.Position, pose.Rotation);
            model.ApplyPosePersonLocalPositions(pose);
        }

        private static void ApplyInterpolatedPose(
            Transform target,
            PassengerModel model,
            PassengerUnitRoadPose startPose,
            PassengerUnitRoadPose endPose,
            float t,
            float lift)
        {
            var position = Vector3.Lerp(startPose.Position, endPose.Position, t);
            position.y += lift;
            target.SetPositionAndRotation(position, Quaternion.Slerp(startPose.Rotation, endPose.Rotation, t));
            model.ApplyInterpolatedPersonLocalPositions(startPose, endPose, t);
        }
    }
}
