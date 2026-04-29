using oculusit.sync.orchestration;

namespace oculusit.sync
{
    public class Worker(ILogger<Worker> logger, ICompanyOrchestrationService orchestration) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var companies = await orchestration.GetAllCompaniesAsync(stoppingToken);
            logger.LogInformation("Fetched {Count} companies from ConnectWise.", companies.Count);
        }
    }
}
