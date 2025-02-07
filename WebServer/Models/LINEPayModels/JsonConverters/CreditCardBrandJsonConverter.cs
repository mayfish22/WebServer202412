using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class CreditCardBrandJsonConverter : JsonConverter<CreditCardBrand?>
{
    public override CreditCardBrand? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<CreditCardBrand>(str, out var result))
            {
                return result;
            }
        }

        //return CreditCardBrand.VISA; // 預設值
        return null;
    }

    public override void Write(Utf8JsonWriter writer, CreditCardBrand? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}