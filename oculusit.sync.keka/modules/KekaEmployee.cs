using System.Text.Json.Serialization;

namespace oculusit.sync.keka.modules;

public sealed class KekaEmployee
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("employeeNumber")]
    public string? EmployeeNumber { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("middleName")]
    public string? MiddleName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("jobTitle")]
    public KekaEmployeeInfo? JobTitle { get; init; }

    [JsonPropertyName("reportsTo")]
    public KekaManagerInfo? ReportsTo { get; init; }

    [JsonPropertyName("l2Manager")]
    public KekaManagerInfo? L2Manager { get; init; }

    [JsonPropertyName("dottedLineManager")]
    public KekaManagerInfo? DottedLineManager { get; init; }

    [JsonPropertyName("gender")]
    public int? Gender { get; init; }

    [JsonPropertyName("joiningDate")]
    public DateTime? JoiningDate { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public DateTime? DateOfBirth { get; init; }

    [JsonPropertyName("groups")]
    public IReadOnlyList<KekaGroupInfo> Groups { get; init; } = [];
}

public sealed class KekaEmployeeInfo
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

public sealed class KekaManagerInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

public sealed class KekaGroupInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("groupType")]
    public int? GroupType { get; init; }
}
