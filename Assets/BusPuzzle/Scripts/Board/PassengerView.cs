using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public enum PassengerState
    {
        Rotary,
        Feeder,
        MergingToRotary,
        QueuedForBoarding,
        WalkingToBus,
        Boarded
    }

    public sealed class PassengerView : MonoBehaviour
    {
        private const float WalkCycleSpeed = 8.5f;
        private const float MinWalkSpeedFactor = 0.55f;
        private const float MaxWalkSpeedFactor = 1.35f;
        private const float WalkSpeedResponse = 3.2f;
        private const float LegSwingAngle = 24f;

        private Coroutine moveRoutine;
        private PassengerModel model;
        private bool canCirculate;

        public PuzzleColor Color { get; private set; }
        public float RouteProgress { get; private set; }
        public float RouteDistance { get; private set; }
        public float RoutePathLength { get; private set; } = 1f;
        public float CirculationSpeed { get; private set; }
        public int RotarySlotIndex { get; private set; } = -1;
        public int FeederSide { get; private set; } = -1;
        public int FeederSlotIndex { get; private set; } = -1;
        public PassengerState State { get; private set; } = PassengerState.Rotary;
        public bool IsWaitingInFeeder => State == PassengerState.Feeder;
        public bool IsMergingToRotary => State == PassengerState.MergingToRotary;
        public bool IsReservedForBoarding => State == PassengerState.QueuedForBoarding;
        public bool IsMoving => moveRoutine != null;
        public bool CanCirculate => canCirculate && moveRoutine == null && gameObject.activeSelf && (State == PassengerState.Rotary || State == PassengerState.QueuedForBoarding);
        public bool IsAssignedToRotary => (State == PassengerState.Rotary || State == PassengerState.QueuedForBoarding) && RotarySlotIndex >= 0 && gameObject.activeSelf;
        public bool IsRotarySlotReserved => (IsAssignedToRotary || IsMergingToRotary || State == PassengerState.WalkingToBus) && RotarySlotIndex >= 0 && gameObject.activeSelf;
        public bool CanReserveForBoarding => State == PassengerState.Rotary && CanCirculate;

        public static PassengerView Create(PuzzleColor color, Transform parent)
        {
            var passengerObject = new GameObject($"{PuzzlePalette.DisplayName(color)} Passenger Unit");
            passengerObject.transform.SetParent(parent, false);

            var view = passengerObject.AddComponent<PassengerView>();
            view.Initialize(color);
            return view;
        }

        public void Initialize(PuzzleColor color)
        {
            Color = color;
            canCirculate = true;
            State = PassengerState.Rotary;
            model = PassengerModelBuilder.Create(color, transform);
        }

        public void AssignTrafficDistance(float routeDistance, float routePathLength, float circulationSpeed, int rotarySlotIndex)
        {
            RoutePathLength = Mathf.Max(0.01f, routePathLength);
            RouteDistance = Mathf.Repeat(routeDistance, RoutePathLength);
            RouteProgress = RouteDistance / RoutePathLength;
            CirculationSpeed = circulationSpeed;
            RotarySlotIndex = rotarySlotIndex;
            FeederSide = -1;
            FeederSlotIndex = -1;
            State = PassengerState.Rotary;
            canCirculate = true;
        }

        public void AssignFeeder(int side, int slotIndex)
        {
            FeederSide = side;
            FeederSlotIndex = slotIndex;
            RotarySlotIndex = -1;
            CirculationSpeed = 0f;
            State = PassengerState.Feeder;
            canCirculate = false;
        }

        public void AssignMergingToRotary(int side, int feederSlotIndex, int rotarySlotIndex)
        {
            StopMoveRoutine();
            FeederSide = side;
            FeederSlotIndex = feederSlotIndex;
            RotarySlotIndex = rotarySlotIndex;
            RoutePathLength = Mathf.Max(0.01f, RoutePathLength);
            CirculationSpeed = 0f;
            State = PassengerState.MergingToRotary;
            canCirculate = false;
        }

        public bool TryReserveForBoarding()
        {
            if (!CanReserveForBoarding)
            {
                return false;
            }

            State = PassengerState.QueuedForBoarding;
            canCirculate = true;
            return true;
        }

        public void CancelBoardingReservation()
        {
            if (State != PassengerState.QueuedForBoarding)
            {
                return;
            }

            State = PassengerState.Rotary;
            canCirculate = true;
        }

        public void MoveTrafficToward(float targetDistance, float maxDistanceDelta, float routePathLength)
        {
            routePathLength = Mathf.Max(0.01f, routePathLength);
            var currentDistance = Mathf.Repeat(RouteDistance, routePathLength);
            targetDistance = Mathf.Repeat(targetDistance, routePathLength);
            var forwardDelta = Mathf.Repeat(targetDistance - currentDistance, routePathLength);

            if (forwardDelta > routePathLength * 0.85f)
            {
                return;
            }

            SetTrafficDistance(currentDistance + Mathf.Min(forwardDelta, Mathf.Max(0f, maxDistanceDelta)), routePathLength);
        }

        public void SetTrafficDistance(float routeDistance, float routePathLength)
        {
            RoutePathLength = Mathf.Max(0.01f, routePathLength);
            RouteDistance = Mathf.Repeat(routeDistance, RoutePathLength);
            RouteProgress = RouteDistance / RoutePathLength;
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            model.ApplyDefaultPersonLocalPositions();
        }

        internal void SetPose(PassengerUnitRoadPose pose)
        {
            PassengerPoseAnimator.ApplyPose(transform, model, pose);
        }

        public void BeginBoarding()
        {
            canCirculate = false;
            State = PassengerState.WalkingToBus;
        }

        public void MarkBoarded()
        {
            canCirculate = false;
            State = PassengerState.Boarded;
        }

        public void MoveTo(Vector3 targetPosition, float duration, Action onComplete = null)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                transform.position = targetPosition;
                onComplete?.Invoke();
                return;
            }

            StartMoveRoutine(PassengerPoseAnimator.MoveTo(transform, targetPosition, duration, CreateMoveComplete(onComplete)));
        }

        public void MoveToPose(Vector3 targetPosition, Quaternion targetRotation, float duration, Action onComplete = null)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                model.ApplyDefaultPersonLocalPositions();
                onComplete?.Invoke();
                return;
            }

            StartMoveRoutine(PassengerPoseAnimator.MoveToPose(transform, model, targetPosition, targetRotation, duration, CreateMoveComplete(onComplete)));
        }

        internal void MoveAlongPoses(PassengerUnitRoadPose[] poses, float duration, Action onComplete = null)
        {
            if (poses == null || poses.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (!isActiveAndEnabled || duration <= 0f || poses.Length == 1)
            {
                SetPose(poses[poses.Length - 1]);
                onComplete?.Invoke();
                return;
            }

            StartMoveRoutine(PassengerPoseAnimator.MoveAlongPoses(transform, model, poses, duration, CreateMoveComplete(onComplete)));
        }

        internal void MoveAlongDynamicPose(Func<float, PassengerUnitRoadPose> getPose, float duration, Action onComplete = null)
        {
            if (getPose == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (!isActiveAndEnabled || duration <= 0f)
            {
                SetPose(getPose(1f));
                onComplete?.Invoke();
                return;
            }

            StartMoveRoutine(PassengerPoseAnimator.MoveAlongDynamicPose(transform, model, getPose, duration, CreateMoveComplete(onComplete)));
        }

        public void WalkToBoard(
            Vector3 approachPosition,
            Vector3 doorPosition,
            Vector3 entryPosition,
            float walkDuration,
            float personEnterDuration,
            float personEnterInterval,
            Action<Vector3> onPersonEntered = null,
            Action onComplete = null)
        {
            if (!isActiveAndEnabled)
            {
                transform.position = entryPosition;
                transform.localScale = Vector3.zero;
                onComplete?.Invoke();
                return;
            }

            StartMoveRoutine(PassengerBoardingAnimator.WalkToBoard(
                transform,
                model,
                approachPosition,
                doorPosition,
                entryPosition,
                walkDuration,
                personEnterDuration,
                personEnterInterval,
                onPersonEntered,
                CreateMoveComplete(onComplete)));
        }

        private void Update()
        {
            if (model == null || (!canCirculate && moveRoutine == null))
            {
                return;
            }

            var speedFactor = Mathf.Clamp(CirculationSpeed * WalkSpeedResponse, MinWalkSpeedFactor, MaxWalkSpeedFactor);
            var swing = Mathf.Sin(Time.time * WalkCycleSpeed * speedFactor) * LegSwingAngle;
            model.ApplyWalkCycle(swing);
        }

        private void StartMoveRoutine(IEnumerator routine)
        {
            StopMoveRoutine();
            moveRoutine = StartCoroutine(routine);
        }

        private void StopMoveRoutine()
        {
            if (moveRoutine == null)
            {
                return;
            }

            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        private Action CreateMoveComplete(Action onComplete)
        {
            return () =>
            {
                moveRoutine = null;
                onComplete?.Invoke();
            };
        }
    }
}
