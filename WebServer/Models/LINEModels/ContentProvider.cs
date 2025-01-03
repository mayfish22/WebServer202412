using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class ContentProvider
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("originalContentUrl")]
    public string? OriginalContentUrl { get; set; }
    [JsonPropertyName("previewImageUrl")]
    public string? PreviewImageUrl { get; set; }
}