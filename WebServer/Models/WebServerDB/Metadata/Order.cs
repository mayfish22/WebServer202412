using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB;

[ModelMetadataType(typeof(OrderMetadata))]
public partial class Order
{
    /// <summary>
    /// 付款連結
    /// </summary>
    [NotMapped]  
    public string? PaymentUrl { get; set; }
}

public partial class OrderMetadata
{
    public Guid ID { get; set; }
    [Display(Name = "日期")]  // 設定顯示名稱為「日期」
    public DateOnly OrderDate { get; set; }
    [Display(Name = "序號")]  // 設定顯示名稱為「序號」
    public int OrderSeq { get; set; }
    [Display(Name = "訂單編號")]  // 設定顯示名稱為「訂單編號」
    public string OrderNo { get; set; }
    [Display(Name = "總金額")]  // 設定顯示名稱為「總金額」
    public decimal TotalAmount { get; set; }
    [Display(Name = "狀態")]  // 設定顯示名稱為「狀態」
    public string PaymentStatus { get; set; }

    public string LINEUserID { get; set; }
    [Display(Name = "備註")]  // 設定顯示名稱為「備註」
    public string Remark { get; set; }
    [Display(Name = "創建時間")]  // 設定顯示名稱為「創建時間」
    public DateTime CreatedDT { get; set; }
    [Display(Name = "最後修改時間")]  // 設定顯示名稱為「最後修改時間」
    public DateTime? ModifiedDT { get; set; }
}