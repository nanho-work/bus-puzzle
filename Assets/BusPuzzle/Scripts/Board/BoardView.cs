using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BoardView : MonoBehaviour
    {
        public const int VipStationSlotIndex = -1;

        private const float BoardingGateProgressWindow = 0.022f;
        private const float BoardingReservationProgressWindow = 0.180f;
        private const float FeederMergeDuration = 0.34f;
        private const float FeederQueueStepDuration = 0.20f;
        private const float StationUpperRoadForwardCells = 2.05f;
        private const float RotaryStationLaneClearanceCells = 0.55f;
        private const float RotaryVisualGuardrailPaddingCells = 0.45f;
        private const float CameraVisibleFeederRows = 1.45f;
        private const float CameraFeederTopPadding = 0.12f;

        private readonly StationSlotController stationSlots = new StationSlotController(
            BoardLayoutConfig.ActiveStationSlots,
            BoardLayoutConfig.ActiveStationSlots + BoardLayoutConfig.LockedStationSlots);
        private readonly List<GarageView> garages = new List<GarageView>();

        private RotaryLayout rotaryLayout;
        private VehicleTrafficEngine vehicleTraffic;
        private PassengerTrafficEngine passengerTraffic;
        private int rotaryActiveTarget;
        private Transform passengerRoot;
        private Transform busRoot;
        private Transform garageRoot;
        private Transform stationRoot;
        private Transform themeRoot;
        private Coroutine tutorialStationHighlightRoutine;
        private GameObject tutorialStationHighlight;
        private int currentPassengerUnitCount;
        private float rotaryCenterZ = BoardLayoutConfig.RotaryCenterZ;
        private bool vipStationSlotOccupied;

        public int StationCapacity => stationSlots.Capacity;

        public int MaxStationCapacity => stationSlots.MaxCapacity;

        public int LockedStationSlots => stationSlots.LockedSlots;

        public int OccupiedStationSlots => stationSlots.OccupiedSlots;

        public bool CanUnlockStationSlot => stationSlots.CanUnlock;

        public bool CanReserveVipStationSlot => !vipStationSlotOccupied;

        public Bounds GetCameraContentBounds()
        {
            var halfGridWidth = BoardLayoutConfig.GridWorldWidth * 0.5f + 0.14f;
            var halfStationWidth =
                (BoardLayoutConfig.TotalStationSlots - 1) * BoardLayoutConfig.StationSlotSpacing * 0.5f +
                BoardLayoutConfig.StationSlotWidth * 0.5f +
                0.18f;
            var hasVisibleFeederQueue = currentPassengerUnitCount > rotaryActiveTarget;
            var halfFeederWidth = hasVisibleFeederQueue
                ? GetMaxAbsFeederX(rotaryLayout.LeftFeederPath, rotaryLayout.RightFeederPath) +
                    rotaryLayout.RoadWidth +
                    0.18f
                : rotaryLayout.OuterRadiusX + 0.30f;
            var halfWidth = Mathf.Max(halfGridWidth, halfStationWidth, halfFeederWidth);
            var bottomZ = BoardLayoutConfig.GridBottomZ - BoardLayoutConfig.CellSize * 0.48f;
            var rotaryTopY = rotaryLayout.OuterRadiusZ + 0.42f;
            var feederTopY = hasVisibleFeederQueue
                ? rotaryLayout.VisibleFeederTopY + 0.20f
                : rotaryTopY;
            var cameraFeederTopY = hasVisibleFeederQueue
                ? Mathf.Min(
                    feederTopY,
                    rotaryTopY + BoardLayoutConfig.CellSize * CameraVisibleFeederRows + CameraFeederTopPadding)
                : rotaryTopY;
            var topZ = rotaryCenterZ + Mathf.Max(rotaryTopY, cameraFeederTopY);

            var center = new Vector3(0f, 0.10f, (bottomZ + topZ) * 0.5f);
            var size = new Vector3(halfWidth * 2f, 0.36f, topZ - bottomZ);
            return new Bounds(center, size);
        }

        public void BuildLevel(LevelData levelData, List<PassengerView> passengers, List<BusView> buses, int stageNumber = 0)
        {
            var roadPreset = GetRoadPresetForStage(levelData);
            var rotaryUnitCapacity = GetRotaryUnitCapacityForStage(levelData, roadPreset);
            var rotaryUnitSpacing = GetRotaryUnitSpacingForStage(levelData, roadPreset, rotaryUnitCapacity, stageNumber);
            var roadProfile = PassengerUnitLayout.CreateRoadProfile(roadPreset, GetRoadScaleForStage(levelData, roadPreset, rotaryUnitCapacity, stageNumber));
            rotaryLayout = RotaryLayout.Create(
                roadPreset,
                rotaryUnitCapacity,
                rotaryUnitSpacing,
                roadProfile);
            rotaryCenterZ = CalculateRotaryCenterZ(rotaryLayout);
            rotaryActiveTarget = GetStartingRotaryUnitCount(levelData.PassengerUnits.Count, roadPreset);
            currentPassengerUnitCount = levelData.PassengerUnits.Count;
            passengerTraffic = new PassengerTrafficEngine(rotaryLayout, CreatePassengerTrafficSettings(), rotaryActiveTarget);
            vehicleTraffic = new VehicleTrafficEngine(CreateVehicleTrafficSettings());

            ClearBoard();
            ResetStationSlots();
            garages.Clear();
            CreateRoots();
            CreateGround();
            CreateTheme();
            CreatePassengerRotary();
            CreateGrid();
            CreateStationSlots();

            passengers.Clear();
            buses.Clear();

            for (var index = 0; index < levelData.PassengerUnits.Count; index++)
            {
                var passenger = PassengerView.Create(levelData.PassengerUnits[index], passengerRoot);
                passengerTraffic.PlacePassenger(passenger, index);
                passengers.Add(passenger);
            }

            for (var index = 0; index < levelData.Garages.Count; index++)
            {
                var garage = GarageView.Create(levelData.Garages[index], garageRoot, BoardLayoutConfig.CellSize);
                garages.Add(garage);
                var bus = CreateBusView(levelData.Garages[index].FrontVehicle);
                bus.SetSourceGarage(garage);
                buses.Add(bus);
            }

            for (var index = 0; index < levelData.Buses.Count; index++)
            {
                buses.Add(CreateBusView(levelData.Buses[index]));
            }

            WarnIfBusesStartOverlapping(levelData, buses);
        }

        public void UpdatePassengerTraffic(IReadOnlyList<PassengerView> passengers, float deltaTime, float trafficTimeScale)
        {
            GetPassengerTraffic().Advance(passengers, deltaTime, trafficTimeScale);
        }

        public bool TryFindBoardingPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            return GetPassengerTraffic().TryFindBoardingPassenger(passengers, color, out passengerIndex);
        }

        public bool TryFindBoardingReservationPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            return GetPassengerTraffic().TryFindBoardingReservationPassenger(passengers, color, out passengerIndex);
        }

        public bool IsPassengerReadyToBoard(PassengerView passenger)
        {
            return GetPassengerTraffic().IsPassengerReadyToBoard(passenger);
        }

        internal PassengerUnitRoadPose GetBoardingGatePose()
        {
            return GetPassengerTraffic().GetBoardingGatePose();
        }

        public bool HasRotaryPassengerColor(IReadOnlyList<PassengerView> passengers, PuzzleColor color)
        {
            return PassengerTrafficEngine.HasRotaryPassengerColor(passengers, color);
        }

        public bool HasPendingRotaryFill(IReadOnlyList<PassengerView> passengers)
        {
            return GetPassengerTraffic().HasPendingRotaryFill(passengers);
        }

        public void CompactFeederQueues(IReadOnlyList<PassengerView> passengers)
        {
            GetPassengerTraffic().CompactFeederQueues(passengers);
        }

        public bool TryReserveStationSlot(out int slotIndex, out Vector3 slotPosition)
        {
            return stationSlots.TryReserve(BoardLayoutConfig.GetStationPosition, out slotIndex, out slotPosition);
        }

        public bool TryReserveVipStationSlot(out int slotIndex, out Vector3 slotPosition)
        {
            if (vipStationSlotOccupied)
            {
                slotIndex = VipStationSlotIndex;
                slotPosition = Vector3.zero;
                return false;
            }

            vipStationSlotOccupied = true;
            slotIndex = VipStationSlotIndex;
            slotPosition = BoardLayoutConfig.GetFreeStationPosition();
            return true;
        }

        public bool TryUnlockStationSlot()
        {
            if (!stationSlots.TryUnlock())
            {
                return false;
            }

            RebuildStationSlots();
            return true;
        }

        public void ReleaseStationSlot(int slotIndex)
        {
            if (slotIndex == VipStationSlotIndex)
            {
                vipStationSlotOccupied = false;
                return;
            }

            stationSlots.Release(slotIndex);
        }

        public bool IsAnyMoveAvailable(IReadOnlyList<BusView> buses)
        {
            return GetVehicleTraffic().IsAnyMoveAvailable(buses, garages, OccupiedStationSlots, StationCapacity);
        }

        public bool IsPathClear(BusView movingBus, IReadOnlyList<BusView> buses, out BusView blockingBus)
        {
            return IsPathClear(movingBus, buses, out blockingBus, out _);
        }

        public bool IsPathClear(BusView movingBus, IReadOnlyList<BusView> buses, out BusView blockingBus, out Vector3 collisionPosition)
        {
            return GetVehicleTraffic().IsPathClear(movingBus, buses, garages, out blockingBus, out collisionPosition);
        }

        public bool RevealPathClearConcealedBuses(IReadOnlyList<BusView> buses)
        {
            if (buses == null)
            {
                return false;
            }

            var revealedAny = false;
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus == null ||
                    !bus.IsConcealed ||
                    !bus.IsOnBoard ||
                    bus.IsMoving ||
                    bus.IsDeparted)
                {
                    continue;
                }

                if (IsPathClear(bus, buses, out _))
                {
                    revealedAny |= bus.RevealConcealed();
                }
            }

            return revealedAny;
        }

        public bool TryAdvanceGarageAfterLaunch(BusView launchedBus, List<BusView> buses)
        {
            var garage = launchedBus != null ? launchedBus.SourceGarage : null;
            if (garage == null || !garage.TryTakeNextVehicle(out var nextVehicle))
            {
                return false;
            }

            var spawnedBus = CreateBusView(nextVehicle);
            spawnedBus.SetSourceGarage(garage);
            var targetPosition = BoardLayoutConfig.GridToWorld(nextVehicle.GridPosition, nextVehicle.PositionOffsetCells);
            spawnedBus.EmergeFromGarage(
                garage.GetVehicleExitStartPosition(nextVehicle),
                targetPosition,
                garage.HideIfEmpty);
            buses.Add(spawnedBus);
            return true;
        }

        public BusRouteStep[] BuildRouteToStation(BusView bus, Vector3 stationPosition)
        {
            return GetVehicleTraffic().BuildRouteToStation(bus, stationPosition);
        }

        public BusRouteStep[] BuildRouteFromStation(BusView bus)
        {
            return GetVehicleTraffic().BuildRouteFromStation(bus);
        }

        public Vector3 GetStationCounterPosition(int slotIndex)
        {
            var slotPosition = slotIndex == VipStationSlotIndex
                ? BoardLayoutConfig.GetFreeStationPosition()
                : BoardLayoutConfig.GetStationPosition(slotIndex);
            return slotPosition -
                BoardLayoutConfig.StationForward * (BoardLayoutConfig.StationSlotDepth * 0.5f + BoardLayoutConfig.StationCounterBelowSlotOffset) +
                Vector3.up * BoardLayoutConfig.StationCounterY;
        }

        public bool TryGetFirstLockedStationSlotPosition(out Vector3 position)
        {
            if (!CanUnlockStationSlot)
            {
                position = Vector3.zero;
                return false;
            }

            position = GetLockedStationPosition(0);
            return true;
        }

        public bool TryGetLastActiveStationSlotPosition(out Vector3 position)
        {
            if (stationSlots.Capacity <= 0)
            {
                position = Vector3.zero;
                return false;
            }

            position = BoardLayoutConfig.GetStationPosition(stationSlots.Capacity - 1);
            return true;
        }

        public void SetTutorialStationUnlockHighlight(bool highlighted)
        {
            if (!highlighted || !CanUnlockStationSlot || stationRoot == null)
            {
                StopTutorialStationHighlight();
                return;
            }

            ShowTutorialStationHighlight(GetLockedStationPosition(0), 0f);
        }

        public void PulseTutorialStationSlot(Vector3 position)
        {
            ShowTutorialStationHighlight(position, 1.45f);
        }

        public Vector3 GetWorldDirection(BusView bus)
        {
            return bus != null ? bus.VehicleForwardWorld : Vector3.forward;
        }

        private void ResetStationSlots()
        {
            vipStationSlotOccupied = false;
            stationSlots.Reset();
        }

        private void RebuildStationSlots()
        {
            if (stationRoot == null)
            {
                return;
            }

            StopTutorialStationHighlight();
            for (var index = stationRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(stationRoot.GetChild(index).gameObject);
            }

            tutorialStationHighlight = null;
            CreateStationSlots();
        }

        private void ClearBoard()
        {
            StopTutorialStationHighlight();
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }

            tutorialStationHighlight = null;
        }

        private void ShowTutorialStationHighlight(Vector3 position, float duration)
        {
            if (stationRoot == null)
            {
                return;
            }

            if (tutorialStationHighlight == null)
            {
                var material = PuzzlePalette.CreateTransparentMaterial("Tutorial Station Slot Highlight", new Color(1.00f, 0.86f, 0.16f, 0.46f));
                tutorialStationHighlight = BoardGeometry.CreateFlatRoundedRect(
                    "Tutorial Station Slot Highlight",
                    stationRoot,
                    Vector3.zero,
                    new Vector2(BoardLayoutConfig.StationSlotWidth + BoardLayoutConfig.CellSize * 0.08f, BoardLayoutConfig.StationSlotDepth + BoardLayoutConfig.CellSize * 0.08f),
                    BoardLayoutConfig.StationSlotWidth * 0.18f,
                    material,
                    BoardLayoutConfig.StationRotation);
            }

            tutorialStationHighlight.transform.position = position + Vector3.up * 0.036f;
            tutorialStationHighlight.transform.rotation = BoardLayoutConfig.StationRotation;
            tutorialStationHighlight.transform.localScale = Vector3.one;
            tutorialStationHighlight.SetActive(true);

            if (tutorialStationHighlightRoutine != null)
            {
                StopCoroutine(tutorialStationHighlightRoutine);
            }

            tutorialStationHighlightRoutine = StartCoroutine(TutorialStationHighlightRoutine(duration));
        }

        private void StopTutorialStationHighlight()
        {
            if (tutorialStationHighlightRoutine != null)
            {
                StopCoroutine(tutorialStationHighlightRoutine);
                tutorialStationHighlightRoutine = null;
            }

            if (tutorialStationHighlight != null)
            {
                tutorialStationHighlight.SetActive(false);
                tutorialStationHighlight.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator TutorialStationHighlightRoutine(float duration)
        {
            var elapsed = 0f;
            while (duration <= 0f || elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (tutorialStationHighlight != null)
                {
                    var pulse = 1f + Mathf.Sin(Time.time * 6.4f) * 0.055f;
                    tutorialStationHighlight.transform.localScale = new Vector3(pulse, 1f, pulse);
                    tutorialStationHighlight.SetActive(Mathf.PingPong(Time.time * 5.0f, 1f) > 0.16f);
                }

                yield return null;
            }

            if (tutorialStationHighlight != null)
            {
                tutorialStationHighlight.SetActive(false);
                tutorialStationHighlight.transform.localScale = Vector3.one;
            }

            tutorialStationHighlightRoutine = null;
        }

        private void CreateRoots()
        {
            passengerRoot = new GameObject("Passenger Units").transform;
            passengerRoot.SetParent(transform, false);

            busRoot = new GameObject("Buses").transform;
            busRoot.SetParent(transform, false);

            garageRoot = new GameObject("Garages").transform;
            garageRoot.SetParent(transform, false);

            stationRoot = new GameObject("Station Slots").transform;
            stationRoot.SetParent(transform, false);

            themeRoot = new GameObject("Theme Decorations").transform;
            themeRoot.SetParent(transform, false);
        }

        private BusView CreateBusView(BusDefinition definition)
        {
            var bus = BusView.Create(definition, busRoot, BoardLayoutConfig.CellSize);
            bus.SetGridPosition(definition.GridPosition, BoardLayoutConfig.GridToWorld(definition.GridPosition, definition.PositionOffsetCells));
            return bus;
        }

        private RotaryRoadBuildSettings CreateRotaryRoadBuildSettings()
        {
            return new RotaryRoadBuildSettings(
                rotaryCenterZ,
                BoardLayoutConfig.StationZ,
                BoardLayoutConfig.GridWorldWidth,
                BoardLayoutConfig.ParkingYardWorldDepth,
                BoardLayoutConfig.ParkingYardCenterZ,
                rotaryLayout.PassengerPivotOffset);
        }

        private static VehicleTrafficSettings CreateVehicleTrafficSettings()
        {
            return new VehicleTrafficSettings(
                BoardLayoutConfig.CellSize,
                BoardLayoutConfig.GridWorldWidth,
                BoardLayoutConfig.ParkingYardWorldDepth,
                BoardLayoutConfig.ParkingYardTopZ,
                BoardLayoutConfig.GridBottomZ,
                BoardLayoutConfig.GridLeftX,
                BoardLayoutConfig.GridRightX,
                BoardLayoutConfig.StationRotation,
                BoardLayoutConfig.StationForward,
                BoardLayoutConfig.StationRight);
        }

        private PassengerTrafficSettings CreatePassengerTrafficSettings()
        {
            return new PassengerTrafficSettings(
                rotaryCenterZ,
                PassengerUnitLayout.UnitY,
                FeederMergeDuration,
                FeederQueueStepDuration,
                PassengerUnitLayout.FeederVacancyWindowDistance,
                BoardingGateProgressWindow,
                BoardingReservationProgressWindow,
                PassengerUnitLayout.PersonLocalZOffsets);
        }

        private static RoadPresetDefinition GetRoadPresetForStage(LevelData levelData)
        {
            return levelData.RoadPreset;
        }

        private static int GetRotaryUnitCapacityForStage(LevelData levelData, RoadPresetDefinition roadPreset)
        {
            var capacity = levelData.RotaryStartCapacity;
            var shapeMinimumCapacity = GetShapeTestMinimumCapacityUnits(roadPreset.Id);
            if (shapeMinimumCapacity > 0)
            {
                capacity = Mathf.Max(capacity, shapeMinimumCapacity);
            }

            return Mathf.Clamp(capacity, LevelData.MinRotaryUnitCapacity, roadPreset.MaxCapacityUnits);
        }

        private static float GetRotaryUnitSpacingForStage(
            LevelData levelData,
            RoadPresetDefinition roadPreset,
            int rotaryUnitCapacity,
            int stageNumber)
        {
            var pressure = GetRotarySizePressure(levelData, rotaryUnitCapacity, stageNumber);
            return PassengerUnitLayout.RotaryUnitSpacing * GetShapeTestPathScale(roadPreset.Id, pressure);
        }

        private static float GetRoadScaleForStage(
            LevelData levelData,
            RoadPresetDefinition roadPreset,
            int rotaryUnitCapacity,
            int stageNumber)
        {
            var pressure = GetRotarySizePressure(levelData, rotaryUnitCapacity, stageNumber);
            return GetShapeTestRoadScale(roadPreset.Id, pressure);
        }

        private static int GetShapeTestMinimumCapacityUnits(RotaryRoadPresetId presetId)
        {
            switch (presetId)
            {
                case RotaryRoadPresetId.SmallCircleTest:
                    return 24;
                case RotaryRoadPresetId.DropTest:
                    return 26;
                case RotaryRoadPresetId.RoundedSquareTest:
                case RotaryRoadPresetId.OvalTest:
                    return 28;
                case RotaryRoadPresetId.ArrowTest:
                    return 30;
                case RotaryRoadPresetId.LargeCircleTest:
                    return 32;
                case RotaryRoadPresetId.HeartTest:
                case RotaryRoadPresetId.CloverTest:
                case RotaryRoadPresetId.CloudTest:
                case RotaryRoadPresetId.LoopTest:
                case RotaryRoadPresetId.RibbonTest:
                case RotaryRoadPresetId.SnakeTest:
                    return 36;
                default:
                    return 0;
            }
        }

        private static int GetShapeTestVisibleUnits(RotaryRoadPresetId presetId)
        {
            switch (presetId)
            {
                case RotaryRoadPresetId.SmallCircleTest:
                    return 20;
                case RotaryRoadPresetId.RoundedSquareTest:
                case RotaryRoadPresetId.DropTest:
                case RotaryRoadPresetId.ArrowTest:
                    return 26;
                case RotaryRoadPresetId.LargeCircleTest:
                case RotaryRoadPresetId.OvalTest:
                case RotaryRoadPresetId.HeartTest:
                    return 28;
                case RotaryRoadPresetId.CloverTest:
                case RotaryRoadPresetId.CloudTest:
                case RotaryRoadPresetId.LoopTest:
                case RotaryRoadPresetId.RibbonTest:
                    return 30;
                default:
                    return 0;
            }
        }

        private static float GetRotarySizePressure(LevelData levelData, int rotaryUnitCapacity, int stageNumber)
        {
            var capacityPressure = Mathf.InverseLerp(20f, LevelData.MaxRotaryUnitCapacity, rotaryUnitCapacity);
            var passengerPressure = Mathf.InverseLerp(20f, 48f, levelData != null ? levelData.PassengerUnitCount : rotaryUnitCapacity);
            var stagePressure = stageNumber > 0 ? Mathf.InverseLerp(1f, 50f, stageNumber) : capacityPressure;
            var difficultyPressure = GetDifficultySizePressure(levelData != null ? levelData.DifficultyProfile.Difficulty : LevelDifficulty.Normal);
            return Mathf.Clamp01(capacityPressure * 0.58f + passengerPressure * 0.12f + difficultyPressure * 0.20f + stagePressure * 0.10f);
        }

        private static float GetDifficultySizePressure(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.SuperHard:
                    return 0.90f;
                case LevelDifficulty.Hard:
                    return 0.45f;
                default:
                    return 0f;
            }
        }

        private static float GetShapeTestPathScale(RotaryRoadPresetId presetId, float pressure)
        {
            pressure = Mathf.Clamp01(pressure);
            switch (presetId)
            {
                case RotaryRoadPresetId.SmallCircleTest:
                    return Mathf.Lerp(0.98f, 1.08f, pressure);
                case RotaryRoadPresetId.LargeCircleTest:
                    return Mathf.Lerp(1.14f, 1.30f, pressure);
                case RotaryRoadPresetId.OvalTest:
                    return Mathf.Lerp(1.04f, 1.25f, pressure);
                case RotaryRoadPresetId.RoundedSquareTest:
                    return Mathf.Lerp(1.04f, 1.20f, pressure);
                case RotaryRoadPresetId.DropTest:
                    return Mathf.Lerp(1.02f, 1.22f, pressure);
                case RotaryRoadPresetId.ArrowTest:
                    return Mathf.Lerp(1.12f, 1.20f, pressure);
                case RotaryRoadPresetId.HeartTest:
                    return Mathf.Lerp(1.24f, 1.30f, pressure);
                case RotaryRoadPresetId.CloverTest:
                case RotaryRoadPresetId.CloudTest:
                case RotaryRoadPresetId.LoopTest:
                case RotaryRoadPresetId.RibbonTest:
                    return Mathf.Lerp(1.20f, 1.25f, pressure);
                default:
                    return 1f;
            }
        }

        private static float GetShapeTestRoadScale(RotaryRoadPresetId presetId, float pressure)
        {
            pressure = Mathf.Clamp01(pressure);
            switch (presetId)
            {
                case RotaryRoadPresetId.SmallCircleTest:
                    return Mathf.Lerp(1.04f, 1.10f, pressure);
                case RotaryRoadPresetId.LargeCircleTest:
                    return Mathf.Lerp(1.12f, 1.20f, pressure);
                case RotaryRoadPresetId.OvalTest:
                case RotaryRoadPresetId.RoundedSquareTest:
                case RotaryRoadPresetId.DropTest:
                    return Mathf.Lerp(1.08f, 1.20f, pressure);
                case RotaryRoadPresetId.ArrowTest:
                    return Mathf.Lerp(1.12f, 1.20f, pressure);
                case RotaryRoadPresetId.HeartTest:
                case RotaryRoadPresetId.LoopTest:
                    return Mathf.Lerp(1.16f, 1.20f, pressure);
                case RotaryRoadPresetId.CloverTest:
                case RotaryRoadPresetId.CloudTest:
                case RotaryRoadPresetId.RibbonTest:
                    return Mathf.Lerp(1.18f, 1.22f, pressure);
                default:
                    return 1f;
            }
        }

        private static float CalculateRotaryCenterZ(RotaryLayout layout)
        {
            var upperRoadZ = BoardLayoutConfig.StationZ + BoardLayoutConfig.CellSize * StationUpperRoadForwardCells;
            var desiredRotaryBottomZ = upperRoadZ + BoardLayoutConfig.CellSize * RotaryStationLaneClearanceCells;
            var visualRadiusZ = layout.OuterRadiusZ + BoardLayoutConfig.CellSize * RotaryVisualGuardrailPaddingCells;
            return Mathf.Max(BoardLayoutConfig.RotaryCenterZ, desiredRotaryBottomZ + visualRadiusZ);
        }

        private VehicleTrafficEngine GetVehicleTraffic()
        {
            if (vehicleTraffic == null)
            {
                vehicleTraffic = new VehicleTrafficEngine(CreateVehicleTrafficSettings());
            }

            return vehicleTraffic;
        }

        private PassengerTrafficEngine GetPassengerTraffic()
        {
            if (passengerTraffic == null)
            {
                passengerTraffic = new PassengerTrafficEngine(rotaryLayout, CreatePassengerTrafficSettings(), rotaryActiveTarget);
            }

            return passengerTraffic;
        }

        private void CreateGround()
        {
            RotaryRoadBuilder.CreateGround(transform, rotaryLayout, CreateRotaryRoadBuildSettings());
        }

        private void CreateTheme()
        {
            CityTerminalThemeBuilder.Create(themeRoot, rotaryLayout, CreateRotaryRoadBuildSettings());
        }

        private void CreatePassengerRotary()
        {
            RotaryRoadBuilder.CreatePassengerRotary(transform, rotaryLayout, CreateRotaryRoadBuildSettings());
        }

        private void CreateGrid()
        {
            ParkingGridBuilder.Create(
                transform,
                BoardLayoutConfig.GridColumns,
                BoardLayoutConfig.GridRows,
                BoardLayoutConfig.CellSize,
                BoardLayoutConfig.GridBottomZ,
                BoardLayoutConfig.UpperParkingExtensionZ);
        }

        private void CreateStationSlots()
        {
            StationSlotBuilder.Create(
                stationRoot,
                BoardLayoutConfig.FreeStationSlots,
                stationSlots.Capacity,
                stationSlots.LockedSlots,
                BoardLayoutConfig.StationZ,
                BoardLayoutConfig.StationSlotSpacing,
                BoardLayoutConfig.StationSlotWidth,
                BoardLayoutConfig.StationSlotDepth,
                BoardLayoutConfig.CellSize,
                BoardLayoutConfig.StationRotation,
                BoardLayoutConfig.GetFreeStationPosition,
                BoardLayoutConfig.GetStationPosition,
                GetLockedStationPosition);
        }

        private static float GetMaxAbsFeederX(params FeederRoadPath[] paths)
        {
            var max = 0f;
            for (var pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                var path = paths[pathIndex];
                if (path == null)
                {
                    continue;
                }

                for (var pointIndex = 0; pointIndex < path.Points.Length; pointIndex++)
                {
                    max = Mathf.Max(max, Mathf.Abs(path.Points[pointIndex].x));
                }
            }

            return max;
        }

        private static float GetMaxFeederY(params FeederRoadPath[] paths)
        {
            var max = 0f;
            for (var pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                var path = paths[pathIndex];
                if (path == null)
                {
                    continue;
                }

                for (var pointIndex = 0; pointIndex < path.Points.Length; pointIndex++)
                {
                    max = Mathf.Max(max, path.Points[pointIndex].y);
                }
            }

            return max;
        }

        private Vector3 GetLockedStationPosition(int lockedSlotIndex)
        {
            return BoardLayoutConfig.GetStationPositionByTotalIndex(
                BoardLayoutConfig.FreeStationSlots + stationSlots.Capacity + lockedSlotIndex);
        }

        private static void WarnIfBusesStartOverlapping(LevelData levelData, IReadOnlyList<BusView> buses)
        {
            for (var firstIndex = 0; firstIndex < buses.Count; firstIndex++)
            {
                var firstBus = buses[firstIndex];
                if (firstBus == null)
                {
                    continue;
                }

                for (var secondIndex = firstIndex + 1; secondIndex < buses.Count; secondIndex++)
                {
                    var secondBus = buses[secondIndex];
                    if (secondBus == null || !firstBus.CurrentFootprint.Overlaps(secondBus.CurrentFootprint))
                    {
                        continue;
                    }

                    Debug.LogWarning(
                        $"{levelData.LevelName}: {PuzzlePalette.DisplayName(firstBus.Color)} {BusSizeUtility.DisplayName(firstBus.Size)} overlaps " +
                        $"{PuzzlePalette.DisplayName(secondBus.Color)} {BusSizeUtility.DisplayName(secondBus.Size)} at start. " +
                        "Tune positionOffsetCells or angleOffsetDegrees.");
                }
            }
        }

        private int GetStartingRotaryUnitCount(int passengerUnitCount, RoadPresetDefinition roadPreset)
        {
            var maxVisibleUnits = rotaryLayout.CapacityUnits;
            var shapeVisibleUnits = GetShapeTestVisibleUnits(roadPreset.Id);
            if (shapeVisibleUnits > 0)
            {
                maxVisibleUnits = Mathf.Min(maxVisibleUnits, shapeVisibleUnits);
            }

            return Mathf.Clamp(passengerUnitCount, 0, maxVisibleUnits);
        }

    }
}
