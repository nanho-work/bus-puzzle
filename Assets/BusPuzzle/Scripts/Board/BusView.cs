using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BusPuzzle
{
    public struct BusRouteStep
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public BusRouteStep(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    public sealed class BusView : MonoBehaviour
    {
        private const string EditorBusPrefabPath = "Assets/Cobra Games Studio/Low Poly Bus Pack/Prefabs/kozak_i_van.prefab";
        private const string BusPrefabResourcePath = "BusModels/kozak_i_van";
        private const float BusVisualScale = 0.67f;
        private const float SourceBusWidth = 2.413354f;
        private const float SourceBusHeight = 3.2162495f;
        private const float SourceBusLength = 8.064726f;
        private const float CounterCharacterScale = 0.085f;

        private readonly List<Transform> unitMarkers = new List<Transform>();

        private Coroutine motionRoutine;
        private GameObject directionArrow;
        private Transform boardingCounterRoot;
        private TextMesh boardingCounterText;
        private TextMesh boardingCounterShadowText;
        private float cellSize = 1.2f;
        private int boardedUnits;
        private bool usingModelVisual;
        private static GameObject cachedBusPrefab;
        private static bool reportedModelLoad;

        public PuzzleColor Color { get; private set; }
        public BusSize Size { get; private set; }
        public GridDirection Direction { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public int StationSlotIndex { get; private set; } = -1;
        public bool IsOnBoard { get; private set; }
        public bool IsParkedAtStation { get; private set; }
        public bool IsDeparted { get; private set; }
        public bool IsMoving => motionRoutine != null;
        public int SizeCells => BusSizeUtility.ToBoardCells(Size);
        public int CapacityUnits => BusSizeUtility.ToPassengerUnits(Size);
        public int CapacityPeople => BusSizeUtility.ToPeopleCapacity(Size);
        public int BoardedUnits => boardedUnits;
        public int RemainingPeople => Mathf.Max(0, (CapacityUnits - boardedUnits) * 4);
        public bool IsFull => boardedUnits >= CapacityUnits;
        private float VisualLength => Mathf.Max(cellSize * 0.72f, SizeCells * cellSize * 0.90f * BusVisualScale);
        private float VisualWidth => cellSize * 0.72f * BusVisualScale;
        private float VisualHeight => cellSize * 0.90f * BusVisualScale;
        private float VisualCharacterLength => VisualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(Size));
        private float VisualCenterZ => (VisualLength - VisualCharacterLength) * 0.5f;
        private float VisualFrontZ => VisualLength - VisualCharacterLength * 0.5f;
        private float VisualRearZ => -VisualCharacterLength * 0.5f;

        public Vector2Int FrontCell
        {
            get
            {
                return GridPosition + GridDirectionUtility.ToGridVector(Direction) * (SizeCells - 1);
            }
        }

        public static BusView Create(BusDefinition definition, Transform parent, float cellSize)
        {
            var busObject = new GameObject($"{PuzzlePalette.DisplayName(definition.Color)} {BusSizeUtility.DisplayName(definition.Size)} Bus");
            busObject.transform.SetParent(parent, false);

            var view = busObject.AddComponent<BusView>();
            view.Initialize(definition, cellSize);
            return view;
        }

        public void Initialize(BusDefinition definition, float boardCellSize)
        {
            Color = definition.Color;
            Size = definition.Size;
            Direction = definition.Direction;
            GridPosition = definition.GridPosition;
            cellSize = boardCellSize;
            boardedUnits = 0;
            StationSlotIndex = -1;
            IsOnBoard = true;
            IsParkedAtStation = false;
            IsDeparted = false;

            transform.rotation = GridDirectionUtility.ToRotation(Direction);

            usingModelVisual = CreateModelBody();
            if (!usingModelVisual)
            {
                CreateBody();
                CreateWheels();
            }

            CreateArrow();
            CreateBoardingCounter();
            CreateUnitMarkers();
        }

        public void SetGridPosition(Vector2Int gridPosition, Vector3 worldPosition)
        {
            GridPosition = gridPosition;
            transform.position = worldPosition;
        }

        public Vector3 GetRootPositionForVisualCenter(Vector3 visualCenterPosition, GridDirection facingDirection)
        {
            return visualCenterPosition - GridDirectionUtility.ToRotation(facingDirection) * new Vector3(0f, 0f, VisualCenterZ);
        }

        public bool OccupiesCell(Vector2Int cell)
        {
            if (!IsOnBoard || IsDeparted)
            {
                return false;
            }

            var step = GridDirectionUtility.ToGridVector(Direction);
            for (var index = 0; index < SizeCells; index++)
            {
                if (GridPosition + step * index == cell)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanBoard(PassengerView passenger)
        {
            return passenger != null && IsParkedAtStation && !IsDeparted && !IsFull && passenger.Color == Color;
        }

        public void MoveToStation(BusRouteStep[] route, int stationSlotIndex, Action onComplete)
        {
            if (route == null || route.Length == 0)
            {
                IsOnBoard = false;
                IsParkedAtStation = true;
                StationSlotIndex = stationSlotIndex;
                HideDirectionArrow();
                ShowBoardingCounter();
                onComplete?.Invoke();
                return;
            }

            StopMotion();
            IsOnBoard = false;
            StationSlotIndex = stationSlotIndex;
            motionRoutine = StartCoroutine(MoveRouteRoutine(route, 0.17f, () =>
            {
                IsParkedAtStation = true;
                motionRoutine = null;
                HideDirectionArrow();
                ShowBoardingCounter();
                onComplete?.Invoke();
            }));
        }

        public void BounceBlocked(Vector3 worldDirection, Action onComplete)
        {
            StopMotion();
            motionRoutine = StartCoroutine(BounceRoutine(worldDirection, onComplete));
        }

        public void BoardPassenger(PassengerView passenger, Action onComplete)
        {
            if (!CanBoard(passenger))
            {
                onComplete?.Invoke();
                return;
            }

            var absorbPosition = transform.TransformPoint(new Vector3(0f, VisualHeight + cellSize * 0.08f, VisualCenterZ));
            passenger.BeginBoarding();
            passenger.AbsorbTo(absorbPosition, 0.24f, () =>
            {
                boardedUnits++;
                UpdateBoardingCounter();
                Destroy(passenger.gameObject);
                onComplete?.Invoke();
            });
        }

        public void Depart(Action onComplete)
        {
            if (IsDeparted)
            {
                onComplete?.Invoke();
                return;
            }

            StopMotion();
            HideBoardingCounter();
            motionRoutine = StartCoroutine(DepartRoutine(onComplete));
        }

        private void LateUpdate()
        {
            if (boardingCounterRoot == null || !boardingCounterRoot.gameObject.activeSelf)
            {
                return;
            }

            FaceBoardingCounter();
        }

        private void StopMotion()
        {
            if (motionRoutine == null)
            {
                return;
            }

            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }

        private bool CreateModelBody()
        {
            var prefab = LoadBusPrefab();
            if (prefab == null)
            {
                Debug.LogWarning($"Bus model prefab not found. Checked {EditorBusPrefabPath} and Resources/{BusPrefabResourcePath}. Falling back to primitive bus.");
                return false;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = "Bus Model";
            instance.transform.localPosition = new Vector3(0f, 0.025f, VisualCenterZ);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = new Vector3(
                VisualWidth / SourceBusWidth,
                VisualHeight / SourceBusHeight,
                VisualLength / SourceBusLength);

            ApplyBusPaint(instance);
            if (!reportedModelLoad)
            {
                reportedModelLoad = true;
                Debug.Log($"Bus model applied: {prefab.name}");
            }

            return true;
        }

        private static GameObject LoadBusPrefab()
        {
            if (cachedBusPrefab != null)
            {
                return cachedBusPrefab;
            }

#if UNITY_EDITOR
            AssetDatabase.ImportAsset(EditorBusPrefabPath, ImportAssetOptions.ForceUpdate);
            cachedBusPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorBusPrefabPath);
            if (cachedBusPrefab != null)
            {
                return cachedBusPrefab;
            }
#endif

            cachedBusPrefab = Resources.Load<GameObject>(BusPrefabResourcePath);
            return cachedBusPrefab;
        }

        private void ApplyBusPaint(GameObject modelRoot)
        {
            var bodyMaterial = PuzzlePalette.CreateMaterial(Color, "Bus Model Paint");
            var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);

            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var materials = renderer.sharedMaterials;
                var changed = false;

                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (material == null || !IsPaintMaterial(material))
                    {
                        continue;
                    }

                    materials[materialIndex] = bodyMaterial;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private static bool IsPaintMaterial(Material material)
        {
            return material.name.ToLowerInvariant().Contains("color");
        }

        private void CreateBody()
        {
            var color = PuzzlePalette.ToColor(Color);
            var bodyLength = VisualLength;
            var bodyCenter = VisualCenterZ;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, VisualHeight * 0.5f, bodyCenter);
            body.transform.localScale = new Vector3(VisualWidth, VisualHeight, bodyLength);
            body.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial($"{PuzzlePalette.DisplayName(Color)} Bus Body", color);

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Front Cabin";
            cabin.transform.SetParent(transform, false);
            cabin.transform.localPosition = new Vector3(0f, VisualHeight + cellSize * 0.14f, VisualFrontZ - VisualCharacterLength * 0.18f);
            cabin.transform.localScale = new Vector3(VisualWidth * 0.86f, cellSize * 0.22f, VisualCharacterLength * 0.58f);
            cabin.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial($"{PuzzlePalette.DisplayName(Color)} Bus Cabin", PuzzlePalette.Darken(color, 0.14f));
        }

        private void CreateWheels()
        {
            var wheelMaterial = PuzzlePalette.CreateSolidMaterial("Wheel", new Color(0.08f, 0.09f, 0.11f));
            var rearZ = VisualRearZ + VisualCharacterLength * 0.20f;
            var frontZ = VisualFrontZ - VisualCharacterLength * 0.20f;
            var xOffset = VisualWidth * 0.58f;
            var wheelY = cellSize * 0.12f;
            var wheelScale = new Vector3(cellSize * 0.24f, cellSize * 0.13f, cellSize * 0.24f);
            var wheelPositions = new[]
            {
                new Vector3(-xOffset, wheelY, rearZ),
                new Vector3(xOffset, wheelY, rearZ),
                new Vector3(-xOffset, wheelY, frontZ),
                new Vector3(xOffset, wheelY, frontZ)
            };

            foreach (var localPosition in wheelPositions)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(transform, false);
                wheel.transform.localPosition = localPosition;
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = wheelScale;
                wheel.GetComponent<Renderer>().sharedMaterial = wheelMaterial;
            }
        }

        private void CreateArrow()
        {
            var arrowMaterial = PuzzlePalette.CreateSolidMaterial("White Direction Arrow", UnityEngine.Color.white);
            var arrow = new GameObject("Direction Arrow Icon");
            directionArrow = arrow;
            arrow.transform.SetParent(transform, false);
            arrow.transform.localPosition = new Vector3(0f, VisualHeight + cellSize * 0.12f, VisualCenterZ);
            arrow.transform.localRotation = Quaternion.identity;

            var meshFilter = arrow.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateArrowMesh(VisualWidth * 0.82f, VisualLength * 0.58f);

            var meshRenderer = arrow.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = arrowMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private void CreateBoardingCounter()
        {
            boardingCounterRoot = new GameObject("Boarding Counter").transform;
            boardingCounterRoot.SetParent(transform, false);
            boardingCounterRoot.localPosition = new Vector3(0f, VisualHeight + cellSize * 0.22f, VisualRearZ - cellSize * 0.18f);
            boardingCounterRoot.gameObject.SetActive(false);

            CreateCounterBadge("Counter Background Shadow", new Color(0.06f, 0.07f, 0.09f), new Vector3(cellSize * 0.009f, -cellSize * 0.009f, 0.010f));
            CreateCounterBadge("Counter Badge", PuzzlePalette.ToColor(Color), Vector3.zero);
            boardingCounterShadowText = CreateCounterText("Counter Shadow", new Color(0.02f, 0.025f, 0.03f), new Vector3(cellSize * 0.004f, -cellSize * 0.004f, -0.018f));
            boardingCounterText = CreateCounterText("Counter Text", UnityEngine.Color.white, new Vector3(0f, 0f, -0.028f));
            UpdateBoardingCounter();
        }

        private void CreateCounterBadge(string name, UnityEngine.Color color, Vector3 localPosition)
        {
            var badge = new GameObject(name);
            badge.transform.SetParent(boardingCounterRoot, false);
            badge.transform.localPosition = localPosition;

            var meshFilter = badge.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateRoundedBadgeMesh(cellSize * 0.40f, cellSize * 0.20f, cellSize * 0.085f);

            var renderer = badge.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = PuzzlePalette.CreateSolidMaterial(name, color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private TextMesh CreateCounterText(string name, UnityEngine.Color color, Vector3 localPosition)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(boardingCounterRoot, false);
            textObject.transform.localPosition = localPosition;

            var text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = cellSize * CounterCharacterScale;
            text.fontSize = 36;
            text.color = color;

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return text;
        }

        private void ShowBoardingCounter()
        {
            UpdateBoardingCounter();
            if (boardingCounterRoot != null)
            {
                boardingCounterRoot.gameObject.SetActive(true);
                FaceBoardingCounter();
            }
        }

        private void HideBoardingCounter()
        {
            if (boardingCounterRoot != null)
            {
                boardingCounterRoot.gameObject.SetActive(false);
            }
        }

        private void HideDirectionArrow()
        {
            if (directionArrow != null)
            {
                directionArrow.SetActive(false);
            }
        }

        private void FaceBoardingCounter()
        {
            var camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            boardingCounterRoot.rotation = camera.transform.rotation;
        }

        private void UpdateBoardingCounter()
        {
            var text = RemainingPeople.ToString();
            if (boardingCounterText != null)
            {
                boardingCounterText.text = text;
            }

            if (boardingCounterShadowText != null)
            {
                boardingCounterShadowText.text = text;
            }
        }

        private static Mesh CreateRoundedBadgeMesh(float width, float height, float radius)
        {
            const int cornerSegments = 5;
            radius = Mathf.Clamp(radius, 0.01f, Mathf.Min(width, height) * 0.5f);

            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var centers = new[]
            {
                new Vector2(halfWidth - radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, -halfHeight + radius),
                new Vector2(halfWidth - radius, -halfHeight + radius)
            };
            var startAngles = new[] { 0f, 90f, 180f, 270f };
            var points = new List<Vector2>();

            for (var corner = 0; corner < centers.Length; corner++)
            {
                for (var segment = 0; segment <= cornerSegments; segment++)
                {
                    var angle = (startAngles[corner] + segment * 90f / cornerSegments) * Mathf.Deg2Rad;
                    points.Add(centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }

            var vertices = new Vector3[points.Count + 1];
            vertices[0] = Vector3.zero;
            for (var index = 0; index < points.Count; index++)
            {
                vertices[index + 1] = new Vector3(points[index].x, points[index].y, 0f);
            }

            var triangles = new int[points.Count * 12];
            for (var index = 0; index < points.Count; index++)
            {
                var current = index + 1;
                var next = index + 1 == points.Count ? 1 : index + 2;
                var triangleIndex = index * 12;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = current;
                triangles[triangleIndex + 2] = next;
                triangles[triangleIndex + 3] = 0;
                triangles[triangleIndex + 4] = next;
                triangles[triangleIndex + 5] = current;
                triangles[triangleIndex + 6] = 0;
                triangles[triangleIndex + 7] = next;
                triangles[triangleIndex + 8] = current;
                triangles[triangleIndex + 9] = 0;
                triangles[triangleIndex + 10] = current;
                triangles[triangleIndex + 11] = next;
            }

            var mesh = new Mesh { name = "Counter Badge Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateArrowMesh(float width, float length)
        {
            var halfWidth = width * 0.5f;
            var tailHalfWidth = width * 0.16f;
            var halfLength = length * 0.5f;
            var headStart = halfLength - length * 0.34f;

            var mesh = new Mesh { name = "Direction Arrow Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-tailHalfWidth, 0f, -halfLength),
                new Vector3(tailHalfWidth, 0f, -halfLength),
                new Vector3(tailHalfWidth, 0f, headStart),
                new Vector3(halfWidth, 0f, headStart),
                new Vector3(0f, 0f, halfLength),
                new Vector3(-halfWidth, 0f, headStart),
                new Vector3(-tailHalfWidth, 0f, headStart)
            };
            mesh.triangles = new[]
            {
                0, 2, 1,
                0, 6, 2,
                6, 5, 2,
                5, 3, 2,
                5, 4, 3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateUnitMarkers()
        {
            var rows = Mathf.CeilToInt(CapacityUnits / 2f);
            var rearZ = VisualRearZ + VisualCharacterLength * 0.18f;
            var frontZ = VisualFrontZ - VisualCharacterLength * 0.18f;
            var markerY = usingModelVisual ? VisualHeight + cellSize * 0.12f : VisualHeight + cellSize * 0.44f;

            for (var index = 0; index < CapacityUnits; index++)
            {
                var row = index / 2;
                var column = index % 2;
                var t = rows <= 1 ? 0.5f : row / (rows - 1f);

                var marker = new GameObject($"Unit Seat {index + 1}");
                marker.name = $"Unit Seat {index + 1}";
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = new Vector3(column == 0 ? -VisualWidth * 0.24f : VisualWidth * 0.24f, markerY, Mathf.Lerp(rearZ, frontZ, t));

                unitMarkers.Add(marker.transform);
            }
        }

        private IEnumerator MoveRouteRoutine(BusRouteStep[] route, float secondsPerSegment, Action onComplete)
        {
            for (var index = 0; index < route.Length; index++)
            {
                var startPosition = transform.position;
                var startRotation = transform.rotation;
                var targetPosition = route[index].Position;
                var targetRotation = route[index].Rotation;
                var distance = Vector3.Distance(startPosition, targetPosition);
                var duration = distance < 0.01f ? 0.07f : Mathf.Max(0.07f, secondsPerSegment * Mathf.Max(1f, distance));
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                    yield return null;
                }

                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }

            onComplete?.Invoke();
        }

        private IEnumerator BounceRoutine(Vector3 worldDirection, Action onComplete)
        {
            var startPosition = transform.position;
            var targetPosition = startPosition + worldDirection.normalized * 0.25f;
            var elapsed = 0f;
            const float duration = 0.16f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = startPosition;
            motionRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator DepartRoutine(Action onComplete)
        {
            IsDeparted = true;
            IsParkedAtStation = false;
            yield return new WaitForSeconds(0.12f);

            var startPosition = transform.position;
            var targetPosition = startPosition + new Vector3(7.5f, 0f, 0.8f);
            var elapsed = 0f;
            const float duration = 0.62f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            gameObject.SetActive(false);
            motionRoutine = null;
            onComplete?.Invoke();
        }
    }
}
