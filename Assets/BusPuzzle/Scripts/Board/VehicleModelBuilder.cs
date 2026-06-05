using UnityEngine;

namespace BusPuzzle
{
    public static class VehicleModelBuilder
    {
        private const float WheelRadiusFactor = 0.055f;
        private const float WheelWidthFactor = 0.035f;
        private const float SideDetailInset = 0.006f;

        public static GameObject Create(
            BusSize size,
            PuzzleColor color,
            Transform parent,
            float visualWidth,
            float visualHeight,
            float visualLength,
            float visualCenterZ,
            float cellSize)
        {
            var root = new GameObject($"{BusSizeUtility.DisplayName(size)} Model");
            root.transform.SetParent(parent, false);

            var materials = new VehicleMaterials(color);
            switch (size)
            {
                case BusSize.Small:
                    CreateCompactVan(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
                case BusSize.Medium:
                    CreateFreezerTruck(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
                case BusSize.Large:
                    CreateBus(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
                default:
                    CreateCompactVan(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
            }

            return root;
        }

        private static void CreateCompactVan(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var rearZ = centerZ - length * 0.5f;
            var frontZ = centerZ + length * 0.5f;

            CreateBox("Compact Lower Body", parent, materials.Body, new Vector3(0f, height * 0.33f, centerZ - length * 0.01f), new Vector3(width * 0.80f, height * 0.42f, length * 0.80f));
            CreateBox("Compact Hood", parent, materials.BodyLight, new Vector3(0f, height * 0.48f, frontZ - length * 0.20f), new Vector3(width * 0.64f, height * 0.13f, length * 0.20f));
            CreateBox("Compact Roof", parent, materials.BodyLight, new Vector3(0f, height * 0.63f, centerZ - length * 0.02f), new Vector3(width * 0.58f, height * 0.23f, length * 0.42f));
            CreateBodyLightLift(parent, materials, "Compact", width, height, centerZ, length, 0.28f, 0.36f, 0.52f);
            CreateBox("Compact Top Glass", parent, materials.Glass, new Vector3(0f, height * 0.765f, centerZ + length * 0.015f), new Vector3(width * 0.48f, height * 0.030f, length * 0.28f));
            CreateBox("Compact Windshield", parent, materials.Glass, new Vector3(0f, height * 0.58f, frontZ - length * 0.18f), new Vector3(width * 0.42f, height * 0.035f, length * 0.062f));
            CreateBox("Compact Rear Glass", parent, materials.Glass, new Vector3(0f, height * 0.53f, rearZ + length * 0.12f), new Vector3(width * 0.35f, height * 0.030f, length * 0.050f));
            CreateSideWindows("Compact Side Windows", parent, materials.Glass, width, height * 0.57f, centerZ - length * 0.03f, length * 0.34f, 2);
            CreateSideStripe("Compact Side Stripe", parent, materials.Trim, width, height * 0.34f, centerZ - length * 0.04f, length * 0.52f);
            CreateBumpers(parent, materials, width, height, rearZ, frontZ, length);
            CreateFrontLights(parent, materials, width, height, frontZ, length);
            CreateTailLights(parent, materials, width, height, rearZ, length);
            CreateWheels(parent, materials, width, height, rearZ + length * 0.24f, frontZ - length * 0.24f, cellSize);
        }

        private static void CreateFreezerTruck(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var rearZ = centerZ - length * 0.5f;
            var frontZ = centerZ + length * 0.5f;
            var cabCenterZ = frontZ - length * 0.20f;
            var boxCenterZ = rearZ + length * 0.33f;

            CreateBox("Cargo Box", parent, materials.Body, new Vector3(0f, height * 0.52f, boxCenterZ), new Vector3(width * 0.80f, height * 0.74f, length * 0.55f));
            CreateBox("Cargo Top Highlight", parent, materials.BodyLight, new Vector3(0f, height * 0.92f, boxCenterZ), new Vector3(width * 0.68f, height * 0.038f, length * 0.45f));
            CreateBodyLightLift(parent, materials, "Truck Cargo", width, height, boxCenterZ, length, 0.32f, 0.46f, 0.44f);
            CreateBox("Cargo Side Rail Left", parent, materials.Trim, new Vector3(-width * 0.415f, height * 0.66f, boxCenterZ), new Vector3(width * 0.020f, height * 0.060f, length * 0.42f));
            CreateBox("Cargo Side Rail Right", parent, materials.Trim, new Vector3(width * 0.415f, height * 0.66f, boxCenterZ), new Vector3(width * 0.020f, height * 0.060f, length * 0.42f));
            CreateBox("Cargo Rear Door Seam", parent, materials.Trim, new Vector3(0f, height * 0.54f, rearZ + length * 0.07f), new Vector3(width * 0.58f, height * 0.035f, length * 0.020f));
            CreateBox("Cab Base", parent, materials.BodyDark, new Vector3(0f, height * 0.34f, cabCenterZ), new Vector3(width * 0.70f, height * 0.42f, length * 0.28f));
            CreateBox("Cab Roof", parent, materials.Body, new Vector3(0f, height * 0.64f, cabCenterZ - length * 0.015f), new Vector3(width * 0.60f, height * 0.20f, length * 0.20f));
            CreateBodyLightLift(parent, materials, "Truck Cab", width, height, cabCenterZ, length, 0.28f, 0.30f, 0.20f);
            CreateBox("Cab Windshield", parent, materials.Glass, new Vector3(0f, height * 0.66f, frontZ - length * 0.12f), new Vector3(width * 0.42f, height * 0.034f, length * 0.060f));
            CreateSideWindows("Cab Side Windows", parent, materials.Glass, width, height * 0.53f, cabCenterZ + length * 0.015f, length * 0.16f, 1);
            CreateBox("Freezer Unit", parent, materials.DetailLight, new Vector3(0f, height * 0.99f, boxCenterZ + length * 0.19f), new Vector3(width * 0.36f, height * 0.12f, length * 0.070f));
            CreateBox("Freezer Vent Slat 1", parent, materials.Bumper, new Vector3(0f, height * 1.01f, boxCenterZ + length * 0.19f), new Vector3(width * 0.25f, height * 0.018f, length * 0.012f));
            CreateBox("Freezer Vent Slat 2", parent, materials.Bumper, new Vector3(0f, height * 0.965f, boxCenterZ + length * 0.19f), new Vector3(width * 0.25f, height * 0.018f, length * 0.012f));
            CreateSideStripe("Truck Side Stripe", parent, materials.Trim, width, height * 0.31f, boxCenterZ, length * 0.44f);
            CreateBumpers(parent, materials, width, height, rearZ, frontZ, length);
            CreateFrontLights(parent, materials, width, height, frontZ, length);
            CreateTailLights(parent, materials, width, height, rearZ, length);
            CreateWheels(parent, materials, width, height, rearZ + length * 0.22f, frontZ - length * 0.20f, cellSize);
        }

        private static void CreateBus(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var rearZ = centerZ - length * 0.5f;
            var frontZ = centerZ + length * 0.5f;
            CreateBox("Bus Lower Body", parent, materials.Body, new Vector3(0f, height * 0.39f, centerZ), new Vector3(width * 0.84f, height * 0.54f, length * 0.86f));
            CreateBox("Bus Upper Body", parent, materials.BodyLight, new Vector3(0f, height * 0.68f, centerZ - length * 0.02f), new Vector3(width * 0.76f, height * 0.24f, length * 0.76f));
            CreateBox("Bus Roof Cap", parent, materials.BodyLight, new Vector3(0f, height * 0.84f, centerZ - length * 0.03f), new Vector3(width * 0.64f, height * 0.070f, length * 0.66f));
            CreateBodyLightLift(parent, materials, "Bus", width, height, centerZ, length, 0.28f, 0.44f, 0.70f);
            CreateBox("Destination Sign", parent, materials.Bumper, new Vector3(0f, height * 0.70f, frontZ - length * 0.13f), new Vector3(width * 0.39f, height * 0.045f, length * 0.038f));
            CreateBox("Bus Windshield", parent, materials.Glass, new Vector3(0f, height * 0.58f, frontZ - length * 0.105f), new Vector3(width * 0.46f, height * 0.040f, length * 0.064f));
            CreateBox("Rear Window", parent, materials.Glass, new Vector3(0f, height * 0.57f, rearZ + length * 0.095f), new Vector3(width * 0.38f, height * 0.032f, length * 0.050f));
            CreateWindowStrip("Left Windows", parent, materials.Glass, -width * 0.43f, height * 0.61f, centerZ - length * 0.04f, width * 0.028f, height * 0.16f, length * 0.62f, 5);
            CreateWindowStrip("Right Windows", parent, materials.Glass, width * 0.43f, height * 0.61f, centerZ - length * 0.04f, width * 0.028f, height * 0.16f, length * 0.62f, 5);
            CreateBox("Front Door Line", parent, materials.DetailLight, new Vector3(width * 0.30f, height * 0.43f, frontZ - length * 0.24f), new Vector3(width * 0.018f, height * 0.34f, length * 0.016f));
            CreateBox("Rear Door Line", parent, materials.DetailLight, new Vector3(width * 0.30f, height * 0.43f, centerZ - length * 0.08f), new Vector3(width * 0.016f, height * 0.30f, length * 0.014f));
            CreateSideStripe("Bus Belt Line", parent, materials.Trim, width, height * 0.35f, centerZ - length * 0.03f, length * 0.72f);
            CreateBox("Bus Lower Skirt", parent, materials.BodyDark, new Vector3(0f, height * 0.20f, centerZ), new Vector3(width * 0.84f, height * 0.070f, length * 0.76f));
            CreateBumpers(parent, materials, width, height, rearZ, frontZ, length);
            CreateFrontLights(parent, materials, width, height, frontZ, length);
            CreateTailLights(parent, materials, width, height, rearZ, length);
            CreateWheels(parent, materials, width, height, rearZ + length * 0.22f, frontZ - length * 0.20f, cellSize);
        }

        private static void CreateSideWindows(
            string name,
            Transform parent,
            Material material,
            float width,
            float y,
            float centerZ,
            float totalLength,
            int count)
        {
            CreateWindowStrip($"{name} Left", parent, material, -width * 0.42f, y, centerZ, width * 0.024f, width * 0.21f, totalLength, count);
            CreateWindowStrip($"{name} Right", parent, material, width * 0.42f, y, centerZ, width * 0.024f, width * 0.21f, totalLength, count);
        }

        private static void CreateWindowStrip(
            string name,
            Transform parent,
            Material material,
            float x,
            float y,
            float centerZ,
            float width,
            float height,
            float totalLength,
            int count)
        {
            var spacing = totalLength / count;
            for (var index = 0; index < count; index++)
            {
                var z = centerZ - totalLength * 0.5f + spacing * (index + 0.5f);
                CreateBox($"{name} {index + 1}", parent, material, new Vector3(x, y, z), new Vector3(width, height, spacing * 0.72f));
            }
        }

        private static void CreateSideStripe(
            string name,
            Transform parent,
            Material material,
            float width,
            float y,
            float centerZ,
            float totalLength)
        {
            CreateBox($"{name} Left", parent, material, new Vector3(-width * 0.425f - SideDetailInset, y, centerZ), new Vector3(width * 0.018f, width * 0.060f, totalLength));
            CreateBox($"{name} Right", parent, material, new Vector3(width * 0.425f + SideDetailInset, y, centerZ), new Vector3(width * 0.018f, width * 0.060f, totalLength));
        }

        private static void CreateBodyLightLift(
            Transform parent,
            VehicleMaterials materials,
            string name,
            float width,
            float height,
            float centerZ,
            float length,
            float bottomYFactor,
            float heightFactor,
            float lengthFactor)
        {
            var sideX = width * 0.432f + SideDetailInset * 1.5f;
            var lowerY = height * (bottomYFactor + heightFactor * 0.32f);
            var upperY = height * (bottomYFactor + heightFactor * 0.70f);
            var liftLength = length * lengthFactor;
            CreateBox(
                $"{name} Left Lower Light Lift",
                parent,
                materials.BodyLift,
                new Vector3(-sideX, lowerY, centerZ + length * 0.02f),
                new Vector3(width * 0.014f, height * heightFactor * 0.58f, liftLength));
            CreateBox(
                $"{name} Right Lower Light Lift",
                parent,
                materials.BodyLift,
                new Vector3(sideX, lowerY, centerZ + length * 0.02f),
                new Vector3(width * 0.014f, height * heightFactor * 0.58f, liftLength));
            CreateBox(
                $"{name} Left Upper Light Fade",
                parent,
                materials.BodyLiftSoft,
                new Vector3(-sideX - width * 0.002f, upperY, centerZ + length * 0.03f),
                new Vector3(width * 0.010f, height * heightFactor * 0.34f, liftLength * 0.82f));
            CreateBox(
                $"{name} Right Upper Light Fade",
                parent,
                materials.BodyLiftSoft,
                new Vector3(sideX + width * 0.002f, upperY, centerZ + length * 0.03f),
                new Vector3(width * 0.010f, height * heightFactor * 0.34f, liftLength * 0.82f));
        }

        private static void CreateBumpers(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float rearZ,
            float frontZ,
            float length)
        {
            CreateBox("Front Bumper", parent, materials.Bumper, new Vector3(0f, height * 0.245f, frontZ - length * 0.045f), new Vector3(width * 0.58f, height * 0.070f, length * 0.030f));
            CreateBox("Rear Bumper", parent, materials.Bumper, new Vector3(0f, height * 0.245f, rearZ + length * 0.045f), new Vector3(width * 0.54f, height * 0.060f, length * 0.028f));
        }

        private static void CreateFrontLights(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float frontZ,
            float length)
        {
            CreateBox("Left Headlight", parent, materials.HeadLight, new Vector3(-width * 0.22f, height * 0.34f, frontZ - length * 0.062f), new Vector3(width * 0.105f, height * 0.055f, length * 0.020f));
            CreateBox("Right Headlight", parent, materials.HeadLight, new Vector3(width * 0.22f, height * 0.34f, frontZ - length * 0.062f), new Vector3(width * 0.105f, height * 0.055f, length * 0.020f));
        }

        private static void CreateTailLights(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float rearZ,
            float length)
        {
            CreateBox("Left Tail Light", parent, materials.TailLight, new Vector3(-width * 0.28f, height * 0.35f, rearZ + length * 0.055f), new Vector3(width * 0.060f, height * 0.080f, length * 0.020f));
            CreateBox("Right Tail Light", parent, materials.TailLight, new Vector3(width * 0.28f, height * 0.35f, rearZ + length * 0.055f), new Vector3(width * 0.060f, height * 0.080f, length * 0.020f));
        }

        private static void CreateWheels(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float rearZ,
            float frontZ,
            float cellSize)
        {
            var wheelRadius = cellSize * WheelRadiusFactor;
            var wheelWidth = cellSize * WheelWidthFactor;
            var x = width * 0.34f;
            var y = height * 0.105f;
            CreateWheel("Rear Left Wheel", parent, materials.Wheel, new Vector3(-x, y, rearZ), wheelRadius, wheelWidth);
            CreateWheel("Rear Right Wheel", parent, materials.Wheel, new Vector3(x, y, rearZ), wheelRadius, wheelWidth);
            CreateWheel("Front Left Wheel", parent, materials.Wheel, new Vector3(-x, y, frontZ), wheelRadius, wheelWidth);
            CreateWheel("Front Right Wheel", parent, materials.Wheel, new Vector3(x, y, frontZ), wheelRadius, wheelWidth);
            CreateWheel("Rear Left Hub", parent, materials.WheelHub, new Vector3(-x, y, rearZ), wheelRadius * 0.44f, wheelWidth * 1.08f);
            CreateWheel("Rear Right Hub", parent, materials.WheelHub, new Vector3(x, y, rearZ), wheelRadius * 0.44f, wheelWidth * 1.08f);
            CreateWheel("Front Left Hub", parent, materials.WheelHub, new Vector3(-x, y, frontZ), wheelRadius * 0.44f, wheelWidth * 1.08f);
            CreateWheel("Front Right Hub", parent, materials.WheelHub, new Vector3(x, y, frontZ), wheelRadius * 0.44f, wheelWidth * 1.08f);
        }

        private static GameObject CreateBox(string name, Transform parent, Material material, Vector3 localPosition, Vector3 localScale)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            var renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return box;
        }

        private static void CreateWheel(string name, Transform parent, Material material, Vector3 localPosition, float radius, float width)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(width, radius, radius);
            var renderer = wheel.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private sealed class VehicleMaterials
        {
            public readonly Material Body;
            public readonly Material BodyDark;
            public readonly Material BodyLight;
            public readonly Material Glass;
            public readonly Material Wheel;
            public readonly Material WheelHub;
            public readonly Material DetailLight;
            public readonly Material Bumper;
            public readonly Material HeadLight;
            public readonly Material TailLight;
            public readonly Material Trim;
            public readonly Material BodyLift;
            public readonly Material BodyLiftSoft;

            public VehicleMaterials(PuzzleColor color)
            {
                var bodyColor = PuzzlePalette.ToColor(color);
                Body = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body", bodyColor, 0.72f);
                BodyDark = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body Dark", PuzzlePalette.Darken(bodyColor, 0.16f), 0.62f);
                BodyLight = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body Light", Color.Lerp(bodyColor, Color.white, 0.18f), 0.78f);
                Glass = CreateLitMaterial("Vehicle Glass", new Color(0.09f, 0.19f, 0.30f), 0.88f);
                Wheel = PuzzlePalette.CreateSolidMaterial("Vehicle Wheel", new Color(0.035f, 0.038f, 0.045f));
                WheelHub = PuzzlePalette.CreateSolidMaterial("Vehicle Wheel Hub", new Color(0.72f, 0.76f, 0.78f));
                DetailLight = PuzzlePalette.CreateSolidMaterial("Vehicle Light Detail", new Color(0.86f, 0.91f, 0.94f));
                Bumper = PuzzlePalette.CreateSolidMaterial("Vehicle Bumper", new Color(0.13f, 0.15f, 0.18f));
                HeadLight = PuzzlePalette.CreateSolidMaterial("Vehicle Headlight", new Color(1.00f, 0.94f, 0.64f));
                TailLight = PuzzlePalette.CreateSolidMaterial("Vehicle Tail Light", new Color(0.90f, 0.12f, 0.10f));
                Trim = PuzzlePalette.CreateSolidMaterial("Vehicle Trim", Color.Lerp(bodyColor, Color.white, 0.40f));
                BodyLift = PuzzlePalette.CreateTransparentMaterial("Vehicle Body Light Lift", WithAlpha(Color.Lerp(bodyColor, Color.white, 0.30f), 0.18f));
                BodyLiftSoft = PuzzlePalette.CreateTransparentMaterial("Vehicle Body Soft Light Lift", WithAlpha(Color.Lerp(bodyColor, Color.white, 0.42f), 0.10f));
            }

            private static Material CreateLitMaterial(string name, Color color, float smoothness)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Unlit/Color");
                var material = new Material(shader) { name = name, color = color };
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", smoothness);
                }

                if (material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", smoothness);
                }

                return material;
            }

            private static Color WithAlpha(Color color, float alpha)
            {
                return new Color(color.r, color.g, color.b, alpha);
            }
        }
    }
}
