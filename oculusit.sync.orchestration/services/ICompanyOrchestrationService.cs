using oculusit.sync.core.models;

namespace oculusit.sync.orchestration;

public interface ICompanyOrchestrationService
{
    /// <summary>
    /// Syncs all ConnectWise companies to Keka — creates new clients or updates existing ones.
    /// Returns the list of company-to-client mappings processed during this run.
    /// </summary>
    Task<IReadOnlyList<SyncedCompanyEntry>> SyncCompaniesToKekaAsync(CancellationToken cancellationToken = default);
}
