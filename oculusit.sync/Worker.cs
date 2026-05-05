using oculusit.sync.core.interfaces;
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

            var syncState = await syncStateService.GetAsync("company", stoppingToken);
            if (syncState is null)
            {
                logger.LogInformation("No previous sync state found in DynamoDB. This is a fresh run.");
                await orchestration.SyncCompaniesToKekaAsync(stoppingToken);
            }                
            else
                logger.LogInformation("Last successful company sync was at {LastSyncedAt}.", syncState.LastSyncedAt);
                      
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
