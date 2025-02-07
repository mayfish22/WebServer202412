using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB;

[ModelMetadataType(typeof(CartItemMetadata))]
public partial class CartItem 
{
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public string? ProductCode { get; set; }
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public string? ProductName { get; set; }
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public string? MainImageURL { get; set; }
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public decimal? UnitPrice { get; set; }
}

public partial class CartItemMetadata
{
    
}