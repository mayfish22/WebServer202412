using System.Text;
using System.Text.Json;

namespace WebServer.Services;

public class GeminiAPIService
{
    // 私有只讀變數，用於存儲 IHttpClientFactory 的實例
    private readonly IHttpClientFactory _httpClient;
    private readonly string _url = @"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={0}";
    private readonly string _apiKey;
    public GeminiAPIService(IHttpClientFactory httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration.GetValue<string>("Google:GeminiAPIKey");
    }

    public async Task<string?> GetResult(string text)
    {
        // 初始化 Profile 變數為 null
        string? result = string.Empty;
        try
        {
            var requestData = new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new {
                                text = text 
                            },
                        }
                    },
                },
            };

            // 格式化 URL，將 userId 插入到指定的 URL 模板中
            var url = string.Format(_url, _apiKey);

            // 創建一個 HTTP 請求，使用 GET 方法和指定的 URL
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            // 設置請求內容，將 requestData 序列化為 JSON 格式
            request.Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            // 使用 HttpClient 創建一個客戶端
            var client = _httpClient.CreateClient();

            // 發送請求並等待響應
            var response = await client.SendAsync(request);

            var responseValue = string.Empty;

            // 讀取響應內容的流
            var stream = await response.Content.ReadAsStreamAsync();

            // 使用 StreamReader 讀取流中的內容
            using var reader = new StreamReader(stream);
            responseValue = await reader.ReadToEndAsync();

            // 檢查響應是否成功
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(responseValue);
            }
            else
            {
                // 使用 JsonDocument 解析 JSON 字符串
                using JsonDocument doc = JsonDocument.Parse(responseValue);
                // 獲取根元素
                JsonElement root = doc.RootElement;

                // 獲取 candidates 陣列
                JsonElement candidates = root.GetProperty("candidates");

                // 獲取第一個候選者
                JsonElement firstCandidate = candidates[0];

                // 獲取 content 屬性
                JsonElement content = firstCandidate.GetProperty("content");

                // 獲取 parts 陣列
                JsonElement parts = content.GetProperty("parts");

                // 獲取第一個 part
                JsonElement firstPart = parts[0];

                // 獲取 text 屬性
                string textValue = firstPart.GetProperty("text").GetString();

                // 輸出 text 的值
                result = textValue;
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並輸出異常消息到控制台，後續需要進一步處理
            Console.WriteLine(ex.Message);
        }

        // 返回獲取的 Profile 對象，可能為 null
        return result;
    }
}