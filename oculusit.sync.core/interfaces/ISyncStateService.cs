using oculusit.sync.core.models;

namespace oculusit.sync.core.interfaces;

public interface ISyncStateService
{
    /// <summary>Returns the sync state for the given sync type, or null if none exists.</summary>
    Task<SyncState?> GetAsync(string syncType, CancellationToken cancellationToken = default);

    /// <summary>Persists the sync state for the given sync type.</summary>
    Task SaveAsync(SyncState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends new company entries to the existing Companies list and updates LastUpdatedAt.
    /// </summary>
    Task AppendCompaniesAsync(string syncType, IReadOnlyList<SyncedCompanyEntry> newEntries, DateTime lastUpdatedAt, CancellationToken cancellationToken = default);
}
