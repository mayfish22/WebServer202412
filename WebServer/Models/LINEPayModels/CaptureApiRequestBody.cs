using WebServer.Models.LINEPayModels.Enums;
using WebServer.Models.LINEPayModels.JsonConverters;
using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

/// <summary>
/// Capture API 請求主體
/// 用於請求對已授權的交易進行請款操作。
/// </summary>
public class CaptureAPIRequestBody
{
    /// <summary>
    /// **付款金額**
    ///  *  需要請款的總金額。
    ///  *  必須與授權時的金額相同，除非進行部分請款。
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// **貨幣類型** (ISO 4217)
    /// *   指定交易使用的貨幣種類。
    /// *   支援的貨幣包括：USD (美元), JPY (日圓), TWD (新台幣), THB (泰銖)。
    /// </summary>
    [JsonPropertyName("currency")]
    [JsonConverter(typeof(CurrencyJsonConverter))]
    public Currency Currency { get; set; }

    /// <summary>
    /// **額外選項**
    ///  *  包含額外設定的選項，例如點數限制資訊。
    /// </summary>
    [JsonPropertyName("options")]
    public Option Options { get; set; }

    public class Option
    {
        /// <summary>
        /// **額外資訊**
        ///  *  用於設定推廣活動的相關限制。
        /// </summary>
        [JsonPropertyName("extra")]
        public ExtraOption Extra { get; set; }
        public class ExtraOption
        {
            /// <summary>
            ///  **點數限制資訊**
            ///   *  設定是否可使用點數折抵或回饋點數。
            /// </summary>
            [JsonPropertyName("promotionRestriction")]
            public PromotionRestrictionOption PromotionRestriction { get; set; }
        }
    }
    /// <summary>
    ///  **點數限制選項**
    ///   *  設定不可使用點數折抵的金額和不可回饋點數的金額。
    /// </summary>
    public class PromotionRestrictionOption
    {
        /// <summary>
        /// **不可使用點數折抵的金額**
        /// *   設定此金額以上時，不可使用點數折抵。
        /// </summary>
        [JsonPropertyName("useLimit")]
        public decimal UseLimit { get; set; }
        /// <summary>
        /// **不可回饋點數的金額**
        ///  *  設定此金額以上時，不可回饋點數。
        /// </summary>
        [JsonPropertyName("rewardLimit")]
        public decimal RewardLimit { get; set; }
    }
}