using System;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class StationSlotController
    {
        private readonly int initialCapacity;
        private readonly bool[] occupiedSlots;
        private int capacity;

        public StationSlotController(int initialCapacity, int maxCapacity)
        {
            this.initialCapacity = Mathf.Clamp(initialCapacity, 0, Mathf.Max(0, maxCapacity));
            occupiedSlots = new bool[Mathf.Max(this.initialCapacity, maxCapacity)];
            capacity = this.initialCapacity;
        }

        public int Capacity => capacity;

        public int MaxCapacity => occupiedSlots.Length;

        public int LockedSlots => Mathf.Max(0, MaxCapacity - Capacity);

        public bool CanUnlock => Capacity < MaxCapacity;

        public int OccupiedSlots
        {
            get
            {
                var count = 0;
                for (var index = 0; index < capacity; index++)
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

            capacity = initialCapacity;
        }

        public bool TryUnlock()
        {
            if (!CanUnlock)
            {
                return false;
            }

            capacity++;
            return true;
        }

        public bool TryReserve(Func<int, Vector3> getSlotPosition, out int slotIndex, out Vector3 slotPosition)
        {
            for (var index = 0; index < capacity; index++)
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
