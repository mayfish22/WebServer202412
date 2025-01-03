using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

//https://developers.line.biz/en/reference/line-login/#profile
public class Profile
{
    [JsonPropertyName("userId")]
    public string? UserID { get; set; }
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    [JsonPropertyName("pictureUrl")]
    public string? PictureUrl { get; set; }
    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }
    [JsonPropertyName("language")]
    public string? Language { get; set; }
}