using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class PassengerView : MonoBehaviour
    {
        private const float PassengerVisualScale = 1.20f;
        private const float MoveLift = 0.18f;
        private const float WalkCycleSpeed = 15f;
        private const float LegSwingAngle = 24f;

        private Coroutine moveRoutine;
        private bool canCirculate;
        private Transform[] leftLegs;
        private Transform[] rightLegs;

        public PuzzleColor Color { get; private set; }
        public float RouteProgress { get; private set; }
        public float RouteDistance { get; private set; }
        public float RoutePathLength { get; private set; } = 1f;
        public float CirculationSpeed { get; private set; }
        public float LaneOffset { get; private set; }
        public int RotarySlotIndex { get; private set; } = -1;
        public int FeederSide { get; private set; } = -1;
        public int FeederSlotIndex { get; private set; } = -1;
        public bool IsWaitingInFeeder { get; private set; }
        public bool CanCirculate => canCirculate && moveRoutine == null && gameObject.activeSelf;
        public bool IsAssignedToRotary => !IsWaitingInFeeder && RotarySlotIndex >= 0 && gameObject.activeSelf;

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

            var material = PuzzlePalette.CreateMaterial(color, "Passenger Unit");
            var headMaterial = material;
            var legMaterial = PuzzlePalette.CreateSolidMaterial("Passenger Legs", PuzzlePalette.Darken(PuzzlePalette.ToColor(color), 0.18f));
            var shadowMaterial = PuzzlePalette.CreateSolidMaterial("Passenger Soft Shadow", new Color(0.24f, 0.28f, 0.31f));
            var offsets = new[]
            {
                new Vector3(0f, 0f, -0.20f * PassengerVisualScale),
                new Vector3(0f, 0f, -0.067f * PassengerVisualScale),
                new Vector3(0f, 0f, 0.067f * PassengerVisualScale),
                new Vector3(0f, 0f, 0.20f * PassengerVisualScale)
            };

            leftLegs = new Transform[offsets.Length];
            rightLegs = new Transform[offsets.Length];
            for (var index = 0; index < offsets.Length; index++)
            {
                CreatePerson(index, offsets[index], material, headMaterial, legMaterial, shadowMaterial);
            }
        }

        public void AssignTrafficDistance(float routeDistance, float routePathLength, float circulationSpeed, float laneOffset, int rotarySlotIndex)
        {
            RoutePathLength = Mathf.Max(0.01f, routePathLength);
            RouteDistance = Mathf.Repeat(routeDistance, RoutePathLength);
            RouteProgress = RouteDistance / RoutePathLength;
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
        }

        private void Update()
        {
            if (leftLegs == null || rightLegs == null || (!canCirculate && moveRoutine == null))
            {
                return;
            }

            var speedFactor = Mathf.Max(0.45f, CirculationSpeed * 26f);
            var swing = Mathf.Sin(Time.time * WalkCycleSpeed * speedFactor) * LegSwingAngle;
            for (var index = 0; index < leftLegs.Length; index++)
            {
                var offsetSwing = index % 2 == 0 ? swing : -swing;
                if (leftLegs[index] != null)
                {
                    leftLegs[index].localRotation = Quaternion.Euler(offsetSwing, 0f, 0f);
                }

                if (rightLegs[index] != null)
                {
                    rightLegs[index].localRotation = Quaternion.Euler(-offsetSwing, 0f, 0f);
                }
            }
        }

        public void BeginBoarding()
        {
            canCirculate = false;
            IsWaitingInFeeder = false;
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

        public void MoveToPose(Vector3 targetPosition, Quaternion targetRotation, float duration, Action onComplete = null)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                onComplete?.Invoke();
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MovePoseRoutine(targetPosition, targetRotation, duration, onComplete));
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
                var lastPose = poses[poses.Length - 1];
                transform.SetPositionAndRotation(lastPose.Position, lastPose.Rotation);
                onComplete?.Invoke();
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MovePosePathRoutine(poses, duration, onComplete));
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

        private void CreatePerson(int index, Vector3 rootPosition, Material bodyMaterial, Material headMaterial, Material legMaterial, Material shadowMaterial)
        {
            var personRoot = new GameObject($"Person {index + 1}").transform;
            personRoot.SetParent(transform, false);
            personRoot.localPosition = rootPosition;

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Ground Shadow";
            shadow.transform.SetParent(personRoot, false);
            shadow.transform.localPosition = new Vector3(0f, 0.010f * PassengerVisualScale, 0f);
            shadow.transform.localScale = new Vector3(0.070f, 0.012f, 0.052f) * PassengerVisualScale;
            shadow.GetComponent<Renderer>().sharedMaterial = shadowMaterial;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(personRoot, false);
            body.transform.localPosition = new Vector3(0f, 0.155f * PassengerVisualScale, 0f);
            body.transform.localScale = new Vector3(0.092f, 0.105f, 0.092f) * PassengerVisualScale;
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(personRoot, false);
            head.transform.localPosition = new Vector3(0f, 0.318f * PassengerVisualScale, 0.012f * PassengerVisualScale);
            head.transform.localScale = new Vector3(0.096f, 0.096f, 0.096f) * PassengerVisualScale;
            head.GetComponent<Renderer>().sharedMaterial = headMaterial;

            leftLegs[index] = CreateLeg(personRoot, "Left Leg", new Vector3(-0.026f, 0.055f, 0.016f) * PassengerVisualScale, legMaterial);
            rightLegs[index] = CreateLeg(personRoot, "Right Leg", new Vector3(0.026f, 0.055f, 0.016f) * PassengerVisualScale, legMaterial);
        }

        private static Transform CreateLeg(Transform parent, string name, Vector3 localPosition, Material material)
        {
            var legRoot = new GameObject(name).transform;
            legRoot.SetParent(parent, false);
            legRoot.localPosition = localPosition;

            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg Mesh";
            leg.transform.SetParent(legRoot, false);
            leg.transform.localPosition = new Vector3(0f, -0.035f * PassengerVisualScale, 0f);
            leg.transform.localScale = new Vector3(0.024f, 0.070f, 0.026f) * PassengerVisualScale;
            leg.GetComponent<Renderer>().sharedMaterial = material;

            return legRoot;
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

        private IEnumerator MovePoseRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration, Action onComplete)
        {
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                var position = Vector3.Lerp(startPosition, targetPosition, easedTime);
                position.y += Mathf.Sin(easedTime * Mathf.PI) * MoveLift;
                transform.SetPositionAndRotation(position, Quaternion.Slerp(startRotation, targetRotation, easedTime));
                yield return null;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            moveRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator MovePosePathRoutine(PassengerUnitRoadPose[] poses, float duration, Action onComplete)
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
                var position = Vector3.Lerp(startPose.Position, endPose.Position, segmentTime);
                position.y += Mathf.Sin(normalizedTime * Mathf.PI) * MoveLift * 0.25f;
                transform.SetPositionAndRotation(position, Quaternion.Slerp(startPose.Rotation, endPose.Rotation, segmentTime));
                yield return null;
            }

            var lastPose = poses[poses.Length - 1];
            transform.SetPositionAndRotation(lastPose.Position, lastPose.Rotation);
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
