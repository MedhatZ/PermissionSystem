using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using PermissionSystemWeb.Models.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

public class DashboardController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IHttpClientFactory clientFactory, ILogger<DashboardController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        _logger?.LogInformation("Dashboard Index invoked");
        // 1) Read token from session
        var token = HttpContext.Session.GetString("token");
        if (token == null)
            return RedirectToAction("Login", "Account");

        // 2) Decode JWT and extract claims
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var exists = jwt.Claims.FirstOrDefault(c => c.Type == "exists")?.Value;
        var dbUserId = jwt.Claims.FirstOrDefault(c => c.Type == "dbUserId")?.Value;
        var employeeId = jwt.Claims.FirstOrDefault(c => c.Type == "employeeId")?.Value;

        var role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

        _logger?.LogDebug("JWT claims: exists={Exists}, dbUserId={DbUserId}, employeeId={EmployeeId}, role={Role}", exists, dbUserId, employeeId, role);
        // 3) If user does NOT exist in DB → redirect to Requests/Index
        if (role == "User" || string.IsNullOrEmpty(dbUserId))
        {
            _logger?.LogInformation("User with role '{Role}' or missing dbUserId - redirecting to Requests/Index", role);
            return RedirectToAction("Index", "Requests");
        }else if (role == "Implementor")
        {
            _logger?.LogInformation("Role is Implementor - redirecting to Approvals/Index");
            return RedirectToAction("Index", "Approvals");
        }

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // جلب البيانات من الـ API
            _logger?.LogInformation("Requesting dashboard data from API for dbUserId={DbUserId} empId={EmployeeId}", dbUserId, employeeId);
            var stats = await client.GetFromJsonAsync<DashboardStatsVm>($"api/dashboard/stats?userId={dbUserId}&empId={employeeId}");
            var recent = await client.GetFromJsonAsync<List<RecentRequestVm>>("api/dashboard/recent");
            var pending = await client.GetFromJsonAsync<List<PendingApprovalVm>>($"api/Requests/pending/direct?userId={dbUserId}");
            var activity = await client.GetFromJsonAsync<List<ActivityLogVm>>("api/dashboard/activity");
            var chart = await client.GetFromJsonAsync<DashboardChartVm>("api/dashboard/chart");

            var vm = new DashboardViewModel
            {
                Stats = stats ?? new DashboardStatsVm(),
                Recent = recent ?? new List<RecentRequestVm>(),
                Pending = pending ?? new List<PendingApprovalVm>(),
                Activity = activity ?? new List<ActivityLogVm>(),
                Chart = chart ?? new DashboardChartVm()
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            // لو فيه أي error نرجع فيو empty بدل ما يقع
            _logger?.LogError(ex, "Failed to load dashboard data for dbUserId={DbUserId}", jwt?.Claims.FirstOrDefault(c => c.Type == "dbUserId")?.Value);
            var vm = new DashboardViewModel
            {
                Stats = new DashboardStatsVm(),
                Recent = new List<RecentRequestVm>(),
                Pending = new List<PendingApprovalVm>(),
                Activity = new List<ActivityLogVm>(),
                Chart = new DashboardChartVm()
            };

            ViewBag.Error = "Failed to load dashboard data";
            return View(vm);
        }
    }
}
