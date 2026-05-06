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
    /// Incremental sync — fetches only companies updated since <paramref name="since"/> and
    /// creates or updates Keka clients. Returns only newly created company-to-client mappings.
    /// </summary>
    Task<IReadOnlyList<SyncedCompanyEntry>> SyncCompaniesIncrementalAsync(DateTime since, CancellationToken cancellationToken = default);
}
