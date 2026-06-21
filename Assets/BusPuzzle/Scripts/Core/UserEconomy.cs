using UnityEngine;

namespace BusPuzzle
{
    internal static class UserEconomy
    {
        private const string GoldBalanceKey = "bus_puzzle_gold_balance";
        private const string AdSkipTicketBalanceKey = "bus_puzzle_ad_skip_ticket_balance";
        private const string StageClearGoldClaimedPrefix = "bus_puzzle_stage_clear_gold_claimed_";

        public static int GoldBalance => Mathf.Max(0, PlayerPrefs.GetInt(GoldBalanceKey, 0));
        public static int AdSkipTicketBalance => Mathf.Max(0, PlayerPrefs.GetInt(AdSkipTicketBalanceKey, 0));

        public static bool CanSpendGold(int amount)
        {
            return amount > 0 && GoldBalance >= amount;
        }

        public static void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            PlayerPrefs.SetInt(GoldBalanceKey, GoldBalance + amount);
            PlayerPrefs.Save();
        }

        public static bool TrySpendGold(int amount)
        {
            if (!CanSpendGold(amount))
            {
                return false;
            }

            PlayerPrefs.SetInt(GoldBalanceKey, GoldBalance - amount);
            PlayerPrefs.Save();
            return true;
        }

        public static void AddAdSkipTickets(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            PlayerPrefs.SetInt(AdSkipTicketBalanceKey, AdSkipTicketBalance + amount);
            PlayerPrefs.Save();
        }

        public static bool TryUseAdSkipTicket()
        {
            if (AdSkipTicketBalance <= 0)
            {
                return false;
            }

            PlayerPrefs.SetInt(AdSkipTicketBalanceKey, AdSkipTicketBalance - 1);
            PlayerPrefs.Save();
            return true;
        }

        public static bool TryGrantStageClearGold(int stageNumber, int amount)
        {
            if (stageNumber <= 0 || amount <= 0)
            {
                return false;
            }

            var claimedKey = GetStageClearGoldClaimedKey(stageNumber);
            if (PlayerPrefs.GetInt(claimedKey, 0) != 0)
            {
                return false;
            }

            PlayerPrefs.SetInt(claimedKey, 1);
            PlayerPrefs.SetInt(GoldBalanceKey, GoldBalance + amount);
            PlayerPrefs.Save();
            return true;
        }

        private static string GetStageClearGoldClaimedKey(int stageNumber)
        {
            return $"{StageClearGoldClaimedPrefix}{stageNumber}";
        }
    }
}
