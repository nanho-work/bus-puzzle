using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    internal static class PassengerBoardingAnimator
    {
        private const float BoardingPersonLift = 0.030f;
        private const float BoardingLaneSpacing = 0.026f;
        private const float BoardingEntryLaneScale = 0.35f;
        private const float BoardingStageSpacing = 0.055f;
        private const float BoardingStageLaneScale = 0.55f;
        private const float BoardingFollowDelayScale = 0.26f;
        private const float BoardingStageProgress = 0.34f;
        private const float BoardingDoorProgress = 0.72f;
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
            var boardingOrder = BuildBoardingOrder(model, doorPosition);
            var directBoardingDuration = Mathf.Max(0.01f, walkDuration + personEnterDuration);
            yield return MovePeopleIntoBus(
                target,
                model,
                boardingOrder,
                approachPosition,
                doorPosition,
                entryPosition,
                directBoardingDuration,
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
            Vector3 approachPosition,
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
            var followDelay = Mathf.Max(interval, duration * BoardingFollowDelayScale);
            var entryDirection = GetFlatDirection(entryPosition - doorPosition, target.forward);
            var stagingDirection = GetFlatDirection(approachPosition - doorPosition, -entryDirection);
            var boardingRight = Vector3.Cross(Vector3.up, entryDirection);
            boardingRight = GetFlatDirection(boardingRight, target.right);
            var motions = new BoardingPersonMotion[boardingOrder.Length];

            for (var orderIndex = 0; orderIndex < boardingOrder.Length; orderIndex++)
            {
                var personRoot = model.GetPersonRoot(boardingOrder[orderIndex]);
                if (personRoot == null)
                {
                    continue;
                }

                var laneOffset = GetBoardingLaneOffset(orderIndex, boardingOrder.Length);
                var stagePosition = approachPosition +
                    stagingDirection * (orderIndex * BoardingStageSpacing) +
                    boardingRight * laneOffset * BoardingStageLaneScale;

                motions[orderIndex] = new BoardingPersonMotion(
                    personRoot,
                    personRoot.position,
                    personRoot.rotation,
                    personRoot.localScale,
                    stagePosition,
                    doorPosition + boardingRight * laneOffset,
                    entryPosition + boardingRight * laneOffset * BoardingEntryLaneScale,
                    orderIndex * followDelay);
            }

            var totalDuration = duration + followDelay * Mathf.Max(0, boardingOrder.Length - 1);
            var elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                ApplyBoardingMotions(motions, duration, elapsed, onPersonEntered);
                yield return null;
            }

            ApplyBoardingMotions(motions, duration, totalDuration, onPersonEntered);
            for (var index = 0; index < motions.Length; index++)
            {
                var motion = motions[index];
                if (motion == null || motion.PersonRoot == null)
                {
                    continue;
                }

                motion.PersonRoot.position = entryPosition;
                motion.PersonRoot.localScale = Vector3.zero;
                motion.PersonRoot.gameObject.SetActive(false);
            }
        }

        private static void ApplyBoardingMotions(
            BoardingPersonMotion[] motions,
            float duration,
            float elapsed,
            Action<Vector3> onPersonEntered)
        {
            for (var index = 0; index < motions.Length; index++)
            {
                var motion = motions[index];
                if (motion == null || motion.PersonRoot == null || motion.Finished)
                {
                    continue;
                }

                var localElapsed = elapsed - motion.Delay;
                if (localElapsed < 0f)
                {
                    continue;
                }

                var personTime = Mathf.Clamp01(localElapsed / duration);
                var position = GetBoardingPersonPosition(
                    motion.StartPosition,
                    motion.StagePosition,
                    motion.DoorPosition,
                    motion.EntryPosition,
                    personTime);
                var lift = Mathf.Sin(personTime * Mathf.PI) * BoardingPersonLift;
                position.y += lift;
                var lookTarget = GetBoardingLookTarget(motion, personTime);
                motion.PersonRoot.SetPositionAndRotation(position, GetFlatLookRotation(lookTarget - position, motion.StartRotation));
                motion.PersonRoot.localScale = GetBoardingPersonScale(motion.StartScale, personTime);

                if (personTime >= BoardingEffectProgress && !motion.Entered)
                {
                    motion.Entered = true;
                    onPersonEntered?.Invoke(motion.PersonRoot.position);
                }

                if (personTime < 1f)
                {
                    continue;
                }

                motion.Finished = true;
                motion.PersonRoot.position = motion.EntryPosition;
                motion.PersonRoot.localScale = Vector3.zero;
                motion.PersonRoot.gameObject.SetActive(false);
            }
        }

        private static int[] BuildBoardingOrder(PassengerModel model, Vector3 doorPosition)
        {
            var count = model.PersonCount;
            var order = new int[count];
            for (var index = 0; index < count; index++)
            {
                order[index] = index;
            }

            Array.Sort(order, (first, second) =>
                model.GetPersonDistanceToEntry(first, doorPosition)
                    .CompareTo(model.GetPersonDistanceToEntry(second, doorPosition)));
            return order;
        }

        private static float GetBoardingLaneOffset(int orderIndex, int count)
        {
            return (orderIndex - (count - 1) * 0.5f) * BoardingLaneSpacing;
        }

        private static Vector3 GetBoardingPersonPosition(
            Vector3 startPosition,
            Vector3 stagePosition,
            Vector3 doorPosition,
            Vector3 entryPosition,
            float personTime)
        {
            if (personTime <= BoardingStageProgress)
            {
                var stageTime = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(personTime / BoardingStageProgress));
                return Vector3.Lerp(startPosition, stagePosition, stageTime);
            }

            if (personTime <= BoardingDoorProgress)
            {
                var doorTime = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(BoardingStageProgress, BoardingDoorProgress, personTime));
                return Vector3.Lerp(stagePosition, doorPosition, doorTime);
            }

            var entryTime = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BoardingDoorProgress, 1f, personTime));
            return Vector3.Lerp(doorPosition, entryPosition, entryTime);
        }

        private static Vector3 GetBoardingLookTarget(BoardingPersonMotion motion, float personTime)
        {
            if (personTime <= BoardingStageProgress)
            {
                return motion.StagePosition;
            }

            return personTime <= BoardingDoorProgress ? motion.DoorPosition : motion.EntryPosition;
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

        private static Vector3 GetFlatDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        private sealed class BoardingPersonMotion
        {
            public readonly Transform PersonRoot;
            public readonly Vector3 StartPosition;
            public readonly Quaternion StartRotation;
            public readonly Vector3 StartScale;
            public readonly Vector3 StagePosition;
            public readonly Vector3 DoorPosition;
            public readonly Vector3 EntryPosition;
            public readonly float Delay;
            public bool Entered;
            public bool Finished;

            public BoardingPersonMotion(
                Transform personRoot,
                Vector3 startPosition,
                Quaternion startRotation,
                Vector3 startScale,
                Vector3 stagePosition,
                Vector3 doorPosition,
                Vector3 entryPosition,
                float delay)
            {
                PersonRoot = personRoot;
                StartPosition = startPosition;
                StartRotation = startRotation;
                StartScale = startScale;
                StagePosition = stagePosition;
                DoorPosition = doorPosition;
                EntryPosition = entryPosition;
                Delay = delay;
            }
        }
    }
}
