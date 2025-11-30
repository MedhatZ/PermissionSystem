using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using PermissionSystemWeb.Models;
using PermissionSystemWeb.Models.ViewModels;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
 
using System.Text;
using Rotativa;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;

public class RequestsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ApiSettings _settings;

    public RequestsController(IHttpClientFactory clientFactory, IOptions<ApiSettings> settings)
    {
        _clientFactory = clientFactory;
        _settings = settings.Value;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateRequestVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRequestVm vm)
    {
        if (vm.SystemName == "InvoiceQ")
        {
            if (string.IsNullOrWhiteSpace(vm.Department) ||
                string.IsNullOrWhiteSpace(vm.InvoiceQJobTitle) ||
                string.IsNullOrWhiteSpace(vm.Unit))
            {
                ViewBag.Error = "Please fill all required InvoiceQ fields.";
                return View(vm);
            }
        }

        if (vm.SystemName == "Ocity")
        {
            if (string.IsNullOrWhiteSpace(vm.Project) ||
                string.IsNullOrWhiteSpace(vm.OcityJobTitle))
            {
                ViewBag.Error = "Please fill all required Ocity fields.";
                return View(vm);
            }
        }

        var client = _clientFactory.CreateClient("api");

        // Add token
        var token = HttpContext.Session.GetString("token");
        if (token != null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;

        if (vm.SystemName == "InvoiceQ")
        {
            var dto = new {
                Name = vm.Name,
                MobileNumber = vm.MobileNumber,
                Email = vm.Email,
                Department = vm.Department,
                JobTitle = vm.InvoiceQJobTitle, // map correctly
                Unit = vm.Unit,
                RequestDetails = vm.RequestDetails
            };
            response = await client.PostAsJsonAsync("api/requests/invoiceq", dto);
        }
        else
        {
            var dto = new {
                Name = vm.Name,
                MobileNumber = vm.MobileNumber,
                Email = vm.Email,
                Project = vm.Project,
                JobTitle = vm.OcityJobTitle, // map correctly
                RequestDetails = vm.RequestDetails
            };
            response = await client.PostAsJsonAsync("api/requests/ocity", dto);
        }

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Error submitting request";
            return View(vm);
        }

        TempData["success"] = "Request submitted successfully";
        return RedirectToAction("Create");
    }

  
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("token");

        if (token == null)
            return RedirectToAction("Login", "Account");

        // استخراج الـ employeeId من الـ JWT
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var employeeId = jwt.Claims.FirstOrDefault(c => c.Type == "employeeId")?.Value;

        if (string.IsNullOrEmpty(employeeId))
        {
            ViewBag.Error = "Unable to identify user";
            return View(new UserRequestsPageVm());
        }

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"api/requests/my-requests");

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Unable to load requests.";
            return View(new UserRequestsPageVm());
        }

        var result = await response.Content.ReadFromJsonAsync<UserRequestsPageVm>();
      

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> DetailsR(int id, string system)
    {
        var client = _clientFactory.CreateClient("api");

        var token = HttpContext.Session.GetString("token");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var result = await client.GetFromJsonAsync<UserRequestVm>($"api/requests/details?id={id}&system={system}");

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(int id, string system)
    {
        var client = _clientFactory.CreateClient("api");
        var token = HttpContext.Session.GetString("token");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var vm = await client.GetFromJsonAsync<UserRequestVm>($"api/requests/details?id={id}&system={system}");
        if (vm == null)
            return NotFound();


        return new ViewAsPdf("ExportPdfReport", vm)
        {
            FileName = $"Request_{vm.RequestNumber}.pdf",
            PageSize = Rotativa.AspNetCore.Options.Size.A4,
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 10, 15, 10)
        };

        // Create PDF





    }

    private static List<string> SplitText(string text, int maxChars)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text))
            return lines;

        var words = text.Split(' ');
        var sb = new StringBuilder();
        foreach (var w in words)
        {
            if (sb.Length + w.Length + 1 > maxChars)
            {
                lines.Add(sb.ToString());
                sb.Clear();
            }
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(w);
        }
        if (sb.Length > 0)
            lines.Add(sb.ToString());
        return lines;
    }

    [HttpGet]
    public async Task<IActionResult> GetRequestHistory(int requestId, string requestType)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Json(new { success = false, message = "Not authenticated" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"api/approval/history/{requestId}/{requestType}");

        if (!response.IsSuccessStatusCode)
            return Json(new { success = false, message = "Failed to get history" });

        var history = await response.Content.ReadFromJsonAsync<List<RequestActionHistoryDto>>();
        return Json(new { success = true, data = history });
    }

    [HttpGet]
    public async Task<IActionResult> GetRequestStage(int requestId, string requestType)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Json(new { success = false, message = "Not authenticated" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"api/approval/stage/{requestId}/{requestType}");

        if (!response.IsSuccessStatusCode)
            return Json(new { success = false, message = "Failed to get stage" });

        var stage = await response.Content.ReadFromJsonAsync<RequestStageDto>();
        return Json(new { success = true, data = stage });
    }
}
