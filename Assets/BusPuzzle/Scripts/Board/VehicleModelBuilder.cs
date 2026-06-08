using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BusPuzzle
{
    public static class VehicleModelBuilder
    {
        private const int CornerSegments = 5;
        private const float DetailLift = 0.006f;

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

        private static void CreateCompactCar(
            Transform parent,
            VehicleMaterials materials,
            float width,
            float height,
            float length,
            float centerZ,
            float cellSize)
        {
            var bodyWidth = width * 0.98f;
            var bodyLength = length * 0.95f;
            var bodyBottom = height * 0.10f;
            var bodyHeight = height * 0.48f;

            CreateRoundedPrism("Compact Chassis Shadow", parent, materials.Outline, bodyWidth * 1.07f, bodyLength * 1.05f, bodyBottom - height * 0.02f, bodyHeight * 0.62f, centerZ, width * 0.22f);
            CreateRoundedPrism("Compact Body", parent, materials.Body, bodyWidth, bodyLength, bodyBottom, bodyHeight, centerZ, width * 0.20f);
            CreateRoundedPrism("Compact Hood Highlight", parent, materials.BodyLight, width * 0.66f, length * 0.30f, bodyBottom + bodyHeight + DetailLift, height * 0.040f, centerZ + length * 0.23f, width * 0.12f);
            CreateRoundedPrism("Compact Cabin", parent, materials.Glass, width * 0.58f, length * 0.38f, bodyBottom + bodyHeight * 0.72f, height * 0.26f, centerZ + length * 0.01f, width * 0.11f);
            CreateSideWindowPanels(parent, materials, bodyWidth, length * 0.34f, centerZ + length * 0.01f, bodyBottom + bodyHeight * 0.58f, height * 0.15f, 1);
            CreateRoundedPrism("Compact Roof Shine", parent, materials.Gloss, width * 0.24f, length * 0.40f, bodyBottom + bodyHeight + height * 0.05f, height * 0.026f, centerZ + length * 0.04f, width * 0.06f);
            CreateBumpers(parent, materials, width, length, centerZ, bodyBottom + height * 0.12f, bodyLength);
            CreateLights(parent, materials, width, length, centerZ, bodyBottom + height * 0.22f, bodyLength, false);
            CreateWheels(parent, materials, width, height, bodyLength, centerZ, cellSize, 0.28f);
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
    }
}
