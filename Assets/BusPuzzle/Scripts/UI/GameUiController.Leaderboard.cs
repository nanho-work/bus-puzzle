using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BusPuzzle
{
    public sealed partial class GameUiController
    {
        private void BuildLeaderboardPrompt()
        {
            leaderboardPrompt = CreatePromptOverlay("Leaderboard Overlay");
            var modal = CreateGameDialog("Leaderboard Modal", leaderboardPrompt);
            SetAnchors(modal, new Vector2(0.06f, 0.11f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);

            var titlePlate = CreateDialogTitlePlate("Leaderboard Title Plate", modal, Localization.Text("leaderboard_title"));
            leaderboardPromptTitleText = titlePlate.GetComponentInChildren<Text>();
            ApplySettingsTextWeight(leaderboardPromptTitleText);
            SetAnchors(titlePlate, new Vector2(0.17f, 0.88f), new Vector2(0.83f, 1.14f), Vector2.zero, Vector2.zero);

            var closeButton = CreatePromptCloseButton("Leaderboard Close Button", modal);
            closeButton.onClick.AddListener(() => HideLeaderboardPrompt(false));

            var personalRecordPanel = CreateRoundedPanel("Leaderboard Personal Record Panel", modal, new Color(0.12f, 0.32f, 0.40f, 0.72f));
            SetAnchors(personalRecordPanel, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.84f), new Vector2(8f, 0f), new Vector2(-8f, 0f));

            leaderboardStatusText = CreateText("Leaderboard Status", personalRecordPanel, TextAnchor.MiddleCenter, 25, FontStyle.Normal);
            ApplySettingsTextWeight(leaderboardStatusText);
            leaderboardStatusText.color = new Color(0.86f, 0.94f, 1f, 0.96f);
            SetAnchors(leaderboardStatusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            var viewport = CreateRoundedPanel("Leaderboard Viewport", modal, new Color(0.05f, 0.08f, 0.11f, 0.86f));
            SetAnchors(viewport, new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.75f), Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();

            var viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.raycastTarget = true;
            }

            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            leaderboardListContent = CreateRectTransform("Leaderboard List Content", viewport);
            leaderboardListContent.anchorMin = new Vector2(0f, 1f);
            leaderboardListContent.anchorMax = new Vector2(1f, 1f);
            leaderboardListContent.pivot = new Vector2(0.5f, 1f);
            leaderboardListContent.anchoredPosition = Vector2.zero;
            leaderboardListContent.sizeDelta = Vector2.zero;

            var layout = leaderboardListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = leaderboardListContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = leaderboardListContent;

            leaderboardRefreshButton = CreatePromptTextButton(
                "Leaderboard Refresh Button",
                modal,
                Localization.Text("leaderboard_refresh"),
                UiPrimaryActionColor,
                out leaderboardRefreshButtonText);
            ApplySettingsTextWeight(leaderboardRefreshButtonText);
            SetAnchors(leaderboardRefreshButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.01f), new Vector2(0.72f, 0.15f), new Vector2(0f, 14f), new Vector2(0f, -8f));
            leaderboardRefreshButton.onClick.AddListener(RefreshLeaderboardPrompt);

            HideLeaderboardPrompt(false);
        }

        private void ShowLeaderboardPrompt()
        {
            if (leaderboardPrompt == null)
            {
                return;
            }

            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
            }

            leaderboardPrompt.SetAsLastSibling();
            leaderboardPrompt.gameObject.SetActive(true);
            RefreshLocalizedTexts();
            RefreshLeaderboardPrompt();
        }

        private void HideLeaderboardPrompt(bool returnToSettings)
        {
            if (leaderboardPrompt != null)
            {
                leaderboardPrompt.gameObject.SetActive(false);
            }

            if (returnToSettings && settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(true);
                RefreshSettingsToggles();
                RefreshLocalizedTexts();
            }
        }

        private void RefreshLeaderboardPrompt()
        {
            ClearLeaderboardRows();
            SetLeaderboardStatus(Localization.Text("leaderboard_loading"));
            if (leaderboardRefreshButton != null)
            {
                leaderboardRefreshButton.interactable = false;
            }

            LeaderboardService.FetchTopLeaderboard(
                entries =>
                {
                    if (leaderboardRefreshButton != null)
                    {
                        leaderboardRefreshButton.interactable = true;
                    }

                    ApplyLeaderboardEntries(entries);
                },
                _ =>
                {
                    if (leaderboardRefreshButton != null)
                    {
                        leaderboardRefreshButton.interactable = true;
                    }

                    ClearLeaderboardRows();
                    SetLeaderboardStatus(Localization.Text("leaderboard_error"));
                });
        }

        private void ApplyLeaderboardEntries(IReadOnlyList<LeaderboardService.LeaderboardEntry> entries)
        {
            ClearLeaderboardRows();
            SetLeaderboardStatus(GetLeaderboardPersonalStatus());

            if (entries == null || entries.Count == 0)
            {
                CreateLeaderboardMessageRow("Leaderboard Empty Row", Localization.Text("leaderboard_empty"), 28, new Color(0.10f, 0.14f, 0.18f, 0.62f));
                return;
            }

            CreateLeaderboardTableRow(
                "Leaderboard Header Row",
                Localization.Text("leaderboard_column_rank"),
                Localization.Text("leaderboard_column_nickname"),
                Localization.Text("leaderboard_column_stage"),
                22,
                new Color(0.16f, 0.24f, 0.32f, 0.90f),
                44f);

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var isMine = IsLocalLeaderboardEntry(entry);
                var rowColor = isMine
                    ? new Color(0.15f, 0.45f, 0.58f, 0.92f)
                    : index % 2 == 0
                    ? new Color(0.10f, 0.14f, 0.18f, 0.72f)
                    : new Color(0.08f, 0.12f, 0.16f, 0.72f);
                CreateLeaderboardTableRow(
                    $"Leaderboard Row {index:00}",
                    entry.Rank.ToString(),
                    entry.Nickname,
                    entry.MaxClearedStage.ToString("00"),
                    25,
                    rowColor,
                    50f,
                    isMine);
            }
        }

        private static bool IsLocalLeaderboardEntry(LeaderboardService.LeaderboardEntry entry)
        {
            return entry != null &&
                   PlayerIdentityService.IsReady &&
                   !string.IsNullOrWhiteSpace(entry.UserId) &&
                   entry.UserId == PlayerIdentityService.UserId;
        }

        private string GetLeaderboardPersonalStatus()
        {
            var localMaxClearedStage = LeaderboardService.LocalMaxClearedStage;
            return localMaxClearedStage > 0
                ? Localization.Text("leaderboard_my_best", localMaxClearedStage)
                : Localization.Text("leaderboard_no_personal_record");
        }

        private void SetLeaderboardStatus(string message)
        {
            if (leaderboardStatusText != null)
            {
                leaderboardStatusText.text = message;
            }
        }

        private void ClearLeaderboardRows()
        {
            if (leaderboardListContent == null)
            {
                return;
            }

            for (var index = leaderboardListContent.childCount - 1; index >= 0; index--)
            {
                Destroy(leaderboardListContent.GetChild(index).gameObject);
            }
        }

        private void CreateLeaderboardMessageRow(
            string name,
            string label,
            int fontSize,
            Color backgroundColor)
        {
            if (leaderboardListContent == null)
            {
                return;
            }

            var row = CreateRoundedPanel(name, leaderboardListContent, backgroundColor);
            row.GetComponent<Image>().raycastTarget = false;

            var layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 42f;
            layoutElement.preferredHeight = 54f;

            var rowText = CreateText($"{name} Text", row, TextAnchor.MiddleCenter, fontSize, FontStyle.Normal);
            ApplySettingsTextWeight(rowText);
            rowText.text = label;
            rowText.color = new Color(0.96f, 0.98f, 1f, 0.98f);
            rowText.resizeTextMinSize = 16;
            SetAnchors(rowText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 3f), new Vector2(-18f, -3f));
        }

        private void CreateLeaderboardTableRow(
            string name,
            string rank,
            string nickname,
            string stage,
            int fontSize,
            Color backgroundColor,
            float rowHeight,
            bool highlighted = false)
        {
            if (leaderboardListContent == null)
            {
                return;
            }

            var row = CreateRoundedPanel(name, leaderboardListContent, backgroundColor);
            row.GetComponent<Image>().raycastTarget = false;

            var layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = rowHeight;
            layoutElement.preferredHeight = rowHeight;

            CreateLeaderboardColumnText(
                $"{name} Rank",
                row,
                rank,
                TextAnchor.MiddleCenter,
                fontSize,
                new Vector2(0.02f, 0f),
                new Vector2(0.17f, 1f),
                new Vector2(4f, 2f),
                new Vector2(-4f, -2f),
                highlighted);
            CreateLeaderboardColumnText(
                $"{name} Nickname",
                row,
                nickname,
                TextAnchor.MiddleLeft,
                fontSize,
                new Vector2(0.20f, 0f),
                new Vector2(0.72f, 1f),
                new Vector2(4f, 2f),
                new Vector2(-4f, -2f),
                highlighted);
            CreateLeaderboardColumnText(
                $"{name} Stage",
                row,
                stage,
                TextAnchor.MiddleCenter,
                fontSize,
                new Vector2(0.75f, 0f),
                new Vector2(0.98f, 1f),
                new Vector2(4f, 2f),
                new Vector2(-4f, -2f),
                highlighted);
        }

        private void CreateLeaderboardColumnText(
            string name,
            Transform parent,
            string label,
            TextAnchor alignment,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            bool highlighted = false)
        {
            var text = CreateText(name, parent, alignment, fontSize, FontStyle.Normal);
            ApplySettingsTextWeight(text);
            text.text = label;
            text.color = highlighted
                ? new Color(1f, 0.98f, 0.76f, 1f)
                : new Color(0.96f, 0.98f, 1f, 0.98f);
            text.resizeTextMinSize = 15;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetAnchors(text.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
        }
    }
}
