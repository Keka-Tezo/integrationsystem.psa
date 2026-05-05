using System.Text.Json.Serialization;

namespace oculusit.sync.keka.modules;

/// <summary>
/// Wraps all Keka API responses that return a single object under a "data" key.
/// </summary>
internal sealed class KekaDataResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

/// <summary>
/// Wraps all Keka API responses that return a list under a "data" key,
/// including pagination metadata.
/// </summary>
internal sealed class KekaDataListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T>? Data { get; init; }

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("hasMoreItems")]
    public bool HasMoreItems { get; init; }
}
