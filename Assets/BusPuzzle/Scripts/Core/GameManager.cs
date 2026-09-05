using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed partial class GameManager : MonoBehaviour
    {
        private enum GameState
        {
            Loading,
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
        private const float ClearNextStagePreloadSettleSeconds = 0.55f;
        private const float ClearNextStageMinimumPreparingSeconds = 1.25f;
        private const float RuntimeGenerationClearWaitSeconds = 3.25f;
        private const float StageTransitionLoadingSettleSeconds = 0.05f;
        private const float DailyChallengeLoadingSettleSeconds = 0.08f;
        private const int EndgameRemainingBusThreshold = 4;
        private const int VipTeleportAdLimitPerStage = 3;
        private const int StageClearGoldReward = 30;
        private const int StationUnlockGoldCost = 300;
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
        private bool isDailyChallengeMode;
        private int activeDailyChallengeStepIndex;
        private string activeDailyChallengeDateKey;
        private int dailyChallengeReturnLevelIndex;
        private GameInputController inputController;
        private VehicleDispatchController vehicleDispatchController;
        private BoardingFlowController boardingFlowController;
        private Coroutine initialLevelLoadRoutine;
        private Coroutine runtimeAheadPreloadRoutine;
        private Coroutine clearNextStagePreloadRoutine;
        private Coroutine nextLevelLoadRoutine;
        private Coroutine dailyChallengeStartRoutine;
        private Coroutine dailyRewardPromptRoutine;
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
        private bool isShuttingDown;
        private Vector2Int lastCameraFrameScreenSize;
        private Rect lastCameraFrameSafeArea;
        private float lastCameraFrameAspect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyStartupOrientation()
        {
#if UNITY_EDITOR
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
#endif
        }

        private void Awake()
        {
            gameState = GameState.Loading;
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
            initialLevelLoadRoutine = StartCoroutine(LoadInitialLevelRoutine(initialLevelIndex));
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
            isShuttingDown = true;
            SetTutorialGameplayPaused(false);
            boardingFlowController?.Stop();
            StopInitialLevelLoad();
            StopRuntimeAheadPreload();
            StopClearNextStagePreload();
            StopNextLevelLoad();
            StopDailyChallengeStart();
            StopDailyRewardPromptCheck();
            RemoteConfigService.ValuesUpdated -= ApplyRemoteConfigState;

            if (uiController != null)
            {
                uiController.RestartRequested -= RestartLevel;
                uiController.NextLevelRequested -= LoadNextLevel;
                uiController.ClearRewardDoubleRequested -= RequestClearRewardDoubleAd;
                uiController.ExitConfirmed -= QuitApplication;
                uiController.StationUnlockRequested -= ShowStationUnlockPrompt;
                uiController.StationUnlockConfirmed -= RequestStationSlotUnlock;
                uiController.StationUnlockGoldConfirmed -= RequestStationSlotUnlockGold;
                uiController.StationUnlockSkipConfirmed -= RequestStationSlotUnlockSkip;
                uiController.VipTeleportRequested -= HandleVipTeleportRequested;
                uiController.VipTeleportGoldConfirmed -= RequestVipBusTeleportGold;
                uiController.VipTeleportConfirmed -= RequestVipBusTeleportAd;
                uiController.VipTeleportSkipConfirmed -= RequestVipBusTeleportSkip;
                uiController.MixShuffleRequested -= HandleMixShuffleRequested;
                uiController.MixShuffleGoldConfirmed -= RequestMixShuffleGold;
                uiController.MixShuffleConfirmed -= RequestMixShuffleAd;
                uiController.MixShuffleSkipConfirmed -= RequestMixShuffleSkip;
                uiController.DepartRequested -= HandleDepartRequested;
                uiController.DepartGoldConfirmed -= RequestDepartGold;
                uiController.DepartConfirmed -= RequestDepartAd;
                uiController.DepartSkipConfirmed -= RequestDepartSkip;
                uiController.DailyRewardRequested -= ShowDailyRewardPrompt;
                uiController.DailyRewardClaimRequested -= ClaimDailyReward;
                uiController.DailyChallengeRequested -= ShowDailyChallengePrompt;
                uiController.DailyChallengeStartRequested -= StartDailyChallengeStep;
                uiController.DailyChallengeRewardClaimRequested -= ClaimDailyChallengeReward;
                uiController.DailyChallengeReturnRequested -= ReturnFromDailyChallenge;
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

            if (levelSequence != null &&
                levelSequence.IsTransientRuntimeSequence)
            {
                levelSequence.ReleaseRuntimeResources();
                Destroy(levelSequence);
                levelSequence = null;
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
            if (gameState == GameState.Loading || currentLevel == null)
            {
                return;
            }

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
            uiController.StationUnlockGoldConfirmed += RequestStationSlotUnlockGold;
            uiController.StationUnlockSkipConfirmed += RequestStationSlotUnlockSkip;
            uiController.VipTeleportRequested += HandleVipTeleportRequested;
            uiController.VipTeleportGoldConfirmed += RequestVipBusTeleportGold;
            uiController.VipTeleportConfirmed += RequestVipBusTeleportAd;
            uiController.VipTeleportSkipConfirmed += RequestVipBusTeleportSkip;
            uiController.MixShuffleRequested += HandleMixShuffleRequested;
            uiController.MixShuffleGoldConfirmed += RequestMixShuffleGold;
            uiController.MixShuffleConfirmed += RequestMixShuffleAd;
            uiController.MixShuffleSkipConfirmed += RequestMixShuffleSkip;
            uiController.DepartRequested += HandleDepartRequested;
            uiController.DepartGoldConfirmed += RequestDepartGold;
            uiController.DepartConfirmed += RequestDepartAd;
            uiController.DepartSkipConfirmed += RequestDepartSkip;
            uiController.DailyRewardRequested += ShowDailyRewardPrompt;
            uiController.DailyRewardClaimRequested += ClaimDailyReward;
            uiController.DailyChallengeRequested += ShowDailyChallengePrompt;
            uiController.DailyChallengeStartRequested += StartDailyChallengeStep;
            uiController.DailyChallengeRewardClaimRequested += ClaimDailyChallengeReward;
            uiController.DailyChallengeReturnRequested += ReturnFromDailyChallenge;
            uiController.RecoveryPromptCancelled += HandleRecoveryPromptCancelled;
            uiController.InitialNicknamePromptCompleted += HandleInitialNicknamePromptCompleted;

            var levelPlaySettings = LevelPlaySettings.Load();
            var adMobSettings = AdMobSettings.Load();
            rewardedAdService = RewardedAdServiceFactory.Create(
                levelPlaySettings,
                adMobSettings,
                GetRewardedAdStageContext);
            rewardedAdService.AvailabilityChanged += UpdateRewardedAdUi;
            rewardedAdService.Initialize();

            bannerAdService = BannerAdServiceFactory.Create(levelPlaySettings, adMobSettings);
            bannerAdService.Initialize();

            gameCamera = gameCamera != null ? gameCamera : Camera.main;
            if (gameCamera == null)
            {
                gameCamera = CreateDefaultCamera();
            }

            MobilePerformanceProfile.ApplyCamera(gameCamera);

            ConfigureSceneLighting();
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
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = boardView.CameraBackgroundColor;
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
            if (remoteConfigBlocksGameplay)
            {
                StopDailyRewardPromptCheck();
                uiController.HideDailyRewardPrompt();
                uiController.HideDailyChallengePrompt();
                uiController.SetDailyRewardButtonState(false, false);
                uiController.SetDailyChallengeButtonState(false, false);
            }

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
                ScheduleDailyRewardPromptCheck();
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
            ScheduleDailyRewardPromptCheck();
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            Debug.Log("Exit requested.");
#else
            Application.Quit();
#endif
        }

        private bool IsAnyRewardedAdInProgress =>
            isStationUnlockAdInProgress ||
            isVipAdInProgress ||
            isMixShuffleAdInProgress ||
            isDepartAdInProgress ||
            isClearRewardDoubleAdInProgress;

        private string GetRewardedAdStageContext()
        {
            if (!isDailyChallengeMode)
            {
                return $"main:{Mathf.Max(1, currentLevelIndex + 1)}";
            }

            if (string.IsNullOrWhiteSpace(activeDailyChallengeDateKey) ||
                activeDailyChallengeStepIndex < 1)
            {
                // Keep malformed daily state in one restrictive bucket instead of bypassing
                // stage-scoped limits with an empty context.
                return "daily:invalid";
            }

            return $"daily:{activeDailyChallengeDateKey}:{activeDailyChallengeStepIndex}";
        }

        private bool IsRewardedAdAllowed(RewardedAdPlacement placement)
        {
            if (!RemoteConfigService.AreRewardedAdsEnabled || rewardedAdService == null)
            {
                return false;
            }

            if (rewardedAdService is IRewardedAdQuotaStatusProvider quotaStatusProvider)
            {
                return quotaStatusProvider.GetQuotaDecision(placement).IsAllowed;
            }

            return true;
        }

        private static bool IsStationUnlockGoldFallbackEnabled()
        {
            return RemoteConfigService.IsReady &&
                !RemoteConfigService.AreRewardedAdsEnabled;
        }

        private bool HasStationUnlockRecoveryOption()
        {
            return IsRewardedAdAllowed(RewardedAdPlacement.StationSlotUnlock) ||
                UserEconomy.AdSkipTicketBalance > 0 ||
                (IsStationUnlockGoldFallbackEnabled() &&
                    UserEconomy.CanSpendGold(StationUnlockGoldCost));
        }

        private bool HasVipTeleportRecoveryOption()
        {
            if (vipTeleportTickets > 0)
            {
                return true;
            }

            return RemainingVipTeleportUses > 0 &&
                (UserEconomy.CanSpendGold(VipTeleportGoldCost) ||
                    UserEconomy.AdSkipTicketBalance > 0 ||
                    IsRewardedAdAllowed(RewardedAdPlacement.VipBusTeleport));
        }

        private bool HasMixShuffleRecoveryOption()
        {
            return UserEconomy.CanSpendGold(MixShuffleGoldCost) ||
                UserEconomy.AdSkipTicketBalance > 0 ||
                IsRewardedAdAllowed(RewardedAdPlacement.BusColorShuffle);
        }

        private bool HasDepartRecoveryOption()
        {
            return UserEconomy.CanSpendGold(DepartGoldCost) ||
                UserEconomy.AdSkipTicketBalance > 0 ||
                IsRewardedAdAllowed(RewardedAdPlacement.DepartBoost);
        }

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
            RefreshDailyRewardButtonState();
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
            RefreshDailyRewardButtonState();
            if (tutorialFreeUseEnabledForStage || tutorialStep != TutorialStep.None)
            {
                CompleteTutorial();
            }
            else
            {
                uiController.HideTutorial();
            }

            ExitVipSelectionModeForEndState();
            if (isDailyChallengeMode)
            {
                CompleteDailyChallengeLevel();
                return;
            }

            var clearedStageNumber = currentLevelIndex + 1;
            LeaderboardService.RecordStageClear(clearedStageNumber);

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
            ScheduleClearNextStagePreload(currentLevelIndex + 1);
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
            RefreshDailyRewardButtonState();
            ClearTutorialTargetHighlights();
            uiController.HideTutorial();
            ExitVipSelectionModeForEndState();
            UpdateRewardedAdUi();
            uiController.ShowFailed(
                boardView != null &&
                    boardView.CanUnlockStationSlot &&
                    HasStationUnlockRecoveryOption(),
                HasVipTeleportTarget() && HasVipTeleportRecoveryOption(),
                HasMixShuffleTarget() && HasMixShuffleRecoveryOption(),
                HasPotentialDepartTarget() && HasDepartRecoveryOption());
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
            UpdateFailRecoveryUi();
        }

        private void UpdateFailRecoveryUi()
        {
            if (uiController == null || boardView == null || gameState != GameState.Failed)
            {
                return;
            }

            uiController.SetFailRecoveryOptions(
                boardView.CanUnlockStationSlot &&
                    HasStationUnlockRecoveryOption(),
                HasVipTeleportTarget() && HasVipTeleportRecoveryOption(),
                HasMixShuffleTarget() && HasMixShuffleRecoveryOption(),
                HasPotentialDepartTarget() && HasDepartRecoveryOption());
        }

        private void UpdateStationUnlockUi()
        {
            if (uiController == null || boardView == null)
            {
                return;
            }

            var adSkipTickets = UserEconomy.AdSkipTicketBalance;
            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.StationSlotUnlock);
            var goldFallbackEnabled = IsStationUnlockGoldFallbackEnabled();
            var canSpendGold = goldFallbackEnabled && UserEconomy.CanSpendGold(StationUnlockGoldCost);
            uiController.SetStationUnlock(
                boardView.LockedStationSlots,
                CanShowRecoveryPrompt() &&
                    !IsAnyRewardedAdInProgress &&
                    boardView.CanUnlockStationSlot &&
                    (adAllowed || adSkipTickets > 0 || goldFallbackEnabled),
                UserEconomy.GoldBalance,
                StationUnlockGoldCost,
                goldFallbackEnabled,
                canSpendGold,
                adAllowed,
                adAllowed && rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock),
                adSkipTickets,
                isStationUnlockAdInProgress);
        }

        private void UpdateVipTeleportUi()
        {
            if (uiController == null)
            {
                return;
            }

            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.VipBusTeleport);
            var canRequest = gameState == GameState.Playing &&
                !IsAnyRewardedAdInProgress &&
                (IsTutorialActive
                    ? tutorialStep == TutorialStep.VipHint
                    : HasVipTeleportTarget() &&
                        HasVipTeleportRecoveryOption());

            uiController.SetVipTeleport(
                vipUsesGrantedThisStage,
                VipTeleportAdLimitPerStage,
                vipTeleportTickets > 0,
                isVipSelectionMode,
                canRequest,
                UserEconomy.GoldBalance,
                VipTeleportGoldCost,
                UserEconomy.CanSpendGold(VipTeleportGoldCost),
                UserEconomy.AdSkipTicketBalance,
                adAllowed,
                adAllowed && rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport),
                isVipAdInProgress);
        }

        private void UpdateMixShuffleUi()
        {
            if (uiController == null)
            {
                return;
            }

            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.BusColorShuffle);
            var canRequest = gameState == GameState.Playing &&
                !isVipSelectionMode &&
                !IsAnyRewardedAdInProgress &&
                (CanUseTutorialFreeMixShuffleNow() ||
                    (HasMixShuffleTarget() && HasMixShuffleRecoveryOption()));

            uiController.SetMixShuffle(
                canRequest,
                UserEconomy.GoldBalance,
                MixShuffleGoldCost,
                UserEconomy.CanSpendGold(MixShuffleGoldCost),
                UserEconomy.AdSkipTicketBalance,
                adAllowed,
                adAllowed && rewardedAdService.IsReadyFor(RewardedAdPlacement.BusColorShuffle),
                isMixShuffleAdInProgress);
        }

        private void UpdateDepartUi()
        {
            if (uiController == null)
            {
                return;
            }

            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.DepartBoost);
            var canRequest = gameState == GameState.Playing &&
                !isVipSelectionMode &&
                !IsAnyRewardedAdInProgress &&
                (IsTutorialActive
                    ? tutorialStep == TutorialStep.DepartFree && CanUseTutorialFreeDepartNow()
                    : HasPotentialDepartTarget() && HasDepartRecoveryOption());

            uiController.SetDepart(
                canRequest,
                UserEconomy.GoldBalance,
                DepartGoldCost,
                UserEconomy.CanSpendGold(DepartGoldCost),
                UserEconomy.AdSkipTicketBalance,
                adAllowed,
                adAllowed && rewardedAdService.IsReadyFor(RewardedAdPlacement.DepartBoost),
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
            var adAllowed = IsRewardedAdAllowed(RewardedAdPlacement.StageClearDouble);
            var adReady = adAllowed && rewardedAdService.IsReadyFor(RewardedAdPlacement.StageClearDouble);
            if (gameState == GameState.Cleared &&
                currentClearGoldReward > 0 &&
                !clearRewardDoubled &&
                adAllowed &&
                !adReady)
            {
                rewardedAdService.Preload(RewardedAdPlacement.StageClearDouble);
            }

            uiController.SetClearRewardDouble(
                currentClearGoldReward,
                clearRewardDoubled,
                canRequest,
                adAllowed,
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
            camera.backgroundColor = BoardThemePalette.FieldBase;
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
