using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (DateTimeOffset.TryParse(reader.GetString(), out var result))
            {
                return result;
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString("o")); // Converts DateTimeOffset to ISO 8601 format
        else
            writer.WriteNullValue();
    }
}