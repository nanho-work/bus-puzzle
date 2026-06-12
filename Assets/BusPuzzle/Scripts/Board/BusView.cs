using System;
using System.Collections;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BusView : MonoBehaviour
    {
        private const float DepartureStationClearProgress = 0.20f;
        private const float DrivingTrailInterval = 0.040f;
        private const float StationIdlePulseSpeed = 6.1f;
        private const float StationIdlePulseSettleSpeed = 13f;
        private const float StationIdlePulseHorizontalScale = 0.040f;
        private const float StationIdlePulseVerticalScale = 0.018f;
        private const float StationIdlePulseLift = 0.006f;
        private const float BoardingShakeSpeed = 38f;
        private const float BoardingShakeHorizontalScale = 0.018f;
        private const float BoardingShakeVerticalScale = 0.010f;
        private const float BoardingShakeYawDegrees = 1.2f;
        private const float MysteryBadgeCharacterSize = 0.115f;
        private const float MysteryBadgeShadowOffset = 0.007f;

        private Coroutine motionRoutine;
        private Coroutine hitShakeRoutine;
        private Coroutine vipHighlightRoutine;
        private GameObject directionArrow;
        private GameObject vipHighlight;
        private GameObject mysteryBadge;
        private VehicleBoardingCounter boardingCounter;
        private BoxCollider touchCollider;
        private Transform visualBodyRoot;
        private Vector3 hitShakeStartPosition;
        private Quaternion hitShakeStartRotation;
        private float cellSize = 1.2f;
        private int boardedUnits;
        private int reservedUnits;
        private int boardingUnitsInProgress;
        private bool usingModelVisual;
        private Vector3 baseLocalScale = Vector3.one;
        private Vector3 baseVisualBodyLocalPosition = Vector3.zero;
        private Vector3 baseVisualBodyLocalScale = Vector3.one;
        private Quaternion baseVisualBodyLocalRotation = Quaternion.identity;
        private float idlePulsePhase;

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
        public bool IsConcealed { get; private set; }
        public bool IsMoving => motionRoutine != null || hitShakeRoutine != null;
        internal GarageView SourceGarage { get; private set; }
        public int CapacityUnits => BusSizeUtility.ToPassengerUnits(Size);
        public int CapacityPeople => BusSizeUtility.ToPeopleCapacity(Size);
        public int BoardedUnits => boardedUnits;
        public int RemainingPeople => Mathf.Max(0, (CapacityUnits - boardedUnits) * PassengerUnitLayout.PeoplePerUnit);
        public bool IsFull => boardedUnits >= CapacityUnits;
        public bool IsBoardingPassengers => boardingUnitsInProgress > 0;
        public bool HasBoardingReservations => reservedUnits > 0;
        public bool HasAvailableBoardingSeat => boardedUnits + boardingUnitsInProgress + reservedUnits < CapacityUnits;
        private float VisualLength => BusSizeUtility.ToVisualLengthCells(Size) * cellSize;
        private float VisualWidth => BoardLayoutConfig.VehicleVisualWidthCells * cellSize;
        private float VisualHeight => BoardLayoutConfig.VehicleVisualHeightCells * cellSize;
        private float VisualCharacterLength => VisualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(Size));
        private float VisualCenterZ => (VisualLength - VisualCharacterLength) * 0.5f;
        private float BodyVisualWidth => VisualWidth * BoardLayoutConfig.VehicleBodyVisualWidthScale;
        private float BodyVisualLength => VisualLength * BoardLayoutConfig.VehicleBodyVisualLengthScale;
        private float BodyVisualCharacterLength => BodyVisualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(Size));
        private float BodyVisualCenterZ => VisualCenterZ;
        private float BodyVisualFrontZ => BodyVisualCenterZ + BodyVisualLength * 0.5f;
        private float BodyVisualRearZ => BodyVisualCenterZ - BodyVisualLength * 0.5f;
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
            var busObject = new GameObject(GetDisplayName(definition));
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
            IsConcealed = definition.StartsConcealed;
            SourceGarage = null;
            baseLocalScale = Vector3.one;
            transform.localScale = baseLocalScale;
            visualBodyRoot = null;
            baseVisualBodyLocalPosition = Vector3.zero;
            baseVisualBodyLocalScale = Vector3.one;
            baseVisualBodyLocalRotation = Quaternion.identity;
            idlePulsePhase = UnityEngine.Random.value * Mathf.PI * 2f;

            transform.rotation = definition.Rotation;
            BuildVisuals();
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

        public void Recolor(PuzzleColor newColor)
        {
            if (Color == newColor || !IsOnBoard || IsMoving || IsDeparted || IsConcealed)
            {
                return;
            }

            StopVipHighlight();
            ClearVisualChildren();
            Color = newColor;
            gameObject.name = $"{PuzzlePalette.DisplayName(Color)} {BusSizeUtility.DisplayName(Size)}";
            BuildVisuals();
        }

        public bool RevealConcealed()
        {
            if (!IsConcealed || IsMoving || IsDeparted)
            {
                return false;
            }

            StopVipHighlight();
            ClearVisualChildren();
            IsConcealed = false;
            gameObject.name = $"{PuzzlePalette.DisplayName(Color)} {BusSizeUtility.DisplayName(Size)}";
            BuildVisuals();
            return true;
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
            MoveToStation(route, stationSlotIndex, counterWorldPosition, onComplete, null);
        }

        public void MoveToStation(
            BusRouteStep[] route,
            int stationSlotIndex,
            Vector3 counterWorldPosition,
            Action onComplete,
            Action onLaunchClearanceReached)
        {
            StopVipHighlight();
            ResetStationIdlePulse();
            boardingCounter?.SetWorldPosition(counterWorldPosition);
            if (route == null || route.Length == 0)
            {
                IsOnBoard = false;
                IsParkedAtStation = true;
                StationSlotIndex = stationSlotIndex;
                HideDirectionArrow();
                ShowBoardingCounter();
                onLaunchClearanceReached?.Invoke();
                onComplete?.Invoke();
                return;
            }

            StopMotion();
            IsOnBoard = false;
            StationSlotIndex = stationSlotIndex;
            HideDirectionArrow();
            var startPosition = transform.position;
            var clearanceReached = false;

            void InvokeLaunchClearanceOnce()
            {
                if (clearanceReached)
                {
                    return;
                }

                clearanceReached = true;
                onLaunchClearanceReached?.Invoke();
            }

            motionRoutine = StartCoroutine(MoveRouteRoutine(route, 0.17f, () =>
            {
                InvokeLaunchClearanceOnce();
                IsParkedAtStation = true;
                motionRoutine = null;
                ShowBoardingCounter();
                onComplete?.Invoke();
            }, progress =>
            {
                if (!clearanceReached && Vector3.Distance(startPosition, transform.position) >= cellSize * 2f)
                {
                    InvokeLaunchClearanceOnce();
                }
            }, true));
        }

        public void EmergeFromGarage(Vector3 startPosition, Vector3 targetPosition, Action onComplete = null)
        {
            StopMotion();
            ResetStationIdlePulse();
            transform.position = startPosition;
            var route = new[]
            {
                new BusRouteStep(targetPosition, transform.rotation)
            };

            motionRoutine = StartCoroutine(MoveRouteRoutine(route, 0.20f, () =>
            {
                motionRoutine = null;
                onComplete?.Invoke();
            }));
        }

        public void TeleportToStation(Vector3 stationVisualCenterPosition, Quaternion stationRotation, int stationSlotIndex, Vector3 counterWorldPosition, Action onComplete)
        {
            StopMotion();
            StopVipHighlight();
            ResetStationIdlePulse();

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
            BoardReservedPassenger(passenger, null, onComplete);
        }

        internal void BoardReservedPassenger(PassengerView passenger, PassengerUnitRoadPose? boardingGatePose, Action onComplete)
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
            ResetStationIdlePulse();
            VehicleBoardingSequence.BoardPassenger(passenger, transform, Color, cellSize, BodyVisualFrontZ, BodyVisualCharacterLength, boardingGatePose, () =>
            {
                var wasFull = IsFull;
                boardingUnitsInProgress = Mathf.Max(0, boardingUnitsInProgress - 1);
                boardedUnits++;
                UpdateBoardingCounter();
                if (!wasFull && IsFull)
                {
                    EffectAudioPlayer.PlayBusFull();
                    HapticFeedback.PlayBusFull();
                }

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
            ResetStationIdlePulse();
            HideBoardingCounter();
            IsDeparting = true;
            motionRoutine = StartCoroutine(DepartRoutine(route, onStationCleared, onComplete));
        }

        private void LateUpdate()
        {
            ApplyStationIdlePulse();
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
            ResetStationIdlePulse();
        }

        private void ApplyStationIdlePulse()
        {
            if (ShouldPlayBoardingShake())
            {
                var waveA = Mathf.Sin(Time.time * BoardingShakeSpeed + idlePulsePhase);
                var waveB = Mathf.Sin(Time.time * (BoardingShakeSpeed * 1.37f) + idlePulsePhase * 0.6f);
                ApplyVisualBodyPose(
                    new Vector3(
                        1f + waveA * BoardingShakeHorizontalScale,
                        1f + Mathf.Abs(waveB) * BoardingShakeVerticalScale,
                        1f - waveA * BoardingShakeHorizontalScale * 0.55f),
                    Vector3.up * (Mathf.Abs(waveB) * cellSize * StationIdlePulseLift),
                    Quaternion.Euler(0f, waveA * BoardingShakeYawDegrees, 0f));
                return;
            }

            if (ShouldPlayStationIdlePulse())
            {
                var pulse = (Mathf.Sin(Time.time * StationIdlePulseSpeed + idlePulsePhase) + 1f) * 0.5f;
                ApplyVisualBodyPose(
                    new Vector3(
                        1f + pulse * StationIdlePulseHorizontalScale,
                        1f + pulse * StationIdlePulseVerticalScale,
                        1f + pulse * StationIdlePulseHorizontalScale),
                    Vector3.up * (pulse * cellSize * StationIdlePulseLift),
                    Quaternion.identity);
                return;
            }

            if (IsVisualBodyAtRest())
            {
                ResetStationIdlePulse();
                return;
            }

            SettleVisualBodyPose();
        }

        private void ApplyVisualBodyPose(Vector3 scaleMultiplier, Vector3 localOffset, Quaternion localRotationOffset)
        {
            if (visualBodyRoot == null)
            {
                transform.localScale = new Vector3(
                    baseLocalScale.x * scaleMultiplier.x,
                    baseLocalScale.y * scaleMultiplier.y,
                    baseLocalScale.z * scaleMultiplier.z);
                return;
            }

            visualBodyRoot.localScale = new Vector3(
                baseVisualBodyLocalScale.x * scaleMultiplier.x,
                baseVisualBodyLocalScale.y * scaleMultiplier.y,
                baseVisualBodyLocalScale.z * scaleMultiplier.z);
            visualBodyRoot.localPosition = baseVisualBodyLocalPosition + localOffset;
            visualBodyRoot.localRotation = baseVisualBodyLocalRotation * localRotationOffset;
        }

        private bool IsVisualBodyAtRest()
        {
            if (visualBodyRoot == null)
            {
                return (transform.localScale - baseLocalScale).sqrMagnitude <= 0.00001f;
            }

            return (visualBodyRoot.localScale - baseVisualBodyLocalScale).sqrMagnitude <= 0.00001f &&
                   (visualBodyRoot.localPosition - baseVisualBodyLocalPosition).sqrMagnitude <= 0.000001f &&
                   Quaternion.Angle(visualBodyRoot.localRotation, baseVisualBodyLocalRotation) <= 0.05f;
        }

        private void SettleVisualBodyPose()
        {
            if (visualBodyRoot == null)
            {
                transform.localScale = Vector3.Lerp(
                    transform.localScale,
                    baseLocalScale,
                    Time.deltaTime * StationIdlePulseSettleSpeed);
                return;
            }

            visualBodyRoot.localScale = Vector3.Lerp(
                visualBodyRoot.localScale,
                baseVisualBodyLocalScale,
                Time.deltaTime * StationIdlePulseSettleSpeed);
            visualBodyRoot.localPosition = Vector3.Lerp(
                visualBodyRoot.localPosition,
                baseVisualBodyLocalPosition,
                Time.deltaTime * StationIdlePulseSettleSpeed);
            visualBodyRoot.localRotation = Quaternion.Slerp(
                visualBodyRoot.localRotation,
                baseVisualBodyLocalRotation,
                Time.deltaTime * StationIdlePulseSettleSpeed);
        }

        private bool ShouldPlayStationIdlePulse()
        {
            return IsParkedAtStation &&
                   !IsDeparted &&
                   !IsDeparting &&
                   !IsFull &&
                   !IsMoving &&
                   !IsBoardingPassengers &&
                   !HasBoardingReservations;
        }

        private bool ShouldPlayBoardingShake()
        {
            return IsParkedAtStation &&
                   !IsDeparted &&
                   !IsDeparting &&
                   !IsMoving &&
                   IsBoardingPassengers;
        }

        private void ResetStationIdlePulse()
        {
            transform.localScale = baseLocalScale;
            if (visualBodyRoot == null)
            {
                return;
            }

            visualBodyRoot.localPosition = baseVisualBodyLocalPosition;
            visualBodyRoot.localScale = baseVisualBodyLocalScale;
            visualBodyRoot.localRotation = baseVisualBodyLocalRotation;
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
                new Vector2(BodyVisualWidth + cellSize * 0.20f, BodyVisualLength + cellSize * 0.18f),
                cellSize * 0.12f,
                material);
            vipHighlight.transform.localPosition = new Vector3(0f, 0.018f, BodyVisualCenterZ);
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
            return AssignVisualBodyRoot(VehicleModelBuilder.Create(Size, Color, transform, BodyVisualWidth, VisualHeight, BodyVisualLength, BodyVisualCenterZ, cellSize));
        }

        private bool CreateConcealedModelBody()
        {
            return AssignVisualBodyRoot(VehicleModelBuilder.CreateSilhouette(Size, transform, BodyVisualWidth, VisualHeight, BodyVisualLength, BodyVisualCenterZ, cellSize));
        }

        private bool AssignVisualBodyRoot(GameObject modelRoot)
        {
            if (modelRoot == null)
            {
                visualBodyRoot = null;
                return false;
            }

            visualBodyRoot = modelRoot.transform;
            baseVisualBodyLocalPosition = visualBodyRoot.localPosition;
            baseVisualBodyLocalScale = visualBodyRoot.localScale;
            baseVisualBodyLocalRotation = visualBodyRoot.localRotation;
            return true;
        }

        private void BuildVisuals()
        {
            usingModelVisual = IsConcealed ? CreateConcealedModelBody() : CreateModelBody();
            if (!usingModelVisual && !IsConcealed)
            {
                VehicleFallbackVisualBuilder.Create(
                    Color,
                    transform,
                    BodyVisualWidth,
                    VisualHeight,
                    BodyVisualLength,
                    BodyVisualCharacterLength,
                    BodyVisualCenterZ,
                    BodyVisualFrontZ,
                    BodyVisualRearZ,
                    cellSize);
            }

            GroundShadowBuilder.CreateVehicleShadow(transform, BodyVisualWidth, BodyVisualLength, BodyVisualCenterZ, cellSize);
            if (!IsConcealed)
            {
                CreateArrow();
            }

            if (IsConcealed)
            {
                CreateMysteryBadge();
            }
            else
            {
                boardingCounter = VehicleBoardingCounter.Create(transform, Color, cellSize, BodyVisualRearZ);
                UpdateBoardingCounter();
                CreateUnitMarkers();
            }

            ConfigureTouchCollider();
        }

        private void ClearVisualChildren()
        {
            directionArrow = null;
            vipHighlight = null;
            mysteryBadge = null;
            boardingCounter = null;
            visualBodyRoot = null;
            baseVisualBodyLocalPosition = Vector3.zero;
            baseVisualBodyLocalScale = Vector3.one;
            baseVisualBodyLocalRotation = Quaternion.identity;

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
        }

        private void CreateArrow()
        {
            directionArrow = VehicleDirectionArrow.Create(transform, BodyVisualWidth, BodyVisualLength, VisualHeight, BodyVisualCenterZ, cellSize);
        }

        private void CreateMysteryBadge()
        {
            mysteryBadge = new GameObject("Mystery Badge");
            mysteryBadge.transform.SetParent(transform, false);
            mysteryBadge.transform.localPosition = new Vector3(0f, VisualHeight + cellSize * 0.18f, BodyVisualCenterZ);
            mysteryBadge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreateMysteryText("Mystery Shadow", new Color(0.015f, 0.018f, 0.024f, 0.96f), new Vector3(cellSize * MysteryBadgeShadowOffset, -cellSize * MysteryBadgeShadowOffset, 0.010f));
            CreateMysteryText("Mystery Mark", new Color(0.86f, 0.94f, 1.00f, 0.96f), Vector3.zero);
        }

        private TextMesh CreateMysteryText(string name, Color color, Vector3 localPosition)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(mysteryBadge.transform, false);
            textObject.transform.localPosition = localPosition;

            var text = textObject.AddComponent<TextMesh>();
            text.text = "?";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = cellSize * MysteryBadgeCharacterSize;
            text.fontSize = 48;
            text.color = color;
            GameFontProvider.ApplyToTextMesh(text, FontStyle.Bold);

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return text;
        }

        private static string GetDisplayName(BusDefinition definition)
        {
            return definition.StartsConcealed
                ? $"Mystery {BusSizeUtility.DisplayName(definition.Size)}"
                : $"{PuzzlePalette.DisplayName(definition.Color)} {BusSizeUtility.DisplayName(definition.Size)}";
        }

        private void ConfigureTouchCollider()
        {
            if (touchCollider == null)
            {
                touchCollider = GetComponent<BoxCollider>();
                if (touchCollider == null)
                {
                    touchCollider = gameObject.AddComponent<BoxCollider>();
                }
            }

            touchCollider.center = new Vector3(0f, VisualHeight * 0.52f, VisualCenterZ);
            touchCollider.size = new Vector3(
                VisualWidth * 1.22f,
                VisualHeight * 1.32f,
                VisualLength * 1.14f);
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
            var rearZ = BodyVisualRearZ + BodyVisualCharacterLength * 0.18f;
            var frontZ = BodyVisualFrontZ - BodyVisualCharacterLength * 0.18f;
            var markerY = usingModelVisual ? VisualHeight + cellSize * 0.12f : VisualHeight + cellSize * 0.44f;

            for (var index = 0; index < CapacityUnits; index++)
            {
                var row = index / 2;
                var column = index % 2;
                var t = rows <= 1 ? 0.5f : row / (rows - 1f);

                var marker = new GameObject($"Unit Seat {index + 1}");
                marker.name = $"Unit Seat {index + 1}";
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = new Vector3(column == 0 ? -BodyVisualWidth * 0.24f : BodyVisualWidth * 0.24f, markerY, Mathf.Lerp(rearZ, frontZ, t));

            }
        }

        private IEnumerator MoveRouteRoutine(
            BusRouteStep[] route,
            float secondsPerSegment,
            Action onComplete,
            Action<float> onProgress = null,
            bool playDrivingTrail = false)
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
            var nextTrailTime = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = VehicleRouteMotion.EaseDriveProgress(Mathf.Clamp01(elapsed / duration));
                var travelDistance = smoothRoute.Length * progress;
                var position = VehicleRouteMotion.EvaluatePosition(smoothRoute, travelDistance);
                var rotation = VehicleRouteMotion.EvaluateRotation(smoothRoute, travelDistance, progress, transform.rotation, cellSize);
                transform.SetPositionAndRotation(position, rotation);
                if (playDrivingTrail && elapsed >= nextTrailTime && progress > 0.03f && progress < 0.96f)
                {
                    EffectFactory.PlayDrivingTrail(
                        transform.TransformPoint(new Vector3(0f, 0f, BodyVisualRearZ - cellSize * 0.04f)),
                        -VehicleForwardWorld,
                        cellSize);
                    nextTrailTime = elapsed + DrivingTrailInterval;
                }

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
                transform.TransformPoint(new Vector3(0f, 0f, BodyVisualRearZ - cellSize * 0.08f)),
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
