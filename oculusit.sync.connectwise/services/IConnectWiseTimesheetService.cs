using oculusit.sync.connectwise.modules;

namespace oculusit.sync.connectwise.services;

public interface IConnectWiseTimesheetService
{
    /// <summary>
    /// Fetches a ConnectWise timesheet by timesheet ID.
    /// </summary>
    /// <param name="timesheetId">The timesheet ID to fetch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The timesheet if found; null otherwise</returns>
    Task<ConnectWiseTimesheet?> GetTimesheetByIdAsync(
        int timesheetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all ConnectWise timesheets for a specific employee within a date range.
    /// </summary>
    /// <param name="employeeId">The employee ID (member ID) to fetch timesheets for</param>
    /// <param name="startDate">Start date filter (inclusive)</param>
    /// <param name="endDate">End date filter (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of timesheets matching the criteria</returns>
    Task<IReadOnlyList<ConnectWiseTimesheet>> GetTimesheetsByEmployeeAndDateRangeAsync(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all ConnectWise timesheets for a specific week and year.
    /// </summary>
    /// <param name="week">ISO week number (1-53)</param>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of timesheets for the specified week</returns>
    Task<IReadOnlyList<ConnectWiseTimesheet>> GetTimesheetsByWeekAsync(
        int week,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all ConnectWise timesheets with billable/approval statuses updated after the given timestamp.
    /// Statuses: PendingApproval, ErrorsCorrected, PendingProjectApproval, ApprovedByTierOne,
    /// ApprovedByTierTwo, ReadyToBill, Billed, BilledAgreement.
    /// Results are ordered by lastUpdated ascending and paged at 200 records per request.
    /// </summary>
    /// <param name="lastUpdatedSince">Fetch timesheets with lastUpdated strictly after this UTC timestamp.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All matching timesheets across all pages.</returns>
    Task<IReadOnlyList<ConnectWiseTimesheet>> GetTimesheetsSinceAsync(
        DateTime lastUpdatedSince,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the audit trail for a specific timesheet ID.
    /// Audit trail includes all changes, approvals, rejections, and submissions.
    /// </summary>
    /// <param name="timesheetId">The timesheet ID to fetch audit trail for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit trail entries for the timesheet</returns>
    Task<IReadOnlyList<ConnectWiseTimesheetAuditTrail>> GetTimesheetAuditTrailAsync(
        int timesheetId,
        CancellationToken cancellationToken = default);
}
