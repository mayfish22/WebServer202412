using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

public class CheckPaymentStatusAPIResultResult
{
    /// <summary>
    /// 結果代碼
    /// </summary>
    public string ReturnCode { get; set; }
    /// <summary>
    /// 結果代碼說明
    /// </summary>
    [JsonIgnore]
    public string Description
    {
        get
        {
            if (CodeDescriptions.ContainsKey(ReturnCode))
            {
                return CodeDescriptions[ReturnCode];
            }
            return "例外請參閱【https://pay.line.me/th/developers/apis/onlineApis?locale=zh_TW】";
        }
    }
    [JsonIgnore]
    private readonly Dictionary<string, string> CodeDescriptions = new()
    {
        {"0000", "成功"},
        {"0110", "授權完成 - 現在可以呼叫Confirm API"},
        {"0121", "該交易已被用戶取消，或者超時取消（20分鐘）- 交易已經結束了"},
        {"0122", "付款失敗 - 交易已經結束了"},
        {"0123", "付款成功 - 交易已經結束了"},
        {"1104", "此商家不存在"},
        {"1105", "此商家處於無法使用LINE Pay的狀態"},
        {"9000", "內部錯誤"},
    };
    /// <summary>
    /// 結果訊息或失敗原因
    /// </summary>
    public string ReturnMessage { get; set; }
    [JsonPropertyName("info")]
    public InfoOption Info { get; set; }

    public class InfoOption
    {
        [JsonPropertyName("shipping")]
        public ShippingOption Shipping { get; set; }

        public class ShippingOption
        {
            /// <summary>
            /// 用戶所選的配送方式ID
            /// </summary>
            [JsonPropertyName("methodId")]
            public string MethodId { get; set; }
            /// <summary>
            /// 運費
            /// </summary>
            [JsonPropertyName("feeAmount")]
            public decimal FeeAmount { get; set; }
        }
    }
}