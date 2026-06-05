using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BusView : MonoBehaviour
    {
        private const float DepartureStationClearProgress = 0.20f;

        private Coroutine motionRoutine;
        private Coroutine hitShakeRoutine;
        private Coroutine vipHighlightRoutine;
        private GameObject directionArrow;
        private GameObject vipHighlight;
        private VehicleBoardingCounter boardingCounter;
        private Vector3 hitShakeStartPosition;
        private Quaternion hitShakeStartRotation;
        private float cellSize = 1.2f;
        private int boardedUnits;
        private int reservedUnits;
        private int boardingUnitsInProgress;
        private bool usingModelVisual;

        public PuzzleColor Color { get; private set; }
        public BusSize Size { get; private set; }
        public GridDirection Direction { get; private set; }
        public float AngleOffsetDegrees { get; private set; }
        public float YawDegrees => GridDirectionUtility.ToYawDegrees(Direction) + AngleOffsetDegrees;
        public Vector2Int GridPosition { get; private set; }
        public int StationSlotIndex { get; private set; } = -1;
        public bool IsOnBoard { get; private set; }
        public bool IsParkedAtStation { get; private set; }
        public bool IsDeparted { get; private set; }
        public bool IsDeparting { get; private set; }
        public bool IsMoving => motionRoutine != null || hitShakeRoutine != null;
        internal GarageView SourceGarage { get; private set; }
        public int CapacityUnits => BusSizeUtility.ToPassengerUnits(Size);
        public int CapacityPeople => BusSizeUtility.ToPeopleCapacity(Size);
        public int BoardedUnits => boardedUnits;
        public int RemainingPeople => Mathf.Max(0, (CapacityUnits - boardedUnits) * 4);
        public bool IsFull => boardedUnits >= CapacityUnits;
        public bool IsBoardingPassengers => boardingUnitsInProgress > 0;
        public bool HasBoardingReservations => reservedUnits > 0;
        public bool HasAvailableBoardingSeat => boardedUnits + boardingUnitsInProgress + reservedUnits < CapacityUnits;
        private float VisualLength => BusSizeUtility.ToVisualLengthCells(Size) * cellSize;
        private float VisualWidth => BoardLayoutConfig.VehicleVisualWidthCells * cellSize;
        private float VisualHeight => BoardLayoutConfig.VehicleVisualHeightCells * cellSize;
        private float VisualCharacterLength => VisualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(Size));
        private float VisualCenterZ => (VisualLength - VisualCharacterLength) * 0.5f;
        private float VisualFrontZ => VisualLength - VisualCharacterLength * 0.5f;
        private float VisualRearZ => -VisualCharacterLength * 0.5f;
        public Vector3 VehicleForwardWorld
        {
            get
            {
                var forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.0001f ? forward.normalized : GridDirectionUtility.ToWorldVector(Direction);
            }
        }

        public static BusView Create(BusDefinition definition, Transform parent, float cellSize)
        {
            var busObject = new GameObject($"{PuzzlePalette.DisplayName(definition.Color)} {BusSizeUtility.DisplayName(definition.Size)}");
            busObject.transform.SetParent(parent, false);

            var view = busObject.AddComponent<BusView>();
            view.Initialize(definition, cellSize);
            return view;
        }

        public void Initialize(BusDefinition definition, float boardCellSize)
        {
            Color = definition.Color;
            Size = definition.Size;
            Direction = definition.Direction;
            AngleOffsetDegrees = definition.AngleOffsetDegrees;
            GridPosition = definition.GridPosition;
            cellSize = boardCellSize;
            boardedUnits = 0;
            reservedUnits = 0;
            boardingUnitsInProgress = 0;
            StationSlotIndex = -1;
            IsOnBoard = true;
            IsParkedAtStation = false;
            IsDeparted = false;
            IsDeparting = false;
            SourceGarage = null;

            transform.rotation = definition.Rotation;

            usingModelVisual = CreateModelBody();
            if (!usingModelVisual)
            {
                VehicleFallbackVisualBuilder.Create(
                    Color,
                    transform,
                    VisualWidth,
                    VisualHeight,
                    VisualLength,
                    VisualCharacterLength,
                    VisualCenterZ,
                    VisualFrontZ,
                    VisualRearZ,
                    cellSize);
            }

            GroundShadowBuilder.CreateVehicleShadow(transform, VisualWidth, VisualLength, VisualCenterZ, cellSize);
            CreateArrow();
            boardingCounter = VehicleBoardingCounter.Create(transform, Color, cellSize, VisualRearZ);
            UpdateBoardingCounter();
            CreateUnitMarkers();
        }

        public void SetGridPosition(Vector2Int gridPosition, Vector3 worldPosition)
        {
            GridPosition = gridPosition;
            transform.position = worldPosition;
        }

        internal void SetSourceGarage(GarageView garage)
        {
            SourceGarage = garage;
        }

        public void SetVipHighlight(bool highlighted)
        {
            if (!highlighted)
            {
                StopVipHighlight();
                return;
            }

            EnsureVipHighlight();
            if (vipHighlightRoutine == null && gameObject.activeInHierarchy)
            {
                vipHighlightRoutine = StartCoroutine(VipHighlightRoutine());
            }
        }

        public Vector3 GetRootPositionForVisualCenter(Vector3 visualCenterPosition, GridDirection facingDirection)
        {
            return GetRootPositionForVisualCenter(visualCenterPosition, GridDirectionUtility.ToRotation(facingDirection));
        }

        public Vector3 GetRootPositionForVisualCenter(Vector3 visualCenterPosition, Quaternion facingRotation)
        {
            return visualCenterPosition - facingRotation * new Vector3(0f, 0f, VisualCenterZ);
        }

        public VehicleFootprint CurrentFootprint => GetFootprint(transform.position, transform.rotation);

        public VehicleFootprint GetFootprint(Vector3 rootPosition, Quaternion rotation)
        {
            return BoardLayoutConfig.GetVehicleFootprint(rootPosition, rotation, Size, cellSize);
        }

        public bool CanBoard(PassengerView passenger)
        {
            return passenger != null && IsParkedAtStation && !IsDeparted && HasAvailableBoardingSeat && passenger.Color == Color;
        }

        public bool ReserveBoardingSeat()
        {
            if (!IsParkedAtStation || IsDeparted || !HasAvailableBoardingSeat)
            {
                return false;
            }

            reservedUnits++;
            return true;
        }

        public void CancelBoardingReservation()
        {
            reservedUnits = Mathf.Max(0, reservedUnits - 1);
        }

        public void MoveToStation(BusRouteStep[] route, int stationSlotIndex, Vector3 counterWorldPosition, Action onComplete)
        {
            StopVipHighlight();
            boardingCounter?.SetWorldPosition(counterWorldPosition);
            if (route == null || route.Length == 0)
            {
                IsOnBoard = false;
                IsParkedAtStation = true;
                StationSlotIndex = stationSlotIndex;
                HideDirectionArrow();
                ShowBoardingCounter();
                onComplete?.Invoke();
                return;
            }

            StopMotion();
            IsOnBoard = false;
            StationSlotIndex = stationSlotIndex;
            HideDirectionArrow();
            motionRoutine = StartCoroutine(MoveRouteRoutine(route, 0.17f, () =>
            {
                IsParkedAtStation = true;
                motionRoutine = null;
                ShowBoardingCounter();
                onComplete?.Invoke();
            }));
        }

        public void TeleportToStation(Vector3 stationVisualCenterPosition, Quaternion stationRotation, int stationSlotIndex, Vector3 counterWorldPosition, Action onComplete)
        {
            StopMotion();
            StopVipHighlight();

            var rootPosition = GetRootPositionForVisualCenter(stationVisualCenterPosition, stationRotation);
            transform.SetPositionAndRotation(rootPosition, stationRotation);

            boardingCounter?.SetWorldPosition(counterWorldPosition);
            IsOnBoard = false;
            IsParkedAtStation = true;
            StationSlotIndex = stationSlotIndex;
            HideDirectionArrow();
            ShowBoardingCounter();
            onComplete?.Invoke();
        }

        public void BounceBlocked(Vector3 worldDirection, Action onComplete)
        {
            StopMotion();
            motionRoutine = StartCoroutine(VehicleCollisionFeedback.PlayBounce(transform, worldDirection, () =>
            {
                motionRoutine = null;
                onComplete?.Invoke();
            }));
        }

        public void PlayBlockedCollision(Vector3 collisionPosition, Vector3 worldDirection, BusView blockingBus, Action onComplete)
        {
            StopMotion();
            motionRoutine = StartCoroutine(VehicleCollisionFeedback.PlayBlockedCollision(
                transform,
                cellSize,
                collisionPosition,
                worldDirection,
                () => blockingBus?.PlayHitShake(worldDirection),
                () =>
                {
                    motionRoutine = null;
                    onComplete?.Invoke();
                }));
        }

        public void BoardPassenger(PassengerView passenger, Action onComplete)
        {
            if (!CanBoard(passenger))
            {
                onComplete?.Invoke();
                return;
            }

            reservedUnits++;
            BoardReservedPassenger(passenger, onComplete);
        }

        public void BoardReservedPassenger(PassengerView passenger, Action onComplete)
        {
            if (passenger == null || !IsParkedAtStation || IsDeparted || reservedUnits <= 0 || passenger.Color != Color)
            {
                CancelBoardingReservation();
                passenger?.CancelBoardingReservation();
                onComplete?.Invoke();
                return;
            }

            reservedUnits = Mathf.Max(0, reservedUnits - 1);
            boardingUnitsInProgress++;
            VehicleBoardingSequence.BoardPassenger(passenger, transform, Color, cellSize, VisualFrontZ, VisualCharacterLength, () =>
            {
                boardingUnitsInProgress = Mathf.Max(0, boardingUnitsInProgress - 1);
                boardedUnits++;
                UpdateBoardingCounter();
                onComplete?.Invoke();
            });
        }

        public void Depart(BusRouteStep[] route, Action onComplete)
        {
            Depart(route, null, onComplete);
        }

        public void Depart(BusRouteStep[] route, Action onStationCleared, Action onComplete)
        {
            if (IsDeparted)
            {
                onComplete?.Invoke();
                return;
            }

            StopMotion();
            StopVipHighlight();
            HideBoardingCounter();
            IsDeparting = true;
            motionRoutine = StartCoroutine(DepartRoutine(route, onStationCleared, onComplete));
        }

        private void LateUpdate()
        {
            boardingCounter?.LateUpdate();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            VehicleFootprintGizmos.Draw(this, false);
        }

        private void OnDrawGizmosSelected()
        {
            VehicleFootprintGizmos.Draw(this, true);
        }
#endif

        private void StopMotion()
        {
            if (motionRoutine == null)
            {
                return;
            }

            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }

        private void EnsureVipHighlight()
        {
            if (vipHighlight != null)
            {
                vipHighlight.SetActive(true);
                return;
            }

            var material = PuzzlePalette.CreateSolidMaterial("VIP Bus Select Highlight", new Color(1.00f, 0.78f, 0.16f));
            vipHighlight = BoardGeometry.CreateFlatRoundedRect(
                "VIP Select Highlight",
                transform,
                Vector3.zero,
                new Vector2(VisualWidth + cellSize * 0.20f, VisualLength + cellSize * 0.18f),
                cellSize * 0.12f,
                material);
            vipHighlight.transform.localPosition = new Vector3(0f, 0.018f, VisualCenterZ);
            vipHighlight.transform.localRotation = Quaternion.identity;
            vipHighlight.SetActive(true);
        }

        private void StopVipHighlight()
        {
            if (vipHighlightRoutine != null)
            {
                StopCoroutine(vipHighlightRoutine);
                vipHighlightRoutine = null;
            }

            if (vipHighlight != null)
            {
                vipHighlight.SetActive(false);
            }
        }

        private IEnumerator VipHighlightRoutine()
        {
            while (true)
            {
                if (vipHighlight != null)
                {
                    vipHighlight.SetActive(Mathf.PingPong(Time.time * 4.4f, 1f) > 0.28f);
                }

                yield return null;
            }
        }

        private void PlayHitShake(Vector3 worldDirection)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (hitShakeRoutine != null)
            {
                StopCoroutine(hitShakeRoutine);
                transform.SetPositionAndRotation(hitShakeStartPosition, hitShakeStartRotation);
                hitShakeRoutine = null;
            }

            hitShakeStartPosition = transform.position;
            hitShakeStartRotation = transform.rotation;
            hitShakeRoutine = StartCoroutine(VehicleCollisionFeedback.PlayHitShake(transform, cellSize, worldDirection, () => hitShakeRoutine = null));
        }

        private bool CreateModelBody()
        {
            return VehicleModelBuilder.Create(Size, Color, transform, VisualWidth, VisualHeight, VisualLength, VisualCenterZ, cellSize) != null;
        }

        private void CreateArrow()
        {
            directionArrow = VehicleDirectionArrow.Create(transform, VisualWidth, VisualLength, VisualHeight, VisualCenterZ, cellSize);
        }

        private void ShowBoardingCounter()
        {
            boardingCounter?.Show(RemainingPeople);
        }

        private void HideBoardingCounter()
        {
            boardingCounter?.Hide();
        }

        private void HideDirectionArrow()
        {
            if (directionArrow != null)
            {
                directionArrow.SetActive(false);
            }
        }

        private void UpdateBoardingCounter()
        {
            boardingCounter?.UpdateText(RemainingPeople);
        }

        private void CreateUnitMarkers()
        {
            var rows = Mathf.CeilToInt(CapacityUnits / 2f);
            var rearZ = VisualRearZ + VisualCharacterLength * 0.18f;
            var frontZ = VisualFrontZ - VisualCharacterLength * 0.18f;
            var markerY = usingModelVisual ? VisualHeight + cellSize * 0.12f : VisualHeight + cellSize * 0.44f;

            for (var index = 0; index < CapacityUnits; index++)
            {
                var row = index / 2;
                var column = index % 2;
                var t = rows <= 1 ? 0.5f : row / (rows - 1f);

                var marker = new GameObject($"Unit Seat {index + 1}");
                marker.name = $"Unit Seat {index + 1}";
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = new Vector3(column == 0 ? -VisualWidth * 0.24f : VisualWidth * 0.24f, markerY, Mathf.Lerp(rearZ, frontZ, t));

            }
        }

        private IEnumerator MoveRouteRoutine(BusRouteStep[] route, float secondsPerSegment, Action onComplete, Action<float> onProgress = null)
        {
            var smoothRoute = VehicleRouteMotion.Build(route, transform.position, transform.rotation, cellSize);
            if (smoothRoute.Points.Count < 2 || smoothRoute.Length < 0.001f)
            {
                yield return RotateRouteEnd(route);
                onProgress?.Invoke(1f);
                onComplete?.Invoke();
                yield break;
            }

            var duration = Mathf.Max(0.10f, secondsPerSegment * smoothRoute.Length);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = VehicleRouteMotion.EaseDriveProgress(Mathf.Clamp01(elapsed / duration));
                var travelDistance = smoothRoute.Length * progress;
                var position = VehicleRouteMotion.EvaluatePosition(smoothRoute, travelDistance);
                var rotation = VehicleRouteMotion.EvaluateRotation(smoothRoute, travelDistance, progress, transform.rotation, cellSize);
                transform.SetPositionAndRotation(position, rotation);
                onProgress?.Invoke(progress);
                yield return null;
            }

            transform.SetPositionAndRotation(smoothRoute.Points[smoothRoute.Points.Count - 1], smoothRoute.FinalRotation);
            onProgress?.Invoke(1f);
            onComplete?.Invoke();
        }

        private IEnumerator RotateRouteEnd(BusRouteStep[] route)
        {
            if (route == null || route.Length == 0)
            {
                yield break;
            }

            var targetRotation = route[route.Length - 1].Rotation;
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            {
                transform.rotation = targetRotation;
                yield break;
            }

            var startRotation = transform.rotation;
            var elapsed = 0f;
            const float duration = 0.08f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            transform.rotation = targetRotation;
        }

        private IEnumerator DepartRoutine(BusRouteStep[] route, Action onStationCleared, Action onComplete)
        {
            IsDeparted = true;
            IsParkedAtStation = false;
            yield return new WaitForSeconds(0.12f);
            EffectFactory.PlayDepartureTrail(
                transform.TransformPoint(new Vector3(0f, 0f, VisualRearZ - cellSize * 0.08f)),
                -VehicleForwardWorld,
                Color,
                cellSize);

            var stationCleared = false;
            void ClearStationOnce()
            {
                if (stationCleared)
                {
                    return;
                }

                stationCleared = true;
                onStationCleared?.Invoke();
            }

            if (route != null && route.Length > 0)
            {
                yield return MoveRouteRoutine(route, 0.16f, null, progress =>
                {
                    if (progress >= DepartureStationClearProgress)
                    {
                        ClearStationOnce();
                    }
                });
            }
            else
            {
                var fallbackRoute = new[]
                {
                    new BusRouteStep(transform.position + new Vector3(7.5f, 0f, 0.8f), transform.rotation)
                };
                yield return MoveRouteRoutine(fallbackRoute, 0.16f, null, progress =>
                {
                    if (progress >= DepartureStationClearProgress)
                    {
                        ClearStationOnce();
                    }
                });
            }

            ClearStationOnce();
            gameObject.SetActive(false);
            IsDeparting = false;
            motionRoutine = null;
            onComplete?.Invoke();
        }
    }
}
