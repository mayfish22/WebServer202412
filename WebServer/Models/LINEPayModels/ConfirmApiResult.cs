using WebServer.Models.LINEPayModels.Enums;
using WebServer.Models.LINEPayModels.JsonConverters;
using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

public class ConfirmAPIResult
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
        {"1101", "買家不是LINE Pay用戶"},
        {"1102", "買方被停止交易"},
        {"1104", "此商家不存在"},
        {"1105", "此商家無法使用 LINE Pay"},
        {"1106", "標頭(Header)資訊錯誤"},
        {"1110", "無法使用的信用卡"},
        {"1124", "金額錯誤(scale)"},
        {"1141", "付款帳戶狀態錯誤"},
        {"1142", "Balance餘額不足"},
        {"1150", "交易記錄不存在"},
        {"1152", "該transactionId的交易記錄已經存在"},
        {"1153", "付款request時的金額與申請時的金額不一致"},
        {"1159", "無付款申請資訊"},
        {"1169", "用來確認付款的資訊錯誤（請訪問LINE Pay設置付款方式與密碼認證）"},
        {"1170", "使用者帳戶的餘額有變動"},
        {"1172", "該訂單編號(orderId)的交易記錄已經存在"},
        {"1180", "付款時限已過"},
        {"1198", "API調用重覆"},
        {"1199", "內部請求錯誤"},
        {"1264", "一卡通MONEY通相關錯誤"},
        {"1280", "信用卡付款時候發生了臨時錯誤"},
        {"1281", "信用卡付款錯誤"},
        {"1282", "信用卡授權錯誤"},
        {"1283", "因有異常交易疑慮暫停交易，請洽 LINE Pay 客服確認"},
        {"1284", "暫時無法以信用卡付款"},
        {"1285", "信用卡資訊不完整"},
        {"1286", "信用卡付款資訊不正確"},
        {"1287", "信用卡已過期"},
        {"1288", "信用卡的額度不足"},
        {"1289", "超過信用卡付款金額上限"},
        {"1290", "超過一次性付款的額度"},
        {"1291", "此信用卡已被掛失"},
        {"1292", "此信用卡已被停卡"},
        {"1293", "信用卡驗證碼(CVN) 無效"},
        {"1294", "此信用卡已被列入黑名單"},
        {"1295", "信用卡號無效"},
        {"1296", "無效的金額"},
        {"1298", "信用卡付款遭拒絕"},
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
        /// 交易號碼
        /// </summary>
        [JsonPropertyName("transactionId")]
        public long TransactionId { get; set; }
        /// <summary>
        /// 授權過期時間（ISO 8601）
        /// 僅限於完成授權（capture=false）的付款，進行回傳。
        /// </summary>
        [JsonPropertyName("authorizationExpireDate")]
        [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
        public DateTimeOffset? AuthorizationExpireDate { get; set; }
        /// <summary>
        /// 用於自動付款的密鑰（15個字符）
        /// </summary>
        [JsonPropertyName("regKey")]
        public string RegKey { get; set; }
        [JsonPropertyName("payInfo")]
        public List<PayInfoOption> PayInfo { get; set; }
        public class PayInfoOption
        {
            /// <summary>
            /// 付款方式
            /// ❏信用卡：CREDIT_CARD
            /// ❏餘額：BALANCE
            /// ❏折扣：DISCOUNT(發票金額須扣除)
            /// ❏LINE POINTS：POINT(預設不顯示)
            /// </summary>
            [JsonPropertyName("method")]
            [JsonConverter(typeof(PayInfoMethodJsonConverter))]
            public PayInfoMethod? Method { get; set; }
            /// <summary>
            /// 付款金額
            /// </summary>
            [JsonPropertyName("amount")]
            public decimal? Amount { get; set; }
            /// <summary>
            /// 用於自動付款的信用卡別名
            /// ❏綁定在LINE Pay的信用卡名。一般在綁定信用卡時設置。
            /// ❏如果LINE Pay用戶沒有設置別名，會回傳空字符串。
            /// ❏用戶通過LINE Pay可以更改別名，更改內容不會分享到商家。
            /// </summary>
            [JsonPropertyName("creditCardNickname")]
            public string CreditCardNickname { get; set; }
            /// <summary>
            /// 用於自動付款的信用卡品牌
            /// ❏VISA
            /// ❏MASTER
            /// ❏AMEX
            /// ❏DINERS
            /// ❏JCB
            /// </summary>
            [JsonPropertyName("creditCardBrand")]
            [JsonConverter(typeof(CreditCardBrandJsonConverter))]
            public CreditCardBrand? CreditCardBrand { get; set; }
            /// <summary>
            /// 被遮罩（Masking）的信用卡號（僅限於台灣商家回應，若您需要，可以向商家中心管理者申請獲取。）
            /// Format: ************ 1234
            /// </summary>
            [JsonPropertyName("maskedCreditCardNumber")]
            public string MaskedCreditCardNumber { get; set; }
        }
        [JsonPropertyName("packages")]
        public List<Package> Packages { get; set; }
        public class Package
        {
            /// <summary>
            /// Package list的唯一ID
            /// </summary>
            [JsonPropertyName("id")]
            public string Id { get; set; }
            /// <summary>
            /// 一個Package中的商品總價
            /// =sum(products[].quantity* products[].price)
            /// </summary>
            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }
            /// <summary>
            /// 手續費：在付款金額中含手續費時回應
            /// </summary>
            [JsonPropertyName("userFeeAmount")]
            public decimal UserFeeAmount { get; set; }
        }
        [JsonPropertyName("merchantReference")]
        public MerchantReferenceOption MerchantReference { get; set; }
        public class MerchantReferenceOption
        {
            [JsonPropertyName("affiliateCards")]
            public List<AffiliateCard> AffiliateCards { get; set; }
            public class AffiliateCard
            {
                /// <summary>
                /// 交易中若用戶符合商店支援的卡片類型
                /// - 電子發票載具: MOBILE_CARRIER(功能預設不開啟)
                /// - 商家會員卡: {類別名稱需與LINE Pay洽談確認}
                /// </summary>
                [JsonPropertyName("cardType")]
                public string CardType { get; set; }
                /// <summary>
                /// 交易中若用戶符合商店支援的卡片類型所對應的內容值
                /// </summary>
                [JsonPropertyName("cardId")]
                public string CardId { get; set; }
            }
        }

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

            [JsonPropertyName("address")]
            public AddressOption Address { get; set; }
            public class AddressOption
            {
                /// <summary>
                /// 收貨國家
                /// </summary>
                [JsonPropertyName("country")]
                public string Country { get; set; }
                /// <summary>
                /// 收貨地郵政編碼
                /// </summary>
                [JsonPropertyName("PostalCode")]
                public string postalCode { get; set; }
                /// <summary>
                /// 收貨地區
                /// </summary>
                [JsonPropertyName("state")]
                public string State { get; set; }
                /// <summary>
                /// 收貨省市區
                /// </summary>
                [JsonPropertyName("city")]
                public string City { get; set; }
                /// <summary>
                /// 收貨地址
                /// </summary>
                [JsonPropertyName("detail")]
                public string Detail { get; set; }
                /// <summary>
                /// 詳細地址資訊
                /// </summary>
                [JsonPropertyName("optional")]
                public string Optional { get; set; }
                [JsonPropertyName("recipient")]
                public RecipientOption Recipient { get; set; }
            }
        }
    }
}