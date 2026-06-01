using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BusView : MonoBehaviour
    {
        private readonly List<Transform> seatMarkers = new List<Transform>();

        private Coroutine feedbackRoutine;
        private Vector3 baseScale = Vector3.one;
        private int occupiedSeats;

        public PuzzleColor Color { get; private set; }
        public int Capacity { get; private set; }
        public bool IsDeparted { get; private set; }
        public bool IsFull => occupiedSeats >= Capacity;

        public static BusView Create(BusDefinition definition, Transform parent)
        {
            var busObject = new GameObject($"{PuzzlePalette.DisplayName(definition.Color)} Bus");
            busObject.transform.SetParent(parent, false);

            var view = busObject.AddComponent<BusView>();
            view.Initialize(definition);
            return view;
        }

        public void Initialize(BusDefinition definition)
        {
            Color = definition.Color;
            Capacity = definition.Capacity;
            occupiedSeats = 0;
            IsDeparted = false;
            baseScale = transform.localScale;

            CreateBody();
            CreateWheels();
            CreateSeatMarkers();
        }

        public bool CanBoard(PassengerView passenger)
        {
            return passenger != null && !IsDeparted && !IsFull && passenger.Color == Color;
        }

        public void BoardPassenger(PassengerView passenger, Action onComplete)
        {
            if (!CanBoard(passenger))
            {
                onComplete?.Invoke();
                return;
            }

            var seatIndex = occupiedSeats;
            occupiedSeats++;

            var seatPosition = seatMarkers[seatIndex].position + Vector3.up * 0.22f;
            passenger.transform.SetParent(transform, true);
            passenger.MoveTo(seatPosition, 0.35f, () =>
            {
                passenger.transform.position = seatPosition;
                passenger.transform.SetParent(transform, true);

                if (IsFull)
                {
                    StartCoroutine(DepartRoutine(onComplete));
                    return;
                }

                onComplete?.Invoke();
            });
        }

        public void ShowInvalidFeedback()
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }

            feedbackRoutine = StartCoroutine(InvalidFeedbackRoutine());
        }

        private void CreateBody()
        {
            var color = PuzzlePalette.ToColor(Color);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = new Vector3(2.4f, 0.72f, 1.15f);
            body.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial($"{PuzzlePalette.DisplayName(Color)} Bus Body", color);

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(transform, false);
            cabin.transform.localPosition = new Vector3(0.55f, 0.98f, 0f);
            cabin.transform.localScale = new Vector3(0.88f, 0.42f, 0.95f);
            cabin.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial($"{PuzzlePalette.DisplayName(Color)} Bus Cabin", PuzzlePalette.Darken(color, 0.12f));
        }

        private void CreateWheels()
        {
            var wheelMaterial = PuzzlePalette.CreateSolidMaterial("Wheel", new Color(0.08f, 0.09f, 0.11f));
            var wheelPositions = new[]
            {
                new Vector3(-0.75f, 0.13f, -0.62f),
                new Vector3(0.75f, 0.13f, -0.62f),
                new Vector3(-0.75f, 0.13f, 0.62f),
                new Vector3(0.75f, 0.13f, 0.62f)
            };

            foreach (var localPosition in wheelPositions)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(transform, false);
                wheel.transform.localPosition = localPosition;
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.24f, 0.16f, 0.24f);
                wheel.GetComponent<Renderer>().sharedMaterial = wheelMaterial;
            }
        }

        private void CreateSeatMarkers()
        {
            var seatMaterial = PuzzlePalette.CreateSolidMaterial("Seat Marker", new Color(0.96f, 0.96f, 0.90f));

            for (var index = 0; index < Capacity; index++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Seat {index + 1}";
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = GetSeatLocalPosition(index);
                marker.transform.localScale = Vector3.one * 0.18f;
                marker.GetComponent<Renderer>().sharedMaterial = seatMaterial;

                var markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null)
                {
                    Destroy(markerCollider);
                }

                seatMarkers.Add(marker.transform);
            }
        }

        private Vector3 GetSeatLocalPosition(int seatIndex)
        {
            var t = Capacity == 1 ? 0.5f : seatIndex / (Capacity - 1f);
            return new Vector3(Mathf.Lerp(-0.82f, 0.82f, t), 1.08f, 0f);
        }

        private IEnumerator DepartRoutine(Action onComplete)
        {
            IsDeparted = true;
            yield return new WaitForSeconds(0.15f);

            var startPosition = transform.position;
            var targetPosition = startPosition + new Vector3(8f, 0f, 0f);
            var duration = 0.75f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private IEnumerator InvalidFeedbackRoutine()
        {
            var duration = 0.18f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                transform.localScale = baseScale * (1f + t * 0.08f);
                yield return null;
            }

            transform.localScale = baseScale;
            feedbackRoutine = null;
        }
    }
}
