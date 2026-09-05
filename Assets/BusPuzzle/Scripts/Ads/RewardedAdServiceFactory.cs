namespace BusPuzzle
{
    internal static class RewardedAdServiceFactory
    {
        public static IRewardedAdService Create(
            LevelPlaySettings levelPlaySettings,
            AdMobSettings settings,
            System.Func<string> stageContextProvider)
        {
#if BUS_PUZZLE_LEVELPLAY && !UNITY_EDITOR
            var platformService = (IRewardedAdService)new LevelPlayRewardedAdService(levelPlaySettings);
#elif BUS_PUZZLE_ADMOB && !UNITY_EDITOR
            var platformService = (IRewardedAdService)new AdMobRewardedAdService(settings);
#else
            var platformService = (IRewardedAdService)new MockRewardedAdService(settings);
#endif
            var quotaLimitedService = new QuotaLimitedRewardedAdService(
                platformService,
                stageContextProvider,
                RemoteConfigService.GetRewardedAdQuotaPolicy);
            return new RemoteConfigRewardedAdService(quotaLimitedService);
        }
    }
}
