using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private void BuildClearPrompt()
        {
            clearPrompt = CreatePanel("Clear Prompt", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(clearPrompt, new Vector2(0.12f, 0.37f), new Vector2(0.88f, 0.58f), Vector2.zero, Vector2.zero);

            clearPromptText = CreateText("Clear Prompt Text", clearPrompt, TextAnchor.MiddleCenter, 36, FontStyle.Bold);
            SetAnchors(clearPromptText.rectTransform, new Vector2(0f, 0.43f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var homeButton = CreateButton("Clear Home Button", clearPrompt, "Home", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(homeButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.43f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            homeButton.onClick.AddListener(() =>
            {
                HideClearPrompt();
                HomeRequested?.Invoke();
            });

            nextButton = CreateButton("Clear Next Button", clearPrompt, "Next", new Color(0.12f, 0.42f, 0.78f));
            SetAnchors(nextButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.43f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            nextButton.onClick.AddListener(() =>
            {
                HideClearPrompt();
                NextLevelRequested?.Invoke();
            });

            HideClearPrompt();
        }

        private void ShowClearPrompt(int levelNumber, bool hasNextLevel, int goldReward)
        {
            if (clearPrompt == null)
            {
                return;
            }

            clearPrompt.gameObject.SetActive(true);
            if (clearPromptText != null)
            {
                clearPromptText.text = goldReward > 0
                    ? $"Level {levelNumber} Clear\n+{goldReward} Gold"
                    : $"Level {levelNumber} Clear";
            }

            if (nextButton != null)
            {
                nextButton.interactable = hasNextLevel;
            }
        }

        private void HideClearPrompt()
        {
            if (clearPrompt != null)
            {
                clearPrompt.gameObject.SetActive(false);
            }
        }

        private void BuildStationUnlockPrompt()
        {
            stationUnlockPrompt = CreatePanel("Station Unlock Prompt", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(stationUnlockPrompt, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.56f), Vector2.zero, Vector2.zero);

            stationUnlockPromptText = CreateText("Station Unlock Prompt Text", stationUnlockPrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(stationUnlockPromptText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Station Unlock Cancel Button", stationUnlockPrompt, "Cancel", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(HideStationUnlockPrompt);

            stationUnlockConfirmButton = CreateButton("Station Unlock Confirm Button", stationUnlockPrompt, "Watch", new Color(0.10f, 0.48f, 0.30f));
            SetAnchors(stationUnlockConfirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            stationUnlockConfirmButton.onClick.AddListener(() =>
            {
                HideStationUnlockPrompt();
                StationUnlockConfirmed?.Invoke();
            });

            HideStationUnlockPrompt();
        }

        private void BuildVipTeleportPrompt()
        {
            vipTeleportPrompt = CreatePanel("VIP Teleport Prompt", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(vipTeleportPrompt, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.56f), Vector2.zero, Vector2.zero);

            vipTeleportPromptText = CreateText("VIP Teleport Prompt Text", vipTeleportPrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(vipTeleportPromptText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("VIP Teleport Cancel Button", vipTeleportPrompt, "Cancel", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(18f, 16f), new Vector2(-8f, -12f));
            cancelButton.onClick.AddListener(HideVipTeleportPrompt);

            vipTeleportConfirmButton = CreateButton("VIP Teleport Confirm Button", vipTeleportPrompt, "Watch", new Color(0.82f, 0.58f, 0.08f));
            SetAnchors(vipTeleportConfirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 16f), new Vector2(-18f, -12f));
            vipTeleportConfirmButton.onClick.AddListener(() =>
            {
                HideVipTeleportPrompt();
                VipTeleportConfirmed?.Invoke();
            });

            HideVipTeleportPrompt();
        }

        private void BuildMixShufflePrompt()
        {
            mixShufflePrompt = CreatePanel("Mix Shuffle Prompt", transform, new Color(0.08f, 0.10f, 0.13f, 0.94f));
            SetAnchors(mixShufflePrompt, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);

            mixShufflePromptText = CreateText("Mix Shuffle Prompt Text", mixShufflePrompt, TextAnchor.MiddleCenter, 32, FontStyle.Bold);
            SetAnchors(mixShufflePromptText.rectTransform, new Vector2(0f, 0.46f), new Vector2(1f, 1f), new Vector2(20f, 4f), new Vector2(-20f, -8f));

            var cancelButton = CreateButton("Mix Shuffle Cancel Button", mixShufflePrompt, "Cancel", new Color(0.24f, 0.29f, 0.34f));
            SetAnchors(cancelButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.31f, 0.46f), new Vector2(18f, 16f), new Vector2(-6f, -12f));
            cancelButton.onClick.AddListener(HideMixShufflePrompt);

            mixShuffleGoldConfirmButton = CreateButton("Mix Shuffle Gold Button", mixShufflePrompt, "90 Gold", new Color(0.70f, 0.48f, 0.08f));
            SetAnchors(mixShuffleGoldConfirmButton.GetComponent<RectTransform>(), new Vector2(0.345f, 0f), new Vector2(0.655f, 0.46f), new Vector2(6f, 16f), new Vector2(-6f, -12f));
            mixShuffleGoldButtonText = GetButtonLabel(mixShuffleGoldConfirmButton);
            mixShuffleGoldConfirmButton.onClick.AddListener(() =>
            {
                HideMixShufflePrompt();
                MixShuffleGoldConfirmed?.Invoke();
            });

            mixShuffleConfirmButton = CreateButton("Mix Shuffle Confirm Button", mixShufflePrompt, "Watch", new Color(0.21f, 0.46f, 0.66f));
            SetAnchors(mixShuffleConfirmButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0f), new Vector2(1f, 0.46f), new Vector2(6f, 16f), new Vector2(-18f, -12f));
            mixShuffleWatchButtonText = GetButtonLabel(mixShuffleConfirmButton);
            mixShuffleConfirmButton.onClick.AddListener(() =>
            {
                HideMixShufflePrompt();
                MixShuffleConfirmed?.Invoke();
            });

            HideMixShufflePrompt();
        }

        private void ApplyStationUnlockPromptState(int lockedSlotsRemaining, bool adReady, bool adInProgress)
        {
            if (stationUnlockPromptText != null)
            {
                stationUnlockPromptText.text = adInProgress || !adReady
                    ? "Loading Ad"
                    : $"Watch Ad?\n+1 Stop ({lockedSlotsRemaining})";
            }

            if (stationUnlockConfirmButton != null)
            {
                stationUnlockConfirmButton.interactable = adReady && !adInProgress;
            }
        }

        private void ApplyVipTeleportPromptState(int remainingAds, bool adReady, bool adInProgress)
        {
            if (vipTeleportPromptText != null)
            {
                vipTeleportPromptText.text = adInProgress || !adReady
                    ? "Loading Ad"
                    : $"Watch Ad?\nVIP Bus ({remainingAds})";
            }

            if (vipTeleportConfirmButton != null)
            {
                vipTeleportConfirmButton.interactable = adReady && !adInProgress && remainingAds > 0;
            }
        }

        private void ApplyMixShufflePromptState(
            int goldBalance,
            int goldCost,
            bool canSpendGold,
            bool adReady,
            bool adInProgress)
        {
            if (mixShufflePromptText != null)
            {
                mixShufflePromptText.text = canSpendGold
                    ? "Mix Buses\nUse Gold or Watch Ad"
                    : $"Mix Buses\nGold {Mathf.Max(0, goldBalance)}/{Mathf.Max(0, goldCost)}";
            }

            if (mixShuffleGoldButtonText != null)
            {
                mixShuffleGoldButtonText.text = canSpendGold
                    ? $"{Mathf.Max(0, goldCost)} Gold"
                    : "Need Gold";
            }

            if (mixShuffleWatchButtonText != null)
            {
                mixShuffleWatchButtonText.text = adInProgress || !adReady ? "Loading" : "Watch";
            }

            if (mixShuffleGoldConfirmButton != null)
            {
                mixShuffleGoldConfirmButton.interactable = canSpendGold && !adInProgress;
            }

            if (mixShuffleConfirmButton != null)
            {
                mixShuffleConfirmButton.interactable = adReady && !adInProgress;
            }
        }
    }
}
