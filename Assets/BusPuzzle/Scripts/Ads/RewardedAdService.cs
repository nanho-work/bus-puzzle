using System;

namespace BusPuzzle
{
    public enum RewardedAdPlacement
    {
        StationSlotUnlock,
        VipBusTeleport,
        BusColorShuffle
    }

    internal enum RewardedAdResult
    {
        RewardEarned,
        NotReady,
        ClosedWithoutReward,
        Failed
    }

    internal interface IRewardedAdService
    {
        event Action AvailabilityChanged;

        bool IsReady { get; }
        string CurrentAdUnitId { get; }

        bool IsReadyFor(RewardedAdPlacement placement);
        string GetAdUnitId(RewardedAdPlacement placement);
        void Initialize();
        void Preload();
        void Preload(RewardedAdPlacement placement);
        bool ShowStationSlotUnlockAd(Action<RewardedAdResult> onCompleted);
        bool ShowVipBusTeleportAd(Action<RewardedAdResult> onCompleted);
        bool ShowBusColorShuffleAd(Action<RewardedAdResult> onCompleted);
    }
}
