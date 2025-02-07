using System.Text.Json.Serialization;
using WebServer.Models.LINEPayModels.Enums;
using WebServer.Models.LINEPayModels.JsonConverters;

namespace WebServer.Models.LINEPayModels;

public class RequestAPIRequestBody
{
    /// <summary>
    /// 付款金額
    /// = sum(packages[].amount) + sum(packages[].userFee) + options.shipping.feeAmount
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount
    {
        get
        {
            var amount = Packages.Sum(s => s.Amount + (s.UserFee ?? 0) );
            if (Options != null && Options.Shipping != null)
                amount += Options.Shipping.FeeAmount ?? 0;
            return amount;
        }
    }
    /// <summary>
    /// 貨幣（ISO 4217）
    /// ❏支援貨幣：USD、JPY、TWD、THB
    /// </summary>
    [JsonPropertyName("currency")]
    [JsonConverter(typeof(CurrencyJsonConverter))]
    public Currency Currency { get; set; } = Currency.TWD;
    /// <summary>
    /// 商家訂單編號
    /// 商家管理的唯一ID
    /// </summary>
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; }
    [JsonPropertyName("packages")]
    public List<Package> Packages { get; set; }
    [JsonPropertyName("redirectUrls")]
    public RedirectUrlsOption RedirectUrls { get; set; }
    [JsonPropertyName("options")]
    public Option Options { get; set; }
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
        public decimal Amount => Products.Sum(s => s.Quantity * s.Price);
        /// <summary>
        /// 手續費：在付款金額中含手續費時設定
        /// </summary>
        [JsonPropertyName("userFee")]
        public decimal? UserFee { get; set; }
        /// <summary>
        /// Package名稱 （or Shop Name）
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("products")]
        public List<Product> Products { get; set; }
        public class Product
        {
            /// <summary>
            /// 商家商品ID
            /// </summary>
            [JsonPropertyName("id")]
            public string Id { get; set; }
            /// <summary>
            /// 商品名
            /// </summary>
            [JsonPropertyName("name")]
            public string Name { get; set; }
            /// <summary>
            /// 商品圖示的URL
            /// </summary>
            [JsonPropertyName("imageUrl")]
            public string ImageUrl { get; set; }
            /// <summary>
            /// 商品數量
            /// </summary>
            [JsonPropertyName("quantity")]
            public decimal Quantity { get; set; }
            /// <summary>
            /// 各商品付款金額
            /// </summary>
            [JsonPropertyName("price")]
            public decimal Price { get; set; }
            /// <summary>
            /// 各商品原金額
            /// </summary>
            [JsonPropertyName("originalPrice")]
            public decimal? OriginalPrice { get; set; }
        }
    }
    public class RedirectUrlsOption
    {
        /// <summary>
        /// 在Android環境切換應用時所需的資訊，用於防止網路釣魚攻擊（phishing）
        /// </summary>
        [JsonPropertyName("appPackageName")]
        public string AppPackageName { get; set; }
        /// <summary>
        /// 使用者授權付款後，跳轉到該商家URL
        /// </summary>
        [JsonPropertyName("confirmUrl")]
        public string ConfirmUrl { get; set; }
        /// <summary>
        /// 使用者授權付款後，跳轉的confirmUrl類型
        /// </summary>
        [JsonPropertyName("confirmUrlType")]
        public string ConfirmUrlType { get; set; }
        /// <summary>
        /// 使用者通過LINE付款頁，取消付款後跳轉到該URL
        /// </summary>
        [JsonPropertyName("cancelUrl")]
        public string CancelUrl { get; set; }
    }

    public class Option
    {
        [JsonPropertyName("payment")]
        public PaymentOption Payment { get; set; }
        [JsonPropertyName("display")]
        public DisplayOption Display { get; set; }
        [JsonPropertyName("shipping")]
        public ShippingOption Shipping { get; set; }
        [JsonPropertyName("recipient")]
        public RecipientOption Recipient { get; set; }
        [JsonPropertyName("familyService")]
        public List<FamilyServiceOption> FamilyService { get; set; }
        [JsonPropertyName("extra")]
        public ExtraOption Extra { get; set; }
        public class PaymentOption
        {
            /// <summary>
            /// 是否自動請款
            /// ❏true(預設)：呼叫Confirm API，統一進行授權/請款處理
            /// ❏false：呼叫Confirm API只能完成授權，需要呼叫Capture API完成請款
            /// </summary>
            [JsonPropertyName("capture")]
            public bool Capture { get; set; }
            /// <summary>
            /// 付款類型
            /// ❏NORMAL
            /// ❏PREAPPROVED
            /// </summary>
            [JsonPropertyName("payType")]
            [JsonConverter(typeof(PayTypeJsonConverter))]
            public PayType? PayType { get; set; }
        }
        
        public class DisplayOption
        {
            /// <summary>
            /// 等待付款頁的語言程式碼，預設為英文（en）
            /// ❏支援語言：en、ja、ko、th、zh_TW、zh_CN
            /// </summary>
            [JsonPropertyName("locale")]
            [JsonConverter(typeof(LocaleJsonConverter))]
            public Locale? Locale { get; set; }
            /// <summary>
            /// 檢查將用於訪問confirmUrl的瀏覽器
            /// ❏true：如果跟請求付款的瀏覽器不同，引導使用LINE Pay請求付款的瀏覽器
            /// ❏false：無需檢查瀏覽器，直接訪問confirmUrl
            /// </summary>
            [JsonPropertyName("checkConfirmUrlBrowser")]
            public bool CheckConfirmUrlBrowser { get; set; }
        }
        public class ShippingOption
        {
            /// <summary>
            /// 收貨地選項
            /// ❏NO_SHIPPING
            /// ❏FIXED_ADDRESS
            /// ❏SHIPPING
            /// </summary>
            [JsonPropertyName("type")]
            [JsonConverter(typeof(ShippingTypeJsonConverter))]
            public ShippingType? Type { get; set; }
            /// <summary>
            /// 運費
            /// </summary>
            [JsonPropertyName("feeAmount")]
            public decimal? FeeAmount { get; set; }
            /// <summary>
            /// 查詢配送方式的URL
            /// </summary>
            [JsonPropertyName("feeInquiryUrl")]
            public string FeeInquiryUrl { get; set; }
            /// <summary>
            /// 運費查詢類型
            /// ❏CONDITION：收貨地發生變化，就查詢配送方式（運費）
            /// ❏FIXED：作為固定值，收貨地發生變化，也不會查詢配送方式
            /// </summary>
            [JsonPropertyName("feeInquiryType")]
            [JsonConverter(typeof(FeeInquiryTypeJsonConverter))]
            public FeeInquiryType? FeeInquiryType { get; set; }
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
            }
        }
        public class FamilyServiceOption
        {
            /// <summary>
            /// 新增好友的服務類型
            /// </summary>
            [JsonPropertyName("type")]
            [JsonConverter(typeof(FamilyServiceTypeJsonConverter))]
            public FamilyServiceType Type { get; set; }
            /// <summary>
            /// 各服務類型的ID list
            /// </summary>
            [JsonPropertyName("idList")]
            public List<string> IdList { get; set; }
        }

        public class ExtraOption
        {
            /// <summary>
            /// 商店或分店名稱(僅會顯示前 100 字元)
            /// </summary>
            [JsonPropertyName("branchName")]
            public string BranchName { get; set; }
            /// <summary>
            /// 商店或分店代號，可支援英數字及特殊字元
            /// </summary>
            [JsonPropertyName("branchId")]
            public string BranchId { get; set; }
            /// <summary>
            /// 點數限制資訊
            /// </summary>
            [JsonPropertyName("promotionRestriction")]
            public PromotionRestrictionOption PromotionRestriction { get; set; }           
        }
    }
}