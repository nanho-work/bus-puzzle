using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BoardView : MonoBehaviour
    {
        private const int GridColumns = 14;
        private const int GridRows = 14;
        private const int FreeStationSlots = 1;
        private const int ActiveStationSlots = 4;
        private const int LockedStationSlots = 4;
        private const float CellSize = 0.31f;
        private const float GridBottomZ = -4.17f;
        private const float StationZ = 0.66f;
        private const float StationSlotSpacing = 0.44f;
        private const float StationSlotWidth = 0.29f;
        private const float StationSlotDepth = 0.62f;
        private const float RotaryCenterZ = 2.42f;
        private const float BoardingGateProgressWindow = 0.070f;
        private const float PassengerVisualScale = 1.20f;
        private const float PassengerInnerPersonLocalZ = -0.20f * PassengerVisualScale;
        private const float PassengerOuterPersonLocalZ = 0.20f * PassengerVisualScale;
        private const float PassengerPersonRadius = 0.065f * PassengerVisualScale;
        private const float PassengerInnerRailOverlap = 0.095f * PassengerVisualScale;
        private const float PassengerOuterRoadClearance = 0.020f * PassengerVisualScale;
        private const float PassengerTangentialSlotSpacing = 0.142f;
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

        private readonly bool[] stationOccupied = new bool[ActiveStationSlots];
        private readonly PassengerFlowController passengerFlow = new PassengerFlowController();

        private RotaryLayout rotaryLayout;
        private int rotaryActiveTarget;
        private Transform passengerRoot;
        private Transform busRoot;
        private Transform stationRoot;

        public int StationCapacity => ActiveStationSlots;

        public int OccupiedStationSlots
        {
            get
            {
                var count = 0;
                for (var index = 0; index < stationOccupied.Length; index++)
                {
                    if (stationOccupied[index])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void BuildLevel(LevelData levelData, List<PassengerView> passengers, List<BusView> buses)
        {
            rotaryLayout = RotaryLayout.Create(
                levelData.RoadPreset,
                levelData.RotaryStartCapacity,
                PassengerTangentialSlotSpacing,
                PassengerSetRoadWidth,
                PassengerSetPivotOffset,
                PassengerSetPivotOffset + PassengerOuterPersonLocalZ);
            passengerFlow.Configure(rotaryLayout);
            rotaryActiveTarget = GetStartingRotaryUnitCount(levelData.PassengerUnits.Count);

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
                if (index < rotaryActiveTarget)
                {
                    AssignPassengerTraffic(passenger, index);
                    SetPassengerTrafficPose(passenger);
                }
                else
                {
                    AssignPassengerFeeder(passenger, index - rotaryActiveTarget);
                    SetPassengerFeederPose(passenger);
                }

                passengers.Add(passenger);
            }

            for (var index = 0; index < levelData.Buses.Count; index++)
            {
                var definition = levelData.Buses[index];
                var bus = BusView.Create(definition, busRoot, CellSize);
                bus.SetGridPosition(definition.GridPosition, GridToWorld(definition.GridPosition));
                buses.Add(bus);
            }
        }

        public void UpdatePassengerTraffic(IReadOnlyList<PassengerView> passengers, float deltaTime)
        {
            passengerFlow.Advance(passengers, deltaTime);

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.CanCirculate)
                {
                    continue;
                }

                SetPassengerTrafficPose(passenger);
            }

            PromoteFeederPassengers(passengers);
        }

        public bool TryFindBoardingPassenger(IReadOnlyList<PassengerView> passengers, PuzzleColor color, out int passengerIndex)
        {
            var bestDistance = float.MaxValue;
            passengerIndex = -1;

            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.CanCirculate || passenger.Color != color || !IsPassengerAtBoardingGate(passenger))
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(passenger.transform.position - GetBoardingGatePosition(passenger.LaneOffset));
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                passengerIndex = index;
            }

            return passengerIndex >= 0;
        }

        public bool HasPassengerColor(IReadOnlyList<PassengerView> passengers, PuzzleColor color)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger.Color == color && passenger.gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryReserveStationSlot(out int slotIndex, out Vector3 slotPosition)
        {
            for (var index = 0; index < stationOccupied.Length; index++)
            {
                if (stationOccupied[index])
                {
                    continue;
                }

                stationOccupied[index] = true;
                slotIndex = index;
                slotPosition = GetStationPosition(index);
                return true;
            }

            slotIndex = -1;
            slotPosition = Vector3.zero;
            return false;
        }

        public void ReleaseStationSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= stationOccupied.Length)
            {
                return;
            }

            stationOccupied[slotIndex] = false;
        }

        public bool IsAnyMoveAvailable(IReadOnlyList<BusView> buses)
        {
            if (OccupiedStationSlots >= StationCapacity)
            {
                return false;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus.IsOnBoard && !bus.IsMoving && IsPathClear(bus, buses, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPathClear(BusView movingBus, IReadOnlyList<BusView> buses, out BusView blockingBus)
        {
            blockingBus = null;

            var step = GridDirectionUtility.ToGridVector(movingBus.Direction);
            var cell = movingBus.FrontCell + step;

            while (IsInsideBoard(cell))
            {
                for (var index = 0; index < buses.Count; index++)
                {
                    var bus = buses[index];
                    if (bus == movingBus || !bus.IsOnBoard || bus.IsDeparted)
                    {
                        continue;
                    }

                    if (bus.OccupiesCell(cell))
                    {
                        blockingBus = bus;
                        return false;
                    }
                }

                cell += step;
            }

            return true;
        }

        public BusRouteStep[] BuildRouteToStation(BusView bus, Vector3 stationPosition)
        {
            var step = GridDirectionUtility.ToGridVector(bus.Direction);
            var exitCell = bus.FrontCell + step;

            while (IsInsideBoard(exitCell))
            {
                exitCell += step;
            }

            var exitPosition = GridToWorld(exitCell);
            exitPosition.y = bus.transform.position.y;
            stationPosition.y = bus.transform.position.y;

            var topRoadZ = GridTopZ + CellSize * 0.95f;
            var leftRoadX = GridLeftX - CellSize * 0.95f;
            var rightRoadX = GridRightX + CellSize * 0.95f;
            var route = new List<BusRouteStep>();
            var currentPosition = exitPosition;

            AddRouteStep(route, currentPosition, GridDirectionUtility.ToRotation(bus.Direction));

            if (bus.Direction == GridDirection.Down)
            {
                var sideX = Mathf.Abs(exitPosition.x - leftRoadX) <= Mathf.Abs(exitPosition.x - rightRoadX)
                    ? leftRoadX
                    : rightRoadX;
                var horizontalDirection = sideX < exitPosition.x ? GridDirection.Left : GridDirection.Right;
                currentPosition = new Vector3(sideX, exitPosition.y, exitPosition.z);
                AddRouteStep(route, currentPosition, GridDirectionUtility.ToRotation(horizontalDirection));
            }

            if (Mathf.Abs(currentPosition.z - topRoadZ) > 0.01f)
            {
                AddRouteStep(route, currentPosition, GridDirectionUtility.ToRotation(GridDirection.Up));
                currentPosition = new Vector3(currentPosition.x, currentPosition.y, topRoadZ);
                AddRouteStep(route, currentPosition, GridDirectionUtility.ToRotation(GridDirection.Up));
            }

            if (Mathf.Abs(currentPosition.x - stationPosition.x) > 0.01f)
            {
                var horizontalDirection = stationPosition.x > currentPosition.x ? GridDirection.Right : GridDirection.Left;
                AddRouteStep(route, currentPosition, GridDirectionUtility.ToRotation(horizontalDirection));
                currentPosition = new Vector3(stationPosition.x, currentPosition.y, topRoadZ);
                AddRouteStep(route, currentPosition, GridDirectionUtility.ToRotation(horizontalDirection));
            }

            var stationRootPosition = bus.GetRootPositionForVisualCenter(stationPosition, GridDirection.Up);
            AddRouteStep(route, currentPosition, GridDirectionUtility.ToRotation(GridDirection.Up));
            AddRouteStep(route, stationRootPosition, GridDirectionUtility.ToRotation(GridDirection.Up));
            return route.ToArray();
        }

        public Vector3 GetWorldDirection(BusView bus)
        {
            return GridDirectionUtility.ToWorldVector(bus.Direction);
        }

        private void ResetStationSlots()
        {
            for (var index = 0; index < stationOccupied.Length; index++)
            {
                stationOccupied[index] = false;
            }
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
                RotaryCenterZ,
                StationZ,
                GridWorldWidth,
                GridWorldDepth,
                GridCenterZ,
                PassengerSetPivotOffset);
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
            ParkingGridBuilder.Create(transform, GridColumns, GridRows, CellSize, GridBottomZ);
        }

        private void CreateStationSlots()
        {
            StationSlotBuilder.Create(
                stationRoot,
                FreeStationSlots,
                ActiveStationSlots,
                LockedStationSlots,
                StationZ,
                StationSlotSpacing,
                StationSlotWidth,
                StationSlotDepth,
                CellSize,
                GetFreeStationPosition,
                GetStationPosition,
                GetLockedStationPosition);
        }

        private static bool IsInsideBoard(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < GridColumns && cell.y >= 0 && cell.y < GridRows;
        }

        private static Vector3 GridToWorld(Vector2Int cell)
        {
            var x = (cell.x - (GridColumns - 1) * 0.5f) * CellSize;
            var z = GridBottomZ + cell.y * CellSize;
            return new Vector3(x, 0f, z);
        }

        private static void AddRouteStep(List<BusRouteStep> route, Vector3 position, Quaternion rotation)
        {
            route.Add(new BusRouteStep(position, rotation));
        }

        private static Vector3 GetStationPosition(int index)
        {
            var totalIndex = FreeStationSlots + index;
            var x = (totalIndex - (TotalStationSlots - 1) * 0.5f) * StationSlotSpacing;
            return new Vector3(x, 0f, StationZ);
        }

        private static Vector3 GetFreeStationPosition()
        {
            return new Vector3(-(TotalStationSlots - 1) * 0.5f * StationSlotSpacing, 0f, StationZ);
        }

        private static Vector3 GetLockedStationPosition(int index)
        {
            var totalIndex = FreeStationSlots + ActiveStationSlots + index;
            var x = (totalIndex - (TotalStationSlots - 1) * 0.5f) * StationSlotSpacing;
            return new Vector3(x, 0f, StationZ);
        }

        private static int TotalStationSlots => FreeStationSlots + ActiveStationSlots + LockedStationSlots;
        private static float GridWorldWidth => GridColumns * CellSize;
        private static float GridWorldDepth => GridRows * CellSize;
        private static float GridCenterZ => GridBottomZ + (GridRows - 1) * CellSize * 0.5f;
        private static float GridTopZ => GridBottomZ + (GridRows - 1) * CellSize;
        private static float GridLeftX => (0 - (GridColumns - 1) * 0.5f) * CellSize;
        private static float GridRightX => (GridColumns - 1 - (GridColumns - 1) * 0.5f) * CellSize;

        private int GetStartingRotaryUnitCount(int passengerUnitCount)
        {
            return Mathf.Clamp(passengerUnitCount, 0, rotaryLayout.CapacityUnits);
        }

        private void AssignPassengerTraffic(PassengerView passenger, int rotarySlotIndex)
        {
            var clampedSlotIndex = Mathf.Clamp(rotarySlotIndex, 0, rotaryLayout.CapacityUnits - 1);
            passengerFlow.AssignTraffic(passenger, clampedSlotIndex, GetLaneOffset());
        }

        private void AssignPassengerFeeder(PassengerView passenger, int feederQueueIndex)
        {
            var side = feederQueueIndex % 2 == 0 ? -1 : 1;
            var slot = feederQueueIndex / 2;
            passenger.AssignFeeder(side, slot);
        }

        private void PromoteFeederPassengers(IReadOnlyList<PassengerView> passengers)
        {
            var targetCount = Mathf.Min(rotaryActiveTarget, passengers.Count);
            if (CountRotaryAssignedPassengers(passengers) >= targetCount)
            {
                return;
            }

            TryPromoteFeederPassenger(passengers, -1);
            if (CountRotaryAssignedPassengers(passengers) < targetCount)
            {
                TryPromoteFeederPassenger(passengers, 1);
            }
        }

        private bool TryPromoteFeederPassenger(IReadOnlyList<PassengerView> passengers, int side)
        {
            if (!TryFindFeederPassenger(passengers, side, out var passenger))
            {
                return false;
            }

            if (!TryFindOpenRotarySlotAtFeeder(passengers, side, out var slotIndex))
            {
                return false;
            }

            var feederSlotIndex = passenger.FeederSlotIndex;
            var mergePath = BuildFeederMergePath(side, feederSlotIndex, slotIndex);
            AssignPassengerTraffic(passenger, slotIndex);
            passenger.MoveAlongPoses(mergePath, FeederMergeDuration, () => SetPassengerTrafficPose(passenger));
            AdvanceFeederQueue(passengers, side, feederSlotIndex);
            return true;
        }

        private static int CountRotaryAssignedPassengers(IReadOnlyList<PassengerView> passengers)
        {
            var count = 0;
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger.IsAssignedToRotary)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryFindFeederPassenger(IReadOnlyList<PassengerView> passengers, int side, out PassengerView passenger)
        {
            passenger = null;
            var bestSlot = int.MaxValue;

            for (var index = 0; index < passengers.Count; index++)
            {
                var candidate = passengers[index];
                if (!candidate.IsWaitingInFeeder || candidate.FeederSide != side || candidate.FeederSlotIndex >= bestSlot)
                {
                    continue;
                }

                passenger = candidate;
                bestSlot = candidate.FeederSlotIndex;
            }

            return passenger != null;
        }

        private bool TryFindOpenRotarySlotAtFeeder(IReadOnlyList<PassengerView> passengers, int side, out int slotIndex)
        {
            slotIndex = -1;
            var feederDistance = passengerFlow.GetProgressDistance(GetFeederJoinProgress(side));
            var bestDistance = float.MaxValue;

            for (var slot = 0; slot < rotaryLayout.CapacityUnits; slot++)
            {
                if (IsRotarySlotOccupied(passengers, slot))
                {
                    continue;
                }

                var vacancyDistance = passengerFlow.GetSlotDistance(slot);
                var distanceToFeeder = passengerFlow.GetCircularDistance(vacancyDistance, feederDistance);
                if (distanceToFeeder > FeederVacancyWindowDistance || distanceToFeeder >= bestDistance)
                {
                    continue;
                }

                bestDistance = distanceToFeeder;
                slotIndex = slot;
            }

            return slotIndex >= 0;
        }

        private static bool IsRotarySlotOccupied(IReadOnlyList<PassengerView> passengers, int slot)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (passenger.IsAssignedToRotary && passenger.RotarySlotIndex == slot)
                {
                    return true;
                }
            }

            return false;
        }

        private void AdvanceFeederQueue(IReadOnlyList<PassengerView> passengers, int side, int removedSlotIndex)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.IsWaitingInFeeder || passenger.FeederSide != side || passenger.FeederSlotIndex <= removedSlotIndex)
                {
                    continue;
                }

                passenger.AssignFeeder(side, passenger.FeederSlotIndex - 1);
                var pose = GetFeederPose(side, passenger.FeederSlotIndex);
                passenger.MoveToPose(pose.Position, pose.Rotation, FeederQueueStepDuration);
            }
        }

        private void SetPassengerFeederPose(PassengerView passenger)
        {
            var pose = GetFeederPose(passenger.FeederSide, passenger.FeederSlotIndex);
            passenger.SetPose(pose.Position, pose.Rotation);
        }

        private float GetFeederJoinProgress(int side)
        {
            return side < 0 ? rotaryLayout.Preset.LeftFeederProgress : rotaryLayout.Preset.RightFeederProgress;
        }

        private float GetLaneOffset()
        {
            return PassengerSetPivotOffset;
        }

        private PassengerUnitRoadPose GetFeederPose(int side, int slotIndex)
        {
            return rotaryLayout.GetFeederPose(side, slotIndex, RotaryCenterZ, PassengerUnitY);
        }

        private PassengerUnitRoadPose[] BuildFeederMergePath(int side, int feederSlotIndex, int rotarySlotIndex)
        {
            const int PoseCount = 9;
            var poses = new PassengerUnitRoadPose[PoseCount];
            var feederPath = rotaryLayout.GetFeederPath(side);
            var startDistance = rotaryLayout.GetFeederDistanceForSlot(side, feederSlotIndex);

            for (var index = 0; index < PoseCount - 1; index++)
            {
                var t = index / (PoseCount - 2f);
                var distance = Mathf.Lerp(startDistance, feederPath.Length, t);
                poses[index] = rotaryLayout.GetFeederPoseByDistance(side, distance, RotaryCenterZ, PassengerUnitY);
            }

            poses[PoseCount - 1] = GetRotaryPoseByDistance(passengerFlow.GetPredictedSlotDistance(rotarySlotIndex, FeederMergeDuration), PassengerSetPivotOffset);
            return poses;
        }

        private PassengerUnitRoadPose GetRotaryPose(float progress, float laneOffset)
        {
            return rotaryLayout.GetRotaryPose(progress, laneOffset, RotaryCenterZ, PassengerUnitY);
        }

        private PassengerUnitRoadPose GetRotaryPoseByDistance(float routeDistance, float laneOffset)
        {
            return passengerFlow.GetPose(routeDistance, laneOffset, RotaryCenterZ, PassengerUnitY);
        }

        private Vector3 GetBoardingGatePosition(float laneOffset)
        {
            return GetRotaryPose(rotaryLayout.Preset.BoardingGateProgress, laneOffset).Position;
        }

        private static bool IsPassengerAtBoardingGate(PassengerView passenger)
        {
            var progress = Mathf.Repeat(passenger.RouteProgress, 1f);
            return progress <= BoardingGateProgressWindow || progress >= 1f - BoardingGateProgressWindow;
        }

        private void SetPassengerTrafficPose(PassengerView passenger)
        {
            var pose = GetRotaryPoseByDistance(passenger.RouteDistance, passenger.LaneOffset);
            passenger.SetPose(pose.Position, pose.Rotation);
        }

    }
}
