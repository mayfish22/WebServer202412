using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class LocaleJsonConverter : JsonConverter<Locale?>
{
    public override Locale? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<Locale>(str, out var result))
            {
                return result;
            }
        }

        //return Locale.zh_TW; // 預設值
        return null;
    }

    public override void Write(Utf8JsonWriter writer, Locale? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}