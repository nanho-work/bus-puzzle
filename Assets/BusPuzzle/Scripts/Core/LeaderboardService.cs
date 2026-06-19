using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.Functions;
using UnityEngine;

namespace BusPuzzle
{
    internal static class LeaderboardService
    {
        private const string FunctionsRegion = "asia-northeast3";
        private const string LocalMaxClearedStageKey = "bus_puzzle_local_max_cleared_stage";
        private const string LocalReachedAtUtcKey = "bus_puzzle_local_max_cleared_stage_reached_at_utc";
        private const string ServerSubmittedMaxClearedStageKey = "bus_puzzle_server_submitted_max_cleared_stage";
        private const string ServerSyncedNicknameKey = "bus_puzzle_server_synced_nickname";
        private const string CachedTopLeaderboardKey = "bus_puzzle_cached_top_leaderboard_v1";

        private static bool isInitialized;
        private static bool isSubmitting;
        private static bool isFetchingTopLeaderboard;
        private static IReadOnlyList<LeaderboardEntry> cachedTopLeaderboard;
        private static readonly List<Action<IReadOnlyList<LeaderboardEntry>>> PendingFetchCompletions =
            new List<Action<IReadOnlyList<LeaderboardEntry>>>();
        private static readonly List<Action<string>> PendingFetchFailures = new List<Action<string>>();

        public static int LocalMaxClearedStage => Mathf.Max(0, PlayerPrefs.GetInt(LocalMaxClearedStageKey, 0));

        [Serializable]
        private sealed class CachedLeaderboardPayload
        {
            public string fetchedAtUtc;
            public List<CachedLeaderboardEntry> entries = new List<CachedLeaderboardEntry>();
        }

        [Serializable]
        private sealed class CachedLeaderboardEntry
        {
            public string userId;
            public int rank;
            public string nickname;
            public int maxClearedStage;
        }

        public sealed class LeaderboardEntry
        {
            public LeaderboardEntry(string userId, int rank, string nickname, int maxClearedStage)
            {
                UserId = string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim();
                Rank = rank;
                Nickname = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname.Trim();
                MaxClearedStage = Mathf.Max(0, maxClearedStage);
            }

            public string UserId { get; }
            public int Rank { get; }
            public string Nickname { get; }
            public int MaxClearedStage { get; }
        }

        public static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            PlayerIdentityService.IdentityUpdated += HandleIdentityUpdated;
            HandleIdentityUpdated();
        }

        public static bool RecordStageClear(int clearedStageNumber)
        {
            if (clearedStageNumber <= 0)
            {
                Debug.LogWarning($"Leaderboard record ignored. Invalid cleared stage={clearedStageNumber}");
                return false;
            }

            var previousMaxClearedStage = LocalMaxClearedStage;
            if (clearedStageNumber <= previousMaxClearedStage)
            {
                SubmitPendingRecord();
                return false;
            }

            var reachedAtUtc = DateTime.UtcNow.ToString("O");
            PlayerPrefs.SetInt(LocalMaxClearedStageKey, clearedStageNumber);
            PlayerPrefs.SetString(LocalReachedAtUtcKey, reachedAtUtc);
            PlayerPrefs.Save();

            var userId = string.IsNullOrWhiteSpace(PlayerIdentityService.UserId)
                ? "pending"
                : PlayerIdentityService.UserId;
            Debug.Log(
                $"Leaderboard local max cleared stage updated. stage={clearedStageNumber}, uid={userId}, nickname={PlayerIdentityService.Nickname}, reachedAtUtc={reachedAtUtc}");
            SubmitPendingRecord();
            return true;
        }

        private static void SubmitPendingRecord()
        {
            if (isSubmitting || LocalMaxClearedStage <= 0 || !PlayerIdentityService.IsReady)
            {
                return;
            }

            var nickname = PlayerIdentityService.Nickname;
            var serverSubmittedStage = Mathf.Max(0, PlayerPrefs.GetInt(ServerSubmittedMaxClearedStageKey, 0));
            var serverSyncedNickname = PlayerPrefs.GetString(ServerSyncedNicknameKey, string.Empty);
            if (serverSubmittedStage >= LocalMaxClearedStage && serverSyncedNickname == nickname)
            {
                return;
            }

            isSubmitting = true;
            var data = new Dictionary<string, object>
            {
                { "stage", LocalMaxClearedStage },
                { "nickname", nickname },
                { "platform", Application.platform.ToString() },
                { "appVersion", Application.version }
            };

            FirebaseFunctions.GetInstance(FunctionsRegion)
                .GetHttpsCallable("submitStageClear")
                .CallAsync(data)
                .ContinueWithOnMainThread(task =>
                {
                    isSubmitting = false;
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogWarning(
                            $"Leaderboard server sync failed: {task.Exception?.GetBaseException().Message ?? "Canceled"}");
                        return;
                    }

                    PlayerPrefs.SetInt(ServerSubmittedMaxClearedStageKey, LocalMaxClearedStage);
                    PlayerPrefs.SetString(ServerSyncedNicknameKey, nickname);
                    PlayerPrefs.Save();
                    Debug.Log(
                        $"Leaderboard server sync completed. stage={LocalMaxClearedStage}, nickname={nickname}");
                });
        }

        public static bool TryGetCachedTopLeaderboard(out IReadOnlyList<LeaderboardEntry> entries)
        {
            entries = cachedTopLeaderboard;
            if (entries != null && entries.Count > 0)
            {
                return true;
            }

            var json = PlayerPrefs.GetString(CachedTopLeaderboardKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                var payload = JsonUtility.FromJson<CachedLeaderboardPayload>(json);
                if (payload?.entries == null || payload.entries.Count == 0)
                {
                    return false;
                }

                var restoredEntries = new List<LeaderboardEntry>(payload.entries.Count);
                foreach (var cachedEntry in payload.entries)
                {
                    if (cachedEntry == null || cachedEntry.rank <= 0 || cachedEntry.maxClearedStage <= 0)
                    {
                        continue;
                    }

                    restoredEntries.Add(new LeaderboardEntry(
                        cachedEntry.userId,
                        cachedEntry.rank,
                        cachedEntry.nickname,
                        cachedEntry.maxClearedStage));
                }

                if (restoredEntries.Count == 0)
                {
                    return false;
                }

                cachedTopLeaderboard = restoredEntries;
                entries = cachedTopLeaderboard;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Leaderboard cache load failed: {exception.Message}");
                return false;
            }
        }

        public static void PrefetchTopLeaderboard()
        {
            if (!PlayerIdentityService.IsReady)
            {
                return;
            }

            FetchTopLeaderboard(null, null);
        }

        public static void FetchTopLeaderboard(
            Action<IReadOnlyList<LeaderboardEntry>> onCompleted,
            Action<string> onFailed)
        {
            if (onCompleted != null)
            {
                PendingFetchCompletions.Add(onCompleted);
            }

            if (onFailed != null)
            {
                PendingFetchFailures.Add(onFailed);
            }

            if (isFetchingTopLeaderboard)
            {
                return;
            }

            isFetchingTopLeaderboard = true;
            FirebaseDependencyService.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
                {
                    var reason = task.Exception?.GetBaseException().Message;
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        reason = task.IsCanceled ? "Canceled" : task.Result.ToString();
                    }

                    Debug.LogWarning($"Leaderboard dependency check failed: {reason}");
                    CompleteLeaderboardFetchFailure(reason);
                    return;
                }

                FetchTopLeaderboardFromServer();
            });
        }

        private static void FetchTopLeaderboardFromServer()
        {
            FirebaseFunctions.GetInstance(FunctionsRegion)
                .GetHttpsCallable("getTopLeaderboard")
                .CallAsync(new Dictionary<string, object>())
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        var message = task.Exception?.GetBaseException().Message ?? "Canceled";
                        Debug.LogWarning($"Leaderboard fetch failed: {message}");
                        CompleteLeaderboardFetchFailure(message);
                        return;
                    }

                    var entries = ParseLeaderboardEntries(task.Result.Data);
                    SaveCachedTopLeaderboard(entries);
                    CompleteLeaderboardFetchSuccess(entries);
                });
        }

        private static void HandleIdentityUpdated()
        {
            SubmitPendingRecord();
            PrefetchTopLeaderboard();
        }

        private static void CompleteLeaderboardFetchSuccess(IReadOnlyList<LeaderboardEntry> entries)
        {
            isFetchingTopLeaderboard = false;
            var callbacks = new List<Action<IReadOnlyList<LeaderboardEntry>>>(PendingFetchCompletions);
            PendingFetchCompletions.Clear();
            PendingFetchFailures.Clear();

            foreach (var callback in callbacks)
            {
                callback?.Invoke(entries);
            }
        }

        private static void CompleteLeaderboardFetchFailure(string message)
        {
            isFetchingTopLeaderboard = false;
            var callbacks = new List<Action<string>>(PendingFetchFailures);
            PendingFetchCompletions.Clear();
            PendingFetchFailures.Clear();

            foreach (var callback in callbacks)
            {
                callback?.Invoke(message);
            }
        }

        private static void SaveCachedTopLeaderboard(IReadOnlyList<LeaderboardEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                cachedTopLeaderboard = null;
                PlayerPrefs.DeleteKey(CachedTopLeaderboardKey);
                PlayerPrefs.Save();
                return;
            }

            var payload = new CachedLeaderboardPayload
            {
                fetchedAtUtc = DateTime.UtcNow.ToString("O"),
                entries = new List<CachedLeaderboardEntry>(entries.Count)
            };

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                payload.entries.Add(new CachedLeaderboardEntry
                {
                    userId = entry.UserId,
                    rank = entry.Rank,
                    nickname = entry.Nickname,
                    maxClearedStage = entry.MaxClearedStage
                });
            }

            cachedTopLeaderboard = entries;
            PlayerPrefs.SetString(CachedTopLeaderboardKey, JsonUtility.ToJson(payload));
            PlayerPrefs.Save();
        }

        private static IReadOnlyList<LeaderboardEntry> ParseLeaderboardEntries(object data)
        {
            var entries = new List<LeaderboardEntry>();
            if (!TryGetDictionaryValue(data, "entries", out var rawEntries) || rawEntries is string)
            {
                return entries;
            }

            if (!(rawEntries is IEnumerable enumerableEntries))
            {
                return entries;
            }

            foreach (var rawEntry in enumerableEntries)
            {
                if (!TryReadInt(rawEntry, "rank", out var rank) ||
                    !TryReadInt(rawEntry, "maxClearedStage", out var maxClearedStage))
                {
                    continue;
                }

                var nickname = TryGetDictionaryValue(rawEntry, "nickname", out var rawNickname)
                    ? rawNickname as string
                    : string.Empty;
                var userId = TryGetDictionaryValue(rawEntry, "uid", out var rawUserId)
                    ? rawUserId as string
                    : string.Empty;
                entries.Add(new LeaderboardEntry(userId, rank, nickname, maxClearedStage));
            }

            return entries;
        }

        private static bool TryReadInt(object source, string key, out int value)
        {
            value = 0;
            if (!TryGetDictionaryValue(source, key, out var rawValue))
            {
                return false;
            }

            switch (rawValue)
            {
                case int intValue:
                    value = intValue;
                    return true;
                case long longValue:
                    if (longValue <= int.MinValue)
                    {
                        value = int.MinValue;
                        return true;
                    }

                    if (longValue >= int.MaxValue)
                    {
                        value = int.MaxValue;
                        return true;
                    }

                    value = (int)longValue;
                    return true;
                case double doubleValue:
                    value = Mathf.RoundToInt((float)doubleValue);
                    return true;
                case string stringValue when int.TryParse(stringValue, out var parsedValue):
                    value = parsedValue;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetDictionaryValue(object source, string key, out object value)
        {
            value = null;
            if (source is IDictionary<string, object> typedDictionary &&
                typedDictionary.TryGetValue(key, out value))
            {
                return true;
            }

            if (source is IDictionary dictionary && dictionary.Contains(key))
            {
                value = dictionary[key];
                return true;
            }

            return false;
        }
    }
}
