using Coravel.Invocable;
using WebServer.Models.LINEPayModels;
using Serilog;
using System.Text.Json;

namespace WebServer.Services.Invocables;

public class VoidAPIInvocablePara
{
    public long TransactionId { get; set; }
    public RequestAPIRequestBody Data { get; set; }
}

public class VoidAPIInvocable : IInvocable, IInvocableWithPayload<VoidAPIInvocablePara>
{
    // This is the implementation of the interface 👇
    public VoidAPIInvocablePara Payload { get; set; }

    /* Constructor, etc. */
    private readonly LINEPayService _LINEPayService;
    public VoidAPIInvocable(LINEPayService LINEPayService)
    {
        _LINEPayService = LINEPayService;
    }
    public async Task Invoke()
    {
        try
        {
            var response = await _LINEPayService.VoidAPI(Payload.Data.OrderId, Payload.TransactionId);

            if (response.Result != null)
            {
                switch (response.Result.ReturnCode)
                {
                    case "0000":
                        //成功
                        Log.Information(Payload.TransactionId + " VoidAPI成功");
                        break;

                    default:
                        //有問題, 之後再處理
                        Log.Information(Payload.TransactionId + " VoidAPI例外情況");
                        break;
                }
            }
            else
            {
                Log.Error("VoidAPIInvocablePara: {@Payload}", Payload);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "VoidAPIInvocablePara: {@Payload}", Payload);
        }
    }
}
