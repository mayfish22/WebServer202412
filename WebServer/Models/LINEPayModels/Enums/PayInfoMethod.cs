namespace WebServer.Models.LINEPayModels.Enums;

public enum PayInfoMethod
{
    /// <summary>
    /// 信用卡：CREDIT_CARD
    /// </summary>
    CREDIT_CARD,
    /// <summary>
    /// 餘額：BALANCE
    /// </summary>
    BALANCE,
    /// <summary>
    /// 折扣：DISCOUNT(發票金額須扣除)
    /// </summary>
    DISCOUNT,
    /// <summary>
    /// LINE POINTS：POINT(預設不顯示)
    /// </summary>
    POINT,
}