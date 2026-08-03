using System.Text.Json;
using System.Text.Json.Serialization;

namespace QRShop.API.Json;

// Trims leading and trailing whitespace off every incoming string.
//
// Registered globally rather than per-DTO so it cannot be forgotten on a new
// endpoint, and so it holds for callers that never touch the React app — the
// client trims as a convenience, this is what actually keeps the data clean.
//
// A padded value is never meaningful here: " Gokul" and "Gokul" are the same
// shop name, but they compare as different, sort apart, and would let the same
// name be claimed twice.
public class TrimmingStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : reader.GetString()?.Trim();

    // Writing is untouched: existing rows are returned exactly as stored, so a
    // read never silently disagrees with what is in the database.
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
