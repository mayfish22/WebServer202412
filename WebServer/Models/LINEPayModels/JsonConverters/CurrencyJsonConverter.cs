using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class CurrencyJsonConverter : JsonConverter<Currency>
{
    public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<Currency>(str, out var result))
            {
                return result;
            }
        }

        return Currency.TWD; // 預設值
    }

    public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}