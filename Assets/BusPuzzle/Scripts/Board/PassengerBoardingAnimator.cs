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
        private const float BoardingLineupStartProgress = 0.14f;
        private const float BoardingLineupEndProgress = 0.88f;
        private const float BoardingLineSpacingMin = 0.105f;

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
            var enterRotation = GetFlatLookRotation(doorPosition - approachPosition, approachRotation);
            var boardingOrder = BuildBoardingOrder(model);
            var linePositions = BuildBoardingLineLocalPositions(model, boardingOrder);
            var elapsed = 0f;
            walkDuration = Mathf.Max(0.01f, walkDuration);

            while (elapsed < walkDuration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / walkDuration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                var lineupTime = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(BoardingLineupStartProgress, BoardingLineupEndProgress, normalizedTime));

                target.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, approachPosition, easedTime),
                    Quaternion.Slerp(startRotation, enterRotation, easedTime));
                ApplyBoardingLineLocalPositions(model, linePositions, lineupTime);
                yield return null;
            }

            target.SetPositionAndRotation(approachPosition, enterRotation);
            ApplyBoardingLineLocalPositions(model, linePositions, 1f);

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

        private static Vector3[] BuildBoardingLineLocalPositions(PassengerModel model, int[] boardingOrder)
        {
            var count = model.PersonCount;
            var linePositions = new Vector3[count];
            var spacing = EstimateDefaultPersonSpacing(model);
            var centerIndex = (count - 1) * 0.5f;

            for (var orderIndex = 0; orderIndex < count; orderIndex++)
            {
                var personIndex = boardingOrder != null && orderIndex < boardingOrder.Length
                    ? boardingOrder[orderIndex]
                    : orderIndex;
                if (personIndex < 0 || personIndex >= count)
                {
                    continue;
                }

                var defaultPosition = model.GetDefaultPersonLocalPosition(personIndex);
                linePositions[personIndex] = new Vector3(
                    0f,
                    defaultPosition.y,
                    (centerIndex - orderIndex) * spacing);
            }

            return linePositions;
        }

        private static float EstimateDefaultPersonSpacing(PassengerModel model)
        {
            if (model.PersonCount < 2)
            {
                return BoardingLineSpacingMin;
            }

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            for (var index = 0; index < model.PersonCount; index++)
            {
                var localPosition = model.GetDefaultPersonLocalPosition(index);
                minX = Mathf.Min(minX, localPosition.x);
                maxX = Mathf.Max(maxX, localPosition.x);
            }

            return Mathf.Max(BoardingLineSpacingMin, (maxX - minX) / Mathf.Max(1, model.PersonCount - 1));
        }

        private static void ApplyBoardingLineLocalPositions(PassengerModel model, Vector3[] linePositions, float t)
        {
            for (var index = 0; index < model.PersonCount; index++)
            {
                var personRoot = model.GetPersonRoot(index);
                if (personRoot == null)
                {
                    continue;
                }

                var defaultPosition = model.GetDefaultPersonLocalPosition(index);
                var linePosition = linePositions != null && index < linePositions.Length
                    ? linePositions[index]
                    : defaultPosition;
                personRoot.localPosition = Vector3.Lerp(defaultPosition, linePosition, t);
            }
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
