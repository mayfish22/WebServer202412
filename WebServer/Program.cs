using Coravel;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.WebServerDB;
using WebServer.Services;
using WebServer.Services.Invocables;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace WebServer;
public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // 在這裡放置可能會引發例外的程式碼
            var builder = WebApplication.CreateBuilder(args);

            #region Serilog

            // 獲取當前環境變數，通常用於判斷應用程序的運行環境（例如開發、測試或生產）
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // 預設配置為 "Release"
            var confg = "Release";

            // 如果在 DEBUG 模式下，將配置設置為 "Debug"
#if DEBUG
            confg = "Debug";
#endif

            // 獲取名為 "WebServerDBLog" 的連接字符串，通常用於連接到數據庫
            var logDB = builder.Configuration.GetConnectionString("WebServerDBLog");

            // 設置 MSSQL Server 日誌接收器的選項
            var sinkOpts = new MSSqlServerSinkOptions
            {
                // 自動創建 SQL 表
                AutoCreateSqlTable = true,
                // 設置日誌表的架構名稱
                SchemaName = "log",
                // 設置日誌表的名稱，包含環境和配置類型
                TableName = $"WebServer_{env}_{confg}"
            };

            // 設置列選項
            var columnOpts = new ColumnOptions();
            // 移除標準列中的屬性列
            columnOpts.Store.Remove(StandardColumn.Properties);
            // 添加日誌事件列
            columnOpts.Store.Add(StandardColumn.LogEvent);
            // 設置日誌事件的數據長度為無限制
            columnOpts.LogEvent.DataLength = -1;
            // 將時間戳設置為非聚集索引
            columnOpts.TimeStamp.NonClusteredIndex = true;

            // 配置 Serilog 日誌記錄器
            Log.Logger = new LoggerConfiguration()
                // 設置最小日誌級別為 Information
                .MinimumLevel.Information()
                // 覆蓋 Microsoft.AspNetCore 的最小日誌級別為 Warning
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                // 覆蓋 Microsoft.EntityFrameworkCore 的最小日誌級別為 Warning
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                // 覆蓋 Microsoft.EntityFrameworkCore.Database.Command 的最小日誌級別為 Warning
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                // 增強日誌信息，添加機器名稱
                .Enrich.WithMachineName()
                // 增強日誌信息，添加環境名稱
                .Enrich.WithEnvironmentName()
                // 增強日誌信息，添加當前用戶名
                .Enrich.WithEnvironmentUserName()
                // 將日誌寫入控制台
                .WriteTo.Console()
                // 將日誌寫入 MSSQL Server，使用指定的連接字符串和選項
                .WriteTo.MSSqlServer(
                    connectionString: logDB,
                    sinkOptions: sinkOpts,
                    columnOptions: columnOpts)
                // 創建日誌記錄器
                .CreateLogger();

            // 啟用 Serilog 的自我日誌，將錯誤信息輸出到控制台
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

            // 使用 Serilog 作為主機的日誌記錄器
            builder.Host.UseSerilog();

            #endregion

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<WebServerDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("WebServerDB"));
            });

            //資料驗證
            builder.Services.AddScoped<ValidatorService>();

            // 使用 Session
            // 使用分散式記憶體快取 (Distributed Memory Cache)，它在應用程式範圍內存儲Session資料。
            builder.Services.AddDistributedMemoryCache();

            // 設置 Session 相關的配置
            builder.Services.AddSession(options =>
            {
                // 設定會話的閒置超時時間為 60 分鐘
                options.IdleTimeout = TimeSpan.FromMinutes(60);

                // 設定會話 Cookie 只能由伺服器端讀取，避免 JavaScript 存取
                options.Cookie.HttpOnly = true;

                // 設定會話 Cookie 為必要的，這表示該 Cookie 必須被用戶端儲存並且在每次請求時發送
                options.Cookie.IsEssential = true;
            });

            // 設定應用程式的認證方式，這裡使用 Cookie 驗證方案
            builder.Services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme) // 設定認證方案使用 Cookie 認證
                .AddCookie(options =>
                {
                    // 設定當存取被拒絕的頁面時，將轉跳至指定的路徑 (例如，無權限存取頁面會導向至這個頁面)
                    options.AccessDeniedPath = new PathString("/Account/Signin");

                    // 設定當使用者未登入時，將轉跳至登入頁面
                    options.LoginPath = new PathString("/Account/Signin");

                    // 設定登出後的轉跳頁面
                    options.LogoutPath = new PathString("/Account/Signout");
                });

            // 註冊 HttpClient 服務，允許在應用程序中使用 HttpClient 進行 HTTP 請求
            builder.Services.AddHttpClient();

            // 註冊 IHttpContextAccessor 服務，這樣可以在應用程序中訪問 HttpContext
            // HttpContext 提供有關當前 HTTP 請求的資訊
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // 註冊 Coravel 的 Queue 服務，這樣可以在應用程序中使用任務排隊功能
            builder.Services.AddQueue();

            // 註冊 Invocable
            builder.Services.AddTransient<LINEWebhookInvocable>();

            // 註冊自訂服務
            builder.Services.AddScoped<LINEAPIService>();
            builder.Services.AddScoped<SiteService>();
            builder.Services.AddScoped<GeminiAPIService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication(); //驗證 

            app.UseAuthorization(); //授權

            #region RequestLogging: 使用JSON記錄
            // 設定一個中間件來記錄每個請求的詳細信息
            app.Use(async (httpContext, next) =>
            {
                // 創建一個匿名對象來存儲請求的相關信息
                var message = new
                {
                    // 獲取用戶的 ID，從 Claims 中提取 NameIdentifier 類型的聲明
                    UserID = httpContext.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,

                    // 獲取用戶的名稱，從 Claims 中提取 Name 類型的聲明
                    Account = httpContext.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.Name)?.Value,

                    // 獲取請求的來源 IP 地址
                    IP = httpContext.Connection.RemoteIpAddress.ToString(),

                    // 獲取請求的方法（GET、POST 等）
                    Method = httpContext.Request.Method,

                    // 獲取請求的協議（http 或 https）
                    Scheme = httpContext.Request.Scheme,

                    // 獲取請求的主機名稱（如 www.example.com）
                    Host = httpContext.Request.Host.HasValue ? httpContext.Request.Host.Value : null,

                    // 獲取請求的路徑
                    Path = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null,

                    // 獲取請求的查詢字符串
                    QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null,
                };

                // 將請求信息序列化為 JSON 格式並記錄到日誌中
                Log.Information(System.Text.Json.JsonSerializer.Serialize(new
                {
                    // 設定日誌的動作名稱
                    Action = "RequestLogging",

                    // 將請求的詳細信息作為日誌數據
                    Data = message,
                }));

                // 調用下一個中間件
                await next();
            });
            #endregion

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
        catch (Exception ex)
        {
            // 捕捉到例外後，記錄錯誤訊息
            Log.Fatal(ex, "主機意外終止");
        }
        finally
        {
            // 確保在程式結束時關閉日誌
            Log.CloseAndFlush();
        }
    }
}