using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class WebhookEvent
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    [JsonPropertyName("source")]
    public Source? Source { get; set; }
    [JsonPropertyName("replyToken")]
    public string? ReplyToken { get; set; }
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
    [JsonPropertyName("unsend")]
    public Unsend? Unsend { get; set; }
}