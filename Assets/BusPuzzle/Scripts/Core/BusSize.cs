namespace BusPuzzle
{
    public enum BusSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    public static class BusSizeUtility
    {
        public static int ToBoardCells(BusSize size)
        {
            switch (size)
            {
                case BusSize.Small:
                    return 2;
                case BusSize.Medium:
                    return 3;
                case BusSize.Large:
                    return 4;
                default:
                    return 2;
            }
        }

        public static int ToPassengerUnits(BusSize size)
        {
            switch (size)
            {
                case BusSize.Small:
                    return 4;
                case BusSize.Medium:
                    return 7;
                case BusSize.Large:
                    return 10;
                default:
                    return 4;
            }
        }

        public static int ToVisualCharacterUnits(BusSize size)
        {
            switch (size)
            {
                case BusSize.Small:
                    return 2;
                case BusSize.Medium:
                    return 3;
                case BusSize.Large:
                    return 4;
                default:
                    return 2;
            }
        }

        public static float ToVisualLengthCells(BusSize size)
        {
            switch (size)
            {
                case BusSize.Small:
                    return 1.12f;
                case BusSize.Medium:
                    return 1.68f;
                case BusSize.Large:
                    return 2.24f;
                default:
                    return 1.12f;
            }
        }

        public static int ToPeopleCapacity(BusSize size)
        {
            return ToPassengerUnits(size) * 4;
        }

        public static string DisplayName(BusSize size)
        {
            switch (size)
            {
                case BusSize.Small:
                    return "Compact Van";
                case BusSize.Medium:
                    return "Freezer Truck";
                case BusSize.Large:
                    return "Bus";
                default:
                    return "Vehicle";
            }
        }
    }
}
