using oculusit.sync.connectwise.modules;
using oculusit.sync.connectwise.services;
using System;
using System.Collections.Generic;
using System.Text;

namespace oculusit.sync.orchestration.services
{
    public class CompanyOrchestrationService(IConnectWiseService connectWiseService) : ICompanyOrchestrationService
    {
        public async Task<IReadOnlyList<ConnectWiseCompany>> GetAllCompaniesAsync(CancellationToken cancellationToken = default)
        => await connectWiseService.GetAllCompaniesAsync(cancellationToken);
    }
}
