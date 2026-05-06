using oculusit.sync.core.interfaces;
using oculusit.sync.core.models;
using oculusit.sync.orchestration;

namespace oculusit.sync;

public sealed class Worker(
    ILogger<Worker> logger,
    IHostApplicationLifetime lifetime,
    ICompanyOrchestrationService orchestration,
    ISyncStateService syncStateService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Worker started. Beginning ConnectWise to Keka sync.");
            // Capture start time before any data is fetched so mid-run changes are
            // included in the next run's window.
            var syncStartedAt = DateTime.UtcNow;
           
            var syncState = await syncStateService.GetAsync("Company", stoppingToken);
            if (syncState is null)
            {
                logger.LogInformation("No previous sync state found in DynamoDB. This is a fresh run.");
                var syncedEntries = await orchestration.SyncCompaniesToKekaAsync(stoppingToken);

                await syncStateService.SaveAsync(new SyncState
                {
                    SyncType      = "Company",
                    Companies     = syncedEntries,
                    LastUpdatedAt = syncStartedAt
                }, stoppingToken);

                logger.LogInformation("Sync state saved. {Count} company mappings recorded.", syncedEntries.Count);
            }
            else
            {
                logger.LogInformation("Incremental run. Last sync was at {LastUpdatedAt}.", syncState.LastUpdatedAt);
                var newEntries = await orchestration.SyncCompaniesIncrementalAsync(syncState.LastUpdatedAt!.Value, stoppingToken);

                await syncStateService.AppendCompaniesAsync("Company", newEntries, syncStartedAt, stoppingToken);

                logger.LogInformation("Incremental sync state updated. {Count} new company mappings appended.", newEntries.Count);
            }
            
                      
            logger.LogInformation("Sync complete. Worker shutting down.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("Worker was cancelled before sync completed.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An unhandled exception terminated the sync worker.");
            // Stop the host so the process exits with a non-zero code,
            // which signals ECS/container orchestrators to restart the task.
            lifetime.StopApplication();
        }
    }
}
