using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;

namespace WebServer.Models.LINEPayModels.JsonConverters;

public class FamilyServiceTypeJsonConverter : JsonConverter<FamilyServiceType>
{
    public override FamilyServiceType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (Enum.TryParse<FamilyServiceType>(str, out var result))
            {
                return result;
            }
        }

        return FamilyServiceType.lineAt; // 預設值
    }

    public override void Write(Utf8JsonWriter writer, FamilyServiceType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}