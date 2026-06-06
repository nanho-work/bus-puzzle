using UnityEngine;

namespace BusPuzzle
{
    internal static class UserProgress
    {
        private const string LastStageIndexKey = "bus_puzzle_last_stage_index";

        public static int GetLastStageIndex(int stageCount)
        {
            if (stageCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(PlayerPrefs.GetInt(LastStageIndexKey, 0), 0, stageCount - 1);
        }

        public static void SaveLastStageIndex(int stageIndex, int stageCount)
        {
            if (stageCount <= 0)
            {
                return;
            }

            var clampedStageIndex = Mathf.Clamp(stageIndex, 0, stageCount - 1);
            if (PlayerPrefs.GetInt(LastStageIndexKey, -1) == clampedStageIndex)
            {
                return;
            }

            PlayerPrefs.SetInt(LastStageIndexKey, clampedStageIndex);
            PlayerPrefs.Save();
        }
    }
}
