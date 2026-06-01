using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        [SerializeField] private int startingLevelIndex;

        private readonly List<PassengerView> waitingPassengers = new List<PassengerView>();
        private readonly List<BusView> buses = new List<BusView>();

        private LevelData currentLevel;
        private GameState gameState;
        private int currentLevelIndex;
        private bool inputLocked;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            EnsureSceneDependencies();
            LoadLevel(Mathf.Max(0, startingLevelIndex));
        }

        private void OnDestroy()
        {
            if (uiController == null)
            {
                return;
            }

            uiController.RestartRequested -= RestartLevel;
            uiController.NextLevelRequested -= LoadNextLevel;
        }

        private void Update()
        {
            if (gameState != GameState.Playing || inputLocked)
            {
                return;
            }

            if (!TryGetPointerDown(out var screenPosition, out var pointerId))
            {
                return;
            }

            if (IsPointerOverUi(pointerId))
            {
                return;
            }

            var bus = TryGetBusAtScreenPosition(screenPosition);
            if (bus != null)
            {
                TryBoardBus(bus);
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

        private void LoadLevel(int levelIndex)
        {
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
            currentLevel = levelSequence.GetLevel(currentLevelIndex);

            inputLocked = false;
            gameState = GameState.Playing;

            boardView.BuildLevel(currentLevel, waitingPassengers, buses);

            uiController.SetLevel(currentLevelIndex + 1, levelSequence.Count);
            uiController.SetRemaining(waitingPassengers.Count);
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

        private void TryBoardBus(BusView bus)
        {
            if (waitingPassengers.Count == 0)
            {
                return;
            }

            var passenger = waitingPassengers[0];
            if (!bus.CanBoard(passenger))
            {
                bus.ShowInvalidFeedback();
                uiController.ShowInvalid($"{PuzzlePalette.DisplayName(passenger.Color)} passenger");
                CheckBlocked();
                return;
            }

            inputLocked = true;
            waitingPassengers.RemoveAt(0);
            boardView.LayoutWaitingPassengers(waitingPassengers, true);
            uiController.SetRemaining(waitingPassengers.Count);

            bus.BoardPassenger(passenger, () =>
            {
                inputLocked = false;

                if (waitingPassengers.Count == 0)
                {
                    CompleteLevel();
                    return;
                }

                uiController.ShowPlaying(currentLevel.LevelName);
                CheckBlocked();
            });
        }

        private void CompleteLevel()
        {
            gameState = GameState.Cleared;
            inputLocked = false;
            uiController.SetRemaining(0);
            uiController.ShowClear(currentLevelIndex + 1 < levelSequence.Count);
        }

        private void FailLevel()
        {
            gameState = GameState.Failed;
            inputLocked = false;
            uiController.ShowFailed();
        }

        private void CheckBlocked()
        {
            if (gameState != GameState.Playing || waitingPassengers.Count == 0)
            {
                return;
            }

            var nextColor = waitingPassengers[0].Color;
            foreach (var bus in buses)
            {
                if (!bus.IsDeparted && !bus.IsFull && bus.Color == nextColor)
                {
                    return;
                }
            }

            FailLevel();
        }

        private BusView TryGetBusAtScreenPosition(Vector2 screenPosition)
        {
            var ray = gameCamera.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, 100f);

            foreach (var hit in hits)
            {
                var bus = hit.collider.GetComponentInParent<BusView>();
                if (bus != null)
                {
                    return bus;
                }
            }

            return null;
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    pointerId = touch.fingerId;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            screenPosition = Vector2.zero;
            pointerId = -1;
            return false;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private static Camera CreateDefaultCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.position = new Vector3(0f, 8.8f, -7.6f);
            cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.63f, 0.77f, 0.88f);
            return camera;
        }

        private static void CreateDefaultLight()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
        }
    }
}
