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
        private static readonly Color ShopWallA = new Color(0.58f, 0.66f, 0.68f);
        private static readonly Color ShopWallB = new Color(0.60f, 0.50f, 0.44f);
        private static readonly Color ShopWallC = new Color(0.48f, 0.60f, 0.54f);
        private static readonly Color ShopAwningA = new Color(0.18f, 0.39f, 0.55f);
        private static readonly Color ShopAwningB = new Color(0.76f, 0.54f, 0.20f);
        private static readonly Color ShopAwningC = new Color(0.45f, 0.30f, 0.48f);
        private static readonly Color ShopWindow = new Color(0.72f, 0.86f, 0.92f, 0.56f);
        private const float QueueGuideY = -0.011f;

        public static void Create(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings)
        {
            var root = new GameObject("City Terminal Theme").transform;
            root.SetParent(parent, false);
            var materials = CreateMaterials();
            var prefabLibrary = ThemePrefabLibrary.Load();

            CreateBusYardEdgeSkin(CreateSection(root, "1 Bus Yard Edge Skin"), materials);
            CreateStationSkin(CreateSection(root, "2 Station Skin"));
            CreateQueueFloorSkin(CreateSection(root, "3 Queue Floor Skin"), layout, settings, materials);
            CreateQueueOuterLineSkin(CreateSection(root, "4 Queue Outer Line Skin"), layout, settings, materials);
            CreateRotaryCenterSkin(CreateSection(root, "5 Rotary Center Skin"), settings, materials);
            CreateQueueSurroundings(CreateSection(root, "6 Queue Surroundings"), materials, prefabLibrary);
            CreateRotarySideMargins(CreateSection(root, "7 Rotary Side Margins"), settings, materials, prefabLibrary);
            CreateEdgeShopSkin(CreateSection(root, "8 Edge Shop Skin"), materials, prefabLibrary);
        }

        private static ThemeMaterials CreateMaterials()
        {
            return new ThemeMaterials(
                PuzzlePalette.CreateSolidMaterial("City Terminal Pavement", PavementColor),
                PuzzlePalette.CreateSolidMaterial("City Terminal Paver A", PaverColorA),
                PuzzlePalette.CreateSolidMaterial("City Terminal Paver B", PaverColorB),
                PuzzlePalette.CreateSolidMaterial("City Terminal Safety Yellow", SafetyYellow),
                PuzzlePalette.CreateSolidMaterial("City Terminal White Line", LineWhite),
                PuzzlePalette.CreateSolidMaterial("City Terminal Pole", PoleColor),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Lamp Glow", new Color(1.00f, 0.88f, 0.48f, 0.18f)),
                PuzzlePalette.CreateSolidMaterial("City Terminal Lamp Bulb", LightColor),
                PuzzlePalette.CreateSolidMaterial("City Terminal Tree Leaf", TreeLeafColor),
                PuzzlePalette.CreateSolidMaterial("City Terminal Tree Trunk", TrunkColor),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Queue Guide", QueueGuideColor),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Queue Floor", new Color(0.70f, 0.78f, 0.82f, 0.22f)),
                PuzzlePalette.CreateSolidMaterial("City Terminal Shop Wall A", ShopWallA),
                PuzzlePalette.CreateSolidMaterial("City Terminal Shop Wall B", ShopWallB),
                PuzzlePalette.CreateSolidMaterial("City Terminal Shop Wall C", ShopWallC),
                PuzzlePalette.CreateSolidMaterial("City Terminal Shop Awning A", ShopAwningA),
                PuzzlePalette.CreateSolidMaterial("City Terminal Shop Awning B", ShopAwningB),
                PuzzlePalette.CreateSolidMaterial("City Terminal Shop Awning C", ShopAwningC),
                PuzzlePalette.CreateTransparentMaterial("City Terminal Shop Window", ShopWindow));
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

            CreatePaverStrip(root, materials.PaverA, materials.PaverB, -2.48f, BoardLayoutConfig.GridCenterZ, 14, 0.31f);
            CreatePaverStrip(root, materials.PaverB, materials.PaverA, 2.48f, BoardLayoutConfig.GridCenterZ, 14, 0.31f);
            CreateYardMarkings(root, materials.Safety, materials.WhiteLine);
        }

        private static void CreateStationSkin(Transform root)
        {
            // Station slots are gameplay UI. The theme package keeps this section as a hook
            // so future themes can skin it without mixing station edits into other regions.
            root.gameObject.SetActive(true);
        }

        private static void CreateYardMarkings(Transform root, Material safety, Material whiteLine)
        {
            var gridCenter = new Vector3(0f, 0f, BoardLayoutConfig.GridCenterZ);
            var yardTopZ = BoardLayoutConfig.GridTopZ + BoardLayoutConfig.CellSize * 0.52f;
            var yardBottomZ = BoardLayoutConfig.GridBottomZ - BoardLayoutConfig.CellSize * 0.52f;

            BoardGeometry.CreateFlatRect(
                "Parking Yard Top Curb",
                root,
                new Vector3(0f, -0.021f, yardTopZ),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.44f, 0.032f),
                whiteLine);

            BoardGeometry.CreateFlatRect(
                "Parking Yard Bottom Curb",
                root,
                new Vector3(0f, -0.021f, yardBottomZ),
                new Vector2(BoardLayoutConfig.GridWorldWidth + 0.44f, 0.032f),
                whiteLine);

            BoardGeometry.CreateFlatRect(
                "Parking Yard Left Curb",
                root,
                new Vector3(BoardLayoutConfig.GridLeftX - BoardLayoutConfig.CellSize * 0.55f, -0.021f, gridCenter.z),
                new Vector2(0.032f, BoardLayoutConfig.GridWorldDepth + 0.44f),
                whiteLine);

            BoardGeometry.CreateFlatRect(
                "Parking Yard Right Curb",
                root,
                new Vector3(BoardLayoutConfig.GridRightX + BoardLayoutConfig.CellSize * 0.55f, -0.021f, gridCenter.z),
                new Vector2(0.032f, BoardLayoutConfig.GridWorldDepth + 0.44f),
                whiteLine);

            CreateCrosswalk(root, new Vector3(-1.88f, -0.018f, BoardLayoutConfig.GridTopZ + 0.22f), 0.52f, 0.26f, whiteLine);
            CreateCrosswalk(root, new Vector3(1.92f, -0.018f, BoardLayoutConfig.GridTopZ + 0.22f), 0.52f, 0.26f, whiteLine);

            BoardGeometry.CreateFlatRect(
                "Bus Only Yard Label Back",
                root,
                new Vector3(0f, -0.017f, BoardLayoutConfig.GridBottomZ - 0.13f),
                new Vector2(1.02f, 0.13f),
                safety);

            CreateGroundLabel(
                root,
                "Bus Only Yard Label",
                new Vector3(0f, 0.002f, BoardLayoutConfig.GridBottomZ - 0.13f),
                "BUS ONLY",
                TerminalBlue,
                0.034f,
                Quaternion.identity);
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
            ThemeMaterials materials,
            ThemePrefabLibrary prefabLibrary)
        {
            CreateLamp(root, new Vector3(-2.42f, 0f, 2.66f), 0.36f, materials.Pole, materials.LampGlow, materials.LampBulb);
            CreateLamp(root, new Vector3(2.42f, 0f, 2.66f), 0.36f, materials.Pole, materials.LampGlow, materials.LampBulb);

            if (!CreateLibraryBuilding(prefabLibrary, 0, root, "Left Ticket Kiosk Asset", new Vector3(-2.54f, -0.006f, 1.18f), new Vector2(0.36f, 0.58f), 0.34f, Quaternion.Euler(0f, 96f, 0f)))
            {
                CreateSideKiosk(root, "Left Ticket Kiosk", new Vector3(-2.50f, -0.004f, 1.20f), materials.ShopWallA, materials.ShopAwningB, -1);
            }

            if (!CreateLibraryBuilding(prefabLibrary, 1, root, "Right Snack Kiosk Asset", new Vector3(2.54f, -0.006f, 1.20f), new Vector2(0.36f, 0.58f), 0.34f, Quaternion.Euler(0f, -96f, 0f)))
            {
                CreateSideKiosk(root, "Right Snack Kiosk", new Vector3(2.50f, -0.004f, 1.20f), materials.ShopWallC, materials.ShopAwningA, 1);
            }

            CreateLibrarySign(prefabLibrary, 0, root, "Left Parking Sign Asset", new Vector3(-2.34f, -0.006f, BoardLayoutConfig.GridTopZ - 0.10f), 0.26f, Quaternion.Euler(0f, 120f, 0f));
            CreateLibrarySign(prefabLibrary, 1, root, "Right Stop Sign Asset", new Vector3(2.34f, -0.006f, BoardLayoutConfig.GridTopZ - 0.16f), 0.24f, Quaternion.Euler(0f, -120f, 0f));
        }

        private static void CreateRotarySideMargins(
            Transform root,
            RotaryRoadBuildSettings settings,
            ThemeMaterials materials,
            ThemePrefabLibrary prefabLibrary)
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

            if (!CreateLibraryTree(prefabLibrary, 0, root, "Left Rotary Tree Asset", new Vector3(-2.43f, -0.006f, z - 0.48f), 0.30f))
            {
                CreateSmallTree(root, new Vector3(-2.43f, 0f, z - 0.48f), materials.Leaf, materials.Trunk, 0.095f);
            }

            if (!CreateLibraryTree(prefabLibrary, 1, root, "Right Rotary Tree Asset", new Vector3(2.43f, -0.006f, z + 0.48f), 0.30f))
            {
                CreateSmallTree(root, new Vector3(2.43f, 0f, z + 0.48f), materials.Leaf, materials.Trunk, 0.095f);
            }

            CreateLibraryBush(prefabLibrary, 0, root, "Left Rotary Bush Asset", new Vector3(-2.44f, -0.007f, z + 0.38f), 0.22f);
            CreateLibraryBush(prefabLibrary, 0, root, "Right Rotary Bush Asset", new Vector3(2.44f, -0.007f, z - 0.38f), 0.22f);
        }

        private static void CreateEdgeShopSkin(Transform root, ThemeMaterials materials, ThemePrefabLibrary prefabLibrary)
        {
            var createdAssets = 0;
            createdAssets += CreateLibraryBuilding(prefabLibrary, 2, root, "Left Cafe Shop Asset", new Vector3(-2.74f, -0.006f, 2.48f), new Vector2(0.40f, 0.72f), 0.44f, Quaternion.Euler(0f, 84f, 0f)) ? 1 : 0;
            createdAssets += CreateLibraryBuilding(prefabLibrary, 1, root, "Left Market Shop Asset", new Vector3(-2.76f, -0.006f, 3.25f), new Vector2(0.40f, 0.64f), 0.40f, Quaternion.Euler(0f, 98f, 0f)) ? 1 : 0;
            createdAssets += CreateLibraryBuilding(prefabLibrary, 0, root, "Right Ticket Shop Asset", new Vector3(2.74f, -0.006f, 2.38f), new Vector2(0.40f, 0.70f), 0.44f, Quaternion.Euler(0f, -84f, 0f)) ? 1 : 0;
            createdAssets += CreateLibraryBuilding(prefabLibrary, 2, root, "Right Locker Shop Asset", new Vector3(2.76f, -0.006f, 3.12f), new Vector2(0.40f, 0.64f), 0.40f, Quaternion.Euler(0f, -98f, 0f)) ? 1 : 0;

            if (createdAssets >= 4)
            {
                return;
            }

            CreateEdgeShop(root, "Left Cafe Shop", new Vector3(-2.76f, -0.002f, 2.48f), new Vector3(0.22f, 0.20f, 0.64f), materials.ShopWallA, materials.ShopAwningA, materials.ShopWindow, -1);
            CreateEdgeShop(root, "Left Market Shop", new Vector3(-2.76f, -0.002f, 3.25f), new Vector3(0.22f, 0.18f, 0.54f), materials.ShopWallB, materials.ShopAwningB, materials.ShopWindow, -1);
            CreateEdgeShop(root, "Right Ticket Shop", new Vector3(2.76f, -0.002f, 2.38f), new Vector3(0.22f, 0.20f, 0.58f), materials.ShopWallC, materials.ShopAwningC, materials.ShopWindow, 1);
            CreateEdgeShop(root, "Right Locker Shop", new Vector3(2.76f, -0.002f, 3.12f), new Vector3(0.22f, 0.18f, 0.52f), materials.ShopWallA, materials.ShopAwningA, materials.ShopWindow, 1);
        }

        private static bool CreateLibraryBuilding(
            ThemePrefabLibrary library,
            int index,
            Transform root,
            string name,
            Vector3 position,
            Vector2 footprint,
            float height,
            Quaternion rotation)
        {
            if (library == null || !library.TryGetBuilding(index, out var prefab))
            {
                return false;
            }

            return ThemePrefabUtility.InstantiateUniform(prefab, name, root, position, footprint, height, rotation) != null;
        }

        private static bool CreateLibraryTree(
            ThemePrefabLibrary library,
            int index,
            Transform root,
            string name,
            Vector3 position,
            float height)
        {
            if (library == null || !library.TryGetTree(index, out var prefab))
            {
                return false;
            }

            return ThemePrefabUtility.InstantiateUniform(prefab, name, root, position, new Vector2(height * 0.75f, height * 0.75f), height, Quaternion.identity) != null;
        }

        private static bool CreateLibraryBush(
            ThemePrefabLibrary library,
            int index,
            Transform root,
            string name,
            Vector3 position,
            float width)
        {
            if (library == null || !library.TryGetBush(index, out var prefab))
            {
                return false;
            }

            return ThemePrefabUtility.InstantiateUniform(prefab, name, root, position, new Vector2(width, width), width * 0.58f, Quaternion.identity) != null;
        }

        private static bool CreateLibrarySign(
            ThemePrefabLibrary library,
            int index,
            Transform root,
            string name,
            Vector3 position,
            float height,
            Quaternion rotation)
        {
            if (library == null || !library.TryGetSign(index, out var prefab))
            {
                return false;
            }

            return ThemePrefabUtility.InstantiateUniform(prefab, name, root, position, new Vector2(height * 0.60f, height * 0.60f), height, rotation) != null;
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

        private static void CreateSideKiosk(
            Transform root,
            string name,
            Vector3 position,
            Material wall,
            Material awning,
            int side)
        {
            CreateEdgeShop(root, name, position, new Vector3(0.18f, 0.15f, 0.34f), wall, awning, null, side);
        }

        private static void CreateEdgeShop(
            Transform root,
            string name,
            Vector3 groundPosition,
            Vector3 size,
            Material wall,
            Material awning,
            Material window,
            int side)
        {
            var center = groundPosition + Vector3.up * (size.y * 0.5f);
            var inward = side < 0 ? 1f : -1f;
            CreateBox($"{name} Body", root, center, size, wall, Quaternion.identity);
            CreateBox(
                $"{name} Awning",
                root,
                center + new Vector3(inward * size.x * 0.08f, size.y * 0.58f, 0f),
                new Vector3(size.x * 1.12f, 0.034f, size.z * 0.76f),
                awning,
                Quaternion.identity);

            if (window == null)
            {
                return;
            }

            CreateBox(
                $"{name} Window",
                root,
                center + new Vector3(inward * (size.x * 0.52f), size.y * 0.06f, 0f),
                new Vector3(0.018f, size.y * 0.36f, size.z * 0.54f),
                window,
                Quaternion.identity);
        }

        private static void CreateGroundLabel(
            Transform root,
            string name,
            Vector3 position,
            string label,
            Color color,
            float characterSize,
            Quaternion rotation)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(root, false);
            labelObject.transform.SetPositionAndRotation(position, rotation * Quaternion.Euler(90f, 0f, 0f));

            var text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = characterSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            GameFontProvider.ApplyToTextMesh(text, FontStyle.Bold);

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
            const int stripes = 5;
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
            public readonly Material ShopWallA;
            public readonly Material ShopWallB;
            public readonly Material ShopWallC;
            public readonly Material ShopAwningA;
            public readonly Material ShopAwningB;
            public readonly Material ShopAwningC;
            public readonly Material ShopWindow;

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
                Material queueFloor,
                Material shopWallA,
                Material shopWallB,
                Material shopWallC,
                Material shopAwningA,
                Material shopAwningB,
                Material shopAwningC,
                Material shopWindow)
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
                ShopWallA = shopWallA;
                ShopWallB = shopWallB;
                ShopWallC = shopWallC;
                ShopAwningA = shopAwningA;
                ShopAwningB = shopAwningB;
                ShopAwningC = shopAwningC;
                ShopWindow = shopWindow;
            }
        }
    }
}
