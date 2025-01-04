using Coravel.Invocable; // 引入 Coravel 的可調用接口
using WebServer.Models.LINEModels; // 引入 LINE 相關模型

namespace WebServer.Services.Invocables;

// 定義 LINEWebhookInvocable 類，實現 IInvocable 和 IInvocableWithPayload<Webhook> 接口
public class LINEWebhookInvocable : IInvocable, IInvocableWithPayload<Webhook>
{
    // 定義 Payload 屬性，表示接收到的 Webhook 數據
    public Webhook Payload { get; set; }

    // 定義 LINEAPIService 的私有只讀字段，用於處理 LINE API 的請求
    private readonly LINEAPIService _lineAPIService;

    // 定義 GeminiAPIService 的私有只讀字段，用於處理 Gemini API 的請求
    private readonly GeminiAPIService _geminiAPIService;

    // 構造函數，通過依賴注入獲取 LINEAPIService 實例
    public LINEWebhookInvocable(LINEAPIService lineAPIService, GeminiAPIService geminiAPIService)
    {
        _lineAPIService = lineAPIService;
        _geminiAPIService = geminiAPIService;
    }

    // 實現 IInvocable 接口的 Invoke 方法，這是執行任務的主要邏輯
    public async Task Invoke()
    {
        try
        {
            // 檢查 Payload 的 Events 是否為 null，若為 null 則拋出異常
            if (Payload.Events == null)
                throw new Exception("未知的錯誤：webhook.Events == null");

            // 獲取第一個事件
            var lineWebhookEvent = Payload.Events.FirstOrDefault();

            // 檢查 lineWebhookEvent 是否為 null，若為 null 則拋出異常
            if (lineWebhookEvent == null)
                throw new Exception("未知的錯誤：lineWebhookEvent == null");

            // 獲取事件類型
            string eventType = lineWebhookEvent.Type;

            // 根據事件類型執行相應的操作
            switch (eventType)
            {
                case nameof(EventType.message):
                    var geminiResult = await _geminiAPIService.GetResult($"你是一個智能客服，回覆不要提及你是誰，若有人問說回答【智能客服】，此外都用正體中文回覆，然後接下來使用者提問的問題，太過敏感的問題就說【這個問題我不了解!】。以下是使用者的問題：{lineWebhookEvent.Message?.Text}");
                    // 當事件類型為消息時，回覆用戶消息
                    await _lineAPIService.ReplyMessage(lineWebhookEvent.ReplyToken, new object[]
                    {
                        new {
                            type = "text", // 設置消息類型為文本
                            text = geminiResult , // 改用 Gemini 回覆
                            notificationDisabled = false, // 設置通知為啟用
                        }
                    });
                    break;

                case nameof(EventType.follow):
                    // 當事件類型為關注時，處理用戶關注事件
                    await _lineAPIService.Follow(lineWebhookEvent.Source.UserID);
                    break;

                case nameof(EventType.unfollow):
                    // 當事件類型為取消關注時，處理用戶取消關注事件
                    await _lineAPIService.Unfollow(lineWebhookEvent.Source.UserID);
                    break;

                default:
                    // 其他事件類型不做處理
                    break;
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並輸出異常消息到控制台，後續需要進一步處理
            Console.WriteLine(ex.Message);
        }
    }
}