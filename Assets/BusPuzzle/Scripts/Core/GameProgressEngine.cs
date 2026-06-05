namespace BusPuzzle
{
    internal enum GameProgressDecision
    {
        Continue,
        Complete,
        StartBoardingResolver,
        Fail
    }

    internal readonly struct GameProgressSnapshot
    {
        public readonly bool IsPlaying;
        public readonly bool HasBoardingResolver;
        public readonly int PassengerUnitCount;
        public readonly bool HasPendingReservations;
        public readonly bool HasBoardingPassengers;
        public readonly bool HasMovingBus;
        public readonly bool HasReadyDeparture;
        public readonly bool HasReadyBoardingNow;
        public readonly bool HasStationBusThatCanBoardRotaryPassenger;
        public readonly bool IsStationFull;
        public readonly bool HasAnyMoveAvailable;

        public GameProgressSnapshot(
            bool isPlaying,
            bool hasBoardingResolver,
            int passengerUnitCount,
            bool hasPendingReservations,
            bool hasBoardingPassengers,
            bool hasMovingBus,
            bool hasReadyDeparture,
            bool hasReadyBoardingNow,
            bool hasStationBusThatCanBoardRotaryPassenger,
            bool isStationFull,
            bool hasAnyMoveAvailable)
        {
            IsPlaying = isPlaying;
            HasBoardingResolver = hasBoardingResolver;
            PassengerUnitCount = passengerUnitCount;
            HasPendingReservations = hasPendingReservations;
            HasBoardingPassengers = hasBoardingPassengers;
            HasMovingBus = hasMovingBus;
            HasReadyDeparture = hasReadyDeparture;
            HasReadyBoardingNow = hasReadyBoardingNow;
            HasStationBusThatCanBoardRotaryPassenger = hasStationBusThatCanBoardRotaryPassenger;
            IsStationFull = isStationFull;
            HasAnyMoveAvailable = hasAnyMoveAvailable;
        }
    }

    internal static class GameProgressEngine
    {
        public static bool ShouldStartBoardingResolver(
            bool isPlaying,
            bool hasBoardingResolver,
            bool hasReadyBoardingNow,
            bool hasReadyDeparture)
        {
            return isPlaying && !hasBoardingResolver && (hasReadyBoardingNow || hasReadyDeparture);
        }

        public static bool CanComplete(GameProgressSnapshot snapshot)
        {
            return snapshot.IsPlaying &&
                snapshot.PassengerUnitCount == 0 &&
                !snapshot.HasPendingReservations &&
                !snapshot.HasBoardingPassengers &&
                !snapshot.HasMovingBus &&
                !snapshot.HasReadyDeparture;
        }

        public static GameProgressDecision EvaluateBlockedState(GameProgressSnapshot snapshot)
        {
            if (CanComplete(snapshot))
            {
                return GameProgressDecision.Complete;
            }

            if (!snapshot.IsPlaying ||
                snapshot.PassengerUnitCount == 0 ||
                snapshot.HasBoardingResolver ||
                snapshot.HasMovingBus ||
                snapshot.HasBoardingPassengers ||
                snapshot.HasPendingReservations)
            {
                return GameProgressDecision.Continue;
            }

            if (snapshot.HasReadyBoardingNow || snapshot.HasReadyDeparture)
            {
                return GameProgressDecision.StartBoardingResolver;
            }

            if (snapshot.IsStationFull && !snapshot.HasStationBusThatCanBoardRotaryPassenger)
            {
                return GameProgressDecision.Fail;
            }

            if (snapshot.HasAnyMoveAvailable || snapshot.HasStationBusThatCanBoardRotaryPassenger)
            {
                return GameProgressDecision.Continue;
            }

            return GameProgressDecision.Fail;
        }
    }
}
