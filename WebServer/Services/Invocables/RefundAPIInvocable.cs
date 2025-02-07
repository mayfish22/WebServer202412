using Coravel.Invocable;
using WebServer.Models.LINEPayModels;
using Serilog;
using System.Text.Json;

namespace WebServer.Services.Invocables;

public class RefundAPIInvocablePara
{
    public long TransactionId { get; set; }
    public RequestAPIRequestBody Data { get; set; }
    public RefundAPIRequestBody RequestBody { get; set; }
}

public class RefundAPIInvocable : IInvocable, IInvocableWithPayload<RefundAPIInvocablePara>
{
    // This is the implementation of the interface 👇
    public RefundAPIInvocablePara Payload { get; set; }

    /* Constructor, etc. */
    private readonly LINEPayService _LINEPayService;
    public RefundAPIInvocable(LINEPayService LINEPayService)
    {
        _LINEPayService = LINEPayService;
    }
    public async Task Invoke()
    {
        try
        {

            var response = await _LINEPayService.RefundAPI(Payload.Data.OrderId, Payload.TransactionId, Payload.RequestBody);

            if (response.Result != null)
            {
                switch (response.Result.ReturnCode)
                {
                    case "0000":
                        //成功
                        Log.Information(Payload.TransactionId + " RefundAPI成功");
                        break;

                    default:
                        //有問題, 之後再處理
                        Log.Information(Payload.TransactionId + " RefundAPI例外情況");
                        break;
                }
            }
            else
            {
                Log.Error("RefundAPIInvocablePara: {@Payload}", Payload);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RefundAPIInvocablePara: {@Payload}", Payload);
        }
    }
}
