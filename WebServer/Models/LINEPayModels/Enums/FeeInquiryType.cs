namespace WebServer.Models.LINEPayModels.Enums;

public enum FeeInquiryType
{
    /// <summary>
    /// 收貨地發生變化，就查詢配送方式（運費）
    /// </summary>
    CONDITION,
    /// <summary>
    /// 作為固定值，收貨地發生變化，也不會查詢配送方式
    /// </summary>
    FIXED
}