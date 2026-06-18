using System;
using UnityEngine;

namespace BusPuzzle
{
    internal static class StationSlotBuilder
    {
        private const float PlatformBaseExtraWidth = 0.46f;
        private const float PlatformBaseExtraDepth = 0.18f;
        private const float BayShadowPadding = 0.026f;
        private const float BayInset = 0.060f;
        private const float BayLineWidth = 0.010f;

        public static void Create(
            Transform stationRoot,
            int freeSlotCount,
            int activeSlotCount,
            int lockedSlotCount,
            float stationZ,
            float slotSpacing,
            float slotWidth,
            float slotDepth,
            float cellSize,
            Quaternion stationRotation,
            Func<Vector3> getFreeStationPosition,
            Func<int, Vector3> getStationPosition,
            Func<int, Vector3> getLockedStationPosition,
            bool useTerminalSkin = false)
        {
            var platformMaterial = PuzzlePalette.CreateSolidMaterial("Station Platform", new Color(0.36f, 0.41f, 0.52f));
            var platformRoadMaterial = useTerminalSkin
                ? PuzzlePalette.CreateTransparentMaterial("Station Bay Road Terminal", new Color(0.74f, 0.82f, 0.91f, 0.12f))
                : PuzzlePalette.CreateSolidMaterial("Station Bay Road", new Color(0.44f, 0.49f, 0.59f));
            var dividerMaterial = PuzzlePalette.CreateTransparentMaterial("Station Bay Divider", new Color(0.82f, 0.88f, 0.96f, useTerminalSkin ? 0.12f : 0.24f));
            var slotShadowMaterial = PuzzlePalette.CreateTransparentMaterial("Station Slot Shadow", new Color(0.10f, 0.13f, 0.18f, useTerminalSkin ? 0.035f : 0.22f));
            var slotOutlineMaterial = PuzzlePalette.CreateTransparentMaterial("Station Slot Line", new Color(0.90f, 0.95f, 1.00f, useTerminalSkin ? 0.42f : 0.66f));
            var lockedMaterial = useTerminalSkin
                ? PuzzlePalette.CreateTransparentMaterial("Locked Ad Slot Terminal", new Color(0.35f, 0.43f, 0.54f, 0.16f))
                : PuzzlePalette.CreateSolidMaterial("Locked Ad Slot", new Color(0.39f, 0.44f, 0.54f));
            var totalStationSlots = freeSlotCount + activeSlotCount + lockedSlotCount;
            var platformWidth = (totalStationSlots - 1) * slotSpacing + slotWidth + PlatformBaseExtraWidth;
            var platformDepth = slotDepth + PlatformBaseExtraDepth;

            if (!useTerminalSkin)
            {
                BoardGeometry.CreateFlatRoundedRect(
                    "Station Platform Base",
                    stationRoot,
                    new Vector3(0f, -0.070f, stationZ),
                    new Vector2(platformWidth, platformDepth),
                    0.09f,
                    platformMaterial);

                CreateBayDividers(stationRoot, totalStationSlots, slotSpacing, slotDepth, stationZ, stationRotation, dividerMaterial);
            }

            CreateVipStationSlot(stationRoot, getFreeStationPosition(), slotWidth, slotDepth, cellSize, stationRotation, useTerminalSkin);

            for (var index = 0; index < activeSlotCount; index++)
            {
                CreateStationSlotOutline(stationRoot, $"Station Slot {index + 1}", getStationPosition(index), slotWidth, slotDepth, stationRotation, slotOutlineMaterial, platformRoadMaterial, slotShadowMaterial);
            }

            for (var index = 0; index < lockedSlotCount; index++)
            {
                CreateLockedStationSlot(stationRoot, $"Ad Locked Slot {index + 1}", index, getLockedStationPosition(index), slotWidth, slotDepth, cellSize, stationRotation, slotOutlineMaterial, lockedMaterial, slotShadowMaterial);
            }
        }

        private static void CreateBayDividers(Transform root, int totalStationSlots, float slotSpacing, float slotDepth, float stationZ, Quaternion stationRotation, Material dividerMaterial)
        {
            var center = new Vector3(0f, -0.032f, stationZ);
            for (var index = 1; index < totalStationSlots; index++)
            {
                var offset = (index - totalStationSlots * 0.5f) * slotSpacing;
                BoardGeometry.CreateFlatRect(
                    $"Station Bay Divider {index}",
                    root,
                    center + Vector3.right * offset,
                    new Vector2(BayLineWidth, slotDepth * 0.82f),
                    dividerMaterial,
                    stationRotation);
            }
        }

        private static void CreateStationSlotOutline(
            Transform root,
            string name,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            Quaternion stationRotation,
            Material outlineMaterial,
            Material innerMaterial,
            Material shadowMaterial)
        {
            BoardGeometry.CreateFlatRoundedRect(
                $"{name} Bay Shadow",
                root,
                position + Vector3.down * 0.035f,
                new Vector2(slotWidth + BayShadowPadding, slotDepth + BayShadowPadding),
                slotWidth * 0.20f,
                shadowMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                $"{name} Bay Outline",
                root,
                position + Vector3.down * 0.026f,
                new Vector2(slotWidth, slotDepth),
                slotWidth * 0.12f,
                outlineMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                $"{name} Bay Interior",
                root,
                position + Vector3.down * 0.018f,
                new Vector2(slotWidth - BayInset, slotDepth - BayInset),
                slotWidth * 0.10f,
                innerMaterial,
                stationRotation);
        }

        private static void CreateLockedStationSlot(
            Transform root,
            string name,
            int lockedSlotIndex,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            float cellSize,
            Quaternion stationRotation,
            Material outlineMaterial,
            Material lockedMaterial,
            Material shadowMaterial)
        {
            CreateStationSlotOutline(root, name, position, slotWidth, slotDepth, stationRotation, outlineMaterial, lockedMaterial, shadowMaterial);

            var plusMaterial = PuzzlePalette.CreateSolidMaterial("Slot Plus", new Color(0.26f, 0.92f, 0.50f));
            CreatePlusBar(root, $"{name} Plus Vertical", position, new Vector3(slotWidth * 0.14f, 0.04f, slotDepth * 0.25f), stationRotation, plusMaterial);
            CreatePlusBar(root, $"{name} Plus Horizontal", position, new Vector3(slotWidth * 0.46f, 0.04f, slotDepth * 0.065f), stationRotation, plusMaterial);
            CreateUnlockTouchTarget(root, $"{name} Touch Target", lockedSlotIndex, position, slotWidth, slotDepth, cellSize, stationRotation);
        }

        private static void CreateVipStationSlot(
            Transform root,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            float cellSize,
            Quaternion stationRotation,
            bool useTerminalSkin)
        {
            const float VipVisualInset = 0.018f;
            var vipSlotWidth = slotWidth - VipVisualInset;
            var vipSlotDepth = slotDepth - VipVisualInset;
            if (useTerminalSkin)
            {
                CreateTerminalVipStationSlot(root, position, vipSlotWidth, vipSlotDepth, cellSize, stationRotation);
                return;
            }

            var vipAuraMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Station Warm Aura", new Color(1.00f, 0.86f, 0.20f, 0.13f));
            var vipGlowMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Station Gold Glow", new Color(1.00f, 0.78f, 0.18f, 0.24f));
            var vipEdgeMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Station Soft Gold Edge", new Color(1.00f, 0.84f, 0.22f, 0.48f));
            var vipOuterRimMaterial = PuzzlePalette.CreateSolidMaterial("Vip Station Outer Gold Rim", new Color(0.95f, 0.62f, 0.00f));
            var vipInnerRimMaterial = PuzzlePalette.CreateSolidMaterial("Vip Station Cream Gold Rim", new Color(1.00f, 0.90f, 0.36f));
            var vipInteriorMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Station Gold Interior", new Color(1.00f, 0.91f, 0.50f, 0.58f));
            var vipSheenMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Station Diagonal Sheen", new Color(1.00f, 0.98f, 0.78f, 0.26f));
            var vipLightStreakMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Station Light Streak", new Color(1.00f, 0.98f, 0.76f, 0.54f));
            var vipLabelColor = new Color(1.00f, 0.70f, 0.00f);
            var vipLabelShadowColor = new Color(0.42f, 0.25f, 0.00f, 0.58f);
            var vipLabelSize = Mathf.Max(cellSize * 0.120f, vipSlotWidth * 0.132f);
            const float VipLabelYawDegrees = 0f;

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Warm Aura",
                root,
                position + Vector3.down * 0.041f,
                new Vector2(vipSlotWidth + BayShadowPadding * 0.18f, vipSlotDepth + BayShadowPadding * 0.18f),
                vipSlotWidth * 0.18f,
                vipAuraMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Gold Glow",
                root,
                position + Vector3.down * 0.035f,
                new Vector2(vipSlotWidth + BayShadowPadding * 0.08f, vipSlotDepth + BayShadowPadding * 0.08f),
                vipSlotWidth * 0.17f,
                vipGlowMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Gold Edge",
                root,
                position + Vector3.down * 0.031f,
                new Vector2(vipSlotWidth - BayInset * 0.12f, vipSlotDepth - BayInset * 0.12f),
                vipSlotWidth * 0.16f,
                vipEdgeMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Outer Gold Rim",
                root,
                position + Vector3.down * 0.026f,
                new Vector2(vipSlotWidth - BayInset * 0.20f, vipSlotDepth - BayInset * 0.20f),
                vipSlotWidth * 0.15f,
                vipOuterRimMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Inner Cream Rim",
                root,
                position + Vector3.down * 0.020f,
                new Vector2(vipSlotWidth - BayInset * 0.66f, vipSlotDepth - BayInset * 0.66f),
                vipSlotWidth * 0.14f,
                vipInnerRimMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Gold Interior",
                root,
                position + Vector3.down * 0.014f,
                new Vector2(vipSlotWidth - BayInset * 1.18f, vipSlotDepth - BayInset * 1.18f),
                vipSlotWidth * 0.13f,
                vipInteriorMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRect(
                "Vip Station Diagonal Sheen",
                root,
                position + Vector3.down * 0.009f,
                new Vector2(vipSlotWidth * 0.11f, vipSlotDepth * 0.82f),
                vipSheenMaterial,
                stationRotation * Quaternion.Euler(0f, -23f, 0f));

            BoardGeometry.CreateFlatRect(
                "Vip Station Top Light Streak",
                root,
                position + Vector3.up * 0.010f + stationRotation * new Vector3(-vipSlotWidth * 0.14f, 0f, vipSlotDepth * 0.19f),
                new Vector2(vipSlotWidth * 0.40f, 0.016f),
                vipLightStreakMaterial,
                stationRotation * Quaternion.Euler(0f, -22f, 0f));

            BoardGeometry.CreateFlatRect(
                "Vip Station Bottom Light Streak",
                root,
                position + Vector3.up * 0.011f + stationRotation * new Vector3(vipSlotWidth * 0.12f, 0f, -vipSlotDepth * 0.18f),
                new Vector2(vipSlotWidth * 0.30f, 0.012f),
                vipLightStreakMaterial,
                stationRotation * Quaternion.Euler(0f, -22f, 0f));

            BoardGeometry.CreateFlatRect(
                "Vip Station Gold Stop Line",
                root,
                position + Vector3.down * 0.002f - stationRotation * Vector3.forward * (vipSlotDepth * 0.34f),
                new Vector2(vipSlotWidth * 0.48f, 0.020f),
                vipOuterRimMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Inner Badge",
                root,
                position + Vector3.up * 0.004f,
                new Vector2(vipSlotWidth - 0.104f, vipSlotDepth - 0.104f),
                vipSlotWidth * 0.12f,
                vipInteriorMaterial,
                stationRotation);

            CreateStationLabel(
                root,
                "Vip Station Label Shadow",
                position + stationRotation * new Vector3(0.008f, 0f, -0.010f),
                "VIP",
                vipLabelShadowColor,
                vipLabelSize,
                stationRotation,
                FontStyle.Bold,
                58,
                VipLabelYawDegrees);

            CreateStationLabel(
                root,
                "Vip Station Label",
                position,
                "VIP",
                vipLabelColor,
                vipLabelSize,
                stationRotation,
                FontStyle.Bold,
                58,
                VipLabelYawDegrees);
        }

        private static void CreateTerminalVipStationSlot(
            Transform root,
            Vector3 position,
            float vipSlotWidth,
            float vipSlotDepth,
            float cellSize,
            Quaternion stationRotation)
        {
            var cardShadowMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Pass Card Shadow", new Color(0.36f, 0.24f, 0.04f, 0.20f));
            var cardBorderMaterial = PuzzlePalette.CreateSolidMaterial("Vip Pass Card Gold Border", new Color(0.94f, 0.70f, 0.22f));
            var cardMaterial = PuzzlePalette.CreateSolidMaterial("Vip Pass Card Cream", new Color(1.00f, 0.94f, 0.62f));
            var starMaterial = PuzzlePalette.CreateSolidMaterial("Vip Pass Card Star", new Color(0.94f, 0.67f, 0.16f));
            var starShadowMaterial = PuzzlePalette.CreateTransparentMaterial("Vip Pass Card Star Shadow", new Color(0.48f, 0.30f, 0.02f, 0.22f));
            var vipLabelColor = new Color(0.78f, 0.53f, 0.12f);
            var vipLabelShadowColor = new Color(0.46f, 0.30f, 0.06f, 0.24f);
            var vipLabelSize = Mathf.Max(cellSize * 0.080f, vipSlotWidth * 0.088f);
            const float VipLabelYawDegrees = 0f;

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Pass Card Shadow",
                root,
                position + Vector3.down * 0.027f + stationRotation * new Vector3(0.012f, 0f, -0.012f),
                new Vector2(vipSlotWidth - BayInset * 0.18f, vipSlotDepth - BayInset * 0.16f),
                vipSlotWidth * 0.16f,
                cardShadowMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Pass Card Border",
                root,
                position + Vector3.down * 0.020f,
                new Vector2(vipSlotWidth - BayInset * 0.26f, vipSlotDepth - BayInset * 0.22f),
                vipSlotWidth * 0.16f,
                cardBorderMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                "Vip Pass Card Cream Face",
                root,
                position + Vector3.down * 0.013f,
                new Vector2(vipSlotWidth - BayInset * 0.64f, vipSlotDepth - BayInset * 0.58f),
                vipSlotWidth * 0.13f,
                cardMaterial,
                stationRotation);

            var starCenter = position + stationRotation * new Vector3(0f, 0f, -vipSlotDepth * 0.18f);
            CreateFlatStar(
                root,
                "Vip Pass Star Shadow",
                starCenter + Vector3.down * 0.003f + stationRotation * new Vector3(0.006f, 0f, -0.007f),
                vipSlotWidth * 0.285f,
                stationRotation,
                starShadowMaterial);
            CreateFlatStar(
                root,
                "Vip Pass Star",
                starCenter + Vector3.up * 0.006f,
                vipSlotWidth * 0.270f,
                stationRotation,
                starMaterial);

            CreateStationLabel(
                root,
                "Vip Terminal Label Shadow",
                position + stationRotation * new Vector3(0.004f, 0f, vipSlotDepth * 0.205f),
                "VIP",
                vipLabelShadowColor,
                vipLabelSize,
                stationRotation,
                FontStyle.Bold,
                48,
                VipLabelYawDegrees);

            CreateStationLabel(
                root,
                "Vip Terminal Label",
                position + stationRotation * new Vector3(0f, 0f, vipSlotDepth * 0.215f),
                "VIP",
                vipLabelColor,
                vipLabelSize,
                stationRotation,
                FontStyle.Bold,
                48,
                VipLabelYawDegrees);
        }

        private static void CreateFlatStar(
            Transform root,
            string name,
            Vector3 position,
            float radius,
            Quaternion stationRotation,
            Material material)
        {
            const int pointCount = 5;
            var vertices = new Vector3[pointCount * 2 + 1];
            var triangles = new int[pointCount * 2 * 6];
            vertices[0] = Vector3.zero;
            var innerRadius = radius * 0.47f;

            for (var index = 0; index < pointCount * 2; index++)
            {
                var angle = Mathf.PI * 0.5f + index * Mathf.PI / pointCount;
                var pointRadius = index % 2 == 0 ? radius : innerRadius;
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * pointRadius,
                    0f,
                    Mathf.Sin(angle) * pointRadius);
            }

            for (var index = 0; index < pointCount * 2; index++)
            {
                var current = index + 1;
                var next = index + 1 == pointCount * 2 ? 1 : index + 2;
                var triangleIndex = index * 6;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = current;
                triangles[triangleIndex + 2] = next;
                triangles[triangleIndex + 3] = 0;
                triangles[triangleIndex + 4] = next;
                triangles[triangleIndex + 5] = current;
            }

            var star = new GameObject(name).transform;
            star.SetParent(root, false);
            star.SetPositionAndRotation(position, stationRotation);
            BoardGeometry.CreateMeshObject(name, star, vertices, triangles, material, true);
        }

        private static void CreateStationLabel(
            Transform root,
            string name,
            Vector3 position,
            string label,
            Color color,
            float characterSize,
            Quaternion stationRotation,
            FontStyle fontStyle = FontStyle.Normal,
            int fontSize = 48,
            float yawDegrees = 0f)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(root, false);
            labelObject.transform.SetPositionAndRotation(
                position + new Vector3(0f, 0.025f, 0f),
                stationRotation * Quaternion.Euler(90f, yawDegrees, 0f));

            var text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = characterSize;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            GameFontProvider.ApplyToTextMesh(text, fontStyle);

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void CreatePlusBar(Transform root, string name, Vector3 position, Vector3 scale, Quaternion stationRotation, Material material)
        {
            BoardGeometry.CreateFlatRect(
                name,
                root,
                position + Vector3.up * 0.02f,
                new Vector2(scale.x, scale.z),
                material,
                stationRotation);
        }

        private static void CreateUnlockTouchTarget(
            Transform root,
            string name,
            int lockedSlotIndex,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            float cellSize,
            Quaternion stationRotation)
        {
            var targetObject = new GameObject(name);
            targetObject.transform.SetParent(root, false);
            targetObject.transform.SetPositionAndRotation(position + Vector3.up * (cellSize * 0.18f), stationRotation);

            var collider = targetObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(slotWidth * 1.45f, cellSize * 0.36f, slotDepth * 1.12f);
            collider.center = Vector3.zero;

            targetObject.AddComponent<StationSlotUnlockTarget>().Initialize(lockedSlotIndex);
        }
    }
}
