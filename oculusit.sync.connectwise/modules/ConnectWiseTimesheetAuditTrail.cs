using System.Text.Json.Serialization;

namespace oculusit.sync.connectwise.modules;

/// <summary>
/// Represents an audit trail entry for a ConnectWise timesheet.
/// </summary>
public sealed class ConnectWiseTimesheetAuditTrail
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("transactionType")]
    public string TransactionType { get; init; } = string.Empty;

    [JsonPropertyName("dateTimeStamp")]
    public DateTime? DateTimeStamp { get; init; }

    /// <summary>
    /// The user who performed the action
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; init; }

    /// <summary>
    /// Brief description of the audit action
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; init; }

    /// <summary>
    /// Field name that was changed
    /// </summary>
    [JsonPropertyName("fieldName")]
    public string? FieldName { get; init; }

    /// <summary>
    /// Old value before the change
    /// </summary>
    [JsonPropertyName("oldValue")]
    public string? OldValue { get; init; }

    /// <summary>
    /// New value after the change
    /// </summary>
    [JsonPropertyName("newValue")]
    public string? NewValue { get; init; }

    /// <summary>
    /// Status before the action
    /// </summary>
    [JsonPropertyName("statusBefore")]
    public string? StatusBefore { get; init; }

    /// <summary>
    /// Status after the action
    /// </summary>
    [JsonPropertyName("statusAfter")]
    public string? StatusAfter { get; init; }

    [JsonPropertyName("_info")]
    public ConnectWiseTimesheetAuditTrailInfo? Info { get; init; }

    public DateTime? LastUpdated => Info?.LastUpdated;
}

public sealed class ConnectWiseTimesheetAuditTrailInfo
{
    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; init; }
}
