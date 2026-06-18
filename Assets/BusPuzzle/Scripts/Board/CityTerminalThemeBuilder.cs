using UnityEngine;

namespace BusPuzzle
{
    internal static class CityTerminalThemeBuilder
    {
        private static readonly Color PavementColor = new Color(0.68f, 0.73f, 0.77f);
        private static readonly Color PaverColorA = new Color(0.74f, 0.79f, 0.82f);
        private static readonly Color PaverColorB = new Color(0.62f, 0.68f, 0.72f);
        private static readonly Color SafetyYellow = new Color(0.93f, 0.70f, 0.16f);
        private static readonly Color LineWhite = new Color(0.90f, 0.94f, 0.96f);
        private static readonly Color TerminalBlue = new Color(0.18f, 0.35f, 0.50f);
        private static readonly Color PoleColor = new Color(0.32f, 0.38f, 0.44f);
        private static readonly Color LightColor = new Color(1.00f, 0.92f, 0.64f);
        private static readonly Color TreeLeafColor = new Color(0.24f, 0.49f, 0.30f);
        private static readonly Color TrunkColor = new Color(0.39f, 0.25f, 0.14f);
        private static readonly Color QueueGuideColor = new Color(0.96f, 0.86f, 0.52f, 0.42f);
        private const float QueueGuideY = -0.011f;

        public static void Create(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings)
        {
            var root = new GameObject("City Terminal Theme").transform;
            root.SetParent(parent, false);
            var materials = CreateMaterials();

            CreateBusYardEdgeSkin(CreateSection(root, "1 Bus Yard Edge Skin"), materials);
            CreateStationSkin(CreateSection(root, "2 Station Skin"));
            CreateQueueFloorSkin(CreateSection(root, "3 Queue Floor Skin"), layout, settings, materials);
            CreateQueueOuterLineSkin(CreateSection(root, "4 Queue Outer Line Skin"), layout, settings, materials);
            CreateRotaryCenterSkin(CreateSection(root, "5 Rotary Center Skin"), settings, materials);
            CreateQueueSurroundings(CreateSection(root, "6 Queue Surroundings"), settings, materials);
            CreateRotarySideMargins(CreateSection(root, "7 Rotary Side Margins"), settings, materials);
        }

        private static ThemeMaterials CreateMaterials()
        {
            return new ThemeMaterials(
                PuzzlePalette.CreateSolidMaterial("City Terminal Pavement", PavementColor),
                PuzzlePalette.CreateSolidMaterial("City Terminal Paver A", PaverColorA),
                PuzzlePalette.CreateSolidMaterial("City Terminal Paver B", PaverColorB),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Safety Yellow", new Color(SafetyYellow.r, SafetyYellow.g, SafetyYellow.b, 0.36f)),
                PuzzlePalette.CreateTransparentMaterial("City Terminal White Line", new Color(LineWhite.r, LineWhite.g, LineWhite.b, 0.52f)),
                PuzzlePalette.CreateSolidMaterial("City Terminal Pole", PoleColor),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Lamp Glow", new Color(1.00f, 0.88f, 0.48f, 0.18f)),
                PuzzlePalette.CreateSolidMaterial("City Terminal Lamp Bulb", LightColor),
                PuzzlePalette.CreateSolidMaterial("City Terminal Tree Leaf", TreeLeafColor),
                PuzzlePalette.CreateSolidMaterial("City Terminal Tree Trunk", TrunkColor),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Queue Guide", QueueGuideColor),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Queue Floor", new Color(0.70f, 0.78f, 0.82f, 0.22f)));
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

            var paverCount = Mathf.CeilToInt(BoardLayoutConfig.ParkingYardWorldDepth / 0.31f);
            CreatePaverStrip(root, materials.PaverA, materials.PaverB, -2.48f, BoardLayoutConfig.ParkingYardCenterZ, paverCount, 0.31f);
            CreatePaverStrip(root, materials.PaverB, materials.PaverA, 2.48f, BoardLayoutConfig.ParkingYardCenterZ, paverCount, 0.31f);
            CreateYardMarkings(root, materials.WhiteLine);
        }

        private static void CreateStationSkin(Transform root)
        {
            root.gameObject.SetActive(true);
            CreateTerminalStationSkin(root);
        }

        private static void CreateTerminalStationSkin(Transform root)
        {
            var rotation = Quaternion.identity;
            var totalSlots = BoardLayoutConfig.TotalStationSlots;
            var slotSpacing = BoardLayoutConfig.StationSlotSpacing;
            var slotWidth = BoardLayoutConfig.StationSlotWidth;
            var slotDepth = BoardLayoutConfig.StationSlotDepth;
            var platformWidth = (totalSlots - 1) * slotSpacing + slotWidth + 0.40f;
            var platformDepth = slotDepth + 0.20f;

            var deckShadowMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal Platform Shadow", new Color(0.12f, 0.16f, 0.22f, 0.16f));
            var deckMaterial = PuzzlePalette.CreateSolidMaterial("Stage 14 Terminal Deck", new Color(0.58f, 0.66f, 0.74f));
            var deckTopMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal Deck Top", new Color(0.72f, 0.80f, 0.88f, 0.44f));
            var bayMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal Bay", new Color(0.78f, 0.86f, 0.94f, 0.24f));
            var bayTrimMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal Bay Trim", new Color(0.96f, 0.99f, 1.00f, 0.54f));
            var railMaterial = PuzzlePalette.CreateSolidMaterial("Stage 14 Terminal Rail", new Color(0.78f, 0.85f, 0.92f));
            var railPostMaterial = PuzzlePalette.CreateSolidMaterial("Stage 14 Terminal Rail Post", new Color(0.63f, 0.71f, 0.80f));
            var vipMaterial = PuzzlePalette.CreateSolidMaterial("Stage 14 Terminal VIP Gold", new Color(0.96f, 0.67f, 0.08f));
            var vipInsetMaterial = PuzzlePalette.CreateSolidMaterial("Stage 14 Terminal VIP Inset", new Color(1.00f, 0.86f, 0.28f));
            var whiteLineMaterial = PuzzlePalette.CreateTransparentMaterial("Stage 14 Terminal White Paint", new Color(0.92f, 0.96f, 1.00f, 0.72f));

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

            CreateVipTerminalBay(root, BoardLayoutConfig.GetFreeStationPosition(), slotWidth, slotDepth, rotation, vipMaterial, vipInsetMaterial, whiteLineMaterial);
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
            Material whiteLineMaterial)
        {
            CreateBox(
                "Stage 14 Terminal VIP Raised Plinth",
                root,
                new Vector3(position.x, -0.048f, position.z),
                new Vector3(slotWidth + 0.090f, 0.035f, slotDepth + 0.055f),
                vipMaterial,
                rotation);
            BoardGeometry.CreateFlatRoundedRect(
                "Stage 14 Terminal VIP Inset",
                root,
                position + Vector3.up * -0.026f,
                new Vector2(slotWidth - 0.040f, slotDepth - 0.070f),
                slotWidth * 0.13f,
                vipInsetMaterial,
                rotation);
            BoardGeometry.CreateFlatRect(
                "Stage 14 Terminal VIP Front Stripe",
                root,
                position + Vector3.up * -0.017f - rotation * Vector3.forward * (slotDepth * 0.33f),
                new Vector2(slotWidth * 0.54f, 0.018f),
                whiteLineMaterial,
                rotation);
        }

        private static Vector3 StationLocalPoint(float localX, float y, float worldZ)
        {
            return new Vector3(localX, y, worldZ);
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
            ThemeMaterials materials)
        {
            var center = new Vector3(0f, 0f, settings.RotaryCenterZ);
            CreateSmallTree(root, center + new Vector3(0.38f, 0f, -0.05f), materials.Leaf, materials.Trunk, 0.105f);
            CreateSmallTree(root, center + new Vector3(-0.42f, 0f, -0.15f), materials.Leaf, materials.Trunk, 0.085f);
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
            ThemeMaterials materials)
        {
            var z = settings.RotaryCenterZ + 0.12f;
            BoardGeometry.CreateFlatRect(
                "Left Rotary Plaza Pavers",
                root,
                new Vector3(-2.45f, -0.083f, z),
                new Vector2(0.38f, 1.55f),
                materials.PaverA);
            BoardGeometry.CreateFlatRect(
                "Right Rotary Plaza Pavers",
                root,
                new Vector3(2.45f, -0.083f, z),
                new Vector2(0.38f, 1.55f),
                materials.PaverB);

            CreateSmallTree(root, new Vector3(-2.43f, 0f, z - 0.48f), materials.Leaf, materials.Trunk, 0.095f);
            CreateSmallTree(root, new Vector3(2.43f, 0f, z + 0.48f), materials.Leaf, materials.Trunk, 0.095f);
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

        private static void CreateSmallTree(Transform root, Vector3 position, Material leaf, Material trunk, float radius)
        {
            CreateBox("Terminal Tree Trunk", root, position + Vector3.up * 0.10f, new Vector3(radius * 0.18f, 0.20f, radius * 0.18f), trunk, Quaternion.identity);
            CreateSphere("Terminal Tree Crown", root, position + Vector3.up * 0.25f, radius, leaf);
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

        private static void CreatePaverStrip(
            Transform root,
            Material first,
            Material second,
            float x,
            float startZ,
            int count,
            float spacing)
        {
            for (var index = 0; index < count; index++)
            {
                BoardGeometry.CreateFlatRect(
                    $"Sidewalk Paver {x:0.0} {index + 1}",
                    root,
                    new Vector3(x, -0.078f, startZ + (index - count * 0.5f) * spacing),
                    new Vector2(0.18f, 0.12f),
                    index % 2 == 0 ? first : second);
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
            public readonly Material Leaf;
            public readonly Material Trunk;
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
                Material leaf,
                Material trunk,
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
                Leaf = leaf;
                Trunk = trunk;
                QueueGuide = queueGuide;
                QueueFloor = queueFloor;
            }
        }
    }
}
