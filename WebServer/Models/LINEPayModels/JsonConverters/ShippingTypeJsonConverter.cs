using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class ShippingTypeJsonConverter : JsonConverter<ShippingType?>
{
    public override ShippingType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<ShippingType>(str, out var result))
            {
                return result;
            }
        }

        //return ShippingType.NO_SHIPPING; // 預設值
        return null;
    }

    public override void Write(Utf8JsonWriter writer, ShippingType? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}