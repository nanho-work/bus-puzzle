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
        private const float BoardingGateAngle = 270f;
        private const float BoardingGateHalfAngle = 24f;
        private const float PassengerUnitInnerEdgeToPivot = 0.28f;

        private readonly bool[] stationOccupied = new bool[ActiveStationSlots];

        private RotaryLayout rotaryLayout;
        private int rotaryActiveTarget;
        private Transform passengerRoot;
        private Transform busRoot;
        private Transform stationRoot;

        private struct RotaryLayout
        {
            public readonly int CapacityUnits;
            public readonly int LaneCount;
            public readonly int SlotsPerLane;
            public readonly int SegmentCount;
            public readonly float InnerRadius;
            public readonly float LaneSpacing;
            public readonly float RoadShoulder;
            public readonly float PassengerSpeed;

            public RotaryLayout(int capacityUnits, int laneCount, int segmentCount, float innerRadius, float laneSpacing, float roadShoulder, float passengerSpeed)
            {
                CapacityUnits = capacityUnits;
                LaneCount = laneCount;
                SlotsPerLane = Mathf.Max(1, capacityUnits / laneCount);
                SegmentCount = segmentCount;
                InnerRadius = innerRadius;
                LaneSpacing = laneSpacing;
                RoadShoulder = roadShoulder;
                PassengerSpeed = passengerSpeed;
            }

            public float RoadWidth => LaneCount * LaneSpacing + RoadShoulder * 2f;
            public float OuterRadius => InnerRadius + RoadWidth;
            public float OuterRadiusX => OuterRadius;
            public float OuterRadiusZ => OuterRadius;
        }

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
            rotaryLayout = CreateRotaryLayout(levelData.PassengerUnits.Count);
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

        public void LayoutWaitingPassengers(IReadOnlyList<PassengerView> passengers, bool animate)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (index < rotaryActiveTarget)
                {
                    AssignPassengerTraffic(passenger, index);
                    var pose = GetRotaryPose(passenger.RouteProgress, passenger.LaneOffset);

                    if (animate)
                    {
                        passenger.transform.rotation = GetPassengerUnitRotation(pose.position);
                        passenger.MoveTo(pose.position, 0.22f);
                    }
                    else
                    {
                        passenger.SetPose(pose.position, GetPassengerUnitRotation(pose.position));
                    }
                }
                else
                {
                    AssignPassengerFeeder(passenger, index - rotaryActiveTarget);
                    SetPassengerFeederPose(passenger);
                }
            }
        }

        public void UpdatePassengerTraffic(IReadOnlyList<PassengerView> passengers, float deltaTime)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.CanCirculate)
                {
                    continue;
                }

                passenger.AdvanceTraffic(deltaTime);
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
                if (passenger.CanCirculate && passenger.Color == color)
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

        private void CreateGround()
        {
            CreateFlatRect(
                "Terminal Floor",
                transform,
                new Vector3(0f, -0.120f, -0.52f),
                new Vector2(5.85f, 9.15f),
                PuzzlePalette.CreateSolidMaterial("Terminal Floor", new Color(0.76f, 0.80f, 0.85f)));

            var rotaryDistrictSize = new Vector2(rotaryLayout.OuterRadiusX * 2f + 0.46f, rotaryLayout.OuterRadiusZ * 2f + 0.86f);
            CreateFlatRect(
                "Passenger Rotary District",
                transform,
                new Vector3(0f, -0.095f, 2.36f),
                rotaryDistrictSize,
                PuzzlePalette.CreateSolidMaterial("Passenger Rotary District", new Color(0.82f, 0.86f, 0.89f)));

            CreateRotaryDistrictTiles(new Vector3(0f, -0.088f, 2.36f), rotaryDistrictSize);

            CreateFlatRect(
                "Bus Puzzle Yard",
                transform,
                new Vector3(0f, -0.090f, GridCenterZ),
                new Vector2(GridWorldWidth + 0.48f, GridWorldDepth + 0.48f),
                PuzzlePalette.CreateSolidMaterial("Bus Puzzle Yard", new Color(0.74f, 0.79f, 0.84f)));

            CreateFlatRect(
                "Open Boarding Apron",
                transform,
                new Vector3(0f, -0.070f, StationZ),
                new Vector2(GridWorldWidth + 0.70f, 0.66f),
                PuzzlePalette.CreateSolidMaterial("Open Boarding Apron", new Color(0.52f, 0.55f, 0.62f)));

            CreateFlatRect(
                "Boarding Apron Front Curb",
                transform,
                new Vector3(0f, -0.052f, StationZ - 0.36f),
                new Vector2(GridWorldWidth + 0.86f, 0.045f),
                PuzzlePalette.CreateSolidMaterial("Boarding Apron Front Curb", new Color(0.95f, 0.97f, 0.98f)));

            CreateFlatRect(
                "Boarding Apron Back Curb",
                transform,
                new Vector3(0f, -0.052f, StationZ + 0.36f),
                new Vector2(GridWorldWidth + 0.86f, 0.045f),
                PuzzlePalette.CreateSolidMaterial("Boarding Apron Back Curb", new Color(0.95f, 0.97f, 0.98f)));

            CreateFeederLane(-1);
            CreateFeederLane(1);
        }

        private void CreateStagingBlock(string name, Vector3 position, Vector3 scale)
        {
            CreateFlatRect(
                name,
                transform,
                new Vector3(position.x, -0.062f, position.z),
                new Vector2(scale.x, scale.z),
                PuzzlePalette.CreateSolidMaterial(name, new Color(0.50f, 0.53f, 0.59f)));
        }

        private void CreateRotaryDistrictTiles(Vector3 center, Vector2 size)
        {
            var tileMaterial = PuzzlePalette.CreateSolidMaterial("Rotary District Tile Lines", new Color(0.67f, 0.73f, 0.78f));
            const float spacing = 0.22f;
            const float lineWidth = 0.012f;
            var verticalCount = Mathf.FloorToInt(size.x / spacing);
            var horizontalCount = Mathf.FloorToInt(size.y / spacing);

            for (var index = 0; index <= verticalCount; index++)
            {
                var x = center.x - size.x * 0.5f + index * spacing;
                CreateFlatRect(
                    $"Rotary Tile Vertical {index + 1}",
                    transform,
                    new Vector3(x, center.y, center.z),
                    new Vector2(lineWidth, size.y),
                    tileMaterial);
            }

            for (var index = 0; index <= horizontalCount; index++)
            {
                var z = center.z - size.y * 0.5f + index * spacing;
                CreateFlatRect(
                    $"Rotary Tile Horizontal {index + 1}",
                    transform,
                    new Vector3(center.x, center.y, z),
                    new Vector2(size.x, lineWidth),
                    tileMaterial);
            }
        }

        private void CreateFeederLane(int side)
        {
            var sign = Mathf.Sign(side);
            var x = sign * (rotaryLayout.OuterRadiusX + 0.34f);
            var entryZScale = rotaryLayout.OuterRadiusZ * 1.78f;
            var name = side < 0 ? "Left Passenger Feeder" : "Right Passenger Feeder";
            CreateStagingBlock(name, new Vector3(x, 0.01f, RotaryCenterZ), new Vector3(0.42f, 0.08f, entryZScale));

            CreateFlatRect(
                $"{name} Merge",
                transform,
                new Vector3(sign * (rotaryLayout.OuterRadiusX + 0.12f), -0.050f, RotaryCenterZ - rotaryLayout.OuterRadiusZ * 0.72f),
                new Vector2(0.34f, 0.94f),
                PuzzlePalette.CreateSolidMaterial($"{name} Merge", new Color(0.54f, 0.57f, 0.63f)),
                Quaternion.Euler(0f, sign > 0f ? -18f : 18f, 0f));

            var railMaterial = PuzzlePalette.CreateSolidMaterial($"{name} Guardrail", new Color(0.96f, 0.98f, 0.99f));
            for (var index = 0; index < 2; index++)
            {
                CreateFlatRect(
                    $"{name} Side Guardrail {index + 1}",
                    transform,
                    new Vector3(x + sign * (index == 0 ? -0.24f : 0.24f), -0.032f, RotaryCenterZ),
                    new Vector2(0.060f, entryZScale * 0.96f),
                    railMaterial);
            }
        }

        private void CreatePassengerRotary()
        {
            CreateSampleStyleRotaryRoad();

            CreateFlatEllipse(
                $"Rotary Island {rotaryLayout.CapacityUnits}",
                transform,
                new Vector3(0f, -0.058f, RotaryCenterZ),
                GetRotaryInnerRadiusX() - 0.10f,
                GetRotaryInnerRadiusZ() - 0.07f,
                PuzzlePalette.CreateSolidMaterial("Rotary Island", new Color(0.80f, 0.85f, 0.88f)));

            var gatePosition = GetBoardingGatePosition(0f);
            gatePosition.y = -0.030f;
            CreateFlatRect(
                "Boarding Gate Opening",
                transform,
                gatePosition,
                new Vector2(1.34f, 0.24f),
                PuzzlePalette.CreateSolidMaterial("Boarding Gate Opening", new Color(0.94f, 0.82f, 0.24f)));
        }

        private void CreateSampleStyleRotaryRoad()
        {
            var roadMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Asphalt Road", new Color(0.50f, 0.53f, 0.59f));
            var railMaterial = PuzzlePalette.CreateSolidMaterial("Rotary White Guardrail", new Color(0.96f, 0.98f, 1.00f));
            var railShadowMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Guardrail Shadow", new Color(0.36f, 0.40f, 0.46f));
            var shadowMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Soft Shadow", new Color(0.34f, 0.38f, 0.44f));
            var dividerMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Lane Divider", new Color(0.68f, 0.72f, 0.78f));

            var outerX = GetRotaryOuterRadiusX();
            var outerZ = GetRotaryOuterRadiusZ();
            var innerX = GetRotaryInnerRadiusX();
            var innerZ = GetRotaryInnerRadiusZ();

            CreateFlatEllipseBand(
                "Rotary Soft Shadow",
                transform,
                new Vector3(0f, -0.078f, RotaryCenterZ - 0.035f),
                outerX + 0.04f,
                outerZ + 0.035f,
                innerX - 0.035f,
                innerZ - 0.028f,
                shadowMaterial);

            CreateFlatEllipseBand(
                "Rotary Road",
                transform,
                new Vector3(0f, -0.066f, RotaryCenterZ),
                outerX,
                outerZ,
                innerX,
                innerZ,
                roadMaterial);

            const float railWidth = 0.095f;
            CreateFlatEllipseBand(
                "Outer Rotary Guardrail Shadow",
                transform,
                new Vector3(0f, -0.050f, RotaryCenterZ - 0.010f),
                outerX + railWidth + 0.018f,
                outerZ + railWidth * 0.70f + 0.014f,
                outerX - 0.006f,
                outerZ - 0.004f,
                railShadowMaterial);

            CreateFlatEllipseBand(
                "Outer Rotary Guardrail",
                transform,
                new Vector3(0f, -0.039f, RotaryCenterZ),
                outerX + railWidth,
                outerZ + railWidth * 0.70f,
                outerX,
                outerZ,
                railMaterial);

            CreateFlatEllipseBand(
                "Inner Rotary Guardrail Shadow",
                transform,
                new Vector3(0f, -0.049f, RotaryCenterZ - 0.006f),
                innerX + 0.006f,
                innerZ + 0.004f,
                innerX - railWidth - 0.018f,
                innerZ - railWidth * 0.70f - 0.014f,
                railShadowMaterial);

            CreateFlatEllipseBand(
                "Inner Rotary Guardrail",
                transform,
                new Vector3(0f, -0.038f, RotaryCenterZ),
                innerX,
                innerZ,
                innerX - railWidth,
                innerZ - railWidth * 0.70f,
                railMaterial);

            var dividerCount = Mathf.Max(0, rotaryLayout.LaneCount - 1);
            for (var dividerIndex = 0; dividerIndex < dividerCount; dividerIndex++)
            {
                var t = (dividerIndex + 1f) / rotaryLayout.LaneCount;
                var dividerOuterX = Mathf.Lerp(innerX, outerX, t) + 0.012f;
                var dividerOuterZ = Mathf.Lerp(innerZ, outerZ, t) + 0.010f;
                var dividerInnerX = Mathf.Lerp(innerX, outerX, t) - 0.012f;
                var dividerInnerZ = Mathf.Lerp(innerZ, outerZ, t) - 0.010f;
                CreateFlatEllipseBand(
                    $"Rotary Lane Divider {dividerIndex + 1}",
                    transform,
                    new Vector3(0f, -0.041f, RotaryCenterZ),
                    dividerOuterX,
                    dividerOuterZ,
                    dividerInnerX,
                    dividerInnerZ,
                    dividerMaterial);
            }
        }

        private void CreateRotaryRoadBed()
        {
            var segmentCount = Mathf.Max(72, rotaryLayout.SegmentCount);
            var segmentLength = EstimateRotarySegmentLength(0f, segmentCount) * 1.06f;
            var roadWidth = rotaryLayout.LaneCount * rotaryLayout.LaneSpacing + 0.32f;
            var roadMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Road Bed", new Color(0.42f, 0.52f, 0.62f));

            for (var index = 0; index < segmentCount; index++)
            {
                var progress = index / (float)segmentCount;
                var pose = GetRotaryPose(progress, 0f);
                CreateFlatRect(
                    $"Rotary Road Bed {index + 1}",
                    transform,
                    pose.position + Vector3.down * 0.085f,
                    new Vector2(roadWidth, segmentLength),
                    roadMaterial,
                    pose.rotation);
            }
        }

        private void CreateRotaryLane(int laneIndex)
        {
            var laneOffset = GetLaneOffset(laneIndex);
            var segmentCount = rotaryLayout.SegmentCount + laneIndex * 4;
            var segmentLength = EstimateRotarySegmentLength(laneOffset, segmentCount);
            var color = laneIndex % 2 == 0
                ? new Color(0.58f, 0.67f, 0.76f)
                : new Color(0.50f, 0.60f, 0.70f);
            var laneMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary Lane {laneIndex + 1}", color);

            for (var index = 0; index < segmentCount; index++)
            {
                var progress = index / (float)segmentCount;
                var pose = GetRotaryPose(progress, laneOffset);
                CreateFlatRect(
                    $"Rotary Lane {laneIndex + 1}-{index + 1}",
                    transform,
                    pose.position + Vector3.down * 0.062f,
                    new Vector2(rotaryLayout.LaneSpacing * 0.78f, segmentLength),
                    laneMaterial,
                    pose.rotation);
            }
        }

        private void CreateRotaryRail(string name, float laneOffset)
        {
            var railMaterial = PuzzlePalette.CreateSolidMaterial(name, new Color(0.97f, 0.98f, 1.00f));
            var segmentCount = Mathf.Max(36, rotaryLayout.SegmentCount / 2);
            var segmentLength = EstimateRotarySegmentLength(laneOffset, segmentCount) * 0.90f;

            for (var index = 0; index < segmentCount; index++)
            {
                var progress = index / (float)segmentCount;
                var pose = GetRotaryPose(progress, laneOffset);
                CreateFlatRect(
                    $"{name} {index + 1}",
                    transform,
                    pose.position + Vector3.down * 0.034f,
                    new Vector2(0.060f, segmentLength),
                    railMaterial,
                    pose.rotation);
            }
        }

        private void CreateGrid()
        {
            var cellMaterial = PuzzlePalette.CreateSolidMaterial("Parking Cell", new Color(0.58f, 0.62f, 0.65f));

            for (var y = 0; y < GridRows; y++)
            {
                for (var x = 0; x < GridColumns; x++)
                {
                    CreateFlatRect(
                        $"Cell {x},{y}",
                        transform,
                        GridToWorld(new Vector2Int(x, y)) + Vector3.down * 0.025f,
                        new Vector2(CellSize * 0.92f, CellSize * 0.92f),
                        cellMaterial);
                }
            }
        }

        private void CreateStationSlots()
        {
            var platformMaterial = PuzzlePalette.CreateSolidMaterial("Station Platform", new Color(0.50f, 0.54f, 0.64f));
            var platformInnerMaterial = PuzzlePalette.CreateSolidMaterial("Station Platform Inner", new Color(0.58f, 0.62f, 0.72f));
            var curbMaterial = PuzzlePalette.CreateSolidMaterial("Station Curb", new Color(0.94f, 0.96f, 0.98f));
            var slotOutlineMaterial = PuzzlePalette.CreateSolidMaterial("Station Slot Line", new Color(0.80f, 0.84f, 0.91f));
            var lockedMaterial = PuzzlePalette.CreateSolidMaterial("Locked Ad Slot", new Color(0.39f, 0.43f, 0.51f));
            var freeMaterial = PuzzlePalette.CreateSolidMaterial("Free Station Badge", new Color(0.82f, 0.60f, 0.10f));

            var platformWidth = (TotalStationSlots - 1) * StationSlotSpacing + StationSlotWidth + 0.42f;
            var platformDepth = StationSlotDepth + 0.30f;
            CreateFlatRoundedRect(
                "Station Platform Base",
                stationRoot,
                new Vector3(0f, -0.062f, StationZ),
                new Vector2(platformWidth, platformDepth),
                0.14f,
                platformMaterial);

            CreateFlatRoundedRect(
                "Station Platform Surface",
                stationRoot,
                new Vector3(0f, -0.050f, StationZ),
                new Vector2(platformWidth - 0.12f, platformDepth - 0.12f),
                0.11f,
                platformInnerMaterial);

            CreateFlatRect(
                "Station Front Curb",
                stationRoot,
                new Vector3(0f, -0.032f, StationZ - platformDepth * 0.5f - 0.025f),
                new Vector2(platformWidth + 0.18f, 0.045f),
                curbMaterial);

            CreateFreeStationBadge(GetFreeStationPosition(), freeMaterial, slotOutlineMaterial);

            for (var index = 0; index < ActiveStationSlots; index++)
            {
                CreateStationSlotOutline($"Station Slot {index + 1}", GetStationPosition(index), slotOutlineMaterial, platformInnerMaterial);
            }

            for (var index = 0; index < LockedStationSlots; index++)
            {
                var lockedPosition = GetLockedStationPosition(index);
                CreateLockedStationSlot($"Ad Locked Slot {index + 1}", lockedPosition, slotOutlineMaterial, lockedMaterial);
            }
        }

        private void CreateStationSlotOutline(string name, Vector3 position, Material outlineMaterial, Material innerMaterial)
        {
            CreateFlatRoundedRect(
                $"{name} Outline",
                stationRoot,
                position + Vector3.down * 0.026f,
                new Vector2(StationSlotWidth, StationSlotDepth),
                StationSlotWidth * 0.18f,
                outlineMaterial);

            CreateFlatRoundedRect(
                $"{name} Cutout",
                stationRoot,
                position + Vector3.down * 0.018f,
                new Vector2(StationSlotWidth - 0.045f, StationSlotDepth - 0.045f),
                StationSlotWidth * 0.15f,
                innerMaterial);
        }

        private void CreateLockedStationSlot(string name, Vector3 position, Material outlineMaterial, Material lockedMaterial)
        {
            CreateStationSlotOutline(name, position, outlineMaterial, lockedMaterial);

            var plusMaterial = PuzzlePalette.CreateSolidMaterial("Slot Plus", new Color(0.24f, 0.90f, 0.42f));
            CreatePlusBar($"{name} Plus Vertical", position, new Vector3(0.046f, 0.04f, 0.18f), plusMaterial);
            CreatePlusBar($"{name} Plus Horizontal", position, new Vector3(0.16f, 0.04f, 0.046f), plusMaterial);
        }

        private void CreateFreeStationBadge(Vector3 position, Material badgeMaterial, Material outlineMaterial)
        {
            var badgePosition = position + new Vector3(0f, 0f, -StationSlotDepth * 0.04f);
            CreateFlatRoundedRect(
                "Free Station Badge Outline",
                stationRoot,
                badgePosition + Vector3.down * 0.014f,
                new Vector2(StationSlotWidth * 0.82f, StationSlotDepth * 0.68f),
                StationSlotWidth * 0.16f,
                outlineMaterial);

            CreateFlatRoundedRect(
                "Free Station Badge",
                stationRoot,
                badgePosition + Vector3.down * 0.006f,
                new Vector2(StationSlotWidth * 0.72f, StationSlotDepth * 0.58f),
                StationSlotWidth * 0.14f,
                badgeMaterial);

            CreateStationLabel("Free Station Label", badgePosition, "FREE", new Color(1f, 0.94f, 0.52f));
        }

        private void CreateStationLabel(string name, Vector3 position, string label, Color color)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(stationRoot, false);
            labelObject.transform.SetPositionAndRotation(
                position + new Vector3(0f, 0.025f, -StationSlotDepth * 0.04f),
                Quaternion.Euler(90f, 0f, 0f));

            var text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = CellSize * 0.105f;
            text.fontSize = 48;
            text.color = color;

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CreatePlusBar(string name, Vector3 position, Vector3 scale, Material material)
        {
            CreateFlatRect(
                name,
                stationRoot,
                position + Vector3.up * 0.02f,
                new Vector2(scale.x, scale.z),
                material);
        }

        private static GameObject CreateFlatRect(string name, Transform parent, Vector3 position, Vector2 size, Material material)
        {
            return CreateFlatRect(name, parent, position, size, material, Quaternion.identity);
        }

        private static GameObject CreateFlatRoundedRect(string name, Transform parent, Vector3 position, Vector2 size, float radius, Material material)
        {
            const int cornerSegments = 5;
            var roundedObject = new GameObject(name);
            roundedObject.transform.SetParent(parent, false);
            roundedObject.transform.position = position;

            radius = Mathf.Clamp(radius, 0.01f, Mathf.Min(size.x, size.y) * 0.5f);
            var halfWidth = size.x * 0.5f;
            var halfDepth = size.y * 0.5f;
            var centers = new[]
            {
                new Vector2(halfWidth - radius, halfDepth - radius),
                new Vector2(-halfWidth + radius, halfDepth - radius),
                new Vector2(-halfWidth + radius, -halfDepth + radius),
                new Vector2(halfWidth - radius, -halfDepth + radius)
            };
            var startAngles = new[] { 0f, 90f, 180f, 270f };
            var points = new List<Vector2>();

            for (var corner = 0; corner < centers.Length; corner++)
            {
                for (var segment = 0; segment <= cornerSegments; segment++)
                {
                    var angle = (startAngles[corner] + segment * 90f / cornerSegments) * Mathf.Deg2Rad;
                    points.Add(centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }

            var vertices = new Vector3[points.Count + 1];
            vertices[0] = Vector3.zero;
            for (var index = 0; index < points.Count; index++)
            {
                vertices[index + 1] = new Vector3(points[index].x, 0f, points[index].y);
            }

            var triangles = new int[points.Count * 6];
            for (var index = 0; index < points.Count; index++)
            {
                var current = index + 1;
                var next = index + 1 == points.Count ? 1 : index + 2;
                var triangleIndex = index * 6;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = current;
                triangles[triangleIndex + 2] = next;
                triangles[triangleIndex + 3] = 0;
                triangles[triangleIndex + 4] = next;
                triangles[triangleIndex + 5] = current;
            }

            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = roundedObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = roundedObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return roundedObject;
        }

        private static GameObject CreateFlatRect(string name, Transform parent, Vector3 position, Vector2 size, Material material, Quaternion rotation)
        {
            var flatObject = new GameObject(name);
            flatObject.transform.SetParent(parent, false);
            flatObject.transform.SetPositionAndRotation(position, rotation);

            var halfWidth = size.x * 0.5f;
            var halfDepth = size.y * 0.5f;
            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = flatObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = flatObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return flatObject;
        }

        private static GameObject CreateFlatEllipse(string name, Transform parent, Vector3 position, float radiusX, float radiusZ, Material material)
        {
            const int segments = 96;
            var flatObject = new GameObject(name);
            flatObject.transform.SetParent(parent, false);
            flatObject.transform.position = position;

            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;

            for (var index = 0; index < segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ);
            }

            for (var index = 0; index < segments; index++)
            {
                var current = index + 1;
                var next = index + 1 == segments ? 1 : index + 2;
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = next;
                triangles[triangleIndex + 2] = current;
            }

            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = flatObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = flatObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return flatObject;
        }

        private static GameObject CreateFlatEllipseBand(
            string name,
            Transform parent,
            Vector3 position,
            float outerRadiusX,
            float outerRadiusZ,
            float innerRadiusX,
            float innerRadiusZ,
            Material material)
        {
            const int segments = 128;
            innerRadiusX = Mathf.Max(0.05f, innerRadiusX);
            innerRadiusZ = Mathf.Max(0.05f, innerRadiusZ);

            var flatObject = new GameObject(name);
            flatObject.transform.SetParent(parent, false);
            flatObject.transform.position = position;

            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];

            for (var index = 0; index < segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                var vertexIndex = index * 2;
                vertices[vertexIndex] = new Vector3(cos * outerRadiusX, 0f, sin * outerRadiusZ);
                vertices[vertexIndex + 1] = new Vector3(cos * innerRadiusX, 0f, sin * innerRadiusZ);
            }

            for (var index = 0; index < segments; index++)
            {
                var next = (index + 1) % segments;
                var outerCurrent = index * 2;
                var innerCurrent = outerCurrent + 1;
                var outerNext = next * 2;
                var innerNext = outerNext + 1;
                var triangleIndex = index * 6;

                triangles[triangleIndex] = outerCurrent;
                triangles[triangleIndex + 1] = innerCurrent;
                triangles[triangleIndex + 2] = innerNext;
                triangles[triangleIndex + 3] = outerCurrent;
                triangles[triangleIndex + 4] = innerNext;
                triangles[triangleIndex + 5] = outerNext;
            }

            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = flatObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = flatObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return flatObject;
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

        private float GetRotaryOuterRadiusX()
        {
            return rotaryLayout.OuterRadiusX;
        }

        private float GetRotaryOuterRadiusZ()
        {
            return rotaryLayout.OuterRadiusZ;
        }

        private float GetRotaryInnerRadiusX()
        {
            return rotaryLayout.InnerRadius;
        }

        private float GetRotaryInnerRadiusZ()
        {
            return rotaryLayout.InnerRadius;
        }

        private static RotaryLayout CreateRotaryLayout(int passengerUnitCount)
        {
            if (passengerUnitCount <= 40)
            {
                return new RotaryLayout(40, 1, 96, 0.86f, 0.50f, 0.08f, 0.034f);
            }

            return new RotaryLayout(80, 1, 144, 1.08f, 0.52f, 0.08f, 0.026f);
        }

        private int GetStartingRotaryUnitCount(int passengerUnitCount)
        {
            return Mathf.Clamp(passengerUnitCount, 0, rotaryLayout.CapacityUnits);
        }

        private void AssignPassengerTraffic(PassengerView passenger, int rotarySlotIndex)
        {
            var clampedSlotIndex = Mathf.Clamp(rotarySlotIndex, 0, rotaryLayout.CapacityUnits - 1);
            var laneIndex = clampedSlotIndex % rotaryLayout.LaneCount;
            var laneSlotIndex = clampedSlotIndex / rotaryLayout.LaneCount;
            var laneOffset = GetLaneOffset(laneIndex);
            var progress = (laneSlotIndex + 0.5f) / rotaryLayout.SlotsPerLane;

            progress = Mathf.Repeat(progress + laneIndex * 0.012f, 1f);
            passenger.AssignTraffic(progress, rotaryLayout.PassengerSpeed, laneOffset, clampedSlotIndex);
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
            while (CountRotaryAssignedPassengers(passengers) < targetCount && TryFindFeederPassenger(passengers, out var passenger))
            {
                var slotIndex = FindOpenRotarySlot(passengers);
                if (slotIndex < 0)
                {
                    return;
                }

                AssignPassengerTraffic(passenger, slotIndex);
                var pose = GetRotaryPose(passenger.RouteProgress, passenger.LaneOffset);
                passenger.MoveTo(pose.position, 0.24f);
            }
        }

        private static int CountRotaryAssignedPassengers(IReadOnlyList<PassengerView> passengers)
        {
            var count = 0;
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                if (!passenger.IsWaitingInFeeder && passenger.RotarySlotIndex >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryFindFeederPassenger(IReadOnlyList<PassengerView> passengers, out PassengerView passenger)
        {
            passenger = null;
            var bestSlot = int.MaxValue;

            for (var index = 0; index < passengers.Count; index++)
            {
                var candidate = passengers[index];
                if (!candidate.IsWaitingInFeeder || candidate.FeederSlotIndex >= bestSlot)
                {
                    continue;
                }

                passenger = candidate;
                bestSlot = candidate.FeederSlotIndex;
            }

            return passenger != null;
        }

        private int FindOpenRotarySlot(IReadOnlyList<PassengerView> passengers)
        {
            for (var slot = 0; slot < rotaryLayout.CapacityUnits; slot++)
            {
                var isOccupied = false;
                for (var index = 0; index < passengers.Count; index++)
                {
                    var passenger = passengers[index];
                    if (!passenger.IsWaitingInFeeder && passenger.RotarySlotIndex == slot)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    return slot;
                }
            }

            return -1;
        }

        private void SetPassengerFeederPose(PassengerView passenger)
        {
            var position = GetFeederPosition(passenger.FeederSide, passenger.FeederSlotIndex);
            passenger.SetPose(position, Quaternion.identity);
        }

        private Vector3 GetFeederPosition(int side, int slotIndex)
        {
            var sign = side < 0 ? -1f : 1f;
            var row = slotIndex % 10;
            var stack = slotIndex / 10;
            var zStart = RotaryCenterZ + rotaryLayout.OuterRadiusZ * 0.78f;
            var z = zStart - row * 0.24f;
            var x = sign * (rotaryLayout.OuterRadiusX + 0.34f + stack * 0.18f);
            return new Vector3(x, 0.38f, z);
        }

        private float GetLaneOffset(int laneIndex)
        {
            return PassengerUnitInnerEdgeToPivot;
        }

        private (Vector3 position, Quaternion rotation, Vector3 right) GetRotaryPose(float progress, float laneOffset)
        {
            var angle = progress * Mathf.PI * 2f;
            var radius = rotaryLayout.InnerRadius + laneOffset;
            var position = new Vector3(
                Mathf.Cos(angle) * radius,
                0.38f,
                RotaryCenterZ + Mathf.Sin(angle) * radius);

            var tangent = new Vector3(
                -Mathf.Sin(angle) * radius,
                0f,
                Mathf.Cos(angle) * radius).normalized;

            var rotation = tangent.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(tangent, Vector3.up)
                : Quaternion.identity;

            return (position, rotation, rotation * Vector3.right);
        }

        private Vector3 GetBoardingGatePosition(float laneOffset)
        {
            return new Vector3(0f, 0.38f, RotaryCenterZ - rotaryLayout.InnerRadius - laneOffset);
        }

        private float EstimateRotarySegmentLength(float laneOffset, int segmentCount)
        {
            var radius = rotaryLayout.InnerRadius + laneOffset;
            var circumference = Mathf.PI * 2f * radius;
            return Mathf.Clamp(circumference / segmentCount * 0.78f, 0.16f, 0.31f);
        }

        private static bool IsPassengerAtBoardingGate(PassengerView passenger)
        {
            var angle = Mathf.Repeat(passenger.RouteProgress * 360f, 360f);
            var delta = Mathf.Abs(Mathf.DeltaAngle(angle, BoardingGateAngle));
            return delta <= BoardingGateHalfAngle;
        }

        private void SetPassengerTrafficPose(PassengerView passenger)
        {
            var pose = GetRotaryPose(passenger.RouteProgress, passenger.LaneOffset);
            passenger.SetPose(pose.position, GetPassengerUnitRotation(pose.position));
        }

        private static Quaternion GetPassengerUnitRotation(Vector3 position)
        {
            var outward = new Vector3(position.x, 0f, position.z - RotaryCenterZ);
            return outward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(outward.normalized, Vector3.up)
                : Quaternion.identity;
        }
    }
}
