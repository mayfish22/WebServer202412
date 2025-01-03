using Coravel.Queuing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.LINEModels;
using WebServer.Services;
using WebServer.Services.Invocables;

namespace WebServer.Controllers;

public class LINEAPIController : Controller
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly LINEAPIService _lineAPIService;
    private readonly IQueue _queue;
    public LINEAPIController(IHttpContextAccessor httpContext, LINEAPIService lineAPIService, IQueue queue)
    {
        _httpContext = httpContext;
        _lineAPIService = lineAPIService;
        _queue = queue;
    }

    /// <summary>
    /// 測試用
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("/api/TestWebHook")]
    public async Task<IActionResult> TestWebHook()
    {
        await Task.Yield();
        return Ok("this is TestWebHook!");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("/api/Webhook")]
    public async Task<IActionResult> Webhook()
    {
        try
        {
            string requestContent = string.Empty;
            using (var reader = new StreamReader(_httpContext.HttpContext!.Request.Body))
            {
                requestContent = await reader.ReadToEndAsync();
            }

            var webhook = _lineAPIService.ParseWebhook(requestContent);
            if (webhook == null)
                throw new Exception("無法解析資料");

            if(_lineAPIService.IsTest(webhook))
                return Ok(); // Webhook 測試

            _queue.QueueInvocableWithPayload<LINEWebhookInvocable, Webhook>(webhook);
        }
        catch (Exception ex)
        {
            // 後續需要再處理
            Console.WriteLine(ex.Message);
        }
        // 一律回傳Ok
        return Ok("this is TestWebHook!");
    }
}
