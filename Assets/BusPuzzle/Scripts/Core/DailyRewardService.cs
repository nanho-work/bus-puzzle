using System;
using UnityEngine;

namespace BusPuzzle
{
    internal enum DailyRewardType
    {
        Gold,
        AdSkipTicket
    }

    internal readonly struct DailyReward
    {
        public readonly DailyRewardType Type;
        public readonly int Amount;
        public readonly int CycleDay;

        public DailyReward(DailyRewardType type, int amount, int cycleDay)
        {
            Type = type;
            Amount = amount;
            CycleDay = cycleDay;
        }
    }

    internal static class DailyRewardService
    {
        private const string LastClaimDateKey = "bus_puzzle_daily_reward_last_claim_date_v1";
        private const string ClaimCursorKey = "bus_puzzle_daily_reward_claim_cursor_v1";
        private const string LastClaimRewardTypeKey = "bus_puzzle_daily_reward_last_claim_type_v1";
        private const string LastClaimRewardAmountKey = "bus_puzzle_daily_reward_last_claim_amount_v1";
        private const string LastClaimRewardCycleDayKey = "bus_puzzle_daily_reward_last_claim_cycle_day_v1";

        private static readonly DailyReward[] RewardCycle =
        {
            new DailyReward(DailyRewardType.AdSkipTicket, 1, 1),
            new DailyReward(DailyRewardType.Gold, 30, 2),
            new DailyReward(DailyRewardType.AdSkipTicket, 1, 3),
            new DailyReward(DailyRewardType.Gold, 40, 4),
            new DailyReward(DailyRewardType.Gold, 50, 5),
            new DailyReward(DailyRewardType.AdSkipTicket, 2, 6),
            new DailyReward(DailyRewardType.Gold, 80, 7)
        };

        public static bool CanClaimToday =>
            !string.Equals(
                PlayerPrefs.GetString(LastClaimDateKey, string.Empty),
                GetTodayKey(),
                StringComparison.Ordinal);

        public static DailyReward PeekTodayReward()
        {
            var cursor = Mathf.Max(0, PlayerPrefs.GetInt(ClaimCursorKey, 0));
            var cycleIndex = cursor % RewardCycle.Length;
            return RewardCycle[cycleIndex];
        }

        public static DailyReward GetDisplayReward()
        {
            if (CanClaimToday)
            {
                return PeekTodayReward();
            }

            var amount = Mathf.Max(0, PlayerPrefs.GetInt(LastClaimRewardAmountKey, 0));
            if (amount <= 0)
            {
                return PeekTodayReward();
            }

            var rewardTypeValue = PlayerPrefs.GetInt(LastClaimRewardTypeKey, (int)DailyRewardType.Gold);
            var rewardType = rewardTypeValue == (int)DailyRewardType.AdSkipTicket
                ? DailyRewardType.AdSkipTicket
                : DailyRewardType.Gold;
            var cycleDay = Mathf.Max(1, PlayerPrefs.GetInt(LastClaimRewardCycleDayKey, 1));
            return new DailyReward(rewardType, amount, cycleDay);
        }

        public static bool TryClaimToday(out DailyReward reward)
        {
            reward = default;
            if (!CanClaimToday)
            {
                return false;
            }

            reward = PeekTodayReward();
            GrantReward(reward);

            var cursor = Mathf.Max(0, PlayerPrefs.GetInt(ClaimCursorKey, 0));
            PlayerPrefs.SetString(LastClaimDateKey, GetTodayKey());
            PlayerPrefs.SetInt(ClaimCursorKey, cursor + 1);
            PlayerPrefs.SetInt(LastClaimRewardTypeKey, (int)reward.Type);
            PlayerPrefs.SetInt(LastClaimRewardAmountKey, reward.Amount);
            PlayerPrefs.SetInt(LastClaimRewardCycleDayKey, reward.CycleDay);
            PlayerPrefs.Save();
            return true;
        }

        private static void GrantReward(DailyReward reward)
        {
            switch (reward.Type)
            {
                case DailyRewardType.Gold:
                    UserEconomy.AddGold(reward.Amount);
                    break;
                case DailyRewardType.AdSkipTicket:
                    UserEconomy.AddAdSkipTickets(reward.Amount);
                    break;
            }
        }

        private static string GetTodayKey()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }
    }
}
