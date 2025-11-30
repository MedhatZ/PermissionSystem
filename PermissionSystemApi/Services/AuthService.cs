using Microsoft.IdentityModel.Tokens;
using PermissionSystemApi.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // إزالة دوال CreatePassword و VerifyPassword لأننا بنستخدم AD
    public (string token, bool success, string error) GenerateTokenFromAd(string username,string employeeId, string managerId)
    {
        try
        {
            // check if user exists in DB باستخدام EmployeeId
            var dbUser = _context.Users.FirstOrDefault(u => u.EmployeeId == employeeId);

            string dbUserId;
            string role;

            if (dbUser == null)
            {
                // ============== NEW LOGIC HERE ===============
                // مستخدم AD وليس موجود في DB → User محدود
                dbUserId = "30";   // static guest ID
                role = "User";
            }
            else
            {
                dbUserId = dbUser.Id.ToString();
                role =  _context.ManagerAssignments.FirstOrDefault(u => u.UserId == int.Parse(dbUserId)).RoleType;

            }

            var claims = new List<Claim>
        {
            new Claim("username", username ?? ""),
            new Claim("employeeId", employeeId ?? ""),
            new Claim("managerId", managerId ?? ""),
            new Claim("exists", "true"),
            new Claim("dbUserId", dbUserId ?? ""),
            new Claim("role", role ?? "")

        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "",
                audience: _config["Jwt:Audience"] ?? "",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), true, null);
        }
        catch (Exception ex)
        {
            // ✅ handle any unexpected errors
            return (null, false, "حدث خطأ غير متوقع أثناء إنشاء التوكن");
        }
    }

}