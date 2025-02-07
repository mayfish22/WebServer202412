using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using WebServer.Models.LINEPayModels;
using Serilog;
using System.Text.Json;

namespace WebServer.Services.Invocables;

public class CheckPaymentStatusAPIInvocablePara
{
    public long TransactionId { get; set; }
    public RequestAPIRequestBody Data { get; set; }
}

public class CheckPaymentStatusAPIInvocable : IInvocable, IInvocableWithPayload<CheckPaymentStatusAPIInvocablePara>
{
    // This is the implementation of the interface 👇
    public CheckPaymentStatusAPIInvocablePara Payload { get; set; }

    /* Constructor, etc. */
    private readonly LINEPayService _LINEPayService;
    private readonly IQueue _iqueue;
    public CheckPaymentStatusAPIInvocable(LINEPayService LINEPayService, IQueue iqueue)
    {
        _LINEPayService = LINEPayService;
        _iqueue = iqueue;
    }
    public async Task Invoke()
    {
        try
        {
            var response = await _LINEPayService.CheckPaymentStatusAPI(Payload.TransactionId);

            if (response.Result != null)
            {
                switch (response.Result.ReturnCode)
                {
                    case "0000":
                        //授權尚未完成
#if DEBUG
                        Log.Information(Payload.TransactionId + " CheckPaymentStatusAPI授權尚未完成");
#endif
                        await Task.Delay(3000); // 檢查間隔
                        _iqueue.QueueInvocableWithPayload<CheckPaymentStatusAPIInvocable, CheckPaymentStatusAPIInvocablePara>(Payload);
                        break;
                    case "0110":
                        //授權完成 - 現在可以呼叫Confirm API
                        Log.Information(Payload.TransactionId + " 授權完成 - 現在可以呼叫Confirm API");
                        _iqueue.QueueInvocableWithPayload<ConfirmAPIInvocable, ConfirmAPIInvocablePara>(new ConfirmAPIInvocablePara
                        {
                            TransactionId = Payload.TransactionId,
                            Data = Payload.Data,
                        });
                        break;

                    default:
                        //有問題, 之後再處理
                        Log.Information(Payload.TransactionId + " CheckPaymentStatusAPI例外情況");
                        break;
                }
            }
            else
            {
                Log.Error("CheckPaymentStatusAPIInvocable: {@Payload}", Payload);
                Log.Error("CheckPaymentStatusAPIInvocable: {@response}", response);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CheckPaymentStatusAPIInvocable: {@Payload}", Payload);
        }
    }
}
/*
0000	授權尚未完成
0110	授權完成 - 現在可以呼叫Confirm API
0121	該交易已被用戶取消，或者超時取消（20分鐘）- 交易已經結束了
0122	付款失敗 - 交易已經結束了
0123	付款成功 - 交易已經結束了
1104	此商家不存在
1105	此商家處於無法使用LINE Pay的狀態
9000	內部錯誤
 */