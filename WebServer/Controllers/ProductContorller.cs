using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using WebServer.Models.ViewModels;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers;

// 使用 [Authorize] 特性來限制訪問此控制器的用戶必須經過授權
[Authorize]
// 使用 [Route] 特性來定義控制器的路由規則
[Route("{controller}/{action=Index}")]
public class ProductController : Controller  // 定義產品控制器類別，繼承自 Controller 基類
{
    private readonly WebServerDBContext _webServerDB;  // 定義只讀的資料庫上下文變數

    // 控制器的建構函數，接受 WebServerDBContext 的實例作為參數
    public ProductController(WebServerDBContext webServerDB)
    {
        _webServerDB = webServerDB;  // 將傳入的資料庫上下文賦值給私有變數
    }
    // ⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇ 在這下面加上 Index、Create、Detail、Edit、Delete ⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇⬇

    #region Index
    // GET: /Product/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Task.Yield();  // 讓出控制權，允許其他任務執行
        return View("~/Views/Product/Index.cshtml");  // 返回指定的視圖
    }

    // POST: /Product/GetData
    [HttpPost]
    public async Task<IActionResult> GetData(int draw, int start, int length)
    {
        try
        {
            // 從資料庫中查詢 Product 表
            var query = from n1 in _webServerDB.Product
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
                query = query.Where(t => t.ProductCode.ToUpper().Contains(sQuery)
                            || t.ProductName.ToUpper().Contains(sQuery)
                            || (!string.IsNullOrEmpty(t.ProductDescription) && t.ProductDescription.ToUpper().Contains(sQuery)));
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
                case nameof(Product.ProductCode):
                    query = bDescending ? query.OrderByDescending(o => o.ProductCode) : query.OrderBy(o => o.ProductCode);
                    break;
                case nameof(Product.ProductName):
                    query = bDescending ? query.OrderByDescending(o => o.ProductName) : query.OrderBy(o => o.ProductName);
                    break;
                case nameof(Product.ProductDescription):
                    query = bDescending ? query.OrderByDescending(o => o.ProductDescription) : query.OrderBy(o => o.ProductDescription);
                    break;
                case nameof(Product.UnitPrice):
                    query = bDescending ? query.OrderByDescending(o => o.UnitPrice) : query.OrderBy(o => o.UnitPrice);
                    break;
                case nameof(Product.CreatedDT):
                    query = bDescending ? query.OrderByDescending(o => o.CreatedDT) : query.OrderBy(o => o.CreatedDT);
                    break;
                case nameof(Product.ModifiedDT):
                    query = bDescending ? query.OrderByDescending(o => o.ModifiedDT) : query.OrderBy(o => o.ModifiedDT);
                    break;
                default:
                    query = query.OrderBy(o => o.ProductCode);  // 默認排序
                    break;
            }
            #endregion 排序

            // 獲取過濾後的記錄數
            var recordsFiltered = await query.CountAsync();

            // 根據分頁參數獲取當前頁的數據
            var list = recordsFiltered == 0
                ? new List<Product>() // 如果沒有過濾後的記錄，返回空列表
                : query.Skip(start).Take(Math.Min(length, recordsFiltered - start)).ToList(); // 分頁查詢

            list.ForEach(s =>
            {
                s.MainImageURL = $"/Streaming/Download/{s.MainImageFileID}";
            });

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
            Log.Error(e, $"{nameof(ProductController)}.{nameof(GetData)}");

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

    #region GetProductViewModelAsync
    /// <summary>
    /// 將產品轉為 ProductViewModel
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="isReadonly"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<ProductViewModel> GetProductViewModelAsync(Guid? id, bool isReadonly)
    {
        if (id.HasValue) // 檢查 id 是否有值
        {
            // 嘗試從數據庫中查找指定 id 的產品
            var product = await _webServerDB.Product.FindAsync(id);

            // 如果找不到產品，則拋出異常
            if (product == null)
                throw new Exception("查無產品");

            // 獲取與產品相關的圖片 ID，並按序號排序
            var productImages = await _webServerDB.ProductImage
                .Where(s => s.ProductID.Equals(product.ID)) // 過濾出與產品 ID 匹配的圖片
                .OrderBy(s => s.Seq) // 按 Seq 屬性排序
                .Select(s => s.FileID) // 只選擇 FileID
                ?.ToArrayAsync(); // 將結果轉換為數組

            // 創建並初始化 ProductViewModel 實例
            var model = new ProductViewModel
            {
                IsReadonly = isReadonly, // 設置 IsReadonly 屬性
                Product = product, // 設置 Product 屬性為查找到的產品
                ProductImages = productImages == null ? Array.Empty<Guid>() : productImages, // 如果 productImages 為 null，則初始化為空的 Guid 陣列，否則使用查找到的圖片 ID
            };

            return model; // 返回初始化的 ProductViewModel
        }
        else // 如果 id 沒有值
        {
            // 創建一個新的 ProductViewModel 實例，並初始化產品為新建的產品
            var model = new ProductViewModel
            {
                IsReadonly = isReadonly, // 設置 IsReadonly 屬性
                Product = new Product
                {
                    ID = Guid.NewGuid(), // 為新產品生成一個新的 ID
                },
                ProductImages = Array.Empty<Guid>(), // 初始化 ProductImages 為空的 Guid 陣列
            };

            return model; // 返回初始化的 ProductViewModel
        }
    }
    #endregion

    #region Create

    // GET: /User/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // 異步獲取用戶視圖模型，傳入 null 表示創建新用戶，第二個參數為 false 表示不使用只讀模式
        var model = await GetProductViewModelAsync(null, false);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/Product/Default.cshtml", model);
    }

    // POST: /User/Create
    [HttpPost] // 標記此方法為 HTTP POST 請求的處理方法
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊，確保請求的合法性
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        try
        {
            // 設定模型的 IsReadonly 屬性為 false，表示該模型是可編輯的
            model.IsReadonly = false;

            // 如果 ProductImages 屬性為 null，則將其初始化為一個空的 Guid 陣列
            // 這樣可以避免在後續操作中出現 NullReferenceException
            model.ProductImages = model.ProductImages ?? Array.Empty<Guid>();

            // 過濾 ProductImages 陣列，移除所有值為 Guid.Empty 的項目
            // 這樣可以確保最終的 ProductImages 陣列中不包含任何空的 Guid
            model.ProductImages = model.ProductImages.Where(s => !s.Equals(Guid.Empty)).ToArray();

            // 檢查模型的狀態是否有效
            if (!ModelState.IsValid)
                // 如果模型狀態無效，返回預設視圖並傳遞模型
                return View("~/Views/Product/Default.cshtml", model);

            // 去除產品代碼和名稱的前後空白
            model.Product.ProductCode = model.Product.ProductCode.Trim();
            model.Product.ProductName = model.Product.ProductName.Trim();
            // 設定產品的創建時間為當前時間
            model.Product.CreatedDT = DateTime.Now;

            // 將產品添加到資料庫上下文中，準備進行保存
            await _webServerDB.Product.AddAsync(model.Product);

            // 創建產品圖片的集合，為每個圖片生成新的 ProductImage 實例
            var productImages = model.ProductImages.Select((s, i) => new ProductImage
            {
                ID = Guid.NewGuid(), // 為每個圖片生成唯一的 ID
                Seq = i + 1, // 設定圖片的序號，從 1 開始
                ProductID = model.Product.ID, // 設定圖片所屬產品的 ID
                FileID = s, // 設定圖片的文件 ID
                CreatedDT = DateTime.Now, // 設定圖片的創建時間為當前時間
            });

            // 將產品和產品圖片添加到資料庫上下文中
            await _webServerDB.Product.AddAsync(model.Product);
            await _webServerDB.ProductImage.AddRangeAsync(productImages);
            // 提交所有變更到資料庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(ProductViewModel.ErrorMessage), e.Message);
            // 返回預設視圖並傳遞模型
            return View("~/Views/Product/Default.cshtml", model);
        }

        // 重定向到 Index 方法，通常是產品列表頁面
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Detail
    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 true
        var model = await GetProductViewModelAsync(id, true);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/Product/Default.cshtml", model);
    }
    #endregion

    #region Edit

    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 false
        var model = await GetProductViewModelAsync(id, false);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/Product/Default.cshtml", model);
    }

    // 定義一個 HTTP POST 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpPost("{id:Guid}")] // 指定此方法處理 HTTP POST 請求，並將路由中的 id 參數設置為 Guid 類型
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊，確保請求來自合法用戶
    public async Task<IActionResult> Edit(Guid id, ProductViewModel model)
    {
        try
        {
            // 設定模型的 IsReadonly 屬性為 false，表示該模型是可編輯的
            model.IsReadonly = false;

            // 如果 ProductImages 屬性為 null，則將其初始化為一個空的 Guid 陣列
            // 這樣可以避免在後續操作中出現 NullReferenceException
            model.ProductImages = model.ProductImages ?? Array.Empty<Guid>();

            // 過濾 ProductImages 陣列，移除所有值為 Guid.Empty 的項目
            // 這樣可以確保最終的 ProductImages 陣列中不包含任何空的 Guid
            model.ProductImages = model.ProductImages.Where(s => !s.Equals(Guid.Empty)).ToArray();

            // 檢查模型的狀態是否有效
            if (!ModelState.IsValid)
                // 如果模型狀態無效，返回預設視圖並傳遞模型
                return View("~/Views/Product/Default.cshtml", model);

            // 嘗試從數據庫中查找指定 id 的產品
            var product = await _webServerDB.Product.FindAsync(model.Product.ID);
            if (product == null)
                throw new Exception("產品不存在"); // 如果產品不存在，拋出異常

            // 更新產品的屬性
            product.ProductCode = model.Product.ProductCode.Trim(); // 去除產品代碼的前後空格
            product.ProductName = model.Product.ProductName.Trim(); // 去除產品名稱的前後空格
            product.ProductDescription = model.Product.ProductDescription; // 更新產品描述
            product.UnitPrice = model.Product.UnitPrice; // 更新產品單價
            product.MainImageFileID = model.Product.MainImageFileID; // 更新主圖片的文件 ID
            product.ModifiedDT = DateTime.Now; // 設置修改時間為當前時間

            // 創建當前產品圖片的列表，並設置相關屬性
            var currentProductImages = model.ProductImages.Select((s, i) => new ProductImage
            {
                ID = Guid.NewGuid(), // 為每個產品圖片生成新的 ID
                Seq = i + 1, // 設置圖片的序號
                ProductID = product.ID, // 設置產品 ID
                FileID = s, // 設置文件 ID
                CreatedDT = DateTime.Now, // 設置創建時間為當前時間
                ModifiedDT = DateTime.Now, // 設置修改時間為當前時間
            }).ToList();

            // 從數據庫中獲取當前產品的圖片列表
            var dbProductImages = await _webServerDB.ProductImage.Where(s => s.ProductID.Equals(product.ID)).ToListAsync();

            // 確定需要插入的圖片項目
            var insertItems = from n1 in currentProductImages
                              join n2 in dbProductImages on n1.FileID equals n2.FileID into tempN2
                              from n2 in tempN2.DefaultIfEmpty()
                              where n2 == null // 只選擇在數據庫中不存在的圖片
                              select n1;

            // 確定需要刪除的圖片項目
            var removeItems = from n1 in dbProductImages
                              join n2 in currentProductImages on n1.FileID equals n2.FileID into tempN2
                              from n2 in tempN2.DefaultIfEmpty()
                              where n2 == null // 只選擇在當前圖片列表中不存在的圖片
                              select n1;

            // 確定需要更新的圖片項目
            var updateItems = from n1 in dbProductImages
                              join n2 in currentProductImages on n1.FileID equals n2.FileID
                              select n1;

            // 如果有需要插入的圖片，則批量添加到數據庫
            if (insertItems.Any())
                await _webServerDB.ProductImage.AddRangeAsync(insertItems);

            // 如果有需要刪除的圖片，則批量刪除
            if (removeItems.Any())
                _webServerDB.RemoveRange(removeItems);

            // 如果有需要更新的圖片，則逐一更新
            if (updateItems.Any())
            {
                foreach (var item in updateItems)
                {
                    // 獲取當前圖片的對應項目
                    var currentItem = currentProductImages!.Where(s => s.FileID.Equals(item.FileID)).FirstOrDefault();
                    if (item.Seq != currentItem.Seq) // 如果序號不同，代表有異動
                    {
                        item.Seq = currentItem.Seq;
                        item.ModifiedDT = currentItem.ModifiedDT; // 更新修改時間
                    }
                }
            }

            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(ProductViewModel.ErrorMessage), e.Message);
            // 返回視圖並傳遞當前模型，顯示錯誤信息
            return View("~/Views/Product/Default.cshtml", model);
        }

        // 編輯成功後，重定向到用戶列表頁面
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Delete

    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 true
        var model = await GetProductViewModelAsync(id, true);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/Product/Default.cshtml", model);
    }

    // 定義一個 HTTP POST 請求的路由，要求 id 參數必須是 Guid 類型，並將動作名稱設置為 Delete
    [HttpPost("{id:Guid}"), ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            var product = await _webServerDB.Product.FindAsync(id);
            // 檢查產品是否存在
            if (product == null)
                throw new Exception("產品不存在");
            var productImages = await _webServerDB.ProductImage.Where(s => s.ProductID.Equals(product.ID)).Select(s => s).ToListAsync();
            if (productImages.Count > 0)
                _webServerDB.ProductImage.RemoveRange(productImages);
            _webServerDB.Product.Remove(product);
            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，重新獲取用戶視圖模型
            var model = await GetProductViewModelAsync(id, true);
            // 將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(UserViewModel.ErrorMessage), e.Message);
            // 返回視圖並傳遞當前模型
            return View("~/Views/Product/Default.cshtml", model);
        }

        // 重定向到用戶列表頁面
        return RedirectToAction(nameof(Index));
    }
    #endregion

    // ⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆ 在這上面加上 Index、Create、Detail、Edit、Delete ⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆⬆
}