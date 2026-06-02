using System;
using UnityEngine;

namespace BusPuzzle
{
    internal static class StationSlotBuilder
    {
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
            Func<Vector3> getFreeStationPosition,
            Func<int, Vector3> getStationPosition,
            Func<int, Vector3> getLockedStationPosition)
        {
            var platformMaterial = PuzzlePalette.CreateSolidMaterial("Station Platform", new Color(0.36f, 0.39f, 0.48f));
            var platformInnerMaterial = PuzzlePalette.CreateSolidMaterial("Station Platform Inner", new Color(0.43f, 0.47f, 0.56f));
            var curbMaterial = PuzzlePalette.CreateSolidMaterial("Station Curb", new Color(0.88f, 0.91f, 0.94f));
            var slotOutlineMaterial = PuzzlePalette.CreateSolidMaterial("Station Slot Line", new Color(0.72f, 0.77f, 0.86f));
            var lockedMaterial = PuzzlePalette.CreateSolidMaterial("Locked Ad Slot", new Color(0.31f, 0.35f, 0.44f));
            var freeMaterial = PuzzlePalette.CreateSolidMaterial("Vip Station Badge", new Color(0.86f, 0.63f, 0.08f));
            var totalStationSlots = freeSlotCount + activeSlotCount + lockedSlotCount;
            var platformWidth = (totalStationSlots - 1) * slotSpacing + slotWidth + 0.42f;
            var platformDepth = slotDepth + 0.30f;

            BoardGeometry.CreateFlatRoundedRect(
                "Station Platform Base",
                stationRoot,
                new Vector3(0f, -0.062f, stationZ),
                new Vector2(platformWidth, platformDepth),
                0.14f,
                platformMaterial);

            BoardGeometry.CreateFlatRoundedRect(
                "Station Platform Surface",
                stationRoot,
                new Vector3(0f, -0.050f, stationZ),
                new Vector2(platformWidth - 0.12f, platformDepth - 0.12f),
                0.11f,
                platformInnerMaterial);

            BoardGeometry.CreateFlatRect(
                "Station Front Curb",
                stationRoot,
                new Vector3(0f, -0.032f, stationZ - platformDepth * 0.5f - 0.025f),
                new Vector2(platformWidth + 0.18f, 0.045f),
                curbMaterial);

            CreateVipStationSlot(stationRoot, getFreeStationPosition(), slotWidth, slotDepth, cellSize, freeMaterial, slotOutlineMaterial, platformInnerMaterial);

            for (var index = 0; index < activeSlotCount; index++)
            {
                CreateStationSlotOutline(stationRoot, $"Station Slot {index + 1}", getStationPosition(index), slotWidth, slotDepth, slotOutlineMaterial, platformInnerMaterial);
            }

            for (var index = 0; index < lockedSlotCount; index++)
            {
                CreateLockedStationSlot(stationRoot, $"Ad Locked Slot {index + 1}", getLockedStationPosition(index), slotWidth, slotDepth, cellSize, slotOutlineMaterial, lockedMaterial);
            }
        }

        private static void CreateStationSlotOutline(Transform root, string name, Vector3 position, float slotWidth, float slotDepth, Material outlineMaterial, Material innerMaterial)
        {
            BoardGeometry.CreateFlatRoundedRect(
                $"{name} Outline",
                root,
                position + Vector3.down * 0.026f,
                new Vector2(slotWidth, slotDepth),
                slotWidth * 0.18f,
                outlineMaterial);

            BoardGeometry.CreateFlatRoundedRect(
                $"{name} Cutout",
                root,
                position + Vector3.down * 0.018f,
                new Vector2(slotWidth - 0.045f, slotDepth - 0.045f),
                slotWidth * 0.15f,
                innerMaterial);
        }

        private static void CreateLockedStationSlot(Transform root, string name, Vector3 position, float slotWidth, float slotDepth, float cellSize, Material outlineMaterial, Material lockedMaterial)
        {
            CreateStationSlotOutline(root, name, position, slotWidth, slotDepth, outlineMaterial, lockedMaterial);

            var plusMaterial = PuzzlePalette.CreateSolidMaterial("Slot Plus", new Color(0.24f, 0.90f, 0.42f));
            CreatePlusBar(root, $"{name} Plus Vertical", position, new Vector3(0.046f, 0.04f, 0.18f), plusMaterial);
            CreatePlusBar(root, $"{name} Plus Horizontal", position, new Vector3(0.16f, 0.04f, 0.046f), plusMaterial);
        }

        private static void CreateVipStationSlot(Transform root, Vector3 position, float slotWidth, float slotDepth, float cellSize, Material vipMaterial, Material outlineMaterial, Material innerMaterial)
        {
            CreateStationSlotOutline(root, "Vip Station Slot", position, slotWidth, slotDepth, outlineMaterial, vipMaterial);
            BoardGeometry.CreateFlatRoundedRect(
                "Vip Station Inner Badge",
                root,
                position + Vector3.down * 0.010f,
                new Vector2(slotWidth - 0.070f, slotDepth - 0.070f),
                slotWidth * 0.13f,
                vipMaterial);

            CreateStationLabel(root, "Vip Station Label", position, "V\nI\nP", new Color(0.34f, 0.25f, 0.04f), cellSize * 0.083f);
        }

        private static void CreateStationLabel(Transform root, string name, Vector3 position, string label, Color color, float characterSize)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(root, false);
            labelObject.transform.SetPositionAndRotation(
                position + new Vector3(0f, 0.025f, 0f),
                Quaternion.Euler(90f, 0f, 0f));

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

        private static void CreatePlusBar(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            BoardGeometry.CreateFlatRect(
                name,
                root,
                position + Vector3.up * 0.02f,
                new Vector2(scale.x, scale.z),
                material);
        }
    }
}
