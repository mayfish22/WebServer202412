using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using Coravel.Queuing.Interfaces;
using WebServer.Models.WebServerDB;
using WebServer.Models.LINEPayModels;

namespace WebServer.Services;

/// <summary>
/// LINE Pay服務
/// </summary>
public class LINEPayService
{
    private readonly WebServerDBContext _webServerDB;
    private readonly IHttpClientFactory _httpClient;
    private readonly IQueue _iqueue;

    private readonly string BaseApiUrl;
    private readonly string ChannelId;
    private readonly string ChannelSecretKey;

    public LINEPayService( WebServerDBContext webServerDB
        , IConfiguration configuration
        , IHttpClientFactory httpClient
        , IQueue iqueue)
    {
        _webServerDB = webServerDB;
        _httpClient = httpClient;
        _iqueue = iqueue;

        BaseApiUrl = configuration.GetValue<string>("LINE:LINEPay:BaseApiUrl");
        ChannelId = configuration.GetValue<string>("LINE:LINEPay:ChannelId");
        ChannelSecretKey = configuration.GetValue<string>("LINE:LINEPay:ChannelSecretKey");
    }
    /// <summary>
    /// HMAC Base64 簽章
    /// </summary>
    /// <param name="requestUri"></param>
    /// <param name="nonce"></param>
    /// <param name="requestBodyJson"></param>
    /// <returns></returns>
    private string Encrypt(string requestUri, string nonce, string? requestBodyJson = null)
    {
        var data = ChannelSecretKey + requestUri + (requestBodyJson ?? string.Empty) + nonce;
        byte[] keyBytes = Encoding.UTF8.GetBytes(ChannelSecretKey);
        using HMACSHA256 hmac = new HMACSHA256(keyBytes);
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 本API向LINE Pay請求付款資訊。由此，可以設定使用者的交易資訊與付款方式。請求成功，將生成LINE Pay交易序號，可以進行付款與退款。
    /// </summary>
    /// <param name="requestBody"></param>
    /// <returns></returns>
    public async Task<LINEPayResponse<RequestAPIResult>> RequestAPI(RequestAPIRequestBody requestBody)
    {
        var requestUri = @"/v3/payments/request";
        var url = (new Uri(new Uri(BaseApiUrl), requestUri)).ToString();

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            //設定這樣就會顯示正常的中文
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        string nonce = Guid.NewGuid().ToString();

        string signature = Encrypt(requestUri, nonce, json);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-LINE-ChannelId", ChannelId);//金流整合資訊 - Channel ID
        request.Headers.Add("X-LINE-Authorization-Nonce", nonce);//UUID or timestamp(時間戳)
        request.Headers.Add("X-LINE-Authorization", signature);//HMAC Base64 簽章
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClient.CreateClient();
        client.Timeout = new TimeSpan(0, 2, 0);

        var response = await client.SendAsync(request);
        var responseValue = string.Empty;
        await response.Content.ReadAsStreamAsync().ContinueWith(t =>
        {
            var stream = t.Result;
            using var reader = new StreamReader(stream);
            responseValue = reader.ReadToEnd();
        });

        var data = new LINEPayResponse<RequestAPIResult>
        {
            StatusCode = response.StatusCode,
            Content = responseValue,
        };

        long? transactionId = null;
        var jsonDocument = JsonDocument.Parse(responseValue);
        if (jsonDocument.RootElement.TryGetProperty("info", out JsonElement value))
        {
            if (value.TryGetProperty("transactionId", out JsonElement value2))
                transactionId = value2.GetInt64();
        }

        await _webServerDB.LINEPayRequest.AddAsync(new LINEPayRequest
        {
            ID = Guid.NewGuid(),
            OrderNo = requestBody.OrderId,
            TransactionId = transactionId,
            RequestBody = json,
            ResponseBody = responseValue,
            CreatedDT = DateTime.Now,
        });
        await _webServerDB.SaveChangesAsync();

        return data;
    }

    /// <summary>
    /// 本API向LINE Pay請求付款資訊。由此，可以設定使用者的交易資訊與付款方式。請求成功，將生成LINE Pay交易序號，可以進行付款與退款。
    /// </summary>
    /// <param name="transactionId"></param>
    /// <returns></returns>
    public async Task<LINEPayResponse<CheckPaymentStatusAPIResultResult>> CheckPaymentStatusAPI(long transactionId)
    {
        var requestUri = $"/v3/payments/requests/{transactionId}/check";
        var url = (new Uri(new Uri(BaseApiUrl), requestUri)).ToString();

        string nonce = Guid.NewGuid().ToString();

        string signature = Encrypt(requestUri, nonce);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-LINE-ChannelId", ChannelId);//金流整合資訊 - Channel ID
        request.Headers.Add("X-LINE-Authorization-Nonce", nonce);//UUID or timestamp(時間戳)
        request.Headers.Add("X-LINE-Authorization", signature);//HMAC Base64 簽章

        var client = _httpClient.CreateClient();
        client.Timeout = new TimeSpan(0, 2, 0);

        var response = await client.SendAsync(request);
        var responseValue = string.Empty;
        await response.Content.ReadAsStreamAsync().ContinueWith(t =>
        {
            var stream = t.Result;
            using var reader = new StreamReader(stream);
            responseValue = reader.ReadToEnd();
        });

        var data = new LINEPayResponse<CheckPaymentStatusAPIResultResult>
        {
            StatusCode = response.StatusCode,
            Content = responseValue,
        };

        return data;
    }

    /// <summary>
    /// 本API向LINE Pay請求授權。商家可以對已付款的交易進行授權。授權成功後，消費者的信用卡將被扣款。
    /// </summary>
    /// <param name="orderNo"></param>
    /// <param name="transactionId"></param>
    /// <param name="requestBody"></param>
    /// <returns></returns>
    public async Task<LINEPayResponse<ConfirmAPIResult>> ConfirmAPI(string orderNo, long transactionId, ConfirmAPIRequestBody requestBody)
    {
        var requestUri = $"/v3/payments/{transactionId}/confirm";
        var url = (new Uri(new Uri(BaseApiUrl), requestUri)).ToString();

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            //設定這樣就會顯示正常的中文
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        string nonce = Guid.NewGuid().ToString();

        string signature = Encrypt(requestUri, nonce, json);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-LINE-ChannelId", ChannelId);//金流整合資訊 - Channel ID
        request.Headers.Add("X-LINE-Authorization-Nonce", nonce);//UUID or timestamp(時間戳)
        request.Headers.Add("X-LINE-Authorization", signature);//HMAC Base64 簽章
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClient.CreateClient();
        client.Timeout = new TimeSpan(0, 2, 0);

        var response = await client.SendAsync(request);
        var responseValue = string.Empty;
        await response.Content.ReadAsStreamAsync().ContinueWith(t =>
        {
            var stream = t.Result;
            using var reader = new StreamReader(stream);
            responseValue = reader.ReadToEnd();
        });

        var data = new LINEPayResponse<ConfirmAPIResult>
        {
            StatusCode = response.StatusCode,
            Content = responseValue,
        };

        await _webServerDB.LINEPayConfirm.AddAsync(new LINEPayConfirm
        {
            ID = Guid.NewGuid(),
            OrderNo = orderNo,
            TransactionId = transactionId,
            RequestBody = json,
            ResponseBody = responseValue,
            CreatedDT = DateTime.Now,
        });
        await _webServerDB.SaveChangesAsync();

        return data;
    }

    /// <summary>
    /// 本API向LINE Pay請求扣款。商家可以對已授權的交易進行扣款。扣款成功後，消費者的信用卡將被扣款。
    /// </summary>
    /// <param name="orderNo"></param>
    /// <param name="transactionId"></param>
    /// <param name="requestBody"></param>
    /// <returns></returns>
    public async Task<LINEPayResponse<CaptureAPIResult>> CaptureAPI(string orderNo, long transactionId, CaptureAPIRequestBody requestBody)
    {
        var requestUri = $"/v3/payments/authorizations/{transactionId}/capture";
        var url = (new Uri(new Uri(BaseApiUrl), requestUri)).ToString();

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            //設定這樣就會顯示正常的中文
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        string nonce = Guid.NewGuid().ToString();

        string signature = Encrypt(requestUri, nonce, json);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-LINE-ChannelId", ChannelId);//金流整合資訊 - Channel ID
        request.Headers.Add("X-LINE-Authorization-Nonce", nonce);//UUID or timestamp(時間戳)
        request.Headers.Add("X-LINE-Authorization", signature);//HMAC Base64 簽章
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClient.CreateClient();
        client.Timeout = new TimeSpan(0, 2, 0);

        var response = await client.SendAsync(request);
        var responseValue = string.Empty;
        await response.Content.ReadAsStreamAsync().ContinueWith(t =>
        {
            var stream = t.Result;
            using var reader = new StreamReader(stream);
            responseValue = reader.ReadToEnd();
        });

        var data = new LINEPayResponse<CaptureAPIResult>
        {
            StatusCode = response.StatusCode,
            Content = responseValue,
        };

        await _webServerDB.LINEPayCapture.AddAsync(new LINEPayCapture
        {
            ID = Guid.NewGuid(),
            OrderNo = orderNo,
            TransactionId = transactionId,
            RequestBody = json,
            ResponseBody = responseValue,
            CreatedDT = DateTime.Now,
        });
        await _webServerDB.SaveChangesAsync();

        return data;
    }

    /// <summary>
    /// 本API向LINE Pay請求取消授權。商家可以對已付款的交易進行取消授權。取消授權成功後，消費者的信用卡將不會被扣款。
    /// </summary>
    /// <param name="orderNo"></param>
    /// <param name="transactionId"></param>
    /// <returns></returns>
    public async Task<LINEPayResponse<VoidAPIResult>> VoidAPI(string orderNo, long transactionId)
    {
        var requestUri = $"/v3/payments/authorizations/{transactionId}/void";
        var url = (new Uri(new Uri(BaseApiUrl), requestUri)).ToString();

        string nonce = Guid.NewGuid().ToString();

        string signature = Encrypt(requestUri, nonce);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-LINE-ChannelId", ChannelId);//金流整合資訊 - Channel ID
        request.Headers.Add("X-LINE-Authorization-Nonce", nonce);//UUID or timestamp(時間戳)
        request.Headers.Add("X-LINE-Authorization", signature);//HMAC Base64 簽章

        var client = _httpClient.CreateClient();
        client.Timeout = new TimeSpan(0, 2, 0);

        var response = await client.SendAsync(request);
        var responseValue = string.Empty;
        await response.Content.ReadAsStreamAsync().ContinueWith(t =>
        {
            var stream = t.Result;
            using var reader = new StreamReader(stream);
            responseValue = reader.ReadToEnd();
        });

        var data = new LINEPayResponse<VoidAPIResult>
        {
            StatusCode = response.StatusCode,
            Content = responseValue,
        };

        await _webServerDB.LINEPayVoid.AddAsync(new LINEPayVoid
        {
            ID = Guid.NewGuid(),
            OrderNo = orderNo,
            TransactionId = transactionId,
            ResponseBody = responseValue,
            CreatedDT = DateTime.Now,
        });
        await _webServerDB.SaveChangesAsync();

        return data;
    }

    /// <summary>
    /// 本API向LINE Pay請求退款。商家可以對已付款的交易進行退款。退款成功後，消費者的信用卡將被退款。
    /// </summary>
    /// <param name="orderNo"></param>
    /// <param name="transactionId"></param>
    /// <param name="requestBody"></param>
    /// <returns></returns>
    public async Task<LINEPayResponse<RefundAPIResult>> RefundAPI(string orderNo, long transactionId, RefundAPIRequestBody requestBody)
    {
        var requestUri = $"/v3/payments/{transactionId}/refund";
        var url = (new Uri(new Uri(BaseApiUrl), requestUri)).ToString();

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            //設定這樣就會顯示正常的中文
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        string nonce = Guid.NewGuid().ToString();

        string signature = Encrypt(requestUri, nonce, json);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-LINE-ChannelId", ChannelId);//金流整合資訊 - Channel ID
        request.Headers.Add("X-LINE-Authorization-Nonce", nonce);//UUID or timestamp(時間戳)
        request.Headers.Add("X-LINE-Authorization", signature);//HMAC Base64 簽章
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClient.CreateClient();
        client.Timeout = new TimeSpan(0, 2, 0);

        var response = await client.SendAsync(request);
        var responseValue = string.Empty;
        await response.Content.ReadAsStreamAsync().ContinueWith(t =>
        {
            var stream = t.Result;
            using var reader = new StreamReader(stream);
            responseValue = reader.ReadToEnd();
        });

        var data = new LINEPayResponse<RefundAPIResult>
        {
            StatusCode = response.StatusCode,
            Content = responseValue,
        };

        await _webServerDB.LINEPayRefund.AddAsync(new LINEPayRefund
        {
            ID = Guid.NewGuid(),
            OrderNo = orderNo,
            TransactionId = transactionId,
            RequestBody = json,
            ResponseBody = responseValue,
            CreatedDT = DateTime.Now,
        });
        await _webServerDB.SaveChangesAsync();

        return data;
    }
}