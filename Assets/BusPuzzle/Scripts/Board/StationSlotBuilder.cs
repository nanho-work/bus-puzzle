using System;
using UnityEngine;

namespace BusPuzzle
{
    internal static class StationSlotBuilder
    {
        private const float PlatformBaseExtraWidth = 0.46f;
        private const float PlatformBaseExtraDepth = 0.24f;
        private const float BayShadowPadding = 0.036f;
        private const float BayInset = 0.052f;
        private const float BayLineWidth = 0.018f;

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
            Func<int, Vector3> getLockedStationPosition)
        {
            var platformMaterial = PuzzlePalette.CreateSolidMaterial("Station Platform", new Color(0.33f, 0.37f, 0.46f));
            var platformRoadMaterial = PuzzlePalette.CreateSolidMaterial("Station Bay Road", new Color(0.41f, 0.45f, 0.54f));
            var dividerMaterial = PuzzlePalette.CreateSolidMaterial("Station Bay Divider", new Color(0.61f, 0.67f, 0.76f));
            var slotShadowMaterial = PuzzlePalette.CreateSolidMaterial("Station Slot Shadow", new Color(0.23f, 0.27f, 0.34f));
            var slotOutlineMaterial = PuzzlePalette.CreateSolidMaterial("Station Slot Line", new Color(0.72f, 0.77f, 0.86f));
            var lockedMaterial = PuzzlePalette.CreateSolidMaterial("Locked Ad Slot", new Color(0.31f, 0.35f, 0.44f));
            var totalStationSlots = freeSlotCount + activeSlotCount + lockedSlotCount;
            var platformWidth = (totalStationSlots - 1) * slotSpacing + slotWidth + PlatformBaseExtraWidth;
            var platformDepth = slotDepth + PlatformBaseExtraDepth;

            BoardGeometry.CreateFlatRoundedRect(
                "Station Platform Base",
                stationRoot,
                new Vector3(0f, -0.070f, stationZ),
                new Vector2(platformWidth, platformDepth),
                0.13f,
                platformMaterial);

            CreateBayDividers(stationRoot, totalStationSlots, slotSpacing, slotDepth, stationZ, stationRotation, dividerMaterial);
            CreateVipStationSlot(stationRoot, getFreeStationPosition(), slotWidth, slotDepth, cellSize, stationRotation);

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
                slotWidth * 0.18f,
                outlineMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRoundedRect(
                $"{name} Bay Interior",
                root,
                position + Vector3.down * 0.018f,
                new Vector2(slotWidth - BayInset, slotDepth - BayInset),
                slotWidth * 0.15f,
                innerMaterial,
                stationRotation);

            BoardGeometry.CreateFlatRect(
                $"{name} Stop Line",
                root,
                position + Vector3.down * 0.006f - stationRotation * Vector3.forward * (slotDepth * 0.34f),
                new Vector2(slotWidth * 0.48f, 0.020f),
                outlineMaterial,
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

            var plusMaterial = PuzzlePalette.CreateSolidMaterial("Slot Plus", new Color(0.24f, 0.90f, 0.42f));
            CreatePlusBar(root, $"{name} Plus Vertical", position, new Vector3(slotWidth * 0.16f, 0.04f, slotDepth * 0.29f), stationRotation, plusMaterial);
            CreatePlusBar(root, $"{name} Plus Horizontal", position, new Vector3(slotWidth * 0.55f, 0.04f, slotDepth * 0.075f), stationRotation, plusMaterial);
            CreateUnlockTouchTarget(root, $"{name} Touch Target", lockedSlotIndex, position, slotWidth, slotDepth, cellSize, stationRotation);
        }

        private static void CreateVipStationSlot(
            Transform root,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            float cellSize,
            Quaternion stationRotation)
        {
            const float VipVisualInset = 0.018f;
            var vipSlotWidth = slotWidth - VipVisualInset;
            var vipSlotDepth = slotDepth - VipVisualInset;
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
                position + stationRotation * new Vector3(0.010f, 0f, -0.012f),
                "V\nI\nP",
                vipLabelShadowColor,
                Mathf.Max(cellSize * 0.086f, vipSlotWidth * 0.094f),
                stationRotation,
                FontStyle.Bold,
                52);

            CreateStationLabel(
                root,
                "Vip Station Label",
                position,
                "V\nI\nP",
                vipLabelColor,
                Mathf.Max(cellSize * 0.086f, vipSlotWidth * 0.094f),
                stationRotation,
                FontStyle.Bold,
                52);
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
            int fontSize = 48)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(root, false);
            labelObject.transform.SetPositionAndRotation(
                position + new Vector3(0f, 0.025f, 0f),
                stationRotation * Quaternion.Euler(90f, 0f, 0f));

            var text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = characterSize;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;

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
