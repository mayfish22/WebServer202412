using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models;
using WebServer.Services;

namespace WebServer.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LINEAPIService _lineAPIService;

    public HomeController(ILogger<HomeController> logger, LINEAPIService lineAPIService)
    {
        _logger = logger;
        _lineAPIService = lineAPIService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #region SendMessage
    public class SendMessagePara
    {
        public string? UserID { get; set; }
        public string? Message { get; set; }
    }
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessagePara info)
    {
        await _lineAPIService.SendMessage(info.UserID, new object[]
            {
                new {
                    type = "text",
                    text = info.Message,
                    notificationDisabled = false,
                }
            });
        return Ok();
    }
    #endregion
}
