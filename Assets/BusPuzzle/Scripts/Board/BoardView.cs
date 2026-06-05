using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BoardView : MonoBehaviour
    {
        private const float BoardingGateProgressWindow = 0.070f;
        private const float BoardingReservationProgressWindow = 0.180f;
        private const float PassengerVisualScale = 1.20f;
        private const float PassengerInnerPersonLocalZ = -0.155f * PassengerVisualScale;
        private const float PassengerSecondPersonLocalZ = -0.052f * PassengerVisualScale;
        private const float PassengerThirdPersonLocalZ = 0.052f * PassengerVisualScale;
        private const float PassengerOuterPersonLocalZ = 0.155f * PassengerVisualScale;
        private const float PassengerPersonRadius = 0.065f * PassengerVisualScale;
        private const float PassengerInnerRailOverlap = 0.110f * PassengerVisualScale;
        private const float PassengerOuterRoadClearance = 0.008f * PassengerVisualScale;
        private const float PassengerTangentialSlotSpacing = 0.120f;
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

        private readonly StationSlotController stationSlots = new StationSlotController(BoardLayoutConfig.ActiveStationSlots);

        private RotaryLayout rotaryLayout;
        private VehicleTrafficEngine vehicleTraffic;
        private PassengerTrafficEngine passengerTraffic;
        private int rotaryActiveTarget;
        private Transform passengerRoot;
        private Transform busRoot;
        private Transform stationRoot;

        public int StationCapacity => stationSlots.Capacity;

        public int OccupiedStationSlots => stationSlots.OccupiedSlots;

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
            CreateRoots();
            CreateGround();
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

            for (var index = 0; index < levelData.Buses.Count; index++)
            {
                var definition = levelData.Buses[index];
                var bus = BusView.Create(definition, busRoot, BoardLayoutConfig.CellSize);
                bus.SetGridPosition(definition.GridPosition, BoardLayoutConfig.GridToWorld(definition.GridPosition, definition.PositionOffsetCells));
                buses.Add(bus);
            }

            WarnIfBusesStartOverlapping(levelData, buses);
        }

        public void UpdatePassengerTraffic(IReadOnlyList<PassengerView> passengers, float deltaTime)
        {
            GetPassengerTraffic().Advance(passengers, deltaTime);
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

        public bool HasPassengerColor(IReadOnlyList<PassengerView> passengers, PuzzleColor color)
        {
            return PassengerTrafficEngine.HasPassengerColor(passengers, color);
        }

        public bool TryReserveStationSlot(out int slotIndex, out Vector3 slotPosition)
        {
            return stationSlots.TryReserve(BoardLayoutConfig.GetStationPosition, out slotIndex, out slotPosition);
        }

        public void ReleaseStationSlot(int slotIndex)
        {
            stationSlots.Release(slotIndex);
        }

        public bool IsAnyMoveAvailable(IReadOnlyList<BusView> buses)
        {
            return GetVehicleTraffic().IsAnyMoveAvailable(buses, OccupiedStationSlots, StationCapacity);
        }

        public bool IsPathClear(BusView movingBus, IReadOnlyList<BusView> buses, out BusView blockingBus)
        {
            return IsPathClear(movingBus, buses, out blockingBus, out _);
        }

        public bool IsPathClear(BusView movingBus, IReadOnlyList<BusView> buses, out BusView blockingBus, out Vector3 collisionPosition)
        {
            return GetVehicleTraffic().IsPathClear(movingBus, buses, out blockingBus, out collisionPosition);
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
            var slotPosition = BoardLayoutConfig.GetStationPosition(slotIndex);
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
            stationSlots.Reset();
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

            stationRoot = new GameObject("Station Slots").transform;
            stationRoot.SetParent(transform, false);
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
                BoardLayoutConfig.ActiveStationSlots,
                BoardLayoutConfig.LockedStationSlots,
                BoardLayoutConfig.StationZ,
                BoardLayoutConfig.StationSlotSpacing,
                BoardLayoutConfig.StationSlotWidth,
                BoardLayoutConfig.StationSlotDepth,
                BoardLayoutConfig.CellSize,
                BoardLayoutConfig.StationRotation,
                BoardLayoutConfig.GetFreeStationPosition,
                BoardLayoutConfig.GetStationPosition,
                BoardLayoutConfig.GetLockedStationPosition);
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
