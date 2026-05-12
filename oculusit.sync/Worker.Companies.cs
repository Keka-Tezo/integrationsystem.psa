using oculusit.sync.core.models;

namespace oculusit.sync;

public sealed partial class Worker
{
    private async Task SyncCompaniesAsync(DateTime syncStartedAt, CancellationToken stoppingToken)
    {
        var syncState = await syncStateService.GetAsync(SyncTypes.Company, stoppingToken);

        if (syncState is null)
        {
            logger.LogInformation("No previous sync state found in DynamoDB. Running full company sync.");

            var syncedEntries = await companyOrchestration.SyncCompaniesToKekaAsync(stoppingToken);

            await syncStateService.SaveAsync(new SyncState
            {
                SyncType      = SyncTypes.Company,
                Companies     = syncedEntries,
                LastUpdatedAt = syncStartedAt
            }, stoppingToken);

            logger.LogInformation("Full company sync complete. {Count} company mappings saved.", syncedEntries.Count);
        }
        else
        {
            logger.LogInformation("Incremental company sync. Last sync was at {LastUpdatedAt}.", syncState.LastUpdatedAt);

            var newEntries = await companyOrchestration.SyncCompaniesIncrementalAsync(syncState, stoppingToken);

            await syncStateService.AppendCompaniesAsync(SyncTypes.Company, newEntries, syncStartedAt, stoppingToken);

            logger.LogInformation("Incremental company sync complete. {Count} new mappings appended.", newEntries.Count);
        }
    }
}
