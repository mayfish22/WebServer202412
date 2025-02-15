using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WebServer.Models.LINEPayModels;
using WebServer.Models.ViewModels;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer.Controllers;

/// <summary>
/// 購物車控制器
/// </summary>
[Authorize]
[Route("{controller}/{action=Index}")]
public class CartItemController : Controller
{
    private readonly WebServerDBContext _webServerDB;
    private readonly SiteService _siteService;
    private readonly LINEPayService _linePayService;
    private readonly LINEAPIService _lineAPIService;
    private readonly string LIFFID;

    public CartItemController( WebServerDBContext webServerDB
        , SiteService siteService
        , LINEPayService linePayService
        , LINEAPIService lineAPIService
        , IConfiguration configuration)
    {
        _webServerDB = webServerDB;
        _siteService = siteService;
        _linePayService = linePayService;
        _lineAPIService = lineAPIService;
        LIFFID = configuration.GetValue<string>("LINE:LINELogin:LIFFID");
    }
    /// <summary>
    /// 將指定產品加入購物車
    /// </summary>
    /// <param name="productId">要加入購物車的產品ID</param>
    /// <returns>返回操作結果，包含購物車中商品的數量</returns>
    [HttpPost("{productId}")]
    public async Task<IActionResult> AddCartItem(Guid productId)
    {
        try
        {
            // 初始化一個空的購物車項目列表
            var result = new List<CartItem>();

            // 獲取當前的LINE用戶
            var currentLINEUser = await _siteService.GetCurrentLINEUser();

            // 如果用戶未登入，返回未授權的響應
            if (currentLINEUser == null)
                return Unauthorized();

            // 查詢用戶的購物車中是否已經存在該產品
            var cartItem = await (from n1 in _webServerDB.CartItem
                                  where n1.LINEUserID == currentLINEUser.ID && n1.ProductID == productId
                                  orderby n1.Seq
                                  select n1).FirstOrDefaultAsync();

            // 如果購物車中不存在該產品，則新增一個購物車項目
            if (cartItem == null)
            {
                // 初始化當前序號
                var currentSeq = 0;

                // 檢查用戶的購物車中是否有其他項目
                if (await _webServerDB.CartItem.Where(s => s.LINEUserID == currentLINEUser.ID).AnyAsync())
                    // 獲取當前最大序號
                    currentSeq = await _webServerDB.CartItem.Where(s => s.LINEUserID == currentLINEUser.ID).MaxAsync(s => s.Seq);

                // 新增新的購物車項目
                await _webServerDB.CartItem.AddAsync(new CartItem
                {
                    ID = Guid.NewGuid(), // 生成新的唯一ID
                    Seq = currentSeq + 1, // 設定序號為當前最大序號加1
                    LINEUserID = currentLINEUser.ID, // 設定用戶ID
                    ProductID = productId, // 設定產品ID
                    Quantity = 1, // 設定數量為1
                    CreatedDT = DateTime.Now // 設定創建時間為當前時間
                });
            }
            else
            {
                // 如果購物車中已經存在該產品，則數量加1
                cartItem.Quantity++;
                cartItem.ModifiedDT = DateTime.Now; // 更新修改時間
            }

            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();

            // 獲取當前用戶的所有購物車項目
            var cartItems = await _siteService.GetCartItems(currentLINEUser.ID).ToListAsync();

            // 返回JSON格式的響應，包含購物車中商品的數量
            return Json(new
            {
                count = cartItems.Count, // 購物車中商品的數量
            });
        }
        catch (Exception ex)
        {
            // 如果發生異常，返回錯誤響應
            return BadRequest(ex.Message);
        }
    }


    /// <summary>
    /// 設定購物車項目的數量
    /// </summary>
    /// <param name="productId">要設定數量的產品ID</param>
    /// <param name="quantity">要設定的數量</param>
    /// <returns>返回操作結果，包含購物車中商品的數量</returns>
    [HttpPost("{productId}/{quantity}")]
    public async Task<IActionResult> SeqCartItemQuantity(Guid productId, int quantity)
    {
        try
        {
            // 初始化一個空的購物車項目列表
            var result = new List<CartItem>();

            // 獲取當前的LINE用戶
            var currentLINEUser = await _siteService.GetCurrentLINEUser();

            // 如果用戶未登入，返回未授權的響應
            if (currentLINEUser == null)
                return Unauthorized();

            // 查詢用戶的購物車中是否已經存在該產品
            var cartItem = await (from n1 in _webServerDB.CartItem
                                  where n1.LINEUserID == currentLINEUser.ID && n1.ProductID == productId
                                  orderby n1.Seq
                                  select n1).FirstOrDefaultAsync();

            // 如果購物車中不存在該產品，則新增一個購物車項目
            if (cartItem == null)
            {
                // 獲取當前用戶購物車中最大序號
                var currentSeq = await _webServerDB.CartItem.Where(s => s.LINEUserID == currentLINEUser.ID).MaxAsync(s => s.Seq);

                // 新增新的購物車項目
                await _webServerDB.CartItem.AddAsync(new CartItem
                {
                    ID = Guid.NewGuid(), // 生成新的唯一ID
                    Seq = currentSeq + 1, // 設定序號為當前最大序號加1
                    LINEUserID = currentLINEUser.ID, // 設定用戶ID
                    ProductID = productId, // 設定產品ID
                    Quantity = quantity, // 設定數量為傳入的數量
                    CreatedDT = DateTime.Now // 設定創建時間為當前時間
                });
            }
            else
            {
                // 如果購物車中已經存在該產品，則更新數量
                cartItem.Quantity = quantity; // 設定數量為傳入的數量
                cartItem.ModifiedDT = DateTime.Now; // 更新修改時間
            }

            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();

            // 獲取當前用戶的所有購物車項目
            var cartItems = await _siteService.GetCartItems(currentLINEUser.ID).ToListAsync();

            // 返回JSON格式的響應，包含購物車中商品的數量
            return Json(new
            {
                count = cartItems.Count, // 購物車中商品的數量
            });
        }
        catch (Exception ex)
        {
            // 如果發生異常，返回錯誤響應
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 移除購物車中的指定項目
    /// </summary>
    /// <param name="cartItemId">要移除的購物車項目ID</param>
    /// <returns>返回操作結果，包含購物車中商品的數量</returns>
    [HttpPost("{cartItemId}")]
    public async Task<IActionResult> RemoveCartItem(Guid cartItemId)
    {
        try
        {
            // 初始化一個空的購物車項目列表
            var result = new List<CartItem>();

            // 獲取當前的LINE用戶
            var currentLINEUser = await _siteService.GetCurrentLINEUser();

            // 如果用戶未登入，返回未授權的響應
            if (currentLINEUser == null)
                return Unauthorized();

            // 根據傳入的購物車項目ID查找該項目
            var cartItem = await _webServerDB.CartItem.FindAsync(cartItemId);

            // 如果找到該購物車項目，則將其移除
            if (cartItem != null)
            {
                _webServerDB.CartItem.Remove(cartItem); // 從數據庫中移除該項目
                await _webServerDB.SaveChangesAsync(); // 保存更改到數據庫
            }

            // 獲取當前用戶的所有購物車項目
            var cartItems = await _siteService.GetCartItems(currentLINEUser.ID).ToListAsync();

            // 返回JSON格式的響應，包含購物車中商品的數量
            return Json(new
            {
                count = cartItems.Count, // 購物車中商品的數量
            });
        }
        catch (Exception ex)
        {
            // 如果發生異常，返回錯誤響應
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 取得當前用戶的購物車內容
    /// </summary>
    /// <returns>返回包含購物車項目的部分視圖</returns>
    [HttpPost]
    public async Task<IActionResult> GetCartItems()
    {
        // 初始化一個空的購物車項目列表
        var result = new List<CartItem>();

        // 獲取當前的LINE用戶
        var currentLINEUser = await _siteService.GetCurrentLINEUser();

        // 如果用戶未登入，返回未授權的響應
        if (currentLINEUser == null)
            return Unauthorized();

        // 查詢用戶的購物車項目，並聯接產品信息
        var cartItems = await (from n1 in _webServerDB.CartItem
                               join n2 in _webServerDB.Product on n1.ProductID equals n2.ID
                               where n1.LINEUserID == currentLINEUser.ID // 只查詢當前用戶的購物車項目
                               orderby n1.Seq // 根據序號排序
                               select new CartItem
                               {
                                   ID = n1.ID, // 購物車項目ID
                                   Seq = n1.Seq, // 購物車項目序號
                                   ProductID = n1.ProductID, // 產品ID
                                   ProductCode = n2.ProductCode, // 產品代碼
                                   ProductName = n2.ProductName, // 產品名稱
                                   MainImageURL = $"/Streaming/Download/{n2.MainImageFileID}", // 產品主圖URL
                                   Quantity = n1.Quantity, // 購物車中該產品的數量
                                   UnitPrice = n2.UnitPrice, // 產品單價
                                   CreatedDT = n1.CreatedDT, // 購物車項目創建時間
                                   ModifiedDT = n1.ModifiedDT // 購物車項目修改時間
                               }).ToListAsync(); // 將查詢結果轉換為列表

        // 返回部分視圖，顯示購物車內容
        return PartialView(@"~/Views/CartItem/ShopcartPartial.cshtml", cartItems);
    }

    /// <summary>
    /// 取得訂單預覽
    /// </summary>
    /// <returns>返回包含訂單和訂單明細的部分視圖</returns>
    [HttpPost]
    public async Task<IActionResult> GetOrderPreview()
    {
        // 初始化一個空的購物車項目列表
        var result = new List<CartItem>();

        // 獲取當前的LINE用戶
        var currentLINEUser = await _siteService.GetCurrentLINEUser();

        // 如果用戶未登入，返回未授權的響應
        if (currentLINEUser == null)
            return Unauthorized();

        // 創建一個新的訂單對象
        var order = new Order
        {
            ID = Guid.NewGuid(), // 訂單ID
            OrderDate = DateOnly.FromDateTime(DateTime.Now.Date), // 訂單日期
            OrderSeq = 0, // 訂單序號（初始為0）
            OrderNo = string.Empty, // 訂單號（初始為空）
            LINEUserID = currentLINEUser.ID, // 訂單所屬用戶ID
            CreatedDT = DateTime.Now, // 訂單創建時間
        };

        // 查詢當前用戶的購物車項目，並聯接產品信息以獲取訂單明細
        var orderDetails = await (from n1 in _webServerDB.CartItem
                                  join n2 in _webServerDB.Product on n1.ProductID equals n2.ID
                                  where n1.LINEUserID == currentLINEUser.ID // 只查詢當前用戶的購物車項目
                                  orderby n1.Seq // 根據序號排序
                                  select new OrderDetail
                                  {
                                      ID = Guid.NewGuid(), // 訂單明細ID
                                      OrderID = order.ID, // 關聯的訂單ID
                                      Seq = n1.Seq, // 購物車項目序號
                                      ProductID = n1.ProductID, // 產品ID
                                      ProductName = n2.ProductName, // 產品名稱
                                      Quantity = n1.Quantity, // 購物車中該產品的數量
                                      UnitPrice = n2.UnitPrice, // 產品單價
                                      CreatedDT = n1.CreatedDT, // 購物車項目創建時間
                                      ModifiedDT = n1.ModifiedDT // 購物車項目修改時間
                                  }).ToListAsync(); // 將查詢結果轉換為列表

        // 為每個訂單明細分配序號
        int seq = 1;
        orderDetails.ForEach((s) => {
            s.Seq = seq++; // 設置序號
        });

        // 計算訂單的總金額
        order.TotalAmount = orderDetails.Sum(s => s.Quantity * s.UnitPrice); // 總金額 = 數量 * 單價的總和

        // 返回部分視圖，顯示訂單預覽內容
        return PartialView(@"~/Views/CartItem/OrderPreviewPartial.cshtml", new OrderPreviewViewModel
        {
            Order = order, // 訂單對象
            OrderDetails = orderDetails // 訂單明細列表
        });
    }

    /// <summary>
    /// 結帳參數，包含備註與完整域名
    /// </summary>
    public class CheckoutParameter
    {
        /// <summary>
        /// 備註訊息
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 完整域名，用於產生回呼網址
        /// </summary>
        public string? FullDomain { get; set; }
    }

    /// <summary>
    /// 結帳功能，將購物車內容轉換為訂單並與 LINE Pay 整合
    /// </summary>
    /// <param name="parameter">結帳參數</param>
    /// <returns>JSON 結果，包含 LINE Pay 付款連結</returns>
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutParameter parameter)
    {
        try
        {
            // 取得當前的 LINE 使用者
            var currentLINEUser = await _siteService.GetCurrentLINEUser();
            if (currentLINEUser == null)
                return Unauthorized(); // 未登入則返回未授權

            // 設定當前日期與時間
            var currentDateTime = DateTime.Now;
            var currentDate = DateOnly.FromDateTime(DateTime.Now.Date);

            // 查詢當日最大訂單序號
            var maxOrderSeq = 0;
            if (await _webServerDB.Order.Where(s => s.OrderDate == currentDate).AnyAsync())
                maxOrderSeq = await _webServerDB.Order.Where(s => s.OrderDate == currentDate).MaxAsync(s => s.OrderSeq);

            // 建立訂單物件
            var order = new Order
            {
                ID = Guid.NewGuid(), // 訂單唯一識別碼
                OrderDate = currentDate,
                OrderSeq = maxOrderSeq + 1,
                OrderNo = $"LP{currentDateTime:yyyyMMddHHmmssfff}", // 訂單編號
                LINEUserID = currentLINEUser.ID,
                Remark = parameter.Remark,
                PaymentStatus = "處理中", // 初始狀態
                CreatedDT = currentDateTime,
            };

            // 取得購物車明細並轉換為訂單明細
            var orderDetails = await (from n1 in _webServerDB.CartItem
                                      join n2 in _webServerDB.Product on n1.ProductID equals n2.ID
                                      where n1.LINEUserID == currentLINEUser.ID
                                      orderby n1.Seq
                                      select new OrderDetail
                                      {
                                          ID = Guid.NewGuid(),
                                          OrderID = order.ID,
                                          Seq = n1.Seq,
                                          ProductID = n1.ProductID,
                                          ProductCode = n2.ProductCode,
                                          ProductName = n2.ProductName,
                                          Quantity = n1.Quantity,
                                          UnitPrice = n2.UnitPrice,
                                          CreatedDT = n1.CreatedDT,
                                          ModifiedDT = null,
                                      }).ToListAsync();

            // 設定訂單明細的序號
            int seq = 1;
            orderDetails.ForEach((s) => {
                s.Seq = seq++;
            });

            // 計算訂單總金額
            order.TotalAmount = orderDetails.Sum(s => s.Quantity * s.UnitPrice);

            // 新增訂單與訂單明細至資料庫
            await _webServerDB.Order.AddAsync(order);
            await _webServerDB.OrderDetail.AddRangeAsync(orderDetails);
            await _webServerDB.SaveChangesAsync();

            // 準備 LINE Pay 付款資料
            var packages = new List<RequestAPIRequestBody.Package>
        {
            new RequestAPIRequestBody.Package
            {
                Id = "Package01",
                Name = "PackageA",
                Products = orderDetails.Select(s => new RequestAPIRequestBody.Package.Product
                {
                    Id = s.ProductCode,
                    Name = s.ProductName,
                    Price = (int)s.UnitPrice,
                    Quantity = s.Quantity,
                    OriginalPrice = (int)s.UnitPrice,
                }).ToList(),
            }
        };

            // 建立 LINE Pay 請求資料
            var requestAPIRequestBody = new RequestAPIRequestBody
            {
                OrderId = order.OrderNo,
                Packages = packages,
                RedirectUrls = new RequestAPIRequestBody.RedirectUrlsOption
                {
                    ConfirmUrl = $"{parameter.FullDomain}/CartItem/{nameof(OrderConfirm)}/{order.ID}",
                    CancelUrl = $"{parameter.FullDomain}/CartItem/{nameof(OrderCancel)}/{order.ID}",
                },
                Options = new RequestAPIRequestBody.Option
                {
                    Payment = new RequestAPIRequestBody.Option.PaymentOption
                    {
                        Capture = true,
                    }
                }
            };

            // 呼叫 LINE Pay API
            var result = await _linePayService.RequestAPI(requestAPIRequestBody);

            // 更新訂單狀態為待付款
            order = await _webServerDB.Order.FindAsync(order.ID);
            order.PaymentStatus = "待付款";
            order.ModifiedDT = DateTime.Now;

            // 清空購物車
            var cartItems = _webServerDB.CartItem.Where(s => s.LINEUserID.Equals(currentLINEUser.ID)).Select(s => s);
            _webServerDB.CartItem.RemoveRange(cartItems);

            // 儲存變更
            await _webServerDB.SaveChangesAsync();

            // 使用 JsonDocument 解析 LINE Pay 回應
            using JsonDocument doc = JsonDocument.Parse(result.Content);
            JsonElement root = doc.RootElement;
            JsonElement info = root.GetProperty("info");
            JsonElement paymentUrl = info.GetProperty("paymentUrl");
            string webUrl = paymentUrl.GetProperty("web").GetString();
            string appUrl = paymentUrl.GetProperty("app").GetString();

            // 回傳結帳結果
            return Json(new
            {
                liffId = LIFFID,
                webUrl,
                appUrl
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // 保持屬性名稱不變
            });
        }
        catch (Exception ex)
        {
            // 錯誤處理
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 確認訂單
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet("{orderId}")]
    public async Task<IActionResult> OrderConfirm(Guid orderId)
    {
        try
        {
            var order = await _webServerDB.Order.FindAsync(orderId);
            var linePayRequest = await _webServerDB.LINEPayRequest.Where(s => s.OrderNo.Equals(order.OrderNo)).OrderByDescending(s => s.CreatedDT).FirstOrDefaultAsync();
            var checkPaymentStatusAPIResult = await _linePayService.CheckPaymentStatusAPI(linePayRequest.TransactionId.Value);


            // 取得 FlexMessage 範本
            var filePath = Path.Combine(AppContext.BaseDirectory, $"Resources/OrderConfirmMessage.json");
            string flexMessage = System.IO.File.ReadAllText(filePath, Encoding.UTF8);  // 預設使用UTF-8編碼

            // 替換範本中的變數
            flexMessage = flexMessage.Replace("{{取餐號碼}}", order.OrderSeq.ToString());
            flexMessage = flexMessage.Replace("{{查看訂單}}", $"https://liff.line.me/{LIFFID}");
            var flexMessageObject = System.Text.Json.JsonSerializer.Deserialize<object>(flexMessage);

            switch (checkPaymentStatusAPIResult.Result.ReturnCode)
            {
                case "0000"://成功
                    order.PaymentStatus = "已付款";
                    await _lineAPIService.SendMessage(order.LINEUserID, new object[]{
                        new {
                            type = "flex",
                            altText =  "訂單確認",
                            contents = flexMessageObject,
                            notificationDisabled = false,
                        },
                    });
                    //await _lineAPIService.SendMessage(order.LINEUserID, new object[]
                    //{
                    //new {
                    //    type = "text",
                    //    text = $"感謝您的訂購，我們將盡快為您處理。取餐號碼:{order.OrderSeq}",
                    //    notificationDisabled = false,
                    //}
                    //});
                    break;
                case "0110"://授權完成 - 現在可以呼叫Confirm API
                    var confirmAPIResult = await _linePayService.ConfirmAPI(linePayRequest.OrderNo, linePayRequest.TransactionId.Value, new ConfirmAPIRequestBody
                    {
                        Amount = (int)order.TotalAmount,
                        Currency = Models.LINEPayModels.Enums.Currency.TWD,
                    });
                    if(confirmAPIResult.Result.ReturnCode == "0000")
                    {
                        // 此時可去後台查看訂單 https://sandbox-pay.line.me/zh_TW/deal/integrate
                        order.PaymentStatus = "已付款";
                        await _lineAPIService.SendMessage(order.LINEUserID, new object[]{
                            new {
                                type = "flex",
                                altText =  "訂單確認",
                                contents = flexMessageObject,
                                notificationDisabled = false,
                            },
                        });
                        //await _lineAPIService.SendMessage(order.LINEUserID, new object[]
                        //{
                        //new {
                        //    type = "text",
                        //    text = $"感謝您的訂購，我們將盡快為您處理。取餐號碼:{order.OrderSeq}",
                        //    notificationDisabled = false,
                        //}
                        //});
                    }
                    else
                    {
                        order.PaymentStatus = confirmAPIResult.Result.Description;
                    }
                    break;
                case "0123"://付款成功 - 交易已經結束了
                    order.PaymentStatus = "已付款";
                    await _lineAPIService.SendMessage(order.LINEUserID, new object[]{
                        new {
                            type = "flex",
                            altText =  "訂單確認",
                            contents = flexMessageObject,
                            notificationDisabled = false,
                        },
                    });
                    //await _lineAPIService.SendMessage(order.LINEUserID, new object[]
                    //{
                    //    new {
                    //        type = "text",
                    //        text = $"感謝您的訂購，我們將盡快為您處理。取餐號碼:{order.OrderSeq}",
                    //        notificationDisabled = false,
                    //    }
                    //});
                    break;
                default:
                    order.PaymentStatus = checkPaymentStatusAPIResult.Result.Description;
                    break;
            }
            order.ModifiedDT = DateTime.Now;
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(nameof(OrderConfirm), ex);
        }

        return Redirect($"https://liff.line.me/{LIFFID}");
    }

    /// <summary>
    /// 取消訂單
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet("{orderId}")]
    public async Task<IActionResult> OrderCancel(Guid orderId)
    {
        var order = await _webServerDB.Order.FindAsync(orderId);
        order.PaymentStatus = "取消訂單";
        await _webServerDB.SaveChangesAsync();
        return Redirect($"https://liff.line.me/{LIFFID}");
    }

    /// <summary>
    /// 取得訂單歷史
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> GetOrderHistory()
    {
        var result = new List<CartItem>();
        var currentLINEUser = await _siteService.GetCurrentLINEUser();
        if (currentLINEUser == null)
            return Unauthorized();

        var orders = await _webServerDB.Order.Where(s=>s.LINEUserID.Equals(currentLINEUser.ID))
            .OrderByDescending(s => s.OrderDate).ThenByDescending(s => s.OrderSeq).Take(20).ToListAsync();

        orders.ForEach((s) =>
        {
            if(s.PaymentStatus == "待付款")
            {
                var linePayRequest = _webServerDB.LINEPayRequest.Where(s=>s.OrderNo.Equals(s.OrderNo)).OrderByDescending(s => s.CreatedDT).FirstOrDefault();
                if (linePayRequest != null)
                {
                    // 使用 JsonDocument 解析 JSON
                    using JsonDocument doc = JsonDocument.Parse(linePayRequest.ResponseBody);
                    // 獲取根元素
                    JsonElement root = doc.RootElement;
                    // 獲取 info 元素
                    JsonElement info = root.GetProperty("info");
                    // 獲取 paymentUrl 元素
                    JsonElement paymentUrl = info.GetProperty("paymentUrl");
                    // 提取 web 和 app 的值
                    string webUrl = paymentUrl.GetProperty("web").GetString();
                    string appUrl = paymentUrl.GetProperty("app").GetString();
                    s.PaymentUrl = webUrl;
                }
            }
        });

        return PartialView(@"~/Views/CartItem/OrderHistoryPartial.cshtml", orders);
    }
}