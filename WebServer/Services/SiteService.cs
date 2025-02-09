using Serilog;
using System.Security.Claims;
using WebServer.Models.WebServerDB;

namespace WebServer.Services;

/// <summary>
/// 網站服務類別
/// </summary>
public class SiteService
{
    private readonly WebServerDBContext _webServerDB;
    private readonly IHttpContextAccessor _httpContext; 

    public SiteService(WebServerDBContext webServerDB, IHttpContextAccessor httpContext)
    {
        _webServerDB = webServerDB; // 將傳入的上下文賦值給私有字段
        _httpContext = httpContext;
    }

    /// <summary>
    /// 獲取所有 LINEUser
    /// </summary>
    /// <returns></returns>
    public IQueryable<LINEUser> GetLINEUsers()
    {
        // 使用 LINQ 查詢從數據庫中選擇所有 LINEUser
        return _webServerDB.LINEUser.Select(s => s);
    }

    /// <summary>
    /// 獲取指定 LINEUser 的購物車項目
    /// </summary>
    /// <param name="lineUserId"></param>
    /// <returns></returns>
    public IQueryable<CartItem> GetCartItems(string lineUserId)
    {
        // 使用 LINQ 查詢從數據庫中該名 LINEUser 的所有 CartItem
        return _webServerDB.CartItem.Where(s => s.LINEUserID.Equals(lineUserId)).OrderBy(s => s.Seq).Select(s => s);
    }

    /// <summary>
    /// 獲取當前使用者的 LINEUser 資料
    /// </summary>
    /// <returns></returns>
    public async Task<LINEUser?> GetCurrentLINEUser()
    {
        LINEUser? userProfile = null; // 用於儲存使用者資料的變數
        try
        {
            // 獲取當前 HttpContext
            var httpContext = _httpContext.HttpContext;
            // 確保 HttpContext 不為 null
            if (httpContext != null)
            {
                // 獲取當前使用者的 ClaimsPrincipal
                var user = httpContext.User;
                // 確保使用者已登入
                if (user.Identity.IsAuthenticated)
                {
                    // 從 Claims 中獲取 LINEUser ID
                    var lineUserIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    // 從資料庫中查找使用者資料
                    userProfile = await _webServerDB.LINEUser.FindAsync(lineUserIdClaim.Value);
                }
            }
        }
        catch (Exception ex)
        {
            // 記錄錯誤資訊
            Log.Error(ex, nameof(SiteService));
        }
        return userProfile;
    }
}