using System.Collections.Generic;

namespace BusPuzzle
{
    internal static class BoardingRuleEngine
    {
        public static BusView FindStationBusAtSlot(IReadOnlyList<BusView> buses, int slotIndex)
        {
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus != null && bus.IsParkedAtStation && !bus.IsDeparted && bus.StationSlotIndex == slotIndex)
                {
                    return bus;
                }
            }

            return null;
        }

        public static bool IsValidReservation(BusView bus, PassengerView passenger)
        {
            return bus != null &&
                passenger != null &&
                passenger.gameObject.activeSelf &&
                passenger.IsReservedForBoarding &&
                bus.IsParkedAtStation &&
                !bus.IsDeparted &&
                bus.HasBoardingReservations;
        }

        public static bool EarlierSameColorStationBusBlocks(BusView candidateBus, BusView earlierBus)
        {
            return candidateBus != null &&
                earlierBus != null &&
                earlierBus != candidateBus &&
                earlierBus.Color == candidateBus.Color &&
                earlierBus.StationSlotIndex >= 0 &&
                candidateBus.StationSlotIndex >= 0 &&
                earlierBus.StationSlotIndex < candidateBus.StationSlotIndex;
        }
    }
}
