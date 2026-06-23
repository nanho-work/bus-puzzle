using UnityEngine;

namespace BusPuzzle
{
    internal static class DailyChallengeEventMapBuilder
    {
        private const float DeckY = -0.050f;
        private const string StadiumModelResourcePath = "EventModels/DailyChallengeStadium";
        private const float StadiumModelPitchDegrees = -18f;
        private const float StadiumModelTurnDegrees = 180f;
        private static GameObject stadiumModelPrefab;
        private static bool stadiumModelPrefabLoaded;

        public static void PreloadResources()
        {
            GetStadiumModelPrefab();
        }

        public static void CreateGround(
            Transform parent,
            BoardThemeId theme)
        {
            var style = BoardThemePalette.GetStyle(theme);
            var root = new GameObject("Daily Challenge Ground").transform;
            root.SetParent(parent, false);

            var floor = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Full Floor", style.Floor);
            var playBand = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Play Band", BoardThemePalette.WithAlpha(style.RotaryDistrict, 0.56f));
            var topBand = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Station Band", BoardThemePalette.WithAlpha(style.Pavement, 0.74f));
            var softEdge = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Soft Edge", BoardThemePalette.WithAlpha(style.RailShadow, 0.22f));

            BoardGeometry.CreateFlatRect(
                "Daily Challenge Full Floor",
                root,
                new Vector3(0f, DeckY - 0.060f, 3.12f),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 1.22f, 16.20f),
                floor);

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Main Play Band",
                root,
                new Vector3(0f, DeckY - 0.047f, BoardLayoutConfig.GridCenterZ - 0.14f),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.64f, BoardLayoutConfig.GridWorldDepth + 1.10f),
                0.12f,
                playBand);

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Top Station Band",
                root,
                new Vector3(0f, DeckY - 0.043f, BoardLayoutConfig.StationZ + 4.74f),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.82f, 8.70f),
                0.12f,
                topBand);

            BoardGeometry.CreateFlatRect(
                "Daily Challenge Separator Shadow",
                root,
                new Vector3(0f, DeckY - 0.030f, BoardLayoutConfig.GridTopZ + 0.60f),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.44f, 0.050f),
                softEdge);
        }

        public static void Create(
            Transform parent,
            BoardThemeId theme)
        {
            var style = BoardThemePalette.GetStyle(theme);
            var root = new GameObject("Daily Challenge Event Map").transform;
            root.SetParent(parent, false);

            var plaza = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Challenge Plaza", style.Pavement);
            var plazaPanel = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Challenge Plaza Panel", BoardThemePalette.WithAlpha(style.PaverA, 0.44f));
            var road = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Challenge Road", style.Road);
            var roadLine = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Challenge Road Line", BoardThemePalette.WithAlpha(style.Gate, 0.68f));
            var rail = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Challenge Rail", style.Rail);
            var pole = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Challenge Pole", style.Pole);
            var lamp = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Challenge Lamp", style.Light);
            var shadow = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Challenge Soft Shadow", style.PropShadow);

            CreateEventStationDeck(root, plaza, plazaPanel, rail, shadow);
            CreateStraightChallengeRoad(root, road, roadLine, rail);
            CreateSideProps(root, pole, lamp, shadow);
        }

        public static void CreatePassengerQueueArea(
            Transform parent,
            BoardThemeId theme)
        {
            var style = BoardThemePalette.GetStyle(theme);
            var root = new GameObject("Daily Challenge Passenger Queue Area").transform;
            root.SetParent(parent, false);

            var sidewalk = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Queue Sidewalk", new Color(0.88f, 0.94f, 0.84f, 0.42f));
            var queueFloor = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Queue Path Floor", new Color(0.91f, 0.96f, 0.86f, 0.42f));
            var queueLine = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Queue Path Line", BoardThemePalette.WithAlpha(style.QueueGuide, 0.72f));
            var rail = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Queue Path Rail", BoardThemePalette.WithAlpha(style.Rail, 0.50f));
            var gate = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Queue Front Gate", style.Gate);
            var stadiumConcrete = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Concrete", new Color(0.64f, 0.72f, 0.72f));
            var stadiumConcreteShade = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Concrete Shade", new Color(0.42f, 0.51f, 0.55f));
            var stadiumUpper = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Upper Bowl", new Color(0.36f, 0.48f, 0.55f));
            var stadiumRoof = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Roof", new Color(0.93f, 0.98f, 1.00f));
            var stadiumRoofShade = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Roof Shade", new Color(0.68f, 0.78f, 0.82f));
            var stadiumGlass = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Glass", new Color(0.16f, 0.62f, 0.78f));
            var stadiumTunnel = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Entrance Tunnel", new Color(0.08f, 0.11f, 0.14f));
            var stadiumAccent = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Accent", new Color(0.02f, 0.34f, 0.55f));
            var stadiumCable = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Cable", new Color(0.95f, 0.96f, 0.97f));
            var stadiumGoldTrim = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Gold Trim", new Color(1.00f, 0.70f, 0.12f));
            var stadiumSeatGold = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Seat Gold", new Color(1.00f, 0.72f, 0.16f));
            var stadiumSeatRed = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Seat Red", new Color(0.90f, 0.16f, 0.18f));
            var stadiumSeatBlue = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Seat Blue", new Color(0.14f, 0.41f, 0.78f));
            var stadiumTurf = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Pitch Turf", new Color(0.20f, 0.56f, 0.28f));
            var stadiumLine = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Stadium Pitch Line", new Color(0.96f, 0.98f, 0.92f));
            var shadow = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Queue Building Shadow", BoardThemePalette.WithAlpha(style.RailShadow, 0.26f));
            var path = CreateQueueGuidePath(DeckY + 0.010f);

            CreateQueueSidewalk(root, sidewalk);
            CreateQueuePathSegments(root, path, 2, queueFloor, queueLine, rail);
            if (!TryCreateStadiumEntranceModel(
                    root,
                    stadiumConcrete,
                    stadiumConcreteShade,
                    stadiumUpper,
                    stadiumRoof,
                    stadiumRoofShade,
                    stadiumGlass,
                    stadiumTunnel,
                    stadiumAccent,
                    stadiumCable,
                    stadiumGoldTrim,
                    stadiumSeatGold,
                    stadiumSeatRed,
                    stadiumSeatBlue,
                    stadiumTurf,
                    stadiumLine,
                    shadow))
            {
                CreateStadiumEntrance(root, stadiumConcrete, stadiumUpper, stadiumRoof, stadiumGlass, stadiumTunnel, stadiumAccent, stadiumCable, shadow);
            }

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Queue Front Pad",
                root,
                path[0] + Vector3.up * 0.018f,
                new Vector2(0.36f, 0.20f),
                0.050f,
                gate);
        }

        public static void CreateVehiclePuzzleArea(
            Transform parent,
            BoardThemeId theme)
        {
            var style = BoardThemePalette.GetStyle(theme);
            var root = new GameObject("Daily Challenge Vehicle Puzzle Area").transform;
            root.SetParent(parent, false);

            var surface = PuzzlePalette.CreateSolidMaterial($"{style.Name} Daily Vehicle Puzzle Surface", style.YardSurface);
            var panelA = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Vehicle Puzzle Panel A", BoardThemePalette.WithAlpha(style.YardPanelA, 0.34f));
            var panelB = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Vehicle Puzzle Panel B", BoardThemePalette.WithAlpha(style.YardPanelB, 0.24f));
            var edge = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Vehicle Puzzle Edge", BoardThemePalette.WithAlpha(style.YardLine, 0.52f));
            var accent = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Daily Vehicle Puzzle Accent", BoardThemePalette.WithAlpha(style.Gate, 0.42f));
            var width = BoardLayoutConfig.GridWorldWidth + 0.36f;
            var depth = BoardLayoutConfig.GridWorldDepth + BoardLayoutConfig.UpperParkingExtensionZ + 0.28f;
            var centerZ = BoardLayoutConfig.ParkingYardCenterZ - 0.03f;

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Vehicle Puzzle Surface",
                root,
                new Vector3(0f, DeckY - 0.003f, centerZ),
                new Vector2(width, depth),
                0.12f,
                surface);

            for (var index = 0; index < 5; index++)
            {
                var z = BoardLayoutConfig.GridBottomZ + BoardLayoutConfig.UpperParkingExtensionZ * 0.5f + index * BoardLayoutConfig.CellSize * 2.15f;
                BoardGeometry.CreateFlatRoundedRect(
                    $"Daily Challenge Vehicle Puzzle Soft Row {index + 1}",
                    root,
                    new Vector3(0f, DeckY + 0.006f, z),
                    new Vector2(width * 0.88f, BoardLayoutConfig.CellSize * 1.16f),
                    0.060f,
                    index % 2 == 0 ? panelA : panelB);
            }

            BoardGeometry.CreateFlatRect(
                "Daily Challenge Vehicle Puzzle Top Edge",
                root,
                new Vector3(0f, DeckY + 0.022f, BoardLayoutConfig.ParkingYardTopZ + 0.04f),
                new Vector2(width * 0.86f, 0.026f),
                edge);

            BoardGeometry.CreateFlatRect(
                "Daily Challenge Vehicle Puzzle Bottom Edge",
                root,
                new Vector3(0f, DeckY + 0.022f, BoardLayoutConfig.GridBottomZ - 0.04f),
                new Vector2(width * 0.82f, 0.026f),
                edge);

            CreateCornerAccent(root, -width * 0.42f, BoardLayoutConfig.GridBottomZ + 0.34f, accent);
            CreateCornerAccent(root, width * 0.42f, BoardLayoutConfig.ParkingYardTopZ - 0.34f, accent);
        }

        private static Vector3[] CreateQueueGuidePath(float y)
        {
            var stationZ = BoardLayoutConfig.StationZ;
            return new[]
            {
                new Vector3(-1.72f, y, stationZ + 1.48f),
                new Vector3(1.76f, y, stationZ + 1.48f),
                new Vector3(1.76f, y, stationZ + 1.96f),
                new Vector3(1.76f, y, stationZ + 4.08f),
                new Vector3(-2.05f, y, stationZ + 4.08f),
                new Vector3(-2.05f, y, stationZ + 4.42f),
                new Vector3(2.05f, y, stationZ + 4.42f),
                new Vector3(2.05f, y, stationZ + 4.76f),
                new Vector3(-2.05f, y, stationZ + 4.76f),
                new Vector3(-2.05f, y, stationZ + 5.10f),
                new Vector3(2.05f, y, stationZ + 5.10f),
                new Vector3(2.05f, y, stationZ + 5.44f),
                new Vector3(-2.05f, y, stationZ + 5.44f),
                new Vector3(-2.05f, y, stationZ + 5.78f),
                new Vector3(2.05f, y, stationZ + 5.78f),
                new Vector3(2.05f, y, stationZ + 6.12f),
                new Vector3(-2.05f, y, stationZ + 6.12f),
                new Vector3(-2.05f, y, stationZ + 6.46f),
                new Vector3(2.05f, y, stationZ + 6.46f),
                new Vector3(2.05f, y, stationZ + 6.80f),
                new Vector3(-2.05f, y, stationZ + 6.80f),
                new Vector3(-2.05f, y, stationZ + 7.14f),
                new Vector3(2.05f, y, stationZ + 7.14f),
                new Vector3(2.05f, y, stationZ + 7.48f),
                new Vector3(-2.05f, y, stationZ + 7.48f),
                new Vector3(-2.05f, y, stationZ + 7.82f),
                new Vector3(2.05f, y, stationZ + 7.82f),
                new Vector3(2.05f, y, stationZ + 8.16f),
                new Vector3(-2.05f, y, stationZ + 8.16f)
            };
        }

        private static void CreateQueuePathSegments(
            Transform root,
            Vector3[] path,
            int visibleSegmentCount,
            Material floor,
            Material line,
            Material rail)
        {
            var segmentCount = Mathf.Clamp(visibleSegmentCount, 0, Mathf.Max(0, path.Length - 1));
            for (var index = 0; index < segmentCount; index++)
            {
                var start = path[index];
                var end = path[index + 1];
                var direction = (end - start).normalized;
                var side = Vector3.Cross(Vector3.up, direction).normalized;

                BoardGeometry.CreateFlatSegment(
                    $"Daily Challenge Single Queue Floor {index + 1}",
                    root,
                    start,
                    end,
                    DeckY + 0.010f,
                    0.280f,
                    floor);

                BoardGeometry.CreateFlatSegment(
                    $"Daily Challenge Single Queue Guide {index + 1}",
                    root,
                    start,
                    end,
                    DeckY + 0.026f,
                    0.026f,
                    line);

                BoardGeometry.CreateFlatSegment(
                    $"Daily Challenge Single Queue Rail A {index + 1}",
                    root,
                    start + side * 0.165f,
                    end + side * 0.165f,
                    DeckY + 0.030f,
                    0.018f,
                    rail);

                BoardGeometry.CreateFlatSegment(
                    $"Daily Challenge Single Queue Rail B {index + 1}",
                    root,
                    start - side * 0.165f,
                    end - side * 0.165f,
                    DeckY + 0.030f,
                    0.018f,
                    rail);
            }
        }

        private static void CreateQueueSidewalk(
            Transform root,
            Material material)
        {
            var stationZ = BoardLayoutConfig.StationZ;
            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Queue Sidewalk Pad",
                root,
                new Vector3(0f, DeckY + 0.004f, stationZ + 1.48f),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.38f, 0.46f),
                0.060f,
                material);

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Queue Entrance Pad",
                root,
                new Vector3(1.76f, DeckY + 0.005f, stationZ + 1.88f),
                new Vector2(0.48f, 0.62f),
                0.055f,
                material);
        }

        private static void CreateTerminalBuilding(
            Transform root,
            Material wall,
            Material roof,
            Material trim,
            Material window,
            Material door,
            Material shadow)
        {
            var stationZ = BoardLayoutConfig.StationZ;
            var width = BoardLayoutConfig.GridWorldWidth + 0.84f;
            var centerZ = stationZ + 2.82f;

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Queue Building Shadow",
                root,
                new Vector3(0.035f, DeckY + 0.018f, centerZ - 0.020f),
                new Vector2(width + 0.10f, 1.76f),
                0.095f,
                shadow);

            CreateBox(
                "Daily Challenge Queue Building Body",
                root,
                new Vector3(0f, 0.48f, centerZ),
                new Vector3(width, 0.40f, 1.64f),
                wall,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Queue Building Roof Cap",
                root,
                new Vector3(0f, 0.72f, centerZ + 0.10f),
                new Vector3(width + 0.12f, 0.10f, 1.48f),
                roof,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Queue Building Front Fascia",
                root,
                new Vector3(0f, 0.76f, stationZ + 2.02f),
                new Vector3(width + 0.18f, 0.12f, 0.12f),
                trim,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Queue Building Entrance",
                root,
                new Vector3(1.76f, 0.82f, stationZ + 2.00f),
                new Vector3(0.48f, 0.14f, 0.20f),
                door,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Queue Building Left Accent",
                root,
                new Vector3(-2.04f, 0.79f, centerZ + 0.08f),
                new Vector3(0.42f, 0.12f, 1.16f),
                roof,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Queue Building Right Accent",
                root,
                new Vector3(2.04f, 0.79f, centerZ + 0.08f),
                new Vector3(0.42f, 0.12f, 1.16f),
                roof,
                Quaternion.identity);

            for (var index = 0; index < 4; index++)
            {
                var x = -1.12f + index * 0.58f;
                CreateBox(
                    $"Daily Challenge Queue Building Window {index + 1}",
                    root,
                    new Vector3(x, 0.82f, stationZ + 2.46f),
                    new Vector3(0.42f, 0.055f, 0.22f),
                    window,
                    Quaternion.identity);
            }

            for (var index = 0; index < 5; index++)
            {
                var x = -1.36f + index * 0.68f;
                CreateBox(
                    $"Daily Challenge Queue Building Awning Stripe {index + 1}",
                    root,
                    new Vector3(x, 0.84f, stationZ + 2.11f),
                    new Vector3(0.34f, 0.050f, 0.13f),
                    index % 2 == 0 ? roof : window,
                    Quaternion.identity);
            }
        }

        private static bool TryCreateStadiumEntranceModel(
            Transform root,
            Material concrete,
            Material concreteShade,
            Material upperBowl,
            Material roof,
            Material roofShade,
            Material glass,
            Material tunnel,
            Material accent,
            Material cable,
            Material goldTrim,
            Material seatGold,
            Material seatRed,
            Material seatBlue,
            Material turf,
            Material line,
            Material shadow)
        {
            var prefab = GetStadiumModelPrefab();
            if (prefab == null)
            {
                return false;
            }

            var stationZ = BoardLayoutConfig.StationZ;
            var width = BoardLayoutConfig.GridWorldWidth + 0.72f;
            var centerZ = stationZ + 2.68f;
            var targetWidth = width + 0.54f;
            const float targetDepth = 1.92f;
            var stadiumRotation = Quaternion.Euler(StadiumModelPitchDegrees, StadiumModelTurnDegrees, 0f);

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Stadium Model Shadow",
                root,
                new Vector3(0.035f, DeckY + 0.018f, centerZ - 0.020f),
                new Vector2(targetWidth + 0.10f, targetDepth),
                0.12f,
                shadow);

            var instance = Object.Instantiate(prefab, root, false);
            instance.name = "Daily Challenge Stadium Model";
            instance.transform.localPosition = new Vector3(0f, 0f, centerZ);
            instance.transform.localRotation = stadiumRotation;
            instance.transform.localScale = Vector3.one;

            ApplyStadiumModelMaterials(instance, concrete, concreteShade, upperBowl, roof, roofShade, glass, tunnel, accent, cable, goldTrim, seatGold, seatRed, seatBlue, turf, line);
            FitStadiumModelToBoard(instance, targetWidth, targetDepth, new Vector3(0f, 0f, centerZ), 0.030f);
            DisableModelPhysics(instance);
            return true;
        }

        private static GameObject GetStadiumModelPrefab()
        {
            if (!stadiumModelPrefabLoaded)
            {
                stadiumModelPrefab = Resources.Load<GameObject>(StadiumModelResourcePath);
                stadiumModelPrefabLoaded = true;
            }

            return stadiumModelPrefab;
        }

        private static void ApplyStadiumModelMaterials(
            GameObject instance,
            Material concrete,
            Material concreteShade,
            Material upperBowl,
            Material roof,
            Material roofShade,
            Material glass,
            Material tunnel,
            Material accent,
            Material cable,
            Material goldTrim,
            Material seatGold,
            Material seatRed,
            Material seatBlue,
            Material turf,
            Material line)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                var sharedMaterials = renderer.sharedMaterials;
                var changed = false;
                for (var materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    var replacement = GetStadiumModelMaterial(renderer, sharedMaterials[materialIndex], concrete, concreteShade, upperBowl, roof, roofShade, glass, tunnel, accent, cable, goldTrim, seatGold, seatRed, seatBlue, turf, line);
                    if (replacement != null && replacement != sharedMaterials[materialIndex])
                    {
                        sharedMaterials[materialIndex] = replacement;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = sharedMaterials;
                }
            }
        }

        private static Material GetStadiumModelMaterial(
            Renderer renderer,
            Material current,
            Material concrete,
            Material concreteShade,
            Material upperBowl,
            Material roof,
            Material roofShade,
            Material glass,
            Material tunnel,
            Material accent,
            Material cable,
            Material goldTrim,
            Material seatGold,
            Material seatRed,
            Material seatBlue,
            Material turf,
            Material line)
        {
            var materialName = current != null ? current.name : string.Empty;
            var key = $"{renderer.name} {materialName}".ToLowerInvariant();
            if (key.Contains("goldtrim") || key.Contains("tick"))
            {
                return goldTrim;
            }

            if (key.Contains("seatgold") || key.Contains("seatyellow"))
            {
                return seatGold;
            }

            if (key.Contains("seatred"))
            {
                return seatRed;
            }

            if (key.Contains("seatblue"))
            {
                return seatBlue;
            }

            if (key.Contains("pitchline"))
            {
                return line;
            }

            if (key.Contains("pitch") || key.Contains("turf"))
            {
                return turf;
            }

            if (key.Contains("glass") || key.Contains("skylight"))
            {
                return glass;
            }

            if (key.Contains("roofshade") || key.Contains("underside"))
            {
                return roofShade;
            }

            if (key.Contains("shade") || key.Contains("shadow") || key.Contains("recess"))
            {
                return concreteShade;
            }

            if (key.Contains("tunnel") || key.Contains("interior"))
            {
                return tunnel;
            }

            if (key.Contains("cable"))
            {
                return cable;
            }

            if (key.Contains("roof") || key.Contains("mast"))
            {
                return roof;
            }

            if (key.Contains("accent") || key.Contains("sign") || key.Contains("fascia"))
            {
                return accent;
            }

            if (key.Contains("upper"))
            {
                return upperBowl;
            }

            return concrete;
        }

        private static void FitStadiumModelToBoard(
            GameObject instance,
            float targetWidth,
            float targetDepth,
            Vector3 targetCenter,
            float targetBaseY)
        {
            if (instance == null || !TryGetRendererBounds(instance, out var bounds))
            {
                return;
            }

            var widthScale = bounds.size.x > 0.001f ? targetWidth / bounds.size.x : 1f;
            var depthScale = bounds.size.z > 0.001f ? targetDepth / bounds.size.z : 1f;
            var fitScale = Mathf.Min(widthScale, depthScale);
            if (fitScale > 0.001f && !float.IsNaN(fitScale) && !float.IsInfinity(fitScale))
            {
                instance.transform.localScale *= fitScale;
            }

            if (!TryGetRendererBounds(instance, out bounds))
            {
                return;
            }

            var offset = new Vector3(
                targetCenter.x - bounds.center.x,
                targetBaseY - bounds.min.y,
                targetCenter.z - bounds.center.z);
            instance.transform.position += offset;
        }

        private static bool TryGetRendererBounds(GameObject instance, out Bounds bounds)
        {
            bounds = default;
            if (instance == null)
            {
                return false;
            }

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static void CreateStadiumEntrance(
            Transform root,
            Material concrete,
            Material upperBowl,
            Material roof,
            Material glass,
            Material tunnel,
            Material accent,
            Material cable,
            Material shadow)
        {
            var stationZ = BoardLayoutConfig.StationZ;
            var width = BoardLayoutConfig.GridWorldWidth + 0.72f;
            var centerZ = stationZ + 2.68f;
            var frontZ = stationZ + 1.96f;

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Stadium Shadow",
                root,
                new Vector3(0.035f, DeckY + 0.018f, centerZ - 0.020f),
                new Vector2(width + 0.24f, 1.78f),
                0.11f,
                shadow);

            CreateBox(
                "Daily Challenge Stadium Lower Concourse",
                root,
                new Vector3(0f, 0.44f, centerZ),
                new Vector3(width, 0.36f, 1.30f),
                concrete,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Stadium Upper Bowl",
                root,
                new Vector3(0f, 0.78f, centerZ + 0.12f),
                new Vector3(width + 0.14f, 0.24f, 0.92f),
                upperBowl,
                Quaternion.identity);

            for (var row = 0; row < 5; row++)
            {
                var rowWidth = width - row * 0.28f;
                var rowZ = stationZ + 2.30f + row * 0.16f;
                var rowY = 0.60f + row * 0.070f;
                CreateBox(
                    $"Daily Challenge Stadium Seating Step {row + 1}",
                    root,
                    new Vector3(0f, rowY, rowZ),
                    new Vector3(rowWidth, 0.052f, 0.075f),
                    row % 2 == 0 ? accent : glass,
                    Quaternion.identity);
            }

            CreateBox(
                "Daily Challenge Stadium Roof Canopy",
                root,
                new Vector3(0f, 1.04f, centerZ + 0.08f),
                new Vector3(width + 0.44f, 0.10f, 0.98f),
                roof,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Stadium Roof Front Lip",
                root,
                new Vector3(0f, 0.95f, frontZ + 0.05f),
                new Vector3(width + 0.54f, 0.13f, 0.18f),
                roof,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Stadium Dark Interior",
                root,
                new Vector3(0f, 0.82f, stationZ + 2.22f),
                new Vector3(width - 0.46f, 0.24f, 0.22f),
                tunnel,
                Quaternion.identity);

            CreateStadiumEntrancePortal(root, new Vector3(1.62f, 0f, frontZ), concrete, tunnel, roof, accent);
            CreateStadiumColumns(root, width, frontZ, concrete, accent);
            CreateStadiumMastsAndCables(root, width, frontZ, roof, cable);
        }

        private static void CreateStadiumEntrancePortal(
            Transform root,
            Vector3 basePosition,
            Material concrete,
            Material tunnel,
            Material roof,
            Material accent)
        {
            CreateBox(
                "Daily Challenge Stadium Entrance Tunnel",
                root,
                basePosition + new Vector3(0f, 0.45f, -0.070f),
                new Vector3(0.98f, 0.66f, 0.26f),
                tunnel,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Stadium Entrance Header",
                root,
                basePosition + new Vector3(0f, 0.82f, -0.085f),
                new Vector3(1.16f, 0.13f, 0.24f),
                roof,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Stadium Entrance Left Pier",
                root,
                basePosition + new Vector3(-0.57f, 0.46f, -0.075f),
                new Vector3(0.14f, 0.68f, 0.25f),
                concrete,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Stadium Entrance Right Pier",
                root,
                basePosition + new Vector3(0.57f, 0.46f, -0.075f),
                new Vector3(0.14f, 0.68f, 0.25f),
                concrete,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Stadium Entrance Sign",
                root,
                basePosition + new Vector3(0f, 0.99f, -0.100f),
                new Vector3(0.82f, 0.070f, 0.055f),
                accent,
                Quaternion.identity);
        }

        private static void CreateStadiumColumns(
            Transform root,
            float width,
            float frontZ,
            Material concrete,
            Material accent)
        {
            const int columnCount = 7;
            var minX = -width * 0.43f;
            var maxX = width * 0.43f;
            for (var index = 0; index < columnCount; index++)
            {
                var t = columnCount <= 1 ? 0f : index / (columnCount - 1f);
                var x = Mathf.Lerp(minX, maxX, t);
                var material = index % 2 == 0 ? concrete : accent;
                CreateBox(
                    $"Daily Challenge Stadium Front Column {index + 1}",
                    root,
                    new Vector3(x, 0.47f, frontZ + 0.04f),
                    new Vector3(0.090f, 0.62f, 0.12f),
                    material,
                    Quaternion.identity);
            }
        }

        private static void CreateStadiumMastsAndCables(
            Transform root,
            float width,
            float frontZ,
            Material roof,
            Material cable)
        {
            const int mastCount = 5;
            var minX = -width * 0.46f;
            var maxX = width * 0.46f;
            for (var index = 0; index < mastCount; index++)
            {
                var t = mastCount <= 1 ? 0f : index / (mastCount - 1f);
                var x = Mathf.Lerp(minX, maxX, t);
                var basePoint = new Vector3(x, 1.05f, frontZ + 0.12f);
                var tipPoint = new Vector3(x * 1.05f, 1.62f, frontZ + 0.72f);

                CreateBeam(
                    $"Daily Challenge Stadium Mast {index + 1}",
                    root,
                    basePoint,
                    tipPoint,
                    0.055f,
                    roof);

                CreateBeam(
                    $"Daily Challenge Stadium Inner Cable {index + 1}",
                    root,
                    tipPoint,
                    new Vector3(x * 0.70f, 1.06f, frontZ + 0.28f),
                    0.018f,
                    cable);

                CreateBeam(
                    $"Daily Challenge Stadium Outer Cable {index + 1}",
                    root,
                    tipPoint,
                    new Vector3(x * 1.14f, 1.03f, frontZ + 0.02f),
                    0.016f,
                    cable);
            }
        }

        private static void CreateEventStationDeck(
            Transform root,
            Material plaza,
            Material plazaPanel,
            Material rail,
            Material shadow)
        {
            var stationZ = BoardLayoutConfig.StationZ;
            var width = BoardLayoutConfig.GridWorldWidth + 0.52f;

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Station Shadow",
                root,
                new Vector3(0f, DeckY - 0.018f, stationZ + 0.035f),
                new Vector2(width, 1.34f),
                0.105f,
                shadow);

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Station Plaza",
                root,
                new Vector3(0f, DeckY, stationZ + 0.04f),
                new Vector2(width - 0.12f, 1.22f),
                0.100f,
                plaza);

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Station Upper Panel",
                root,
                new Vector3(0f, DeckY + 0.006f, stationZ + 0.33f),
                new Vector2(width - 0.46f, 0.42f),
                0.070f,
                plazaPanel);

            CreateBox(
                "Daily Challenge Station Front Rail",
                root,
                new Vector3(0f, 0.020f, stationZ - 0.58f),
                new Vector3(width - 0.52f, 0.030f, 0.030f),
                rail,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Station Back Rail",
                root,
                new Vector3(0f, 0.020f, stationZ + 0.62f),
                new Vector3(width - 0.62f, 0.028f, 0.030f),
                rail,
                Quaternion.identity);
        }

        private static void CreateStraightChallengeRoad(
            Transform root,
            Material road,
            Material roadLine,
            Material rail)
        {
            var roadZ = BoardLayoutConfig.StationZ + 1.04f;
            var width = BoardLayoutConfig.GridWorldWidth + 0.38f;

            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Straight Road",
                root,
                new Vector3(0f, DeckY + 0.004f, roadZ),
                new Vector2(width, 0.52f),
                0.055f,
                road);

            CreateDashedLine(root, "Daily Challenge Center Dashes", roadZ, width * 0.82f, 7, roadLine);

            CreateBox(
                "Daily Challenge Road Top Rail",
                root,
                new Vector3(0f, 0.015f, roadZ + 0.285f),
                new Vector3(width - 0.30f, 0.016f, 0.020f),
                rail,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Road Bottom Rail",
                root,
                new Vector3(0f, 0.015f, roadZ - 0.285f),
                new Vector3(width - 0.30f, 0.016f, 0.020f),
                rail,
                Quaternion.identity);
        }

        private static void CreateSideProps(
            Transform root,
            Material pole,
            Material lamp,
            Material shadow)
        {
            var z = BoardLayoutConfig.StationZ + 1.10f;
            CreateLamp(root, new Vector3(-2.28f, 0f, z), pole, lamp, shadow);
            CreateLamp(root, new Vector3(2.28f, 0f, z), pole, lamp, shadow);
        }

        private static void CreateCornerAccent(
            Transform root,
            float x,
            float z,
            Material material)
        {
            BoardGeometry.CreateFlatRect(
                "Daily Challenge Vehicle Puzzle Corner Accent A",
                root,
                new Vector3(x, DeckY + 0.026f, z),
                new Vector2(0.030f, 0.32f),
                material,
                Quaternion.Euler(0f, -28f, 0f));

            BoardGeometry.CreateFlatRect(
                "Daily Challenge Vehicle Puzzle Corner Accent B",
                root,
                new Vector3(x + 0.08f, DeckY + 0.026f, z + 0.02f),
                new Vector2(0.030f, 0.28f),
                material,
                Quaternion.Euler(0f, -28f, 0f));
        }

        private static void CreateDashedLine(
            Transform root,
            string name,
            float z,
            float width,
            int dashCount,
            Material material)
        {
            for (var index = 0; index < dashCount; index++)
            {
                var x = Mathf.Lerp(-width * 0.5f, width * 0.5f, index / (dashCount - 1f));
                BoardGeometry.CreateFlatRect(
                    $"{name} {index + 1}",
                    root,
                    new Vector3(x, DeckY + 0.020f, z),
                    new Vector2(width / (dashCount * 2.4f), 0.026f),
                    material);
            }
        }

        private static void CreateLamp(
            Transform root,
            Vector3 basePosition,
            Material pole,
            Material lamp,
            Material shadow)
        {
            BoardGeometry.CreateFlatRoundedRect(
                "Daily Challenge Lamp Shadow",
                root,
                basePosition + new Vector3(0.040f, DeckY + 0.030f, -0.018f),
                new Vector2(0.20f, 0.10f),
                0.045f,
                shadow,
                Quaternion.Euler(0f, 18f, 0f));

            CreateCylinder(
                "Daily Challenge Lamp Pole",
                root,
                basePosition + Vector3.up * 0.135f,
                new Vector3(0.026f, 0.270f, 0.026f),
                pole,
                Quaternion.identity);

            CreateBox(
                "Daily Challenge Lamp Arm",
                root,
                basePosition + new Vector3(0.066f, 0.245f, 0f),
                new Vector3(0.132f, 0.018f, 0.018f),
                pole,
                Quaternion.identity);

            CreateSphere(
                "Daily Challenge Lamp Bulb",
                root,
                basePosition + new Vector3(0.142f, 0.235f, 0f),
                0.036f,
                lamp);
        }

        private static GameObject CreateBeam(
            string name,
            Transform root,
            Vector3 start,
            Vector3 end,
            float thickness,
            Material material)
        {
            var direction = end - start;
            var length = direction.magnitude;
            if (length <= 0.001f)
            {
                return null;
            }

            return CreateBox(
                name,
                root,
                Vector3.Lerp(start, end, 0.5f),
                new Vector3(thickness, thickness, length),
                material,
                Quaternion.LookRotation(direction.normalized, Vector3.up));
        }

        private static GameObject CreateBox(
            string name,
            Transform root,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion rotation)
        {
            var box = VisualPrimitiveFactory.Create(PrimitiveType.Cube, name);
            box.transform.SetParent(root, false);
            box.transform.SetPositionAndRotation(position, rotation);
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            DisablePhysics(box);
            return box;
        }

        private static GameObject CreateCylinder(
            string name,
            Transform root,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion rotation)
        {
            var cylinder = VisualPrimitiveFactory.Create(PrimitiveType.Cylinder, name);
            cylinder.transform.SetParent(root, false);
            cylinder.transform.SetPositionAndRotation(position, rotation);
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            DisablePhysics(cylinder);
            return cylinder;
        }

        private static GameObject CreateSphere(
            string name,
            Transform root,
            Vector3 position,
            float radius,
            Material material)
        {
            var sphere = VisualPrimitiveFactory.Create(PrimitiveType.Sphere, name);
            sphere.transform.SetParent(root, false);
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * radius;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            DisablePhysics(sphere);
            return sphere;
        }

        private static void DisablePhysics(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        private static void DisableModelPhysics(GameObject gameObject)
        {
            var colliders = gameObject.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                Object.Destroy(colliders[index]);
            }
        }
    }
}
