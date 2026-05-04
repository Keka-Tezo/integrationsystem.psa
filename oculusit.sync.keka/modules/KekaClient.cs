using System.Text.Json.Serialization;
using oculusit.sync.keka.converters;

namespace oculusit.sync.keka.modules;

public sealed class KekaClient
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    [JsonConverter(typeof(NullableIntFromStringConverter))]
    public int? Code { get; init; }

    [JsonPropertyName("phone")]
    public string Phone { get; init; } = string.Empty;

    [JsonPropertyName("website")]
    public string Website { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("billingInfo")]
    public KekaBillingInfo? BillingInfo { get; init; }
}

public sealed class KekaBillingInfo
{
    [JsonPropertyName("billingCurrencyId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BillingCurrencyId { get; init; }

    [JsonPropertyName("billingAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public KekaBillingAddress? BillingAddress { get; init; }
}

public sealed class KekaBillingAddress
{
    [JsonPropertyName("addressLine1")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AddressLine1 { get; init; }

    [JsonPropertyName("addressLine2")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AddressLine2 { get; init; }

    [JsonPropertyName("countryCode")]
    public string CountryCode { get; init; } = "US";

    [JsonPropertyName("city")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? City { get; init; }

    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; init; }

    [JsonPropertyName("zip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Zip { get; init; }
}