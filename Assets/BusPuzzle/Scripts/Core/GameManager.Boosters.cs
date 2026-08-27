using UnityEngine;

namespace BusPuzzle
{
    public sealed partial class GameManager
    {
        private void ShowStationUnlockPrompt()
        {
            if (TryBlockTutorialAction(TutorialStep.PlusFree))
            {
                return;
            }

            var adSkipTickets = UserEconomy.AdSkipTicketBalance;
            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.StationSlotUnlock);
            var goldFallbackEnabled = IsStationUnlockGoldFallbackEnabled();
            var canSpendGold = goldFallbackEnabled && UserEconomy.CanSpendGold(StationUnlockGoldCost);
            if (!CanShowRecoveryPrompt() ||
                IsAnyRewardedAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot ||
                uiController == null ||
                (!adAllowed && adSkipTickets <= 0 && !goldFallbackEnabled))
            {
                UpdateRewardedAdUi();
                return;
            }

            if (adAllowed &&
                rewardedAdService != null &&
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock))
            {
                rewardedAdService.Preload(RewardedAdPlacement.StationSlotUnlock);
            }

            HoldPendingFailureForRecoveryChoice();
            uiController.ShowStationUnlockPrompt(
                boardView.LockedStationSlots,
                UserEconomy.GoldBalance,
                StationUnlockGoldCost,
                goldFallbackEnabled,
                canSpendGold,
                adAllowed,
                adAllowed && rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock),
                adSkipTickets,
                isStationUnlockAdInProgress);
        }

        private void RequestStationSlotUnlockGold()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot ||
                !IsStationUnlockGoldFallbackEnabled())
            {
                UpdateRewardedAdUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TrySpendGold(StationUnlockGoldCost))
            {
                uiController.ShowInvalid(Localization.Text("need_gold"));
                ShowStationUnlockPrompt();
                UpdateGoldUi();
                UpdateRewardedAdUi();
                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();

            if (boardView.TryUnlockStationSlot())
            {
                UpdateGoldUi();
                UpdateCounters();
                CheckBlocked();
            }
            else
            {
                UserEconomy.AddGold(StationUnlockGoldCost);
                UpdateGoldUi();
                CheckBlocked();
            }

            UpdateRewardedAdUi();
        }

        private void RequestStationSlotUnlock()
        {
            var wasRecoveringFromFailure = gameState == GameState.Failed;
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot ||
                rewardedAdService == null ||
                !RemoteConfigService.AreRewardedAdsEnabled ||
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock))
            {
                UpdateRewardedAdUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            isStationUnlockAdInProgress = true;
            UpdateRewardedAdUi();

            if (!rewardedAdService.ShowStationSlotUnlockAd(HandleStationSlotUnlockAdCompleted))
            {
                isStationUnlockAdInProgress = false;
                UpdateRewardedAdUi();
                if (wasRecoveringFromFailure || wasHoldingFailureChoice)
                {
                    CheckBlocked();
                }
            }
        }

        private void RequestStationSlotUnlockSkip()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot)
            {
                UpdateRewardedAdUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TryUseAdSkipTicket())
            {
                ShowStationUnlockPrompt();
                UpdateRewardedAdUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();

            if (boardView.TryUnlockStationSlot())
            {
                UpdateCounters();
                CheckBlocked();
            }
            else
            {
                UserEconomy.AddAdSkipTickets(1);
                CheckBlocked();
            }

            UpdateRewardedAdUi();
        }

        private void HandleStationSlotUnlockAdCompleted(RewardedAdResult result)
        {
            isStationUnlockAdInProgress = false;

            if (result == RewardedAdResult.RewardEarned && boardView.TryUnlockStationSlot())
            {
                UpdateCounters();
                CheckBlocked();
            }
            else
            {
                CheckBlocked();
            }

            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                rewardedAdService?.Preload();
            }

            UpdateRewardedAdUi();
        }

        private void HandleVipTeleportRequested()
        {
            if (TryBlockTutorialAction(TutorialStep.VipHint))
            {
                return;
            }

            if (tutorialStep == TutorialStep.VipHint)
            {
                TryUseTutorialFreeVipTeleport();
                return;
            }

            if (!CanShowRecoveryPrompt())
            {
                UpdateVipTeleportUi();
                return;
            }

            if (isVipSelectionMode)
            {
                ExitVipSelectionMode();
                return;
            }

            if (vipTeleportTickets > 0)
            {
                HoldPendingFailureForRecoveryChoice();
                ResumeFailedLevelForRecovery();
                EnterVipSelectionMode();
                if (!isVipSelectionMode)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ShowVipTeleportPrompt();
        }

        private void ShowVipTeleportPrompt()
        {
            var adSkipTickets = UserEconomy.AdSkipTicketBalance;
            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.VipBusTeleport);
            if (!CanShowRecoveryPrompt() ||
                IsAnyRewardedAdInProgress ||
                uiController == null ||
                RemainingVipTeleportUses <= 0)
            {
                UpdateVipTeleportUi();
                return;
            }

            if (!HasVipTeleportTarget())
            {
                uiController.ShowInvalid(Localization.Text("status_no_vip_target"));
                UpdateVipTeleportUi();
                return;
            }

            if (adAllowed &&
                rewardedAdService != null &&
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport))
            {
                rewardedAdService.Preload(RewardedAdPlacement.VipBusTeleport);
            }

            HoldPendingFailureForRecoveryChoice();
            uiController.ShowVipTeleportPrompt(
                vipUsesGrantedThisStage,
                VipTeleportAdLimitPerStage,
                UserEconomy.GoldBalance,
                VipTeleportGoldCost,
                UserEconomy.CanSpendGold(VipTeleportGoldCost),
                adSkipTickets,
                adAllowed,
                adAllowed &&
                    rewardedAdService != null &&
                    rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport),
                isVipAdInProgress);
        }

        private void RequestVipBusTeleportGold()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                RemainingVipTeleportUses <= 0 ||
                !HasVipTeleportTarget())
            {
                UpdateVipTeleportUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TrySpendGold(VipTeleportGoldCost))
            {
                uiController.ShowInvalid(Localization.Text("need_gold"));
                ShowVipTeleportPrompt();
                UpdateGoldUi();
                UpdateVipTeleportUi();
                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            vipUsesGrantedThisStage++;
            vipTeleportTickets++;
            EnterVipSelectionMode();
            UpdateGoldUi();
            UpdateRewardedAdUi();
        }

        private void RequestVipBusTeleportSkip()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                RemainingVipTeleportUses <= 0 ||
                !HasVipTeleportTarget())
            {
                UpdateVipTeleportUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TryUseAdSkipTicket())
            {
                ShowVipTeleportPrompt();
                UpdateVipTeleportUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            vipUsesGrantedThisStage++;
            vipTeleportTickets++;
            EnterVipSelectionMode();
            UpdateRewardedAdUi();
        }

        private void RequestVipBusTeleportAd()
        {
            var wasRecoveringFromFailure = gameState == GameState.Failed;
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                rewardedAdService == null ||
                !RemoteConfigService.AreRewardedAdsEnabled ||
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport) ||
                RemainingVipTeleportUses <= 0 ||
                !HasVipTeleportTarget())
            {
                UpdateVipTeleportUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            isVipAdInProgress = true;
            UpdateRewardedAdUi();

            if (!rewardedAdService.ShowVipBusTeleportAd(HandleVipBusTeleportAdCompleted))
            {
                isVipAdInProgress = false;
                UpdateRewardedAdUi();
                if (wasRecoveringFromFailure || wasHoldingFailureChoice)
                {
                    CheckBlocked();
                }
            }
        }

        private void HandleVipBusTeleportAdCompleted(RewardedAdResult result)
        {
            isVipAdInProgress = false;

            if (result == RewardedAdResult.RewardEarned)
            {
                vipUsesGrantedThisStage++;
                vipTeleportTickets++;
                EnterVipSelectionMode();
            }
            else
            {
                CheckBlocked();
            }

            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                rewardedAdService?.Preload();
            }

            UpdateRewardedAdUi();
        }

        private void EnterVipSelectionMode()
        {
            if (vipTeleportTickets <= 0 || !HasVipTeleportTarget())
            {
                uiController.ShowInvalid(boardView != null && !boardView.CanReserveVipStationSlot
                    ? Localization.Text("status_vip_busy")
                    : Localization.Text("status_no_vip_target"));
                UpdateVipTeleportUi();
                return;
            }

            isVipSelectionMode = true;
            ApplyVipHighlights();
            uiController.ShowInvalid(Localization.Text("status_choose_vip_bus"));
            UpdateRewardedAdUi();
        }

        private void ExitVipSelectionMode()
        {
            isVipSelectionMode = false;
            ApplyVipHighlights();
            uiController.HideVipTeleportPrompt();
            uiController.ShowPlaying(GetCurrentLevelName());
            UpdateRewardedAdUi();
            if (isRecoveryChoiceHoldingFailure)
            {
                RecheckHeldFailureAfterRecoveryChoice();
            }
            else
            {
                CheckBlocked();
            }
        }

        private void ExitVipSelectionModeForEndState()
        {
            isVipSelectionMode = false;
            ApplyVipHighlights();
            uiController.HideVipTeleportPrompt();
            uiController.HideMixShufflePrompt();
            uiController.HideDepartPrompt();
        }

        private void TryUseVipTeleport(BusView bus)
        {
            if (!isVipSelectionMode || vipTeleportTickets <= 0)
            {
                ExitVipSelectionMode();
                return;
            }

            if (!CanVipTeleportTarget(bus))
            {
                uiController.ShowInvalid(Localization.Text("status_pick_waiting_bus"));
                ApplyVipHighlights();
                return;
            }

            if (!vehicleDispatchController.TryVipTeleport(bus))
            {
                ApplyVipHighlights();
                UpdateVipTeleportUi();
                return;
            }

            vipTeleportTickets = Mathf.Max(0, vipTeleportTickets - 1);
            isVipSelectionMode = false;
            ClearPendingFailureRecoveryState();
            ApplyVipHighlights();
            UpdateRewardedAdUi();
            CheckBlocked();
        }

        private void ResetVipTeleportState()
        {
            isVipAdInProgress = false;
            isVipSelectionMode = false;
            vipUsesGrantedThisStage = 0;
            vipTeleportTickets = 0;
            ApplyVipHighlights();
        }

        private void HandleMixShuffleRequested()
        {
            if (TryBlockTutorialAction(TutorialStep.MixFree))
            {
                return;
            }

            if (TryUseTutorialFreeMixShuffle())
            {
                return;
            }

            if (IsTutorialActive)
            {
                ShowCurrentTutorialMessage();
                return;
            }

            if (!CanShowRecoveryPrompt() || isVipSelectionMode)
            {
                UpdateMixShuffleUi();
                return;
            }

            ShowMixShufflePrompt();
        }

        private void ShowMixShufflePrompt()
        {
            var adSkipTickets = UserEconomy.AdSkipTicketBalance;
            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.BusColorShuffle);
            if (!CanShowRecoveryPrompt() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                uiController == null)
            {
                UpdateMixShuffleUi();
                return;
            }

            if (!HasMixShuffleTarget())
            {
                uiController.ShowInvalid(Localization.Text("status_no_mix_target"));
                UpdateMixShuffleUi();
                return;
            }

            if (adAllowed &&
                rewardedAdService != null &&
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle))
            {
                rewardedAdService.Preload(RewardedAdPlacement.BusColorShuffle);
            }

            HoldPendingFailureForRecoveryChoice();
            uiController.ShowMixShufflePrompt(
                UserEconomy.GoldBalance,
                MixShuffleGoldCost,
                UserEconomy.CanSpendGold(MixShuffleGoldCost),
                adSkipTickets,
                adAllowed,
                adAllowed && rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle),
                isMixShuffleAdInProgress);
        }

        private void RequestMixShuffleGold()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                !HasMixShuffleTarget())
            {
                UpdateMixShuffleUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TrySpendGold(MixShuffleGoldCost))
            {
                uiController.ShowInvalid(Localization.Text("need_gold"));
                ShowMixShufflePrompt();
                UpdateGoldUi();
                UpdateMixShuffleUi();
                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            if (TryShuffleVisibleBusColors())
            {
                uiController.ShowInvalid(Localization.Text("status_mixed"));
                CheckBlocked();
            }
            else
            {
                UserEconomy.AddGold(MixShuffleGoldCost);
                uiController.ShowInvalid(Localization.Text("status_no_mix_target"));
                CheckBlocked();
            }

            UpdateGoldUi();
            UpdateRewardedAdUi();
        }

        private void RequestMixShuffleSkip()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                !HasMixShuffleTarget())
            {
                UpdateMixShuffleUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TryUseAdSkipTicket())
            {
                ShowMixShufflePrompt();
                UpdateMixShuffleUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            if (TryShuffleVisibleBusColors())
            {
                uiController.ShowInvalid(Localization.Text("status_mixed"));
                CheckBlocked();
            }
            else
            {
                UserEconomy.AddAdSkipTickets(1);
                uiController.ShowInvalid(Localization.Text("status_no_mix_target"));
                CheckBlocked();
            }

            UpdateRewardedAdUi();
        }

        private void RequestMixShuffleAd()
        {
            var wasRecoveringFromFailure = gameState == GameState.Failed;
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                rewardedAdService == null ||
                !RemoteConfigService.AreRewardedAdsEnabled ||
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle) ||
                !HasMixShuffleTarget())
            {
                UpdateMixShuffleUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            isMixShuffleAdInProgress = true;
            UpdateRewardedAdUi();

            if (!rewardedAdService.ShowBusColorShuffleAd(HandleMixShuffleAdCompleted))
            {
                isMixShuffleAdInProgress = false;
                UpdateRewardedAdUi();
                if (wasRecoveringFromFailure || wasHoldingFailureChoice)
                {
                    CheckBlocked();
                }
            }
        }

        private void HandleMixShuffleAdCompleted(RewardedAdResult result)
        {
            isMixShuffleAdInProgress = false;

            if (result == RewardedAdResult.RewardEarned)
            {
                if (TryShuffleVisibleBusColors())
                {
                    uiController.ShowInvalid(Localization.Text("status_mixed"));
                    CheckBlocked();
                }
                else
                {
                    uiController.ShowInvalid(Localization.Text("status_no_mix_target"));
                    CheckBlocked();
                }
            }
            else
            {
                CheckBlocked();
            }

            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                rewardedAdService?.Preload();
            }

            UpdateGoldUi();
            UpdateRewardedAdUi();
        }

        private void HandleDepartRequested()
        {
            if (TryBlockTutorialAction(TutorialStep.DepartFree))
            {
                return;
            }

            if (TryUseTutorialFreeDepart())
            {
                return;
            }

            if (IsTutorialActive)
            {
                ShowCurrentTutorialMessage();
                return;
            }

            if (!CanShowRecoveryPrompt() || isVipSelectionMode)
            {
                UpdateDepartUi();
                return;
            }

            ShowDepartPrompt();
        }

        private void ShowDepartPrompt()
        {
            var adSkipTickets = UserEconomy.AdSkipTicketBalance;
            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.DepartBoost);
            if (!CanShowRecoveryPrompt() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                uiController == null)
            {
                UpdateDepartUi();
                return;
            }

            if (!HasPotentialDepartTarget())
            {
                uiController.ShowInvalid(Localization.Text("status_no_depart_target"));
                UpdateDepartUi();
                return;
            }

            if (adAllowed &&
                rewardedAdService != null &&
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.DepartBoost))
            {
                rewardedAdService.Preload(RewardedAdPlacement.DepartBoost);
            }

            HoldPendingFailureForRecoveryChoice();
            uiController.ShowDepartPrompt(
                UserEconomy.GoldBalance,
                DepartGoldCost,
                UserEconomy.CanSpendGold(DepartGoldCost),
                adSkipTickets,
                adAllowed,
                adAllowed && rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.DepartBoost),
                isDepartAdInProgress);
        }

        private void RequestDepartGold()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                !HasPotentialDepartTarget())
            {
                UpdateDepartUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TrySpendGold(DepartGoldCost))
            {
                uiController.ShowInvalid(Localization.Text("need_gold"));
                ShowDepartPrompt();
                UpdateGoldUi();
                UpdateDepartUi();
                return;
            }

            if (TryStartDepartBoost())
            {
                uiController.ShowInvalid(Localization.Text("status_departing"));
            }
            else
            {
                UserEconomy.AddGold(DepartGoldCost);
                uiController.ShowInvalid(Localization.Text("status_no_depart_target"));
                CheckBlocked();
            }

            UpdateGoldUi();
            UpdateRewardedAdUi();
        }

        private void RequestDepartSkip()
        {
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                !HasPotentialDepartTarget())
            {
                UpdateDepartUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (!UserEconomy.TryUseAdSkipTicket())
            {
                ShowDepartPrompt();
                UpdateDepartUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            if (TryStartDepartBoost())
            {
                uiController.ShowInvalid(Localization.Text("status_departing"));
            }
            else
            {
                UserEconomy.AddAdSkipTickets(1);
                uiController.ShowInvalid(Localization.Text("status_no_depart_target"));
                CheckBlocked();
            }

            UpdateRewardedAdUi();
        }

        private void RequestDepartAd()
        {
            var wasRecoveringFromFailure = gameState == GameState.Failed;
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                isVipSelectionMode ||
                IsAnyRewardedAdInProgress ||
                rewardedAdService == null ||
                !RemoteConfigService.AreRewardedAdsEnabled ||
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.DepartBoost) ||
                !HasPotentialDepartTarget())
            {
                UpdateDepartUi();
                if (wasHoldingFailureChoice)
                {
                    RecheckHeldFailureAfterRecoveryChoice();
                }

                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            isDepartAdInProgress = true;
            UpdateRewardedAdUi();

            if (!rewardedAdService.ShowDepartBoostAd(HandleDepartAdCompleted))
            {
                isDepartAdInProgress = false;
                UpdateRewardedAdUi();
                if (wasRecoveringFromFailure || wasHoldingFailureChoice)
                {
                    CheckBlocked();
                }
            }
        }

        private void HandleDepartAdCompleted(RewardedAdResult result)
        {
            isDepartAdInProgress = false;

            if (result == RewardedAdResult.RewardEarned)
            {
                if (TryStartDepartBoost())
                {
                    uiController.ShowInvalid(Localization.Text("status_departing"));
                }
                else
                {
                    uiController.ShowInvalid(Localization.Text("status_no_depart_target"));
                    CheckBlocked();
                }
            }
            else
            {
                CheckBlocked();
            }

            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                rewardedAdService?.Preload();
            }

            UpdateGoldUi();
            UpdateRewardedAdUi();
        }

        private void ResetMixShuffleState()
        {
            isMixShuffleAdInProgress = false;
        }

        private void ResetDepartState()
        {
            isDepartAdInProgress = false;
            if (departBoostRoutine == null)
            {
                return;
            }

            StopCoroutine(departBoostRoutine);
            departBoostRoutine = null;
        }

        private void ResetClearRewardDoubleState()
        {
            isClearRewardDoubleAdInProgress = false;
            currentClearGoldReward = 0;
            clearRewardDoubled = false;
        }
    }
}
