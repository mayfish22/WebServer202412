using System.ComponentModel;
using System.ComponentModel.DataAnnotations; 
using WebServer.Models.WebServerDB; 
using WebServer.Services; 

namespace WebServer.Models.ViewModels;

// 定義產品視圖模型類別，實現 IValidatableObject 接口以支持自定義驗證
public class ProductViewModel : IValidatableObject
{
    // 屬性：指示產品是否為唯讀
    public bool IsReadonly { get; set; }

    // 屬性：用於存儲錯誤訊息
    public string? ErrorMessage { get; set; }

    // 屬性：產品
    public Product Product { get; set; }

    // 屬性：產品圖片
    [DisplayName("產品圖片明細")]
    public Guid[]? ProductImages { get; set; }

    // 實現 IValidatableObject 接口的方法，用於自定義驗證邏輯
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // 從驗證上下文中獲取 ValidatorService 的實例
        var service = validationContext.GetService<ValidatorService>();

        // 使用 ValidatorService 進行產品資料的驗證
        var rs = service!.ValidateProduct(Product);

        // 如果驗證結果不為 null 且有任何錯誤
        if (rs != null && rs.Any())
        {
            // 將驗證結果轉換為 ValidationResult 集合
            var x = rs.Select(r => new ValidationResult(
                r.Text, // 錯誤訊息
                new[] { string.IsNullOrEmpty(r.ElementID) ? nameof(ErrorMessage) : r.ElementID } // 錯誤對應的屬性名稱
            ));
            return x; // 返回所有的驗證結果
        }

        // 如果沒有錯誤，返回空的驗證結果集合
        return Enumerable.Empty<ValidationResult>();
    }
}