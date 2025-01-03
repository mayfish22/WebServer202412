using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class Emoji
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    [JsonPropertyName("length")]
    public int Length { get; set; }
    [JsonPropertyName("productId")]
    public string? ProductID { get; set; }
    [JsonPropertyName("emojiId")]
    public string? EmojiID { get; set; }
}