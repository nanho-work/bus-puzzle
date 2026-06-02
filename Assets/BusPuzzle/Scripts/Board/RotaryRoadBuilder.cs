using UnityEngine;

namespace BusPuzzle
{
    internal readonly struct RotaryRoadBuildSettings
    {
        public readonly float RotaryCenterZ;
        public readonly float StationZ;
        public readonly float GridWorldWidth;
        public readonly float GridWorldDepth;
        public readonly float GridCenterZ;
        public readonly float PassengerPivotOffset;

        public RotaryRoadBuildSettings(
            float rotaryCenterZ,
            float stationZ,
            float gridWorldWidth,
            float gridWorldDepth,
            float gridCenterZ,
            float passengerPivotOffset)
        {
            RotaryCenterZ = rotaryCenterZ;
            StationZ = stationZ;
            GridWorldWidth = gridWorldWidth;
            GridWorldDepth = gridWorldDepth;
            GridCenterZ = gridCenterZ;
            PassengerPivotOffset = passengerPivotOffset;
        }
    }

    internal static class RotaryRoadBuilder
    {
        public static void CreateGround(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings)
        {
            BoardGeometry.CreateFlatRect(
                "Terminal Floor",
                parent,
                new Vector3(0f, -0.120f, -0.52f),
                new Vector2(5.85f, 9.15f),
                PuzzlePalette.CreateSolidMaterial("Terminal Floor", new Color(0.63f, 0.69f, 0.75f)));

            var rotaryDistrictSize = new Vector2(layout.OuterRadiusX * 2f + 0.46f, layout.OuterRadiusZ * 2f + 0.86f);
            var rotaryDistrictCenter = new Vector3(0f, -0.095f, settings.RotaryCenterZ - 0.06f);
            BoardGeometry.CreateFlatRect(
                "Passenger Rotary District",
                parent,
                rotaryDistrictCenter,
                rotaryDistrictSize,
                PuzzlePalette.CreateSolidMaterial("Passenger Rotary District", new Color(0.70f, 0.75f, 0.80f)));

            CreateRotaryDistrictTiles(parent, new Vector3(0f, -0.088f, settings.RotaryCenterZ - 0.06f), rotaryDistrictSize);

            BoardGeometry.CreateFlatRect(
                "Bus Puzzle Yard",
                parent,
                new Vector3(0f, -0.090f, settings.GridCenterZ),
                new Vector2(settings.GridWorldWidth + 0.48f, settings.GridWorldDepth + 0.48f),
                PuzzlePalette.CreateSolidMaterial("Bus Puzzle Yard", new Color(0.58f, 0.66f, 0.74f)));

            BoardGeometry.CreateFlatRect(
                "Open Boarding Apron",
                parent,
                new Vector3(0f, -0.070f, settings.StationZ),
                new Vector2(settings.GridWorldWidth + 0.70f, 0.66f),
                PuzzlePalette.CreateSolidMaterial("Open Boarding Apron", new Color(0.45f, 0.49f, 0.58f)));

            BoardGeometry.CreateFlatRect(
                "Boarding Apron Front Curb",
                parent,
                new Vector3(0f, -0.052f, settings.StationZ - 0.36f),
                new Vector2(settings.GridWorldWidth + 0.86f, 0.045f),
                PuzzlePalette.CreateSolidMaterial("Boarding Apron Front Curb", new Color(0.88f, 0.91f, 0.94f)));

            BoardGeometry.CreateFlatRect(
                "Boarding Apron Back Curb",
                parent,
                new Vector3(0f, -0.052f, settings.StationZ + 0.36f),
                new Vector2(settings.GridWorldWidth + 0.86f, 0.045f),
                PuzzlePalette.CreateSolidMaterial("Boarding Apron Back Curb", new Color(0.88f, 0.91f, 0.94f)));

            CreateFeederLane(parent, layout, settings, -1);
            CreateFeederLane(parent, layout, settings, 1);
        }

        public static void CreatePassengerRotary(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings)
        {
            CreateRoad(parent, layout, settings);
            BoardGeometry.CreatePathFill(
                $"Rotary Island {layout.CapacityUnits}",
                parent,
                layout,
                settings.RotaryCenterZ,
                -0.058f,
                -settings.PassengerPivotOffset - 0.060f,
                PuzzlePalette.CreateSolidMaterial("Rotary Island", new Color(0.72f, 0.77f, 0.82f)));

            var gatePosition = layout.ToWorldPoint(
                layout.Path.Sample(layout.Preset.BoardingGateProgress).Point,
                settings.RotaryCenterZ,
                -0.030f);
            BoardGeometry.CreateFlatRect(
                "Boarding Gate Opening",
                parent,
                gatePosition,
                new Vector2(1.34f, 0.24f),
                PuzzlePalette.CreateSolidMaterial("Boarding Gate Opening", new Color(0.88f, 0.73f, 0.17f)));
        }

        private static void CreateRoad(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings)
        {
            var roadMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Asphalt Road", new Color(0.42f, 0.46f, 0.54f));
            var railMaterial = PuzzlePalette.CreateSolidMaterial("Rotary White Guardrail", new Color(0.96f, 0.98f, 1.00f));
            var railShadowMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Guardrail Shadow", new Color(0.24f, 0.28f, 0.34f));
            var shadowMaterial = PuzzlePalette.CreateSolidMaterial("Rotary Soft Shadow", new Color(0.30f, 0.34f, 0.40f));

            const float railWidth = 0.092f;
            var innerOffset = -settings.PassengerPivotOffset;
            var outerOffset = layout.RoadWidth - settings.PassengerPivotOffset;
            BoardGeometry.CreatePathBand("Rotary Soft Shadow", parent, layout, settings.RotaryCenterZ, -0.078f, innerOffset - 0.045f, outerOffset + 0.055f, shadowMaterial);
            BoardGeometry.CreatePathBand("Rotary Road", parent, layout, settings.RotaryCenterZ, -0.066f, innerOffset, outerOffset, roadMaterial);
            BoardGeometry.CreatePathBand("Outer Rotary Guardrail Shadow", parent, layout, settings.RotaryCenterZ, -0.050f, outerOffset - 0.006f, outerOffset + railWidth + 0.020f, railShadowMaterial);
            BoardGeometry.CreatePathBand("Outer Rotary Guardrail", parent, layout, settings.RotaryCenterZ, -0.026f, outerOffset, outerOffset + railWidth, railMaterial);
            BoardGeometry.CreatePathBand("Inner Rotary Guardrail Shadow", parent, layout, settings.RotaryCenterZ, -0.049f, innerOffset - railWidth - 0.020f, innerOffset + 0.006f, railShadowMaterial);
            BoardGeometry.CreatePathBand("Inner Rotary Guardrail", parent, layout, settings.RotaryCenterZ, -0.025f, innerOffset - railWidth, innerOffset, railMaterial);
        }

        private static void CreateRotaryDistrictTiles(Transform parent, Vector3 center, Vector2 size)
        {
            var tileMaterial = PuzzlePalette.CreateSolidMaterial("Rotary District Tile Lines", new Color(0.58f, 0.64f, 0.70f));
            const float spacing = 0.22f;
            const float lineWidth = 0.012f;
            var verticalCount = Mathf.FloorToInt(size.x / spacing);
            var horizontalCount = Mathf.FloorToInt(size.y / spacing);

            for (var index = 0; index <= verticalCount; index++)
            {
                var x = center.x - size.x * 0.5f + index * spacing;
                BoardGeometry.CreateFlatRect($"Rotary Tile Vertical {index + 1}", parent, new Vector3(x, center.y, center.z), new Vector2(lineWidth, size.y), tileMaterial);
            }

            for (var index = 0; index <= horizontalCount; index++)
            {
                var z = center.z - size.y * 0.5f + index * spacing;
                BoardGeometry.CreateFlatRect($"Rotary Tile Horizontal {index + 1}", parent, new Vector3(center.x, center.y, z), new Vector2(size.x, lineWidth), tileMaterial);
            }
        }

        private static void CreateFeederLane(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, int side)
        {
            var name = side < 0 ? "Left Passenger Feeder" : "Right Passenger Feeder";
            var feederPath = layout.GetFeederPath(side);
            var laneMaterial = PuzzlePalette.CreateSolidMaterial(name, new Color(0.43f, 0.47f, 0.55f));
            var railMaterial = PuzzlePalette.CreateSolidMaterial($"{name} Guardrail", new Color(0.88f, 0.92f, 0.96f));
            var railShadowMaterial = PuzzlePalette.CreateSolidMaterial($"{name} Guardrail Shadow", new Color(0.25f, 0.29f, 0.35f));

            const float railWidth = 0.066f;
            var innerOffset = -settings.PassengerPivotOffset;
            var outerOffset = layout.RoadWidth - settings.PassengerPivotOffset;
            BoardGeometry.CreateOpenPathBand($"{name} Road", parent, layout, feederPath, settings.RotaryCenterZ, -0.060f, innerOffset, outerOffset, laneMaterial);
            BoardGeometry.CreateOpenPathBand($"{name} Outer Rail Shadow", parent, layout, feederPath, settings.RotaryCenterZ, -0.042f, outerOffset - 0.004f, outerOffset + railWidth + 0.018f, railShadowMaterial);
            BoardGeometry.CreateOpenPathBand($"{name} Outer Rail", parent, layout, feederPath, settings.RotaryCenterZ, -0.024f, outerOffset, outerOffset + railWidth, railMaterial);
            BoardGeometry.CreateOpenPathBand($"{name} Inner Rail Shadow", parent, layout, feederPath, settings.RotaryCenterZ, -0.042f, innerOffset - railWidth - 0.018f, innerOffset + 0.004f, railShadowMaterial);
            BoardGeometry.CreateOpenPathBand($"{name} Inner Rail", parent, layout, feederPath, settings.RotaryCenterZ, -0.024f, innerOffset - railWidth, innerOffset, railMaterial);
        }
    }
}
