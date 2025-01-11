using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Serilog;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using WebServer.Components;
using WebServer.Helpers;
using WebServer.Models;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers;

[Authorize]
[Route("{controller}/{action=Index}")]
public class StreamingController : Controller
{
    private readonly ILogger<StreamingController> _logger;
    private static readonly FormOptions _defaultFormOptions = new FormOptions();
    private string[] _permittedExtensions = new string[] { ".jpg", ".png" }; //允許的檔案類型
    private long _fileSizeLimit = 50 * 1024 * 1024; // 50MB, 檔案大小限制
    private string _targetFilePath; // 儲存路徑
    private readonly WebServerDBContext _webServerDB;
    private readonly IHttpContextAccessor _httpContext;

    public StreamingController(ILogger<StreamingController> logger, WebServerDBContext webServerDB, IHttpContextAccessor httpContext)
    {
        _logger = logger;
        _webServerDB = webServerDB;
        _httpContext = httpContext;
        //檔案儲存路徑
        _targetFilePath = Path.GetTempPath();

        //記錄檔案儲存路徑
        _logger.LogInformation("FilePath: " + _targetFilePath);
    }

    /// <summary>
    /// 上傳檔案的 API
    /// </summary>
    /// <returns>上傳結果</returns>
    [HttpPost]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> Upload()
    {
        try
        {
            // 記錄本次上傳的檔案 ID
            var ids = new List<Guid>();

            // 驗證是否為 Multipart Content-Type
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
                return BadRequest(ModelState);
            }

            #region 取得使用者資訊
            Guid? userId = null;
            var httpContext = _httpContext.HttpContext;

            if (httpContext != null)
            {
                var user = httpContext.User;

                if (user.Identity.IsAuthenticated)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid tmp))
                        userId = tmp;
                }
            }
            #endregion

            var formAccumulator = new KeyValueAccumulator();
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            var streamedFileContent = Array.Empty<byte>();

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    // 處理檔案上傳部分
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value);

                        streamedFileContent = await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                            ModelState, _permittedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        var fileId = Guid.NewGuid();
                        var fileName = trustedFileNameForDisplay;
                        var filePath = Path.Combine(_targetFilePath, fileId.ToString());

                        // 儲存檔案至指定路徑
                        using (var targetStream = System.IO.File.Create(filePath))
                        {
                            await targetStream.WriteAsync(streamedFileContent);
                        }

                        // 將檔案資訊寫入資料庫
                        await _webServerDB.FileStorage.AddAsync(new WebServer.Models.WebServerDB.FileStorage
                        {
                            ID = fileId,
                            Type = nameof(Upload),
                            Name = fileName,
                            Size = streamedFileContent.Length,
                            Path = filePath,
                            CreatedUserID = userId,
                            CreatedDT = DateTime.Now,
                        });
                        await _webServerDB.SaveChangesAsync();
                        ids.Add(fileId);
                    }
                    // 處理表單資料部分
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;
                        var encoding = GetEncoding(section);

                        if (encoding == null)
                        {
                            ModelState.AddModelError("File", "The request couldn't be processed (Error 2).");
                            return BadRequest(ModelState);
                        }

                        using (var streamReader = new StreamReader(section.Body, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                        {
                            var value = await streamReader.ReadToEndAsync();
                            if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }
                            formAccumulator.Append(key, value);

                            if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            {
                                ModelState.AddModelError("File", "The request couldn't be processed (Error 3).");
                                return BadRequest(ModelState);
                            }
                        }
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }

            // 綁定表單資料至模型
            var formData = new FormData();
            var formValueProvider = new FormValueProvider(BindingSource.Form, new FormCollection(formAccumulator.GetResults()), CultureInfo.CurrentCulture);
            var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "", valueProvider: formValueProvider);

            //if (!bindingSuccessful)
            //{
            //    ModelState.AddModelError("File", "The request couldn't be processed (Error 5).");
            //    return BadRequest(ModelState);
            //}

            return Json(new { message = formData.Message, ids = ids });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// 下載檔案的 API
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous] // 允許未經身份驗證的用戶訪問此方法
    [HttpGet("{id}")] // 指定此方法為HTTP GET請求
    public async Task<IActionResult> Download(Guid id) // 定義下載檔案的方法，接受檔案ID作為參數
    {
        try
        {
            // 根據檔案ID從資料庫中查找檔案
            var file = await _webServerDB.FileStorage.FindAsync(id);
            if (file == null) // 如果找不到檔案
                throw new Exception("找不到檔案編號"); // 拋出異常，提示檔案不存在

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider(); // 創建檔案擴展名內容類型提供者
            string contentType; // 定義內容類型變數
                                // 取得檔案 MIME Type
            if (!provider.TryGetContentType(file.Name, out contentType)) // 嘗試獲取檔案的內容類型
            {
                contentType = "application/octet-stream"; // 如果無法獲取，則設置為通用二進制流
            }

            // 讀取檔案
            using (FileStream fsSource = new FileStream(file.Path, FileMode.Open, FileAccess.Read)) // 打開檔案流以讀取檔案
            {
                // 將源檔案讀入字節數組
                byte[] bytes = new byte[fsSource.Length]; // 創建字節數組以存儲檔案內容
                int numBytesToRead = (int)fsSource.Length; // 設置要讀取的字節數
                int numBytesRead = 0; // 記錄已讀取的字節數

                while (numBytesToRead > 0) // 當還有字節需要讀取時
                {
                    // Read 可能返回從 0 到 numBytesToRead 的任何數字
                    int n = fsSource.Read(bytes, numBytesRead, numBytesToRead); // 讀取檔案內容
                                                                                // 當到達檔案結尾時中斷
                    if (n == 0)
                        break; // 如果沒有讀取到任何字節，則退出循環

                    numBytesRead += n; // 更新已讀取的字節數
                    numBytesToRead -= n; // 更新剩餘要讀取的字節數
                }
                // 返回檔案流結果，並設置下載檔案的名稱
                return new FileStreamResult(new MemoryStream(bytes), contentType)
                {
                    FileDownloadName = file.Name, // 設置下載檔案的名稱
                    LastModified = DateTimeOffset.Now,
                    EnableRangeProcessing = true,
                };
            }
        }
        catch (Exception e) // 捕獲異常
        {
            return BadRequest(e.Message); // 返回400錯誤，並顯示異常消息
        }
    }

    public class FormData
    {
        public string Message { get; set; }
    }

    public static Encoding GetEncoding(MultipartSection section)
    {
        MediaTypeHeaderValue mediaType;
        var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
        if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
        {
            return Encoding.UTF8;
        }
        return mediaType.Encoding;
    }

}
