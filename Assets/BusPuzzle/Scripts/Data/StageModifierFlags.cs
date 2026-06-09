using System;

namespace BusPuzzle
{
    [Flags]
    public enum StageModifierFlags
    {
        None = 0,
        Garages = 1 << 0,
        MysteryVehicles = 1 << 1,
        LightMysteryVehicles = 1 << 2
    }
}
