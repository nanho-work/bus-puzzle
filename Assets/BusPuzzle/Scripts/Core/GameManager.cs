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
        private const int EndgameRemainingBusThreshold = 4;

        private readonly List<PassengerView> circulatingPassengerUnits = new List<PassengerView>();
        private readonly List<BusView> buses = new List<BusView>();

        private LevelData currentLevel;
        private GameState gameState;
        private int currentLevelIndex;
        private GameInputController inputController;
        private VehicleDispatchController vehicleDispatchController;
        private BoardingFlowController boardingFlowController;

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

            if (uiController == null)
            {
                return;
            }

            uiController.RestartRequested -= RestartLevel;
            uiController.NextLevelRequested -= LoadNextLevel;
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

            if (inputController.TryTakeBusTap(out var bus))
            {
                vehicleDispatchController.TryLaunch(bus);
            }
        }

        private void EnsureSceneDependencies()
        {
            levelSequence = levelSequence != null ? levelSequence : Resources.Load<LevelSequence>("Levels/LevelSequence");
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
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
            currentLevel = levelSequence.GetLevel(currentLevelIndex);
            var validationReport = LevelValidator.Validate(currentLevel);
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
            UpdateCounters();

            uiController.SetLevel(currentLevelIndex + 1, levelSequence.Count);
            uiController.ShowPlaying(currentLevel.LevelName);

            CheckBlocked();
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

        private void StartBoardingResolver()
        {
            boardingFlowController.Start();
        }

        private void CompleteLevel()
        {
            gameState = GameState.Cleared;
            UpdateCounters();
            uiController.ShowClear(currentLevelIndex + 1 < levelSequence.Count);
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
            gameState = GameState.Failed;
            uiController.ShowFailed();
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
                includeBlockedChecks && boardingFlowController.HasStationBusThatCanEventuallyBoard(),
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
            camera.orthographicSize = 4.68f;
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
