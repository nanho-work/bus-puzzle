using UnityEngine;

namespace BusPuzzle
{
    internal static class CityTerminalThemeBuilder
    {
        private const float QueueGuideY = -0.011f;

        public static void Create(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, BoardThemeId theme = BoardThemeId.Field)
        {
            var style = BoardThemePalette.GetStyle(theme);
            var root = new GameObject("City Terminal Theme").transform;
            root.SetParent(parent, false);
            var materials = CreateMaterials(style);

            CreateBusYardEdgeSkin(CreateSection(root, "1 Bus Yard Edge Skin"), materials);
            CreateStationSkin(CreateSection(root, "2 Station Skin"), style);
            CreateStationApproachLane(CreateSection(root, "3 Station Approach Lane"), settings, materials, style);
            CreateQueueFloorSkin(CreateSection(root, "4 Queue Floor Skin"), layout, settings, materials);
            CreateQueueOuterLineSkin(CreateSection(root, "5 Queue Outer Line Skin"), layout, settings, materials);
            CreateRotaryCenterSkin(CreateSection(root, "6 Rotary Center Skin"), settings, materials, theme, style);
            CreateQueueSurroundings(CreateSection(root, "7 Queue Surroundings"), settings, materials);
            CreateRotarySideMargins(CreateSection(root, "8 Rotary Side Margins"), settings, materials, theme, style);
        }

        private static ThemeMaterials CreateMaterials(BoardThemeStyle style)
        {
            return new ThemeMaterials(
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Pavement", style.Pavement),
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Paver A", style.PaverA),
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Paver B", style.PaverB),
                PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Safety", BoardThemePalette.WithAlpha(style.Gate, 0.36f)),
                PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Line", BoardThemePalette.WithAlpha(style.Rail, 0.52f)),
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Pole", style.Pole),
                PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Lamp Glow", BoardThemePalette.WithAlpha(style.Light, 0.24f)),
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Lamp Bulb", style.Light),
                PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Queue Guide", style.QueueGuide),
                PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Queue Floor", style.QueueFloor));
        }

        private static Transform CreateSection(Transform root, string name)
        {
            var section = new GameObject(name).transform;
            section.SetParent(root, false);
            return section;
        }

        private static void CreateBusYardEdgeSkin(Transform root, ThemeMaterials materials)
        {
            BoardGeometry.CreateFlatRect(
                "City Sidewalk Upper",
                root,
                new Vector3(0f, -0.086f, BoardLayoutConfig.StationZ + 0.42f),
                new Vector2(5.28f, 0.22f),
                materials.Pavement);

            BoardGeometry.CreateFlatRect(
                "City Sidewalk Lower",
                root,
                new Vector3(0f, -0.084f, BoardLayoutConfig.GridBottomZ - 0.34f),
                new Vector2(5.24f, 0.26f),
                materials.Pavement);

            CreateYardMarkings(root, materials.WhiteLine);
        }

        private static void CreateStationSkin(Transform root, BoardThemeStyle style)
        {
            root.gameObject.SetActive(true);
            CreateTerminalStationSkin(root, style);
        }

        private static void CreateStationApproachLane(
            Transform root,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials,
            BoardThemeStyle style)
        {
            var laneDepth = GetStationApproachLaneDepth();
            var laneCenterZ = GetStationApproachLaneCenterZ(settings);
            var laneWidth = BoardLayoutConfig.GridWorldWidth + BoardLayoutConfig.CellSize * 0.10f;
            var roadMaterial = PuzzlePalette.CreateTransparentMaterial(
                $"{style.Name} Station Approach Lane",
                BoardThemePalette.WithAlpha(style.StationBayRoad, 0.46f));
            var roadEdgeMaterial = PuzzlePalette.CreateTransparentMaterial(
                $"{style.Name} Station Approach Lane Edge",
                BoardThemePalette.WithAlpha(style.Rail, 0.42f));
            var roadGuideMaterial = PuzzlePalette.CreateTransparentMaterial(
                $"{style.Name} Station Approach Lane Guide",
                BoardThemePalette.WithAlpha(style.Rail, 0.30f));

            BoardGeometry.CreateFlatRoundedRect(
                "Station Approach Lane Surface",
                root,
                new Vector3(0f, -0.082f, laneCenterZ),
                new Vector2(laneWidth, laneDepth),
                0.075f,
                roadMaterial);

            BoardGeometry.CreateFlatRect(
                "Station Approach Lane Lower Edge",
                root,
                new Vector3(0f, -0.020f, laneCenterZ - laneDepth * 0.5f),
                new Vector2(laneWidth - BoardLayoutConfig.CellSize * 0.26f, 0.018f),
                roadEdgeMaterial);

            BoardGeometry.CreateFlatRect(
                "Station Approach Lane Upper Edge",
                root,
                new Vector3(0f, -0.020f, laneCenterZ + laneDepth * 0.5f),
                new Vector2(laneWidth - BoardLayoutConfig.CellSize * 0.26f, 0.018f),
                roadEdgeMaterial);

            for (var index = 0; index < 5; index++)
            {
                var x = (index - 2) * BoardLayoutConfig.CellSize * 1.48f;
                BoardGeometry.CreateFlatRect(
                    $"Station Approach Lane Center Dash {index + 1}",
                    root,
                    new Vector3(x, -0.018f, laneCenterZ),
                    new Vector2(BoardLayoutConfig.CellSize * 0.55f, 0.014f),
                    roadGuideMaterial);
            }

            CreateCrosswalk(
                root,
                new Vector3(-2.28f, -0.017f, laneCenterZ),
                BoardLayoutConfig.CellSize * 0.56f,
                laneDepth * 0.72f,
                materials.WhiteLine);
            CreateCrosswalk(
                root,
                new Vector3(2.28f, -0.017f, laneCenterZ),
                BoardLayoutConfig.CellSize * 0.56f,
                laneDepth * 0.72f,
                materials.WhiteLine);
        }

        private static void CreateTerminalStationSkin(Transform root, BoardThemeStyle style)
        {
            var rotation = Quaternion.identity;
            var totalSlots = BoardLayoutConfig.TotalStationSlots;
            var slotSpacing = BoardLayoutConfig.StationSlotSpacing;
            var slotWidth = BoardLayoutConfig.StationSlotWidth;
            var slotDepth = BoardLayoutConfig.StationSlotDepth;
            var platformWidth = (totalSlots - 1) * slotSpacing + slotWidth + 0.40f;
            var platformDepth = slotDepth + 0.20f;

            var deckShadowMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Platform Shadow", BoardThemePalette.WithAlpha(style.RoadShadow, 0.24f));
            var deckMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Deck", style.StationPlatform);
            var deckTopMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Deck Top", BoardThemePalette.WithAlpha(style.LockedSlot, 0.42f));
            var bayMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Bay", style.StationBayTerminal);
            var bayTrimMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Bay Trim", BoardThemePalette.WithAlpha(style.Gate, 0.50f));
            var railMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Rail", style.Rail);
            var railPostMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Terminal Rail Post", style.Pole);
            var vipMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal VIP Trim", new Color(0.96f, 0.58f, 0.04f, 0.62f));
            var vipInsetMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal VIP Warm Inset", new Color(1.00f, 0.88f, 0.38f, 0.42f));
            var vipGlowMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal VIP Soft Glow", new Color(1.00f, 0.77f, 0.20f, 0.14f));
            var vipPostMaterial = PuzzlePalette.CreateSolidMaterial("Stage 14 Terminal VIP Post", new Color(0.88f, 0.54f, 0.06f));
            var vipCapMaterial = PuzzlePalette.CreateSolidMaterial("Stage 14 Terminal VIP Post Cap", new Color(1.00f, 0.82f, 0.20f));
            var vipRibbonMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal VIP Ribbon", new Color(0.95f, 0.70f, 0.16f, 0.70f));
            var whiteLineMaterial = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Terminal Safety Paint", BoardThemePalette.WithAlpha(style.Rail, 0.76f));

            BoardGeometry.CreateFlatRoundedRect(
                "Stage 14 Terminal Soft Shadow",
                root,
                StationLocalPoint(0f, -0.082f, BoardLayoutConfig.StationZ - 0.015f),
                new Vector2(platformWidth + 0.06f, platformDepth + 0.05f),
                0.085f,
                deckShadowMaterial,
                rotation);

            CreateBox(
                "Stage 14 Terminal Low Base",
                root,
                StationLocalPoint(0f, -0.074f, BoardLayoutConfig.StationZ),
                new Vector3(platformWidth, 0.018f, platformDepth),
                deckMaterial,
                rotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Stage 14 Terminal Top Plate",
                root,
                StationLocalPoint(0f, -0.053f, BoardLayoutConfig.StationZ),
                new Vector2(platformWidth - 0.12f, platformDepth - 0.10f),
                0.075f,
                deckTopMaterial,
                rotation);

            CreateBox(
                "Stage 14 Terminal Front Curb",
                root,
                StationLocalPoint(0f, -0.022f, BoardLayoutConfig.StationZ - slotDepth * 0.55f),
                new Vector3(platformWidth - 0.16f, 0.012f, 0.024f),
                railMaterial,
                rotation);

            CreateBox(
                "Stage 14 Terminal Back Rail",
                root,
                StationLocalPoint(0f, -0.018f, BoardLayoutConfig.StationZ + slotDepth * 0.55f),
                new Vector3(platformWidth - 0.26f, 0.014f, 0.024f),
                railMaterial,
                rotation);

            for (var index = 0; index < totalSlots; index++)
            {
                var x = (index - (totalSlots - 1) * 0.5f) * slotSpacing;
                CreateTerminalBay(root, index, x, slotWidth, slotDepth, rotation, bayMaterial, bayTrimMaterial);
                CreateBox(
                    $"Stage 14 Terminal Rail Post {index + 1}",
                    root,
                    StationLocalPoint(x, -0.006f, BoardLayoutConfig.StationZ + slotDepth * 0.55f),
                    new Vector3(0.020f, 0.028f, 0.020f),
                    railPostMaterial,
                    rotation);
            }

            CreateVipTerminalBay(root, BoardLayoutConfig.GetFreeStationPosition(), slotWidth, slotDepth, rotation, vipMaterial, vipInsetMaterial, whiteLineMaterial, vipPostMaterial, vipCapMaterial, vipRibbonMaterial, vipGlowMaterial);
        }

        private static void CreateTerminalBay(
            Transform root,
            int index,
            float x,
            float slotWidth,
            float slotDepth,
            Quaternion rotation,
            Material bayMaterial,
            Material bayTrimMaterial)
        {
            var position = StationLocalPoint(x, -0.061f, BoardLayoutConfig.StationZ);
            BoardGeometry.CreateFlatRoundedRect(
                $"Stage 14 Terminal Bay Floor {index + 1}",
                root,
                position,
                new Vector2(slotWidth + 0.070f, slotDepth + 0.035f),
                slotWidth * 0.16f,
                bayTrimMaterial,
                rotation);
            BoardGeometry.CreateFlatRoundedRect(
                $"Stage 14 Terminal Bay Recess {index + 1}",
                root,
                StationLocalPoint(x, -0.055f, BoardLayoutConfig.StationZ),
                new Vector2(slotWidth - 0.035f, slotDepth - 0.060f),
                slotWidth * 0.12f,
                bayMaterial,
                rotation);
            BoardGeometry.CreateFlatRect(
                $"Stage 14 Terminal Stop Line {index + 1}",
                root,
                StationLocalPoint(x, -0.049f, BoardLayoutConfig.StationZ - slotDepth * 0.35f),
                new Vector2(slotWidth * 0.56f, 0.018f),
                bayTrimMaterial,
                rotation);
        }

        private static void CreateVipTerminalBay(
            Transform root,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            Quaternion rotation,
            Material vipMaterial,
            Material vipInsetMaterial,
            Material whiteLineMaterial,
            Material vipPostMaterial,
            Material vipCapMaterial,
            Material vipRibbonMaterial,
            Material vipGlowMaterial)
        {
            BoardGeometry.CreateFlatRoundedRect(
                "Stage 14 Terminal VIP Soft Glow",
                root,
                position + Vector3.down * 0.055f,
                new Vector2(slotWidth + 0.070f, slotDepth + 0.045f),
                slotWidth * 0.18f,
                vipGlowMaterial,
                rotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Stage 14 Terminal VIP Trim",
                root,
                new Vector3(position.x, -0.048f, position.z),
                new Vector2(slotWidth + 0.034f, slotDepth + 0.022f),
                slotWidth * 0.14f,
                vipMaterial,
                rotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Stage 14 Terminal VIP Inset",
                root,
                position + Vector3.down * 0.034f,
                new Vector2(slotWidth - 0.082f, slotDepth - 0.122f),
                slotWidth * 0.11f,
                vipInsetMaterial,
                rotation);

            BoardGeometry.CreateFlatRect(
                "Stage 14 Terminal VIP Front Stripe",
                root,
                position + Vector3.up * -0.017f - rotation * Vector3.forward * (slotDepth * 0.33f),
                new Vector2(slotWidth * 0.42f, 0.014f),
                whiteLineMaterial,
                rotation);

            BoardGeometry.CreateFlatRect(
                "Stage 14 Terminal VIP Ticket Mark",
                root,
                position + Vector3.down * 0.015f + rotation * new Vector3(-slotWidth * 0.20f, 0f, slotDepth * 0.14f),
                new Vector2(slotWidth * 0.24f, 0.012f),
                whiteLineMaterial,
                rotation);

            CreateVipGateMarker(root, position, slotWidth, slotDepth, rotation, vipPostMaterial, vipCapMaterial, vipRibbonMaterial);
        }

        private static void CreateVipGateMarker(
            Transform root,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            Quaternion rotation,
            Material postMaterial,
            Material capMaterial,
            Material ribbonMaterial)
        {
            var postZ = slotDepth * 0.47f;
            var leftPost = position + rotation * new Vector3(-slotWidth * 0.36f, 0f, postZ);
            var rightPost = position + rotation * new Vector3(slotWidth * 0.36f, 0f, postZ);
            var ribbonCenter = position + rotation * new Vector3(0f, 0f, postZ);

            CreateCylinder(
                "Stage 14 Terminal VIP Left Stanchion",
                root,
                leftPost + Vector3.up * 0.044f,
                new Vector3(0.022f, 0.088f, 0.022f),
                postMaterial,
                rotation);

            CreateCylinder(
                "Stage 14 Terminal VIP Right Stanchion",
                root,
                rightPost + Vector3.up * 0.044f,
                new Vector3(0.022f, 0.088f, 0.022f),
                postMaterial,
                rotation);

            CreateSphere("Stage 14 Terminal VIP Left Stanchion Cap", root, leftPost + Vector3.up * 0.134f, 0.032f, capMaterial);
            CreateSphere("Stage 14 Terminal VIP Right Stanchion Cap", root, rightPost + Vector3.up * 0.134f, 0.032f, capMaterial);

            CreateBox(
                "Stage 14 Terminal VIP Ribbon",
                root,
                ribbonCenter + Vector3.up * 0.114f,
                new Vector3(slotWidth * 0.62f, 0.016f, 0.018f),
                ribbonMaterial,
                rotation);
        }

        private static Vector3 StationLocalPoint(float localX, float y, float worldZ)
        {
            return new Vector3(localX, y, worldZ);
        }

        private static float GetStationApproachLaneDepth()
        {
            return Mathf.Max(
                BoardLayoutConfig.CellSize * 1.24f,
                BoardLayoutConfig.VehicleVisualWidthCells * BoardLayoutConfig.CellSize * 1.30f);
        }

        private static float GetStationApproachLaneCenterZ(RotaryRoadBuildSettings settings)
        {
            var stationUpperLineZ = settings.StationZ + BoardLayoutConfig.StationSlotDepth * 0.5f;
            return stationUpperLineZ + GetStationApproachLaneDepth() * 0.5f + BoardLayoutConfig.CellSize * 0.22f;
        }

        private static void CreateYardMarkings(Transform root, Material whiteLine)
        {
            var gridCenter = new Vector3(0f, 0f, BoardLayoutConfig.ParkingYardCenterZ);
            var yardTopZ = BoardLayoutConfig.ParkingYardTopZ + BoardLayoutConfig.CellSize * 0.52f;
            var yardBottomZ = BoardLayoutConfig.GridBottomZ - BoardLayoutConfig.CellSize * 0.52f;

            BoardGeometry.CreateFlatRect(
                "Parking Yard Top Curb",
                root,
                new Vector3(0f, -0.021f, yardTopZ),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.38f, 0.024f),
                whiteLine);

            BoardGeometry.CreateFlatRect(
                "Parking Yard Bottom Curb",
                root,
                new Vector3(0f, -0.021f, yardBottomZ),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.38f, 0.024f),
                whiteLine);

            BoardGeometry.CreateFlatRect(
                "Parking Yard Left Curb",
                root,
                new Vector3(BoardLayoutConfig.GridLeftX - BoardLayoutConfig.CellSize * 0.55f, -0.021f, gridCenter.z),
                new Vector2(0.024f, BoardLayoutConfig.ParkingYardWorldDepth + 0.38f),
                whiteLine);

            BoardGeometry.CreateFlatRect(
                "Parking Yard Right Curb",
                root,
                new Vector3(BoardLayoutConfig.GridRightX + BoardLayoutConfig.CellSize * 0.55f, -0.021f, gridCenter.z),
                new Vector2(0.024f, BoardLayoutConfig.ParkingYardWorldDepth + 0.38f),
                whiteLine);

            CreateCrosswalk(root, new Vector3(-1.88f, -0.018f, BoardLayoutConfig.ParkingYardTopZ + 0.22f), 0.46f, 0.22f, whiteLine);
            CreateCrosswalk(root, new Vector3(1.92f, -0.018f, BoardLayoutConfig.ParkingYardTopZ + 0.22f), 0.46f, 0.22f, whiteLine);
        }

        private static void CreateQueueFloorSkin(
            Transform root,
            RotaryLayout layout,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials)
        {
            CreateFeederPathBand(root, "Left Queue Floor", layout, -1, settings, 0.115f, 0.270f, -0.072f, materials.QueueFloor);
            CreateFeederPathBand(root, "Right Queue Floor", layout, 1, settings, 0.115f, 0.270f, -0.072f, materials.QueueFloor);
        }

        private static void CreateQueueOuterLineSkin(
            Transform root,
            RotaryLayout layout,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials)
        {
            CreateFeederPathBand(root, "Left Queue Outer Guide", layout, -1, settings, 0.305f, 0.335f, QueueGuideY, materials.QueueGuide);
            CreateFeederPathBand(root, "Right Queue Outer Guide", layout, 1, settings, 0.305f, 0.335f, QueueGuideY, materials.QueueGuide);
            CreateQueuePosts(root, layout, -1, settings, materials);
            CreateQueuePosts(root, layout, 1, settings, materials);
        }

        private static void CreateRotaryCenterSkin(
            Transform root,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials,
            BoardThemeId theme,
            BoardThemeStyle style)
        {
            var center = new Vector3(0f, 0f, settings.RotaryCenterZ);
            var baseMaterial = PuzzlePalette.CreateLitMaterial($"Rotary {style.Name} Base Prop", style.Pavement, 0.22f);
            var accentA = PuzzlePalette.CreateLitMaterial($"Rotary {style.Name} Accent A", style.AccentA, 0.24f);
            var accentB = PuzzlePalette.CreateLitMaterial($"Rotary {style.Name} Accent B", style.AccentB, 0.24f);
            var accentC = PuzzlePalette.CreateLitMaterial($"Rotary {style.Name} Accent C", style.AccentC, 0.24f);
            var shadowMaterial = PuzzlePalette.CreateTransparentMaterial($"Rotary {style.Name} Shadow", style.PropShadow);

            switch (theme)
            {
                case BoardThemeId.Harbor:
                    CreateCargoPallet(root, center + new Vector3(-0.08f, 0f, 0.04f), Quaternion.Euler(0f, -8f, 0f), baseMaterial, accentC, shadowMaterial);
                    CreateContainerStack(root, center + new Vector3(0.36f, 0f, -0.08f), Quaternion.Euler(0f, 18f, 0f), accentB, accentA, accentC, shadowMaterial, 0.70f);
                    CreateBollardPair(root, center + new Vector3(-0.42f, 0f, -0.15f), Quaternion.identity, materials.Pole, materials.Safety, shadowMaterial, 0.78f);
                    break;
                case BoardThemeId.Future:
                case BoardThemeId.Space:
                    CreateHologramCore(root, center + new Vector3(0.04f, 0f, 0.02f), accentA, materials.LampGlow, baseMaterial, accentB, shadowMaterial);
                    CreateDataNodeCluster(root, center + new Vector3(-0.34f, 0f, -0.14f), accentC, shadowMaterial, 0.72f);
                    break;
                case BoardThemeId.Waikiki:
                    CreateTidePool(root, center + new Vector3(0.02f, 0f, 0.02f), accentA, materials.LampGlow, baseMaterial);
                    CreateShellCluster(root, center + new Vector3(-0.36f, 0f, -0.12f), accentC, shadowMaterial, 0.66f);
                    break;
                default:
                    CreateCargoPallet(root, center + new Vector3(-0.08f, 0f, 0.04f), Quaternion.Euler(0f, -8f, 0f), baseMaterial, accentC, shadowMaterial, 0.78f);
                    CreateBollardPair(root, center + new Vector3(0.34f, 0f, -0.12f), Quaternion.identity, materials.Pole, materials.Safety, shadowMaterial, 0.72f);
                    break;
            }

            CreateFlowerDot(root, center + new Vector3(0.05f, -0.010f, 0.18f), materials.Safety);
            CreateFlowerDot(root, center + new Vector3(-0.17f, -0.010f, 0.07f), materials.WhiteLine);
            CreateFlowerDot(root, center + new Vector3(0.19f, -0.010f, -0.18f), materials.Safety);
        }

        private static void CreateQueueSurroundings(
            Transform root,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials)
        {
            var z = settings.RotaryCenterZ - 0.04f;
            CreateLamp(root, new Vector3(-2.42f, 0f, z), 0.36f, materials.Pole, materials.LampGlow, materials.LampBulb);
            CreateLamp(root, new Vector3(2.42f, 0f, z), 0.36f, materials.Pole, materials.LampGlow, materials.LampBulb);
        }

        private static void CreateRotarySideMargins(
            Transform root,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials,
            BoardThemeId theme,
            BoardThemeStyle style)
        {
            var laneCenterZ = GetStationApproachLaneCenterZ(settings);
            var propZ = laneCenterZ + GetStationApproachLaneDepth() * 0.5f + BoardLayoutConfig.CellSize * 0.52f;
            var propX = (BoardLayoutConfig.GridWorldWidth + BoardLayoutConfig.CellSize * 0.10f) * 0.5f + BoardLayoutConfig.CellSize * 0.18f;
            var leftPropPosition = new Vector3(-propX, 0f, propZ);
            var rightPropPosition = new Vector3(propX, 0f, propZ);
            BoardGeometry.CreateFlatRect(
                "Left Rotary Plaza Pavers",
                root,
                new Vector3(leftPropPosition.x, -0.083f, leftPropPosition.z),
                new Vector2(0.38f, 0.56f),
                materials.PaverA);
            BoardGeometry.CreateFlatRect(
                "Right Rotary Plaza Pavers",
                root,
                new Vector3(rightPropPosition.x, -0.083f, rightPropPosition.z),
                new Vector2(0.38f, 0.56f),
                materials.PaverB);

            var shadow = PuzzlePalette.CreateTransparentMaterial($"Side {style.Name} Prop Shadow", style.PropShadow);
            var accentA = PuzzlePalette.CreateLitMaterial($"Side {style.Name} Accent A", style.AccentA, 0.28f);
            var accentB = PuzzlePalette.CreateLitMaterial($"Side {style.Name} Accent B", style.AccentB, 0.28f);
            var accentC = PuzzlePalette.CreateLitMaterial($"Side {style.Name} Accent C", style.AccentC, 0.28f);
            var baseMaterial = PuzzlePalette.CreateLitMaterial($"Side {style.Name} Base", style.Pavement, 0.28f);

            switch (theme)
            {
                case BoardThemeId.Harbor:
                    CreateContainerStack(root, leftPropPosition, Quaternion.Euler(0f, 90f, 0f), accentB, accentA, accentC, shadow, 1.22f);
                    CreateHarborCrane(root, rightPropPosition, Quaternion.Euler(0f, 28f, 0f), accentC, materials.Pole, materials.Safety, shadow, 1.18f);
                    break;
                case BoardThemeId.Future:
                    CreateCityBlockCluster(root, leftPropPosition, Quaternion.Euler(0f, 90f, 0f), baseMaterial, accentA, accentB, shadow, 1.18f);
                    CreateEnergyPylon(root, rightPropPosition, Quaternion.Euler(0f, 28f, 0f), baseMaterial, accentA, accentC, shadow, 1.18f);
                    break;
                case BoardThemeId.Space:
                    CreateMoonRadar(root, leftPropPosition, Quaternion.Euler(0f, 78f, 0f), baseMaterial, accentA, accentB, shadow);
                    CreateEnergyPylon(root, rightPropPosition, Quaternion.Euler(0f, 28f, 0f), baseMaterial, accentA, accentC, shadow, 1.24f);
                    break;
                case BoardThemeId.Waikiki:
                    CreatePalmTree(root, leftPropPosition, Quaternion.Euler(0f, 90f, 0f), baseMaterial, accentA, shadow, 1.24f);
                    CreateBeachUmbrella(root, rightPropPosition, Quaternion.Euler(0f, 28f, 0f), accentB, accentC, materials.Pole, shadow, 1.24f);
                    break;
                case BoardThemeId.Ice:
                    CreateSnowyPine(root, leftPropPosition, Quaternion.Euler(0f, 12f, 0f), baseMaterial, accentA, accentC, shadow);
                    CreateSnowman(root, rightPropPosition, Quaternion.Euler(0f, -18f, 0f), accentA, accentB, accentC, shadow);
                    break;
                case BoardThemeId.Desert:
                    CreatePyramid(root, leftPropPosition, Quaternion.Euler(0f, 16f, 0f), baseMaterial, accentB, shadow);
                    CreateDesertStatue(root, rightPropPosition, Quaternion.Euler(0f, -20f, 0f), accentB, accentC, shadow);
                    break;
                default:
                    CreateSoccerGoal(root, leftPropPosition, Quaternion.Euler(0f, 90f, 0f), materials.Pole, materials.WhiteLine, shadow);
                    CreateTeamBench(root, rightPropPosition, Quaternion.Euler(0f, -18f, 0f), baseMaterial, accentA, materials.Pole, shadow);
                    break;
            }
        }

        private static void CreateSoccerGoal(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material frameMaterial,
            Material netMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Field Soccer Goal Shadow", center, new Vector2(0.86f, 0.44f), shadowMaterial, rotation);
            CreateBox("Field Soccer Goal Back Bar", root, center + rotation * new Vector3(0f, 0.270f, -0.120f), new Vector3(0.680f, 0.035f, 0.040f), frameMaterial, rotation);
            CreateBox("Field Soccer Goal Left Post", root, center + rotation * new Vector3(-0.340f, 0.150f, -0.120f), new Vector3(0.040f, 0.300f, 0.040f), frameMaterial, rotation);
            CreateBox("Field Soccer Goal Right Post", root, center + rotation * new Vector3(0.340f, 0.150f, -0.120f), new Vector3(0.040f, 0.300f, 0.040f), frameMaterial, rotation);
            CreateBox("Field Soccer Goal Net A", root, center + rotation * new Vector3(-0.170f, 0.150f, -0.142f), new Vector3(0.020f, 0.220f, 0.020f), netMaterial, rotation);
            CreateBox("Field Soccer Goal Net B", root, center + rotation * new Vector3(0.170f, 0.150f, -0.142f), new Vector3(0.020f, 0.220f, 0.020f), netMaterial, rotation);
            CreateBox("Field Soccer Goal Ground Line", root, center + rotation * new Vector3(0f, 0.030f, 0.110f), new Vector3(0.760f, 0.024f, 0.030f), netMaterial, rotation);
        }

        private static void CreateTeamBench(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material seatMaterial,
            Material canopyMaterial,
            Material legMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Field Team Bench Shadow", center, new Vector2(0.82f, 0.38f), shadowMaterial, rotation);
            CreateBox("Field Team Bench Seat", root, center + Vector3.up * 0.110f, new Vector3(0.680f, 0.070f, 0.180f), seatMaterial, rotation);
            CreateBox("Field Team Bench Back", root, center + rotation * new Vector3(0f, 0.225f, 0.095f), new Vector3(0.720f, 0.180f, 0.045f), canopyMaterial, rotation);
            CreateBox("Field Team Bench Canopy", root, center + rotation * new Vector3(0f, 0.370f, 0.005f), new Vector3(0.820f, 0.050f, 0.300f), canopyMaterial, rotation);
            CreateBox("Field Team Bench Left Leg", root, center + rotation * new Vector3(-0.260f, 0.055f, -0.040f), new Vector3(0.045f, 0.110f, 0.050f), legMaterial, rotation);
            CreateBox("Field Team Bench Right Leg", root, center + rotation * new Vector3(0.260f, 0.055f, -0.040f), new Vector3(0.045f, 0.110f, 0.050f), legMaterial, rotation);
        }

        private static void CreateSnowyPine(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material trunkMaterial,
            Material pineMaterial,
            Material snowMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Ice Snow Pine Shadow", center, new Vector2(0.56f, 0.46f), shadowMaterial, rotation);
            CreateCylinder("Ice Snow Pine Trunk", root, center + Vector3.up * 0.130f, new Vector3(0.065f, 0.260f, 0.065f), trunkMaterial, rotation);
            CreateCylinder("Ice Snow Pine Lower Branches", root, center + Vector3.up * 0.255f, new Vector3(0.310f, 0.170f, 0.310f), pineMaterial, rotation);
            CreateCylinder("Ice Snow Pine Upper Branches", root, center + Vector3.up * 0.405f, new Vector3(0.225f, 0.150f, 0.225f), pineMaterial, rotation);
            CreateSphere("Ice Snow Pine Cap", root, center + Vector3.up * 0.520f, 0.150f, snowMaterial);
            CreateSphere("Ice Snow Pine Snow Patch", root, center + rotation * new Vector3(-0.110f, 0.345f, 0.040f), 0.092f, snowMaterial);
        }

        private static void CreateSnowman(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material snowMaterial,
            Material scarfMaterial,
            Material accentMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Ice Snowman Shadow", center, new Vector2(0.56f, 0.42f), shadowMaterial, rotation);
            CreateSphere("Ice Snowman Body", root, center + Vector3.up * 0.145f, 0.230f, snowMaterial);
            CreateSphere("Ice Snowman Head", root, center + Vector3.up * 0.410f, 0.155f, snowMaterial);
            CreateBox("Ice Snowman Scarf", root, center + Vector3.up * 0.315f, new Vector3(0.270f, 0.040f, 0.050f), scarfMaterial, rotation);
            CreateBox("Ice Snowman Nose", root, center + rotation * new Vector3(0f, 0.420f, -0.145f), new Vector3(0.040f, 0.030f, 0.095f), accentMaterial, rotation);
            CreateSphere("Ice Snowman Button A", root, center + rotation * new Vector3(0f, 0.205f, -0.205f), 0.030f, accentMaterial);
            CreateSphere("Ice Snowman Button B", root, center + rotation * new Vector3(0f, 0.120f, -0.220f), 0.026f, accentMaterial);
        }

        private static void CreatePyramid(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material stoneMaterial,
            Material capMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Desert Pyramid Shadow", center, new Vector2(0.82f, 0.58f), shadowMaterial, rotation);
            CreateBox("Desert Pyramid Base", root, center + Vector3.up * 0.070f, new Vector3(0.720f, 0.140f, 0.520f), stoneMaterial, rotation);
            CreateBox("Desert Pyramid Middle", root, center + Vector3.up * 0.195f, new Vector3(0.520f, 0.130f, 0.380f), stoneMaterial, rotation);
            CreateBox("Desert Pyramid Top", root, center + Vector3.up * 0.305f, new Vector3(0.300f, 0.110f, 0.220f), capMaterial, rotation);
            CreateBox("Desert Pyramid Cap", root, center + Vector3.up * 0.405f, new Vector3(0.120f, 0.090f, 0.090f), capMaterial, rotation);
        }

        private static void CreateDesertStatue(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material stoneMaterial,
            Material accentMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Desert Statue Shadow", center, new Vector2(0.72f, 0.40f), shadowMaterial, rotation);
            CreateBox("Desert Statue Body", root, center + Vector3.up * 0.140f, new Vector3(0.520f, 0.160f, 0.240f), stoneMaterial, rotation);
            CreateBox("Desert Statue Chest", root, center + rotation * new Vector3(-0.070f, 0.275f, -0.020f), new Vector3(0.280f, 0.160f, 0.210f), stoneMaterial, rotation);
            CreateBox("Desert Statue Head", root, center + rotation * new Vector3(-0.230f, 0.365f, -0.020f), new Vector3(0.160f, 0.150f, 0.160f), stoneMaterial, rotation);
            CreateBox("Desert Statue Face Mark", root, center + rotation * new Vector3(-0.318f, 0.375f, -0.020f), new Vector3(0.018f, 0.070f, 0.110f), accentMaterial, rotation);
            CreateBox("Desert Statue Front Paw A", root, center + rotation * new Vector3(-0.290f, 0.070f, -0.120f), new Vector3(0.240f, 0.070f, 0.070f), stoneMaterial, rotation);
            CreateBox("Desert Statue Front Paw B", root, center + rotation * new Vector3(-0.290f, 0.070f, 0.120f), new Vector3(0.240f, 0.070f, 0.070f), stoneMaterial, rotation);
        }

        private static void CreateMoonRadar(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material baseMaterial,
            Material dishMaterial,
            Material glowMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Space Radar Shadow", center, new Vector2(0.68f, 0.44f), shadowMaterial, rotation);
            CreateCylinder("Space Radar Base", root, center + Vector3.up * 0.065f, new Vector3(0.210f, 0.085f, 0.210f), baseMaterial, rotation);
            CreateBox("Space Radar Mast", root, center + Vector3.up * 0.240f, new Vector3(0.052f, 0.340f, 0.052f), baseMaterial, rotation);
            BoardGeometry.CreateFlatRoundedRect(
                "Space Radar Dish",
                root,
                center + rotation * new Vector3(0f, 0.445f, -0.040f),
                new Vector2(0.520f, 0.370f),
                0.170f,
                dishMaterial,
                rotation * Quaternion.Euler(22f, 0f, 0f));
            CreateSphere("Space Radar Glow", root, center + rotation * new Vector3(0f, 0.460f, -0.090f), 0.095f, glowMaterial);
        }

        private static void CreateThemeProps(
            Transform root,
            ThemeMaterials materials,
            BoardThemeId theme,
            BoardThemeStyle style)
        {
            switch (theme)
            {
                case BoardThemeId.Harbor:
                    CreateHarborProps(root, materials);
                    break;
                case BoardThemeId.Future:
                    CreateFutureCityProps(root, materials);
                    break;
                case BoardThemeId.Waikiki:
                    CreateWaikikiProps(root, materials);
                    break;
                case BoardThemeId.Space:
                    CreateSpaceProps(root, materials, style);
                    break;
                default:
                    CreateSimpleThemeProps(root, materials, style);
                    break;
            }
        }

        private static void CreateSimpleThemeProps(Transform root, ThemeMaterials materials, BoardThemeStyle style)
        {
            var leftEdgeX = BoardLayoutConfig.GridLeftX - BoardLayoutConfig.CellSize * 1.10f;
            var rightEdgeX = BoardLayoutConfig.GridRightX + BoardLayoutConfig.CellSize * 1.10f;
            var lowerSideZ = BoardLayoutConfig.GridBottomZ + BoardLayoutConfig.CellSize * 1.65f;
            var centerSideZ = BoardLayoutConfig.ParkingYardCenterZ - BoardLayoutConfig.CellSize * 0.18f;
            var upperSideZ = BoardLayoutConfig.ParkingYardTopZ - BoardLayoutConfig.CellSize * 1.16f;
            var baseMaterial = PuzzlePalette.CreateLitMaterial($"{style.Name} Prop Base", style.Pavement, 0.26f);
            var accentA = PuzzlePalette.CreateLitMaterial($"{style.Name} Prop Accent A", style.AccentA, 0.26f);
            var accentB = PuzzlePalette.CreateLitMaterial($"{style.Name} Prop Accent B", style.AccentB, 0.26f);
            var accentC = PuzzlePalette.CreateLitMaterial($"{style.Name} Prop Accent C", style.AccentC, 0.26f);
            var shadow = PuzzlePalette.CreateTransparentMaterial($"{style.Name} Prop Contact Shadow", style.PropShadow);

            CreateCargoPallet(root, new Vector3(leftEdgeX, 0f, upperSideZ - 0.16f), Quaternion.Euler(0f, 90f, 0f), baseMaterial, accentC, shadow, 0.78f);
            CreateBollardPair(root, new Vector3(rightEdgeX, 0f, upperSideZ), Quaternion.Euler(0f, -90f, 0f), materials.Pole, materials.Safety, shadow, 0.82f);
            CreateDataNodeCluster(root, new Vector3(leftEdgeX, 0f, lowerSideZ), accentA, shadow, 0.86f);
            CreateCargoPallet(root, new Vector3(rightEdgeX - 0.03f, 0f, centerSideZ + 0.58f), Quaternion.Euler(0f, -90f, 0f), baseMaterial, accentB, shadow, 0.82f);
            CreateBollardPair(root, new Vector3(leftEdgeX - 0.04f, 0f, centerSideZ), Quaternion.Euler(0f, 20f, 0f), materials.Pole, materials.Safety, shadow, 0.82f);
            CreateDataNodeCluster(root, new Vector3(rightEdgeX + 0.04f, 0f, lowerSideZ + 0.55f), accentC, shadow, 0.78f);
        }

        private static void CreateWaikikiProps(Transform root, ThemeMaterials materials)
        {
            var leftEdgeX = BoardLayoutConfig.GridLeftX - BoardLayoutConfig.CellSize * 1.10f;
            var rightEdgeX = BoardLayoutConfig.GridRightX + BoardLayoutConfig.CellSize * 1.10f;
            var lowerSideZ = BoardLayoutConfig.GridBottomZ + BoardLayoutConfig.CellSize * 1.65f;
            var centerSideZ = BoardLayoutConfig.ParkingYardCenterZ - BoardLayoutConfig.CellSize * 0.18f;
            var upperSideZ = BoardLayoutConfig.ParkingYardTopZ - BoardLayoutConfig.CellSize * 1.16f;

            var trunk = PuzzlePalette.CreateLitMaterial("Waikiki Prop Trunk", BoardThemePalette.WaikikiTrunk, 0.26f);
            var palm = PuzzlePalette.CreateLitMaterial("Waikiki Prop Palm", BoardThemePalette.WaikikiPalm, 0.26f);
            var palmLight = PuzzlePalette.CreateLitMaterial("Waikiki Prop Palm Light", BoardThemePalette.WaikikiPalmLight, 0.26f);
            var boardBlue = PuzzlePalette.CreateLitMaterial("Waikiki Prop Board Blue", BoardThemePalette.WaikikiUmbrellaBlue, 0.26f);
            var coral = PuzzlePalette.CreateLitMaterial("Waikiki Prop Coral", BoardThemePalette.WaikikiCoral, 0.26f);
            var sand = PuzzlePalette.CreateLitMaterial("Waikiki Prop Sand", BoardThemePalette.WaikikiSandLight, 0.24f);
            var shadow = PuzzlePalette.CreateTransparentMaterial("Waikiki Prop Contact Shadow", BoardThemePalette.WithAlpha(BoardThemePalette.WaikikiRoadShadow, 0.20f));

            CreatePalmTree(root, new Vector3(leftEdgeX, 0f, upperSideZ - 0.16f), Quaternion.Euler(0f, 90f, 0f), trunk, palm, shadow);
            CreateSurfboardRack(root, new Vector3(rightEdgeX, 0f, upperSideZ), Quaternion.Euler(0f, -90f, 0f), boardBlue, coral, materials.Safety, trunk, shadow);
            CreateBeachUmbrella(root, new Vector3(leftEdgeX, 0f, lowerSideZ), Quaternion.Euler(0f, 90f, 0f), coral, sand, trunk, shadow, 0.86f);
            CreateBeachBench(root, new Vector3(rightEdgeX - 0.03f, 0f, centerSideZ + 0.58f), Quaternion.Euler(0f, -90f, 0f), trunk, sand, shadow);
            CreateLifebuoyStack(root, new Vector3(leftEdgeX - 0.04f, 0f, centerSideZ), Quaternion.Euler(0f, 20f, 0f), coral, sand, shadow, 0.88f);
            CreateShellCluster(root, new Vector3(rightEdgeX + 0.04f, 0f, lowerSideZ + 0.55f), palmLight, shadow, 0.84f);
        }

        private static void CreateSpaceProps(Transform root, ThemeMaterials materials, BoardThemeStyle style)
        {
            var leftEdgeX = BoardLayoutConfig.GridLeftX - BoardLayoutConfig.CellSize * 1.10f;
            var rightEdgeX = BoardLayoutConfig.GridRightX + BoardLayoutConfig.CellSize * 1.10f;
            var lowerSideZ = BoardLayoutConfig.GridBottomZ + BoardLayoutConfig.CellSize * 1.65f;
            var centerSideZ = BoardLayoutConfig.ParkingYardCenterZ - BoardLayoutConfig.CellSize * 0.18f;
            var upperSideZ = BoardLayoutConfig.ParkingYardTopZ - BoardLayoutConfig.CellSize * 1.16f;

            var steel = PuzzlePalette.CreateLitMaterial("Space Dock Prop Steel", style.Pole, 0.34f);
            var darkPanel = PuzzlePalette.CreateLitMaterial("Space Dock Prop Panel", style.PaverB, 0.34f);
            var glowBlue = PuzzlePalette.CreateLitMaterial("Space Dock Prop Glow Blue", style.AccentA, 0.32f);
            var glowPurple = PuzzlePalette.CreateLitMaterial("Space Dock Prop Glow Purple", style.AccentB, 0.32f);
            var dust = PuzzlePalette.CreateLitMaterial("Space Dock Prop Dust", style.AccentC, 0.30f);
            var glass = PuzzlePalette.CreateTransparentMaterial("Space Dock Prop Glass", BoardThemePalette.WithAlpha(style.Rail, 0.42f));
            var glow = PuzzlePalette.CreateTransparentMaterial("Space Dock Prop Glow", BoardThemePalette.WithAlpha(style.AccentA, 0.30f));
            var shadow = PuzzlePalette.CreateTransparentMaterial("Space Dock Prop Contact Shadow", style.PropShadow);

            CreateDronePad(root, new Vector3(leftEdgeX, 0f, upperSideZ - 0.16f), Quaternion.Euler(0f, 90f, 0f), steel, darkPanel, glowBlue, shadow);
            CreateHoloBillboard(root, new Vector3(rightEdgeX, 0f, upperSideZ), Quaternion.Euler(0f, -90f, 0f), steel, glass, glowPurple, glow, shadow);
            CreateEnergyPylon(root, new Vector3(leftEdgeX, 0f, lowerSideZ), Quaternion.Euler(0f, 90f, 0f), steel, glass, glowBlue, shadow, 0.86f);
            CreateDataBench(root, new Vector3(rightEdgeX - 0.03f, 0f, centerSideZ + 0.58f), Quaternion.Euler(0f, -90f, 0f), darkPanel, glowBlue, steel, shadow);
            CreateDataNodeCluster(root, new Vector3(leftEdgeX - 0.04f, 0f, centerSideZ), glowPurple, shadow, 0.88f);
            CreateCityBlockCluster(root, new Vector3(rightEdgeX + 0.04f, 0f, lowerSideZ + 0.55f), Quaternion.Euler(0f, -18f, 0f), darkPanel, glass, dust, shadow, 0.84f);
        }

        private static void CreateHarborProps(Transform root, ThemeMaterials materials)
        {
            var leftEdgeX = BoardLayoutConfig.GridLeftX - BoardLayoutConfig.CellSize * 1.10f;
            var rightEdgeX = BoardLayoutConfig.GridRightX + BoardLayoutConfig.CellSize * 1.10f;
            var lowerSideZ = BoardLayoutConfig.GridBottomZ + BoardLayoutConfig.CellSize * 1.65f;
            var centerSideZ = BoardLayoutConfig.ParkingYardCenterZ - BoardLayoutConfig.CellSize * 0.18f;
            var upperSideZ = BoardLayoutConfig.ParkingYardTopZ - BoardLayoutConfig.CellSize * 1.16f;

            var redContainer = PuzzlePalette.CreateLitMaterial("Harbor Prop Red Container", BoardThemePalette.HarborContainerRed, 0.24f);
            var blueContainer = PuzzlePalette.CreateLitMaterial("Harbor Prop Blue Container", BoardThemePalette.HarborContainerBlue, 0.24f);
            var orangeContainer = PuzzlePalette.CreateLitMaterial("Harbor Prop Orange Container", BoardThemePalette.HarborContainerOrange, 0.24f);
            var greenContainer = PuzzlePalette.CreateLitMaterial("Harbor Prop Green Container", BoardThemePalette.HarborContainerGreen, 0.24f);
            var craneMaterial = PuzzlePalette.CreateLitMaterial("Harbor Prop Crane", BoardThemePalette.HarborCrane, 0.30f);
            var steel = PuzzlePalette.CreateLitMaterial("Harbor Prop Steel", BoardThemePalette.HarborSteel, 0.30f);
            var palletMaterial = PuzzlePalette.CreateLitMaterial("Harbor Prop Pallet", BoardThemePalette.HarborConcreteLight, 0.22f);
            var shadow = PuzzlePalette.CreateTransparentMaterial("Harbor Prop Contact Shadow", BoardThemePalette.WithAlpha(BoardThemePalette.HarborAsphaltDark, 0.22f));

            CreateHarborCrane(root, new Vector3(leftEdgeX, 0f, upperSideZ - 0.16f), Quaternion.Euler(0f, 90f, 0f), craneMaterial, steel, materials.Safety, shadow);
            CreateContainerStack(root, new Vector3(rightEdgeX, 0f, upperSideZ), Quaternion.Euler(0f, -90f, 0f), blueContainer, orangeContainer, greenContainer, shadow);
            CreateContainerStack(root, new Vector3(leftEdgeX, 0f, lowerSideZ), Quaternion.Euler(0f, 90f, 0f), redContainer, blueContainer, orangeContainer, shadow, 0.86f);
            CreateCargoPallet(root, new Vector3(rightEdgeX - 0.03f, 0f, centerSideZ + 0.58f), Quaternion.Euler(0f, -90f, 0f), palletMaterial, greenContainer, shadow);

            CreateBollardPair(root, new Vector3(leftEdgeX - 0.04f, 0f, centerSideZ), Quaternion.Euler(0f, 20f, 0f), steel, materials.Safety, shadow);
            CreateBollardPair(root, new Vector3(rightEdgeX + 0.04f, 0f, lowerSideZ + 0.55f), Quaternion.Euler(0f, -18f, 0f), steel, materials.Safety, shadow, 0.88f);
            CreateContainerStack(root, new Vector3(leftEdgeX + 0.04f, 0f, lowerSideZ - 0.50f), Quaternion.Euler(0f, 76f, 0f), greenContainer, orangeContainer, blueContainer, shadow, 0.72f);
            CreateCargoPallet(root, new Vector3(rightEdgeX - 0.04f, 0f, upperSideZ + 0.44f), Quaternion.Euler(0f, -78f, 0f), palletMaterial, redContainer, shadow, 0.76f);
            CreateHarborBeacon(root, new Vector3(leftEdgeX + 0.02f, 0f, upperSideZ + 0.43f), Quaternion.Euler(0f, -12f, 0f), steel, materials.LampBulb, materials.LampGlow, shadow);
            CreateHarborCrane(root, new Vector3(rightEdgeX - 0.02f, 0f, lowerSideZ - 0.44f), Quaternion.Euler(0f, 34f, 0f), craneMaterial, steel, materials.Safety, shadow, 0.78f);
        }

        private static void CreateContainerStack(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material firstMaterial,
            Material secondMaterial,
            Material thirdMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Harbor Container Stack Shadow", center, new Vector2(0.68f * scale, 0.36f * scale), shadowMaterial, rotation);
            CreateContainerBox(root, "Harbor Container Stack Lower", center + rotation * new Vector3(-0.115f * scale, 0.070f * scale, -0.012f * scale), rotation, firstMaterial, scale);
            CreateContainerBox(root, "Harbor Container Stack Upper", center + rotation * new Vector3(0.090f * scale, 0.165f * scale, 0.018f * scale), rotation, secondMaterial, scale * 0.92f);
            CreateContainerBox(root, "Harbor Container Stack Short", center + rotation * new Vector3(0.220f * scale, 0.072f * scale, -0.038f * scale), rotation, thirdMaterial, scale * 0.72f);
        }

        private static void CreateContainerBox(
            Transform root,
            string name,
            Vector3 center,
            Quaternion rotation,
            Material material,
            float scale)
        {
            CreateBox(name, root, center, new Vector3(0.36f * scale, 0.110f * scale, 0.155f * scale), material, rotation);
            var ribMaterial = PuzzlePalette.CreateTransparentMaterial($"{name} Rib", BoardThemePalette.WithAlpha(BoardThemePalette.HarborLine, 0.20f));
            for (var index = 0; index < 3; index++)
            {
                var x = Mathf.Lerp(-0.120f, 0.120f, index / 2f) * scale;
                CreateBox($"{name} Rib {index + 1}", root, center + rotation * new Vector3(x, 0.058f * scale, 0.081f * scale), new Vector3(0.012f * scale, 0.010f * scale, 0.010f * scale), ribMaterial, rotation);
            }
        }

        private static void CreateCargoPallet(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material baseMaterial,
            Material crateMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Harbor Cargo Pallet Shadow", center, new Vector2(0.56f * scale, 0.30f * scale), shadowMaterial, rotation);
            CreateBox("Harbor Cargo Pallet Base", root, center + Vector3.up * (0.045f * scale), new Vector3(0.44f * scale, 0.045f * scale, 0.170f * scale), baseMaterial, rotation);
            CreateBox("Harbor Cargo Crate A", root, center + rotation * new Vector3(-0.095f * scale, 0.120f * scale, 0.000f), new Vector3(0.160f * scale, 0.150f * scale, 0.145f * scale), crateMaterial, rotation);
            CreateBox("Harbor Cargo Crate B", root, center + rotation * new Vector3(0.100f * scale, 0.105f * scale, 0.010f * scale), new Vector3(0.145f * scale, 0.120f * scale, 0.130f * scale), crateMaterial, rotation);
            CreateBox("Harbor Cargo Strap", root, center + Vector3.up * (0.190f * scale), new Vector3(0.360f * scale, 0.016f * scale, 0.018f * scale), baseMaterial, rotation);
        }

        private static void CreateBollardPair(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material postMaterial,
            Material stripeMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Harbor Bollard Pair Shadow", center, new Vector2(0.40f * scale, 0.28f * scale), shadowMaterial, rotation);
            for (var index = 0; index < 2; index++)
            {
                var x = (index == 0 ? -0.105f : 0.105f) * scale;
                var postCenter = center + rotation * new Vector3(x, 0.075f * scale, 0f);
                CreateCylinder($"Harbor Bollard {index + 1}", root, postCenter, new Vector3(0.050f * scale, 0.150f * scale, 0.050f * scale), postMaterial, rotation);
                CreateBox($"Harbor Bollard Stripe {index + 1}", root, postCenter + Vector3.up * (0.038f * scale), new Vector3(0.070f * scale, 0.014f * scale, 0.018f * scale), stripeMaterial, rotation * Quaternion.Euler(0f, 35f, 0f));
            }
        }

        private static void CreateHarborCrane(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material craneMaterial,
            Material steelMaterial,
            Material stripeMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Harbor Crane Shadow", center, new Vector2(0.72f * scale, 0.38f * scale), shadowMaterial, rotation);
            CreateBox("Harbor Crane Left Leg", root, center + rotation * new Vector3(-0.165f * scale, 0.165f * scale, -0.020f * scale), new Vector3(0.040f * scale, 0.330f * scale, 0.045f * scale), steelMaterial, rotation);
            CreateBox("Harbor Crane Right Leg", root, center + rotation * new Vector3(0.165f * scale, 0.165f * scale, -0.020f * scale), new Vector3(0.040f * scale, 0.330f * scale, 0.045f * scale), steelMaterial, rotation);
            CreateBox("Harbor Crane Top Beam", root, center + rotation * new Vector3(0f, 0.335f * scale, -0.020f * scale), new Vector3(0.420f * scale, 0.038f * scale, 0.050f * scale), craneMaterial, rotation);
            CreateBox("Harbor Crane Boom", root, center + rotation * new Vector3(0.135f * scale, 0.300f * scale, 0.175f * scale), new Vector3(0.070f * scale, 0.032f * scale, 0.460f * scale), craneMaterial, rotation * Quaternion.Euler(0f, -8f, 0f));
            CreateBox("Harbor Crane Hook Cable", root, center + rotation * new Vector3(0.205f * scale, 0.190f * scale, 0.365f * scale), new Vector3(0.012f * scale, 0.170f * scale, 0.012f * scale), steelMaterial, rotation);
            CreateBox("Harbor Crane Hook", root, center + rotation * new Vector3(0.205f * scale, 0.092f * scale, 0.365f * scale), new Vector3(0.058f * scale, 0.026f * scale, 0.032f * scale), stripeMaterial, rotation);
        }

        private static void CreateHarborBeacon(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material poleMaterial,
            Material lightMaterial,
            Material glowMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Harbor Beacon Shadow", center, new Vector2(0.38f * scale, 0.30f * scale), shadowMaterial, rotation);
            CreateCylinder("Harbor Beacon Base", root, center + Vector3.up * (0.045f * scale), new Vector3(0.070f * scale, 0.090f * scale, 0.070f * scale), poleMaterial, rotation);
            CreateBox("Harbor Beacon Pole", root, center + Vector3.up * (0.175f * scale), new Vector3(0.030f * scale, 0.270f * scale, 0.030f * scale), poleMaterial, rotation);
            CreateSphere("Harbor Beacon Light", root, center + Vector3.up * (0.330f * scale), 0.065f * scale, lightMaterial);
            BoardGeometry.CreateFlatRoundedRect(
                "Harbor Beacon Glow",
                root,
                center + Vector3.up * (0.338f * scale),
                new Vector2(0.220f * scale, 0.190f * scale),
                0.090f * scale,
                glowMaterial,
                rotation);
        }

        private static void CreateFutureCityProps(Transform root, ThemeMaterials materials)
        {
            var leftEdgeX = BoardLayoutConfig.GridLeftX - BoardLayoutConfig.CellSize * 1.10f;
            var rightEdgeX = BoardLayoutConfig.GridRightX + BoardLayoutConfig.CellSize * 1.10f;
            var lowerSideZ = BoardLayoutConfig.GridBottomZ + BoardLayoutConfig.CellSize * 1.65f;
            var centerSideZ = BoardLayoutConfig.ParkingYardCenterZ - BoardLayoutConfig.CellSize * 0.18f;
            var upperSideZ = BoardLayoutConfig.ParkingYardTopZ - BoardLayoutConfig.CellSize * 1.16f;

            var steel = PuzzlePalette.CreateLitMaterial("Future City Prop Steel", BoardThemePalette.FutureSteel, 0.36f);
            var darkPanel = PuzzlePalette.CreateLitMaterial("Future City Prop Dark Panel", BoardThemePalette.FuturePanelDark, 0.34f);
            var neonBlue = PuzzlePalette.CreateLitMaterial("Future City Prop Neon Blue", BoardThemePalette.FutureNeonBlue, 0.32f);
            var neonPink = PuzzlePalette.CreateLitMaterial("Future City Prop Neon Pink", BoardThemePalette.FutureNeonPink, 0.32f);
            var neonGreen = PuzzlePalette.CreateLitMaterial("Future City Prop Neon Green", BoardThemePalette.FutureNeonGreen, 0.32f);
            var glass = PuzzlePalette.CreateTransparentMaterial("Future City Prop Holo Glass", BoardThemePalette.WithAlpha(BoardThemePalette.FutureGlass, 0.42f));
            var glow = PuzzlePalette.CreateTransparentMaterial("Future City Prop Glow", BoardThemePalette.WithAlpha(BoardThemePalette.FutureNeonBlue, 0.30f));
            var shadow = PuzzlePalette.CreateTransparentMaterial("Future City Prop Contact Shadow", BoardThemePalette.WithAlpha(BoardThemePalette.FutureRoadShadow, 0.20f));

            CreateHoloBillboard(root, new Vector3(leftEdgeX, 0f, upperSideZ - 0.16f), Quaternion.Euler(0f, 90f, 0f), steel, glass, neonPink, glow, shadow);
            CreateDronePad(root, new Vector3(rightEdgeX, 0f, upperSideZ), Quaternion.Euler(0f, -90f, 0f), steel, darkPanel, neonBlue, shadow);
            CreateDronePad(root, new Vector3(leftEdgeX, 0f, lowerSideZ), Quaternion.Euler(0f, 90f, 0f), steel, darkPanel, neonGreen, shadow, 0.86f);
            CreateDataBench(root, new Vector3(rightEdgeX - 0.03f, 0f, centerSideZ + 0.58f), Quaternion.Euler(0f, -90f, 0f), darkPanel, neonBlue, steel, shadow);

            CreateEnergyPylon(root, new Vector3(leftEdgeX - 0.04f, 0f, centerSideZ), Quaternion.Euler(0f, 20f, 0f), steel, glass, neonBlue, shadow);
            CreateEnergyPylon(root, new Vector3(rightEdgeX + 0.04f, 0f, lowerSideZ + 0.55f), Quaternion.Euler(0f, -18f, 0f), steel, glass, neonPink, shadow, 0.88f);
            CreateDataNodeCluster(root, new Vector3(leftEdgeX + 0.04f, 0f, lowerSideZ - 0.50f), neonGreen, shadow, 0.90f);
            CreateDataNodeCluster(root, new Vector3(rightEdgeX - 0.04f, 0f, upperSideZ + 0.44f), neonBlue, shadow, 0.76f);
            CreateCityBlockCluster(root, new Vector3(leftEdgeX + 0.02f, 0f, upperSideZ + 0.43f), Quaternion.Euler(0f, -12f, 0f), darkPanel, glass, neonBlue, shadow);
            CreateCityBlockCluster(root, new Vector3(rightEdgeX - 0.02f, 0f, lowerSideZ - 0.44f), Quaternion.Euler(0f, 34f, 0f), darkPanel, glass, neonPink, shadow, 0.86f);
        }

        private static void CreateHoloBillboard(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material frameMaterial,
            Material glassMaterial,
            Material accentMaterial,
            Material glowMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Future City Holo Billboard Shadow", center, new Vector2(0.70f, 0.34f), shadowMaterial, rotation);
            CreateBox("Future City Holo Billboard Left Post", root, center + rotation * new Vector3(-0.24f, 0.130f, -0.040f), new Vector3(0.034f, 0.260f, 0.036f), frameMaterial, rotation);
            CreateBox("Future City Holo Billboard Right Post", root, center + rotation * new Vector3(0.24f, 0.130f, -0.040f), new Vector3(0.034f, 0.260f, 0.036f), frameMaterial, rotation);
            CreateBox("Future City Holo Billboard Panel", root, center + rotation * new Vector3(0f, 0.220f, 0.006f), new Vector3(0.58f, 0.220f, 0.030f), glassMaterial, rotation);
            CreateBox("Future City Holo Billboard Top Pulse", root, center + rotation * new Vector3(0f, 0.345f, 0.020f), new Vector3(0.48f, 0.020f, 0.034f), accentMaterial, rotation);
            CreateBox("Future City Holo Billboard Scan Line", root, center + rotation * new Vector3(0f, 0.225f, 0.026f), new Vector3(0.44f, 0.012f, 0.020f), glowMaterial, rotation);
        }

        private static void CreateDronePad(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material rimMaterial,
            Material deckMaterial,
            Material neonMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Future City Drone Pad Shadow", center, new Vector2(0.58f * scale, 0.34f * scale), shadowMaterial, rotation);
            CreateCylinder("Future City Drone Pad Outer Ring", root, center + Vector3.up * (0.035f * scale), new Vector3(0.34f * scale, 0.036f * scale, 0.34f * scale), rimMaterial, rotation);
            CreateCylinder("Future City Drone Pad Deck", root, center + Vector3.up * (0.041f * scale), new Vector3(0.255f * scale, 0.038f * scale, 0.255f * scale), deckMaterial, rotation);
            CreateCylinder("Future City Drone Pad Core", root, center + Vector3.up * (0.047f * scale), new Vector3(0.105f * scale, 0.041f * scale, 0.105f * scale), neonMaterial, rotation);
            CreateBox("Future City Drone Pad Approach A", root, center + rotation * new Vector3(0f, 0.058f * scale, 0.220f * scale), new Vector3(0.070f * scale, 0.010f * scale, 0.160f * scale), neonMaterial, rotation);
            CreateBox("Future City Drone Pad Approach B", root, center + rotation * new Vector3(0f, 0.059f * scale, -0.220f * scale), new Vector3(0.070f * scale, 0.010f * scale, 0.160f * scale), neonMaterial, rotation);
        }

        private static void CreateDataBench(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material seatMaterial,
            Material screenMaterial,
            Material frameMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Future City Data Bench Shadow", center, new Vector2(0.54f, 0.27f), shadowMaterial, rotation);
            CreateBox("Future City Data Bench Seat", root, center + Vector3.up * 0.105f, new Vector3(0.46f, 0.055f, 0.15f), seatMaterial, rotation);
            CreateBox("Future City Data Bench Back Screen", root, center + rotation * new Vector3(0f, 0.190f, 0.085f), new Vector3(0.46f, 0.145f, 0.046f), screenMaterial, rotation);
            CreateBox("Future City Data Bench Left Leg", root, center + rotation * new Vector3(-0.16f, 0.045f, -0.040f), new Vector3(0.040f, 0.090f, 0.050f), frameMaterial, rotation);
            CreateBox("Future City Data Bench Right Leg", root, center + rotation * new Vector3(0.16f, 0.045f, -0.040f), new Vector3(0.040f, 0.090f, 0.050f), frameMaterial, rotation);
        }

        private static void CreateEnergyPylon(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material baseMaterial,
            Material beamMaterial,
            Material capMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Future City Energy Pylon Shadow", center, new Vector2(0.38f * scale, 0.30f * scale), shadowMaterial, rotation);
            CreateCylinder("Future City Energy Pylon Base", root, center + Vector3.up * (0.042f * scale), new Vector3(0.150f * scale, 0.055f * scale, 0.150f * scale), baseMaterial, rotation);
            CreateCylinder("Future City Energy Pylon Beam", root, center + Vector3.up * (0.175f * scale), new Vector3(0.042f * scale, 0.300f * scale, 0.042f * scale), beamMaterial, rotation);
            CreateSphere("Future City Energy Pylon Cap", root, center + Vector3.up * (0.335f * scale), 0.082f * scale, capMaterial);
            BoardGeometry.CreateFlatRoundedRect(
                "Future City Energy Pylon Glow",
                root,
                center + Vector3.up * (0.342f * scale),
                new Vector2(0.270f * scale, 0.245f * scale),
                0.120f * scale,
                beamMaterial,
                rotation);
        }

        private static void CreateDataNodeCluster(
            Transform root,
            Vector3 position,
            Material nodeMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Future City Data Node Shadow", position, new Vector2(0.38f * scale, 0.26f * scale), shadowMaterial, Quaternion.identity);
            CreateSphere("Future City Data Node A", root, position + new Vector3(-0.075f * scale, 0.052f * scale, -0.020f * scale), 0.085f * scale, nodeMaterial);
            CreateSphere("Future City Data Node B", root, position + new Vector3(0.060f * scale, 0.066f * scale, 0.030f * scale), 0.073f * scale, nodeMaterial);
            CreateSphere("Future City Data Node C", root, position + new Vector3(0.150f * scale, 0.046f * scale, -0.040f * scale), 0.058f * scale, nodeMaterial);
        }

        private static void CreateCityBlockCluster(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material bodyMaterial,
            Material glassMaterial,
            Material neonMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Future City Block Cluster Shadow", center, new Vector2(0.48f * scale, 0.34f * scale), shadowMaterial, rotation);
            CreateBox("Future City Block Tower A", root, center + rotation * new Vector3(-0.120f * scale, 0.130f * scale, -0.020f * scale), new Vector3(0.110f * scale, 0.260f * scale, 0.110f * scale), bodyMaterial, rotation);
            CreateBox("Future City Block Tower B", root, center + rotation * new Vector3(0.015f * scale, 0.185f * scale, 0.035f * scale), new Vector3(0.120f * scale, 0.370f * scale, 0.110f * scale), glassMaterial, rotation);
            CreateBox("Future City Block Tower C", root, center + rotation * new Vector3(0.150f * scale, 0.105f * scale, -0.030f * scale), new Vector3(0.095f * scale, 0.210f * scale, 0.100f * scale), bodyMaterial, rotation);
            CreateBox("Future City Block Neon Bridge", root, center + rotation * new Vector3(0.020f * scale, 0.235f * scale, 0.090f * scale), new Vector3(0.250f * scale, 0.018f * scale, 0.026f * scale), neonMaterial, rotation);
        }

        private static void CreateHologramCore(
            Transform root,
            Vector3 center,
            Material coreMaterial,
            Material glowMaterial,
            Material baseMaterial,
            Material beamMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Future City Hologram Core Shadow", center, new Vector2(0.58f, 0.38f), shadowMaterial, Quaternion.identity);
            BoardGeometry.CreateFlatRoundedRect("Future City Hologram Core Rim", root, center + Vector3.down * 0.006f, new Vector2(0.54f, 0.34f), 0.15f, baseMaterial, Quaternion.Euler(0f, -8f, 0f));
            BoardGeometry.CreateFlatRoundedRect("Future City Hologram Core Plate", root, center + Vector3.up * 0.004f, new Vector2(0.43f, 0.24f), 0.11f, coreMaterial, Quaternion.Euler(0f, -8f, 0f));
            BoardGeometry.CreateFlatRoundedRect("Future City Hologram Core Glow", root, center + new Vector3(-0.06f, 0.012f, 0.02f), new Vector2(0.16f, 0.055f), 0.025f, glowMaterial, Quaternion.Euler(0f, -18f, 0f));
            CreateCylinder("Future City Hologram Core Beam", root, center + Vector3.up * 0.145f, new Vector3(0.055f, 0.270f, 0.055f), beamMaterial, Quaternion.identity);
        }

        private static void CreateSurfboardRack(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material boardMaterialA,
            Material boardMaterialB,
            Material stripeMaterial,
            Material poleMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Waikiki Surfboard Rack Shadow", center, new Vector2(0.70f, 0.36f), shadowMaterial, rotation);
            CreateBox("Waikiki Surfboard Rack Left Post", root, center + rotation * new Vector3(-0.22f, 0.058f, -0.045f), new Vector3(0.032f, 0.116f, 0.038f), poleMaterial, rotation);
            CreateBox("Waikiki Surfboard Rack Right Post", root, center + rotation * new Vector3(0.22f, 0.058f, -0.045f), new Vector3(0.032f, 0.116f, 0.038f), poleMaterial, rotation);
            CreateBox("Waikiki Surfboard A", root, center + rotation * new Vector3(-0.16f, 0.120f, 0.018f), new Vector3(0.108f, 0.032f, 0.530f), boardMaterialA, rotation * Quaternion.Euler(0f, -8f, 0f));
            CreateBox("Waikiki Surfboard B", root, center + rotation * new Vector3(0.00f, 0.140f, -0.004f), new Vector3(0.112f, 0.032f, 0.560f), boardMaterialB, rotation * Quaternion.Euler(0f, 6f, 0f));
            CreateBox("Waikiki Surfboard C", root, center + rotation * new Vector3(0.17f, 0.116f, 0.024f), new Vector3(0.100f, 0.032f, 0.500f), boardMaterialA, rotation * Quaternion.Euler(0f, 12f, 0f));
            CreateBox("Waikiki Surfboard Stripe", root, center + rotation * new Vector3(0.00f, 0.163f, -0.020f), new Vector3(0.066f, 0.010f, 0.400f), stripeMaterial, rotation * Quaternion.Euler(0f, 6f, 0f));
        }

        private static void CreateLifebuoyStack(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material outerMaterial,
            Material insetMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Waikiki Lifebuoy Stack Shadow", center, new Vector2(0.56f * scale, 0.32f * scale), shadowMaterial, rotation);
            for (var index = 0; index < 3; index++)
            {
                var layerCenter = center + Vector3.up * ((0.040f + index * 0.055f) * scale) + rotation * new Vector3((index - 1) * 0.018f * scale, 0f, 0f);
                CreateCylinder($"Waikiki Lifebuoy Outer {index + 1}", root, layerCenter, new Vector3(0.32f * scale, 0.038f * scale, 0.32f * scale), outerMaterial, rotation);
                CreateCylinder($"Waikiki Lifebuoy Inset {index + 1}", root, layerCenter + Vector3.up * (0.003f * scale), new Vector3(0.205f * scale, 0.041f * scale, 0.205f * scale), insetMaterial, rotation);
                CreateCylinder($"Waikiki Lifebuoy Center {index + 1}", root, layerCenter + Vector3.up * (0.006f * scale), new Vector3(0.095f * scale, 0.044f * scale, 0.095f * scale), outerMaterial, rotation);
            }
        }

        private static void CreateBeachBench(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material seatMaterial,
            Material legMaterial,
            Material shadowMaterial)
        {
            CreatePropShadow(root, "Waikiki Bench Shadow", center, new Vector2(0.54f, 0.27f), shadowMaterial, rotation);
            CreateBox("Waikiki Bench Seat", root, center + Vector3.up * 0.105f, new Vector3(0.46f, 0.055f, 0.15f), seatMaterial, rotation);
            CreateBox("Waikiki Bench Back", root, center + rotation * new Vector3(0f, 0.185f, 0.085f), new Vector3(0.46f, 0.155f, 0.050f), seatMaterial, rotation);
            CreateBox("Waikiki Bench Left Leg", root, center + rotation * new Vector3(-0.16f, 0.045f, -0.040f), new Vector3(0.040f, 0.090f, 0.050f), legMaterial, rotation);
            CreateBox("Waikiki Bench Right Leg", root, center + rotation * new Vector3(0.16f, 0.045f, -0.040f), new Vector3(0.040f, 0.090f, 0.050f), legMaterial, rotation);
        }

        private static void CreateBeachUmbrella(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material primaryMaterial,
            Material secondaryMaterial,
            Material poleMaterial,
            Material shadowMaterial)
        {
            CreateBeachUmbrella(root, center, rotation, primaryMaterial, secondaryMaterial, poleMaterial, shadowMaterial, 1f);
        }

        private static void CreateBeachUmbrella(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material primaryMaterial,
            Material secondaryMaterial,
            Material poleMaterial,
            Material shadowMaterial,
            float scale)
        {
            CreatePropShadow(root, "Waikiki Umbrella Shadow", center, new Vector2(0.36f * scale, 0.30f * scale), shadowMaterial, rotation);
            CreateCylinder("Waikiki Umbrella Pole", root, center + Vector3.up * (0.102f * scale), new Vector3(0.020f * scale, 0.205f * scale, 0.020f * scale), poleMaterial, rotation);
            BoardGeometry.CreateFlatRoundedRect(
                "Waikiki Umbrella Canopy",
                root,
                center + Vector3.up * (0.220f * scale),
                new Vector2(0.330f * scale, 0.285f * scale),
                0.130f * scale,
                primaryMaterial,
                rotation);
            CreateBox("Waikiki Umbrella Stripe A", root, center + Vector3.up * (0.230f * scale), new Vector3(0.040f * scale, 0.010f * scale, 0.245f * scale), secondaryMaterial, rotation);
            CreateBox("Waikiki Umbrella Stripe B", root, center + Vector3.up * (0.234f * scale), new Vector3(0.230f * scale, 0.010f * scale, 0.040f * scale), secondaryMaterial, rotation);
        }

        private static void CreateShellCluster(
            Transform root,
            Vector3 position,
            Material shellMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Waikiki Shell Cluster Shadow", position, new Vector2(0.38f * scale, 0.26f * scale), shadowMaterial, Quaternion.identity);
            CreateSphere("Waikiki Shell Cluster A", root, position + new Vector3(-0.075f * scale, 0.052f * scale, -0.020f * scale), 0.090f * scale, shellMaterial);
            CreateSphere("Waikiki Shell Cluster B", root, position + new Vector3(0.060f * scale, 0.060f * scale, 0.030f * scale), 0.082f * scale, shellMaterial);
            CreateSphere("Waikiki Shell Cluster C", root, position + new Vector3(0.150f * scale, 0.044f * scale, -0.040f * scale), 0.064f * scale, shellMaterial);
        }

        private static void CreatePalmTree(
            Transform root,
            Vector3 center,
            Quaternion rotation,
            Material trunkMaterial,
            Material leafMaterial,
            Material shadowMaterial,
            float scale = 1f)
        {
            CreatePropShadow(root, "Waikiki Palm Shadow", center, new Vector2(0.42f * scale, 0.32f * scale), shadowMaterial, rotation);
            CreateCylinder("Waikiki Palm Trunk", root, center + Vector3.up * (0.150f * scale), new Vector3(0.052f * scale, 0.300f * scale, 0.052f * scale), trunkMaterial, rotation * Quaternion.Euler(0f, 0f, -6f));
            CreateSphere("Waikiki Palm Crown", root, center + Vector3.up * (0.315f * scale), 0.100f * scale, leafMaterial);
            for (var index = 0; index < 5; index++)
            {
                var leafRotation = rotation * Quaternion.Euler(0f, index * 72f, 0f);
                CreateBox(
                    $"Waikiki Palm Leaf {index + 1}",
                    root,
                    center + Vector3.up * (0.315f * scale) + leafRotation * Vector3.forward * (0.115f * scale),
                    new Vector3(0.070f * scale, 0.018f * scale, 0.270f * scale),
                    leafMaterial,
                    leafRotation);
            }
        }

        private static void CreateTidePool(
            Transform root,
            Vector3 center,
            Material waterMaterial,
            Material waterLightMaterial,
            Material shellMaterial)
        {
            BoardGeometry.CreateFlatRoundedRect(
                "Waikiki Tide Pool Sand Rim",
                root,
                center + Vector3.down * 0.006f,
                new Vector2(0.54f, 0.34f),
                0.15f,
                shellMaterial,
                Quaternion.Euler(0f, -8f, 0f));
            BoardGeometry.CreateFlatRoundedRect(
                "Waikiki Tide Pool Water",
                root,
                center + Vector3.up * 0.004f,
                new Vector2(0.43f, 0.24f),
                0.11f,
                waterMaterial,
                Quaternion.Euler(0f, -8f, 0f));
            BoardGeometry.CreateFlatRoundedRect(
                "Waikiki Tide Pool Highlight",
                root,
                center + new Vector3(-0.06f, 0.012f, 0.02f),
                new Vector2(0.16f, 0.055f),
                0.025f,
                waterLightMaterial,
                Quaternion.Euler(0f, -18f, 0f));
        }

        private static void CreatePropShadow(
            Transform root,
            string name,
            Vector3 center,
            Vector2 size,
            Material material,
            Quaternion rotation)
        {
            BoardGeometry.CreateFlatRoundedRect(
                name,
                root,
                new Vector3(center.x, -0.016f, center.z),
                size,
                Mathf.Min(size.x, size.y) * 0.42f,
                material,
                rotation);
        }

        private static void CreateFeederPathBand(
            Transform root,
            string name,
            RotaryLayout layout,
            int side,
            RotaryRoadBuildSettings settings,
            float innerExtraOffset,
            float outerExtraOffset,
            float y,
            Material material)
        {
            var path = CreateExtendedFeederPath(layout.GetFeederPath(side));
            if (path == null)
            {
                return;
            }

            var outerRoadOffset = layout.RoadOuterOffset;
            BoardGeometry.CreateOpenPathBand(
                name,
                root,
                layout,
                path,
                settings.RotaryCenterZ,
                y,
                outerRoadOffset + innerExtraOffset,
                outerRoadOffset + outerExtraOffset,
                material,
                progress => layout.SampleFeederPath(side, path, progress));
        }

        private static void CreateQueuePosts(
            Transform root,
            RotaryLayout layout,
            int side,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials)
        {
            var path = CreateExtendedFeederPath(layout.GetFeederPath(side));
            if (path == null)
            {
                return;
            }

            var outerRoadOffset = layout.RoadOuterOffset + 0.36f;
            for (var index = 0; index < 5; index++)
            {
                var progress = Mathf.Lerp(0.16f, 0.84f, index / 4f);
                var sample = layout.SampleFeederPath(side, path, progress);
                var position = layout.ToWorldPoint(sample.Point + sample.Outward * outerRoadOffset, settings.RotaryCenterZ, 0f);
                if (position.z < BoardLayoutConfig.StationZ + 0.40f)
                {
                    continue;
                }

                CreateBox(
                    $"Queue Guide Post {side} {index + 1}",
                    root,
                    position + Vector3.up * 0.055f,
                    new Vector3(0.026f, 0.110f, 0.026f),
                    materials.Pole,
                    Quaternion.identity);
            }
        }

        private static FeederRoadPath CreateExtendedFeederPath(FeederRoadPath path)
        {
            if (path == null || path.Points.Length < 2)
            {
                return path;
            }

            var start = path.Points[0];
            var next = path.Points[1];
            var extensionDirection = start - next;
            if (extensionDirection.sqrMagnitude < 0.0001f)
            {
                extensionDirection = Vector2.up;
            }

            extensionDirection.Normalize();

            var points = new Vector2[path.Points.Length + 1];
            points[0] = start + extensionDirection * 1.20f;
            for (var index = 0; index < path.Points.Length; index++)
            {
                points[index + 1] = path.Points[index];
            }

            return new FeederRoadPath(points);
        }

        private static void CreateFlowerDot(Transform root, Vector3 position, Material material)
        {
            BoardGeometry.CreateFlatRoundedRect(
                "Rotary Flower Dot",
                root,
                position,
                new Vector2(0.050f, 0.050f),
                0.025f,
                material);
        }

        private static void CreateLamp(Transform root, Vector3 position, float height, Material pole, Material glow, Material bulb)
        {
            CreateBox("Terminal Lamp Pole", root, position + Vector3.up * (height * 0.5f), new Vector3(0.020f, height, 0.020f), pole, Quaternion.identity);
            CreateBox("Terminal Lamp Arm", root, position + new Vector3(0.040f, height * 0.92f, 0f), new Vector3(0.090f, 0.018f, 0.018f), pole, Quaternion.identity);
            CreateSphere("Terminal Lamp Bulb", root, position + new Vector3(0.082f, height * 0.88f, 0f), 0.032f, bulb);
            BoardGeometry.CreateFlatRoundedRect(
                "Terminal Lamp Glow",
                root,
                position + new Vector3(0.082f, height * 0.875f, 0f),
                new Vector2(0.12f, 0.12f),
                0.060f,
                glow,
                Quaternion.Euler(0f, 35f, 0f));
        }

        private static void CreateCrosswalk(Transform root, Vector3 center, float width, float depth, Material material)
        {
            const int stripes = 4;
            for (var index = 0; index < stripes; index++)
            {
                var x = Mathf.Lerp(-width * 0.40f, width * 0.40f, index / (stripes - 1f));
                BoardGeometry.CreateFlatRect(
                    $"Crosswalk Stripe {index + 1}",
                    root,
                    center + Vector3.right * x,
                    new Vector2(0.045f, depth),
                    material);
            }
        }

        private static GameObject CreateBox(string name, Transform root, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var box = VisualPrimitiveFactory.Create(PrimitiveType.Cube, name);
            box.transform.SetParent(root, false);
            box.transform.SetPositionAndRotation(position, rotation);
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            DisablePhysics(box);
            return box;
        }

        private static GameObject CreateCylinder(string name, Transform root, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var cylinder = VisualPrimitiveFactory.Create(PrimitiveType.Cylinder, name);
            cylinder.transform.SetParent(root, false);
            cylinder.transform.SetPositionAndRotation(position, rotation);
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            DisablePhysics(cylinder);
            return cylinder;
        }

        private static GameObject CreateSphere(string name, Transform root, Vector3 position, float radius, Material material)
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

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private readonly struct ThemeMaterials
        {
            public readonly Material Pavement;
            public readonly Material PaverA;
            public readonly Material PaverB;
            public readonly Material Safety;
            public readonly Material WhiteLine;
            public readonly Material Pole;
            public readonly Material LampGlow;
            public readonly Material LampBulb;
            public readonly Material QueueGuide;
            public readonly Material QueueFloor;

            public ThemeMaterials(
                Material pavement,
                Material paverA,
                Material paverB,
                Material safety,
                Material whiteLine,
                Material pole,
                Material lampGlow,
                Material lampBulb,
                Material queueGuide,
                Material queueFloor)
            {
                Pavement = pavement;
                PaverA = paverA;
                PaverB = paverB;
                Safety = safety;
                WhiteLine = whiteLine;
                Pole = pole;
                LampGlow = lampGlow;
                LampBulb = lampBulb;
                QueueGuide = queueGuide;
                QueueFloor = queueFloor;
            }
        }
    }
}
