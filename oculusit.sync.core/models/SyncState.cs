namespace oculusit.sync.core.models;

public sealed class SyncState
{
    /// <summary>Partition key — identifies the type of sync (e.g. "company").</summary>
    public string SyncType { get; init; } = string.Empty;

    /// <summary>UTC timestamp of the last successful sync completion.</summary>
    public DateTime? LastSyncedAt { get; init; }

    /// <summary>ISO-8601 string stored in DynamoDB.</summary>
    public string? LastSyncedAtIso => LastSyncedAt?.ToString("o");
}
