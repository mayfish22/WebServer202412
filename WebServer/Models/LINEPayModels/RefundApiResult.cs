using WebServer.Models.LINEPayModels.JsonConverters;
using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

public class RefundAPIResult
{
    /// <summary>
    /// 結果代碼
    /// </summary>
    [JsonPropertyName("returnCode")]
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
        {"1101", "買家不是LINE Pay的用戶"},
        {"1102", "買方被停止交易"},
        {"1104", "此商家不存在"},
        {"1105", "此商家無法使用LINE Pay"},
        {"1106", "標頭(Header)資訊錯誤"},
        {"1124", "金額有誤（scale）"},
        {"1150", "交易記錄不存在"},
        {"1155", "交易編號不符合退款資格"},
        {"1163", "可退款期限已過無法退款"},
        {"1164", "退款金額超過限制金額"},
        {"1165", "已經退款而關閉的交易"},
        {"1179", "無法處理的狀態"},
        {"1198", "API呼叫重複"},
        {"1199", "內部請求錯誤"},
        {"1264", "一卡通MONEY通相關錯誤"},
        {"9000", "內部錯誤"},
    };
    /// <summary>
    /// 結果訊息
    /// </summary>
    [JsonPropertyName("returnMessage")]
    public string ReturnMessage { get; set; }
    [JsonPropertyName("info")]
    public InfoOption Info { get; set; }
    public class InfoOption
    {
        /// <summary>
        /// 退款序號（該次退款產生的新序號, 19 digits）
        /// </summary>
        [JsonPropertyName("refundTransactionId")]
        public long RefundTransactionId { get; set; }
        /// <summary>
        /// 退款日期（ISO 8601）
        /// </summary>
        [JsonPropertyName("refundTransactionDate")]
        [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
        public DateTimeOffset? RefundTransactionDate { get; set; }
    }
}