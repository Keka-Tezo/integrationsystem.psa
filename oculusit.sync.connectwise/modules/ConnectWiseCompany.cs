using System.Text.Json.Serialization;

namespace oculusit.sync.connectwise.modules;

public sealed class ConnectWiseCompany
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public ConnectWiseCompanyStatus? Status { get; init; }

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; init; } = string.Empty;

    [JsonPropertyName("website")]
    public string Website { get; init; } = string.Empty;
}

public sealed class ConnectWiseCompanyStatus
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
