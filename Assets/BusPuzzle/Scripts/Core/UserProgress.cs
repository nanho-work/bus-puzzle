using UnityEngine;

namespace BusPuzzle
{
    internal static class UserProgress
    {
        private const string LastStageIndexKey = "bus_puzzle_last_stage_index";
        private const string LastActivatedStageIndexKey = "bus_puzzle_last_activated_stage_index_v1";
        private const string TutorialCompletedKey = "bus_puzzle_tutorial_completed_v1";

        public static bool HasCompletedTutorial => PlayerPrefs.GetInt(TutorialCompletedKey, 0) != 0;

        public static int GetLastStageIndex(int stageCount)
        {
            if (stageCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(PlayerPrefs.GetInt(LastStageIndexKey, 0), 0, stageCount - 1);
        }

        public static int GetLastActivatedStageIndex(int stageCount)
        {
            if (stageCount <= 0)
            {
                return -1;
            }

            var savedStageIndex = PlayerPrefs.GetInt(
                LastActivatedStageIndexKey,
                -1);
            return savedStageIndex < 0
                ? -1
                : Mathf.Clamp(savedStageIndex, 0, stageCount - 1);
        }

        public static void SaveLastStageIndex(int stageIndex, int stageCount)
        {
            if (stageCount <= 0)
            {
                return;
            }

            var clampedStageIndex = Mathf.Clamp(stageIndex, 0, stageCount - 1);
            SaveStageIndex(clampedStageIndex);
        }

        public static bool SavePreparedStageIndex(int stageIndex, int stageCount)
        {
            if (stageCount <= 0 || stageIndex < 0 || stageIndex >= stageCount)
            {
                return false;
            }

            SaveStageIndex(stageIndex);
            return true;
        }

        public static bool SaveActivatedStageIndex(int stageIndex, int stageCount)
        {
            if (stageCount <= 0 || stageIndex < 0 || stageIndex >= stageCount)
            {
                return false;
            }

            var savedStageIndex = PlayerPrefs.GetInt(
                LastActivatedStageIndexKey,
                -1);
            if (savedStageIndex >= stageIndex)
            {
                return true;
            }

            PlayerPrefs.SetInt(LastActivatedStageIndexKey, stageIndex);
            PlayerPrefs.Save();
            return true;
        }

        private static void SaveStageIndex(int stageIndex)
        {
            var savedStageIndex = PlayerPrefs.GetInt(LastStageIndexKey, -1);
            if (savedStageIndex >= stageIndex)
            {
                return;
            }

            PlayerPrefs.SetInt(LastStageIndexKey, stageIndex);
            PlayerPrefs.Save();
        }

        public static void MarkTutorialCompleted()
        {
            if (HasCompletedTutorial)
            {
                return;
            }

            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }
    }
}
