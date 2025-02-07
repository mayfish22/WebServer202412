using System.Net;
using System.Text.Json;

namespace WebServer.Models.LINEPayModels;

/// <summary>
/// NET8 才有 JsonUnmappedMemberHandling, 所以有些要另外處理
/// </summary>
/// <typeparam name="T"></typeparam>
public class LINEPayResponse<T>
{
    public HttpStatusCode StatusCode { get; set; }
    public string Content { get; set; }
    public T Result
    {
        get
        {
            if (StatusCode >= HttpStatusCode.OK && StatusCode <= (HttpStatusCode)299)
            {
                var typeName = typeof(T).Name;
                if (typeName == typeof(CheckPaymentStatusAPIResultResult).Name)
                {
                    var jsonDocument = JsonDocument.Parse(Content);
                    if (!jsonDocument.RootElement.TryGetProperty("info", out JsonElement value))
                    {
                        return (T)Convert.ChangeType(new CheckPaymentStatusAPIResultResult
                        {
                            ReturnCode = jsonDocument.RootElement.GetProperty("returnCode").GetString(),
                            ReturnMessage = jsonDocument.RootElement.GetProperty("returnMessage").GetString(),
                        }, typeof(T));
                    }
                    else
                    {
                        return JsonSerializer.Deserialize<T>(Content);
                    }
                }
                else
                {
                    //https://learn.microsoft.com/zh-tw/dotnet/standard/serialization/system-text-json/missing-members
                    return JsonSerializer.Deserialize<T>(Content);
                }
            }
            else
                return default(T);
        }
    }
}