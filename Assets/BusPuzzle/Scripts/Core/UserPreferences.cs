using UnityEngine;

namespace BusPuzzle
{
    internal static class UserPreferences
    {
        private const string EffectSoundKey = "bus_puzzle_effect_sound";
        private const string MainSoundKey = "bus_puzzle_main_sound";
        private const string VibrationKey = "bus_puzzle_vibration";
        private const string LanguageCodeKey = "bus_puzzle_language_code";

        public static bool EffectSoundEnabled
        {
            get => GetBool(EffectSoundKey, true);
            set => SetBool(EffectSoundKey, value);
        }

        public static bool MainSoundEnabled
        {
            get => GetBool(MainSoundKey, true);
            set => SetBool(MainSoundKey, value);
        }

        public static bool VibrationEnabled
        {
            get => GetBool(VibrationKey, true);
            set => SetBool(VibrationKey, value);
        }

        public static string LanguageCode
        {
            get => PlayerPrefs.GetString(LanguageCodeKey, string.Empty);
            set
            {
                PlayerPrefs.SetString(LanguageCodeKey, value ?? string.Empty);
                PlayerPrefs.Save();
            }
        }

        private static bool GetBool(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
