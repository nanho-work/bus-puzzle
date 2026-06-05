namespace BusPuzzle
{
    internal static class RewardedAdServiceFactory
    {
        public static IRewardedAdService Create(AdMobSettings settings)
        {
#if BUS_PUZZLE_ADMOB
            return new AdMobRewardedAdService(settings);
#else
            return new MockRewardedAdService(settings);
#endif
        }
    }
}
