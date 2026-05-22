using oculusit.sync.keka.modules;

namespace oculusit.sync.keka.services;

public interface IKekaTimesheetEntryService
{
    /// <summary>
    /// Creates a Keka timesheet entry and returns the API response.
    /// </summary>
    Task<string> CreateTimesheetEntryAsync(
        string employeeId,
        KekaTimesheetEntryRequest request,
        CancellationToken cancellationToken = default);
}
