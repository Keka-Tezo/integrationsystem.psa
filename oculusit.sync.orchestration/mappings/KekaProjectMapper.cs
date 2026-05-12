using oculusit.sync.connectwise.modules;
using oculusit.sync.keka.modules;

namespace oculusit.sync.orchestration.mappings;

public static class KekaProjectMapper
{
    // Minimum date accepted by SQL Server datetime columns (1753-01-01).
    private static readonly DateTime SqlMinDate = new(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Maximum length of Keka's Group.Description column.
    private const int MaxDescriptionLength = 100;

    public static KekaProjectRequest MapToKekaProjectRequest(
        ConnectWiseProject project,
        string kekaClientId)
    {
        return new KekaProjectRequest
        {
            ClientId    = kekaClientId,
            Name        = project.Name,
            Description = TruncateDescription(project.Description),
            Code        = project.Id.ToString(),
            Status      = MapStatus(project.Status?.Name),
            StartDate   = project.ActualStart ?? SqlMinDate,
            EndDate     = project.ActualEnd   ?? SqlMinDate,
            IsBillable  = true
        };
    }

    public static KekaProjectUpdateRequest MapToKekaProjectUpdateRequest(ConnectWiseProject project)
    {
        return new KekaProjectUpdateRequest
        {
            Name        = project.Name,
            Description = TruncateDescription(project.Description),
            Code        = project.Id.ToString(),
            Status      = MapStatus(project.Status?.Name),
            StartDate   = project.ActualStart ?? SqlMinDate,
            EndDate     = project.ActualEnd   ?? SqlMinDate,
            IsBillable  = true
        };
    }

    /// <summary>
    /// Maps a ConnectWise project status name to the corresponding Keka numeric status value.
    /// 0 = InProgress, 1 = Completed, 2 = Cancelled, 3 = Not Yet Started, 4 = On Hold.
    /// Defaults to 0 (InProgress) for any unrecognised status.
    /// </summary>
    internal static int MapStatus(string? cwStatus)
    {
        if (string.IsNullOrWhiteSpace(cwStatus))
            return 0;

        return cwStatus.Trim().ToLowerInvariant() switch
        {
            "completed"                => 1,
            "closed"                   => 1,
            "closed - not implemented" => 1,
            "not started"              => 3,
            "new"                      => 3,
            "terminated"               => 2,
            "on-hold"                  => 4,
            _                          => 0
        };
    }

    /// <summary>
    /// Trims whitespace and truncates the description to <see cref="MaxDescriptionLength"/>
    /// characters to stay within Keka's column limit. Returns null for blank input.
    /// </summary>
    private static string? TruncateDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        return value.Length <= MaxDescriptionLength
            ? value
            : value[..MaxDescriptionLength];
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
