using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PermissionSystemApi.Data;
using PermissionSystemApi.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// Logging
// ==============================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ==============================
// Services (Controllers + Swagger)
// ==============================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==============================
// Database (SQL Server)
// ==============================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==============================
// Authentication (JWT ONLY)
// ==============================
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new Exception("JWT Key is missing in configuration!");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

// ==============================
// Authorization
// ==============================
builder.Services.AddAuthorization();

// ==============================
// CORS
// ==============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ==============================
// Dependency Injection (Services)
// ==============================
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<RequestHistoryService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();

// ==============================
// Build App
// ==============================
var app = builder.Build();

// ==============================
// Swagger (Always enabled)
// ==============================
app.UseSwagger();
app.UseSwaggerUI();

// ==============================
// Middleware Pipeline
// ==============================
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ==============================
// Map Controllers
// ==============================
app.MapControllers();

// ==============================
// Run Application
// ==============================
app.Run();
