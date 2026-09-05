namespace BusPuzzle
{
    internal static class BannerAdServiceFactory
    {
        public static IBannerAdService Create(LevelPlaySettings levelPlaySettings, AdMobSettings adMobSettings)
        {
#if BUS_PUZZLE_LEVELPLAY && !UNITY_EDITOR
            return new LevelPlayBannerAdService(levelPlaySettings);
#elif BUS_PUZZLE_ADMOB && !UNITY_EDITOR
            return new AdMobBannerAdService(adMobSettings);
#else
            return new MockBannerAdService(adMobSettings);
#endif
        }
    }
}
