namespace oculusit.sync.core.models;

public sealed class SyncState
{
    /// <summary>Partition key — identifies the type of sync (e.g. "Company").</summary>
    public string SyncType { get; init; } = string.Empty;

    /// <summary>Company-to-Keka client mappings captured during the sync run.</summary>
    public IReadOnlyList<SyncedCompanyEntry> Companies { get; init; } = [];

    /// <summary>UTC timestamp of the last successful sync completion.</summary>
    public DateTime? LastUpdatedAt { get; init; }
}

/// <summary>Records the mapping between a ConnectWise company ID and its Keka client ID.</summary>
public sealed class SyncedCompanyEntry
{
    /// <summary>ConnectWise company ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Keka client ID.</summary>
    public string ClientId { get; init; } = string.Empty;
}
