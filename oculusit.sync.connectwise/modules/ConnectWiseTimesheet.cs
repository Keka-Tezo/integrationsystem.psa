using System.Text.Json.Serialization;

namespace oculusit.sync.connectwise.modules;

/// <summary>
/// Represents a ConnectWise Timesheet entry returned from GET /time/sheets.
/// </summary>
public sealed class ConnectWiseTimesheet
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("member")]
    public ConnectWiseTimesheetMember? Member { get; init; }

    [JsonPropertyName("year")]
    public int Year { get; init; }

    [JsonPropertyName("period")]
    public int Period { get; init; }

    [JsonPropertyName("dateStart")]
    public string? DateStart { get; init; }

    [JsonPropertyName("dateEnd")]
    public string? DateEnd { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("hours")]
    public decimal? Hours { get; init; }

    [JsonPropertyName("deadline")]
    public string? Deadline { get; init; }

    [JsonPropertyName("_info")]
    public ConnectWiseTimesheetInfo? Info { get; init; }

    public DateTime? LastUpdated => Info?.LastUpdated;
}

public sealed class ConnectWiseTimesheetMember
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("dailyCapacity")]
    public decimal? DailyCapacity { get; init; }
}

public sealed class ConnectWiseTimesheetInfo
{
    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; init; }

    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; init; }

    [JsonPropertyName("dateEntered")]
    public DateTime? DateEntered { get; init; }

    [JsonPropertyName("enteredBy")]
    public string? EnteredBy { get; init; }
}
