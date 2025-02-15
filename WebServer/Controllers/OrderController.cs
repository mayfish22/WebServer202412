using CsvHelper;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.Json;
using WebServer.Models.CustomModels;
using WebServer.Models.ViewModels;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer.Controllers;

// 使用 [Authorize] 特性來限制訪問此控制器的用戶必須經過授權
[Authorize]
// 使用 [Route] 特性來定義控制器的路由規則
[Route("{controller}/{action=Index}")]
public class OrderController : Controller
{
    private readonly WebServerDBContext _webServerDB;
    private readonly LINEAPIService _lineAPIService;

    public OrderController(WebServerDBContext webServerDB, LINEAPIService lineAPIService)
    {
        _webServerDB = webServerDB;
        _lineAPIService = lineAPIService;
    }
    // ⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇ 在這下面加上 Index、Create、Detail、Edit、Delete ⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇

    #region Index
    // GET: /Order/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Task.Yield();  // 讓出控制權，允許其他任務執行
        return View("~/Views/Order/Index.cshtml");  // 返回指定的視圖
    }

    // POST: /Order/GetData
    [HttpPost]
    public async Task<IActionResult> GetData(int draw, int start, int length)
    {
        try
        {
            // 從資料庫中查詢 Order 表
            var query = from n1 in _webServerDB.Order
                        select n1;

            // 獲取總記錄數
            var recordsTotal = await query.CountAsync();

            #region 關鍵字搜尋
            // 檢查是否有搜尋關鍵字
            if (!string.IsNullOrEmpty((string)Request.Form["search[value]"]))
            {
                // 取得搜尋關鍵字並轉為大寫
                string sQuery = Request.Form["search[value]"].ToString().ToUpper();

                // 根據搜尋關鍵字過濾查詢
                query = query.Where(t => t.OrderNo.ToUpper().Contains(sQuery)
                            || t.PaymentStatus.ToUpper().Contains(sQuery));
            }
            #endregion

            #region 排序
            // 獲取排序的列索引和方向
            int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
            string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
            string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

            // 根據排序方向和列進行排序
            bool bDescending = sortDirection.Equals("DESC");
            switch (sortColumn)
            {
                case nameof(Order.OrderDate):
                    query = bDescending ? query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderSeq) 
                        : query.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderSeq);
                    break;
                case nameof(Order.OrderSeq):
                    query = bDescending ? query.OrderByDescending(o => o.OrderSeq) : query.OrderBy(o => o.OrderSeq);
                    break;
                case nameof(Order.OrderNo):
                    query = bDescending ? query.OrderByDescending(o => o.OrderNo) : query.OrderBy(o => o.OrderNo);
                    break;
                case nameof(Order.TotalAmount):
                    query = bDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount);
                    break;
                case nameof(Order.PaymentStatus):
                    query = bDescending ? query.OrderByDescending(o => o.PaymentStatus) : query.OrderBy(o => o.PaymentStatus);
                    break;
                case nameof(Order.Remark):
                    query = bDescending ? query.OrderByDescending(o => o.Remark) : query.OrderBy(o => o.Remark);
                    break;
                case nameof(Order.CreatedDT):
                    query = bDescending ? query.OrderByDescending(o => o.CreatedDT) : query.OrderBy(o => o.CreatedDT);
                    break;
                case nameof(Order.ModifiedDT):
                    query = bDescending ? query.OrderByDescending(o => o.ModifiedDT) : query.OrderBy(o => o.ModifiedDT);
                    break;
                default:
                    query = query.OrderByDescending(o => o.OrderNo);  // 默認排序
                    break;
            }
            #endregion 排序

            // 獲取過濾後的記錄數
            var recordsFiltered = await query.CountAsync();

            // 根據分頁參數獲取當前頁的數據
            var list = recordsFiltered == 0
                ? new List<Order>() // 如果沒有過濾後的記錄，返回空列表
                : query.Skip(start).Take(Math.Min(length, recordsFiltered - start)).ToList(); // 分頁查詢

            // 構建返回給 DataTable 的數據對象
            dynamic dataTableData = new
            {
                draw = draw, // DataTable 的 draw 參數
                data = list, // 當前頁的數據
                recordsTotal = recordsTotal, // 總記錄數
                recordsFiltered = recordsFiltered, // 過濾後的記錄數
            };

            // 返回 JSON 格式的數據
            return Json(dataTableData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // 保持屬性名稱不變
            });
        }
        catch (Exception e)
        {
            // 記錄錯誤信息
            Log.Error(e, $"{nameof(OrderController)}.{nameof(GetData)}");

            // 構建返回的錯誤數據對象
            dynamic dataTableData = new
            {
                draw = draw, // DataTable 的 draw 參數
                data = Array.Empty<string>(), // 返回空數據
                recordsTotal = 0, // 總記錄數為 0
                recordsFiltered = 0, // 過濾後的記錄數為 0
                errorMessage = e.Message, // 錯誤信息
            };

            // 返回 JSON 格式的錯誤數據
            return Json(dataTableData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // 保持屬性名稱不變
            });
        }
    }
    #endregion

    #region GetOrderViewModelAsync
    /// <summary>
    /// 獲取訂單視圖模型的方法。
    /// </summary>
    /// <param name="id">訂單的唯一標識符，可能為空。</param>
    /// <param name="isReadonly">指示訂單是否為只讀的布林值。</param>
    /// <returns>返回一個包含訂單及其詳細信息的訂單視圖模型。</returns>
    private async Task<OrderViewModel> GetOrderViewModelAsync(Guid? id, bool isReadonly)
    {
        // 檢查是否提供了訂單ID
        if (id.HasValue)
        {
            // 根據ID查找訂單
            var order = await _webServerDB.Order.FindAsync(id);

            // 如果未找到訂單，則拋出異常
            if (order == null)
                throw new Exception("查無訂單");

            // 獲取與訂單相關的詳細信息
            var orderDetails = await (from n1 in _webServerDB.OrderDetail
                                      join n2 in _webServerDB.Product on n1.ProductID equals n2.ID
                                      where n1.OrderID == id
                                      orderby n1.Seq
                                      select new OrderDetail
                                      {
                                          ID = n1.ID, // 訂單詳細信息的唯一標識符
                                          OrderID = n1.OrderID, // 關聯的訂單ID
                                          Seq = n1.Seq, // 詳細信息的序列號
                                          ProductID = n1.ProductID, // 產品ID
                                          ProductCode = n2.ProductCode, // 產品代碼
                                          ProductName = n2.ProductName, // 產品名稱
                                          Quantity = n1.Quantity, // 訂購數量
                                          UnitPrice = n1.UnitPrice, // 單價
                                          CreatedDT = n1.CreatedDT, // 創建日期時間
                                          ModifiedDT = n1.ModifiedDT, // 修改日期時間
                                      }).ToListAsync(); // 將結果轉換為列表

            // 創建訂單視圖模型
            var model = new OrderViewModel
            {
                IsReadonly = isReadonly, // 設置只讀屬性
                Order = order, // 設置訂單
                OrderDetails = orderDetails == null ? new List<OrderDetail>() : orderDetails, // 設置訂單詳細信息
            };

            return model; // 返回模型
        }
        else
        {
            // 如果未提供訂單ID，則創建一個新的訂單視圖模型
            var model = new OrderViewModel
            {
                IsReadonly = isReadonly, // 設置只讀屬性
                Order = new Order
                {
                    ID = Guid.NewGuid(), // 為新訂單生成唯一標識符
                },
                OrderDetails = new List<OrderDetail>(), // 初始化訂單詳細信息列表
            };

            return model; // 返回模型
        }
    }

    #endregion

    #region Detail
    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 true
        var model = await GetOrderViewModelAsync(id, true);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/Order/Default.cshtml", model);
    }
    #endregion

    #region ExportCSV
    /// <summary>
    /// 異步導出訂單數據為 CSV 檔案的方法。
    /// </summary>
    /// <returns>返回 CSV 檔案作為下載，或在發生錯誤時返回 BadRequest。</returns>
    [HttpGet]
    public async Task<IActionResult> ExportCSV()
    {
        try
        {
            // 查詢訂單數據，並按訂單日期和序列號排序
            var query = from n1 in _webServerDB.Order
                        orderby n1.OrderDate, n1.OrderSeq
                        select new OrderCsvModel
                        {
                            OrderDate = n1.OrderDate.ToString("yyyy-MM-dd"), // 格式化訂單日期
                            OrderSeq = n1.OrderSeq.ToString(), // 將訂單序列號轉換為字串
                            OrderNo = n1.OrderNo, // 訂單號
                            TotalAmount = n1.TotalAmount.ToString("N0"), // 格式化總金額為千位分隔字串
                            PaymentStatus = n1.PaymentStatus, // 付款狀態
                            Remark = n1.Remark, // 備註
                        };

            // 執行查詢並將結果轉換為列表
            var orders = await query.ToListAsync();

            // 初始化一個空的 byte 陣列以存儲 CSV 檔案的內容
            byte[] fileStream = Array.Empty<byte>();

            // 使用 MemoryStream 來寫入 CSV 檔案
            using var memoryStream = new MemoryStream();

            // 設定編碼為 Big5（繁體中文編碼）
            using var streamWriter = new StreamWriter(memoryStream, Encoding.GetEncoding(950));
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            // 寫入訂單數據到 CSV 檔案
            csvWriter.WriteRecords(orders);

            // 將 MemoryStream 的內容轉換為 byte 陣列
            fileStream = memoryStream.ToArray();

            // 返回 CSV 檔案作為下載
            return new FileStreamResult(new MemoryStream(fileStream), "application/octet-stream")
            {
                FileDownloadName = $"訂單資料.csv", // 設定下載檔案的名稱
            };
        }
        catch (Exception ex)
        {
            // 如果發生錯誤，返回 BadRequest 並顯示錯誤訊息
            return BadRequest(ex.Message);
        }
    }
    #endregion

    #region ExportPDF
    /// <summary>
    /// 匯出訂單PDF
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> ExportPDF()
    {
        try
        {
            var query = from n1 in _webServerDB.Order
                        orderby n1.OrderDate, n1.OrderSeq
                        select n1;

            // 將查詢結果轉換為陣列
            var orders = await query.ToArrayAsync();

            #region 產生PDF
            // 建立中文字型（msjh.ttf 微軟正黑體）
            BaseFont bfChinese = BaseFont.CreateFont(Path.Combine("wwwroot", "fonts", "msjh.ttf"), BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

            // 建立 PDF 檔案的記憶體流
            MemoryStream pdfFileStream = new MemoryStream();

            // 設定紙張大小為 A4 直印
            iTextSharp.text.Document doc1 = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);

            // 計算每公分的寬度和高度
            float wcm = (float)Math.Round(doc1.PageSize.Width / (float)21.00 * 1, 3);
            float hcm = (float)Math.Round(doc1.PageSize.Height / (float)29.70 * 1, 3);
            float shiftX = 1 * wcm;

            // 初始化 PdfWriter 並打開文件
            iTextSharp.text.pdf.PdfWriter pdfWriter = iTextSharp.text.pdf.PdfWriter.GetInstance(doc1, pdfFileStream);
            doc1.Open();
            iTextSharp.text.pdf.PdfContentByte cb = pdfWriter.DirectContent;

            int pageRecords = 10; // 每頁顯示10筆資料
            int pages = (orders.Length / pageRecords) + 1;

            // 計算字的高度
            float bodyFontSize = 14;
            var bodyAscentPoint = bfChinese.GetAscentPoint("計算高度用", bodyFontSize);
            var bodyDescentPoint = bfChinese.GetDescentPoint("計算高度用", bodyFontSize);
            var bodyFontHeight = bodyAscentPoint - bodyDescentPoint + 0.05f * hcm;

            // 生成每頁的內容
            for (int i = 0; i < pages; i++)
            {
                // 記錄目前的Y軸位置
                float currentBodyY = doc1.PageSize.Height;
                if (i > 0)
                    doc1.NewPage();

                #region Header
                // 添加頁首
                cb.BeginText();
                cb.SetFontAndSize(bfChinese, 16);
                cb.SetColorFill(new BaseColor(Color.Black));
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER, $"訂單資料", doc1.PageSize.Width / 2, doc1.PageSize.Height - 1f * hcm, 0);
                cb.EndText();
                #endregion

                currentBodyY = currentBodyY - 2f * hcm;

                #region Body
                // 畫表格的橫線
                cb.SetLineWidth(0.1f);
                cb.SetColorStroke(new BaseColor(Color.Black));
                cb.MoveTo(shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                cb.Stroke();

                // 添加表格標題
                cb.BeginText();
                cb.SetFontAndSize(bfChinese, bodyFontSize);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"#", shiftX, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"訂單日期", shiftX + 1f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"訂單序號", shiftX + 5f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"訂單編號", shiftX + 9f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_RIGHT, $"金額", shiftX + 16.5f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"付款狀態", shiftX + 17f * wcm, currentBodyY, 0);
                cb.EndText();

                // 畫表格的橫線
                cb.SetLineWidth(0.1f);
                cb.SetColorStroke(new BaseColor(Color.Black));
                cb.MoveTo(shiftX, currentBodyY - 0.1f * hcm);
                cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY - 0.1f * hcm);
                cb.Stroke();

                currentBodyY -= (bodyFontHeight + 0.1f * hcm);

                // 添加每筆訂單資料
                cb.BeginText();
                for (int j = 0; j < pageRecords && (i * pageRecords + j) < orders.Length; j++)
                {
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{(i * pageRecords + j + 1)}", shiftX, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{orders[i * pageRecords + j].OrderDate.ToString("yyyy-MM-dd")}", shiftX + 1f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{orders[i * pageRecords + j].OrderSeq}", shiftX + 5f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{orders[i * pageRecords + j].OrderNo}", shiftX + 9f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_RIGHT, $"{orders[i * pageRecords + j].TotalAmount.ToString("N0")}", shiftX + 16.5f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{orders[i * pageRecords + j].PaymentStatus}", shiftX + 17f * wcm, currentBodyY, 0);

                    currentBodyY -= bodyFontHeight;
                }
                cb.EndText();
                #endregion

                #region Footer
                // 添加頁尾
                float footerFontSize = 8;
                cb.SetColorFill(new BaseColor(Color.Black));
                cb.SetFontAndSize(bfChinese, footerFontSize);
                float footerY = currentBodyY - 0.1f * hcm;
                var sCurrentDT = $" 列印日期/時間：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 第{i + 1}頁/共{pages}頁 ----";
                // 計算字的寬度
                var sCurrentDT_width = bfChinese.GetWidthPoint(sCurrentDT, footerFontSize);
                var dash_width = bfChinese.GetWidthPoint("-", footerFontSize);
                var dashCount = Convert.ToInt32((doc1.PageSize.Width - 2 * shiftX - sCurrentDT_width) / dash_width);
                cb.BeginText();
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_RIGHT, new String('-', dashCount) + sCurrentDT, doc1.PageSize.Width - shiftX, footerY, 0);
                cb.EndText();
                #endregion
            }
            doc1.Close();
            #endregion

            var result = Array.Empty<byte>();
            // 加密PDF檔案
            using (MemoryStream output = new MemoryStream())
            {
                PdfReader reader = new PdfReader(pdfFileStream.ToArray());
                // 設定密碼 123456
                PdfEncryptor.Encrypt(reader, output, PdfWriter.ENCRYPTION_AES_128, "123456", null, PdfWriter.AllowPrinting);
                result = output.ToArray();
            }
            // 返回PDF檔案給用戶下載
            return new FileStreamResult(new MemoryStream(result), "application/pdf")
            {
                FileDownloadName = $"訂單資料.pdf",
            };
        }
        catch (Exception ex)
        {
            // 如果發生錯誤，返回 BadRequest 並顯示錯誤訊息
            return BadRequest(ex.Message);
        }
    }
    #endregion

    // ⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆ 在這上面加上 Index、Create、Detail、Edit、Delete ⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆
}