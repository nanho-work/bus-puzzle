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
            var freeMaterial = PuzzlePalette.CreateSolidMaterial("Vip Station Badge", new Color(0.86f, 0.63f, 0.08f));
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
            CreateVipStationSlot(stationRoot, getFreeStationPosition(), slotWidth, slotDepth, cellSize, stationRotation, freeMaterial, slotOutlineMaterial, slotShadowMaterial);

            for (var index = 0; index < activeSlotCount; index++)
            {
                CreateStationSlotOutline(stationRoot, $"Station Slot {index + 1}", getStationPosition(index), slotWidth, slotDepth, stationRotation, slotOutlineMaterial, platformRoadMaterial, slotShadowMaterial);
            }

            for (var index = 0; index < lockedSlotCount; index++)
            {
                CreateLockedStationSlot(stationRoot, $"Ad Locked Slot {index + 1}", getLockedStationPosition(index), slotWidth, slotDepth, cellSize, stationRotation, slotOutlineMaterial, lockedMaterial, slotShadowMaterial);
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
            CreatePlusBar(root, $"{name} Plus Vertical", position, new Vector3(0.046f, 0.04f, 0.18f), stationRotation, plusMaterial);
            CreatePlusBar(root, $"{name} Plus Horizontal", position, new Vector3(0.16f, 0.04f, 0.046f), stationRotation, plusMaterial);
        }

        private static void CreateVipStationSlot(
            Transform root,
            Vector3 position,
            float slotWidth,
            float slotDepth,
            float cellSize,
            Quaternion stationRotation,
            Material vipMaterial,
            Material outlineMaterial,
            Material shadowMaterial)
        {
            CreateStationSlotOutline(root, "Vip Station Slot", position, slotWidth, slotDepth, stationRotation, outlineMaterial, vipMaterial, shadowMaterial);
            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Inner Badge",
                root,
                position + Vector3.down * 0.004f,
                new Vector2(slotWidth - 0.075f, slotDepth - 0.075f),
                slotWidth * 0.13f,
                vipMaterial,
                stationRotation);

            CreateStationLabel(root, "Vip Station Label", position, "V\nI\nP", new Color(0.34f, 0.25f, 0.04f), cellSize * 0.083f, stationRotation);
        }

        private static void CreateStationLabel(Transform root, string name, Vector3 position, string label, Color color, float characterSize, Quaternion stationRotation)
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
            text.fontSize = 48;
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
    }
}
