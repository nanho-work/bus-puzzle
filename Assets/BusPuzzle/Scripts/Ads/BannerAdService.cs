namespace BusPuzzle
{
    internal interface IBannerAdService
    {
        bool ShouldReserveSpace(int stageNumber);
        void Initialize();
        void Shutdown();
        void SetStage(int stageNumber);
    }
}
