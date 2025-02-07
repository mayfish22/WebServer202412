using System.Text.Json.Serialization;

namespace WebServer.Models.LINEPayModels;

public class RecipientOption
{
    /// <summary>
    /// 收貨人名
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }
    /// <summary>
    /// 收貨人姓
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }
    /// <summary>
    /// 詳細名資訊
    /// </summary>
    [JsonPropertyName("firstNameOptional")]
    public string FirstNameOptional { get; set; }
    /// <summary>
    /// 詳細姓資訊
    /// </summary>
    [JsonPropertyName("lastNameOptional")]
    public string LastNameOptional { get; set; }
    /// <summary>
    /// 收貨人電子郵件
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; }
    /// <summary>
    /// 收貨人電話號碼
    /// </summary>
    [JsonPropertyName("phoneNo")]
    public string PhoneNo { get; set; }
}