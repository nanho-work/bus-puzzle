namespace BusPuzzle
{
    internal static class BannerAdServiceFactory
    {
        public static IBannerAdService Create(AdMobSettings settings)
        {
#if BUS_PUZZLE_ADMOB && !UNITY_EDITOR
            return new AdMobBannerAdService(settings);
#else
            return new MockBannerAdService(settings);
#endif
        }
    }
}
