using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using PermissionSystemWeb.Models.ViewModels;
using System.IdentityModel.Tokens.Jwt;

public class ReviewController : Controller
{
    private readonly IHttpClientFactory _clientFactory;

    public ReviewController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("token");

        if (token == null)
            return RedirectToAction("Login", "Account");

        // استخراج الـ dbUserId من الـ JWT
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var dbUserId = jwt.Claims.FirstOrDefault(c => c.Type == "dbUserId")?.Value;

        if (string.IsNullOrEmpty(dbUserId))
        {
            ViewBag.Error = "User not found in system";
            return View(new PeriodicReviewPageVm());
        }

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var overview = await client.GetFromJsonAsync<ReviewOverviewVm>("api/review/overview");
            var items = await client.GetFromJsonAsync<List<ReviewItemVm>>("api/review/items");
            var history = await client.GetFromJsonAsync<List<ReviewHistoryVm>>("api/review/history");

            var vm = new PeriodicReviewPageVm
            {
                Overview = overview ?? new ReviewOverviewVm(),
                Items = items ?? new List<ReviewItemVm>(),
                History = history ?? new List<ReviewHistoryVm>()
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            // لو فيه أي error نرجع فيو empty بدل ما يقع
            var vm = new PeriodicReviewPageVm
            {
                Overview = new ReviewOverviewVm(),
                Items = new List<ReviewItemVm>(),
                History = new List<ReviewHistoryVm>()
            };

            ViewBag.Error = "Failed to load review data";
            return View(vm);
        }
    }

    // يبدأ دورة Review جديدة
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartCycle()
    {
        var token = HttpContext.Session.GetString("token");

        if (token == null)
            return RedirectToAction("Login", "Account");

        // استخراج الـ dbUserId من الـ JWT
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var dbUserId = jwt.Claims.FirstOrDefault(c => c.Type == "dbUserId")?.Value;

        if (string.IsNullOrEmpty(dbUserId))
        {
            TempData["error"] = "User not found in system";
            return RedirectToAction("Index");
        }

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new { ManagerUserId = int.Parse(dbUserId) };
        var response = await client.PostAsJsonAsync("api/review/start", body);

        if (!response.IsSuccessStatusCode)
        {
            TempData["error"] = "Failed to start review cycle";
            return RedirectToAction("Index");
        }

        TempData["success"] = "New review cycle started.";
        return RedirectToAction("Index");
    }

    // حفظ قرارات ال Review (Keep / Revoke لكل صف)
    // حفظ قرارات ال Review (Keep / Revoke لكل صف)
    [HttpPost]
    public async Task<IActionResult> SaveDecisions([FromBody] List<SaveDecisionVm> decisions)
    {
        var token = HttpContext.Session.GetString("token");
        if (token == null)
            return Json(new { success = false, message = "Not authenticated" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("api/review/save-decisions", decisions);

        if (!response.IsSuccessStatusCode)
        {
            return Json(new { success = false, message = "Failed to save decisions" });
        }

        // ✅ الطريقة الأولى - باستخدام DTO
        var result = await response.Content.ReadFromJsonAsync<SaveDecisionResult>();
        return Json(new { success = true, saved = result.Saved });

        // ✅ الطريقة الثانية - باستخدام dynamic
        // var result = await response.Content.ReadFromJsonAsync<dynamic>();
        // return Json(new { success = true, saved = result.saved });
    }

    // تأكيد وإقفال دورة ال Review
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm()
    {
        var token = HttpContext.Session.GetString("token");
        if (token == null)
            return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("api/review/confirm", null);

        if (!response.IsSuccessStatusCode)
        {
            TempData["error"] = "Failed to confirm review.";
            return RedirectToAction("Index");
        }

        TempData["success"] = "Review confirmed successfully.";
        return RedirectToAction("Index");
    }

    // جلب الـ Review History
    [HttpGet]
    public async Task<IActionResult> GetReviewHistory()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Json(new { success = false, message = "Not authenticated" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("api/review/history");

        if (!response.IsSuccessStatusCode)
            return Json(new { success = false, message = "Failed to get history" });

        var history = await response.Content.ReadFromJsonAsync<List<ReviewHistoryVm>>();
        return Json(new { success = true, data = history });
    }
}

 