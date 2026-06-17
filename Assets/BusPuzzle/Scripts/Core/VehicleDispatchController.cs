using System;
using System.Collections.Generic;

namespace BusPuzzle
{
    internal sealed class VehicleDispatchController
    {
        private readonly BoardView boardView;
        private readonly GameUiController uiController;
        private readonly List<BusView> buses;
        private readonly Action updateCounters;
        private readonly Action revealConcealedVehicles;
        private readonly Action startBoardingResolver;
        private readonly Action checkBlocked;
        private readonly Func<string> getCurrentLevelName;

        public VehicleDispatchController(
            BoardView boardView,
            GameUiController uiController,
            List<BusView> buses,
            Action updateCounters,
            Action revealConcealedVehicles,
            Action startBoardingResolver,
            Action checkBlocked,
            Func<string> getCurrentLevelName)
        {
            this.boardView = boardView;
            this.uiController = uiController;
            this.buses = buses;
            this.updateCounters = updateCounters;
            this.revealConcealedVehicles = revealConcealedVehicles;
            this.startBoardingResolver = startBoardingResolver;
            this.checkBlocked = checkBlocked;
            this.getCurrentLevelName = getCurrentLevelName;
        }

        public bool TryLaunch(BusView bus)
        {
            if (bus != null && bus.IsConcealed)
            {
                uiController.ShowInvalid(Localization.Text("status_mystery_bus"));
                checkBlocked();
                return false;
            }

            if (!CanLaunchBus(bus))
            {
                return false;
            }

            if (!boardView.TryReserveStationSlot(out var stationSlotIndex, out var stationPosition))
            {
                uiController.ShowInvalid(Localization.Text("status_station_full"));
                checkBlocked();
                return false;
            }

            if (!boardView.IsPathClear(bus, buses, out var blockingBus, out var collisionPosition))
            {
                boardView.ReleaseStationSlot(stationSlotIndex);
                updateCounters();

                uiController.ShowInvalid(Localization.Text("status_blocked"));
                bus.PlayBlockedCollision(collisionPosition, boardView.GetWorldDirection(bus), blockingBus, checkBlocked);
                return false;
            }

            updateCounters();
            uiController.ShowInvalid(Localization.Text("status_bus_dispatched", Localization.ColorName(bus.Color)));
            EffectAudioPlayer.PlayVehicleLaunch();
            HapticFeedback.PlayVehicleLaunch();

            var route = boardView.BuildRouteToStation(bus, stationPosition);
            var counterPosition = boardView.GetStationCounterPosition(stationSlotIndex);
            var garageAdvanced = false;
            void AdvanceGarageOnce()
            {
                if (garageAdvanced)
                {
                    return;
                }

                garageAdvanced = true;
                if (boardView.TryAdvanceGarageAfterLaunch(bus, buses))
                {
                    updateCounters();
                }

                revealConcealedVehicles?.Invoke();
            }

            bus.MoveToStation(
                route,
                stationSlotIndex,
                counterPosition,
                () =>
                {
                    revealConcealedVehicles?.Invoke();
                    updateCounters();
                    uiController.ShowPlaying(getCurrentLevelName());
                    startBoardingResolver();
                    checkBlocked();
                },
                AdvanceGarageOnce);

            return true;
        }

        public bool TryVipTeleport(BusView bus)
        {
            if (bus != null && bus.IsConcealed)
            {
                uiController.ShowInvalid(Localization.Text("status_mystery_bus"));
                checkBlocked();
                return false;
            }

            if (!CanLaunchBus(bus))
            {
                return false;
            }

            if (!boardView.TryReserveVipStationSlot(out var stationSlotIndex, out var stationPosition))
            {
                uiController.ShowInvalid(Localization.Text("status_vip_busy"));
                checkBlocked();
                return false;
            }

            updateCounters();
            uiController.ShowInvalid(Localization.Text("status_bus_vip", Localization.ColorName(bus.Color)));

            var counterPosition = boardView.GetStationCounterPosition(stationSlotIndex);
            bus.TeleportToStation(
                stationPosition,
                boardView.ActiveStationRotation,
                stationSlotIndex,
                counterPosition,
                () =>
                {
                    revealConcealedVehicles?.Invoke();
                    updateCounters();
                    uiController.ShowPlaying(getCurrentLevelName());
                    startBoardingResolver();
                    checkBlocked();
                });
            if (boardView.TryAdvanceGarageAfterLaunch(bus, buses))
            {
                updateCounters();
            }

            revealConcealedVehicles?.Invoke();
            return true;
        }

        private static bool CanLaunchBus(BusView bus)
        {
            return bus != null && bus.IsOnBoard && !bus.IsConcealed && !bus.IsMoving && !bus.IsDeparted;
        }
    }
}
