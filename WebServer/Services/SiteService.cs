// 引入 WebServerDB 的模型命名空間
using WebServer.Models.WebServerDB;

namespace WebServer.Services;

// 定義 SiteService 類，負責與 WebServerDB 進行交互
public class SiteService
{
    // 定義私有只讀字段，用於存儲 WebServerDBContext 的實例
    private readonly WebServerDBContext _webServerDB;

    // 構造函數，通過依賴注入獲取 WebServerDBContext 實例
    public SiteService(WebServerDBContext webServerDB)
    {
        _webServerDB = webServerDB; // 將傳入的上下文賦值給私有字段
    }

    // 定義一個方法，返回所有 LINEUser 的查詢結果
    public IQueryable<LINEUser> GetLINEUsers()
    {
        // 使用 LINQ 查詢從數據庫中選擇所有 LINEUser
        return _webServerDB.LINEUser.Select(s => s);
    }
}