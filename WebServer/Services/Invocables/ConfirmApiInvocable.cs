using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using WebServer.Models.LINEPayModels;
using Serilog;
using System.Text.Json;

namespace WebServer.Services.Invocables;

public class ConfirmAPIInvocablePara
{
    public long TransactionId { get; set; }
    public RequestAPIRequestBody Data { get; set; }
}

public class ConfirmAPIInvocable : IInvocable, IInvocableWithPayload<ConfirmAPIInvocablePara>
{
    // This is the implementation of the interface 👇
    public ConfirmAPIInvocablePara Payload { get; set; }

    /* Constructor, etc. */
    private readonly LINEPayService _LINEPayService;
    private readonly IQueue _iqueue;
    public ConfirmAPIInvocable(LINEPayService LINEPayService, IQueue iqueue)
    {
        _LINEPayService = LINEPayService;
        _iqueue = iqueue;
    }
    public async Task Invoke()
    {
        try
        {
            var response = await _LINEPayService.ConfirmAPI(Payload.Data.OrderId, Payload.TransactionId, new ConfirmAPIRequestBody
            {
                Amount = Payload.Data.Amount,
                Currency = Payload.Data.Currency,
            });

            if (response.Result != null)
            {
                switch (response.Result.ReturnCode)
                {
                    case "0000":
                        //成功
                        Log.Information(Payload.TransactionId + " ConfirmAPI成功");
                        if (Payload.Data.Options.Payment.Capture)
                        {
                            Log.Information(Payload.TransactionId + " 呼叫Confirm API，統一進行授權/請款處理");

                            //Test Void
                            //_iqueue.QueueInvocableWithPayload<VoidAPIInvocable, VoidAPIInvocablePara>(new VoidAPIInvocablePara
                            //{
                            //    TransactionId = Payload.TransactionId,
                            //    Data = Payload.Data,
                            //});

                            //Test Refund
                            //_iqueue.QueueInvocableWithPayload<RefundAPIInvocable, RefundAPIInvocablePara>(new RefundAPIInvocablePara
                            //{
                            //    TransactionId = Payload.TransactionId,
                            //    Data = Payload.Data,
                            //    RequestBody = new RefundAPIRequestBody
                            //    {
                            //        RefundAmount = 15,
                            //    }
                            //});
                        }
                        else
                        {
                            Log.Information(Payload.TransactionId + " 呼叫Confirm API只能完成授權，需要呼叫Capture API完成請款");
                            _iqueue.QueueInvocableWithPayload<CaptureAPIInvocable, CaptureAPIInvocablePara>(new CaptureAPIInvocablePara
                            {
                                TransactionId = Payload.TransactionId,
                                Data = Payload.Data,
                            });
                        }
                        break;

                    default:
                        //有問題, 之後再處理
                        Log.Information(Payload.TransactionId + " ConfirmAPI例外情況");
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
