using Microsoft.Extensions.Options;
using PermissionSystemWeb.Models.ViewModels;
 
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using Rotativa.AspNetCore;

// Enforce TLS
System.Net.ServicePointManager.SecurityProtocol =
    SecurityProtocolType.Tls12 |
    SecurityProtocolType.Tls11 |
    SecurityProtocolType.Tls;

// ========== Build Setup ==========
var builder = WebApplication.CreateBuilder(args);

// ========== Configure Serilog ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "C:/saptco/PermissionSystemWeb/logs/permission-log-.txt",
        rollingInterval: RollingInterval.Day,
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog(); // Use Serilog before building the app

Log.Information("🔧 Application starting up...");

// ========== Services ==========

// MVC + Razor Views
builder.Services.AddControllersWithViews();

// Read API URL from appsettings
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<IOptions<ApiSettings>>().Value);

// Enable Sessions
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure HttpClient
var apiUrl = builder.Configuration["ApiSettings:BaseUrl"];
Log.Information("🌐 API Base URL: {ApiUrl}", apiUrl);

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.ExpectContinue = false;
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
});

// Cookie Authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Account/Login";
    });

// ========== Build App ==========
var app = builder.Build();

app.UseSerilogRequestLogging(); // Middleware for request logging

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseRotativa();

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

try
{
    Log.Information("🚀 Application started successfully.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
