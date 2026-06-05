using UnityEngine;

namespace BusPuzzle
{
    public sealed class StationSlotUnlockTarget : MonoBehaviour
    {
        public int LockedSlotIndex { get; private set; }

        public void Initialize(int lockedSlotIndex)
        {
            LockedSlotIndex = Mathf.Max(0, lockedSlotIndex);
        }
    }
}
