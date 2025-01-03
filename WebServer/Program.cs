using Coravel;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.WebServerDB;
using WebServer.Services;
using WebServer.Services.Invocables;

namespace WebServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
