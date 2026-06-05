using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct VehicleTrafficSettings
    {
        public readonly float CellSize;
        public readonly float GridWorldWidth;
        public readonly float GridWorldDepth;
        public readonly float GridTopZ;
        public readonly float GridBottomZ;
        public readonly float GridLeftX;
        public readonly float GridRightX;
        public readonly Quaternion StationRotation;
        public readonly Vector3 StationForward;
        public readonly Vector3 StationRight;

        public VehicleTrafficSettings(
            float cellSize,
            float gridWorldWidth,
            float gridWorldDepth,
            float gridTopZ,
            float gridBottomZ,
            float gridLeftX,
            float gridRightX,
            Quaternion stationRotation,
            Vector3 stationForward,
            Vector3 stationRight)
        {
            CellSize = cellSize;
            GridWorldWidth = gridWorldWidth;
            GridWorldDepth = gridWorldDepth;
            GridTopZ = gridTopZ;
            GridBottomZ = gridBottomZ;
            GridLeftX = gridLeftX;
            GridRightX = gridRightX;
            StationRotation = stationRotation;
            StationForward = stationForward;
            StationRight = stationRight;
        }
    }

    internal sealed class VehicleTrafficEngine
    {
        private const float VehiclePathSweepStepFactor = 0.16f;
        private const float VehiclePathCollisionBackoffFactor = 0.22f;
        private const float VehiclePathExitClearanceFactor = 0.75f;
        private const float VehiclePathCollisionClearanceFactor = 0.035f;

        private readonly VehicleTrafficSettings settings;

        public VehicleTrafficEngine(VehicleTrafficSettings settings)
        {
            this.settings = settings;
        }

        public bool IsAnyMoveAvailable(IReadOnlyList<BusView> buses, int occupiedStationSlots, int stationCapacity)
        {
            if (occupiedStationSlots >= stationCapacity)
            {
                return false;
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus.IsOnBoard && !bus.IsMoving && IsPathClear(bus, buses, out _, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPathClear(BusView movingBus, IReadOnlyList<BusView> buses, out BusView blockingBus, out Vector3 collisionPosition)
        {
            blockingBus = null;
            collisionPosition = movingBus != null ? movingBus.transform.position : Vector3.zero;
            if (movingBus == null)
            {
                return false;
            }

            var worldDirection = movingBus.VehicleForwardWorld;
            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            worldDirection.Normalize();
            var sweepDistance = GetBoardExitSweepDistance(movingBus, worldDirection);
            var sweepStep = Mathf.Max(0.025f, settings.CellSize * VehiclePathSweepStepFactor);
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(sweepDistance / sweepStep));

            for (var sample = 1; sample <= sampleCount; sample++)
            {
                var distance = Mathf.Min(sweepDistance, sample * sweepStep);
                var footprint = movingBus.GetFootprint(
                    movingBus.transform.position + worldDirection * distance,
                    movingBus.transform.rotation);

                for (var index = 0; index < buses.Count; index++)
                {
                    var bus = buses[index];
                    if (bus == movingBus || !bus.IsOnBoard || bus.IsDeparted)
                    {
                        continue;
                    }

                    if (footprint.Overlaps(bus.CurrentFootprint, settings.CellSize * VehiclePathCollisionClearanceFactor))
                    {
                        blockingBus = bus;
                        collisionPosition = GetBlockedCollisionPosition(movingBus, worldDirection, distance);
                        return false;
                    }
                }
            }

            return true;
        }

        public BusRouteStep[] BuildRouteToStation(BusView bus, Vector3 stationPosition)
        {
            var launchDirection = bus.VehicleForwardWorld;
            var exitPosition = bus.transform.position + launchDirection * GetBoardExitSweepDistance(bus, launchDirection);
            exitPosition.y = bus.transform.position.y;
            stationPosition.y = bus.transform.position.y;

            var topRoadZ = settings.GridTopZ + settings.CellSize * 0.95f;
            var leftRoadX = settings.GridLeftX - settings.CellSize * 0.95f;
            var rightRoadX = settings.GridRightX + settings.CellSize * 0.95f;
            var route = new List<BusRouteStep>();
            var currentPosition = exitPosition;

            AddRouteStep(route, currentPosition, bus.transform.rotation);

            if (currentPosition.z < topRoadZ - 0.01f)
            {
                var sideX = SelectExitSideX(currentPosition, launchDirection, leftRoadX, rightRoadX);
                if (Mathf.Abs(currentPosition.x - sideX) > 0.01f)
                {
                    var sideDirection = sideX > currentPosition.x ? Vector3.right : Vector3.left;
                    AddRouteStep(route, currentPosition, GetRotationForWorldDirection(sideDirection));
                    currentPosition = new Vector3(sideX, currentPosition.y, currentPosition.z);
                    AddRouteStep(route, currentPosition, GetRotationForWorldDirection(sideDirection));
                }
            }

            if (Mathf.Abs(currentPosition.z - topRoadZ) > 0.01f)
            {
                var verticalDirection = topRoadZ > currentPosition.z ? Vector3.forward : Vector3.back;
                AddRouteStep(route, currentPosition, GetRotationForWorldDirection(verticalDirection));
                currentPosition = new Vector3(currentPosition.x, currentPosition.y, topRoadZ);
                AddRouteStep(route, currentPosition, GetRotationForWorldDirection(verticalDirection));
            }

            if (Mathf.Abs(currentPosition.x - stationPosition.x) > 0.01f)
            {
                var horizontalDirection = stationPosition.x > currentPosition.x ? Vector3.right : Vector3.left;
                AddRouteStep(route, currentPosition, GetRotationForWorldDirection(horizontalDirection));
                currentPosition = new Vector3(stationPosition.x, currentPosition.y, topRoadZ);
                AddRouteStep(route, currentPosition, GetRotationForWorldDirection(horizontalDirection));
            }

            var stationApproachPosition = stationPosition - settings.StationForward * (settings.CellSize * 0.62f);
            var stationApproachRootPosition = bus.GetRootPositionForVisualCenter(stationApproachPosition, settings.StationRotation);
            var stationRootPosition = bus.GetRootPositionForVisualCenter(stationPosition, settings.StationRotation);
            AddRouteStep(route, currentPosition, GetRotationForWorldDirection(Vector3.forward));
            AddRouteStep(route, stationApproachRootPosition, settings.StationRotation);
            AddRouteStep(route, stationRootPosition, settings.StationRotation);
            return route.ToArray();
        }

        public BusRouteStep[] BuildRouteFromStation(BusView bus)
        {
            var route = new List<BusRouteStep>();
            var startPosition = bus.transform.position;
            var exitLaneDirection = Vector3.right;
            var exitLaneRotation = Quaternion.LookRotation(exitLaneDirection, Vector3.up);
            var stationExit = startPosition + settings.StationForward * (settings.CellSize * 2.20f);
            var upperRoadEntry = stationExit + settings.StationRight * (settings.CellSize * 0.65f) + settings.StationForward * (settings.CellSize * 0.30f);
            var upperRoadExit = upperRoadEntry + exitLaneDirection * (settings.GridWorldWidth * 1.50f);

            AddRouteStep(route, stationExit, settings.StationRotation);
            AddRouteStep(route, upperRoadEntry, exitLaneRotation);
            AddRouteStep(route, upperRoadExit, exitLaneRotation);
            return route.ToArray();
        }

        private float GetBoardExitSweepDistance(BusView bus, Vector3 worldDirection)
        {
            var footprint = bus.CurrentFootprint;
            var clearance = settings.CellSize * VehiclePathExitClearanceFactor;
            var leftBoundary = settings.GridLeftX - settings.CellSize * 0.5f - clearance;
            var rightBoundary = settings.GridRightX + settings.CellSize * 0.5f + clearance;
            var bottomBoundary = settings.GridBottomZ - settings.CellSize * 0.5f - clearance;
            var topBoundary = settings.GridTopZ + settings.CellSize * 0.5f + clearance;
            var bestDistance = float.PositiveInfinity;

            if (worldDirection.x > 0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (rightBoundary - footprint.ProjectMax(Vector2.right)) / worldDirection.x);
            }
            else if (worldDirection.x < -0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (footprint.ProjectMin(Vector2.right) - leftBoundary) / -worldDirection.x);
            }

            if (worldDirection.z > 0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (topBoundary - footprint.ProjectMax(Vector2.up)) / worldDirection.z);
            }
            else if (worldDirection.z < -0.001f)
            {
                bestDistance = Mathf.Min(bestDistance, (footprint.ProjectMin(Vector2.up) - bottomBoundary) / -worldDirection.z);
            }

            if (float.IsInfinity(bestDistance) || float.IsNaN(bestDistance))
            {
                return Mathf.Max(settings.GridWorldWidth, settings.GridWorldDepth);
            }

            return Mathf.Max(settings.CellSize * 0.5f, bestDistance);
        }

        private Vector3 GetBlockedCollisionPosition(BusView bus, Vector3 worldDirection, float blockedDistance)
        {
            var direction = worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : bus.VehicleForwardWorld;
            var safeDistance = Mathf.Max(settings.CellSize * 0.24f, blockedDistance - settings.CellSize * VehiclePathCollisionBackoffFactor);
            return bus.transform.position + direction * safeDistance;
        }

        private static void AddRouteStep(List<BusRouteStep> route, Vector3 position, Quaternion rotation)
        {
            route.Add(new BusRouteStep(position, rotation));
        }

        private static float SelectExitSideX(Vector3 currentPosition, Vector3 launchDirection, float leftRoadX, float rightRoadX)
        {
            if (currentPosition.x <= leftRoadX)
            {
                return leftRoadX;
            }

            if (currentPosition.x >= rightRoadX)
            {
                return rightRoadX;
            }

            if (Mathf.Abs(launchDirection.x) > 0.05f)
            {
                return launchDirection.x < 0f ? leftRoadX : rightRoadX;
            }

            return Mathf.Abs(currentPosition.x - leftRoadX) <= Mathf.Abs(currentPosition.x - rightRoadX)
                ? leftRoadX
                : rightRoadX;
        }

        private static Quaternion GetRotationForWorldDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
