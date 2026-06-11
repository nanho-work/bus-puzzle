using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    internal static class PassengerBoardingAnimator
    {
        private const float BoardingPersonLift = 0.030f;
        private const float BoardingDoorProgress = 0.42f;
        private const float BoardingEffectProgress = 0.82f;

        public static IEnumerator WalkToBoard(
            Transform target,
            PassengerModel model,
            Vector3 approachPosition,
            Vector3 doorPosition,
            Vector3 entryPosition,
            float walkDuration,
            float personEnterDuration,
            float personEnterInterval,
            PassengerUnitRoadPose? boardingGatePose,
            Action<Vector3> onPersonEntered,
            Action onComplete)
        {
            var startPosition = target.position;
            var startRotation = target.rotation;
            var rawGatePosition = boardingGatePose.HasValue ? boardingGatePose.Value.Position : startPosition;
            var rawExitPosition = GetBoardingExitPosition(rawGatePosition, approachPosition);
            var adjustedGatePose = boardingGatePose.HasValue
                ? boardingGatePose.Value.WithForwardDirection(rawExitPosition - rawGatePosition)
                : (PassengerUnitRoadPose?)null;
            var gatePosition = adjustedGatePose.HasValue ? adjustedGatePose.Value.Position : startPosition;
            var gateRotation = adjustedGatePose.HasValue ? adjustedGatePose.Value.Rotation : startRotation;
            var exitPosition = GetBoardingExitPosition(gatePosition, approachPosition);
            var exitRotation = GetFlatLookRotation(exitPosition - gatePosition, gateRotation);
            var approachRotation = GetFlatLookRotation(approachPosition - exitPosition, exitRotation);
            var enterRotation = GetFlatLookRotation(doorPosition - approachPosition, approachRotation);
            var boardingOrder = BuildBoardingOrder(model);
            var elapsed = 0f;
            walkDuration = Mathf.Max(0.01f, walkDuration);
            var gateDistance = FlatDistance(startPosition, gatePosition);
            var exitDistance = FlatDistance(gatePosition, exitPosition);
            var approachDistance = FlatDistance(exitPosition, approachPosition);
            var totalDistance = Mathf.Max(0.0001f, gateDistance + exitDistance + approachDistance);
            var gateProgress = gateDistance > 0.010f ? Mathf.Clamp(gateDistance / totalDistance, 0.08f, 0.22f) : 0f;
            var exitProgress = Mathf.Clamp(gateProgress + exitDistance / totalDistance, gateProgress + 0.48f, 0.86f);

            if (adjustedGatePose.HasValue)
            {
                model.ApplyPosePersonLocalPositions(adjustedGatePose.Value);
            }

            while (elapsed < walkDuration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / walkDuration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

                if (gateProgress > 0f && easedTime <= gateProgress)
                {
                    var gateTime = Mathf.Clamp01(easedTime / Mathf.Max(0.0001f, gateProgress));
                    target.SetPositionAndRotation(
                        Vector3.Lerp(startPosition, gatePosition, gateTime),
                        Quaternion.Slerp(startRotation, gateRotation, gateTime));
                }
                else if (easedTime <= exitProgress)
                {
                    var exitTime = Mathf.Clamp01((easedTime - gateProgress) / Mathf.Max(0.0001f, exitProgress - gateProgress));
                    target.SetPositionAndRotation(
                        Vector3.Lerp(gatePosition, exitPosition, exitTime),
                        Quaternion.Slerp(gateRotation, exitRotation, exitTime));
                }
                else
                {
                    var approachTime = Mathf.Clamp01((easedTime - exitProgress) / Mathf.Max(0.0001f, 1f - exitProgress));
                    target.SetPositionAndRotation(
                        Vector3.Lerp(exitPosition, approachPosition, approachTime),
                        Quaternion.Slerp(exitRotation, enterRotation, approachTime));
                }

                yield return null;
            }

            target.SetPositionAndRotation(approachPosition, enterRotation);

            yield return MovePeopleIntoBus(
                target,
                model,
                boardingOrder,
                doorPosition,
                entryPosition,
                personEnterDuration,
                personEnterInterval,
                onPersonEntered);

            target.position = entryPosition;
            target.localScale = Vector3.zero;
            onComplete?.Invoke();
        }

        private static IEnumerator MovePeopleIntoBus(
            Transform target,
            PassengerModel model,
            int[] boardingOrder,
            Vector3 doorPosition,
            Vector3 entryPosition,
            float duration,
            float interval,
            Action<Vector3> onPersonEntered)
        {
            if (boardingOrder == null || boardingOrder.Length == 0)
            {
                yield break;
            }

            duration = Mathf.Max(0.01f, duration);
            interval = Mathf.Max(0f, interval);
            var doorLocalPosition = target.InverseTransformPoint(doorPosition);
            var entryLocalPosition = target.InverseTransformPoint(entryPosition);

            for (var orderIndex = 0; orderIndex < boardingOrder.Length; orderIndex++)
            {
                var personRoot = model.GetPersonRoot(boardingOrder[orderIndex]);
                if (personRoot == null)
                {
                    continue;
                }

                yield return MoveOnePersonIntoBus(
                    personRoot,
                    personRoot.localPosition,
                    personRoot.localScale,
                    doorLocalPosition,
                    entryLocalPosition,
                    duration,
                    onPersonEntered);

                if (interval > 0f && orderIndex < boardingOrder.Length - 1)
                {
                    yield return new WaitForSeconds(interval);
                }
            }

            for (var orderIndex = 0; orderIndex < boardingOrder.Length; orderIndex++)
            {
                var personRoot = model.GetPersonRoot(boardingOrder[orderIndex]);
                if (personRoot == null)
                {
                    continue;
                }

                personRoot.localPosition = entryLocalPosition;
                personRoot.localScale = Vector3.zero;
                personRoot.gameObject.SetActive(false);
            }
        }

        private static IEnumerator MoveOnePersonIntoBus(
            Transform personRoot,
            Vector3 startLocalPosition,
            Vector3 startScale,
            Vector3 doorLocalPosition,
            Vector3 entryLocalPosition,
            float duration,
            Action<Vector3> onPersonEntered)
        {
            var elapsed = 0f;
            var entered = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var personTime = Mathf.Clamp01(elapsed / duration);
                var localPosition = GetBoardingPersonLocalPosition(startLocalPosition, doorLocalPosition, entryLocalPosition, personTime);
                localPosition.y += Mathf.Sin(personTime * Mathf.PI) * BoardingPersonLift;
                personRoot.localPosition = localPosition;
                personRoot.localScale = GetBoardingPersonScale(startScale, personTime);

                if (personTime >= BoardingEffectProgress && !entered)
                {
                    entered = true;
                    onPersonEntered?.Invoke(personRoot.position);
                }

                yield return null;
            }

            personRoot.localPosition = entryLocalPosition;
            personRoot.localScale = Vector3.zero;
            personRoot.gameObject.SetActive(false);
        }

        private static int[] BuildBoardingOrder(PassengerModel model)
        {
            var count = model.PersonCount;
            var order = new int[count];
            for (var index = 0; index < count; index++)
            {
                order[index] = count - 1 - index;
            }

            return order;
        }

        private static Vector3 GetBoardingExitPosition(Vector3 startPosition, Vector3 approachPosition)
        {
            return new Vector3(startPosition.x, approachPosition.y, approachPosition.z);
        }

        private static float FlatDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private static Vector3 GetBoardingPersonLocalPosition(Vector3 startPosition, Vector3 doorPosition, Vector3 entryPosition, float personTime)
        {
            if (personTime <= BoardingDoorProgress)
            {
                var doorTime = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(personTime / BoardingDoorProgress));
                return Vector3.Lerp(startPosition, doorPosition, doorTime);
            }

            var entryTime = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BoardingDoorProgress, 1f, personTime));
            return Vector3.Lerp(doorPosition, entryPosition, entryTime);
        }

        private static Vector3 GetBoardingPersonScale(Vector3 startScale, float personTime)
        {
            var shrinkTime = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BoardingDoorProgress, 1f, personTime));
            return Vector3.Lerp(startScale, Vector3.zero, shrinkTime);
        }

        private static Quaternion GetFlatLookRotation(Vector3 direction, Quaternion fallback)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : fallback;
        }
    }
}
