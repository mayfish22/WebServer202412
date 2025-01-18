using Microsoft.AspNetCore.Authentication.Cookies; // 引入 Cookie 認證相關的命名空間
using Microsoft.AspNetCore.Authentication; // 引入認證相關的命名空間
using Microsoft.AspNetCore.Mvc; // 引入 MVC 控制器相關的命名空間
using System.Security.Claims; // 引入安全性聲明相關的命名空間
using WebServer.Models.WebServerDB; // 引入 WebServerDB 模型相關的命名空間
using WebServer.Services; // 引入服務相關的命名空間
using System.Text.Json; // 引入 JSON 處理相關的命名空間

namespace WebServer.Controllers;

// 設定路由，控制器的路由格式為 {controller}/{action=Index}
[Route("{controller}/{action=Index}")]
public class LIFFController : Controller
{
    private readonly WebServerDBContext _webServerDB; // 用於訪問資料庫的上下文
    private readonly LINEAPIService _lineAPIService; // 用於與 LINE API 進行交互的服務

    // 控制器的建構函數，注入資料庫上下文和 LINE API 服務
    public LIFFController(WebServerDBContext webServerDB, LINEAPIService lineAPIService)
    {
        _webServerDB = webServerDB;
        _lineAPIService = lineAPIService;
    }

    // GET: /LIFF/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Task.Yield(); // 讓控制器的執行權限回到調用者，這裡可以用於模擬非同步操作
        return View(@"~/Views/LIFF/Index.cshtml"); // 返回視圖
    }

    // 用於接收從前端發送的請求以獲取用戶資料的參數類別
    public class GetProfileParameter
    {
        public string AccessToken { get; set; } // 用於 LINE 登入的訪問令牌
    }

    // POST: /LIFF/GetProfile
    [HttpPost]
    public async Task<IActionResult> GetProfile([FromBody] GetProfileParameter para)
    {
        // 通過 LINE API 獲取用戶資料
        var profile = await _lineAPIService.GetProfileByLINELogin(para.AccessToken);
        if (profile != null) // 如果成功獲取到用戶資料
        {
            // 嘗試在資料庫中查找該用戶
            var lineUser = await _webServerDB.LINEUser.FindAsync(profile.UserID);
            if (lineUser == null) // 如果用戶不存在，則創建新用戶
            {
                lineUser = new LINEUser
                {
                    ID = profile.UserID,
                    DisplaName = profile.DisplayName,
                    PictureUrl = profile.PictureUrl,
                    Language = profile.Language,
                    StatusMessage = profile.StatusMessage,
                };
                await _webServerDB.LINEUser.AddAsync(lineUser); // 將新用戶添加到資料庫
            }
            else // 如果用戶已存在，則更新用戶資料
            {
                lineUser.DisplaName = profile.DisplayName;
                lineUser.PictureUrl = profile.PictureUrl;
                lineUser.Language = profile.Language;
                lineUser.StatusMessage = profile.StatusMessage;
            }
            await _webServerDB.SaveChangesAsync(); // 保存更改到資料庫

            // 設定 Cookie 認證
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, nameof(LINEUser)), // 設定用戶角色
                new Claim(ClaimTypes.NameIdentifier, profile.UserID), // 設定用戶 ID
                new Claim(ClaimTypes.Name, profile.DisplayName), // 設定用戶顯示名稱
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); // 創建 ClaimsIdentity
            var principal = new ClaimsPrincipal(identity); // 創建 ClaimsPrincipal

            await HttpContext.SignInAsync(principal); // 登入用戶
        }

        // 返回用戶資料的 JSON 格式，保持屬性名稱不變
        return Json(profile, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // 保持屬性名稱不變
        });
    }
}
