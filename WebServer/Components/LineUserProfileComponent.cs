using Microsoft.AspNetCore.Mvc;
using WebServer.Services;

namespace WebServer.Components;

// 設定此類別為 ViewComponent，並指定名稱為 "LineUserProfile"
[ViewComponent(Name = "LineUserProfile")]
public class LineUserProfileComponent : ViewComponent
{
    private readonly SiteService _siteService; 

    public LineUserProfileComponent(SiteService siteService)
    {
        _siteService = siteService; 
    }

    // 異步方法，負責執行 ViewComponent 的邏輯
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userProfile = await _siteService.GetCurrentLINEUser();
        // 返回視圖，並傳遞使用者資料（如果有的話）
        return View("Default", userProfile);
    }
}