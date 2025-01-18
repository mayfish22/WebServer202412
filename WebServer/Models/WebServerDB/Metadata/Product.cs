using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB;

// 使用 ModelMetadataType 特性來指定元資料類別
[ModelMetadataType(typeof(ProductMetadata))]
public partial class Product  // 定義產品類別
{
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public string? MainImageURL { get; set; }  // 主要圖片的 URL 屬性
}

// 定義產品的元資料類別
public partial class ProductMetadata
{
    public Guid ID { get; set; }  // 產品的唯一識別碼，使用 GUID 類型

    [Display(Name = "產品編號")]  // 設定顯示名稱為「產品編號」
    [MaxLength(20)]  // 限制產品編號的最大長度為 20 字元
    [Required(ErrorMessage = "產品編號必填")]  // 設定為必填欄位，若未填寫則顯示錯誤訊息
    public string ProductCode { get; set; }  // 產品編號屬性

    [Display(Name = "產品名稱")]  // 設定顯示名稱為「產品名稱」
    [MaxLength(50)]  // 限制產品名稱的最大長度為 50 字元
    [Required(ErrorMessage = "產品名稱必填")]  // 設定為必填欄位，若未填寫則顯示錯誤訊息
    public string ProductName { get; set; }  // 產品名稱屬性

    [Display(Name = "產品描述")]  // 設定顯示名稱為「產品描述」
    [MaxLength(500)]  // 限制產品描述的最大長度為 500 字元
    public string ProductDescription { get; set; }  // 產品描述屬性

    [Display(Name = "單價")]  // 設定顯示名稱為「單價」
    [Required(ErrorMessage = "單價必填")]  // 設定為必填欄位，若未填寫則顯示錯誤訊息
    public decimal UnitPrice { get; set; }  // 單價屬性，使用 decimal 類型以支持金額

    [Display(Name = "主要產品圖片")]  // 設定顯示名稱為「主要產品圖片」
    public Guid? MainImageFileID { get; set; }  // 主要產品圖片的檔案識別碼，使用可為空的 GUID 類型

    [Display(Name = "創建時間")]  // 設定顯示名稱為「創建時間」
    public DateTime CreatedDT { get; set; }  // 記錄創建時間，使用 DateTime 類型

    [Display(Name = "最後修改時間")]  // 設定顯示名稱為「最後修改時間」
    public DateTime? ModifiedDT { get; set; }  // 記錄最後修改時間，使用可為空的 DateTime 類型
}