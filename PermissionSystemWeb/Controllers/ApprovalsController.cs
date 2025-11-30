using Microsoft.AspNetCore.Mvc;
 
 
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

public class ApprovalsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
 
    public ApprovalsController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    // ===============================
    // VIEW
    // ===============================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
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

        // 3) If user does NOT exist in DB → redirect to Requests/Index
        if (role == "User" || string.IsNullOrEmpty(dbUserId))
        {
            return RedirectToAction("Index", "Requests");
        }

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // استخدام الـ endpoints الجديدة
        var url1 = $"api/requests/pending/direct?userId={dbUserId}";
        var url2 = $"api/requests/pending/final?userId={dbUserId}";
        var url3 = $"api/requests/pending/Implementor?userId={dbUserId}";


        var direct = await client.GetFromJsonAsync<List<RequestItem>>(url1) ?? new();
        var final = await client.GetFromJsonAsync<List<RequestItem>>(url2) ?? new();
        var Implementor = await client.GetFromJsonAsync<List<RequestItem>>(url3) ?? new();


        var vm = new ApprovalsVm
        {
            DirectPending = direct,
            FinalPending = final,
            ImplementPending = Implementor   
        };
         
      //  var role = await client.GetStringAsync($"api/Approval/get-role?userId={dbUserId}");
        ViewBag.Role = role?.Replace("\"", "").Trim();
        ViewData["Role"] = role?.Replace("\"", "").Trim();
      

        return View(vm);
    }

    // ===============================
    // APPROVE FIRST LEVEL
    // ===============================
   
    [HttpPost]
    public async Task<IActionResult> ApproveFirst([FromBody] ApproveRequestDto model)
    {
        
        int requestId = model.RequestId;
        string requestType = model.RequestType;
        string comments = model.Comments;

        var token = HttpContext.Session.GetString("token");
        var dbUserId = HttpContext.Session.GetString("dbUserId");

        if (string.IsNullOrEmpty(dbUserId))
            return Json(new { success = false, message = "User not found" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        model.ApproverId = int.Parse(dbUserId);
        //var payload = new
        //{
        //    RequestId = requestId,
        //    RequestType = requestType, // "InvoiceQ" or "Ocity"
        //    ApproverId = int.Parse(dbUserId),
        //    Comments = comments
        //};

        var response = await client.PutAsJsonAsync("api/Approval/approve-first", model);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return Json(new { success = false, message = error });
        }

        return Json(new { success = true });
    }

    // ===============================
    // APPROVE SECOND LEVEL
    // ===============================
    [HttpPost]
    public async Task<IActionResult> ApproveSecond([FromBody] ApproveRequestDto model)
    {
        var token = HttpContext.Session.GetString("token");
        var dbUserId = HttpContext.Session.GetString("dbUserId");

        if (string.IsNullOrEmpty(dbUserId))
            return Json(new { success = false, message = "User not found" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        model.ApproverId = int.Parse(dbUserId);
        //var payload = new
        //{
        //    RequestId = requestId,
        //    RequestType = requestType, // "InvoiceQ" or "Ocity"
        //    ApproverId = int.Parse(dbUserId),
        //    Comments = comments
        //};

        var response = await client.PutAsJsonAsync("api/Approval/approve-second", model);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return Json(new { success = false, message = error });
        }

        return Json(new { success = true });
    }

    // ===============================
    // Implementor
    // ===============================
    [HttpPost]
    public async Task<IActionResult> Implementor([FromBody] ApproveRequestDto model)
    {
        var token = HttpContext.Session.GetString("token");
        var dbUserId = HttpContext.Session.GetString("dbUserId");

        if (string.IsNullOrEmpty(dbUserId))
            return Json(new { success = false, message = "User not found" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        model.ApproverId = int.Parse(dbUserId);
        //var payload = new
        //{
        //    RequestId = requestId,
        //    RequestType = requestType, // "InvoiceQ" or "Ocity"
        //    ApproverId = int.Parse(dbUserId),
        //    Comments = comments
        //};

        var response = await client.PutAsJsonAsync("api/Approval/Implementor", model);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return Json(new { success = false, message = error });
        }

        return Json(new { success = true });
    }

    // ===============================
    // REJECT
    // ===============================
    [HttpPost]
    public async Task<IActionResult> Reject(int requestId, string requestType, string reason, string additionalComments = "")
    {
        var token = HttpContext.Session.GetString("token");
        var dbUserId = HttpContext.Session.GetString("dbUserId");

        if (string.IsNullOrEmpty(dbUserId))
            return Json(new { success = false, message = "User not found" });

        var client = _clientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            RequestId = requestId,
            RequestType = requestType, // "InvoiceQ" or "Ocity"
            ApproverId = int.Parse(dbUserId),
            Reason = reason,
            AdditionalComments = additionalComments
        };

        var response = await client.PutAsJsonAsync("api/approval/reject", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return Json(new { success = false, message = error });
        }

        return Json(new { success = true });
    }

    // ===============================
    // GET REQUEST HISTORY
    // ===============================
    [HttpGet]
    public async Task<IActionResult> GetHistory(int requestId, string requestType)
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

    // ===============================
    // GET REQUEST CURRENT STAGE
    // ===============================
    [HttpGet]
    public async Task<IActionResult> GetCurrentStage(int requestId, string requestType)
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


    [HttpGet]
    public async Task<IActionResult> Details(int id, string system)
    {
        var client = _clientFactory.CreateClient("api");

        var token = HttpContext.Session.GetString("token");
        var dbUserId = HttpContext.Session.GetString("dbUserId");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var result = await client.GetFromJsonAsync<UserRequestVm>($"api/requests/details?id={id}&system={system}");


        var role = await client.GetStringAsync($"api/Approval/get-role?userId={dbUserId}");
        ViewBag.Role = role;


        ViewBag.SystemName = system;

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
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
        // 3) If user does NOT exist in DB → redirect to Requests/Index
        if (role == "User" || string.IsNullOrEmpty(dbUserId))
        {
            return RedirectToAction("Index", "Requests");
        }

      //  var dbUserId = HttpContext.Session.GetString("dbUserId");
        //var username = HttpContext.Session.GetString("employeeId");
        //if (string.IsNullOrEmpty(username))
        //    return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("api");

        //var token = HttpContext.Session.GetString("token");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var result = await client.GetFromJsonAsync<List<UserRequestVm>>(
            $"api/Approval/manager-history?username={dbUserId}"
        );

        return View(result ?? new List<UserRequestVm>());
    }


}

