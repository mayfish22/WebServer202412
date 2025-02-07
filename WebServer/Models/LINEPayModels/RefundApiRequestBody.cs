using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

public class RefundAPIRequestBody
{
    /// <summary>
    /// 退款金額
    /// 返回空值的話，進行全部退款
    /// </summary>
    [JsonPropertyName("refundAmount")]
    public decimal? RefundAmount { get; set; }
    

    [JsonPropertyName("options")]
    public Option Options { get; set; }
    public class Option
    {
        [JsonPropertyName("extra")]
        public ExtraOption Extra { get; set; }
        public class ExtraOption
        {
            /// <summary>
            /// 點數限制資訊
            /// </summary>
            [JsonPropertyName("promotionRestriction")]
            public PromotionRestrictionOption PromotionRestriction { get; set; }
        }
    }
}