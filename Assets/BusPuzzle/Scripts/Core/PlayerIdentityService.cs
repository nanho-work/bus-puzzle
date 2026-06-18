using System;
using System.Globalization;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

namespace BusPuzzle
{
    internal static class PlayerIdentityService
    {
        private const string NicknameKey = "bus_puzzle_player_nickname";
        private const string NicknamePromptSeenKey = "bus_puzzle_nickname_prompt_seen_v1";
        private const int MinNicknameDisplayWidth = 6;
        private const int MaxNicknameDisplayWidth = 16;

        private static FirebaseAuth auth;
        private static bool isInitializing;

        public static event Action IdentityUpdated;

        public static bool IsReady { get; private set; }
        public static string UserId { get; private set; } = string.Empty;
        public static string Nickname => GetOrCreateNickname();
        public static bool ShouldShowInitialNicknamePrompt => PlayerPrefs.GetInt(NicknamePromptSeenKey, 0) == 0;

        public static void Initialize()
        {
            GetOrCreateNickname();
            if (IsReady || isInitializing)
            {
                return;
            }

            isInitializing = true;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted || task.Result != DependencyStatus.Available)
                {
                    var reason = task.Exception?.GetBaseException().Message;
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        reason = task.IsCanceled
                            ? "Canceled"
                            : task.IsFaulted
                                ? "Faulted"
                                : task.Result.ToString();
                    }

                    Debug.LogWarning($"Firebase anonymous auth dependency check failed: {reason}");
                    FinishInitialization(null);
                    return;
                }

                auth = FirebaseAuth.DefaultInstance;
                if (auth.CurrentUser != null && !string.IsNullOrWhiteSpace(auth.CurrentUser.UserId))
                {
                    FinishInitialization(auth.CurrentUser);
                    return;
                }

                auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask =>
                {
                    if (authTask.IsCanceled || authTask.IsFaulted)
                    {
                        Debug.LogWarning(
                            $"Firebase anonymous auth sign-in failed: {authTask.Exception?.GetBaseException().Message ?? "Canceled"}");
                        FinishInitialization(null);
                        return;
                    }

                    FinishInitialization(authTask.Result.User);
                });
            });
        }

        public static void MarkInitialNicknamePromptSeen()
        {
            if (!ShouldShowInitialNicknamePrompt)
            {
                return;
            }

            PlayerPrefs.SetInt(NicknamePromptSeenKey, 1);
            PlayerPrefs.Save();
        }

        public static bool TrySetNickname(string nickname, out string normalizedNickname, out string validationMessage)
        {
            normalizedNickname = NormalizeNickname(nickname);
            if (!IsValidNickname(normalizedNickname, out validationMessage))
            {
                return false;
            }

            PlayerPrefs.SetString(NicknameKey, normalizedNickname);
            PlayerPrefs.Save();
            IdentityUpdated?.Invoke();
            return true;
        }

        private static void FinishInitialization(FirebaseUser user)
        {
            UserId = user != null ? user.UserId : string.Empty;
            IsReady = !string.IsNullOrWhiteSpace(UserId);
            isInitializing = false;

            if (IsReady)
            {
                Debug.Log($"Firebase anonymous auth ready. uid={UserId}");
            }

            IdentityUpdated?.Invoke();
        }

        private static string GetOrCreateNickname()
        {
            var nickname = NormalizeNickname(PlayerPrefs.GetString(NicknameKey, string.Empty));
            if (IsValidNickname(nickname, out _))
            {
                return nickname;
            }

            nickname = $"Player{UnityEngine.Random.Range(1000, 10000)}";
            PlayerPrefs.SetString(NicknameKey, nickname);
            PlayerPrefs.Save();
            return nickname;
        }

        private static string NormalizeNickname(string nickname)
        {
            return nickname == null ? string.Empty : nickname.Trim();
        }

        private static bool IsValidNickname(string nickname, out string validationMessage)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                validationMessage = "nickname_error_empty";
                return false;
            }

            if (ContainsDisallowedCharacter(nickname))
            {
                validationMessage = "nickname_error_unsupported";
                return false;
            }

            var displayWidth = GetDisplayWidth(nickname);
            if (displayWidth < MinNicknameDisplayWidth || displayWidth > MaxNicknameDisplayWidth)
            {
                validationMessage = "nickname_error_width";
                return false;
            }

            validationMessage = string.Empty;
            return true;
        }

        private static bool ContainsDisallowedCharacter(string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsSurrogate(character) || char.IsControl(character))
                {
                    return true;
                }

                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.OtherSymbol)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetDisplayWidth(string value)
        {
            var width = 0;
            for (var index = 0; index < value.Length; index++)
            {
                width += value[index] <= 0x7f ? 1 : 2;
            }

            return width;
        }
    }
}
