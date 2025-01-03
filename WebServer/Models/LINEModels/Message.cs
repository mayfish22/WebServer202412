using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class Message
{
    [JsonPropertyName("id")]
    public string? ID { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    [JsonPropertyName("emojis")]
    public Emoji[]? Emojis { get; set; }
    [JsonPropertyName("contentProvider")]
    public ContentProvider? ContentProvider { get; set; }
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
    [JsonPropertyName("fileSize")]
    public int FileSize { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }
    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }
    [JsonPropertyName("packageId")]
    public string? PackageID { get; set; }
    [JsonPropertyName("stickerId")]
    public string? StickerID { get; set; }
    [JsonPropertyName("stickerResourceType")]
    public string? StickerResourceType { get; set; }
    [JsonPropertyName("keywords")]
    public string[]? Keywords { get; set; }
}