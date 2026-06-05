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
            Action<Vector3> onPersonEntered,
            Action onComplete)
        {
            model.ApplyDefaultPersonLocalPositions();

            var startPosition = target.position;
            var startRotation = target.rotation;
            var approachRotation = GetFlatLookRotation(approachPosition - startPosition, startRotation);
            var elapsed = 0f;
            walkDuration = Mathf.Max(0.01f, walkDuration);

            while (elapsed < walkDuration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / walkDuration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                target.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, approachPosition, easedTime),
                    Quaternion.Slerp(startRotation, approachRotation, easedTime));
                yield return null;
            }

            var enterRotation = GetFlatLookRotation(doorPosition - approachPosition, approachRotation);
            target.SetPositionAndRotation(approachPosition, enterRotation);
            model.ApplyDefaultPersonLocalPositions();

            yield return MovePeopleIntoBus(
                target,
                model,
                BuildBoardingOrder(model, doorPosition),
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
            var startLocalPositions = new Vector3[boardingOrder.Length];
            var startScales = new Vector3[boardingOrder.Length];
            var entered = new bool[boardingOrder.Length];

            for (var orderIndex = 0; orderIndex < boardingOrder.Length; orderIndex++)
            {
                var personRoot = model.GetPersonRoot(boardingOrder[orderIndex]);
                if (personRoot == null)
                {
                    continue;
                }

                startLocalPositions[orderIndex] = personRoot.localPosition;
                startScales[orderIndex] = personRoot.localScale;
            }

            var elapsed = 0f;
            var totalDuration = duration + interval * Mathf.Max(0, boardingOrder.Length - 1);

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                for (var orderIndex = 0; orderIndex < boardingOrder.Length; orderIndex++)
                {
                    var personRoot = model.GetPersonRoot(boardingOrder[orderIndex]);
                    if (personRoot == null)
                    {
                        continue;
                    }

                    var personTime = Mathf.Clamp01((elapsed - interval * orderIndex) / duration);
                    if (personTime <= 0f)
                    {
                        continue;
                    }

                    var localPosition = GetBoardingPersonLocalPosition(startLocalPositions[orderIndex], doorLocalPosition, entryLocalPosition, personTime);
                    localPosition.y += Mathf.Sin(personTime * Mathf.PI) * BoardingPersonLift;
                    personRoot.localPosition = localPosition;
                    personRoot.localScale = GetBoardingPersonScale(startScales[orderIndex], personTime);

                    if (personTime >= BoardingEffectProgress && !entered[orderIndex])
                    {
                        entered[orderIndex] = true;
                        onPersonEntered?.Invoke(personRoot.position);
                    }

                    if (personTime >= 1f && personRoot.gameObject.activeSelf)
                    {
                        personRoot.gameObject.SetActive(false);
                    }
                }

                yield return null;
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

        private static int[] BuildBoardingOrder(PassengerModel model, Vector3 entryPosition)
        {
            var count = model.PersonCount;
            var order = new int[count];
            for (var index = 0; index < count; index++)
            {
                order[index] = index;
            }

            for (var index = 0; index < count - 1; index++)
            {
                var bestIndex = index;
                var bestDistance = model.GetPersonDistanceToEntry(order[index], entryPosition);
                for (var candidate = index + 1; candidate < count; candidate++)
                {
                    var candidateDistance = model.GetPersonDistanceToEntry(order[candidate], entryPosition);
                    if (candidateDistance >= bestDistance)
                    {
                        continue;
                    }

                    bestIndex = candidate;
                    bestDistance = candidateDistance;
                }

                if (bestIndex == index)
                {
                    continue;
                }

                var swap = order[index];
                order[index] = order[bestIndex];
                order[bestIndex] = swap;
            }

            return order;
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
