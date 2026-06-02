using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class PassengerView : MonoBehaviour
    {
        private const float MoveLift = 0.18f;

        private Coroutine moveRoutine;
        private Vector3 baseScale;
        private bool canCirculate;

        public PuzzleColor Color { get; private set; }
        public float RouteProgress { get; private set; }
        public float CirculationSpeed { get; private set; }
        public float LaneOffset { get; private set; }
        public int RotarySlotIndex { get; private set; } = -1;
        public int FeederSide { get; private set; } = -1;
        public int FeederSlotIndex { get; private set; } = -1;
        public bool IsWaitingInFeeder { get; private set; }
        public bool CanCirculate => canCirculate && moveRoutine == null && gameObject.activeSelf;

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
            baseScale = Vector3.one;
            canCirculate = true;

            var material = PuzzlePalette.CreateMaterial(color, "Passenger Unit");
            var offsets = new[]
            {
                new Vector3(0f, 0.20f, -0.24f),
                new Vector3(0f, 0.20f, -0.08f),
                new Vector3(0f, 0.20f, 0.08f),
                new Vector3(0f, 0.20f, 0.24f)
            };

            for (var index = 0; index < offsets.Length; index++)
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = $"Person {index + 1}";
                capsule.transform.SetParent(transform, false);
                capsule.transform.localPosition = offsets[index];
                capsule.transform.localScale = new Vector3(0.13f, 0.23f, 0.13f);
                capsule.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        public void AssignTraffic(float routeProgress, float circulationSpeed, float laneOffset, int rotarySlotIndex)
        {
            RouteProgress = Mathf.Repeat(routeProgress, 1f);
            CirculationSpeed = circulationSpeed;
            LaneOffset = laneOffset;
            RotarySlotIndex = rotarySlotIndex;
            FeederSide = -1;
            FeederSlotIndex = -1;
            IsWaitingInFeeder = false;
            canCirculate = true;
        }

        public void AssignFeeder(int side, int slotIndex)
        {
            FeederSide = side;
            FeederSlotIndex = slotIndex;
            RotarySlotIndex = -1;
            CirculationSpeed = 0f;
            LaneOffset = 0f;
            IsWaitingInFeeder = true;
            canCirculate = false;
        }

        public void AdvanceTraffic(float deltaTime)
        {
            RouteProgress = Mathf.Repeat(RouteProgress + CirculationSpeed * deltaTime, 1f);
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void BeginBoarding()
        {
            canCirculate = false;
            IsWaitingInFeeder = false;
        }

        public void SetPosition(Vector3 position)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }

            transform.position = position;
        }

        public void MoveTo(Vector3 targetPosition, float duration, Action onComplete = null)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                transform.position = targetPosition;
                onComplete?.Invoke();
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveRoutine(targetPosition, duration, onComplete));
        }

        public void AbsorbTo(Vector3 targetPosition, float duration, Action onComplete = null)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                transform.position = targetPosition;
                transform.localScale = Vector3.zero;
                onComplete?.Invoke();
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(AbsorbRoutine(targetPosition, duration, onComplete));
        }

        public void SetEmphasis(bool isEmphasized)
        {
            transform.localScale = isEmphasized ? baseScale * 1.14f : baseScale;
        }

        private IEnumerator MoveRoutine(Vector3 targetPosition, float duration, Action onComplete)
        {
            var startPosition = transform.position;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                var position = Vector3.Lerp(startPosition, targetPosition, easedTime);
                position.y += Mathf.Sin(easedTime * Mathf.PI) * MoveLift;
                transform.position = position;
                yield return null;
            }

            transform.position = targetPosition;
            moveRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator AbsorbRoutine(Vector3 targetPosition, float duration, Action onComplete)
        {
            var startPosition = transform.position;
            var startScale = transform.localScale;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                var position = Vector3.Lerp(startPosition, targetPosition, easedTime);
                position.y += Mathf.Sin(easedTime * Mathf.PI) * MoveLift * 0.45f;
                transform.position = position;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easedTime);
                yield return null;
            }

            transform.position = targetPosition;
            transform.localScale = Vector3.zero;
            moveRoutine = null;
            onComplete?.Invoke();
        }
    }
}
