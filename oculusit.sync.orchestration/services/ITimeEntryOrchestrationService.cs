using oculusit.sync.connectwise.modules;

namespace oculusit.sync.orchestration.services;

public interface ITimeEntryOrchestrationService
{
    /// <summary>
    /// Resolves Keka project/task for a ConnectWise time entry and logs hours for the given employee email.
    /// Returns true when the entry was successfully posted to Keka; otherwise false.
    /// </summary>
    Task<bool> LogTimeEntryAsync(
        ConnectWiseTimeEntry entry,
        string employeeEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves Keka project/task for all provided ConnectWise time entries and posts them to Keka
    /// in a single batch call for the given employee email.
    /// Returns the count of entries successfully included in the batch.
    /// </summary>
    Task<int> LogTimeEntriesBatchAsync(
        IReadOnlyList<ConnectWiseTimeEntry> entries,
        string employeeEmail,
        CancellationToken cancellationToken = default);
}
