using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oculusit.sync.connectwise.modules;

namespace oculusit.sync.orchestration;

public interface ICompanyOrchestrationService
{
    Task<IReadOnlyList<ConnectWiseCompany>> GetAllCompaniesAsync(CancellationToken cancellationToken = default);
}