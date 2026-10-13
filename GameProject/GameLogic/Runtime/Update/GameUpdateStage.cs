namespace GameLogic
{
    public enum GameUpdateStage
    {
        Idle,
        CheckingVersion,
        WaitingForForceUpdate,
        InitializingResources,
        CheckingCatalogs,
        UpdatingCatalogs,
        CheckingDownloadSize,
        WaitingForHotUpdate,
        Downloading,
        Complete,
        Failed
    }
}
