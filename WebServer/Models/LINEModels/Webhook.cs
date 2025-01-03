using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class Webhook
{
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }
    [JsonPropertyName("events")]
    public WebhookEvent[]? Events { get; set; }
}