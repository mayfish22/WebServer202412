using System.Text;
using System.Text.Json;
using WebServer.Models.LINEModels;
using WebServer.Models.WebServerDB;

namespace WebServer.Services;

public class LINEAPIService
{
    // 私有只讀變數，用於存儲 WebServerDBContext 的實例
    private readonly WebServerDBContext _webServerDB;

    // 私有只讀變數，用於存儲 IHttpClientFactory 的實例
    private readonly IHttpClientFactory _httpClient;

    // 私有只讀變數，用於存儲 webhook 驗證時傳入的測試回覆令牌
    private readonly string _testReplyToken;

    // 私有只讀變數，用於存儲 Messaging API 的存取令牌
    private readonly string _accessToken;

    // 私有只讀變數，用於存儲獲取用戶資料的 API URL，{0} 將被用戶 ID 替換
    private readonly string _urlProfileByBot = @"https://api.line.me/v2/bot/profile/{0}";

    // 私有只讀變數，用於存儲推送消息的 API URL
    private readonly string _urlPushMessage = @"https://api.line.me/v2/bot/message/push";

    // 私有只讀變數，用於存儲回覆消息的 API URL
    private readonly string _urlReplyMessage = @"https://api.line.me/v2/bot/message/reply";

    // 構造函數，接收 WebServerDBContext、IHttpClientFactory 和 IConfiguration 的實例
    public LINEAPIService(WebServerDBContext webServerDB, IHttpClientFactory httpClient, IConfiguration configuration)
    {
        // 初始化 _webServerDB 變數
        _webServerDB = webServerDB;

        // 初始化 _httpClient 變數
        _httpClient = httpClient;

        // 從配置中獲取 LINE 測試回覆令牌並初始化 _testReplyToken 變數
        _testReplyToken = configuration.GetValue<string>("LINE:TestReplyToken");

        // 從配置中獲取 Messaging API 的存取令牌並初始化 _accessToken 變數
        _accessToken = configuration.GetValue<string>("LINE:MessagingAPIChannelAccessToken");
    }

    // 定義 ParseWebhook 方法，接受一個字串參數 requestContent，返回 Webhook 對象或 null
    public Webhook? ParseWebhook(string requestContent)
    {
        try
        {
            // 嘗試將 JSON 格式的 requestContent 反序列化為 Webhook 對象
            return JsonSerializer.Deserialize<Webhook>(requestContent);
        }
        catch (Exception ex)
        {
            // 如果反序列化過程中發生異常，輸出異常消息到控制台
            Console.WriteLine(ex.Message);
        }

        // 如果反序列化失敗，返回 null
        return null;
    }

    // 定義 IsTest 方法，接受一個 Webhook 對象作為參數，返回布林值
    public bool IsTest(Webhook webhook)
    {
        // 檢查 webhook 的 Events 屬性是否為 null
        if (webhook.Events == null)
            return false; // 如果為 null，返回 false，表示不是測試 webhook

        // 獲取 webhook.Events 中的第一個事件
        var lineWebhookEvent = webhook.Events.FirstOrDefault();

        // 返回 true 如果 lineWebhookEvent 為 null 或其 ReplyToken 與 _testReplyToken 相同
        return lineWebhookEvent == null || lineWebhookEvent.ReplyToken == _testReplyToken;
    }

    // 定義一個異步方法 GetProfileByBot，接受一個字串參數 userId，返回一個可為 null 的 Profile 對象
    public async Task<Profile?> GetProfileByBot(string userId)
    {
        // 初始化 Profile 變數為 null
        Profile? profile = null;

        try
        {
            // 格式化 URL，將 userId 插入到指定的 URL 模板中
            var url = string.Format(_urlProfileByBot, userId ?? string.Empty);

            // 創建一個 HTTP 請求，使用 GET 方法和指定的 URL
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // 添加授權標頭，使用 Bearer Token 認證
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            // 使用 HttpClient 創建一個客戶端
            var client = _httpClient.CreateClient();

            // 發送請求並等待響應
            var response = await client.SendAsync(request);

            // 檢查響應是否成功
            if (response.IsSuccessStatusCode)
            {
                var responseValue = string.Empty;

                // 讀取響應內容的流
                var stream = await response.Content.ReadAsStreamAsync();

                // 使用 StreamReader 讀取流中的內容
                using var reader = new StreamReader(stream);
                responseValue = await reader.ReadToEndAsync();

                // 將讀取的 JSON 字串反序列化為 Profile 對象
                profile = JsonSerializer.Deserialize<Profile>(responseValue);
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並輸出異常消息到控制台，後續需要進一步處理
            Console.WriteLine(ex.Message);
        }

        // 返回獲取的 Profile 對象，可能為 null
        return profile;
    }
    public async Task Follow(string userId)
    {
        try
        {
            var userProfile = await GetProfileByBot(userId);
            if (userProfile == null)
                throw new Exception("無法取得使用者資訊UserID：" + userId);

            var lineUser = await _webServerDB.LINEUser.FindAsync(userId);
            if (lineUser == null)
            {
                lineUser = new LINEUser
                {
                    ID = userId,
                    DisplaName = userProfile.DisplayName,
                    PictureUrl = userProfile.PictureUrl,
                    Language = userProfile.Language,
                    StatusMessage = userProfile.StatusMessage,
                    FollowDT = DateTime.Now,
                };
                await _webServerDB.LINEUser.AddAsync(lineUser);
            }
            else
            {
                lineUser.DisplaName = userProfile.DisplayName;
                lineUser.PictureUrl = userProfile.PictureUrl;
                lineUser.Language = userProfile.Language;
                lineUser.StatusMessage = userProfile.StatusMessage;
                lineUser.FollowDT = DateTime.Now;
            }

            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // 後續需要再處理
            Console.WriteLine(ex.Message);
        }
    }
    // 定義一個異步方法 Unfollow，接受一個字串參數 userId，無返回值
    public async Task Unfollow(string userId)
    {
        try
        {
            // 在資料庫中查找該 userId 的 LINEUser
            var lineUser = await _webServerDB.LINEUser.FindAsync(userId);

            // 檢查是否找到該使用者
            if (lineUser == null)
            {
                // 如果未找到，拋出異常並顯示錯誤信息
                throw new Exception("使用者不存在UserID：" + userId);
            }
            else
            {
                // 如果找到，更新該使用者的 UnfollowDT 為當前時間
                lineUser.UnfollowDT = DateTime.Now;

                // 保存對資料庫的更改
                await _webServerDB.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並輸出異常消息到控制台，後續需要進一步處理
            Console.WriteLine(ex.Message);
        }
    }

    // 定義一個異步方法 ReplyMessage，接受一個字串參數 replyToken 和一個物件陣列 messages，無返回值
    public async Task ReplyMessage(string replyToken, object[] messages)
    {
        try
        {
            // 構建請求數據，包含 replyToken 和 messages
            var requestData = new
            {
                replyToken,
                messages,
            };

            // 創建一個 HTTP POST 請求
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _urlReplyMessage);

            // 添加授權標頭，使用 Bearer token 進行身份驗證
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            // 設置請求內容，將 requestData 序列化為 JSON 格式
            request.Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            // 創建 HTTP 客戶端並發送請求
            var client = _httpClient.CreateClient();
            var response = await client.SendAsync(request);

            // 檢查響應是否成功
            if (!response.IsSuccessStatusCode)
            {
                var responseValue = string.Empty;
                // 讀取響應內容流
                var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                responseValue = await reader.ReadToEndAsync(); // 讀取響應內容

                // 如果響應不成功，拋出異常並顯示響應內容
                throw new Exception(responseValue);
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並輸出異常消息到控制台，後續需要進一步處理
            Console.WriteLine(ex.Message);
        }
    }

    // 定義一個異步方法 SendMessage，接受一個字串參數 userId 和一個物件陣列 messages，返回 MessageStatus
    public async Task<MessageStatus> SendMessage(string userId, object[] messages)
    {
        try
        {
            // 構建推送請求數據，包含接收者 userId 和要發送的 messages
            var pushRequestData = new
            {
                to = userId,
                messages,
            };

            // 創建一個 HTTP POST 請求
            var request = new HttpRequestMessage(HttpMethod.Post, _urlPushMessage);

            // 添加授權標頭，使用 Bearer token 進行身份驗證
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            // 設置請求內容，將 pushRequestData 序列化為 JSON 格式
            request.Content = new StringContent(JsonSerializer.Serialize(pushRequestData), Encoding.UTF8, "application/json");

            // 創建 HTTP 客戶端並發送請求
            var client = _httpClient.CreateClient();
            var response = await client.SendAsync(request);

            // 檢查響應是否成功
            if (response.IsSuccessStatusCode)
            {
                // 如果成功，返回 MessageStatus.sent
                return MessageStatus.sent;
            }
            else
            {
                var responseValue = string.Empty;
                // 讀取響應內容流
                var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                responseValue = await reader.ReadToEndAsync(); // 讀取響應內容

                // 如果響應不成功，拋出異常並顯示響應內容
                throw new Exception(responseValue);
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並輸出異常消息到控制台，後續需要進一步處理
            Console.WriteLine(ex.Message);
        }

        // 如果發生異常，返回 MessageStatus.error
        return MessageStatus.error;
    }
}