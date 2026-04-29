using oculusit.sync.connectwise.modules;

namespace oculusit.sync.connectwise.services;

public interface IConnectWiseService
{
    /// <summary>
    /// Fetches all companies from ConnectWise by paginating through all pages
    /// and returns the complete list stored in memory.
    /// </summary>
    Task<IReadOnlyList<ConnectWiseCompany>> GetAllCompaniesAsync(CancellationToken cancellationToken = default);
}