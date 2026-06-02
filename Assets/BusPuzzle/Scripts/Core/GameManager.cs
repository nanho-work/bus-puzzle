using System.Collections;
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
        [SerializeField] private int startingLevelIndex = 0;

        private const float PassengerFastForwardDuration = 2.0f;
        private const float PassengerFastForwardMultiplier = 3.0f;
        private const float BoardingUnitInterval = 0.12f;

        private readonly List<PassengerView> circulatingPassengerUnits = new List<PassengerView>();
        private readonly List<BusView> buses = new List<BusView>();

        private LevelData currentLevel;
        private GameState gameState;
        private int currentLevelIndex;
        private float passengerFastForwardTimer;
        private Coroutine boardingRoutine;

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
            if (gameState != GameState.Playing)
            {
                return;
            }

            var passengerDeltaTime = Time.deltaTime;
            if (passengerFastForwardTimer > 0f)
            {
                passengerFastForwardTimer = Mathf.Max(0f, passengerFastForwardTimer - Time.deltaTime);
                passengerDeltaTime *= PassengerFastForwardMultiplier;
            }

            boardView.UpdatePassengerTraffic(circulatingPassengerUnits, passengerDeltaTime);

            if (boardingRoutine == null && HasStationBusReadyToBoardNow())
            {
                StartBoardingResolver();
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
                TryLaunchBus(bus);
                return;
            }

            passengerFastForwardTimer = PassengerFastForwardDuration;
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
            if (boardingRoutine != null)
            {
                StopCoroutine(boardingRoutine);
                boardingRoutine = null;
            }

            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelSequence.Count - 1);
            currentLevel = levelSequence.GetLevel(currentLevelIndex);

            passengerFastForwardTimer = 0f;
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

        private void TryLaunchBus(BusView bus)
        {
            if (!bus.IsOnBoard || bus.IsMoving || bus.IsDeparted || HasMovingBus())
            {
                return;
            }

            if (!boardView.TryReserveStationSlot(out var stationSlotIndex, out var stationPosition))
            {
                uiController.ShowInvalid("Station full");
                CheckBlocked();
                return;
            }

            if (!boardView.IsPathClear(bus, buses, out _))
            {
                boardView.ReleaseStationSlot(stationSlotIndex);
                UpdateCounters();

                uiController.ShowInvalid("Blocked");
                bus.BounceBlocked(boardView.GetWorldDirection(bus), () =>
                {
                    CheckBlocked();
                });
                return;
            }

            UpdateCounters();
            uiController.ShowInvalid($"{PuzzlePalette.DisplayName(bus.Color)} bus dispatched");

            var route = boardView.BuildRouteToStation(bus, stationPosition);
            bus.MoveToStation(route, stationSlotIndex, () =>
            {
                UpdateCounters();
                uiController.ShowPlaying(currentLevel.LevelName);
                StartBoardingResolver();
                CheckBlocked();
            });
        }

        private void StartBoardingResolver()
        {
            if (boardingRoutine != null || gameState != GameState.Playing)
            {
                return;
            }

            boardingRoutine = StartCoroutine(BoardingResolverRoutine());
        }

        private IEnumerator BoardingResolverRoutine()
        {
            var didBoard = true;

            while (didBoard && gameState == GameState.Playing)
            {
                didBoard = false;

                for (var busIndex = 0; busIndex < buses.Count; busIndex++)
                {
                    var bus = buses[busIndex];
                    if (!bus.IsParkedAtStation || bus.IsDeparted || bus.IsFull)
                    {
                        continue;
                    }

                    if (!boardView.TryFindBoardingPassenger(circulatingPassengerUnits, bus.Color, out var passengerIndex))
                    {
                        continue;
                    }

                    var passenger = circulatingPassengerUnits[passengerIndex];
                    circulatingPassengerUnits.RemoveAt(passengerIndex);
                    UpdateCounters();

                    didBoard = true;
                    var boarded = false;
                    bus.BoardPassenger(passenger, () => boarded = true);
                    yield return new WaitUntil(() => boarded);
                    yield return new WaitForSeconds(BoardingUnitInterval);

                    if (bus.IsFull)
                    {
                        var stationSlotIndex = bus.StationSlotIndex;
                        var departureRoute = boardView.BuildRouteFromStation(bus);
                        var departed = false;
                        bus.Depart(departureRoute, () =>
                        {
                            boardView.ReleaseStationSlot(stationSlotIndex);
                            UpdateCounters();
                            departed = true;
                        });

                        yield return new WaitUntil(() => departed);
                    }

                    UpdateCounters();
                    break;
                }
            }

            boardingRoutine = null;

            if (circulatingPassengerUnits.Count == 0)
            {
                CompleteLevel();
                yield break;
            }

            CheckBlocked();
        }

        private bool HasStationBusReadyToBoardNow()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (!bus.IsParkedAtStation || bus.IsDeparted || bus.IsFull)
                {
                    continue;
                }

                if (boardView.TryFindBoardingPassenger(circulatingPassengerUnits, bus.Color, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasStationBusThatCanEventuallyBoard()
        {
            for (var index = 0; index < buses.Count; index++)
            {
                var bus = buses[index];
                if (bus.IsParkedAtStation && !bus.IsDeparted && !bus.IsFull && boardView.HasPassengerColor(circulatingPassengerUnits, bus.Color))
                {
                    return true;
                }
            }

            return false;
        }

        private void CompleteLevel()
        {
            gameState = GameState.Cleared;
            UpdateCounters();
            uiController.ShowClear(currentLevelIndex + 1 < levelSequence.Count);
        }

        private void FailLevel()
        {
            gameState = GameState.Failed;
            uiController.ShowFailed();
        }

        private void CheckBlocked()
        {
            if (gameState != GameState.Playing || circulatingPassengerUnits.Count == 0 || boardingRoutine != null || HasMovingBus())
            {
                return;
            }

            if (HasStationBusReadyToBoardNow())
            {
                StartBoardingResolver();
                return;
            }

            if (HasStationBusThatCanEventuallyBoard())
            {
                return;
            }

            if (boardView.IsAnyMoveAvailable(buses))
            {
                return;
            }

            FailLevel();
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
