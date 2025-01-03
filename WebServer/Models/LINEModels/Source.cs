using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class Source
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("userId")]
    public string? UserID { get; set; }
    [JsonPropertyName("groupId")]
    public string? GroupID { get; set; }
    [JsonPropertyName("roomId")]
    public string? RoomID { get; set; }
}