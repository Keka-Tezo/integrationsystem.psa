using System.Text.Json.Serialization;

namespace oculusit.sync.keka.modules;

public sealed class KekaEmployeeSearchRequest
{
    [JsonPropertyName("workEmail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WorkEmail { get; init; }

    [JsonPropertyName("workPhone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WorkPhone { get; init; }
}
