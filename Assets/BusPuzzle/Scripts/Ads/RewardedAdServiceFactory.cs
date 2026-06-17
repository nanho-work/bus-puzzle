namespace BusPuzzle
{
    internal static class RewardedAdServiceFactory
    {
        public static IRewardedAdService Create(AdMobSettings settings)
        {
#if BUS_PUZZLE_ADMOB && !UNITY_EDITOR
            return new RemoteConfigRewardedAdService(new AdMobRewardedAdService(settings));
#else
            return new RemoteConfigRewardedAdService(new MockRewardedAdService(settings));
#endif
        }
    }
}
