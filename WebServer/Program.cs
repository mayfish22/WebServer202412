using Coravel;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.WebServerDB;
using WebServer.Services;
using WebServer.Services.Invocables;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace WebServer;
public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // �b�o�̩�m�i��|�޵o�ҥ~���{���X
            var builder = WebApplication.CreateBuilder(args);

            #region Serilog

            // �����e�����ܼơA�q�`�Ω�P�_���ε{�Ǫ��B�����ҡ]�Ҧp�}�o�B���թΥͲ��^
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // �w�]�t�m�� "Release"
            var confg = "Release";

            // �p�G�b DEBUG �Ҧ��U�A�N�t�m�]�m�� "Debug"
#if DEBUG
            confg = "Debug";
#endif

            // ����W�� "WebServerDBLog" ���s���r�Ŧ�A�q�`�Ω�s����ƾڮw
            var logDB = builder.Configuration.GetConnectionString("WebServerDBLog");

            // �]�m MSSQL Server ��x���������ﶵ
            var sinkOpts = new MSSqlServerSinkOptions
            {
                // �۰ʳЫ� SQL ��
                AutoCreateSqlTable = true,
                // �]�m��x���[�c�W��
                SchemaName = "log",
                // �]�m��x���W�١A�]�t���ҩM�t�m����
                TableName = $"WebServer_{env}_{confg}"
            };

            // �]�m�C�ﶵ
            var columnOpts = new ColumnOptions();
            // �����зǦC�����ݩʦC
            columnOpts.Store.Remove(StandardColumn.Properties);
            // �K�[��x�ƥ�C
            columnOpts.Store.Add(StandardColumn.LogEvent);
            // �]�m��x�ƥ󪺼ƾڪ��׬��L����
            columnOpts.LogEvent.DataLength = -1;
            // �N�ɶ��W�]�m���D�E������
            columnOpts.TimeStamp.NonClusteredIndex = true;

            // �t�m Serilog ��x�O����
            Log.Logger = new LoggerConfiguration()
                // �]�m�̤p��x�ŧO�� Information
                .MinimumLevel.Information()
                // �л\ Microsoft.AspNetCore ���̤p��x�ŧO�� Warning
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                // �л\ Microsoft.EntityFrameworkCore ���̤p��x�ŧO�� Warning
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                // �л\ Microsoft.EntityFrameworkCore.Database.Command ���̤p��x�ŧO�� Warning
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                // �W�j��x�H���A�K�[�����W��
                .Enrich.WithMachineName()
                // �W�j��x�H���A�K�[���ҦW��
                .Enrich.WithEnvironmentName()
                // �W�j��x�H���A�K�[��e�Τ�W
                .Enrich.WithEnvironmentUserName()
                // �N��x�g�J����x
                .WriteTo.Console()
                // �N��x�g�J MSSQL Server�A�ϥΫ��w���s���r�Ŧ�M�ﶵ
                .WriteTo.MSSqlServer(
                    connectionString: logDB,
                    sinkOptions: sinkOpts,
                    columnOptions: columnOpts)
                // �Ыؤ�x�O����
                .CreateLogger();

            // �ҥ� Serilog ���ۧڤ�x�A�N���~�H����X�챱��x
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

            // �ϥ� Serilog �@���D������x�O����
            builder.Host.UseSerilog();

            #endregion

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<WebServerDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("WebServerDB"));
            });

            //�������
            builder.Services.AddScoped<ValidatorService>();

            // �ϥ� Session
            // �ϥΤ������O����֨� (Distributed Memory Cache)�A���b���ε{���d�򤺦s�xSession��ơC
            builder.Services.AddDistributedMemoryCache();

            // �]�m Session �������t�m
            builder.Services.AddSession(options =>
            {
                // �]�w�|�ܪ����m�W�ɮɶ��� 60 ����
                options.IdleTimeout = TimeSpan.FromMinutes(60);

                // �]�w�|�� Cookie �u��Ѧ��A����Ū���A�קK JavaScript �s��
                options.Cookie.HttpOnly = true;

                // �]�w�|�� Cookie �����n���A�o��ܸ� Cookie �����Q�Τ���x�s�åB�b�C���ШD�ɵo�e
                options.Cookie.IsEssential = true;
            });

            // �]�w���ε{�����{�Ҥ覡�A�o�̨ϥ� Cookie ���Ҥ��
            builder.Services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme) // �]�w�{�Ҥ�רϥ� Cookie �{��
                .AddCookie(options =>
                {
                    // �]�w��s���Q�ڵ��������ɡA�N����ܫ��w�����| (�Ҧp�A�L�v���s�������|�ɦV�ܳo�ӭ���)
                    options.AccessDeniedPath = new PathString("/Account/Signin");

                    // �]�w��ϥΪ̥��n�J�ɡA�N����ܵn�J����
                    options.LoginPath = new PathString("/Account/Signin");

                    // �]�w�n�X�᪺�������
                    options.LogoutPath = new PathString("/Account/Signout");
                })
                .AddJwtBearer(options =>
                {
                    // �]�w Token ���ҰѼ�
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // �]�w�ɶ������A�w�]�� 5 �����A�o�̳]�� 0�A��ܤ����\�ɶ�����
                        ClockSkew = TimeSpan.Zero,

                        // �]�w�Ω�����Τ�W�٪��n�������A�o�̨ϥ� "sub" �n��
                        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",

                        // �]�w�Ω�����Τᨤ�⪺�n�������A�o�̨ϥ� "roles" �n��
                        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

                        // ���ҵo��̪��]�w
                        ValidateIssuer = true, // �ҥεo�������
                        ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"), // �q�t�m��������Ī��o���

                        // ���Ҩ������]�w
                        ValidateAudience = true, // �ҥΨ�������
                        ValidAudience = builder.Configuration.GetValue<string>("JwtSettings:Audience"), // �q�t�m��������Ī�����

                        // ���� Token �����Ĵ���
                        ValidateLifetime = true, // �ҥΦ��Ĵ�������

                        // ����ñ�W���]�w
                        ValidateIssuerSigningKey = true, // �ҥ�ñ�W���_����
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:SignKey"))) // �q�t�m�����ñ�W���_
                    };
                });

            // ���U HttpClient �A�ȡA���\�b���ε{�Ǥ��ϥ� HttpClient �i�� HTTP �ШD
            builder.Services.AddHttpClient();

            // ���U IHttpContextAccessor �A�ȡA�o�˥i�H�b���ε{�Ǥ��X�� HttpContext
            // HttpContext ���Ѧ�����e HTTP �ШD����T
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // ���U Coravel �� Queue �A�ȡA�o�˥i�H�b���ε{�Ǥ��ϥΥ��ȱƶ��\��
            builder.Services.AddQueue();

            // ���U Invocable
            builder.Services.AddTransient<LINEWebhookInvocable>();

            // ���U�ۭq�A��
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

            app.UseAuthentication(); //���� 

            app.UseAuthorization(); //���v

            #region RequestLogging: �ϥ�JSON�O��
            // �]�w�@�Ӥ�����ӰO���C�ӽШD���ԲӫH��
            app.Use(async (httpContext, next) =>
            {
                // �Ыؤ@�ӰΦW��H�Ӧs�x�ШD�������H��
                var message = new
                {
                    // ����Τ᪺ ID�A�q Claims ������ NameIdentifier �������n��
                    UserID = httpContext.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,

                    // ����Τ᪺�W�١A�q Claims ������ Name �������n��
                    Account = httpContext.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.Name)?.Value,

                    // ����ШD���ӷ� IP �a�}
                    IP = httpContext.Connection.RemoteIpAddress.ToString(),

                    // ����ШD����k�]GET�BPOST ���^
                    Method = httpContext.Request.Method,

                    // ����ШD����ĳ�]http �� https�^
                    Scheme = httpContext.Request.Scheme,

                    // ����ШD���D���W�١]�p www.example.com�^
                    Host = httpContext.Request.Host.HasValue ? httpContext.Request.Host.Value : null,

                    // ����ШD�����|
                    Path = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null,

                    // ����ШD���d�ߦr�Ŧ�
                    QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null,
                };

                // �N�ШD�H���ǦC�Ƭ� JSON �榡�ðO�����x��
                Log.Information(System.Text.Json.JsonSerializer.Serialize(new
                {
                    // �]�w��x���ʧ@�W��
                    Action = "RequestLogging",

                    // �N�ШD���ԲӫH���@����x�ƾ�
                    Data = message,
                }));

                // �եΤU�@�Ӥ�����
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
            // ������ҥ~��A�O�����~�T��
            Log.Fatal(ex, "�D���N�~�פ�");
        }
        finally
        {
            // �T�O�b�{��������������x
            Log.CloseAndFlush();
        }
    }
}