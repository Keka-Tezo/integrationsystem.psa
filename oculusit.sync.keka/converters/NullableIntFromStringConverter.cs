using System.Text.Json;
using System.Text.Json.Serialization;

namespace oculusit.sync.keka.converters;

/// <summary>
/// Handles Keka API inconsistency where "code" can arrive as a JSON number,
/// a quoted string, or null.
/// </summary>
internal sealed class NullableIntFromStringConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null   => null,
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => int.TryParse(reader.GetString(), out var v) ? v : null,
            _                    => throw new JsonException(
                                        $"Unexpected token type '{reader.TokenType}' for nullable int.")
        };

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteNumberValue(value.Value);
    }
}
