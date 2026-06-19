using UnityEngine;

namespace BusPuzzle
{
    internal enum BoardThemeId
    {
        Field,
        Ice,
        Desert,
        Waikiki,
        Future,
        Harbor,
        Space
    }

    internal readonly struct BoardThemeStyle
    {
        public readonly BoardThemeId Id;
        public readonly string Name;
        public readonly Color Floor;
        public readonly Color RotaryDistrict;
        public readonly Color PuzzleYard;
        public readonly Color Road;
        public readonly Color RoadShadow;
        public readonly Color Rail;
        public readonly Color RailShadow;
        public readonly Color Gate;
        public readonly Color Island;
        public readonly Color AccentA;
        public readonly Color AccentB;
        public readonly Color AccentC;
        public readonly Color Pavement;
        public readonly Color PaverA;
        public readonly Color PaverB;
        public readonly Color Pole;
        public readonly Color Light;
        public readonly Color QueueGuide;
        public readonly Color QueueFloor;
        public readonly Color StationPlatform;
        public readonly Color StationBayRoad;
        public readonly Color StationBayTerminal;
        public readonly Color StationDivider;
        public readonly Color StationShadow;
        public readonly Color StationOutline;
        public readonly Color LockedSlot;
        public readonly Color YardSurface;
        public readonly Color YardPanelA;
        public readonly Color YardPanelB;
        public readonly Color YardLine;
        public readonly Color YardRoute;
        public readonly Color YardCheckpoint;
        public readonly Color YardTrack;
        public readonly Color PropShadow;

        public BoardThemeStyle(
            BoardThemeId id,
            string name,
            Color floor,
            Color rotaryDistrict,
            Color puzzleYard,
            Color road,
            Color roadShadow,
            Color rail,
            Color railShadow,
            Color gate,
            Color island,
            Color accentA,
            Color accentB,
            Color accentC,
            Color pavement,
            Color paverA,
            Color paverB,
            Color pole,
            Color light,
            Color queueGuide,
            Color queueFloor,
            Color stationPlatform,
            Color stationBayRoad,
            Color stationBayTerminal,
            Color stationDivider,
            Color stationShadow,
            Color stationOutline,
            Color lockedSlot,
            Color yardSurface,
            Color yardPanelA,
            Color yardPanelB,
            Color yardLine,
            Color yardRoute,
            Color yardCheckpoint,
            Color yardTrack,
            Color propShadow)
        {
            Id = id;
            Name = name;
            Floor = floor;
            RotaryDistrict = rotaryDistrict;
            PuzzleYard = puzzleYard;
            Road = road;
            RoadShadow = roadShadow;
            Rail = rail;
            RailShadow = railShadow;
            Gate = gate;
            Island = island;
            AccentA = accentA;
            AccentB = accentB;
            AccentC = accentC;
            Pavement = pavement;
            PaverA = paverA;
            PaverB = paverB;
            Pole = pole;
            Light = light;
            QueueGuide = queueGuide;
            QueueFloor = queueFloor;
            StationPlatform = stationPlatform;
            StationBayRoad = stationBayRoad;
            StationBayTerminal = stationBayTerminal;
            StationDivider = stationDivider;
            StationShadow = stationShadow;
            StationOutline = stationOutline;
            LockedSlot = lockedSlot;
            YardSurface = yardSurface;
            YardPanelA = yardPanelA;
            YardPanelB = yardPanelB;
            YardLine = yardLine;
            YardRoute = yardRoute;
            YardCheckpoint = yardCheckpoint;
            YardTrack = yardTrack;
            PropShadow = propShadow;
        }
    }

    internal static class BoardThemePalette
    {
        private static readonly BoardThemeId[] StageThemeCycle =
        {
            BoardThemeId.Field,
            BoardThemeId.Ice,
            BoardThemeId.Desert,
            BoardThemeId.Waikiki,
            BoardThemeId.Future,
            BoardThemeId.Harbor,
            BoardThemeId.Space
        };

        public static readonly Color FieldBase = new Color(0.38f, 0.57f, 0.39f);
        public static readonly Color FieldDark = new Color(0.28f, 0.48f, 0.31f);
        public static readonly Color FieldMid = new Color(0.43f, 0.64f, 0.42f);
        public static readonly Color FieldLight = new Color(0.51f, 0.70f, 0.48f);
        public static readonly Color FieldMuted = new Color(0.59f, 0.72f, 0.55f);
        public static readonly Color Road = new Color(0.34f, 0.40f, 0.44f);
        public static readonly Color RoadShadow = new Color(0.17f, 0.24f, 0.25f);
        public static readonly Color Curb = new Color(0.90f, 0.95f, 0.88f);
        public static readonly Color CurbShadow = new Color(0.28f, 0.37f, 0.31f);
        public static readonly Color Line = new Color(0.95f, 0.98f, 0.90f);
        public static readonly Color AccentYellow = new Color(0.98f, 0.78f, 0.16f);
        public static readonly Color Pole = new Color(0.35f, 0.39f, 0.38f);
        public static readonly Color WarmLight = new Color(1.00f, 0.92f, 0.62f);
        public static readonly Color Leaf = new Color(0.22f, 0.48f, 0.25f);
        public static readonly Color Trunk = new Color(0.40f, 0.27f, 0.14f);

        public static readonly Color IceBase = new Color(0.68f, 0.84f, 0.90f);
        public static readonly Color IceDeep = new Color(0.42f, 0.66f, 0.75f);
        public static readonly Color IceMid = new Color(0.58f, 0.78f, 0.86f);
        public static readonly Color IceLight = new Color(0.82f, 0.94f, 0.97f);
        public static readonly Color IceMuted = new Color(0.72f, 0.87f, 0.91f);
        public static readonly Color IceTrack = new Color(0.30f, 0.56f, 0.68f);
        public static readonly Color IceTrackShadow = new Color(0.17f, 0.34f, 0.42f);
        public static readonly Color IceBarrier = new Color(0.88f, 0.96f, 0.99f);
        public static readonly Color IceBarrierShadow = new Color(0.32f, 0.54f, 0.64f);
        public static readonly Color IceLine = new Color(0.94f, 0.99f, 1.00f);
        public static readonly Color IceLaneBlue = new Color(0.15f, 0.48f, 0.90f);
        public static readonly Color IceLaneRed = new Color(0.93f, 0.24f, 0.25f);
        public static readonly Color IceAccentOrange = new Color(1.00f, 0.52f, 0.12f);
        public static readonly Color IceSteel = new Color(0.29f, 0.42f, 0.50f);
        public static readonly Color IceGlow = new Color(0.78f, 0.95f, 1.00f);
        public static readonly Color IcePine = new Color(0.18f, 0.38f, 0.42f);
        public static readonly Color IceWood = new Color(0.45f, 0.34f, 0.26f);

        public static readonly Color DesertBase = new Color(0.72f, 0.57f, 0.34f);
        public static readonly Color DesertDeep = new Color(0.54f, 0.40f, 0.23f);
        public static readonly Color DesertMid = new Color(0.80f, 0.64f, 0.38f);
        public static readonly Color DesertLight = new Color(0.91f, 0.76f, 0.48f);
        public static readonly Color DesertMuted = new Color(0.67f, 0.55f, 0.38f);
        public static readonly Color DesertRoad = new Color(0.45f, 0.35f, 0.26f);
        public static readonly Color DesertRoadShadow = new Color(0.27f, 0.20f, 0.15f);
        public static readonly Color DesertCurb = new Color(0.86f, 0.74f, 0.52f);
        public static readonly Color DesertCurbShadow = new Color(0.39f, 0.29f, 0.19f);
        public static readonly Color DesertLine = new Color(1.00f, 0.88f, 0.58f);
        public static readonly Color DesertAccentOrange = new Color(1.00f, 0.45f, 0.12f);
        public static readonly Color DesertAccentTeal = new Color(0.10f, 0.64f, 0.70f);
        public static readonly Color DesertRed = new Color(0.82f, 0.20f, 0.14f);
        public static readonly Color DesertCactus = new Color(0.18f, 0.44f, 0.30f);
        public static readonly Color DesertWood = new Color(0.47f, 0.30f, 0.16f);
        public static readonly Color DesertStone = new Color(0.58f, 0.52f, 0.42f);
        public static readonly Color DesertMetal = new Color(0.36f, 0.34f, 0.31f);
        public static readonly Color DesertWater = new Color(0.10f, 0.55f, 0.70f);
        public static readonly Color DesertWaterLight = new Color(0.42f, 0.84f, 0.86f);

        public static readonly Color WaikikiSand = new Color(0.82f, 0.68f, 0.44f);
        public static readonly Color WaikikiSandLight = new Color(0.95f, 0.84f, 0.58f);
        public static readonly Color WaikikiSandMuted = new Color(0.72f, 0.61f, 0.43f);
        public static readonly Color WaikikiOcean = new Color(0.23f, 0.63f, 0.70f);
        public static readonly Color WaikikiOceanDeep = new Color(0.10f, 0.40f, 0.50f);
        public static readonly Color WaikikiLagoon = new Color(0.40f, 0.83f, 0.84f);
        public static readonly Color WaikikiBoardwalk = new Color(0.50f, 0.33f, 0.20f);
        public static readonly Color WaikikiBoardwalkLight = new Color(0.67f, 0.45f, 0.27f);
        public static readonly Color WaikikiRoad = new Color(0.37f, 0.46f, 0.44f);
        public static readonly Color WaikikiRoadShadow = new Color(0.16f, 0.27f, 0.28f);
        public static readonly Color WaikikiCurb = new Color(0.94f, 0.86f, 0.66f);
        public static readonly Color WaikikiCurbShadow = new Color(0.38f, 0.31f, 0.20f);
        public static readonly Color WaikikiLine = new Color(1.00f, 0.93f, 0.70f);
        public static readonly Color WaikikiCoral = new Color(1.00f, 0.36f, 0.30f);
        public static readonly Color WaikikiSun = new Color(1.00f, 0.76f, 0.20f);
        public static readonly Color WaikikiPalm = new Color(0.18f, 0.46f, 0.25f);
        public static readonly Color WaikikiPalmLight = new Color(0.30f, 0.62f, 0.31f);
        public static readonly Color WaikikiTrunk = new Color(0.47f, 0.29f, 0.13f);
        public static readonly Color WaikikiUmbrellaBlue = new Color(0.12f, 0.54f, 0.78f);
        public static readonly Color WaikikiWhite = new Color(0.97f, 0.96f, 0.86f);

        public static readonly Color FutureFloor = new Color(0.08f, 0.15f, 0.19f);
        public static readonly Color FutureDistrict = new Color(0.13f, 0.25f, 0.31f);
        public static readonly Color FuturePanel = new Color(0.17f, 0.31f, 0.37f);
        public static readonly Color FuturePanelLight = new Color(0.25f, 0.45f, 0.52f);
        public static readonly Color FuturePanelDark = new Color(0.06f, 0.12f, 0.16f);
        public static readonly Color FutureRoad = new Color(0.12f, 0.18f, 0.23f);
        public static readonly Color FutureRoadShadow = new Color(0.02f, 0.05f, 0.08f);
        public static readonly Color FutureRail = new Color(0.66f, 0.92f, 0.96f);
        public static readonly Color FutureRailShadow = new Color(0.08f, 0.20f, 0.26f);
        public static readonly Color FutureNeonBlue = new Color(0.20f, 0.88f, 1.00f);
        public static readonly Color FutureNeonPink = new Color(1.00f, 0.24f, 0.70f);
        public static readonly Color FutureNeonGreen = new Color(0.30f, 1.00f, 0.62f);
        public static readonly Color FutureAmber = new Color(1.00f, 0.70f, 0.18f);
        public static readonly Color FutureGlass = new Color(0.52f, 0.92f, 1.00f);
        public static readonly Color FutureSteel = new Color(0.34f, 0.46f, 0.52f);

        public static readonly Color HarborWater = new Color(0.08f, 0.24f, 0.32f);
        public static readonly Color HarborConcrete = new Color(0.43f, 0.48f, 0.47f);
        public static readonly Color HarborConcreteLight = new Color(0.56f, 0.61f, 0.59f);
        public static readonly Color HarborYardSurface = new Color(0.31f, 0.37f, 0.36f);
        public static readonly Color HarborAsphalt = new Color(0.20f, 0.24f, 0.25f);
        public static readonly Color HarborAsphaltDark = new Color(0.10f, 0.13f, 0.14f);
        public static readonly Color HarborLine = new Color(0.92f, 0.93f, 0.84f);
        public static readonly Color HarborSafetyYellow = new Color(1.00f, 0.75f, 0.12f);
        public static readonly Color HarborContainerRed = new Color(0.78f, 0.19f, 0.14f);
        public static readonly Color HarborContainerBlue = new Color(0.13f, 0.35f, 0.58f);
        public static readonly Color HarborContainerOrange = new Color(0.92f, 0.43f, 0.13f);
        public static readonly Color HarborContainerGreen = new Color(0.18f, 0.47f, 0.32f);
        public static readonly Color HarborCrane = new Color(0.95f, 0.64f, 0.10f);
        public static readonly Color HarborSteel = new Color(0.32f, 0.38f, 0.40f);
        public static readonly Color HarborLight = new Color(1.00f, 0.90f, 0.52f);

        public static readonly Color SpaceBase = new Color(0.04f, 0.05f, 0.12f);
        public static readonly Color SpacePanel = new Color(0.12f, 0.14f, 0.28f);
        public static readonly Color SpacePanelLight = new Color(0.24f, 0.26f, 0.45f);
        public static readonly Color SpaceYardSurface = new Color(0.18f, 0.20f, 0.34f);
        public static readonly Color SpaceRoad = new Color(0.09f, 0.10f, 0.18f);
        public static readonly Color SpaceLine = new Color(0.62f, 0.82f, 1.00f);
        public static readonly Color SpaceGlowBlue = new Color(0.25f, 0.68f, 1.00f);
        public static readonly Color SpaceGlowPurple = new Color(0.74f, 0.42f, 1.00f);
        public static readonly Color SpaceMoonDust = new Color(0.55f, 0.57f, 0.64f);

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static BoardThemeId GetThemeForStage(int stageNumber)
        {
            var zeroBasedStage = Mathf.Max(0, stageNumber - 1);
            var cycleIndex = (zeroBasedStage / 10) % StageThemeCycle.Length;
            return StageThemeCycle[cycleIndex];
        }

        public static BoardThemeStyle GetStyle(BoardThemeId theme)
        {
            switch (theme)
            {
                case BoardThemeId.Ice:
                    return new BoardThemeStyle(
                        BoardThemeId.Ice,
                        "Ice Rink",
                        IceBase,
                        IceMid,
                        IceDeep,
                        IceTrack,
                        IceTrackShadow,
                        IceBarrier,
                        IceBarrierShadow,
                        IceAccentOrange,
                        IceLight,
                        IceLaneBlue,
                        IceLaneRed,
                        IceGlow,
                        IceMuted,
                        IceLight,
                        IceMid,
                        IceSteel,
                        IceGlow,
                        WithAlpha(IceLine, 0.42f),
                        WithAlpha(IceDeep, 0.14f),
                        IceMid,
                        IceTrack,
                        WithAlpha(IceLine, 0.16f),
                        WithAlpha(IceLine, 0.26f),
                        WithAlpha(IceTrackShadow, 0.18f),
                        WithAlpha(IceLaneBlue, 0.58f),
                        IceMuted,
                        IceBase,
                        WithAlpha(IceLight, 0.24f),
                        WithAlpha(IceDeep, 0.16f),
                        WithAlpha(IceLine, 0.66f),
                        WithAlpha(IceLaneBlue, 0.30f),
                        WithAlpha(IceLaneRed, 0.46f),
                        WithAlpha(IceTrackShadow, 0.28f),
                        WithAlpha(IceTrackShadow, 0.20f));
                case BoardThemeId.Desert:
                    return new BoardThemeStyle(
                        BoardThemeId.Desert,
                        "Desert Yard",
                        DesertBase,
                        DesertMid,
                        DesertMuted,
                        DesertRoad,
                        DesertRoadShadow,
                        DesertCurb,
                        DesertCurbShadow,
                        DesertAccentOrange,
                        DesertLight,
                        DesertCactus,
                        DesertRed,
                        DesertAccentTeal,
                        DesertLight,
                        DesertMid,
                        DesertMuted,
                        DesertMetal,
                        DesertLine,
                        WithAlpha(DesertLine, 0.42f),
                        WithAlpha(DesertWater, 0.16f),
                        DesertMid,
                        DesertRoad,
                        WithAlpha(DesertLine, 0.14f),
                        WithAlpha(DesertLine, 0.24f),
                        WithAlpha(DesertRoadShadow, 0.22f),
                        WithAlpha(DesertAccentOrange, 0.62f),
                        DesertStone,
                        DesertMid,
                        WithAlpha(DesertLight, 0.24f),
                        WithAlpha(DesertMuted, 0.16f),
                        WithAlpha(DesertLine, 0.62f),
                        WithAlpha(DesertAccentOrange, 0.30f),
                        WithAlpha(DesertAccentTeal, 0.42f),
                        WithAlpha(DesertRoadShadow, 0.28f),
                        WithAlpha(DesertRoadShadow, 0.20f));
                case BoardThemeId.Waikiki:
                    return new BoardThemeStyle(
                        BoardThemeId.Waikiki,
                        "Waikiki",
                        WaikikiOcean,
                        WaikikiSandLight,
                        WaikikiSand,
                        WaikikiRoad,
                        WaikikiRoadShadow,
                        WaikikiCurb,
                        WaikikiCurbShadow,
                        WaikikiSun,
                        WaikikiSandLight,
                        WaikikiPalm,
                        WaikikiCoral,
                        WaikikiUmbrellaBlue,
                        WaikikiSandLight,
                        WaikikiSand,
                        WaikikiBoardwalkLight,
                        WaikikiTrunk,
                        WaikikiWhite,
                        WithAlpha(WaikikiSun, 0.42f),
                        WithAlpha(WaikikiLagoon, 0.18f),
                        WaikikiBoardwalkLight,
                        WaikikiRoad,
                        WithAlpha(WaikikiWhite, 0.16f),
                        WithAlpha(WaikikiWhite, 0.26f),
                        WithAlpha(WaikikiRoadShadow, 0.22f),
                        WithAlpha(WaikikiSun, 0.62f),
                        WaikikiSandMuted,
                        WaikikiSand,
                        WithAlpha(WaikikiSandLight, 0.24f),
                        WithAlpha(WaikikiOcean, 0.16f),
                        WithAlpha(WaikikiLine, 0.64f),
                        WithAlpha(WaikikiSun, 0.30f),
                        WithAlpha(WaikikiCoral, 0.42f),
                        WithAlpha(WaikikiRoadShadow, 0.26f),
                        WithAlpha(WaikikiRoadShadow, 0.20f));
                case BoardThemeId.Future:
                    return new BoardThemeStyle(
                        BoardThemeId.Future,
                        "Future City",
                        FutureFloor,
                        FutureDistrict,
                        FuturePanel,
                        FutureRoad,
                        FutureRoadShadow,
                        FutureRail,
                        FutureRailShadow,
                        FutureAmber,
                        FuturePanelLight,
                        FutureNeonBlue,
                        FutureNeonPink,
                        FutureNeonGreen,
                        FuturePanelLight,
                        FuturePanel,
                        FuturePanelDark,
                        FutureSteel,
                        FutureGlass,
                        WithAlpha(FutureNeonBlue, 0.42f),
                        WithAlpha(FutureGlass, 0.16f),
                        FuturePanel,
                        FutureRoad,
                        WithAlpha(FutureRail, 0.16f),
                        WithAlpha(FutureRail, 0.28f),
                        WithAlpha(FutureRoadShadow, 0.20f),
                        WithAlpha(FutureNeonBlue, 0.62f),
                        FuturePanelDark,
                        FuturePanel,
                        WithAlpha(FuturePanelLight, 0.24f),
                        WithAlpha(FutureFloor, 0.16f),
                        WithAlpha(FutureRail, 0.62f),
                        WithAlpha(FutureNeonBlue, 0.30f),
                        WithAlpha(FutureNeonPink, 0.42f),
                        WithAlpha(FutureRoadShadow, 0.32f),
                        WithAlpha(FutureRoadShadow, 0.22f));
                case BoardThemeId.Harbor:
                    return new BoardThemeStyle(
                        BoardThemeId.Harbor,
                        "Harbor Yard",
                        HarborWater,
                        HarborConcreteLight,
                        HarborConcrete,
                        HarborAsphalt,
                        HarborAsphaltDark,
                        HarborLine,
                        HarborAsphaltDark,
                        HarborSafetyYellow,
                        HarborConcreteLight,
                        HarborContainerBlue,
                        HarborContainerRed,
                        HarborContainerOrange,
                        HarborConcreteLight,
                        HarborConcrete,
                        HarborAsphalt,
                        HarborSteel,
                        HarborLight,
                        WithAlpha(HarborSafetyYellow, 0.42f),
                        WithAlpha(HarborWater, 0.14f),
                        HarborConcrete,
                        HarborAsphalt,
                        WithAlpha(HarborLine, 0.16f),
                        WithAlpha(HarborLine, 0.28f),
                        WithAlpha(HarborAsphaltDark, 0.24f),
                        WithAlpha(HarborSafetyYellow, 0.68f),
                        HarborConcreteLight,
                        HarborYardSurface,
                        WithAlpha(HarborConcreteLight, 0.20f),
                        WithAlpha(HarborWater, 0.12f),
                        WithAlpha(HarborLine, 0.60f),
                        WithAlpha(HarborSafetyYellow, 0.28f),
                        WithAlpha(HarborContainerOrange, 0.54f),
                        WithAlpha(HarborAsphaltDark, 0.32f),
                        WithAlpha(HarborAsphaltDark, 0.22f));
                case BoardThemeId.Space:
                    return new BoardThemeStyle(
                        BoardThemeId.Space,
                        "Space Dock",
                        SpaceBase,
                        SpacePanel,
                        SpacePanelLight,
                        SpaceRoad,
                        SpaceBase,
                        SpaceLine,
                        SpacePanel,
                        SpaceGlowPurple,
                        SpacePanelLight,
                        SpaceGlowBlue,
                        SpaceGlowPurple,
                        SpaceMoonDust,
                        SpacePanelLight,
                        SpacePanel,
                        SpaceRoad,
                        SpaceMoonDust,
                        SpaceLine,
                        WithAlpha(SpaceGlowBlue, 0.42f),
                        WithAlpha(SpaceGlowPurple, 0.16f),
                        SpacePanel,
                        SpaceRoad,
                        WithAlpha(SpaceLine, 0.16f),
                        WithAlpha(SpaceLine, 0.30f),
                        WithAlpha(SpaceBase, 0.28f),
                        WithAlpha(SpaceGlowBlue, 0.66f),
                        SpacePanelLight,
                        SpaceYardSurface,
                        WithAlpha(SpacePanelLight, 0.24f),
                        WithAlpha(SpaceBase, 0.14f),
                        WithAlpha(SpaceLine, 0.64f),
                        WithAlpha(SpaceGlowBlue, 0.30f),
                        WithAlpha(SpaceGlowPurple, 0.46f),
                        WithAlpha(SpaceBase, 0.34f),
                        WithAlpha(SpaceBase, 0.24f));
                default:
                    return new BoardThemeStyle(
                        BoardThemeId.Field,
                        "Field",
                        FieldBase,
                        FieldMid,
                        FieldDark,
                        Road,
                        RoadShadow,
                        Curb,
                        CurbShadow,
                        AccentYellow,
                        FieldLight,
                        Leaf,
                        AccentYellow,
                        WarmLight,
                        FieldMuted,
                        FieldMid,
                        FieldDark,
                        Pole,
                        WarmLight,
                        WithAlpha(AccentYellow, 0.42f),
                        WithAlpha(FieldLight, 0.16f),
                        FieldMid,
                        Road,
                        WithAlpha(Line, 0.14f),
                        WithAlpha(Line, 0.26f),
                        WithAlpha(RoadShadow, 0.22f),
                        WithAlpha(AccentYellow, 0.62f),
                        FieldLight,
                        FieldDark,
                        WithAlpha(FieldLight, 0.22f),
                        WithAlpha(FieldBase, 0.16f),
                        WithAlpha(Line, 0.60f),
                        WithAlpha(AccentYellow, 0.28f),
                        WithAlpha(WarmLight, 0.42f),
                        WithAlpha(RoadShadow, 0.26f),
                        WithAlpha(RoadShadow, 0.20f));
            }
        }
    }
}
