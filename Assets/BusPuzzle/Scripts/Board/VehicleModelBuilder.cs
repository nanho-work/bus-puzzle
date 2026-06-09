using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BusPuzzle
{
    public static class VehicleModelBuilder
    {
        private const int CornerSegments = 5;
        private const float DetailLift = 0.006f;
        private static readonly int[] BoxTriangles =
        {
            0, 4, 5, 0, 5, 1,
            3, 2, 6, 3, 6, 7,
            0, 3, 7, 0, 7, 4,
            1, 5, 6, 1, 6, 2,
            4, 7, 6, 4, 6, 5,
            0, 1, 2, 0, 2, 3,
        };
        private static readonly int[] DoubleSidedQuadTriangles =
        {
            0, 1, 2, 0, 2, 3,
            0, 2, 1, 0, 3, 2,
        };

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
                    CreateCompactCar(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
                case BusSize.Medium:
                    CreateShuttleVan(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
                case BusSize.Large:
                    CreateCityBus(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
                default:
                    CreateCompactCar(root.transform, materials, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
                    break;
            }

            return root;
        }

        public static GameObject CreateSilhouette(
            BusSize size,
            Transform parent,
            float visualWidth,
            float visualHeight,
            float visualLength,
            float visualCenterZ,
            float cellSize)
        {
            var root = new GameObject($"{BusSizeUtility.DisplayName(size)} Mystery Model");
            root.transform.SetParent(parent, false);

            var materials = new SilhouetteMaterials();
            CreateMysteryVehicle(root.transform, materials, size, visualWidth, visualHeight, visualLength, visualCenterZ, cellSize);
            return root;
        }

        private static void CreateMysteryVehicle(
            Transform parent,
            SilhouetteMaterials materials,
            BusSize size,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var bodyWidth = width * (size == BusSize.Large ? 1.02f : 0.99f);
            var bodyLength = length * 0.95f;
            var bodyBottom = height * 0.10f;
            var bodyHeight = height * (size == BusSize.Small ? 0.48f : 0.56f);
            var cornerRadius = width * (size == BusSize.Large ? 0.15f : 0.18f);

            CreateRoundedPrism("Mystery Outer Rim", parent, materials.Rim, bodyWidth * 1.08f, bodyLength * 1.05f, bodyBottom - height * 0.022f, bodyHeight * 0.63f, centerZ, cornerRadius * 1.10f);
            CreateRoundedPrism("Mystery Body", parent, materials.Body, bodyWidth, bodyLength, bodyBottom, bodyHeight, centerZ, cornerRadius);
            CreateRoundedPrism("Mystery Roof Mass", parent, materials.Top, width * 0.76f, length * 0.54f, bodyBottom + bodyHeight * 0.62f, height * 0.20f, centerZ - length * 0.02f, width * 0.10f);
            CreateRoundedPrism("Mystery Front Lip", parent, materials.RimLight, width * 0.56f, length * 0.062f, bodyBottom + bodyHeight * 0.48f, height * 0.050f, centerZ + bodyLength * 0.47f, width * 0.035f);
            CreateRoundedPrism("Mystery Rear Shade", parent, materials.Shade, width * 0.58f, length * 0.050f, bodyBottom + height * 0.14f, height * 0.036f, centerZ - bodyLength * 0.48f, width * 0.030f);
            CreateMysteryWheels(parent, materials, width, height, bodyLength, centerZ, cellSize);
        }

        private static void CreateCompactCar(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var bodyWidth = width * 1.04f;
            var bodyLength = length * 0.84f;
            var bodyBottom = height * 0.08f;
            var lowerTop = height * 0.42f;
            var cabinTop = height * 0.80f;
            var frontZ = centerZ + bodyLength * 0.47f;
            var rearZ = centerZ - bodyLength * 0.47f;
            var sideX = bodyWidth * 0.51f;

            CreateTaperedBox("Compact SUV Chassis Shadow", parent, materials.Outline, bodyWidth * 1.08f, bodyWidth * 1.03f, bodyWidth * 0.96f, bodyWidth * 0.90f, bodyLength * 1.03f, bodyBottom - height * 0.022f, bodyBottom + height * 0.19f, centerZ, width * 0.02f);
            CreateTaperedBox("Compact SUV Lower Cladding", parent, materials.Outline, bodyWidth * 1.02f, bodyWidth * 1.00f, bodyWidth * 0.94f, bodyWidth * 0.90f, bodyLength * 0.95f, bodyBottom, bodyBottom + height * 0.18f, centerZ, width * 0.01f);
            CreateTaperedBox("Compact SUV Main Body", parent, materials.Body, bodyWidth, bodyWidth * 0.98f, bodyWidth * 0.93f, bodyWidth * 0.87f, bodyLength, bodyBottom + height * 0.11f, lowerTop, centerZ, width * 0.02f);
            CreateTaperedCabin("Compact SUV Cabin Shell", parent, materials.BodyLight, width * 0.76f, width * 0.62f, lowerTop - height * 0.03f, cabinTop, centerZ - length * 0.24f, centerZ + length * 0.18f, centerZ - length * 0.26f, centerZ + length * 0.06f);

            CreateSlopedQuad("Compact SUV Windshield", parent, materials.Glass,
                new Vector3(-width * 0.31f, lowerTop + height * 0.005f, centerZ + length * 0.18f),
                new Vector3(width * 0.31f, lowerTop + height * 0.005f, centerZ + length * 0.18f),
                new Vector3(width * 0.25f, cabinTop + height * 0.010f, centerZ + length * 0.06f),
                new Vector3(-width * 0.25f, cabinTop + height * 0.010f, centerZ + length * 0.06f));
            CreateSlopedQuad("Compact SUV Rear Hatch Glass", parent, materials.Glass,
                new Vector3(width * 0.29f, lowerTop + height * 0.02f, rearZ - length * 0.012f),
                new Vector3(-width * 0.29f, lowerTop + height * 0.02f, rearZ - length * 0.012f),
                new Vector3(-width * 0.25f, cabinTop - height * 0.05f, rearZ - length * 0.020f),
                new Vector3(width * 0.25f, cabinTop - height * 0.05f, rearZ - length * 0.020f));
            CreateSlopedQuad("Compact SUV Roof Glass", parent, materials.Glass,
                new Vector3(-width * 0.24f, cabinTop + height * 0.014f, centerZ + length * 0.015f),
                new Vector3(width * 0.24f, cabinTop + height * 0.014f, centerZ + length * 0.015f),
                new Vector3(width * 0.25f, cabinTop + height * 0.014f, centerZ - length * 0.19f),
                new Vector3(-width * 0.25f, cabinTop + height * 0.014f, centerZ - length * 0.19f));

            CreateSideWindowSet(parent, materials, -sideX, width, height, length, centerZ, lowerTop, cabinTop);
            CreateSideWindowSet(parent, materials, sideX, width, height, length, centerZ, lowerTop, cabinTop);
            CreateSideCladdingSet(parent, materials, -sideX, width, height, length, centerZ, lowerTop);
            CreateSideCladdingSet(parent, materials, sideX, width, height, length, centerZ, lowerTop);

            CreateVerticalPanel("Compact SUV Front Black Face", parent, materials.Outline, Vector3.forward, 0f, bodyBottom + height * 0.33f, frontZ + length * 0.006f, width * 0.73f, height * 0.26f);
            CreateVerticalPanel("Compact SUV Upper Light Bar", parent, materials.HeadLight, Vector3.forward, 0f, bodyBottom + height * 0.48f, frontZ + length * 0.012f, width * 0.58f, height * 0.035f);
            CreateCircleDisc("Compact SUV Left Lamp Ring", parent, materials.Bumper, Vector3.forward, -width * 0.255f, bodyBottom + height * 0.34f, frontZ + length * 0.020f, width * 0.098f, height * 0.068f, 28);
            CreateCircleDisc("Compact SUV Right Lamp Ring", parent, materials.Bumper, Vector3.forward, width * 0.255f, bodyBottom + height * 0.34f, frontZ + length * 0.020f, width * 0.098f, height * 0.068f, 28);
            CreateCircleDisc("Compact SUV Left Round Lamp", parent, materials.HeadLight, Vector3.forward, -width * 0.255f, bodyBottom + height * 0.34f, frontZ + length * 0.024f, width * 0.060f, height * 0.042f, 24);
            CreateCircleDisc("Compact SUV Right Round Lamp", parent, materials.HeadLight, Vector3.forward, width * 0.255f, bodyBottom + height * 0.34f, frontZ + length * 0.024f, width * 0.060f, height * 0.042f, 24);
            CreateVerticalPanel("Compact SUV Silver Front Skid", parent, materials.WheelHub, Vector3.forward, 0f, bodyBottom + height * 0.21f, frontZ + length * 0.021f, width * 0.50f, height * 0.060f);

            CreateVerticalPanel("Compact SUV Rear Glass Face", parent, materials.Glass, Vector3.back, 0f, lowerTop + height * 0.12f, rearZ - length * 0.026f, width * 0.55f, height * 0.16f);
            CreateVerticalPanel("Compact SUV Left Tail Lamp", parent, materials.TailLight, Vector3.back, -width * 0.35f, lowerTop + height * 0.05f, rearZ - length * 0.028f, width * 0.070f, height * 0.14f);
            CreateVerticalPanel("Compact SUV Right Tail Lamp", parent, materials.TailLight, Vector3.back, width * 0.35f, lowerTop + height * 0.05f, rearZ - length * 0.028f, width * 0.070f, height * 0.14f);

            CreateRoundedPrism("Compact SUV Front Left Arch Cap", parent, materials.Outline, width * 0.23f, length * 0.17f, lowerTop + height * 0.010f, height * 0.025f, centerZ + bodyLength * 0.30f, width * 0.040f, -bodyWidth * 0.43f);
            CreateRoundedPrism("Compact SUV Front Right Arch Cap", parent, materials.Outline, width * 0.23f, length * 0.17f, lowerTop + height * 0.010f, height * 0.025f, centerZ + bodyLength * 0.30f, width * 0.040f, bodyWidth * 0.43f);
            CreateRoundedPrism("Compact SUV Rear Left Arch Cap", parent, materials.Outline, width * 0.23f, length * 0.17f, lowerTop + height * 0.010f, height * 0.025f, centerZ - bodyLength * 0.30f, width * 0.040f, -bodyWidth * 0.43f);
            CreateRoundedPrism("Compact SUV Rear Right Arch Cap", parent, materials.Outline, width * 0.23f, length * 0.17f, lowerTop + height * 0.010f, height * 0.025f, centerZ - bodyLength * 0.30f, width * 0.040f, bodyWidth * 0.43f);
            CreateWheels(parent, materials, width, height, bodyLength, centerZ, cellSize, 0.30f);
        }

        private static void CreateShuttleVan(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var bodyWidth = width * 1.00f;
            var bodyLength = length * 0.95f;
            var bodyBottom = height * 0.10f;
            var bodyHeight = height * 0.54f;

            CreateRoundedPrism("Van Chassis Shadow", parent, materials.Outline, bodyWidth * 1.07f, bodyLength * 1.04f, bodyBottom - height * 0.02f, bodyHeight * 0.62f, centerZ, width * 0.20f);
            CreateRoundedPrism("Van Body", parent, materials.Body, bodyWidth, bodyLength, bodyBottom, bodyHeight, centerZ, width * 0.18f);
            CreateRoundedPrism("Van Cargo Top", parent, materials.BodyLight, width * 0.78f, length * 0.55f, bodyBottom + bodyHeight + DetailLift, height * 0.038f, centerZ - length * 0.08f, width * 0.10f);
            CreateRoundedPrism("Van Cabin Glass", parent, materials.Glass, width * 0.58f, length * 0.26f, bodyBottom + bodyHeight * 0.80f, height * 0.25f, centerZ + length * 0.30f, width * 0.10f);
            CreateWindowPanels(parent, materials, width, length, centerZ - length * 0.10f, bodyBottom + bodyHeight + height * 0.030f, 2);
            CreateSideWindowPanels(parent, materials, bodyWidth, length * 0.50f, centerZ - length * 0.06f, bodyBottom + bodyHeight * 0.62f, height * 0.16f, 2);
            CreateBumpers(parent, materials, width, length, centerZ, bodyBottom + height * 0.13f, bodyLength);
            CreateLights(parent, materials, width, length, centerZ, bodyBottom + height * 0.24f, bodyLength, true);
            CreateWheels(parent, materials, width, height, bodyLength, centerZ, cellSize, 0.30f);
        }

        private static void CreateCityBus(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var bodyWidth = width * 1.02f;
            var bodyLength = length * 0.96f;
            var bodyBottom = height * 0.10f;
            var bodyHeight = height * 0.58f;

            CreateRoundedPrism("Bus Chassis Shadow", parent, materials.Outline, bodyWidth * 1.07f, bodyLength * 1.035f, bodyBottom - height * 0.02f, bodyHeight * 0.60f, centerZ, width * 0.17f);
            CreateRoundedPrism("Bus Body", parent, materials.Body, bodyWidth, bodyLength, bodyBottom, bodyHeight, centerZ, width * 0.15f);
            CreateRoundedPrism("Bus Upper Body", parent, materials.BodyLight, width * 0.83f, length * 0.78f, bodyBottom + bodyHeight * 0.58f, height * 0.25f, centerZ - length * 0.03f, width * 0.11f);
            CreateRoundedPrism("Bus Windshield", parent, materials.Glass, width * 0.62f, length * 0.18f, bodyBottom + bodyHeight * 0.82f, height * 0.15f, centerZ + length * 0.36f, width * 0.08f);
            CreateWindowPanels(parent, materials, width, length, centerZ - length * 0.12f, bodyBottom + bodyHeight + height * 0.030f, 4);
            CreateSideWindowPanels(parent, materials, bodyWidth, length * 0.62f, centerZ - length * 0.10f, bodyBottom + bodyHeight * 0.64f, height * 0.15f, 4);
            CreateBumpers(parent, materials, width, length, centerZ, bodyBottom + height * 0.13f, bodyLength);
            CreateLights(parent, materials, width, length, centerZ, bodyBottom + height * 0.24f, bodyLength, true);
            CreateWheels(parent, materials, width, height, bodyLength, centerZ, cellSize, 0.31f);
        }

        private static void CreateWindowPanels(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float length,
            float centerZ,
            float topY,
            int count)
        {
            var totalLength = length * 0.44f;
            var spacing = totalLength / Mathf.Max(1, count);
            for (var index = 0; index < count; index++)
            {
                var z = centerZ - totalLength * 0.5f + spacing * (index + 0.5f);
                CreateRoundedPrism(
                    $"Window Panel {index + 1}",
                    parent,
                    materials.Glass,
                    width * 0.38f,
                    spacing * 0.58f,
                    topY,
                    length * 0.018f,
                    z,
                    width * 0.045f);
            }
        }

        private static void CreateBumpers(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float length,
            float centerZ,
            float y,
            float bodyLength)
        {
            CreateRoundedPrism("Front Bumper", parent, materials.Bumper, width * 0.62f, length * 0.048f, y, length * 0.020f, centerZ + bodyLength * 0.49f, width * 0.035f);
            CreateRoundedPrism("Rear Bumper", parent, materials.Bumper, width * 0.56f, length * 0.044f, y, length * 0.018f, centerZ - bodyLength * 0.49f, width * 0.035f);
        }

        private static void CreateSideWindowPanels(
            Transform parent,
            VehicleMaterials materials,
            float bodyWidth,
            float totalLength,
            float centerZ,
            float centerY,
            float panelHeight,
            int count)
        {
            var spacing = totalLength / Mathf.Max(1, count);
            var panelLength = spacing * 0.64f;
            var sideX = bodyWidth * 0.505f;
            for (var index = 0; index < count; index++)
            {
                var z = centerZ - totalLength * 0.5f + spacing * (index + 0.5f);
                CreateSideWindowPanel($"Left Side Window {index + 1}", parent, materials.Glass, -sideX, centerY, z, bodyWidth, panelHeight, panelLength);
                CreateSideWindowPanel($"Right Side Window {index + 1}", parent, materials.Glass, sideX, centerY, z, bodyWidth, panelHeight, panelLength);
            }
        }

        private static void CreateSideWindowPanel(
            string name,
            Transform parent,
            Material material,
            float x,
            float y,
            float z,
            float bodyWidth,
            float height,
            float length)
        {
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = name;
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = new Vector3(x, y, z);
            panel.transform.localScale = new Vector3(bodyWidth * 0.030f, height, length);
            ConfigureRenderer(panel, material);
        }

        private static void CreateSideWindowSet(
            Transform parent,
            VehicleMaterials materials,
            float sideX,
            float width,
            float height,
            float length,
            float centerZ,
            float lowerTop,
            float cabinTop)
        {
            var xSign = Mathf.Sign(sideX);
            var x = sideX + xSign * width * 0.004f;
            var yBottom = lowerTop + height * 0.035f;
            var yTop = cabinTop - height * 0.065f;
            CreateSlopedQuad(
                sideX < 0f ? "Compact SUV Left Front Window" : "Compact SUV Right Front Window",
                parent,
                materials.Glass,
                new Vector3(x, yBottom, centerZ + length * 0.145f),
                new Vector3(x, yBottom, centerZ + length * 0.035f),
                new Vector3(x, yTop, centerZ + length * 0.015f),
                new Vector3(x, yTop - height * 0.012f, centerZ + length * 0.120f));
            CreateSlopedQuad(
                sideX < 0f ? "Compact SUV Left Rear Window" : "Compact SUV Right Rear Window",
                parent,
                materials.Glass,
                new Vector3(x, yBottom, centerZ + length * 0.005f),
                new Vector3(x, yBottom, centerZ - length * 0.145f),
                new Vector3(x, yTop - height * 0.020f, centerZ - length * 0.170f),
                new Vector3(x, yTop, centerZ - length * 0.010f));
            CreateSlopedQuad(
                sideX < 0f ? "Compact SUV Left Quarter Window" : "Compact SUV Right Quarter Window",
                parent,
                materials.Glass,
                new Vector3(x, yBottom + height * 0.010f, centerZ - length * 0.160f),
                new Vector3(x, yBottom + height * 0.010f, centerZ - length * 0.250f),
                new Vector3(x, yTop - height * 0.035f, centerZ - length * 0.240f),
                new Vector3(x, yTop - height * 0.020f, centerZ - length * 0.175f));
            CreateCubeDetail(
                sideX < 0f ? "Compact SUV Left B Pillar" : "Compact SUV Right B Pillar",
                parent,
                materials.Outline,
                new Vector3(x, (yBottom + yTop) * 0.5f, centerZ + length * 0.018f),
                new Vector3(width * 0.022f, yTop - yBottom + height * 0.010f, length * 0.020f));
        }

        private static void CreateSideCladdingSet(
            Transform parent,
            VehicleMaterials materials,
            float sideX,
            float width,
            float height,
            float length,
            float centerZ,
            float lowerTop)
        {
            var xSign = Mathf.Sign(sideX);
            var x = sideX + xSign * width * 0.004f;
            CreateCubeDetail(
                sideX < 0f ? "Compact SUV Left Black Belt" : "Compact SUV Right Black Belt",
                parent,
                materials.Outline,
                new Vector3(x, lowerTop - height * 0.065f, centerZ - length * 0.045f),
                new Vector3(width * 0.030f, height * 0.045f, length * 0.53f));
            CreateCubeDetail(
                sideX < 0f ? "Compact SUV Left Front Handle" : "Compact SUV Right Front Handle",
                parent,
                materials.Bumper,
                new Vector3(x, lowerTop - height * 0.020f, centerZ + length * 0.075f),
                new Vector3(width * 0.020f, height * 0.022f, length * 0.050f));
            CreateCubeDetail(
                sideX < 0f ? "Compact SUV Left Rear Handle" : "Compact SUV Right Rear Handle",
                parent,
                materials.Bumper,
                new Vector3(x, lowerTop - height * 0.020f, centerZ - length * 0.105f),
                new Vector3(width * 0.020f, height * 0.022f, length * 0.050f));
        }

        private static void CreateTaperedBox(
            string name,
            Transform parent,
            Material material,
            float bottomRearWidth,
            float bottomFrontWidth,
            float topRearWidth,
            float topFrontWidth,
            float length,
            float bottomY,
            float topY,
            float centerZ,
            float frontInset)
        {
            var rearZ = centerZ - length * 0.5f;
            var frontZ = centerZ + length * 0.5f - frontInset;
            var vertices = new[]
            {
                new Vector3(-bottomRearWidth * 0.5f, bottomY, rearZ),
                new Vector3(bottomRearWidth * 0.5f, bottomY, rearZ),
                new Vector3(bottomFrontWidth * 0.5f, bottomY, frontZ),
                new Vector3(-bottomFrontWidth * 0.5f, bottomY, frontZ),
                new Vector3(-topRearWidth * 0.5f, topY, rearZ),
                new Vector3(topRearWidth * 0.5f, topY, rearZ),
                new Vector3(topFrontWidth * 0.5f, topY, frontZ),
                new Vector3(-topFrontWidth * 0.5f, topY, frontZ),
            };
            CreateMeshDetail(name, parent, material, vertices, BoxTriangles);
        }

        private static void CreateTaperedCabin(
            string name,
            Transform parent,
            Material material,
            float bottomWidth,
            float topWidth,
            float bottomY,
            float topY,
            float bottomRearZ,
            float bottomFrontZ,
            float topRearZ,
            float topFrontZ)
        {
            var vertices = new[]
            {
                new Vector3(-bottomWidth * 0.5f, bottomY, bottomRearZ),
                new Vector3(bottomWidth * 0.5f, bottomY, bottomRearZ),
                new Vector3(bottomWidth * 0.5f, bottomY, bottomFrontZ),
                new Vector3(-bottomWidth * 0.5f, bottomY, bottomFrontZ),
                new Vector3(-topWidth * 0.5f, topY, topRearZ),
                new Vector3(topWidth * 0.5f, topY, topRearZ),
                new Vector3(topWidth * 0.5f, topY, topFrontZ),
                new Vector3(-topWidth * 0.5f, topY, topFrontZ),
            };
            CreateMeshDetail(name, parent, material, vertices, BoxTriangles);
        }

        private static void CreateSlopedQuad(
            string name,
            Transform parent,
            Material material,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            CreateMeshDetail(name, parent, material, new[] { a, b, c, d }, DoubleSidedQuadTriangles);
        }

        private static void CreateVerticalPanel(
            string name,
            Transform parent,
            Material material,
            Vector3 normalDirection,
            float centerX,
            float centerY,
            float z,
            float width,
            float height)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var a = new Vector3(centerX - halfWidth, centerY - halfHeight, z);
            var b = new Vector3(centerX + halfWidth, centerY - halfHeight, z);
            var c = new Vector3(centerX + halfWidth, centerY + halfHeight, z);
            var d = new Vector3(centerX - halfWidth, centerY + halfHeight, z);
            if (normalDirection.z < 0f)
            {
                CreateSlopedQuad(name, parent, material, b, a, d, c);
                return;
            }

            CreateSlopedQuad(name, parent, material, a, b, c, d);
        }

        private static void CreateCircleDisc(
            string name,
            Transform parent,
            Material material,
            Vector3 normalDirection,
            float centerX,
            float centerY,
            float z,
            float radiusX,
            float radiusY,
            int segments)
        {
            var vertices = new Vector3[segments + 1];
            vertices[0] = new Vector3(centerX, centerY, z);
            for (var index = 0; index < segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                vertices[index + 1] = new Vector3(
                    centerX + Mathf.Cos(angle) * radiusX,
                    centerY + Mathf.Sin(angle) * radiusY,
                    z);
            }

            var triangles = new List<int>(segments * 6);
            for (var index = 0; index < segments; index++)
            {
                var next = index + 1 == segments ? 1 : index + 2;
                if (normalDirection.z < 0f)
                {
                    triangles.Add(0);
                    triangles.Add(next);
                    triangles.Add(index + 1);

                    triangles.Add(0);
                    triangles.Add(index + 1);
                    triangles.Add(next);
                }
                else
                {
                    triangles.Add(0);
                    triangles.Add(index + 1);
                    triangles.Add(next);

                    triangles.Add(0);
                    triangles.Add(next);
                    triangles.Add(index + 1);
                }
            }

            CreateMeshDetail(name, parent, material, vertices, triangles);
        }

        private static void CreateCubeDetail(
            string name,
            Transform parent,
            Material material,
            Vector3 localPosition,
            Vector3 localScale)
        {
            var detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            detail.name = name;
            detail.transform.SetParent(parent, false);
            detail.transform.localPosition = localPosition;
            detail.transform.localRotation = Quaternion.identity;
            detail.transform.localScale = localScale;
            ConfigureRenderer(detail, material);
            RemoveCollider(detail);
        }

        private static void CreateSphereDetail(
            string name,
            Transform parent,
            Material material,
            Vector3 localPosition,
            Vector3 localScale)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = localScale;
            ConfigureRenderer(sphere, material);
            RemoveCollider(sphere);
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void CreateMeshDetail(
            string name,
            Transform parent,
            Material material,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> triangles)
        {
            var detail = new GameObject(name);
            detail.transform.SetParent(parent, false);

            var mesh = new Mesh { name = $"{name} Mesh" };
            var vertexArray = new Vector3[vertices.Count];
            for (var index = 0; index < vertices.Count; index++)
            {
                vertexArray[index] = vertices[index];
            }

            var triangleArray = new int[triangles.Count];
            for (var index = 0; index < triangles.Count; index++)
            {
                triangleArray[index] = triangles[index];
            }

            mesh.vertices = vertexArray;
            mesh.triangles = triangleArray;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = detail.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = detail.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void CreateLights(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float length,
            float centerZ,
            float y,
            float bodyLength,
            bool wide)
        {
            var x = wide ? width * 0.25f : width * 0.22f;
            var lightWidth = wide ? width * 0.13f : width * 0.12f;
            var lightLength = length * 0.035f;
            var frontZ = centerZ + bodyLength * 0.455f;
            var rearZ = centerZ - bodyLength * 0.455f;

            CreateRoundedPrism("Left Headlight", parent, materials.HeadLight, lightWidth, lightLength, y, length * 0.018f, frontZ, width * 0.025f, -x);
            CreateRoundedPrism("Right Headlight", parent, materials.HeadLight, lightWidth, lightLength, y, length * 0.018f, frontZ, width * 0.025f, x);
            CreateRoundedPrism("Left Tail Light", parent, materials.TailLight, lightWidth * 0.72f, lightLength, y, length * 0.018f, rearZ, width * 0.022f, -x);
            CreateRoundedPrism("Right Tail Light", parent, materials.TailLight, lightWidth * 0.72f, lightLength, y, length * 0.018f, rearZ, width * 0.022f, x);
        }

        private static void CreateWheels(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float bodyLength,
            float centerZ,
            float cellSize,
            float zOffsetFactor)
        {
            var wheelRadius = cellSize * 0.060f;
            var wheelWidth = cellSize * 0.050f;
            var x = width * 0.50f;
            var y = height * 0.18f;
            var rearZ = centerZ - bodyLength * zOffsetFactor;
            var frontZ = centerZ + bodyLength * zOffsetFactor;

            CreateWheel("Rear Left Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(-x, y, rearZ), wheelRadius, wheelWidth);
            CreateWheel("Rear Right Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(x, y, rearZ), wheelRadius, wheelWidth);
            CreateWheel("Front Left Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(-x, y, frontZ), wheelRadius, wheelWidth);
            CreateWheel("Front Right Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(x, y, frontZ), wheelRadius, wheelWidth);
        }

        private static void CreateMysteryWheels(
            Transform parent,
            SilhouetteMaterials materials,
            float width,
            float height,
            float bodyLength,
            float centerZ,
            float cellSize)
        {
            var wheelRadius = cellSize * 0.060f;
            var wheelWidth = cellSize * 0.050f;
            var x = width * 0.50f;
            var y = height * 0.18f;
            var rearZ = centerZ - bodyLength * 0.30f;
            var frontZ = centerZ + bodyLength * 0.30f;

            CreateWheel("Mystery Rear Left Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(-x, y, rearZ), wheelRadius, wheelWidth);
            CreateWheel("Mystery Rear Right Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(x, y, rearZ), wheelRadius, wheelWidth);
            CreateWheel("Mystery Front Left Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(-x, y, frontZ), wheelRadius, wheelWidth);
            CreateWheel("Mystery Front Right Wheel", parent, materials.Wheel, materials.WheelHub, new Vector3(x, y, frontZ), wheelRadius, wheelWidth);
        }

        private static void CreateWheel(string name, Transform parent, Material wheelMaterial, Material hubMaterial, Vector3 localPosition, float radius, float width)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(width, radius, radius);
            ConfigureRenderer(wheel, wheelMaterial);

            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = $"{name} Hub";
            hub.transform.SetParent(parent, false);
            hub.transform.localPosition = localPosition + Vector3.up * 0.001f;
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hub.transform.localScale = new Vector3(width * 1.05f, radius * 0.42f, radius * 0.42f);
            ConfigureRenderer(hub, hubMaterial);
        }

        private static GameObject CreateRoundedPrism(
            string name,
            Transform parent,
            Material material,
            float width,
            float length,
            float bottomY,
            float prismHeight,
            float centerZ,
            float radius,
            float centerX = 0f)
        {
            var safeRadius = Mathf.Min(radius, Mathf.Min(width * 0.48f, length * 0.48f));
            var boundary = BuildRoundedRectangleBoundary(width, length, safeRadius, CornerSegments);
            return CreatePrism(name, parent, material, boundary, new Vector3(centerX, bottomY, centerZ), Mathf.Max(0.001f, prismHeight));
        }

        private static List<Vector3> BuildRoundedRectangleBoundary(float width, float length, float radius, int segments)
        {
            var halfWidth = Mathf.Max(0.001f, width * 0.5f);
            var halfLength = Mathf.Max(0.001f, length * 0.5f);
            radius = Mathf.Clamp(radius, 0.001f, Mathf.Min(halfWidth, halfLength));

            var points = new List<Vector3>((segments + 1) * 4);
            AddArc(points, new Vector2(halfWidth - radius, halfLength - radius), radius, 0f, 90f, segments);
            AddArc(points, new Vector2(-halfWidth + radius, halfLength - radius), radius, 90f, 180f, segments);
            AddArc(points, new Vector2(-halfWidth + radius, -halfLength + radius), radius, 180f, 270f, segments);
            AddArc(points, new Vector2(halfWidth - radius, -halfLength + radius), radius, 270f, 360f, segments);
            return points;
        }

        private static void AddArc(List<Vector3> points, Vector2 center, float radius, float startDegrees, float endDegrees, int segments)
        {
            for (var index = 0; index <= segments; index++)
            {
                var angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)segments) * Mathf.Deg2Rad;
                points.Add(new Vector3(center.x + Mathf.Cos(angle) * radius, 0f, center.y + Mathf.Sin(angle) * radius));
            }
        }

        private static GameObject CreatePrism(
            string name,
            Transform parent,
            Material material,
            IReadOnlyList<Vector3> boundary,
            Vector3 localPosition,
            float height)
        {
            var shape = new GameObject(name);
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localRotation = Quaternion.identity;

            var boundaryCount = boundary.Count;
            var topCenterIndex = boundaryCount * 2;
            var bottomCenterIndex = topCenterIndex + 1;
            var vertices = new Vector3[boundaryCount * 2 + 2];
            for (var index = 0; index < boundaryCount; index++)
            {
                vertices[index] = new Vector3(boundary[index].x, height, boundary[index].z);
                vertices[index + boundaryCount] = new Vector3(boundary[index].x, 0f, boundary[index].z);
            }

            vertices[topCenterIndex] = new Vector3(0f, height, 0f);
            vertices[bottomCenterIndex] = Vector3.zero;

            var triangles = new List<int>(boundaryCount * 12);
            for (var index = 0; index < boundaryCount; index++)
            {
                var next = index + 1 == boundaryCount ? 0 : index + 1;
                var bottom = index + boundaryCount;
                var nextBottom = next + boundaryCount;

                triangles.Add(topCenterIndex);
                triangles.Add(next);
                triangles.Add(index);

                triangles.Add(bottomCenterIndex);
                triangles.Add(bottom);
                triangles.Add(nextBottom);

                triangles.Add(index);
                triangles.Add(next);
                triangles.Add(nextBottom);

                triangles.Add(index);
                triangles.Add(nextBottom);
                triangles.Add(bottom);
            }

            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = shape.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = shape.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return shape;
        }

        private static void ConfigureRenderer(GameObject gameObject, Material material)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private sealed class VehicleMaterials
        {
            public readonly Material Body;
            public readonly Material BodyDark;
            public readonly Material BodyLight;
            public readonly Material Glass;
            public readonly Material Outline;
            public readonly Material Bumper;
            public readonly Material Wheel;
            public readonly Material WheelHub;
            public readonly Material HeadLight;
            public readonly Material TailLight;
            public readonly Material Gloss;

            public VehicleMaterials(PuzzleColor color)
            {
                var bodyColor = PuzzlePalette.ToColor(color);
                Body = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body", bodyColor, 0.74f);
                BodyDark = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body Shade", PuzzlePalette.Darken(bodyColor, 0.22f), 0.62f);
                BodyLight = CreateLitMaterial($"{PuzzlePalette.DisplayName(color)} Vehicle Body Light", Color.Lerp(bodyColor, Color.white, 0.24f), 0.82f);
                Glass = CreateLitMaterial("Vehicle Glass", new Color(0.055f, 0.20f, 0.32f), 0.88f);
                Outline = CreateLitMaterial("Vehicle Dark Undercarriage", new Color(0.045f, 0.052f, 0.064f), 0.58f);
                Bumper = CreateLitMaterial("Vehicle Bumper", new Color(0.065f, 0.075f, 0.088f), 0.50f);
                Wheel = CreateLitMaterial("Vehicle Wheel", new Color(0.030f, 0.033f, 0.040f), 0.42f);
                WheelHub = CreateLitMaterial("Vehicle Wheel Hub", new Color(0.72f, 0.76f, 0.78f), 0.70f);
                HeadLight = PuzzlePalette.CreateSolidMaterial("Vehicle Headlight", new Color(1.00f, 0.94f, 0.64f));
                TailLight = PuzzlePalette.CreateSolidMaterial("Vehicle Tail Light", new Color(0.90f, 0.12f, 0.10f));
                Gloss = CreateLitMaterial("Vehicle Gloss", Color.Lerp(Color.white, bodyColor, 0.18f), 0.96f);
            }

            private static Material CreateLitMaterial(string name, Color color, float smoothness)
            {
                var shader = PuzzlePalette.FindDefaultShader();
                var material = PuzzlePalette.CreateMaterialFromShader(shader, name);
                if (material == null)
                {
                    return null;
                }

                material.color = color;
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

                if (material.HasProperty("_Cull"))
                {
                    material.SetFloat("_Cull", (float)CullMode.Off);
                }

                return material;
            }
        }

        private sealed class SilhouetteMaterials
        {
            public readonly Material Body;
            public readonly Material Top;
            public readonly Material Shade;
            public readonly Material Rim;
            public readonly Material RimLight;
            public readonly Material Wheel;
            public readonly Material WheelHub;

            public SilhouetteMaterials()
            {
                Body = CreateMaterial("Mystery Vehicle Body", new Color(0.022f, 0.027f, 0.034f));
                Top = CreateMaterial("Mystery Vehicle Top", new Color(0.038f, 0.047f, 0.058f));
                Shade = CreateMaterial("Mystery Vehicle Shade", new Color(0.010f, 0.013f, 0.018f));
                Rim = CreateMaterial("Mystery Vehicle Rim", new Color(0.105f, 0.145f, 0.180f));
                RimLight = CreateMaterial("Mystery Vehicle Front Rim", new Color(0.165f, 0.215f, 0.260f));
                Wheel = CreateMaterial("Mystery Vehicle Wheel", new Color(0.006f, 0.008f, 0.012f));
                WheelHub = CreateMaterial("Mystery Vehicle Wheel Hub", new Color(0.140f, 0.170f, 0.200f));
            }

            private static Material CreateMaterial(string name, Color color)
            {
                var material = PuzzlePalette.CreateSolidMaterial(name, color);
                if (material != null && material.HasProperty("_Cull"))
                {
                    material.SetFloat("_Cull", (float)CullMode.Off);
                }

                return material;
            }
        }
    }
}
