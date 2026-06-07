using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BoardView : MonoBehaviour
    {
        public const int VipStationSlotIndex = -1;

        private const float BoardingGateProgressWindow = 0.070f;
        private const float BoardingReservationProgressWindow = 0.180f;
        private const float PassengerVisualScale = 1.26f;
        private const float PassengerInnerPersonLocalZ = -0.155f * PassengerVisualScale;
        private const float PassengerSecondPersonLocalZ = -0.052f * PassengerVisualScale;
        private const float PassengerThirdPersonLocalZ = 0.052f * PassengerVisualScale;
        private const float PassengerOuterPersonLocalZ = 0.155f * PassengerVisualScale;
        private const float PassengerPersonRadius = 0.065f * PassengerVisualScale;
        private const float PassengerInnerRailOverlap = 0.110f * PassengerVisualScale;
        private const float PassengerOuterRoadClearance = 0.008f * PassengerVisualScale;
        private const float PassengerTangentialSlotSpacing = 0.126f;
        private const float PassengerUnitY = 0.08f;
        private const float FeederMergeDuration = 0.34f;
        private const float FeederQueueStepDuration = 0.20f;
        private const float FeederVacancyWindowDistance = PassengerTangentialSlotSpacing * 0.62f;
        private const float PassengerSetRoadWidth =
            PassengerOuterPersonLocalZ - PassengerInnerPersonLocalZ +
            PassengerPersonRadius * 2f -
            PassengerInnerRailOverlap +
            PassengerOuterRoadClearance;
        private const float PassengerSetPivotOffset =
            PassengerPersonRadius - PassengerInnerPersonLocalZ - PassengerInnerRailOverlap;

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
            var halfFeederWidth = GetMaxAbsFeederX(rotaryLayout.LeftFeederPath, rotaryLayout.RightFeederPath) +
                rotaryLayout.RoadWidth +
                0.18f;
            var halfWidth = Mathf.Max(halfGridWidth, halfStationWidth, halfFeederWidth);
            var bottomZ = BoardLayoutConfig.GridBottomZ - BoardLayoutConfig.CellSize * 0.48f;
            var topZ = BoardLayoutConfig.RotaryCenterZ +
                rotaryLayout.VisibleFeederTopY +
                0.20f;

            var center = new Vector3(0f, 0.10f, (bottomZ + topZ) * 0.5f);
            var size = new Vector3(halfWidth * 2f, 0.36f, topZ - bottomZ);
            return new Bounds(center, size);
        }

        public void BuildLevel(LevelData levelData, List<PassengerView> passengers, List<BusView> buses)
        {
            rotaryLayout = RotaryLayout.Create(
                levelData.RoadPreset,
                levelData.RotaryStartCapacity,
                PassengerTangentialSlotSpacing,
                PassengerSetRoadWidth,
                PassengerSetPivotOffset,
                new Vector4(
                    PassengerInnerPersonLocalZ,
                    PassengerSecondPersonLocalZ,
                    PassengerThirdPersonLocalZ,
                    PassengerOuterPersonLocalZ));
            rotaryActiveTarget = GetStartingRotaryUnitCount(levelData.PassengerUnits.Count);
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

            for (var index = stationRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(stationRoot.GetChild(index).gameObject);
            }

            CreateStationSlots();
        }

        private void ClearBoard()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
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

        private static RotaryRoadBuildSettings CreateRotaryRoadBuildSettings()
        {
            return new RotaryRoadBuildSettings(
                BoardLayoutConfig.RotaryCenterZ,
                BoardLayoutConfig.StationZ,
                BoardLayoutConfig.GridWorldWidth,
                BoardLayoutConfig.GridWorldDepth,
                BoardLayoutConfig.GridCenterZ,
                PassengerSetPivotOffset);
        }

        private static VehicleTrafficSettings CreateVehicleTrafficSettings()
        {
            return new VehicleTrafficSettings(
                BoardLayoutConfig.CellSize,
                BoardLayoutConfig.GridWorldWidth,
                BoardLayoutConfig.GridWorldDepth,
                BoardLayoutConfig.GridTopZ,
                BoardLayoutConfig.GridBottomZ,
                BoardLayoutConfig.GridLeftX,
                BoardLayoutConfig.GridRightX,
                BoardLayoutConfig.StationRotation,
                BoardLayoutConfig.StationForward,
                BoardLayoutConfig.StationRight);
        }

        private static PassengerTrafficSettings CreatePassengerTrafficSettings()
        {
            return new PassengerTrafficSettings(
                BoardLayoutConfig.RotaryCenterZ,
                PassengerUnitY,
                FeederMergeDuration,
                FeederQueueStepDuration,
                FeederVacancyWindowDistance,
                BoardingGateProgressWindow,
                BoardingReservationProgressWindow,
                new Vector4(
                    PassengerInnerPersonLocalZ,
                    PassengerSecondPersonLocalZ,
                    PassengerThirdPersonLocalZ,
                    PassengerOuterPersonLocalZ));
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
                BoardLayoutConfig.GridBottomZ);
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

        private int GetStartingRotaryUnitCount(int passengerUnitCount)
        {
            return Mathf.Clamp(passengerUnitCount, 0, rotaryLayout.CapacityUnits);
        }

    }
}
