using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class Unsend
{
    [JsonPropertyName("messageId")]
    public string? MessageID { get; set; }
}