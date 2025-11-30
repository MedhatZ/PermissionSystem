using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;
using PermissionSystemApi.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _auth;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthController(ApplicationDbContext context, AuthService auth, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _auth = auth;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("login-from-ad")]
    public IActionResult LoginFromAD([FromBody] LoginFromAdDto dto)
    {
        if (dto.User == null)
            return BadRequest("Missing user");

        // generate JWT - استخدم EmployeeId بدل Username
        var (token, success, error) = _auth.GenerateTokenFromAd(
            dto.User.DisplayName, // ✅ استخدم DisplayName بدل Username
            dto.User.EmployeeId,   // ✅ ده اللي بنستخدمه دلوقتي
            dto.User.Manager
        );

        if (!success)
        {
            return Unauthorized(new
            {
                message = error,
                code = "USER_NOT_REGISTERED"
            });
        }

        return Ok(new LoginResponseDto
        {
            Token = token,
            UserId = int.Parse(dto.User.EmployeeId),
            Username = dto.User.DisplayName  // ✅ استخدم DisplayName
        });
    }

    [HttpGet("check-user")]
    [Authorize]
    public IActionResult CheckUser()
    {
        var employeeId = User.FindFirst("employeeId")?.Value; // ✅ استخدم employeeId

        if (string.IsNullOrEmpty(employeeId))
            return Forbid();

        var exists = _context.Users.Any(u => u.EmployeeId.ToLower() == employeeId.ToLower());

        if (!exists)
            return Forbid();

        return Ok();
    }

    // --------------------------
    // AUTO REGISTER FROM AD
    // --------------------------
    [HttpPost("auto-register")]
    public async Task<IActionResult> AutoRegisterFromAD([FromBody] AdUser adUser)
    {
        if (adUser == null || string.IsNullOrEmpty(adUser.EmployeeId))
            return BadRequest("Invalid user data");

        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.EmployeeId == adUser.EmployeeId))
            return BadRequest("User already exists");

        var user = new User
        {
            EmployeeId = adUser.EmployeeId,
            FullName = adUser.DisplayName,
            Email = adUser.Email,
            Status = "Active",
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("User registered successfully");
    }

    // --------------------------
    // GET CURRENT USER INFO
    // --------------------------
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var employeeId = User.FindFirst("employeeId")?.Value;

        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.EmployeeId,
            user.FullName,
            user.Email,
            user.Status
        });
    }
}

// ============================================
// DTOs
// ============================================

public class LoginFromAdDto
{
    public AdUser User { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } // ده بيكون DisplayName من الـ AD
    public string Message { get; set; } // Optional for errors
    public bool Success { get; set; }
}

public class ErrorResponseDto
{
    public string Message { get; set; }
    public string Code { get; set; }
}