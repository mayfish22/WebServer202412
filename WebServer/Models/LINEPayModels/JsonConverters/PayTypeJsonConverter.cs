using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class PayTypeJsonConverter : JsonConverter<PayType?>
{
    public override PayType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<PayType>(str, out var result))
            {
                return result;
            }
        }

        //return PayType.NORMAL; // 預設值
        return null;
    }

    public override void Write(Utf8JsonWriter writer, PayType? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}