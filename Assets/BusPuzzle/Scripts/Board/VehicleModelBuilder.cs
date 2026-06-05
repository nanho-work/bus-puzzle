using UnityEngine;

namespace BusPuzzle
{
    public static class VehicleModelBuilder
    {
        private const float WheelRadiusFactor = 0.055f;
        private const float WheelWidthFactor = 0.035f;

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
            CreateBox("Compact Body", parent, materials.Body, new Vector3(0f, height * 0.34f, centerZ), new Vector3(width * 0.78f, height * 0.42f, length * 0.78f));
            CreateBox("Compact Roof", parent, materials.BodyLight, new Vector3(0f, height * 0.62f, centerZ + length * 0.02f), new Vector3(width * 0.58f, height * 0.22f, length * 0.45f));
            CreateBox("Top Glass", parent, materials.Glass, new Vector3(0f, height * 0.75f, centerZ + length * 0.04f), new Vector3(width * 0.48f, height * 0.030f, length * 0.30f));
            CreateBox("Front Glass", parent, materials.Glass, new Vector3(0f, height * 0.57f, frontZ - length * 0.19f), new Vector3(width * 0.42f, height * 0.030f, length * 0.055f));
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

            CreateBox("Cargo Box", parent, materials.Body, new Vector3(0f, height * 0.52f, boxCenterZ), new Vector3(width * 0.78f, height * 0.74f, length * 0.55f));
            CreateBox("Cargo Top Highlight", parent, materials.BodyLight, new Vector3(0f, height * 0.92f, boxCenterZ), new Vector3(width * 0.66f, height * 0.035f, length * 0.45f));
            CreateBox("Cab Base", parent, materials.BodyDark, new Vector3(0f, height * 0.34f, cabCenterZ), new Vector3(width * 0.70f, height * 0.42f, length * 0.28f));
            CreateBox("Cab Roof", parent, materials.Body, new Vector3(0f, height * 0.64f, cabCenterZ - length * 0.015f), new Vector3(width * 0.60f, height * 0.20f, length * 0.20f));
            CreateBox("Cab Glass", parent, materials.Glass, new Vector3(0f, height * 0.66f, frontZ - length * 0.12f), new Vector3(width * 0.42f, height * 0.030f, length * 0.060f));
            CreateBox("Freezer Unit", parent, materials.DetailLight, new Vector3(0f, height * 0.99f, boxCenterZ + length * 0.19f), new Vector3(width * 0.36f, height * 0.12f, length * 0.070f));
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
            CreateBox("Bus Body", parent, materials.Body, new Vector3(0f, height * 0.42f, centerZ), new Vector3(width * 0.82f, height * 0.56f, length * 0.84f));
            CreateBox("Bus Roof", parent, materials.BodyLight, new Vector3(0f, height * 0.78f, centerZ - length * 0.02f), new Vector3(width * 0.70f, height * 0.10f, length * 0.70f));
            CreateBox("Front Glass", parent, materials.Glass, new Vector3(0f, height * 0.57f, frontZ - length * 0.10f), new Vector3(width * 0.45f, height * 0.035f, length * 0.060f));
            CreateWindowStrip("Left Windows", parent, materials.Glass, -width * 0.42f, height * 0.58f, centerZ - length * 0.03f, width * 0.026f, height * 0.17f, length * 0.56f, 3);
            CreateWindowStrip("Right Windows", parent, materials.Glass, width * 0.42f, height * 0.58f, centerZ - length * 0.03f, width * 0.026f, height * 0.17f, length * 0.56f, 3);
            CreateBox("Door Line", parent, materials.DetailLight, new Vector3(width * 0.26f, height * 0.40f, frontZ - length * 0.24f), new Vector3(width * 0.020f, height * 0.34f, length * 0.018f));
            CreateWheels(parent, materials, width, height, rearZ + length * 0.22f, frontZ - length * 0.20f, cellSize);
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
            public readonly Material DetailLight;

            public VehicleMaterials(PuzzleColor color)
            {
                var bodyColor = PuzzlePalette.ToColor(color);
                Body = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body", bodyColor, 0.58f);
                BodyDark = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body Dark", PuzzlePalette.Darken(bodyColor, 0.16f), 0.50f);
                BodyLight = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body Light", Color.Lerp(bodyColor, Color.white, 0.18f), 0.64f);
                Glass = CreateLitMaterial("Vehicle Glass", new Color(0.09f, 0.19f, 0.30f), 0.78f);
                Wheel = PuzzlePalette.CreateSolidMaterial("Vehicle Wheel", new Color(0.035f, 0.038f, 0.045f));
                DetailLight = PuzzlePalette.CreateSolidMaterial("Vehicle Light Detail", new Color(0.86f, 0.91f, 0.94f));
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
        }
    }
}
