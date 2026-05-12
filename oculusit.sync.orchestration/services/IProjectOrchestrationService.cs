using oculusit.sync.core.models;

namespace oculusit.sync.orchestration;

public interface IProjectOrchestrationService
{
    /// <summary>
    /// Full sync — fetches all ConnectWise projects and records them.
    /// <paramref name="companySyncState"/> is used to resolve the Keka client ID
    /// from the ConnectWise company ID on each project.
    /// </summary>
    Task<ProjectSyncResult> SyncProjectsAsync(
        SyncState companySyncState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Incremental sync — fetches only projects updated since <paramref name="projectSyncState"/>.LastUpdatedAt.
    /// <paramref name="companySyncState"/> is used to resolve the Keka client ID.
    /// Returns newly created entries and any failures.
    /// </summary>
    Task<ProjectSyncResult> SyncProjectsIncrementalAsync(
        SyncState projectSyncState,
        SyncState companySyncState,
        CancellationToken cancellationToken = default);
}
