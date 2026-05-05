using oculusit.sync.core.models;

namespace oculusit.sync.core.interfaces;

public interface ISyncStateService
{
    /// <summary>Returns the sync state for the given sync type, or null if none exists.</summary>
    Task<SyncState?> GetAsync(string syncType, CancellationToken cancellationToken = default);

    /// <summary>Persists the sync state for the given sync type.</summary>
    Task SaveAsync(SyncState state, CancellationToken cancellationToken = default);
}
