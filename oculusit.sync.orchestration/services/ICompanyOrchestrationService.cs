using oculusit.sync.core.models;

namespace oculusit.sync.orchestration;

public interface ICompanyOrchestrationService
{
    /// <summary>
    /// Full sync — fetches all ConnectWise companies and creates or updates Keka clients.
    /// Returns all company-to-client mappings.
    /// </summary>
    Task<IReadOnlyList<SyncedCompanyEntry>> SyncCompaniesToKekaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Incremental sync — fetches only companies updated since <paramref name="syncState"/>.LastUpdatedAt
    /// and creates or updates Keka clients.
    /// Existing mappings are resolved from <paramref name="syncState"/>.Companies (no Keka list fetch).
    /// Returns only newly created company-to-client mappings.
    /// </summary>
    Task<IReadOnlyList<SyncedCompanyEntry>> SyncCompaniesIncrementalAsync(SyncState syncState, CancellationToken cancellationToken = default);
}
