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

        private enum TutorialStep
        {
            None,
            TapFirstBus,
            DepartHint,
            TapSecondBus,
            FastForwardHint,
            PlusFree,
            MixFree,
            DepartFree,
            VipHint,
            Complete
        }

        private sealed class DepartAssignment
        {
            public readonly BusView Bus;
            public readonly List<PassengerView> Passengers;

            public DepartAssignment(BusView bus, List<PassengerView> passengers)
            {
                Bus = bus;
                Passengers = passengers;
            }
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
        private const int VipTeleportGoldCost = 120;
        private const int MixShuffleGoldCost = 90;
        private const int DepartGoldCost = 90;
        private const float TutorialDepartHintMinimumSeconds = 0.75f;
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
        private IBannerAdService bannerAdService;
        private float bannerReservedHeightPixels;
        private float bannerGameplayReservedHeightPixels;
        private bool isStationUnlockAdInProgress;
        private bool isVipAdInProgress;
        private bool isMixShuffleAdInProgress;
        private bool isDepartAdInProgress;
        private bool isClearRewardDoubleAdInProgress;
        private bool isVipSelectionMode;
        private bool isFailureWaitingForRotaryFill;
        private bool isRecoveryChoiceHoldingFailure;
        private bool remoteConfigBlocksGameplay;
        private int currentClearGoldReward;
        private int vipUsesGrantedThisStage;
        private int vipTeleportTickets;
        private bool clearRewardDoubled;
        private bool tutorialFreeUseEnabledForStage;
        private bool tutorialStationUnlockFreeUsed;
        private bool tutorialMixShuffleFreeUsed;
        private bool tutorialDepartFreeUsed;
        private bool tutorialGameplayPaused;
        private int tutorialDispatchedBusCount;
        private float tutorialStepTimer;
        private float tutorialPreviousTimeScale = 1f;
        private TutorialStep tutorialStep;
        private BusView tutorialFirstBusTarget;
        private BusView tutorialSecondBusTarget;
        private BusView tutorialDepartHintBus;
        private BusView tutorialHighlightedBus;
        private Coroutine departBoostRoutine;
        private Vector2Int lastCameraFrameScreenSize;
        private Rect lastCameraFrameSafeArea;
        private float lastCameraFrameAspect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyStartupOrientation()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private void Awake()
        {
            ApplyStartupOrientation();
            MobilePerformanceProfile.Apply();
            InitializeOptionalService("Firebase anonymous auth", PlayerIdentityService.Initialize);
            InitializeOptionalService("Leaderboard", LeaderboardService.Initialize);

            EnsureSceneDependencies();
            RemoteConfigService.ValuesUpdated += ApplyRemoteConfigState;
            uiController.RemoteConfigActionRequested += HandleRemoteConfigActionRequested;
            InitializeOptionalService("Remote Config", RemoteConfigService.Initialize);
            ConfigureControllers();
            BackgroundMusicPlayer.ApplyPreferences();
            var initialLevelIndex = startingLevelIndex > 0
                ? startingLevelIndex
                : UserProgress.GetLastStageIndex(levelSequence.Count);
            LoadLevel(initialLevelIndex);
            ApplyRemoteConfigState();
        }

        private static void InitializeOptionalService(string serviceName, System.Action initialize)
        {
            try
            {
                initialize?.Invoke();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"{serviceName} initialization failed: {exception.Message}");
            }
        }

        private void OnDestroy()
        {
            SetTutorialGameplayPaused(false);
            boardingFlowController?.Stop();
            StopStagePreload();
            RemoteConfigService.ValuesUpdated -= ApplyRemoteConfigState;

            if (uiController != null)
            {
                uiController.RestartRequested -= RestartLevel;
                uiController.NextLevelRequested -= LoadNextLevel;
                uiController.ClearRewardDoubleRequested -= RequestClearRewardDoubleAd;
                uiController.ExitConfirmed -= QuitApplication;
                uiController.StationUnlockRequested -= ShowStationUnlockPrompt;
                uiController.StationUnlockConfirmed -= RequestStationSlotUnlock;
                uiController.VipTeleportRequested -= HandleVipTeleportRequested;
                uiController.VipTeleportGoldConfirmed -= RequestVipBusTeleportGold;
                uiController.VipTeleportConfirmed -= RequestVipBusTeleportAd;
                uiController.MixShuffleRequested -= HandleMixShuffleRequested;
                uiController.MixShuffleGoldConfirmed -= RequestMixShuffleGold;
                uiController.MixShuffleConfirmed -= RequestMixShuffleAd;
                uiController.DepartRequested -= HandleDepartRequested;
                uiController.DepartGoldConfirmed -= RequestDepartGold;
                uiController.DepartConfirmed -= RequestDepartAd;
                uiController.RecoveryPromptCancelled -= HandleRecoveryPromptCancelled;
                uiController.RemoteConfigActionRequested -= HandleRemoteConfigActionRequested;
                uiController.InitialNicknamePromptCompleted -= HandleInitialNicknamePromptCompleted;
            }

            if (rewardedAdService != null)
            {
                rewardedAdService.AvailabilityChanged -= UpdateRewardedAdUi;
                rewardedAdService.Shutdown();
                rewardedAdService = null;
            }

            if (bannerAdService != null)
            {
                bannerAdService.Shutdown();
                bannerAdService = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowExitPrompt();
                return;
            }

            if (remoteConfigBlocksGameplay)
            {
                return;
            }

            if (gameState != GameState.Playing)
            {
                return;
            }

            UpdateTutorial(Time.deltaTime);
            if (tutorialGameplayPaused)
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
                if (TryBlockTutorialAction(TutorialStep.PlusFree))
                {
                    return;
                }

                if (TryUseTutorialFreeStationUnlock())
                {
                    return;
                }

                if (IsTutorialActive)
                {
                    ShowCurrentTutorialMessage();
                    return;
                }

                ShowStationUnlockPrompt();
                return;
            }

            if (isFailureWaitingForRotaryFill || isRecoveryChoiceHoldingFailure)
            {
                return;
            }

            if (inputController.TryTakeBusTap(out var bus))
            {
                if (!IsTutorialBusTapAllowed(bus))
                {
                    return;
                }

                if (vehicleDispatchController.TryLaunch(bus))
                {
                    HandleTutorialBusDispatched(bus);
                }
            }
        }

        private void LateUpdate()
        {
            ReframeBoardCamera(false);
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
            uiController.ClearRewardDoubleRequested += RequestClearRewardDoubleAd;
            uiController.ExitConfirmed += QuitApplication;
            uiController.StationUnlockRequested += ShowStationUnlockPrompt;
            uiController.StationUnlockConfirmed += RequestStationSlotUnlock;
            uiController.VipTeleportRequested += HandleVipTeleportRequested;
            uiController.VipTeleportGoldConfirmed += RequestVipBusTeleportGold;
            uiController.VipTeleportConfirmed += RequestVipBusTeleportAd;
            uiController.MixShuffleRequested += HandleMixShuffleRequested;
            uiController.MixShuffleGoldConfirmed += RequestMixShuffleGold;
            uiController.MixShuffleConfirmed += RequestMixShuffleAd;
            uiController.DepartRequested += HandleDepartRequested;
            uiController.DepartGoldConfirmed += RequestDepartGold;
            uiController.DepartConfirmed += RequestDepartAd;
            uiController.RecoveryPromptCancelled += HandleRecoveryPromptCancelled;
            uiController.InitialNicknamePromptCompleted += HandleInitialNicknamePromptCompleted;

            rewardedAdService = RewardedAdServiceFactory.Create(AdMobSettings.Load());
            rewardedAdService.AvailabilityChanged += UpdateRewardedAdUi;
            rewardedAdService.Initialize();

            bannerAdService = BannerAdServiceFactory.Create(AdMobSettings.Load());
            bannerAdService.Initialize();

            gameCamera = gameCamera != null ? gameCamera : Camera.main;
            if (gameCamera == null)
            {
                gameCamera = CreateDefaultCamera();
            }

            MobilePerformanceProfile.ApplyCamera(gameCamera);

            ConfigureSceneLighting();
        }

        private LevelSequence ResolveLevelSequence()
        {
            var stageGenerationConfig = Resources.Load<StageGenerationConfig>(StageGenerationConfigResourcePath);
            var generatedSequence = Resources.Load<LevelSequence>(GeneratedLevelSequenceResourcePath);
            if (TryResolveVerifiedSequence(generatedSequence, stageGenerationConfig, "Generated level sequence", out var resolvedSequence))
            {
                return resolvedSequence;
            }

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
            if (TryResolveVerifiedSequence(activeSequence, stageGenerationConfig, "Active level sequence", out resolvedSequence))
            {
                return resolvedSequence;
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

        private static bool TryResolveVerifiedSequence(
            LevelSequence sequence,
            StageGenerationConfig config,
            string sourceName,
            out LevelSequence resolvedSequence)
        {
            resolvedSequence = null;
            if (sequence == null || sequence.Count <= 0 || !sequence.IsVerifiedGeneratedSet)
            {
                return false;
            }

            if (config == null)
            {
                resolvedSequence = sequence;
                return true;
            }

            if (sequence.Count != config.GeneratedStageCount)
            {
                Debug.LogWarning(
                    $"{sourceName} contains {sequence.Count} verified prebuilt stages, while StageGenerationConfig expects " +
                    $"{config.GeneratedStageCount}. Existing stages will be used first, and later stages will be generated at runtime.");
            }

            resolvedSequence = LevelSequence.CreateRuntimeGenerated(config, sequence.StaticLevels);
            return true;
        }

        private void ConfigureControllers()
        {
            inputController = new GameInputController(gameCamera);
            vehicleDispatchController = new VehicleDispatchController(
                boardView,
                uiController,
                buses,
                UpdateCounters,
                RevealReadyConcealedBuses,
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
            ResetDepartState();
            ResetClearRewardDoubleState();
            ResetTutorialState();
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
            UserProgress.SaveLastStageIndex(currentLevelIndex, levelSequence.Count);
            currentLevel = levelSequence.GetLevel(currentLevelIndex);
            UpdateBannerAdState(false);
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

            boardView.BuildLevel(currentLevel, circulatingPassengerUnits, buses, currentLevelIndex + 1);
            RevealReadyConcealedBuses();
            ReframeBoardCamera(true);
            UpdateCounters();
            UpdateGoldUi();
            UpdateRewardedAdUi();

            uiController.SetLevel(currentLevelIndex + 1, levelSequence.Count);
            uiController.ShowPlaying(currentLevel.LevelName);
            if (currentLevel.DifficultyProfile.Difficulty == LevelDifficulty.SuperHard)
            {
                uiController.ShowSuperHardBanner();
            }

            CheckBlocked();
            StartTutorialIfNeeded();
            ScheduleStagePreload();
        }

        private void ReframeBoardCamera(bool force)
        {
            if (gameCamera == null || boardView == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            UpdateBannerReservedArea(false);
            var safeArea = GetGameplaySafeArea();
            var aspect = gameCamera.aspect > 0.01f ? gameCamera.aspect : screenSize.x / (float)screenSize.y;
            if (!force &&
                lastCameraFrameScreenSize == screenSize &&
                lastCameraFrameSafeArea == safeArea &&
                Mathf.Abs(lastCameraFrameAspect - aspect) < 0.001f)
            {
                return;
            }

            lastCameraFrameScreenSize = screenSize;
            lastCameraFrameSafeArea = safeArea;
            lastCameraFrameAspect = aspect;

            MobilePerformanceProfile.ApplyCamera(gameCamera);
            MobilePerformanceProfile.ApplyRenderScaleForCurrentScreen();
            BoardCameraFramer.Apply(gameCamera, boardView.GetCameraContentBounds(), safeArea, screenSize);
        }

        private void UpdateBannerAdState(bool reframeCamera = true)
        {
            var stageNumber = Mathf.Max(1, currentLevelIndex + 1);
            bannerAdService?.SetStage(stageNumber);
            UpdateBannerReservedArea(reframeCamera);
        }

        private void UpdateBannerReservedArea(bool reframeCamera)
        {
            var stageNumber = Mathf.Max(1, currentLevelIndex + 1);
            var shouldReserveSpace = bannerAdService != null && bannerAdService.ShouldReserveSpace(stageNumber);
            var reservedHeight = shouldReserveSpace ? BannerAdLayout.GetReservedHeightPixels() : 0f;
            var gameplayReservedHeight = shouldReserveSpace ? BannerAdLayout.GetGameplayReservedHeightPixels() : 0f;
            uiController?.SetExternalBottomSafeAreaInsetPixels(reservedHeight);

            if (Mathf.Abs(bannerReservedHeightPixels - reservedHeight) < 0.5f &&
                Mathf.Abs(bannerGameplayReservedHeightPixels - gameplayReservedHeight) < 0.5f)
            {
                return;
            }

            bannerReservedHeightPixels = reservedHeight;
            bannerGameplayReservedHeightPixels = gameplayReservedHeight;
            if (reframeCamera)
            {
                ReframeBoardCamera(true);
            }
        }

        private Rect GetGameplaySafeArea()
        {
            var safeArea = Screen.safeArea;
            if (bannerGameplayReservedHeightPixels > 0f)
            {
                safeArea.yMin = Mathf.Min(safeArea.yMax - 1f, safeArea.yMin + bannerGameplayReservedHeightPixels);
            }

            return safeArea;
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

        private void ApplyRemoteConfigState()
        {
            if (uiController == null)
            {
                return;
            }

            remoteConfigBlocksGameplay = RemoteConfigService.IsCurrentBuildUnsupported || RemoteConfigService.MaintenanceEnabled;
            if (RemoteConfigService.IsCurrentBuildUnsupported)
            {
                uiController.ShowRemoteConfigPrompt(
                    Localization.Text("update_required"),
                    RemoteConfigService.GetUpdateMessage(),
                    Localization.Text("update"),
                    !string.IsNullOrWhiteSpace(RemoteConfigService.GetUpdateUrl()));
            }
            else if (RemoteConfigService.MaintenanceEnabled)
            {
                uiController.ShowRemoteConfigPrompt(
                    Localization.Text("maintenance_title"),
                    RemoteConfigService.GetMaintenanceMessage(),
                    string.Empty,
                    false);
            }
            else
            {
                uiController.HideRemoteConfigPrompt();
            }

            UpdateRewardedAdUi();
            UpdateBannerAdState();
        }

        private void HandleRemoteConfigActionRequested()
        {
            var updateUrl = RemoteConfigService.GetUpdateUrl();
            if (!string.IsNullOrWhiteSpace(updateUrl))
            {
                Application.OpenURL(updateUrl);
            }
        }

        private void HandleInitialNicknamePromptCompleted()
        {
            StartTutorialIfNeeded();
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
            if (TryBlockTutorialAction(TutorialStep.PlusFree))
            {
                return;
            }

            if (!CanShowRecoveryPrompt() ||
                IsAnyRewardedAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot ||
                uiController == null ||
                !RemoteConfigService.AreRewardedAdsEnabled)
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
            if (!CanShowRecoveryPrompt() ||
                IsAnyRewardedAdInProgress ||
                uiController == null ||
                rewardedAdService == null ||
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

            if (RemoteConfigService.AreRewardedAdsEnabled && !rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport))
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

            if (RemoteConfigService.AreRewardedAdsEnabled &&
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

            if (RemoteConfigService.AreRewardedAdsEnabled &&
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
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.DepartBoost),
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
                    uiController.ShowTutorialForScreen(
                        new Vector2(Screen.width * 0.50f, Screen.height * 0.42f),
                        104f,
                        Localization.Text("tutorial_fast_forward"));
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
            uiController.ShowTutorialForWorld(
                gameCamera,
                targetBus.transform.position + Vector3.up * 0.35f,
                52f,
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
            uiController.ShowTutorialForWorld(
                gameCamera,
                targetBus.transform.position + Vector3.up * 0.35f,
                54f,
                message);
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

        private bool IsAnyRewardedAdInProgress =>
            isStationUnlockAdInProgress ||
            isVipAdInProgress ||
            isMixShuffleAdInProgress ||
            isDepartAdInProgress ||
            isClearRewardDoubleAdInProgress;

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

        private int RemainingVipTeleportUses => Mathf.Max(0, VipTeleportAdLimitPerStage - vipUsesGrantedThisStage);

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
                !bus.IsConcealed &&
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
                !bus.IsConcealed &&
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

        private bool TryStartDepartBoost()
        {
            if (boardingFlowController.IsRunning || boardingFlowController.HasPendingReservations)
            {
                boardingFlowController.Reset();
            }

            if (!TryBuildDepartPlan(out var assignments))
            {
                return false;
            }

            ClearPendingFailureRecoveryState();
            ResumeFailedLevelForRecovery();
            departBoostRoutine = StartCoroutine(DepartBoostRoutine(assignments));
            return true;
        }

        private bool HasPotentialDepartTarget()
        {
            if (boardView == null ||
                boardingFlowController == null ||
                buses == null ||
                departBoostRoutine != null ||
                boardingFlowController.HasBusBoardingPassengers() ||
                HasMovingBus())
            {
                return false;
            }

            var passengerCounts = CountPotentialDepartPassengers();
            for (var slotIndex = BoardView.VipStationSlotIndex; slotIndex < boardView.StationCapacity; slotIndex++)
            {
                var bus = BoardingRuleEngine.FindStationBusAtSlot(buses, slotIndex);
                if (!CanPotentialDepartTargetBus(bus))
                {
                    continue;
                }

                if (!passengerCounts.TryGetValue(bus.Color, out var count) || count <= 0)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool TryBuildDepartPlan(out List<DepartAssignment> assignments)
        {
            assignments = null;
            if (boardView == null ||
                boardingFlowController == null ||
                buses == null ||
                departBoostRoutine != null ||
                boardingFlowController.IsRunning ||
                boardingFlowController.HasPendingReservations ||
                boardingFlowController.HasBusBoardingPassengers() ||
                HasMovingBus())
            {
                return false;
            }

            var availablePassengers = CollectDepartPassengers();
            if (availablePassengers.Count == 0)
            {
                return false;
            }

            var assignedPassengers = new HashSet<PassengerView>();
            var plan = new List<DepartAssignment>();
            for (var slotIndex = BoardView.VipStationSlotIndex; slotIndex < boardView.StationCapacity; slotIndex++)
            {
                var bus = BoardingRuleEngine.FindStationBusAtSlot(buses, slotIndex);
                if (!CanDepartTargetBus(bus))
                {
                    continue;
                }

                var remainingSeats = bus.CapacityUnits - bus.BoardedUnits;
                if (remainingSeats <= 0)
                {
                    continue;
                }

                var passengersForBus = TakeDepartPassengers(availablePassengers, assignedPassengers, bus.Color, remainingSeats);
                if (passengersForBus.Count == 0)
                {
                    continue;
                }

                for (var index = 0; index < passengersForBus.Count; index++)
                {
                    assignedPassengers.Add(passengersForBus[index]);
                }

                plan.Add(new DepartAssignment(bus, passengersForBus));
            }

            if (plan.Count == 0)
            {
                return false;
            }

            assignments = plan;
            return true;
        }

        private List<PassengerView> CollectDepartPassengers()
        {
            var passengers = new List<PassengerView>();
            for (var index = 0; index < circulatingPassengerUnits.Count; index++)
            {
                var passenger = circulatingPassengerUnits[index];
                if (CanUsePassengerForDepart(passenger))
                {
                    passengers.Add(passenger);
                }
            }

            passengers.Sort(CompareDepartPassengerPriority);
            return passengers;
        }

        private static List<PassengerView> TakeDepartPassengers(
            IReadOnlyList<PassengerView> availablePassengers,
            HashSet<PassengerView> assignedPassengers,
            PuzzleColor color,
            int amount)
        {
            var passengers = new List<PassengerView>(amount);
            for (var index = 0; index < availablePassengers.Count && passengers.Count < amount; index++)
            {
                var passenger = availablePassengers[index];
                if (assignedPassengers.Contains(passenger) || passenger.Color != color)
                {
                    continue;
                }

                passengers.Add(passenger);
            }

            return passengers;
        }

        private IEnumerator DepartBoostRoutine(IReadOnlyList<DepartAssignment> assignments)
        {
            var pendingBoardingUnits = 0;
            for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
            {
                var assignment = assignments[assignmentIndex];
                for (var passengerIndex = 0; passengerIndex < assignment.Passengers.Count; passengerIndex++)
                {
                    var passenger = assignment.Passengers[passengerIndex];
                    if (assignment.Bus == null ||
                        passenger == null ||
                        !assignment.Bus.ReserveBoardingSeat() ||
                        !circulatingPassengerUnits.Remove(passenger))
                    {
                        assignment.Bus?.CancelBoardingReservation();
                        continue;
                    }

                    pendingBoardingUnits++;
                    assignment.Bus.BoardReservedPassenger(passenger, () =>
                    {
                        pendingBoardingUnits = Mathf.Max(0, pendingBoardingUnits - 1);
                        UpdateCounters();
                    });
                }
            }

            boardView.CompactFeederQueues(circulatingPassengerUnits);
            UpdateCounters();

            while (pendingBoardingUnits > 0)
            {
                yield return null;
            }

            departBoostRoutine = null;
            UpdateCounters();
            StartBoardingResolver();
            CheckBlocked();
        }

        private Dictionary<PuzzleColor, int> CountPotentialDepartPassengers()
        {
            var counts = new Dictionary<PuzzleColor, int>();
            for (var index = 0; index < circulatingPassengerUnits.Count; index++)
            {
                var passenger = circulatingPassengerUnits[index];
                if (!CanUsePassengerForPotentialDepart(passenger))
                {
                    continue;
                }

                counts.TryGetValue(passenger.Color, out var count);
                counts[passenger.Color] = count + 1;
            }

            return counts;
        }

        private static bool CanPotentialDepartTargetBus(BusView bus)
        {
            return bus != null &&
                bus.IsParkedAtStation &&
                !bus.IsDeparted &&
                !bus.IsDeparting &&
                !bus.IsMoving &&
                !bus.IsBoardingPassengers &&
                bus.CapacityUnits > bus.BoardedUnits;
        }

        private static bool CanDepartTargetBus(BusView bus)
        {
            return bus != null &&
                bus.IsParkedAtStation &&
                !bus.IsDeparted &&
                !bus.IsDeparting &&
                !bus.IsMoving &&
                !bus.IsBoardingPassengers &&
                !bus.HasBoardingReservations &&
                bus.HasAvailableBoardingSeat;
        }

        private static bool CanUsePassengerForDepart(PassengerView passenger)
        {
            return passenger != null &&
                passenger.gameObject.activeSelf &&
                (passenger.State == PassengerState.Rotary || passenger.State == PassengerState.Feeder);
        }

        private static bool CanUsePassengerForPotentialDepart(PassengerView passenger)
        {
            return passenger != null &&
                passenger.gameObject.activeSelf &&
                (passenger.State == PassengerState.Rotary ||
                    passenger.State == PassengerState.Feeder ||
                    passenger.State == PassengerState.QueuedForBoarding);
        }

        private static int CompareDepartPassengerPriority(PassengerView first, PassengerView second)
        {
            var stateCompare = GetDepartPassengerStatePriority(first).CompareTo(GetDepartPassengerStatePriority(second));
            if (stateCompare != 0)
            {
                return stateCompare;
            }

            if (first.State == PassengerState.Feeder && second.State == PassengerState.Feeder)
            {
                var sideCompare = first.FeederSide.CompareTo(second.FeederSide);
                if (sideCompare != 0)
                {
                    return sideCompare;
                }

                var slotCompare = first.FeederSlotIndex.CompareTo(second.FeederSlotIndex);
                if (slotCompare != 0)
                {
                    return slotCompare;
                }
            }

            return first.GetInstanceID().CompareTo(second.GetInstanceID());
        }

        private static int GetDepartPassengerStatePriority(PassengerView passenger)
        {
            return passenger != null && passenger.State == PassengerState.Rotary ? 0 : 1;
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
            if (tutorialFreeUseEnabledForStage || tutorialStep != TutorialStep.None)
            {
                CompleteTutorial();
            }
            else
            {
                uiController.HideTutorial();
            }

            ExitVipSelectionModeForEndState();
            var clearedStageNumber = currentLevelIndex + 1;
            LeaderboardService.RecordStageClear(clearedStageNumber);
            if (clearedStageNumber < levelSequence.Count)
            {
                UserProgress.SaveLastStageIndex(clearedStageNumber, levelSequence.Count);
            }

            var goldReward = UserEconomy.TryGrantStageClearGold(clearedStageNumber, StageClearGoldReward)
                ? StageClearGoldReward
                : 0;
            currentClearGoldReward = goldReward;
            clearRewardDoubled = false;
            isClearRewardDoubleAdInProgress = false;
            UpdateCounters();
            UpdateGoldUi();
            uiController.ShowClear(clearedStageNumber, clearedStageNumber < levelSequence.Count, goldReward);
            UpdateRewardedAdUi();
            EffectAudioPlayer.PlayVictory();
        }

        private void RequestClearRewardDoubleAd()
        {
            if (gameState != GameState.Cleared ||
                IsAnyRewardedAdInProgress ||
                currentClearGoldReward <= 0 ||
                clearRewardDoubled ||
                rewardedAdService == null ||
                !RemoteConfigService.AreRewardedAdsEnabled ||
                !rewardedAdService.IsReadyFor(RewardedAdPlacement.StageClearDouble))
            {
                UpdateClearRewardDoubleUi();
                return;
            }

            isClearRewardDoubleAdInProgress = true;
            UpdateClearRewardDoubleUi();

            if (!rewardedAdService.ShowStageClearDoubleAd(HandleClearRewardDoubleAdCompleted))
            {
                isClearRewardDoubleAdInProgress = false;
                UpdateClearRewardDoubleUi();
            }
        }

        private void HandleClearRewardDoubleAdCompleted(RewardedAdResult result)
        {
            isClearRewardDoubleAdInProgress = false;

            if (gameState == GameState.Cleared &&
                result == RewardedAdResult.RewardEarned &&
                currentClearGoldReward > 0 &&
                !clearRewardDoubled)
            {
                UserEconomy.AddGold(currentClearGoldReward);
                clearRewardDoubled = true;
                UpdateGoldUi();
            }

            if (RemoteConfigService.AreRewardedAdsEnabled)
            {
                rewardedAdService?.Preload(RewardedAdPlacement.StageClearDouble);
            }

            UpdateClearRewardDoubleUi();
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
            ClearTutorialTargetHighlights();
            uiController.HideTutorial();
            ExitVipSelectionModeForEndState();
            UpdateRewardedAdUi();
            uiController.ShowFailed(
                boardView != null && boardView.CanUnlockStationSlot && RemoteConfigService.AreRewardedAdsEnabled,
                RemainingVipTeleportUses > 0 && HasVipTeleportTarget(),
                HasMixShuffleTarget(),
                HasPotentialDepartTarget());
            EffectAudioPlayer.PlayFail();
        }

        private void CheckBlocked()
        {
            RevealReadyConcealedBuses();

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

        private void RevealReadyConcealedBuses()
        {
            if (boardView == null || !boardView.RevealPathClearConcealedBuses(buses))
            {
                return;
            }

            ApplyVipHighlights();
            UpdateRewardedAdUi();
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
            UpdateDepartUi();
            UpdateClearRewardDoubleUi();
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
                    boardView.CanUnlockStationSlot &&
                    RemoteConfigService.AreRewardedAdsEnabled,
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
                (IsTutorialActive
                    ? tutorialStep == TutorialStep.VipHint
                    : RemainingVipTeleportUses > 0 && HasVipTeleportTarget());

            uiController.SetVipTeleport(
                vipUsesGrantedThisStage,
                VipTeleportAdLimitPerStage,
                vipTeleportTickets > 0,
                isVipSelectionMode,
                canRequest,
                UserEconomy.GoldBalance,
                VipTeleportGoldCost,
                UserEconomy.CanSpendGold(VipTeleportGoldCost),
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
                (HasMixShuffleTarget() || CanUseTutorialFreeMixShuffleNow());

            uiController.SetMixShuffle(
                canRequest,
                UserEconomy.GoldBalance,
                MixShuffleGoldCost,
                UserEconomy.CanSpendGold(MixShuffleGoldCost),
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle),
                isMixShuffleAdInProgress);
        }

        private void UpdateDepartUi()
        {
            if (uiController == null)
            {
                return;
            }

            var canRequest = gameState == GameState.Playing &&
                !isVipSelectionMode &&
                !IsAnyRewardedAdInProgress &&
                (IsTutorialActive
                    ? tutorialStep == TutorialStep.DepartFree && CanUseTutorialFreeDepartNow()
                    : HasPotentialDepartTarget());

            uiController.SetDepart(
                canRequest,
                UserEconomy.GoldBalance,
                DepartGoldCost,
                UserEconomy.CanSpendGold(DepartGoldCost),
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.DepartBoost),
                isDepartAdInProgress);
        }

        private void UpdateClearRewardDoubleUi()
        {
            if (uiController == null)
            {
                return;
            }

            var canRequest = gameState == GameState.Cleared &&
                currentClearGoldReward > 0 &&
                !clearRewardDoubled &&
                !IsAnyRewardedAdInProgress;
            var adReady = rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.StageClearDouble);
            if (gameState == GameState.Cleared &&
                currentClearGoldReward > 0 &&
                !clearRewardDoubled &&
                RemoteConfigService.AreRewardedAdsEnabled &&
                rewardedAdService != null &&
                !adReady)
            {
                rewardedAdService.Preload(RewardedAdPlacement.StageClearDouble);
            }

            uiController.SetClearRewardDouble(
                currentClearGoldReward,
                clearRewardDoubled,
                canRequest,
                adReady,
                isClearRewardDoubleAdInProgress);
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

        private static void ConfigureSceneLighting()
        {
            var keyLight = FindFirstObjectByType<Light>();
            if (keyLight == null)
            {
                keyLight = CreateDefaultLight();
            }

            ConfigureKeyLight(keyLight);
            RenderSettings.sun = keyLight;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.73f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.51f, 0.59f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.34f, 0.40f);
            RenderSettings.ambientIntensity = 0.86f;
            RenderSettings.subtractiveShadowColor = new Color(0.36f, 0.42f, 0.50f);
        }

        private static Light CreateDefaultLight()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            return light;
        }

        private static void ConfigureKeyLight(Light light)
        {
            if (light == null)
            {
                return;
            }

            var lightTravelDirection = new Vector3(0.24f, -0.82f, 0.52f).normalized;
            light.transform.rotation = Quaternion.LookRotation(lightTravelDirection, Vector3.up);
            light.type = LightType.Directional;
            light.color = new Color(1.00f, 0.96f, 0.88f);
            light.intensity = 1.12f;
            light.shadows = LightShadows.None;
        }
    }
}
