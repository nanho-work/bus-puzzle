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
        private bool isVipSelectionMode;
        private int vipAdsWatchedThisStage;
        private int vipTeleportTickets;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            EnsureSceneDependencies();
            ConfigureControllers();
            LoadLevel(Mathf.Max(0, startingLevelIndex));
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
            uiController.HomeRequested -= LoadHome;
            uiController.NextLevelRequested -= LoadNextLevel;
            uiController.StationUnlockConfirmed -= RequestStationSlotUnlock;
            uiController.VipTeleportRequested -= HandleVipTeleportRequested;
            uiController.VipTeleportConfirmed -= RequestVipBusTeleportAd;

            if (rewardedAdService != null)
            {
                rewardedAdService.AvailabilityChanged -= UpdateRewardedAdUi;
            }
        }

        private void Update()
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            var passengerDeltaTime = Time.deltaTime * GetPassengerTimeMultiplier();

            boardView.UpdatePassengerTraffic(circulatingPassengerUnits, passengerDeltaTime);

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
            uiController.HomeRequested += LoadHome;
            uiController.NextLevelRequested += LoadNextLevel;
            uiController.StationUnlockConfirmed += RequestStationSlotUnlock;
            uiController.VipTeleportRequested += HandleVipTeleportRequested;
            uiController.VipTeleportConfirmed += RequestVipBusTeleportAd;

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
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
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

            boardView.BuildLevel(currentLevel, circulatingPassengerUnits, buses);
            BoardCameraFramer.Apply(gameCamera, boardView.GetCameraContentBounds());
            UpdateCounters();
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

        private void LoadHome()
        {
            LoadLevel(0);
        }

        private void LoadNextLevel()
        {
            if (gameState != GameState.Cleared || currentLevelIndex + 1 >= levelSequence.Count)
            {
                return;
            }

            LoadLevel(currentLevelIndex + 1);
        }

        private void ShowStationUnlockPrompt()
        {
            if (gameState != GameState.Playing ||
                isStationUnlockAdInProgress ||
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

            uiController.ShowStationUnlockPrompt(
                boardView.LockedStationSlots,
                rewardedAdService != null && rewardedAdService.IsReadyFor(RewardedAdPlacement.StationSlotUnlock),
                isStationUnlockAdInProgress);
        }

        private void RequestStationSlotUnlock()
        {
            if (gameState != GameState.Playing ||
                isStationUnlockAdInProgress ||
                boardView == null ||
                !boardView.CanUnlockStationSlot ||
                rewardedAdService == null)
            {
                UpdateRewardedAdUi();
                return;
            }

            isStationUnlockAdInProgress = true;
            UpdateRewardedAdUi();

            if (!rewardedAdService.ShowStationSlotUnlockAd(HandleStationSlotUnlockAdCompleted))
            {
                isStationUnlockAdInProgress = false;
                UpdateRewardedAdUi();
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

            rewardedAdService?.Preload();
            UpdateRewardedAdUi();
        }

        private void HandleVipTeleportRequested()
        {
            if (gameState != GameState.Playing)
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
                EnterVipSelectionMode();
                return;
            }

            ShowVipTeleportPrompt();
        }

        private void ShowVipTeleportPrompt()
        {
            if (gameState != GameState.Playing ||
                isVipAdInProgress ||
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

            uiController.ShowVipTeleportPrompt(
                RemainingVipTeleportAds,
                rewardedAdService.IsReadyFor(RewardedAdPlacement.VipBusTeleport),
                isVipAdInProgress);
        }

        private void RequestVipBusTeleportAd()
        {
            if (gameState != GameState.Playing ||
                isVipAdInProgress ||
                rewardedAdService == null ||
                RemainingVipTeleportAds <= 0 ||
                !HasVipTeleportTarget())
            {
                UpdateVipTeleportUi();
                return;
            }

            isVipAdInProgress = true;
            UpdateRewardedAdUi();

            if (!rewardedAdService.ShowVipBusTeleportAd(HandleVipBusTeleportAdCompleted))
            {
                isVipAdInProgress = false;
                UpdateRewardedAdUi();
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
            UpdateVipTeleportUi();
        }

        private void ExitVipSelectionMode()
        {
            isVipSelectionMode = false;
            ApplyVipHighlights();
            uiController.HideVipTeleportPrompt();
            uiController.ShowPlaying(GetCurrentLevelName());
            UpdateVipTeleportUi();
        }

        private void ExitVipSelectionModeForEndState()
        {
            isVipSelectionMode = false;
            ApplyVipHighlights();
            uiController.HideVipTeleportPrompt();
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
            ApplyVipHighlights();
            UpdateVipTeleportUi();
        }

        private void ResetVipTeleportState()
        {
            isVipAdInProgress = false;
            isVipSelectionMode = false;
            vipAdsWatchedThisStage = 0;
            vipTeleportTickets = 0;
            ApplyVipHighlights();
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
            boardingFlowController.Start();
        }

        private void CompleteLevel()
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            gameState = GameState.Cleared;
            ExitVipSelectionModeForEndState();
            UpdateCounters();
            UpdateRewardedAdUi();
            uiController.ShowClear(currentLevelIndex + 1, currentLevelIndex + 1 < levelSequence.Count);
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

            gameState = GameState.Failed;
            ExitVipSelectionModeForEndState();
            UpdateRewardedAdUi();
            uiController.ShowFailed();
            EffectAudioPlayer.PlayFail();
        }

        private void CheckBlocked()
        {
            switch (GameProgressEngine.EvaluateBlockedState(CreateProgressSnapshot(true)))
            {
                case GameProgressDecision.Complete:
                    CompleteLevel();
                    break;
                case GameProgressDecision.StartBoardingResolver:
                    StartBoardingResolver();
                    break;
                case GameProgressDecision.Fail:
                    FailLevel();
                    break;
            }
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

        private void UpdateRewardedAdUi()
        {
            UpdateStationUnlockUi();
            UpdateVipTeleportUi();
        }

        private void UpdateStationUnlockUi()
        {
            if (uiController == null || boardView == null)
            {
                return;
            }

            uiController.SetStationUnlock(
                boardView.LockedStationSlots,
                gameState == GameState.Playing && boardView.CanUnlockStationSlot,
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
                !isVipAdInProgress &&
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
