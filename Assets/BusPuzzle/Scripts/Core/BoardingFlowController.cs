using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal sealed class BoardingFlowController
    {
        private const float BoardingUnitLaunchInterval = 0.12f;
        private const float DepartureLaunchInterval = 0.10f;

        private readonly MonoBehaviour coroutineHost;
        private readonly BoardView boardView;
        private readonly List<BusView> buses;
        private readonly List<PassengerView> circulatingPassengerUnits;
        private readonly BoardingCoordinator boardingCoordinator = new BoardingCoordinator();
        private readonly Action updateCounters;
        private readonly Func<bool> tryCompleteLevelIfReady;
        private readonly Action checkBlocked;
        private readonly Func<bool> isPlaying;

        private Coroutine boardingRoutine;

        public BoardingFlowController(
            MonoBehaviour coroutineHost,
            BoardView boardView,
            List<BusView> buses,
            List<PassengerView> circulatingPassengerUnits,
            Action updateCounters,
            Func<bool> tryCompleteLevelIfReady,
            Action checkBlocked,
            Func<bool> isPlaying)
        {
            this.coroutineHost = coroutineHost;
            this.boardView = boardView;
            this.buses = buses;
            this.circulatingPassengerUnits = circulatingPassengerUnits;
            this.updateCounters = updateCounters;
            this.tryCompleteLevelIfReady = tryCompleteLevelIfReady;
            this.checkBlocked = checkBlocked;
            this.isPlaying = isPlaying;
        }

        public bool IsRunning => boardingRoutine != null;

        public bool HasPendingReservations => boardingCoordinator.HasPendingReservations;

        public void Reset()
        {
            Stop();
            boardingCoordinator.Clear();
        }

        public void Start()
        {
            if (boardingRoutine != null || !isPlaying())
            {
                return;
            }

            boardingRoutine = coroutineHost.StartCoroutine(BoardingResolverRoutine());
        }

        public void Stop()
        {
            if (boardingRoutine == null)
            {
                return;
            }

            coroutineHost.StopCoroutine(boardingRoutine);
            boardingRoutine = null;
        }

        public bool HasBusBoardingPassengers()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index].IsBoardingPassengers)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasStationBusReadyToDepart()
        {
            return TryFindDepartingBus(out _);
        }

        public bool HasStationBusReadyToBoardNow()
        {
            return boardingCoordinator.HasPendingReservations ||
                boardingCoordinator.CanReserveAny(boardView, buses, circulatingPassengerUnits);
        }

        public bool HasStationBusThatCanEventuallyBoard()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus.IsParkedAtStation && !bus.IsDeparted && bus.HasAvailableBoardingSeat && boardView.HasPassengerColor(circulatingPassengerUnits, bus.Color))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator BoardingResolverRoutine()
        {
            while (isPlaying())
            {
                boardingCoordinator.ReserveAvailable(boardView, buses, circulatingPassengerUnits);

                if (TryLaunchReadyDeparture())
                {
                    yield return new WaitForSeconds(DepartureLaunchInterval);
                    continue;
                }

                boardingCoordinator.ReserveAvailable(boardView, buses, circulatingPassengerUnits);

                if (boardingCoordinator.TryTakeReadyReservation(boardView, out var bus, out var passenger))
                {
                    circulatingPassengerUnits.Remove(passenger);
                    updateCounters();

                    bus.BoardReservedPassenger(passenger, updateCounters);

                    yield return new WaitForSeconds(BoardingUnitLaunchInterval);
                    continue;
                }

                if (boardingCoordinator.HasPendingReservations || HasBusBoardingPassengers())
                {
                    yield return null;
                    continue;
                }

                break;
            }

            boardingRoutine = null;

            if (tryCompleteLevelIfReady())
            {
                yield break;
            }

            checkBlocked();
        }

        private bool TryLaunchReadyDeparture()
        {
            if (!TryFindDepartingBus(out var departingBus))
            {
                return false;
            }

            var stationSlotIndex = departingBus.StationSlotIndex;
            var departureRoute = boardView.BuildRouteFromStation(departingBus);
            var stationReleased = false;

            void ReleaseStationOnce()
            {
                if (stationReleased)
                {
                    return;
                }

                stationReleased = true;
                boardView.ReleaseStationSlot(stationSlotIndex);
                updateCounters();
            }

            departingBus.Depart(
                departureRoute,
                ReleaseStationOnce,
                () =>
                {
                    ReleaseStationOnce();
                    updateCounters();

                    if (tryCompleteLevelIfReady())
                    {
                        return;
                    }

                    Start();
                    checkBlocked();
                });

            return true;
        }

        private bool TryFindDepartingBus(out BusView departingBus)
        {
            for (var slotIndex = 0; slotIndex < boardView.StationCapacity; slotIndex++)
            {
                for (var busIndex = 0; busIndex < buses.Count; busIndex++)
                {
                    var bus = buses[busIndex];
                    if (!IsReadyDepartureAtSlot(bus, slotIndex))
                    {
                        continue;
                    }

                    departingBus = bus;
                    return true;
                }
            }

            departingBus = null;
            return false;
        }

        private static bool IsReadyDepartureAtSlot(BusView bus, int slotIndex)
        {
            return bus != null &&
                bus.IsParkedAtStation &&
                !bus.IsDeparted &&
                !bus.IsDeparting &&
                !bus.IsMoving &&
                !bus.IsBoardingPassengers &&
                bus.IsFull &&
                bus.StationSlotIndex == slotIndex;
        }
    }
}
