using System.Text.Json.Serialization;

namespace WebServer.Models.LINEModels;

public class FriendshipStatus
{
    [JsonPropertyName("friendFlag")]
    public bool FriendFlag { get; set; }
}