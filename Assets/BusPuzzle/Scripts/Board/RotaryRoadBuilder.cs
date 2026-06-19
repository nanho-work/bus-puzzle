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
        private const float FeederRoadScreenExitExtension = 1.55f;
        private const float FeederRoadJoinExtensionScale = 0.24f;
        private const float FeederRailJoinExtensionScale = 0.08f;
        private const float FeederJunctionPatchWidthScale = 0.96f;
        private const float FeederJunctionPatchLengthScale = 0.74f;

        public static void CreateGround(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, BoardThemeId theme = BoardThemeId.Field)
        {
            var style = BoardThemePalette.GetStyle(theme);
            var feederTopZ = settings.RotaryCenterZ + GetMaxFeederY(layout) + layout.RoadWidth;
            const float floorBottomZ = -5.10f;
            var floorTopZ = Mathf.Max(4.78f, feederTopZ + FeederRoadScreenExitExtension + 0.42f);
            var floorDepth = floorTopZ - floorBottomZ;
            var floorCenterZ = (floorBottomZ + floorTopZ) * 0.5f;

            BoardGeometry.CreateFlatRect(
                "Terminal Floor",
                parent,
                new Vector3(0f, -0.120f, floorCenterZ),
                new Vector2(5.85f, floorDepth),
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Theme Floor", style.Floor));

            var feederHalfWidth = GetMaxAbsFeederX(layout) + layout.RoadWidth * 0.5f + 0.24f;
            var rotaryDistrictWidth = Mathf.Max(layout.OuterRadiusX * 2f + 0.46f, feederHalfWidth * 2f);
            var rotaryDistrictBottomZ = settings.RotaryCenterZ - layout.OuterRadiusZ - 0.58f;
            var rotaryDistrictTopZ = feederTopZ + 0.26f;
            var rotaryDistrictDepth = rotaryDistrictTopZ - rotaryDistrictBottomZ;
            var rotaryDistrictCenter = new Vector3(0f, -0.095f, (rotaryDistrictBottomZ + rotaryDistrictTopZ) * 0.5f);
            BoardGeometry.CreateFlatRect(
                "Passenger Rotary District",
                parent,
                rotaryDistrictCenter,
                new Vector2(rotaryDistrictWidth, rotaryDistrictDepth),
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Rotary District", style.RotaryDistrict));

            BoardGeometry.CreateFlatRect(
                "Bus Puzzle Yard",
                parent,
                new Vector3(0f, -0.090f, settings.GridCenterZ),
                new Vector2(settings.GridWorldWidth + 0.48f, settings.GridWorldDepth + 0.48f),
                PuzzlePalette.CreateSolidMaterial($"{style.Name} Puzzle Yard", style.PuzzleYard));

            CreateFeederLane(parent, layout, settings, -1, style);
            CreateFeederLane(parent, layout, settings, 1, style);
        }

        public static void CreatePassengerRotary(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, BoardThemeId theme = BoardThemeId.Field)
        {
            var style = BoardThemePalette.GetStyle(theme);
            CreateRoad(parent, layout, settings, style);

            if (layout.Preset.Id != RotaryRoadPresetId.SnakeTest)
            {
                BoardGeometry.CreatePathFill(
                    $"Rotary Island {layout.CapacityUnits}",
                    parent,
                    layout,
                    settings.RotaryCenterZ,
                    -0.058f,
                    layout.RoadInnerOffset - 0.060f,
                    PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Island", style.Island));

                if (!UsesMinimalRotaryCenter(layout.Preset.Id))
                {
                    CreateRotaryGarden(parent, layout, settings, style);
                }
            }

            CreateBoardingGate(parent, layout, settings, style);
        }

        private static void CreateRoad(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, BoardThemeStyle style)
        {
            var roadMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Road", style.Road);
            var railMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Guardrail", style.Rail);
            var railShadowMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Guardrail Shadow", style.RailShadow);
            var shadowMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Soft Shadow", style.RoadShadow);
            var railRimMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Guardrail Rim", style.Gate);

            const float railWidth = 0.104f;
            const float rimWidth = 0.018f;
            var innerOffset = layout.RoadInnerOffset;
            var outerOffset = layout.RoadOuterOffset;
            BoardGeometry.CreatePathBand("Rotary Soft Shadow", parent, layout, settings.RotaryCenterZ, -0.078f, innerOffset - 0.045f, outerOffset + 0.055f, shadowMaterial);
            BoardGeometry.CreatePathBand("Rotary Road", parent, layout, settings.RotaryCenterZ, -0.066f, innerOffset, outerOffset, roadMaterial);
            BoardGeometry.CreatePathBand("Outer Rotary Guardrail Shadow", parent, layout, settings.RotaryCenterZ, -0.050f, outerOffset - 0.006f, outerOffset + railWidth + 0.020f, railShadowMaterial);
            BoardGeometry.CreatePathBand("Outer Rotary Guardrail", parent, layout, settings.RotaryCenterZ, -0.026f, outerOffset, outerOffset + railWidth, railMaterial);
            BoardGeometry.CreatePathBand("Outer Rotary Guardrail Rim", parent, layout, settings.RotaryCenterZ, -0.015f, outerOffset + railWidth - rimWidth, outerOffset + railWidth, railRimMaterial);
            BoardGeometry.CreatePathBand("Inner Rotary Guardrail Shadow", parent, layout, settings.RotaryCenterZ, -0.049f, innerOffset - railWidth - 0.020f, innerOffset + 0.006f, railShadowMaterial);
            BoardGeometry.CreatePathBand("Inner Rotary Guardrail", parent, layout, settings.RotaryCenterZ, -0.025f, innerOffset - railWidth, innerOffset, railMaterial);
            BoardGeometry.CreatePathBand("Inner Rotary Guardrail Rim", parent, layout, settings.RotaryCenterZ, -0.014f, innerOffset - railWidth, innerOffset - railWidth + rimWidth, railRimMaterial);
        }

        private static bool UsesMinimalRotaryCenter(RotaryRoadPresetId presetId)
        {
            switch (presetId)
            {
                case RotaryRoadPresetId.HeartTest:
                case RotaryRoadPresetId.SmallCircleTest:
                case RotaryRoadPresetId.LargeCircleTest:
                case RotaryRoadPresetId.OvalTest:
                case RotaryRoadPresetId.RoundedSquareTest:
                case RotaryRoadPresetId.CloverTest:
                case RotaryRoadPresetId.DropTest:
                case RotaryRoadPresetId.CloudTest:
                case RotaryRoadPresetId.LoopTest:
                case RotaryRoadPresetId.ArrowTest:
                case RotaryRoadPresetId.RibbonTest:
                    return true;
                default:
                    return false;
            }
        }

        private static void CreateFeederLane(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, int side, BoardThemeStyle style)
        {
            var name = side < 0 ? "Left Passenger Feeder" : "Right Passenger Feeder";
            var logicFeederPath = layout.GetFeederPath(side);
            var roadFeederPath = CreateRenderedFeederPath(
                logicFeederPath,
                FeederRoadScreenExitExtension,
                layout.RoadWidth * FeederRoadJoinExtensionScale);
            var railFeederPath = CreateRenderedFeederPath(
                logicFeederPath,
                FeederRoadScreenExitExtension,
                layout.RoadWidth * FeederRailJoinExtensionScale);
            var laneMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} {name}", style.Road);
            var railMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} {name} Guardrail", style.Rail);
            var railShadowMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} {name} Guardrail Shadow", style.RailShadow);

            const float railWidth = 0.066f;
            var innerOffset = layout.RoadInnerOffset;
            var outerOffset = layout.RoadOuterOffset;
            BoardGeometry.CreateOpenPathBand(
                $"{name} Road",
                parent,
                layout,
                roadFeederPath,
                settings.RotaryCenterZ,
                -0.060f,
                innerOffset,
                outerOffset,
                laneMaterial,
                progress => layout.SampleFeederPath(side, roadFeederPath, progress));
            CreateFeederJunctionPatch($"{name} Junction Patch", parent, layout, logicFeederPath, settings, laneMaterial);
            BoardGeometry.CreateOpenPathBand(
                $"{name} Outer Rail Shadow",
                parent,
                layout,
                railFeederPath,
                settings.RotaryCenterZ,
                -0.042f,
                outerOffset - 0.004f,
                outerOffset + railWidth + 0.018f,
                railShadowMaterial,
                progress => layout.SampleFeederPath(side, railFeederPath, progress));
            BoardGeometry.CreateOpenPathBand(
                $"{name} Outer Rail",
                parent,
                layout,
                railFeederPath,
                settings.RotaryCenterZ,
                -0.024f,
                outerOffset,
                outerOffset + railWidth,
                railMaterial,
                progress => layout.SampleFeederPath(side, railFeederPath, progress));
            BoardGeometry.CreateOpenPathBand(
                $"{name} Inner Rail Shadow",
                parent,
                layout,
                railFeederPath,
                settings.RotaryCenterZ,
                -0.042f,
                innerOffset - railWidth - 0.018f,
                innerOffset + 0.004f,
                railShadowMaterial,
                progress => layout.SampleFeederPath(side, railFeederPath, progress));
            BoardGeometry.CreateOpenPathBand(
                $"{name} Inner Rail",
                parent,
                layout,
                railFeederPath,
                settings.RotaryCenterZ,
                -0.024f,
                innerOffset - railWidth,
                innerOffset,
                railMaterial,
                progress => layout.SampleFeederPath(side, railFeederPath, progress));
        }

        private static void CreateFeederJunctionPatch(
            string name,
            Transform parent,
            RotaryLayout layout,
            FeederRoadPath path,
            RotaryRoadBuildSettings settings,
            Material roadMaterial)
        {
            if (path == null || path.Points.Length < 2)
            {
                return;
            }

            var sample = path.Sample(1f);
            var tangent = new Vector3(sample.Tangent.x, 0f, sample.Tangent.y);
            tangent = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector3.forward;
            var center = layout.ToWorldPoint(
                sample.Point + sample.Tangent * layout.RoadWidth * 0.08f,
                settings.RotaryCenterZ,
                -0.058f);
            var width = layout.RoadWidth * FeederJunctionPatchWidthScale;
            var length = layout.RoadWidth * FeederJunctionPatchLengthScale;

            BoardGeometry.CreateFlatRoundedRect(
                name,
                parent,
                center,
                new Vector2(width, length),
                Mathf.Min(width, length) * 0.34f,
                roadMaterial,
                Quaternion.LookRotation(tangent, Vector3.up));
        }

        private static FeederRoadPath CreateRenderedFeederPath(FeederRoadPath path, float startExtension, float endExtension)
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

            var addEndExtension = endExtension > 0.001f;
            var points = new Vector2[path.Points.Length + (addEndExtension ? 2 : 1)];
            points[0] = start + extensionDirection * Mathf.Max(0f, startExtension);
            for (var index = 0; index < path.Points.Length; index++)
            {
                points[index + 1] = path.Points[index];
            }

            if (addEndExtension)
            {
                var end = path.Points[path.Points.Length - 1];
                var previous = path.Points[path.Points.Length - 2];
                var endDirection = end - previous;
                endDirection = endDirection.sqrMagnitude > 0.0001f ? endDirection.normalized : Vector2.up;
                points[points.Length - 1] = end + endDirection * endExtension;
            }

            return new FeederRoadPath(points);
        }

        private static float GetMaxAbsFeederX(RotaryLayout layout)
        {
            return Mathf.Max(GetMaxAbsPathX(layout.LeftFeederPath), GetMaxAbsPathX(layout.RightFeederPath));
        }

        private static float GetMaxFeederY(RotaryLayout layout)
        {
            return Mathf.Max(GetMaxPathY(layout.LeftFeederPath), GetMaxPathY(layout.RightFeederPath));
        }

        private static float GetMaxAbsPathX(FeederRoadPath path)
        {
            if (path == null)
            {
                return 0f;
            }

            var max = 0f;
            for (var index = 0; index < path.Points.Length; index++)
            {
                max = Mathf.Max(max, Mathf.Abs(path.Points[index].x));
            }

            return max;
        }

        private static float GetMaxPathY(FeederRoadPath path)
        {
            if (path == null)
            {
                return 0f;
            }

            var max = 0f;
            for (var index = 0; index < path.Points.Length; index++)
            {
                max = Mathf.Max(max, path.Points[index].y);
            }

            return max;
        }

        private static void CreateBoardingGate(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, BoardThemeStyle style)
        {
            var roadMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Boarding Gate Road", style.Road);
            var railMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Boarding Gate Guardrail", style.Rail);
            var railShadowMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Boarding Gate Guardrail Shadow", style.RailShadow);
            var markerMaterial = PuzzlePalette.CreateSolidMaterial($"{style.Name} Boarding Gate Marker", style.Gate);
            var sample = layout.Path.Sample(layout.Preset.BoardingGateProgress);
            var tangent = new Vector3(sample.Tangent.x, 0f, sample.Tangent.y).normalized;
            var outward = new Vector3(sample.Outward.x, 0f, sample.Outward.y).normalized;
            var outerOffset = layout.RoadOuterOffset;
            var centerPoint = layout.ToWorldPoint(sample.Point + sample.Outward * outerOffset, settings.RotaryCenterZ, 0f);
            var throatStart = centerPoint + outward * 0.04f;
            var throatEnd = centerPoint + outward * 0.17f;
            const float throatWidth = 0.50f;
            const float curbOffset = throatWidth * 0.5f + 0.045f;

            BoardGeometry.CreateFlatSegment(
                "Boarding Gate Shadow",
                parent,
                throatStart - outward * 0.03f,
                throatEnd + outward * 0.03f,
                -0.036f,
                throatWidth + 0.16f,
                railShadowMaterial);

            BoardGeometry.CreateFlatSegment(
                "Boarding Gate Throat",
                parent,
                throatStart,
                throatEnd,
                -0.018f,
                throatWidth,
                roadMaterial);

            BoardGeometry.CreateFlatSegment(
                "Boarding Gate Left Curb",
                parent,
                throatStart - tangent * curbOffset,
                throatEnd - tangent * curbOffset,
                -0.006f,
                0.052f,
                railMaterial);

            BoardGeometry.CreateFlatSegment(
                "Boarding Gate Right Curb",
                parent,
                throatStart + tangent * curbOffset,
                throatEnd + tangent * curbOffset,
                -0.006f,
                0.052f,
                railMaterial);

            BoardGeometry.CreateFlatSegment(
                "Boarding Gate Yellow Threshold",
                parent,
                throatEnd - outward * 0.030f,
                throatEnd + outward * 0.030f,
                -0.002f,
                throatWidth * 0.62f,
                markerMaterial);
        }

        private static void CreateRotaryGarden(Transform parent, RotaryLayout layout, RotaryRoadBuildSettings settings, BoardThemeStyle style)
        {
            var center = new Vector3(0f, -0.040f, settings.RotaryCenterZ);
            var gardenWidth = Mathf.Max(0.72f, layout.Path.RadiusX * 0.92f);
            var gardenDepth = Mathf.Max(0.42f, layout.Path.RadiusZ * 0.72f);
            var curbMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Garden Rim", style.Rail);
            var grassMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Garden Plate", style.Island);
            var bushMaterial = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Garden Node", style.AccentA);
            var flowerYellow = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Garden Mark A", style.AccentB);
            var flowerPink = PuzzlePalette.CreateSolidMaterial($"Rotary {style.Name} Garden Mark B", style.AccentC);

            BoardGeometry.CreateFlatRoundedRect(
                "Rotary Harbor Cargo Rim",
                parent,
                center + Vector3.down * 0.004f,
                new Vector2(gardenWidth + 0.16f, gardenDepth + 0.14f),
                gardenDepth * 0.46f,
                curbMaterial);

            BoardGeometry.CreateFlatRoundedRect(
                "Rotary Harbor Concrete Plate",
                parent,
                center + Vector3.up * 0.006f,
                new Vector2(gardenWidth, gardenDepth),
                gardenDepth * 0.44f,
                grassMaterial);

            CreateGardenBush(parent, "Rotary Bush 1", center + new Vector3(-gardenWidth * 0.22f, 0.072f, gardenDepth * 0.06f), 0.105f, bushMaterial);
            CreateGardenBush(parent, "Rotary Bush 2", center + new Vector3(gardenWidth * 0.18f, 0.072f, -gardenDepth * 0.10f), 0.092f, bushMaterial);
            CreateGardenBush(parent, "Rotary Bush 3", center + new Vector3(gardenWidth * 0.02f, 0.068f, gardenDepth * 0.18f), 0.075f, bushMaterial);
            CreateGardenFlower(parent, "Rotary Flower 1", center + new Vector3(-gardenWidth * 0.06f, 0.075f, -gardenDepth * 0.20f), flowerYellow);
            CreateGardenFlower(parent, "Rotary Flower 2", center + new Vector3(gardenWidth * 0.30f, 0.075f, gardenDepth * 0.12f), flowerPink);
            CreateGardenFlower(parent, "Rotary Flower 3", center + new Vector3(-gardenWidth * 0.32f, 0.075f, -gardenDepth * 0.02f), flowerPink);
        }

        private static void CreateGardenBush(Transform parent, string name, Vector3 position, float radius, Material material)
        {
            var bush = VisualPrimitiveFactory.Create(PrimitiveType.Sphere, name);
            bush.transform.SetParent(parent, false);
            bush.transform.position = position;
            bush.transform.localScale = new Vector3(radius * 1.20f, radius * 0.58f, radius);
            bush.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateGardenFlower(Transform parent, string name, Vector3 position, Material material)
        {
            BoardGeometry.CreateFlatRoundedRect(
                name,
                parent,
                position,
                new Vector2(0.055f, 0.040f),
                0.020f,
                material,
                Quaternion.Euler(0f, 25f, 0f));
        }

    }
}
