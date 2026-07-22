using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using oculusit.sync.core.configurations;
using oculusit.sync.core.interfaces;
using oculusit.sync.core.models;

namespace oculusit.sync.core.services;

/// <summary>
/// File-backed <see cref="ISyncStateService"/> implementation. Stores one JSON document per syncType
/// under <see cref="FileSyncStateConfiguration.DataDirectory"/> — no external database dependency.
/// Reads/writes are serialized per file via an in-process lock, which is sufficient because this
/// service is registered as a singleton inside a single worker process.
/// </summary>
public sealed class FileSyncStateService(
    IOptions<FileSyncStateConfiguration> options,
    ILogger<FileSyncStateService> logger) : ISyncStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _dataDirectory = EnsureDirectory(options.Value.DataDirectory);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private string PathFor(string syncType) => Path.Combine(_dataDirectory, $"{syncType}.json");

    private SemaphoreSlim GetLock(string path) => _locks.GetOrAdd(path, static _ => new SemaphoreSlim(1, 1));

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return default;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        // Write to a temp file then move into place so a container kill mid-write can never leave a
        // truncated/corrupt JSON file behind.
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private async Task<T?> ReadJsonLockedAsync<T>(string path, CancellationToken cancellationToken)
    {
        var sem = GetLock(path);
        await sem.WaitAsync(cancellationToken);
        try
        {
            return await ReadJsonAsync<T>(path, cancellationToken);
        }
        finally
        {
            sem.Release();
        }
    }

    private async Task WriteJsonLockedAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var sem = GetLock(path);
        await sem.WaitAsync(cancellationToken);
        try
        {
            await WriteJsonAsync(path, value, cancellationToken);
        }
        finally
        {
            sem.Release();
        }
    }

    private async Task UpdateJsonAsync<T>(string path, Func<T?, T> mutate, CancellationToken cancellationToken)
        where T : class
    {
        var sem = GetLock(path);
        await sem.WaitAsync(cancellationToken);
        try
        {
            var existing = await ReadJsonAsync<T>(path, cancellationToken);
            await WriteJsonAsync(path, mutate(existing), cancellationToken);
        }
        finally
        {
            sem.Release();
        }
    }

    private async Task EnsureExistsAsync<T>(string path, Func<T> createDefault, CancellationToken cancellationToken)
        where T : class
    {
        var sem = GetLock(path);
        await sem.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(path))
                return;

            await WriteJsonAsync(path, createDefault(), cancellationToken);
        }
        finally
        {
            sem.Release();
        }
    }

    /// <summary>Rebuilds a SyncState record, applying only the overrides passed in and preserving every other field from <paramref name="existing"/>.</summary>
    private static SyncState Clone(
        SyncState existing,
        IReadOnlyList<SyncedCompanyEntry>? companies = null,
        IReadOnlyList<SyncedProjectEntry>? projects = null,
        IReadOnlyList<FailedProjectEntry>? failedProjects = null,
        IReadOnlyList<FailedCompanyEntry>? failedCompanies = null,
        IReadOnlyList<ProjectStatusEntry>? projectStatuses = null,
        bool setFailedProjectStatuses = false,
        FailedProjectStatusEntry? failedProjectStatuses = null,
        IReadOnlyList<RetryTimeSheetEntry>? retryTimeSheets = null,
        CompanySyncSummary? summary = null,
        ProjectSyncSummary? projectSummary = null,
        DateTime? lastUpdatedAt = null) => new()
    {
        SyncType = existing.SyncType,
        DefaultProject = existing.DefaultProject,
        Companies = companies ?? existing.Companies,
        InitialCompanies = existing.InitialCompanies,
        Projects = projects ?? existing.Projects,
        InitialProjects = existing.InitialProjects,
        FailedProjects = failedProjects ?? existing.FailedProjects,
        FailedCompanies = failedCompanies ?? existing.FailedCompanies,
        ProjectStatuses = projectStatuses ?? existing.ProjectStatuses,
        FailedProjectStatuses = setFailedProjectStatuses ? failedProjectStatuses : existing.FailedProjectStatuses,
        BillingType = existing.BillingType,
        Summary = summary ?? existing.Summary,
        ProjectSummary = projectSummary ?? existing.ProjectSummary,
        RetryTimeSheets = retryTimeSheets ?? existing.RetryTimeSheets,
        LastUpdatedAt = lastUpdatedAt ?? existing.LastUpdatedAt
    };

    public Task<SyncState?> GetAsync(string syncType, CancellationToken cancellationToken = default) =>
        ReadJsonLockedAsync<SyncState>(PathFor(syncType), cancellationToken);

    public async Task SaveAsync(SyncState state, CancellationToken cancellationToken = default)
    {
        await WriteJsonLockedAsync(PathFor(state.SyncType), state, cancellationToken);
        logger.LogInformation("Saved sync state for syncType={SyncType}, lastUpdatedAt={LastUpdatedAt}, companies={Count}.",
            state.SyncType, state.LastUpdatedAt, state.Companies.Count);
    }

    public Task UpsertCompaniesAsync(
        string syncType,
        IReadOnlyList<SyncedCompanyEntry> newEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(syncType), existing =>
        {
            var merged = (existing?.Companies ?? []).ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in newEntries)
                merged[entry.Id] = entry;

            return Clone(existing ?? new SyncState { SyncType = syncType }, companies: merged.Values.ToList(), lastUpdatedAt: lastUpdatedAt);
        }, cancellationToken);

    public Task UpsertProjectsAsync(
        string syncType,
        IReadOnlyList<SyncedProjectEntry> newEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(syncType), existing =>
        {
            var merged = (existing?.Projects ?? []).ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in newEntries)
                merged[entry.Id] = entry;

            return Clone(existing ?? new SyncState { SyncType = syncType }, projects: merged.Values.ToList(), lastUpdatedAt: lastUpdatedAt);
        }, cancellationToken);

    public Task SaveFailedProjectsAsync(
        IReadOnlyList<FailedProjectEntry> failedEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(SyncTypes.FailedProjects), existing =>
            Clone(existing ?? new SyncState { SyncType = SyncTypes.FailedProjects }, failedProjects: failedEntries, lastUpdatedAt: lastUpdatedAt),
            cancellationToken);

    public Task SaveFailedCompaniesAsync(
        IReadOnlyList<FailedCompanyEntry> failedEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(SyncTypes.FailedCompanies), existing =>
            Clone(existing ?? new SyncState { SyncType = SyncTypes.FailedCompanies }, failedCompanies: failedEntries, lastUpdatedAt: lastUpdatedAt),
            cancellationToken);

    public Task SaveRetryCompaniesAsync(
        IReadOnlyList<RetryCompanyEntry> retryEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        WriteJsonLockedAsync(PathFor(SyncTypes.RetryCompanies),
            new RetryCompaniesRecord { Companies = retryEntries, LastUpdatedAt = lastUpdatedAt },
            cancellationToken);

    public async Task<IReadOnlyList<RetryCompanyEntry>> GetRetryCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var record = await ReadJsonLockedAsync<RetryCompaniesRecord>(PathFor(SyncTypes.RetryCompanies), cancellationToken);
        return record?.Companies ?? [];
    }

    public Task SaveRetryProjectsAsync(
        IReadOnlyList<RetryProjectEntry> retryEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        WriteJsonLockedAsync(PathFor(SyncTypes.RetryProjects),
            new RetryProjectsRecord { Projects = retryEntries, LastUpdatedAt = lastUpdatedAt },
            cancellationToken);

    public Task SaveRetryTimeSheetsAsync(
        IReadOnlyList<RetryTimeSheetEntry> retryEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(SyncTypes.RetryTimeSheets), existing =>
            Clone(existing ?? new SyncState { SyncType = SyncTypes.RetryTimeSheets }, retryTimeSheets: retryEntries, lastUpdatedAt: lastUpdatedAt),
            cancellationToken);

    public async Task<IReadOnlyList<RetryTimeSheetEntry>> GetRetryTimeSheetsAsync(CancellationToken cancellationToken = default)
    {
        var state = await ReadJsonLockedAsync<SyncState>(PathFor(SyncTypes.RetryTimeSheets), cancellationToken);
        return state?.RetryTimeSheets ?? [];
    }

    public Task SaveCompanySummaryAsync(
        CompanySyncSummary summary,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(SyncTypes.Company), existing =>
            Clone(existing ?? new SyncState { SyncType = SyncTypes.Company }, summary: summary, lastUpdatedAt: lastUpdatedAt),
            cancellationToken);

    public Task SaveProjectSummaryAsync(
        ProjectSyncSummary summary,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(SyncTypes.Project), existing =>
            Clone(existing ?? new SyncState { SyncType = SyncTypes.Project }, projectSummary: summary, lastUpdatedAt: lastUpdatedAt),
            cancellationToken);

    public Task SaveProjectStatusAsync(
        IReadOnlyList<ProjectStatusEntry> entries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(SyncTypes.ProjectStatus), existing =>
            Clone(existing ?? new SyncState { SyncType = SyncTypes.ProjectStatus }, projectStatuses: entries, lastUpdatedAt: lastUpdatedAt),
            cancellationToken);

    public Task SaveFailedProjectStatusAsync(
        FailedProjectStatusEntry? failure,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default) =>
        UpdateJsonAsync<SyncState>(PathFor(SyncTypes.ProjectStatus), existing =>
            Clone(existing ?? new SyncState { SyncType = SyncTypes.ProjectStatus },
                setFailedProjectStatuses: true, failedProjectStatuses: failure, lastUpdatedAt: lastUpdatedAt),
            cancellationToken);

    public async Task<IReadOnlyList<TimeEntryEmployeeDedupeState>> GetTimeEntryEmployeeDedupeStatesAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"{SyncTypes.TimeEntries}#";
        var files = Directory.Exists(_dataDirectory)
            ? Directory.GetFiles(_dataDirectory, $"{prefix}*.json")
            : [];

        var results = new List<TimeEntryEmployeeDedupeState>();
        foreach (var file in files)
        {
            var state = await ReadJsonLockedAsync<TimeEntryEmployeeDedupeState>(file, cancellationToken);
            if (state is not null)
                results.Add(state);
        }

        logger.LogInformation("Loaded {Count} time-entry employee checkpoint records.", results.Count);
        return results;
    }

    public async Task<IReadOnlyList<TimeEntryEmployeeDedupeState>> GetTimeEntryEmployeeDedupeStatesToSyncAsync(
        int year, int period, CancellationToken cancellationToken = default)
    {
        var all = await GetTimeEntryEmployeeDedupeStatesAsync(cancellationToken);

        var results = all
            .Where(s => !s.SyncedPeriods.TryGetValue(year, out var periods) || !periods.Contains(period))
            .ToList();

        logger.LogInformation(
            "Loaded {Count} time-entry employee checkpoint records requiring sync for period {Year}/{Period}.",
            results.Count, year, period);

        return results;
    }

    public Task<TimeEntryEmployeeDedupeState?> GetTimeEntryEmployeeDedupeStateAsync(
        string employeeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
            return Task.FromResult<TimeEntryEmployeeDedupeState?>(null);

        var syncType = $"{SyncTypes.TimeEntries}#{employeeId.Trim()}";
        return ReadJsonLockedAsync<TimeEntryEmployeeDedupeState>(PathFor(syncType), cancellationToken);
    }

    public Task UpsertTimeEntryEmployeeDedupeStateAsync(
        TimeEntryEmployeeDedupeState state, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(state.EmployeeId))
            return Task.CompletedTask;

        var employeeId = state.EmployeeId.Trim();
        var normalized = new TimeEntryEmployeeDedupeState
        {
            EmployeeId = employeeId,
            Email = state.Email,
            SyncedPeriods = state.SyncedPeriods
        };

        return WriteJsonLockedAsync(PathFor($"{SyncTypes.TimeEntries}#{employeeId}"), normalized, cancellationToken);
    }

    public Task EnsureDefaultProjectAsync(CancellationToken cancellationToken = default) =>
        EnsureExistsAsync(PathFor(SyncTypes.DefaultProject), () =>
        {
            const string projectManagerEmail = "jason_williams@oculusit.com";
            const string projectManagerName = "Jason Williams";

            logger.LogInformation(
                "Initialized DefaultProject sync type with project manager: {ProjectManagerName} ({ProjectManagerEmail}).",
                projectManagerName, projectManagerEmail);

            return new SyncState
            {
                SyncType = SyncTypes.DefaultProject,
                DefaultProject = new DefaultProjectEntry
                {
                    ProjectManager = new DefaultProjectManagerEntry
                    {
                        Email = projectManagerEmail,
                        Name = projectManagerName
                    }
                },
                LastUpdatedAt = DateTime.UtcNow
            };
        }, cancellationToken);

    public Task EnsureTimeOffSyncTypeAsync(CancellationToken cancellationToken = default) =>
        EnsureExistsAsync(PathFor(SyncTypes.TimeOff), () =>
        {
            const string workTypes = "Personal,PTO,Sick,Vacation,Holiday";
            logger.LogInformation("Initialized TimeOff sync type with work types: {WorkTypes}.", workTypes);
            return new TimeOffRecord { WorkType = workTypes, LastUpdatedAt = DateTime.UtcNow };
        }, cancellationToken);

    public async Task<string> GetTimeOffSyncTypeAsync(CancellationToken cancellationToken = default)
    {
        var record = await ReadJsonLockedAsync<TimeOffRecord>(PathFor(SyncTypes.TimeOff), cancellationToken);
        return record?.WorkType ?? string.Empty;
    }

    public Task EnsureBillingTypeAsync(CancellationToken cancellationToken = default) =>
        EnsureExistsAsync(PathFor(SyncTypes.BillingType), () =>
        {
            const string billingType = "1";
            logger.LogInformation("Initialized BillingType sync type with default billing type: {BillingType} (Fixed Fee).", billingType);
            return new SyncState { SyncType = SyncTypes.BillingType, BillingType = billingType, LastUpdatedAt = DateTime.UtcNow };
        }, cancellationToken);

    public async Task<string> GetBillingTypeAsync(CancellationToken cancellationToken = default)
    {
        var state = await ReadJsonLockedAsync<SyncState>(PathFor(SyncTypes.BillingType), cancellationToken);
        return state?.BillingType ?? string.Empty;
    }

    /// <summary>File-storage envelope for the RetryCompanies record — has no equivalent field on <see cref="SyncState"/>.</summary>
    private sealed class RetryCompaniesRecord
    {
        public IReadOnlyList<RetryCompanyEntry> Companies { get; init; } = [];
        public DateTime? LastUpdatedAt { get; init; }
    }

    /// <summary>File-storage envelope for the RetryProjects record — has no equivalent field on <see cref="SyncState"/>.</summary>
    private sealed class RetryProjectsRecord
    {
        public IReadOnlyList<RetryProjectEntry> Projects { get; init; } = [];
        public DateTime? LastUpdatedAt { get; init; }
    }

    /// <summary>File-storage envelope for the TimeOff record — has no equivalent field on <see cref="SyncState"/>.</summary>
    private sealed class TimeOffRecord
    {
        public string WorkType { get; init; } = string.Empty;
        public DateTime? LastUpdatedAt { get; init; }
    }
}
