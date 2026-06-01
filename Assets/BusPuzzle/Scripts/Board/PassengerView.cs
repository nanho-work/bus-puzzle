using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class PassengerView : MonoBehaviour
    {
        private const float MoveLift = 0.25f;

        private Coroutine moveRoutine;
        private Vector3 baseScale;

        public PuzzleColor Color { get; private set; }

        public static PassengerView Create(PuzzleColor color, Transform parent)
        {
            var passengerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            passengerObject.name = $"{PuzzlePalette.DisplayName(color)} Passenger";
            passengerObject.transform.SetParent(parent, false);
            passengerObject.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);

            var view = passengerObject.AddComponent<PassengerView>();
            view.Initialize(color);
            return view;
        }

        public void Initialize(PuzzleColor color)
        {
            Color = color;
            baseScale = transform.localScale;

            var meshRenderer = GetComponent<Renderer>();
            meshRenderer.sharedMaterial = PuzzlePalette.CreateMaterial(color, "Passenger");
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

        public void SetEmphasis(bool isEmphasized)
        {
            transform.localScale = isEmphasized ? baseScale * 1.12f : baseScale;
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
    }
}
