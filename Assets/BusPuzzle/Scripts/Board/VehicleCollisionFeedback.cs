using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleCollisionFeedback
    {
        private const float BlockedCollisionMoveSpeed = 3.85f;
        private const float BlockedCollisionMinMoveDuration = 0.11f;
        private const float BlockedCollisionMaxMoveDuration = 0.26f;
        private const float BlockedCollisionImpactDuration = 0.13f;
        private const float BlockedCollisionReturnDuration = 0.18f;
        private const float BlockedCollisionPitchDegrees = 8.0f;
        private const float BlockedCollisionRecoilFactor = 0.12f;
        private const float BlockingBusShakeDuration = 0.30f;
        private const float BlockingBusShakeDistanceFactor = 0.17f;
        private const float BlockingBusShakeYawDegrees = 5.5f;

        public static IEnumerator PlayBlockedCollision(
            Transform target,
            float cellSize,
            Vector3 collisionPosition,
            Vector3 worldDirection,
            Action onImpact,
            Action onComplete)
        {
            var startPosition = target.position;
            var startRotation = target.rotation;
            var direction = NormalizeDirection(worldDirection, target.forward);

            collisionPosition.y = startPosition.y;
            var distance = Vector3.Distance(startPosition, collisionPosition);
            var moveDuration = Mathf.Clamp(distance / BlockedCollisionMoveSpeed, BlockedCollisionMinMoveDuration, BlockedCollisionMaxMoveDuration);
            var elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
                target.SetPositionAndRotation(Vector3.Lerp(startPosition, collisionPosition, t), startRotation);
                yield return null;
            }

            EffectFactory.PlayCollisionSpark(collisionPosition, direction, cellSize);
            EffectAudioPlayer.PlayCollision(collisionPosition);
            onImpact?.Invoke();

            elapsed = 0f;
            var recoilPosition = collisionPosition - direction * (cellSize * BlockedCollisionRecoilFactor);
            while (elapsed < BlockedCollisionImpactDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / BlockedCollisionImpactDuration);
                var pulse = Mathf.Sin(t * Mathf.PI);
                var position = Vector3.Lerp(collisionPosition, recoilPosition, pulse);
                var pitch = -BlockedCollisionPitchDegrees * pulse;
                target.SetPositionAndRotation(position, startRotation * Quaternion.Euler(pitch, 0f, 0f));
                yield return null;
            }

            elapsed = 0f;
            var returnStartPosition = target.position;
            var returnStartRotation = target.rotation;
            while (elapsed < BlockedCollisionReturnDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / BlockedCollisionReturnDuration));
                target.SetPositionAndRotation(
                    Vector3.Lerp(returnStartPosition, startPosition, t),
                    Quaternion.Slerp(returnStartRotation, startRotation, t));
                yield return null;
            }

            target.SetPositionAndRotation(startPosition, startRotation);
            onComplete?.Invoke();
        }

        public static IEnumerator PlayHitShake(Transform target, float cellSize, Vector3 worldDirection, Action onComplete)
        {
            var startPosition = target.position;
            var startRotation = target.rotation;
            var direction = NormalizeDirection(worldDirection, target.forward);
            var yawSign = GetImpactYawSign(direction, target.forward);

            var elapsed = 0f;
            while (elapsed < BlockingBusShakeDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / BlockingBusShakeDuration);
                var falloff = 1f - t;
                var hitPulse = Mathf.Sin(t * Mathf.PI * 4f) * falloff;
                var pushPulse = Mathf.Sin(t * Mathf.PI) * falloff;
                var settlePulse = Mathf.Sin(t * Mathf.PI * 2f) * falloff * 0.45f;
                var position = startPosition + direction * (cellSize * BlockingBusShakeDistanceFactor * pushPulse);
                var yaw = BlockingBusShakeYawDegrees * yawSign * (hitPulse + settlePulse);
                target.SetPositionAndRotation(position, startRotation * Quaternion.Euler(0f, yaw, 0f));
                yield return null;
            }

            target.SetPositionAndRotation(startPosition, startRotation);
            onComplete?.Invoke();
        }

        public static IEnumerator PlayBounce(Transform target, Vector3 worldDirection, Action onComplete)
        {
            var startPosition = target.position;
            var direction = NormalizeDirection(worldDirection, target.forward);
            var targetPosition = startPosition + direction * 0.25f;
            var elapsed = 0f;
            const float duration = 0.16f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                target.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            target.position = startPosition;
            onComplete?.Invoke();
        }

        private static Vector3 NormalizeDirection(Vector3 direction, Vector3 fallback)
        {
            var flatDirection = direction.sqrMagnitude > 0.001f ? direction : fallback;
            flatDirection.y = 0f;
            return flatDirection.sqrMagnitude > 0.001f ? flatDirection.normalized : Vector3.forward;
        }

        private static float GetImpactYawSign(Vector3 impactDirection, Vector3 targetForward)
        {
            impactDirection.y = 0f;
            targetForward.y = 0f;
            if (impactDirection.sqrMagnitude < 0.001f || targetForward.sqrMagnitude < 0.001f)
            {
                return 1f;
            }

            var side = Vector3.Dot(Vector3.Cross(Vector3.up, impactDirection.normalized), targetForward.normalized);
            return Mathf.Abs(side) < 0.001f ? 1f : Mathf.Sign(side);
        }
    }
}
