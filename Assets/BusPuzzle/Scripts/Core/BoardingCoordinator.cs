using System.Collections.Generic;

namespace BusPuzzle
{
    internal sealed class BoardingCoordinator
    {
        private readonly List<BoardingReservation> reservations = new List<BoardingReservation>();

        private readonly struct BoardingReservation
        {
            public readonly BusView Bus;
            public readonly PassengerView Passenger;

            public BoardingReservation(BusView bus, PassengerView passenger)
            {
                Bus = bus;
                Passenger = passenger;
            }
        }

        public bool HasPendingReservations => reservations.Count > 0;

        public void Clear()
        {
            reservations.Clear();
        }

        public bool ReserveAvailable(BoardView boardView, IReadOnlyList<BusView> buses, IReadOnlyList<PassengerView> passengers)
        {
            var reservedAny = false;

            for (var slotIndex = BoardView.VipStationSlotIndex; slotIndex < boardView.StationCapacity; slotIndex++)
            {
                var bus = BoardingRuleEngine.FindStationBusAtSlot(buses, slotIndex);
                if (bus == null)
                {
                    continue;
                }

                while (bus.HasAvailableBoardingSeat)
                {
                    if (!boardView.TryFindBoardingReservationPassenger(passengers, bus.Color, out var passengerIndex))
                    {
                        break;
                    }

                    var passenger = passengers[passengerIndex];
                    if (!passenger.TryReserveForBoarding())
                    {
                        break;
                    }

                    if (!bus.ReserveBoardingSeat())
                    {
                        passenger.CancelBoardingReservation();
                        break;
                    }

                    reservations.Add(new BoardingReservation(bus, passenger));
                    reservedAny = true;
                }
            }

            return reservedAny;
        }

        public bool CanReserveAny(BoardView boardView, IReadOnlyList<BusView> buses, IReadOnlyList<PassengerView> passengers)
        {
            for (var slotIndex = BoardView.VipStationSlotIndex; slotIndex < boardView.StationCapacity; slotIndex++)
            {
                var bus = BoardingRuleEngine.FindStationBusAtSlot(buses, slotIndex);
                if (bus == null || !bus.HasAvailableBoardingSeat)
                {
                    continue;
                }

                if (boardView.TryFindBoardingReservationPassenger(passengers, bus.Color, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryTakeReadyReservation(BoardView boardView, out BusView bus, out PassengerView passenger)
        {
            CleanInvalidReservations();

            for (var index = 0; index < reservations.Count; index++)
            {
                var reservation = reservations[index];
                if (HasEarlierSameColorReservation(reservation.Bus))
                {
                    continue;
                }

                if (!boardView.IsPassengerReadyToBoard(reservation.Passenger))
                {
                    continue;
                }

                reservations.RemoveAt(index);
                bus = reservation.Bus;
                passenger = reservation.Passenger;
                return true;
            }

            bus = null;
            passenger = null;
            return false;
        }

        private void CleanInvalidReservations()
        {
            for (var index = reservations.Count - 1; index >= 0; index--)
            {
                if (BoardingRuleEngine.IsValidReservation(reservations[index].Bus, reservations[index].Passenger))
                {
                    continue;
                }

                reservations[index].Bus?.CancelBoardingReservation();
                reservations[index].Passenger?.CancelBoardingReservation();
                reservations.RemoveAt(index);
            }
        }

        private bool HasEarlierSameColorReservation(BusView bus)
        {
            if (bus == null || bus.StationSlotIndex < 0)
            {
                return false;
            }

            for (var index = 0; index < reservations.Count; index++)
            {
                var reservation = reservations[index];
                if (!BoardingRuleEngine.IsValidReservation(reservation.Bus, reservation.Passenger) || reservation.Bus == bus)
                {
                    continue;
                }

                if (BoardingRuleEngine.EarlierSameColorStationBusBlocks(bus, reservation.Bus))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
