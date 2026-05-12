using oculusit.sync.core.interfaces;
using oculusit.sync.orchestration;

namespace oculusit.sync;

public sealed partial class Worker(
    ILogger<Worker> logger,
    IHostApplicationLifetime lifetime,
    ICompanyOrchestrationService companyOrchestration,
    IProjectOrchestrationService projectOrchestration,
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

            await SyncCompaniesAsync(syncStartedAt, stoppingToken);
            await SyncProjectsAsync(syncStartedAt, stoppingToken);

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
