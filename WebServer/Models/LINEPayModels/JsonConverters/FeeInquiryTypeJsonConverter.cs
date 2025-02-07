using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class FeeInquiryTypeJsonConverter : JsonConverter<FeeInquiryType?>
{
    public override FeeInquiryType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<FeeInquiryType>(str, out var result))
            {
                return result;
            }
        }

        //return FeeInquiryType.CONDITION; // 預設值
        return null;
    }

    public override void Write(Utf8JsonWriter writer, FeeInquiryType? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}