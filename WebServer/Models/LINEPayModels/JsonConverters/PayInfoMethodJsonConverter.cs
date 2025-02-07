using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class PayInfoMethodJsonConverter : JsonConverter<PayInfoMethod?>
{
    public override PayInfoMethod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<PayInfoMethod>(str, out var result))
            {
                return result;
            }
        }

        //return PayInfoMethod.CREDIT_CARD; // 預設值
        return null;
    }

    public override void Write(Utf8JsonWriter writer, PayInfoMethod? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}