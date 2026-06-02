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
                    return 6;
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

        public static int ToPeopleCapacity(BusSize size)
        {
            return ToPassengerUnits(size) * 4;
        }

        public static string DisplayName(BusSize size)
        {
            switch (size)
            {
                case BusSize.Small:
                    return "Small";
                case BusSize.Medium:
                    return "Medium";
                case BusSize.Large:
                    return "Large";
                default:
                    return "Bus";
            }
        }
    }
}
