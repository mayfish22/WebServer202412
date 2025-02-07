using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

public class PromotionRestrictionOption
{
    /// <summary>
    /// 不可使用點數折抵的金額
    /// </summary>
    [JsonPropertyName("useLimit")]
    public decimal? UseLimit { get; set; }
    /// <summary>
    /// 不可回饋點數的金額
    /// </summary>
    [JsonPropertyName("rewardLimit")]
    public decimal? RewardLimit { get; set; }
}