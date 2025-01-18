using Microsoft.AspNetCore.Mvc; 
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB;

// 使用 ModelMetadataType 特性來指定元資料類別
[ModelMetadataType(typeof(ProductImageMetadata))]
public partial class ProductImage  // 定義產品圖片類別
{
    [NotMapped]  // 標記此屬性不應映射到資料庫中的任何列
    public bool IsReadonly { get; set; }  // 此屬性用於指示產品圖片是否為唯讀狀態
}

// 定義產品圖片的元資料類別
public partial class ProductImageMetadata
{
    // 此類別可用於定義與 ProductImage 類別相關的元資料
    // 目前為空，未定義任何屬性或註解
}