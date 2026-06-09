using UnityEngine;

namespace BusPuzzle
{
    internal static class BoardLayoutConfig
    {
        public const int GridColumns = 14;
        public const int GridRows = 14;
        public const int FreeStationSlots = 1;
        public const int ActiveStationSlots = 4;
        public const int LockedStationSlots = 4;

        public const float CellSize = 0.31f;
        public const float GridBottomZ = -4.17f;
        public const float StationZ = 0.66f;
        public const float StationSlotSpacing = 0.47f;
        public const float StationSlotWidth = 0.325f;
        public const float StationSlotDepth = 0.70f;
        public const float StationYawDegrees = 7f;
        public const float StationCounterBelowSlotOffset = 0.12f;
        public const float StationCounterY = 0.12f;
        public const float RotaryCenterZ = 2.42f;

        public const float VehicleBaseVisualWidthCells = 0.72f;
        public const float VehicleBaseVisualHeightCells = 0.90f;
        public const float VehicleVisualWidthScale = 1.18f;
        public const float VehicleVisualHeightScale = 1.16f;
        public const float VehicleBodyVisualWidthScale = 1.12f;
        public const float VehicleBodyVisualLengthScale = 1.01f;
        public const float VehicleVisualWidthCells = VehicleBaseVisualWidthCells * VehicleVisualWidthScale;
        public const float VehicleVisualHeightCells = VehicleBaseVisualHeightCells * VehicleVisualHeightScale;
        public const float VehicleFootprintWidthFactor = 0.96f;
        public const float VehicleFootprintLengthFactor = 0.96f;
        public const float VehicleNearPaddingCells = 0.10f;

        public static int TotalStationSlots => FreeStationSlots + ActiveStationSlots + LockedStationSlots;
        public static float GridWorldWidth => GridColumns * CellSize;
        public static float GridWorldDepth => GridRows * CellSize;
        public static float GridCenterZ => GridBottomZ + (GridRows - 1) * CellSize * 0.5f;
        public static float GridTopZ => GridBottomZ + (GridRows - 1) * CellSize;
        public static float GridLeftX => (0 - (GridColumns - 1) * 0.5f) * CellSize;
        public static float GridRightX => (GridColumns - 1 - (GridColumns - 1) * 0.5f) * CellSize;
        public static Quaternion StationRotation => Quaternion.Euler(0f, StationYawDegrees, 0f);
        public static Vector3 StationForward => StationRotation * Vector3.forward;
        public static Vector3 StationRight => StationRotation * Vector3.right;

        public static bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < GridColumns && cell.y >= 0 && cell.y < GridRows;
        }

        public static Vector3 GridToWorld(Vector2Int cell)
        {
            var x = (cell.x - (GridColumns - 1) * 0.5f) * CellSize;
            var z = GridBottomZ + cell.y * CellSize;
            return new Vector3(x, 0f, z);
        }

        public static Vector3 GridToWorld(Vector2Int cell, Vector2 offsetCells)
        {
            var position = GridToWorld(cell);
            position.x += offsetCells.x * CellSize;
            position.z += offsetCells.y * CellSize;
            return position;
        }

        public static Vector3 GetStationPosition(int activeSlotIndex)
        {
            return GetStationPositionByTotalIndex(FreeStationSlots + activeSlotIndex);
        }

        public static Vector3 GetFreeStationPosition()
        {
            return GetStationPositionByTotalIndex(0);
        }

        public static Vector3 GetLockedStationPosition(int lockedSlotIndex)
        {
            return GetStationPositionByTotalIndex(FreeStationSlots + ActiveStationSlots + lockedSlotIndex);
        }

        public static Vector3 GetStationPositionByTotalIndex(int totalIndex)
        {
            var offset = (totalIndex - (TotalStationSlots - 1) * 0.5f) * StationSlotSpacing;
            return new Vector3(offset, 0f, StationZ);
        }

        public static VehicleFootprint GetVehicleFootprint(Vector3 rootPosition, Quaternion rotation, BusSize size, float cellSize)
        {
            var visualLength = BusSizeUtility.ToVisualLengthCells(size) * cellSize;
            var visualCharacterLength = visualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(size));
            var visualCenterZ = (visualLength - visualCharacterLength) * 0.5f;
            var visualCenter = rootPosition + rotation * new Vector3(0f, 0f, visualCenterZ);

            return new VehicleFootprint(
                visualCenter,
                rotation * Vector3.right,
                rotation * Vector3.forward,
                VehicleBaseVisualWidthCells * cellSize * VehicleFootprintWidthFactor * 0.5f,
                visualLength * VehicleFootprintLengthFactor * 0.5f);
        }

        public static VehicleFootprint GetVehicleFootprintCells(BusDefinition bus)
        {
            var rootPosition = new Vector3(
                bus.GridPosition.x + bus.PositionOffsetCells.x,
                0f,
                bus.GridPosition.y + bus.PositionOffsetCells.y);
            return GetVehicleFootprint(rootPosition, bus.Rotation, bus.Size, 1f);
        }
    }
}
