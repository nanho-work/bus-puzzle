using UnityEngine;

namespace BusPuzzle
{
    public sealed partial class GameManager
    {
        private void ResetTutorialState()
        {
            SetTutorialGameplayPaused(false);
            tutorialStep = TutorialStep.None;
            tutorialStepTimer = 0f;
            tutorialDispatchedBusCount = 0;
            tutorialFreeUseEnabledForStage = false;
            tutorialStationUnlockFreeUsed = false;
            tutorialMixShuffleFreeUsed = false;
            tutorialDepartFreeUsed = false;
            tutorialFirstBusTarget = null;
            tutorialSecondBusTarget = null;
            tutorialDepartHintBus = null;
            ClearTutorialTargetHighlights();
            uiController?.HideTutorial();
        }

        private void StartTutorialIfNeeded()
        {
            if (currentLevelIndex != 0 ||
                UserProgress.HasCompletedTutorial ||
                uiController == null ||
                uiController.IsInitialNicknamePromptBlocking ||
                gameState != GameState.Playing ||
                tutorialStep != TutorialStep.None)
            {
                return;
            }

            tutorialFreeUseEnabledForStage = true;
            tutorialFirstBusTarget = FindTutorialLaunchableBus();
            AdvanceTutorial(TutorialStep.TapFirstBus);
        }

        private void UpdateTutorial(float deltaTime)
        {
            if (tutorialStep == TutorialStep.None || tutorialStep == TutorialStep.Complete)
            {
                return;
            }

            if (gameState != GameState.Playing || uiController == null)
            {
                uiController?.HideTutorial();
                return;
            }

            tutorialStepTimer += deltaTime;
            switch (tutorialStep)
            {
                case TutorialStep.TapFirstBus:
                    ShowTutorialForBusTarget(GetTutorialLaunchTarget(), Localization.Text("tutorial_tap_bus"));
                    break;
                case TutorialStep.DepartHint:
                    ShowTutorialForDepartHintBus(Localization.Text("tutorial_bus_depart"));
                    if (IsTutorialDepartHintComplete())
                    {
                        tutorialSecondBusTarget = FindTutorialLaunchableBus();
                        AdvanceTutorial(TutorialStep.TapSecondBus);
                    }

                    break;
                case TutorialStep.TapSecondBus:
                    if (tutorialSecondBusTarget == null || !IsTutorialLaunchCandidate(tutorialSecondBusTarget))
                    {
                        tutorialSecondBusTarget = FindTutorialLaunchableBus();
                    }

                    ShowTutorialForBusTarget(GetTutorialLaunchTarget(), Localization.Text("tutorial_finish_all"));
                    break;
                case TutorialStep.FastForwardHint:
                    ClearTutorialTargetHighlights();
                    if (TryFindTutorialEmptyBoardWorldPosition(out var emptyPosition))
                    {
                        uiController.ShowTutorialForWorld(
                            gameCamera,
                            emptyPosition + Vector3.up * 0.12f,
                            58f,
                            Localization.Text("tutorial_fast_forward"));
                    }
                    else
                    {
                        uiController.ShowTutorialForScreen(
                            new Vector2(Screen.width * 0.50f, Screen.height * 0.42f),
                            104f,
                            Localization.Text("tutorial_fast_forward"));
                    }

                    if (IsPassengerFastForwardHeld())
                    {
                        AdvanceTutorial(TutorialStep.PlusFree);
                    }

                    break;
                case TutorialStep.PlusFree:
                    if (tutorialStationUnlockFreeUsed || boardView == null || !boardView.CanUnlockStationSlot)
                    {
                        if (!tutorialStationUnlockFreeUsed)
                        {
                            AdvanceTutorial(TutorialStep.MixFree);
                        }

                        break;
                    }

                    if (boardView.TryGetFirstLockedStationSlotPosition(out var lockedPosition))
                    {
                        SetTutorialBusHighlight(null);
                        boardView.SetTutorialStationUnlockHighlight(true);
                        uiController.ShowTutorialForWorld(
                            gameCamera,
                            lockedPosition + Vector3.up * 0.25f,
                            48f,
                            Localization.Text("tutorial_plus_free"));
                    }

                    break;
                case TutorialStep.MixFree:
                    ClearTutorialTargetHighlights();
                    if (tutorialMixShuffleFreeUsed)
                    {
                        AdvanceTutorial(TutorialStep.DepartFree);
                        break;
                    }

                    if (!CanUseTutorialFreeMixShuffleNow())
                    {
                        AdvanceTutorial(TutorialStep.DepartFree);
                        break;
                    }

                    uiController.ShowTutorialForMixButton(Localization.Text("tutorial_mix_free"));
                    break;
                case TutorialStep.DepartFree:
                    ClearTutorialTargetHighlights();
                    if (tutorialDepartFreeUsed)
                    {
                        SetTutorialGameplayPaused(false);
                        AdvanceTutorial(TutorialStep.VipHint);
                        break;
                    }

                    if (!AreOpenStationSlotsFull())
                    {
                        SetTutorialGameplayPaused(false);
                        uiController.HideTutorial();
                        break;
                    }

                    if (!CanUseTutorialFreeDepartNow())
                    {
                        SetTutorialGameplayPaused(false);
                        uiController.HideTutorial();
                        break;
                    }

                    SetTutorialGameplayPaused(true);
                    UpdateDepartUi();
                    uiController.ShowTutorialForDepartButton(Localization.Text("tutorial_depart_free"));
                    break;
                case TutorialStep.VipHint:
                    ClearTutorialTargetHighlights();
                    uiController.ShowTutorialForVipButton(Localization.Text("tutorial_vip_hint"));
                    break;
            }
        }

        private void AdvanceTutorial(TutorialStep nextStep)
        {
            if (nextStep != TutorialStep.DepartFree)
            {
                SetTutorialGameplayPaused(false);
            }

            ClearTutorialTargetHighlights();
            tutorialStep = nextStep;
            tutorialStepTimer = 0f;
            if (nextStep == TutorialStep.Complete)
            {
                CompleteTutorial();
            }
        }

        private void CompleteTutorial()
        {
            SetTutorialGameplayPaused(false);
            tutorialStep = TutorialStep.Complete;
            tutorialStepTimer = 0f;
            ClearTutorialTargetHighlights();
            uiController?.HideTutorial();
            UserProgress.MarkTutorialCompleted();
            UpdateRewardedAdUi();
            ScheduleDailyRewardPromptCheck();
        }

        private bool IsTutorialActive =>
            tutorialStep != TutorialStep.None &&
            tutorialStep != TutorialStep.Complete;

        private bool TryBlockTutorialAction(TutorialStep expectedStep)
        {
            if (!IsTutorialActive || tutorialStep == expectedStep)
            {
                return false;
            }

            ShowCurrentTutorialMessage();
            return true;
        }

        private void ShowCurrentTutorialMessage()
        {
            uiController?.ShowInvalid(GetCurrentTutorialMessage());
        }

        private string GetCurrentTutorialMessage()
        {
            switch (tutorialStep)
            {
                case TutorialStep.TapFirstBus:
                    return Localization.Text("tutorial_tap_bus");
                case TutorialStep.DepartHint:
                    return Localization.Text("tutorial_bus_depart");
                case TutorialStep.TapSecondBus:
                    return Localization.Text("tutorial_finish_all");
                case TutorialStep.FastForwardHint:
                    return Localization.Text("tutorial_fast_forward");
                case TutorialStep.PlusFree:
                    return Localization.Text("tutorial_plus_free");
                case TutorialStep.MixFree:
                    return Localization.Text("tutorial_mix_free");
                case TutorialStep.DepartFree:
                    return Localization.Text("tutorial_depart_free");
                case TutorialStep.VipHint:
                    return Localization.Text("tutorial_vip_hint");
                default:
                    return string.Empty;
            }
        }

        private void SetTutorialBusHighlight(BusView bus)
        {
            if (tutorialHighlightedBus == bus)
            {
                if (tutorialHighlightedBus != null)
                {
                    tutorialHighlightedBus.SetTutorialHighlight(true);
                }

                return;
            }

            if (tutorialHighlightedBus != null)
            {
                tutorialHighlightedBus.SetTutorialHighlight(false);
            }

            tutorialHighlightedBus = bus;
            if (tutorialHighlightedBus != null)
            {
                tutorialHighlightedBus.SetTutorialHighlight(true);
            }
        }

        private void ClearTutorialTargetHighlights()
        {
            if (tutorialHighlightedBus != null)
            {
                tutorialHighlightedBus.SetTutorialHighlight(false);
                tutorialHighlightedBus = null;
            }

            boardView?.SetTutorialStationUnlockHighlight(false);
        }

        private bool IsTutorialBusTapAllowed(BusView bus)
        {
            if (!IsTutorialActive)
            {
                return true;
            }

            if (IsTutorialWaitingForOpenStationSlotsFull())
            {
                return true;
            }

            if (tutorialStep == TutorialStep.DepartHint)
            {
                SetTutorialBusHighlight(tutorialDepartHintBus);
                ShowCurrentTutorialMessage();
                return false;
            }

            var target = GetTutorialLaunchTarget();
            if (tutorialStep != TutorialStep.TapFirstBus && tutorialStep != TutorialStep.TapSecondBus)
            {
                ShowCurrentTutorialMessage();
                return false;
            }

            if (target == null)
            {
                ShowCurrentTutorialMessage();
                return false;
            }

            if (bus == target)
            {
                return true;
            }

            SetTutorialBusHighlight(target);
            ShowCurrentTutorialMessage();
            return false;
        }

        private BusView GetTutorialLaunchTarget()
        {
            switch (tutorialStep)
            {
                case TutorialStep.TapFirstBus:
                    if (tutorialFirstBusTarget == null || !IsTutorialLaunchCandidate(tutorialFirstBusTarget))
                    {
                        tutorialFirstBusTarget = FindTutorialLaunchableBus();
                    }

                    return tutorialFirstBusTarget;
                case TutorialStep.TapSecondBus:
                    if (tutorialSecondBusTarget == null || !IsTutorialLaunchCandidate(tutorialSecondBusTarget))
                    {
                        tutorialSecondBusTarget = FindTutorialLaunchableBus();
                    }

                    return tutorialSecondBusTarget;
                default:
                    return null;
            }
        }

        private void HandleTutorialBusDispatched(BusView bus)
        {
            if (!tutorialFreeUseEnabledForStage)
            {
                return;
            }

            tutorialDispatchedBusCount++;
            if (tutorialStep == TutorialStep.TapFirstBus)
            {
                tutorialDepartHintBus = bus;
                AdvanceTutorial(TutorialStep.DepartHint);
            }
            else if (tutorialStep == TutorialStep.TapSecondBus)
            {
                AdvanceTutorial(TutorialStep.FastForwardHint);
            }
        }

        private void ShowTutorialForBusTarget(BusView targetBus, string message)
        {
            if (targetBus == null)
            {
                SetTutorialBusHighlight(null);
                uiController.ShowTutorialForScreen(
                    new Vector2(Screen.width * 0.50f, Screen.height * 0.36f),
                    58f,
                    message);
                return;
            }

            SetTutorialBusHighlight(targetBus);
            uiController.ShowTutorialForWorldRect(
                gameCamera,
                targetBus.GetTutorialHighlightWorldCorners(),
                18f,
                message);
        }

        private void ShowTutorialForDepartHintBus(string message)
        {
            var targetBus = tutorialDepartHintBus != null && !tutorialDepartHintBus.IsDeparted
                ? tutorialDepartHintBus
                : FindTutorialStationBus();
            if (targetBus == null)
            {
                SetTutorialBusHighlight(null);
                uiController.ShowTutorialForScreen(
                    new Vector2(Screen.width * 0.50f, Screen.height * 0.58f),
                    62f,
                    message);
                return;
            }

            SetTutorialBusHighlight(targetBus);
            uiController.ShowTutorialForWorldRect(
                gameCamera,
                targetBus.GetTutorialHighlightWorldCorners(),
                18f,
                message);
        }

        private bool TryFindTutorialEmptyBoardWorldPosition(out Vector3 position)
        {
            position = Vector3.zero;
            var bestScore = float.NegativeInfinity;
            var preferredCell = new Vector2((BoardLayoutConfig.GridColumns - 1) * 0.5f, BoardLayoutConfig.GridRows * 0.60f);

            for (var y = BoardLayoutConfig.GridRows - 2; y >= 2; y--)
            {
                for (var x = 2; x <= BoardLayoutConfig.GridColumns - 3; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsTutorialCellOccupied(cell))
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(new Vector2(x, y), preferredCell);
                    var score = -distance + y * 0.025f;
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    position = BoardLayoutConfig.GridToWorld(cell);
                }
            }

            return !float.IsNegativeInfinity(bestScore);
        }

        private bool IsTutorialCellOccupied(Vector2Int cell)
        {
            var cellFootprint = new VehicleFootprint(
                BoardLayoutConfig.GridToWorld(cell),
                Vector3.right,
                Vector3.forward,
                BoardLayoutConfig.CellSize * 0.46f,
                BoardLayoutConfig.CellSize * 0.46f);

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus == null || bus.IsDeparted || !bus.IsOnBoard)
                {
                    continue;
                }

                if (bus.CurrentFootprint.Overlaps(cellFootprint))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTutorialDepartHintComplete()
        {
            if (tutorialStepTimer < TutorialDepartHintMinimumSeconds)
            {
                return false;
            }

            if (tutorialDepartHintBus == null)
            {
                return FindTutorialStationBus() == null;
            }

            if (tutorialDepartHintBus.IsDeparted)
            {
                return true;
            }

            if (tutorialDepartHintBus.IsMoving ||
                tutorialDepartHintBus.IsBoardingPassengers ||
                tutorialDepartHintBus.HasBoardingReservations)
            {
                return false;
            }

            return tutorialDepartHintBus.IsParkedAtStation;
        }

        private BusView FindTutorialLaunchableBus()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (!IsTutorialLaunchCandidate(bus))
                {
                    continue;
                }

                if (boardView != null && boardView.IsPathClear(bus, buses, out _))
                {
                    return bus;
                }
            }

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (IsTutorialLaunchCandidate(bus))
                {
                    return bus;
                }
            }

            return null;
        }

        private BusView FindTutorialStationBus()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus != null && bus.IsParkedAtStation && !bus.IsDeparted)
                {
                    return bus;
                }
            }

            return null;
        }

        private static bool IsTutorialLaunchCandidate(BusView bus)
        {
            return bus != null &&
                bus.IsOnBoard &&
                !bus.IsConcealed &&
                !bus.IsMoving &&
                !bus.IsDeparted;
        }

        private bool TryUseTutorialFreeStationUnlock()
        {
            if (!CanUseTutorialFreeStationUnlockNow())
            {
                return false;
            }

            if (!boardView.TryUnlockStationSlot())
            {
                return false;
            }

            tutorialStationUnlockFreeUsed = true;
            if (tutorialStep == TutorialStep.PlusFree)
            {
                AdvanceTutorial(TutorialStep.MixFree);
            }

            if (boardView.TryGetLastActiveStationSlotPosition(out var unlockedPosition))
            {
                boardView.PulseTutorialStationSlot(unlockedPosition);
            }

            uiController.ShowInvalid(Localization.Text("tutorial_plus_unlocked"));
            UpdateCounters();
            UpdateRewardedAdUi();

            CheckBlocked();
            return true;
        }

        private bool TryUseTutorialFreeMixShuffle()
        {
            if (!CanUseTutorialFreeMixShuffleNow())
            {
                return false;
            }

            if (!TryShuffleVisibleBusColors())
            {
                return false;
            }

            tutorialMixShuffleFreeUsed = true;
            uiController.ShowInvalid(Localization.Text("tutorial_mix_done"));
            UpdateRewardedAdUi();
            if (tutorialStep == TutorialStep.MixFree)
            {
                AdvanceTutorial(TutorialStep.DepartFree);
            }

            CheckBlocked();
            return true;
        }

        private bool TryUseTutorialFreeDepart()
        {
            if (!CanUseTutorialFreeDepartNow())
            {
                return false;
            }

            SetTutorialGameplayPaused(false);
            if (!TryStartDepartBoost())
            {
                return false;
            }

            tutorialDepartFreeUsed = true;
            uiController.ShowInvalid(Localization.Text("tutorial_depart_done"));
            UpdateRewardedAdUi();
            if (tutorialStep == TutorialStep.DepartFree)
            {
                AdvanceTutorial(TutorialStep.VipHint);
            }

            return true;
        }

        private bool TryUseTutorialFreeVipTeleport()
        {
            if (tutorialStep != TutorialStep.VipHint ||
                !tutorialFreeUseEnabledForStage ||
                gameState != GameState.Playing ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress)
            {
                return false;
            }

            if (!HasVipTeleportTarget())
            {
                uiController.ShowInvalid(Localization.Text("status_no_vip_target"));
                UpdateVipTeleportUi();
                return true;
            }

            vipTeleportTickets++;
            CompleteTutorial();
            EnterVipSelectionMode();
            return true;
        }

        private bool CanUseTutorialFreeStationUnlockNow()
        {
            return tutorialFreeUseEnabledForStage &&
                tutorialStep == TutorialStep.PlusFree &&
                !tutorialStationUnlockFreeUsed &&
                gameState == GameState.Playing &&
                !IsAnyRewardedAdInProgress &&
                boardView != null &&
                boardView.CanUnlockStationSlot;
        }

        private bool CanUseTutorialFreeMixShuffleNow()
        {
            return tutorialFreeUseEnabledForStage &&
                tutorialStep == TutorialStep.MixFree &&
                !tutorialMixShuffleFreeUsed &&
                gameState == GameState.Playing &&
                !isVipSelectionMode &&
                !IsAnyRewardedAdInProgress &&
                HasMixShuffleTarget();
        }

        private bool CanUseTutorialFreeDepartNow()
        {
            return tutorialFreeUseEnabledForStage &&
                tutorialStep == TutorialStep.DepartFree &&
                !tutorialDepartFreeUsed &&
                gameState == GameState.Playing &&
                !isVipSelectionMode &&
                !IsAnyRewardedAdInProgress &&
                AreOpenStationSlotsFull() &&
                HasPotentialDepartTarget();
        }

        private bool IsTutorialWaitingForOpenStationSlotsFull()
        {
            return tutorialStep == TutorialStep.DepartFree && !AreOpenStationSlotsFull();
        }

        private bool AreOpenStationSlotsFull()
        {
            return boardView != null &&
                boardView.StationCapacity > 0 &&
                boardView.OccupiedStationSlots >= boardView.StationCapacity;
        }

        private void SetTutorialGameplayPaused(bool paused)
        {
            if (tutorialGameplayPaused == paused)
            {
                return;
            }

            if (paused)
            {
                tutorialPreviousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = tutorialPreviousTimeScale > 0f ? tutorialPreviousTimeScale : 1f;
            }

            tutorialGameplayPaused = paused;
        }
    }
}
