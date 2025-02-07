using Coravel.Invocable;
using WebServer.Models.LINEPayModels;
using Serilog;
using System.Text.Json;

namespace WebServer.Services.Invocables;

public class CaptureAPIInvocablePara
{
    public long TransactionId { get; set; }
    public RequestAPIRequestBody Data { get; set; }
}

public class CaptureAPIInvocable : IInvocable, IInvocableWithPayload<CaptureAPIInvocablePara>
{
    // This is the implementation of the interface 👇
    public CaptureAPIInvocablePara Payload { get; set; }

    /* Constructor, etc. */
    private readonly LINEPayService _LINEPayService;
    public CaptureAPIInvocable(LINEPayService LINEPayService)
    {
        _LINEPayService = LINEPayService;
    }
    public async Task Invoke()
    {
        try
        {
            var response = await _LINEPayService.CaptureAPI(Payload.Data.OrderId, Payload.TransactionId, new CaptureAPIRequestBody
            {
                Amount = Payload.Data.Amount,
                Currency = Payload.Data.Currency,
                Options = new CaptureAPIRequestBody.Option
                {
                    Extra = new CaptureAPIRequestBody.Option.ExtraOption
                    {
                        PromotionRestriction = Payload?.Data?.Options?.Extra?.PromotionRestriction,
                    }
                }
            });

            if (response.Result != null)
            {
                switch (response.Result.ReturnCode)
                {
                    case "0000":
                        //成功
                        Log.Information(Payload.TransactionId + " CaptureAPI成功");
                        break;

                    default:
                        //有問題, 之後再處理
                        Log.Information(Payload.TransactionId + " CaptureAPI例外情況");
                        break;
                }
            }
            else
            {
                Log.Error("ConfirmAPIInvocablePara: {@Payload}", Payload);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ConfirmAPIInvocablePara: {@Payload}", Payload);
        }
    }
}
