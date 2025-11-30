using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PermissionSystemWeb.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<AccountController> _logger;


    public AccountController(IHttpClientFactory clientFactory, ILogger<AccountController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;

    }

    [HttpGet]
    public IActionResult Login()
    {
        return RedirectToAction("LoginSSO");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
    [HttpGet]
    public async Task<IActionResult> LoginSSO()
    {
        try
        {
            _logger.LogInformation("🔐 Starting LoginSSO");

            // ==================== External User Login via URL ====================
            var qUser = Request.Query["u"].ToString();
            var qPass = Request.Query["pw"].ToString();

            AdUser adUser = null;
            AdUser managerUser = null;

            if (!string.IsNullOrWhiteSpace(qUser) && !string.IsNullOrWhiteSpace(qPass))
            {
                // ✨ اضف اكتر من مستخدم خارجي هنا حسب رغبتك
                if (qUser.ToLower() == "external" && qPass == "1234")
                {
                    adUser = new AdUser
                    {
                        SamAccountName = "external",
                        DisplayName = "OsamaAlmahamed",
                        Email = "oalmahamed@invoiceq.com",
                        Title = "Consultant",
                        Department = "External",
                        EmployeeId = "999999",
                        Manager = "STATIC"
                    };
                   
                    goto SKIP_LDAP;
                }

                // ❌ If wrong credentials
                _logger.LogWarning("❌ External login failed with u={QUser}", qUser);
                return RedirectToAction("AccessDenied", "Account");
            }

            // ==================== Domain (SSO) Login ====================
            var full = HttpContext.User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(full) || !full.Contains("\\"))
            {
                _logger.LogWarning("⚠ No valid domain user detected for SSO");
                return RedirectToAction("AccessDenied", "Account");
            }

            var username = full.Split('\\')[1];
            _logger.LogInformation("Detected domain user: {Username}", username);

            // Override for dev accounts
            if (username == "devnetadmin08" || username == "alim")
                username = "baqirsm";

            // LDAP Configuration
            var cfg = new LdapCfg(
                "SAPTCO.LOCAL",
                "SAPTCO.LOCAL",
                "",
                "Srv_Adempupdate_Dev",
                "Kvs8$$VWdkzS@67",
                false
            );

            bool ok;
            string err;

            // Lookup user from AD
            (ok, adUser, err) = LdapHelper.FindUser(cfg, "sAMAccountName", username);

            if (!ok || adUser == null)
            {
                _logger.LogWarning("❌ LDAP user not found: {Username}", username);
                return RedirectToAction("AccessDenied", "Account");
            }

            // Try to find manager
            if (!string.IsNullOrWhiteSpace(adUser.Manager) && adUser.Manager != "STATIC")
            {
                var (okMgr, mUser, errMgr) = LdapHelper.FindUser(cfg, "distinguishedName", adUser.Manager);
                if (okMgr && mUser != null)
                    managerUser = mUser;
            }

        // ==================== Skip point for External + SSO ====================
        SKIP_LDAP:
            _logger.LogInformation("✅ LDAP user found: {Username}", adUser.SamAccountName);
            // Prepare HTTP Client and payload
            var client = _clientFactory.CreateClient("api");

            var payload = new
            {
                User = new
                {
                    Username = adUser.SamAccountName,
                    DisplayName = adUser.DisplayName,
                    Email = adUser.Email,
                    Title = adUser.Title,
                    Department = adUser.Department,
                    EmployeeID = adUser.EmployeeId,
                    Manager = managerUser?.EmployeeId,
                    IsGM = adUser.Title?.Contains("GM") == true
                }
            };

            var response = await client.PostAsJsonAsync("api/Auth/login-from-ad", payload);
            _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            // Save session
            HttpContext.Session.SetString("token", result.Token);
            HttpContext.Session.SetString("username", result.Username);
            HttpContext.Session.SetString("fullName", adUser.DisplayName);
            HttpContext.Session.SetString("email", adUser.Email);
            HttpContext.Session.SetString("employeeId", adUser.EmployeeId);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Token);

            var exists = jwt.Claims.FirstOrDefault(c => c.Type == "exists")?.Value;
            var employeeId = jwt.Claims.FirstOrDefault(c => c.Type == "employeeId")?.Value;
            var dbUserId = jwt.Claims.FirstOrDefault(c => c.Type == "dbUserId")?.Value;

            HttpContext.Session.SetString("employeeId", employeeId ?? adUser.EmployeeId);
            HttpContext.Session.SetString("dbUserId", dbUserId ?? "");

            if (exists == "false")
                return RedirectToAction("AccessDenied", "Account");

            _logger.LogInformation("🎉 User {Username} logged in successfully", adUser.SamAccountName);
            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled LoginSSO Exception");
            return RedirectToAction("AccessDenied", "Account");
        }
    }

    public static async Task<string> GetUserNameById(string employeeId)
    {
        // Note: Can't use instance logger in static method. Consider making non-static if logging needed.

        var cfg = new LdapCfg(
                  "SAPTCO.LOCAL",
                  "SAPTCO.LOCAL",
                  "",
                  "Srv_Adempupdate_Dev",
                  "Kvs8$$VWdkzS@67",
                  false
              );

        string fullName;
        var (ok, adUser, err) = LdapHelper.FindUser(cfg, "employeeID", employeeId);

        if (!ok || adUser == null)
            return null;

        fullName = adUser.DisplayName;

        return fullName;
    }
}

