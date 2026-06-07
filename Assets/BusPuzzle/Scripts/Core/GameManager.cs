using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class GameManager : MonoBehaviour
    {
        private enum GameState
        {
            Playing,
            Cleared,
            Failed
        }

        [SerializeField] private LevelSequence levelSequence;
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameUiController uiController;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private int startingLevelIndex = 0;

        private const float PassengerFastForwardMultiplier = 3.0f;
        private const float EndgamePassengerSpeedMultiplier = 1.35f;
        private const float StagePreloadStartDelay = 0.20f;
        private const int EndgameRemainingBusThreshold = 4;
        private const int VipTeleportAdLimitPerStage = 3;
        private const int StageClearGoldReward = 30;
        private const int MixShuffleGoldCost = 90;
        private const string GeneratedLevelSequenceResourcePath = "Levels/Generated/GeneratedLevelSequence";
        private const string ActiveLevelSequenceResourcePath = "Levels/LevelSequence";
        private const string StageGenerationConfigResourcePath = "Levels/StageGenerationConfig";

        private readonly List<PassengerView> circulatingPassengerUnits = new List<PassengerView>();
        private readonly List<BusView> buses = new List<BusView>();

        private LevelData currentLevel;
        private GameState gameState;
        private int currentLevelIndex;
        private GameInputController inputController;
        private VehicleDispatchController vehicleDispatchController;
        private BoardingFlowController boardingFlowController;
        private Coroutine stagePreloadRoutine;
        private IRewardedAdService rewardedAdService;
        private bool isStationUnlockAdInProgress;
        private bool isVipAdInProgress;
        private bool isMixShuffleAdInProgress;
        private bool isVipSelectionMode;
        private bool isFailureWaitingForRotaryFill;
        private bool isRecoveryChoiceHoldingFailure;
        private int vipAdsWatchedThisStage;
        private int vipTeleportTickets;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            EnsureSceneDependencies();
            ConfigureControllers();
            BackgroundMusicPlayer.ApplyPreferences();
            var initialLevelIndex = startingLevelIndex > 0
                ? startingLevelIndex
                : UserProgress.GetLastStageIndex(levelSequence.Count);
            LoadLevel(initialLevelIndex);
        }

        private void OnDestroy()
        {
            boardingFlowController?.Stop();
            StopStagePreload();

            if (uiController == null)
            {
                return;
            }

            uiController.RestartRequested -= RestartLevel;
            uiController.NextLevelRequested -= LoadNextLevel;
            uiController.ExitConfirmed -= QuitApplication;
            uiController.StationUnlockRequested -= ShowStationUnlockPrompt;
            uiController.StationUnlockConfirmed -= RequestStationSlotUnlock;
            uiController.VipTeleportRequested -= HandleVipTeleportRequested;
            uiController.VipTeleportConfirmed -= RequestVipBusTeleportAd;
            uiController.MixShuffleRequested -= HandleMixShuffleRequested;
            uiController.MixShuffleGoldConfirmed -= RequestMixShuffleGold;
            uiController.MixShuffleConfirmed -= RequestMixShuffleAd;
            uiController.RecoveryPromptCancelled -= HandleRecoveryPromptCancelled;

            if (rewardedAdService != null)
            {
                rewardedAdService.AvailabilityChanged -= UpdateRewardedAdUi;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowExitPrompt();
                return;
            }

            if (gameState != GameState.Playing)
            {
                return;
            }

            var passengerTimeMultiplier = GetPassengerTimeMultiplier();
            var passengerDeltaTime = Time.deltaTime * passengerTimeMultiplier;

            boardView.UpdatePassengerTraffic(circulatingPassengerUnits, passengerDeltaTime, passengerTimeMultiplier);

            if (isFailureWaitingForRotaryFill && !ShouldDeferFailureUntilRotaryFill())
            {
                isFailureWaitingForRotaryFill = false;
                CheckBlocked();
                if (gameState != GameState.Playing)
                {
                    return;
                }
            }

            if (GameProgressEngine.ShouldStartBoardingResolver(
                gameState == GameState.Playing,
                boardingFlowController.IsRunning,
                boardingFlowController.HasStationBusReadyToBoardNow(),
                boardingFlowController.HasStationBusReadyToDepart()))
            {
                StartBoardingResolver();
            }

            if (isVipSelectionMode)
            {
                if (inputController.TryTakeBusTap(out var vipBus))
                {
                    TryUseVipTeleport(vipBus);
                }

                return;
            }

            if (inputController.TryTakeStationUnlockTap(out _))
            {
                ShowStationUnlockPrompt();
                return;
            }

            if (isFailureWaitingForRotaryFill || isRecoveryChoiceHoldingFailure)
            {
                return;
            }

            if (inputController.TryTakeBusTap(out var bus))
            {
                vehicleDispatchController.TryLaunch(bus);
            }
        }

        private void EnsureSceneDependencies()
        {
            levelSequence = ResolveLevelSequence();
            if (levelSequence == null || levelSequence.Count == 0)
            {
                levelSequence = LevelSequence.CreateRuntimeFallback();
            }

            boardView = boardView != null ? boardView : FindFirstObjectByType<BoardView>();
            if (boardView == null)
            {
                boardView = new GameObject("Board").AddComponent<BoardView>();
            }

            uiController = uiController != null ? uiController : FindFirstObjectByType<GameUiController>();
            if (uiController == null)
            {
                uiController = GameUiController.CreateDefault();
            }

            uiController.RestartRequested += RestartLevel;
            uiController.NextLevelRequested += LoadNextLevel;
            uiController.ExitConfirmed += QuitApplication;
            uiController.StationUnlockRequested += ShowStationUnlockPrompt;
            uiController.StationUnlockConfirmed += RequestStationSlotUnlock;
            uiController.VipTeleportRequested += HandleVipTeleportRequested;
            uiController.VipTeleportConfirmed += RequestVipBusTeleportAd;
            uiController.MixShuffleRequested += HandleMixShuffleRequested;
            uiController.MixShuffleGoldConfirmed += RequestMixShuffleGold;
            uiController.MixShuffleConfirmed += RequestMixShuffleAd;
            uiController.RecoveryPromptCancelled += HandleRecoveryPromptCancelled;

            rewardedAdService = RewardedAdServiceFactory.Create(AdMobSettings.Load());
            rewardedAdService.AvailabilityChanged += UpdateRewardedAdUi;
            rewardedAdService.Initialize();

            gameCamera = gameCamera != null ? gameCamera : Camera.main;
            if (gameCamera == null)
            {
                gameCamera = CreateDefaultCamera();
            }

            if (FindFirstObjectByType<Light>() == null)
            {
                CreateDefaultLight();
            }
        }

        private LevelSequence ResolveLevelSequence()
        {
            var generatedSequence = Resources.Load<LevelSequence>(GeneratedLevelSequenceResourcePath);
            if (IsUsableVerifiedSequence(generatedSequence))
            {
                return generatedSequence;
            }

            var stageGenerationConfig = Resources.Load<StageGenerationConfig>(StageGenerationConfigResourcePath);
            if (stageGenerationConfig != null)
            {
                if (generatedSequence == null || generatedSequence.Count == 0)
                {
                    Debug.LogWarning("Using runtime generated stage sequence from StageGenerationConfig.");
                }
                else
                {
                    Debug.LogWarning("Generated level sequence is not verified. Using runtime generated stage sequence from StageGenerationConfig.");
                }

                return LevelSequence.CreateRuntimeGenerated(stageGenerationConfig);
            }

            var activeSequence = Resources.Load<LevelSequence>(ActiveLevelSequenceResourcePath);
            if (IsUsableVerifiedSequence(activeSequence))
            {
                return activeSequence;
            }

            if (levelSequence != null && levelSequence.Count > 0)
            {
                Debug.LogWarning("Using inspector LevelSequence fallback. This sequence is not marked as a verified generated release set.");
                return levelSequence;
            }

            if (activeSequence != null && activeSequence.Count > 0)
            {
                Debug.LogWarning("Using active LevelSequence fallback. This sequence is not marked as a verified generated release set.");
                return activeSequence;
            }

            return null;
        }

        private static bool IsUsableVerifiedSequence(LevelSequence sequence)
        {
            return sequence != null && sequence.Count > 0 && sequence.IsVerifiedGeneratedSet;
        }

        private void ConfigureControllers()
        {
            inputController = new GameInputController(gameCamera);
            vehicleDispatchController = new VehicleDispatchController(
                boardView,
                uiController,
                buses,
                UpdateCounters,
                StartBoardingResolver,
                CheckBlocked,
                GetCurrentLevelName);
            boardingFlowController = new BoardingFlowController(
                this,
                boardView,
                buses,
                circulatingPassengerUnits,
                UpdateCounters,
                TryCompleteLevelIfReady,
                CheckBlocked,
                () => gameState == GameState.Playing);
        }

        private void LoadLevel(int levelIndex)
        {
            boardingFlowController.Reset();
            ResetVipTeleportState();
            ResetMixShuffleState();
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
            UserProgress.SaveLastStageIndex(currentLevelIndex, levelSequence.Count);
            currentLevel = levelSequence.GetLevel(currentLevelIndex);
            var shouldValidateExitSequence = levelSequence == null ||
                !levelSequence.UsesRuntimeGeneration &&
                !levelSequence.IsVerifiedGeneratedSet;
            var validationReport = LevelValidator.Validate(
                currentLevel,
                shouldValidateExitSequence);
            if (validationReport.HasIssues)
            {
                var validationMessage = validationReport.ToConsoleMessage(currentLevel != null ? currentLevel.LevelName : "Missing Level");
                if (validationReport.HasErrors)
                {
                    Debug.LogError(validationMessage);
                }
                else
                {
                    Debug.LogWarning(validationMessage);
                }
            }

            gameState = GameState.Playing;
            ClearPendingFailureRecoveryState();

            boardView.BuildLevel(currentLevel, circulatingPassengerUnits, buses);
            BoardCameraFramer.Apply(gameCamera, boardView.GetCameraContentBounds());
            UpdateCounters();
            UpdateGoldUi();
            UpdateRewardedAdUi();

            uiController.SetLevel(currentLevelIndex + 1, levelSequence.Count);
            uiController.ShowPlaying(currentLevel.LevelName);

            CheckBlocked();
            ScheduleStagePreload();
        }

        private void RestartLevel()
        {
            LoadLevel(currentLevelIndex);
        }

        private void LoadNextLevel()
        {
            if (gameState != GameState.Cleared || currentLevelIndex + 1 >= levelSequence.Count)
            {
                return;
            }

            LoadLevel(currentLevelIndex + 1);
        }

        private void ShowExitPrompt()
        {
            if (uiController != null)
            {
                uiController.ShowExitPrompt();
            }
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            Debug.Log("Exit requested.");
#else
            Application.Quit();
#endif
        }

        private void ShowStationUnlockPrompt()
        {
            if (!CanShowRecoveryPrompt() ||
                IsAnyRewardedAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot ||
                uiController == null)
            {
                UpdateRewardedAdUi();
                return;
            }

            if (rewardedAdService != null && !rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock))
            {
                rewardedAdService.Preload(RewardedAdPlacement.StationSlotUnlock);
            }

            HoldPendingFailureForRecoveryChoice();
            uiController.ShowStationUnlockPrompt(
                boardView.LockedStationSlots,
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock),
                isStationUnlockAdInProgress);
        }

        private void RequestStationSlotUnlock()
        {
            var wasRecoveringFromFailure = gameState == GameState.Failed;
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot ||
                rewardedAdService == null)
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

            rewardedAdService?.Preload();
            UpdateRewardedAdUi();
        }

        private void HandleVipTeleportRequested()
        {
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
            if (!CanShowRecoveryPrompt() ||
                IsAnyRewardedAdInProgress ||
                uiController == null ||
                rewardedAdService == null ||
                RemainingVipTeleportAds <= 0)
            {
                UpdateVipTeleportUi();
                return;
            }

            if (!HasVipTeleportTarget())
            {
                uiController.ShowInvalid("No VIP target");
                UpdateVipTeleportUi();
                return;
            }

            if (!rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport))
            {
                rewardedAdService.Preload(RewardedAdPlacement.VipBusTeleport);
            }

            HoldPendingFailureForRecoveryChoice();
            uiController.ShowVipTeleportPrompt(
                RemainingVipTeleportAds,
                rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport),
                isVipAdInProgress);
        }

        private void RequestVipBusTeleportAd()
        {
            var wasRecoveringFromFailure = gameState == GameState.Failed;
            var wasHoldingFailureChoice = isRecoveryChoiceHoldingFailure;
            if (!CanUseRecoveryAction() ||
                IsAnyRewardedAdInProgress ||
                rewardedAdService == null ||
                RemainingVipTeleportAds <= 0 ||
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
                vipAdsWatchedThisStage++;
                vipTeleportTickets++;
                EnterVipSelectionMode();
            }
            else
            {
                CheckBlocked();
            }

            rewardedAdService?.Preload();
            UpdateRewardedAdUi();
        }

        private void EnterVipSelectionMode()
        {
            if (vipTeleportTickets <= 0 || !HasVipTeleportTarget())
            {
                uiController.ShowInvalid(boardView != null && !boardView.CanReserveVipStationSlot ? "VIP busy" : "No VIP target");
                UpdateVipTeleportUi();
                return;
            }

            isVipSelectionMode = true;
            ApplyVipHighlights();
            uiController.ShowInvalid("Choose VIP bus");
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
                uiController.ShowInvalid("Pick waiting bus");
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
            vipAdsWatchedThisStage = 0;
            vipTeleportTickets = 0;
            ApplyVipHighlights();
        }

        private void HandleMixShuffleRequested()
        {
            if (!CanShowRecoveryPrompt() || isVipSelectionMode)
            {
                UpdateMixShuffleUi();
                return;
            }

            ShowMixShufflePrompt();
        }

        private void ShowMixShufflePrompt()
        {
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
                uiController.ShowInvalid("No mix target");
                UpdateMixShuffleUi();
                return;
            }

            if (rewardedAdService != null && !rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle))
            {
                rewardedAdService.Preload(RewardedAdPlacement.BusColorShuffle);
            }

            HoldPendingFailureForRecoveryChoice();
            uiController.ShowMixShufflePrompt(
                UserEconomy.GoldBalance,
                MixShuffleGoldCost,
                UserEconomy.CanSpendGold(MixShuffleGoldCost),
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle),
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
                uiController.ShowInvalid("Need Gold");
                ShowMixShufflePrompt();
                UpdateGoldUi();
                UpdateMixShuffleUi();
                return;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            if (TryShuffleVisibleBusColors())
            {
                uiController.ShowInvalid("Mixed");
                CheckBlocked();
            }
            else
            {
                UserEconomy.AddGold(MixShuffleGoldCost);
                uiController.ShowInvalid("No mix target");
                CheckBlocked();
            }

            UpdateGoldUi();
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
                    uiController.ShowInvalid("Mixed");
                    CheckBlocked();
                }
                else
                {
                    uiController.ShowInvalid("No mix target");
                    CheckBlocked();
                }
            }
            else
            {
                CheckBlocked();
            }

            rewardedAdService?.Preload();
            UpdateGoldUi();
            UpdateRewardedAdUi();
        }

        private void ResetMixShuffleState()
        {
            isMixShuffleAdInProgress = false;
        }

        private bool IsAnyRewardedAdInProgress =>
            isStationUnlockAdInProgress ||
            isVipAdInProgress ||
            isMixShuffleAdInProgress;

        private bool CanShowRecoveryPrompt()
        {
            return gameState == GameState.Playing || gameState == GameState.Failed;
        }

        private bool CanUseRecoveryAction()
        {
            return gameState == GameState.Playing || gameState == GameState.Failed;
        }

        private void ResumeFailedLevelForRecovery()
        {
            if (gameState != GameState.Failed)
            {
                return;
            }

            gameState = GameState.Playing;
            uiController.ShowInvalid(string.Empty);
        }

        private int RemainingVipTeleportAds => Mathf.Max(0, VipTeleportAdLimitPerStage - vipAdsWatchedThisStage);

        private bool HasVipTeleportTarget()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                if (CanVipTeleportTarget(buses[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanVipTeleportTarget(BusView bus)
        {
            return boardView != null &&
                boardView.CanReserveVipStationSlot &&
                bus != null &&
                bus.IsOnBoard &&
                !bus.IsMoving &&
                !bus.IsDeparted;
        }

        private void ApplyVipHighlights()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus != null)
                {
                    bus.SetVipHighlight(isVipSelectionMode && CanVipTeleportTarget(bus));
                }
            }
        }

        private bool HasMixShuffleTarget()
        {
            return HasMixShuffleTarget(BusSize.Small) ||
                HasMixShuffleTarget(BusSize.Medium) ||
                HasMixShuffleTarget(BusSize.Large);
        }

        private bool HasMixShuffleTarget(BusSize size)
        {
            var hasFirstColor = false;
            var firstColor = PuzzleColor.Red;

            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (!IsMixShuffleCandidate(bus) || bus.Size != size)
                {
                    continue;
                }

                if (!hasFirstColor)
                {
                    firstColor = bus.Color;
                    hasFirstColor = true;
                    continue;
                }

                if (bus.Color != firstColor)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryShuffleVisibleBusColors()
        {
            var changed = false;
            changed |= TryShuffleVisibleBusColors(BusSize.Small);
            changed |= TryShuffleVisibleBusColors(BusSize.Medium);
            changed |= TryShuffleVisibleBusColors(BusSize.Large);
            return changed;
        }

        private bool TryShuffleVisibleBusColors(BusSize size)
        {
            var group = new List<BusView>();
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (IsMixShuffleCandidate(bus) && bus.Size == size)
                {
                    group.Add(bus);
                }
            }

            if (group.Count < 2 || !HasDistinctColors(group))
            {
                return false;
            }

            var originalColors = new List<PuzzleColor>(group.Count);
            var shuffledColors = new List<PuzzleColor>(group.Count);
            for (var index = 0; index < group.Count; index++)
            {
                originalColors.Add(group[index].Color);
                shuffledColors.Add(group[index].Color);
            }

            for (var attempt = 0; attempt < 10 && AreSameColors(originalColors, shuffledColors); attempt++)
            {
                ShuffleColors(shuffledColors);
            }

            for (var attempt = 0; attempt < group.Count && AreSameColors(originalColors, shuffledColors); attempt++)
            {
                RotateColors(shuffledColors);
            }

            if (AreSameColors(originalColors, shuffledColors))
            {
                return false;
            }

            for (var index = 0; index < group.Count; index++)
            {
                group[index].Recolor(shuffledColors[index]);
            }

            return true;
        }

        private static bool IsMixShuffleCandidate(BusView bus)
        {
            return bus != null &&
                bus.IsOnBoard &&
                !bus.IsMoving &&
                !bus.IsDeparted;
        }

        private static bool HasDistinctColors(IReadOnlyList<BusView> group)
        {
            var firstColor = group[0].Color;
            for (var index = 1; index < group.Count; index++)
            {
                if (group[index].Color != firstColor)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ShuffleColors(List<PuzzleColor> colors)
        {
            for (var index = colors.Count - 1; index > 0; index--)
            {
                var swapIndex = Random.Range(0, index + 1);
                var temp = colors[index];
                colors[index] = colors[swapIndex];
                colors[swapIndex] = temp;
            }
        }

        private static void RotateColors(List<PuzzleColor> colors)
        {
            if (colors.Count < 2)
            {
                return;
            }

            var first = colors[0];
            for (var index = 0; index < colors.Count - 1; index++)
            {
                colors[index] = colors[index + 1];
            }

            colors[colors.Count - 1] = first;
        }

        private static bool AreSameColors(IReadOnlyList<PuzzleColor> first, IReadOnlyList<PuzzleColor> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (var index = 0; index < first.Count; index++)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void ScheduleStagePreload()
        {
            StopStagePreload();
            if (levelSequence == null || !levelSequence.UsesRuntimeGeneration || levelSequence.RuntimePreloadAheadCount <= 0)
            {
                return;
            }

            stagePreloadRoutine = StartCoroutine(PreloadUpcomingStagesRoutine(currentLevelIndex));
        }

        private void StopStagePreload()
        {
            if (stagePreloadRoutine == null)
            {
                return;
            }

            StopCoroutine(stagePreloadRoutine);
            stagePreloadRoutine = null;
        }

        private IEnumerator PreloadUpcomingStagesRoutine(int baseLevelIndex)
        {
            yield return new WaitForSeconds(StagePreloadStartDelay);

            for (var offset = 1; offset <= levelSequence.RuntimePreloadAheadCount; offset++)
            {
                var levelIndex = baseLevelIndex + offset;
                if (levelIndex >= levelSequence.Count)
                {
                    break;
                }

                if (!levelSequence.IsLevelCached(levelIndex))
                {
                    levelSequence.PreloadLevel(levelIndex);
                    yield return null;
                }
            }

            stagePreloadRoutine = null;
        }

        private void StartBoardingResolver()
        {
            ClearPendingFailureRecoveryState();
            boardingFlowController.Start();
        }

        private void CompleteLevel()
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            ClearPendingFailureRecoveryState();
            gameState = GameState.Cleared;
            ExitVipSelectionModeForEndState();
            if (currentLevelIndex + 1 < levelSequence.Count)
            {
                UserProgress.SaveLastStageIndex(currentLevelIndex + 1, levelSequence.Count);
            }

            var goldReward = UserEconomy.TryGrantStageClearGold(currentLevelIndex + 1, StageClearGoldReward)
                ? StageClearGoldReward
                : 0;
            UpdateCounters();
            UpdateGoldUi();
            UpdateRewardedAdUi();
            uiController.ShowClear(currentLevelIndex + 1, currentLevelIndex + 1 < levelSequence.Count, goldReward);
            EffectAudioPlayer.PlayVictory();
        }

        private bool TryCompleteLevelIfReady()
        {
            if (!GameProgressEngine.CanComplete(CreateProgressSnapshot(false)))
            {
                return false;
            }

            CompleteLevel();
            return true;
        }

        private void FailLevel()
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            ClearPendingFailureRecoveryState();
            gameState = GameState.Failed;
            ExitVipSelectionModeForEndState();
            UpdateRewardedAdUi();
            uiController.ShowFailed(
                boardView != null && boardView.CanUnlockStationSlot,
                rewardedAdService != null && RemainingVipTeleportAds > 0 && HasVipTeleportTarget(),
                HasMixShuffleTarget());
            EffectAudioPlayer.PlayFail();
        }

        private void CheckBlocked()
        {
            switch (GameProgressEngine.EvaluateBlockedState(CreateProgressSnapshot(true)))
            {
                case GameProgressDecision.Complete:
                    ClearPendingFailureRecoveryState();
                    CompleteLevel();
                    break;
                case GameProgressDecision.StartBoardingResolver:
                    ClearPendingFailureRecoveryState();
                    StartBoardingResolver();
                    break;
                case GameProgressDecision.Fail:
                    if (ShouldDeferFailureUntilRotaryFill())
                    {
                        isFailureWaitingForRotaryFill = true;
                        isRecoveryChoiceHoldingFailure = false;
                        break;
                    }

                    ClearPendingFailureRecoveryState();
                    FailLevel();
                    break;
                default:
                    ClearPendingFailureRecoveryState();
                    break;
            }
        }

        private bool ShouldDeferFailureUntilRotaryFill()
        {
            return boardView != null && boardView.HasPendingRotaryFill(circulatingPassengerUnits);
        }

        private void HoldPendingFailureForRecoveryChoice()
        {
            if (!isFailureWaitingForRotaryFill)
            {
                return;
            }

            isFailureWaitingForRotaryFill = false;
            isRecoveryChoiceHoldingFailure = true;
        }

        private void ClearPendingFailureRecoveryState()
        {
            isFailureWaitingForRotaryFill = false;
            isRecoveryChoiceHoldingFailure = false;
        }

        private void HandleRecoveryPromptCancelled()
        {
            RecheckHeldFailureAfterRecoveryChoice();
        }

        private void RecheckHeldFailureAfterRecoveryChoice()
        {
            if (!isRecoveryChoiceHoldingFailure)
            {
                return;
            }

            isRecoveryChoiceHoldingFailure = false;
            CheckBlocked();
        }

        private GameProgressSnapshot CreateProgressSnapshot(bool includeBlockedChecks)
        {
            return new GameProgressSnapshot(
                gameState == GameState.Playing,
                boardingFlowController.IsRunning,
                circulatingPassengerUnits.Count,
                boardingFlowController.HasPendingReservations,
                boardingFlowController.HasBusBoardingPassengers(),
                HasMovingBus(),
                boardingFlowController.HasStationBusReadyToDepart(),
                includeBlockedChecks && boardingFlowController.HasStationBusReadyToBoardNow(),
                includeBlockedChecks && boardingFlowController.HasStationBusThatCanBoardRotaryPassenger(),
                includeBlockedChecks && boardView.OccupiedStationSlots >= boardView.StationCapacity,
                includeBlockedChecks && boardView.IsAnyMoveAvailable(buses));
        }

        private bool HasMovingBus()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index].IsMoving)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateCounters()
        {
            uiController.SetRemaining(circulatingPassengerUnits.Count);
            uiController.SetStationSlots(boardView.OccupiedStationSlots, boardView.StationCapacity);
            UpdateRewardedAdUi();
        }

        private void UpdateGoldUi()
        {
            uiController.SetGold(UserEconomy.GoldBalance);
        }

        private void UpdateRewardedAdUi()
        {
            UpdateStationUnlockUi();
            UpdateVipTeleportUi();
            UpdateMixShuffleUi();
        }

        private void UpdateStationUnlockUi()
        {
            if (uiController == null || boardView == null)
            {
                return;
            }

            uiController.SetStationUnlock(
                boardView.LockedStationSlots,
                gameState == GameState.Playing &&
                    !IsAnyRewardedAdInProgress &&
                    boardView.CanUnlockStationSlot,
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock),
                isStationUnlockAdInProgress);
        }

        private void UpdateVipTeleportUi()
        {
            if (uiController == null)
            {
                return;
            }

            var canRequest = gameState == GameState.Playing &&
                !IsAnyRewardedAdInProgress &&
                RemainingVipTeleportAds > 0 &&
                HasVipTeleportTarget();

            uiController.SetVipTeleport(
                RemainingVipTeleportAds,
                vipTeleportTickets > 0,
                isVipSelectionMode,
                canRequest,
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport),
                isVipAdInProgress);
        }

        private void UpdateMixShuffleUi()
        {
            if (uiController == null)
            {
                return;
            }

            var canRequest = gameState == GameState.Playing &&
                !isVipSelectionMode &&
                !IsAnyRewardedAdInProgress &&
                HasMixShuffleTarget();

            uiController.SetMixShuffle(
                canRequest,
                UserEconomy.GoldBalance,
                MixShuffleGoldCost,
                UserEconomy.CanSpendGold(MixShuffleGoldCost),
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle),
                isMixShuffleAdInProgress);
        }

        private bool IsPassengerFastForwardHeld()
        {
            return inputController.IsPassengerFastForwardHeld();
        }

        private float GetPassengerTimeMultiplier()
        {
            if (IsPassengerFastForwardHeld())
            {
                return PassengerFastForwardMultiplier;
            }

            return CountRemainingActiveBuses() <= EndgameRemainingBusThreshold
                ? EndgamePassengerSpeedMultiplier
                : 1f;
        }

        private int CountRemainingActiveBuses()
        {
            var remaining = 0;
            for (var index = 0; index < buses.Count; index++)
            {
                if (buses[index] != null && !buses[index].IsDeparted)
                {
                    remaining++;
                }
            }

            return remaining;
        }

        private string GetCurrentLevelName()
        {
            return currentLevel != null ? currentLevel.LevelName : string.Empty;
        }

        private static Camera CreateDefaultCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 7.2f, -3.75f);
            cameraObject.transform.rotation = Quaternion.Euler(62f, 0f, 0f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.82f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.69f, 0.80f);
            return camera;
        }

        private static void CreateDefaultLight()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.95f;
        }
    }
}
