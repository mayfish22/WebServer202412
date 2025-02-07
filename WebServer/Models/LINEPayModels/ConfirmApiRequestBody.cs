using WebServer.Models.LINEPayModels.Enums;
using WebServer.Models.LINEPayModels.JsonConverters;
using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

public class ConfirmAPIRequestBody
{
    /// <summary>
    /// 付款金額
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    /// <summary>
    /// 貨幣（ISO 4217）
    /// 支援貨幣：USD、JPY、TWD、THB
    /// </summary>
    [JsonPropertyName("currency")]
    [JsonConverter(typeof(CurrencyJsonConverter))]
    public Currency Currency { get; set; }
}