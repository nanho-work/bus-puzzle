using System;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class StationSlotController
    {
        private readonly bool[] occupiedSlots;

        public StationSlotController(int capacity)
        {
            occupiedSlots = new bool[Mathf.Max(0, capacity)];
        }

        public int Capacity => occupiedSlots.Length;

        public int OccupiedSlots
        {
            get
            {
                var count = 0;
                for (var index = 0; index < occupiedSlots.Length; index++)
                {
                    if (occupiedSlots[index])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Reset()
        {
            for (var index = 0; index < occupiedSlots.Length; index++)
            {
                occupiedSlots[index] = false;
            }
        }

        public bool TryReserve(Func<int, Vector3> getSlotPosition, out int slotIndex, out Vector3 slotPosition)
        {
            for (var index = 0; index < occupiedSlots.Length; index++)
            {
                if (occupiedSlots[index])
                {
                    continue;
                }

                occupiedSlots[index] = true;
                slotIndex = index;
                slotPosition = getSlotPosition != null ? getSlotPosition(index) : Vector3.zero;
                return true;
            }

            slotIndex = -1;
            slotPosition = Vector3.zero;
            return false;
        }

        public void Release(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= occupiedSlots.Length)
            {
                return;
            }

            occupiedSlots[slotIndex] = false;
        }
    }
}
