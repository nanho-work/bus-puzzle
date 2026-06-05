using System;
using System.Collections.Generic;

namespace BusPuzzle
{
    internal sealed class VehicleDispatchController
    {
        private readonly BoardView boardView;
        private readonly GameUiController uiController;
        private readonly IReadOnlyList<BusView> buses;
        private readonly Action updateCounters;
        private readonly Action startBoardingResolver;
        private readonly Action checkBlocked;
        private readonly Func<string> getCurrentLevelName;

        public VehicleDispatchController(
            BoardView boardView,
            GameUiController uiController,
            IReadOnlyList<BusView> buses,
            Action updateCounters,
            Action startBoardingResolver,
            Action checkBlocked,
            Func<string> getCurrentLevelName)
        {
            this.boardView = boardView;
            this.uiController = uiController;
            this.buses = buses;
            this.updateCounters = updateCounters;
            this.startBoardingResolver = startBoardingResolver;
            this.checkBlocked = checkBlocked;
            this.getCurrentLevelName = getCurrentLevelName;
        }

        public bool TryLaunch(BusView bus)
        {
            if (!CanLaunchBus(bus))
            {
                return false;
            }

            if (!boardView.TryReserveStationSlot(out var stationSlotIndex, out var stationPosition))
            {
                uiController.ShowInvalid("Station full");
                checkBlocked();
                return false;
            }

            if (!boardView.IsPathClear(bus, buses, out var blockingBus, out var collisionPosition))
            {
                boardView.ReleaseStationSlot(stationSlotIndex);
                updateCounters();

                uiController.ShowInvalid("Blocked");
                bus.PlayBlockedCollision(collisionPosition, boardView.GetWorldDirection(bus), blockingBus, checkBlocked);
                return false;
            }

            updateCounters();
            uiController.ShowInvalid($"{PuzzlePalette.DisplayName(bus.Color)} bus dispatched");

            var route = boardView.BuildRouteToStation(bus, stationPosition);
            var counterPosition = boardView.GetStationCounterPosition(stationSlotIndex);
            bus.MoveToStation(route, stationSlotIndex, counterPosition, () =>
            {
                updateCounters();
                uiController.ShowPlaying(getCurrentLevelName());
                startBoardingResolver();
                checkBlocked();
            });

            return true;
        }

        private static bool CanLaunchBus(BusView bus)
        {
            return bus != null && bus.IsOnBoard && !bus.IsMoving && !bus.IsDeparted;
        }
    }
}
