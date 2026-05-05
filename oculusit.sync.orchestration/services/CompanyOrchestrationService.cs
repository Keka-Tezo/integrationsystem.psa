using Microsoft.Extensions.Logging;
using oculusit.sync.connectwise.services;
using oculusit.sync.core.models;
using oculusit.sync.keka.modules;
using oculusit.sync.keka.services;
using oculusit.sync.orchestration.mappings;

namespace oculusit.sync.orchestration.services;

public sealed class CompanyOrchestrationService(
    IConnectWiseService connectWiseService,
    IKekaClientService kekaClientService,
    IKekaCurrencyService kekaCurrencyService,
    ILogger<CompanyOrchestrationService> logger) : ICompanyOrchestrationService
{
    public async Task<IReadOnlyList<SyncedCompanyEntry>> SyncCompaniesToKekaAsync(CancellationToken cancellationToken = default)
    {
        var companies = await connectWiseService.GetAllCompaniesAsync(cancellationToken);
        logger.LogInformation("Fetched {Count} companies from ConnectWise. Starting Keka sync.", companies.Count);

        // Fetch USD currency ID once before the loop
        var usdCurrencyId = await kekaCurrencyService.GetUsdCurrencyIdAsync(cancellationToken);
        if (usdCurrencyId is null)
            logger.LogWarning("USD currency ID not found in Keka. billingCurrencyId will be omitted.");

        // Fetch all Keka clients once and index by ConnectWise company ID (code).
        // Clients with a null code are ignored for sync purposes.
        var allKekaClients = await kekaClientService.GetAllClientsAsync(cancellationToken);
        var kekaClientsByCode = allKekaClients
            .Where(c => !string.IsNullOrEmpty(c.Code))
            .ToDictionary(c => c.Code!);

        logger.LogInformation("Fetched {Count} existing Keka clients. {Indexed} have a ConnectWise code.",
            allKekaClients.Count, kekaClientsByCode.Count);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var failed  = 0;

        var syncedEntries = new List<SyncedCompanyEntry>();

        foreach (var company in companies.Take(25))
        {
            try
            {
                var request = KekaClientMapper.MapToKekaClientRequest(company, usdCurrencyId);

                if (!kekaClientsByCode.TryGetValue(company.Id.ToString(), out var existing))
                {
                    var kekaClientId = await kekaClientService.CreateClientAsync(request, cancellationToken);
                    logger.LogInformation("Created Keka client for ConnectWise company {CompanyId} - {CompanyName}",
                        company.Id, company.Name);
                    created++;
                    syncedEntries.Add(new SyncedCompanyEntry
                    {
                        Id       = company.Id.ToString(),
                        ClientId = kekaClientId
                    });
                    continue;
                }
                else if (ShouldUpdateClient(existing, request))
                {
                    await kekaClientService.UpdateClientAsync(existing.Id, request, cancellationToken);
                    logger.LogInformation("Updated Keka client {KekaClientId} for ConnectWise company {CompanyId} - {CompanyName}",
                        existing.Id, company.Id, company.Name);
                    updated++;
                }
                else
                {
                    logger.LogDebug("Keka client {KekaClientId} for company {CompanyId} - {CompanyName} is unchanged, skipping.",
                        existing.Id, company.Id, company.Name);
                    skipped++;
                }

                syncedEntries.Add(new SyncedCompanyEntry
                {
                    Id       = company.Id.ToString(),
                    ClientId = existing.Id
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync ConnectWise company {CompanyId} - {CompanyName} to Keka",
                    company.Id, company.Name);
                failed++;
            }
        }

        logger.LogInformation(
            "Keka sync complete. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Failed: {Failed}",
            created, updated, skipped, failed);

        return syncedEntries;
    }

    private static bool ShouldUpdateClient(KekaClient existing, KekaClientRequest incoming)
    {
        if (existing.Name != incoming.Name) return true;
        if (existing.Description != incoming.Description) return true;
        if (existing.Code != incoming.Code.ToString()) return true;
        return false;
    }
}
