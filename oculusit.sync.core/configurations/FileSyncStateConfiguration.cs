namespace oculusit.sync.core.configurations;

public sealed class FileSyncStateConfiguration
{
    public const string SectionName = "SyncState";

    /// <summary>Directory where one JSON file per sync-state record is stored.</summary>
    public string DataDirectory { get; init; } = "/data/sync-state";
}
