using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ErrorMonitor.API.Controllers;

/// <summary>
/// Controller لإدارة المصادقة وإصدار JWT Tokens
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration, ILogger<AuthController> logger) : ControllerBase
{
    // ─────────────────────────────────────────────────────────────
    // POST /api/auth/login
    // يتحقق من بيانات المستخدم ويُرجع JWT Token
    // ─────────────────────────────────────────────────────────────
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // في المشاريع الحقيقية: يتحقق من قاعدة البيانات
        // هنا نستخدم مستخدمين وهميين للـ Demo
        var demoUsers = new Dictionary<string, (string Password, string Role, string Id)>
        {
            ["admin@demo.com"] = ("Admin@123", "Admin", "usr_001"),
            ["user@demo.com"]  = ("User@123",  "User",  "usr_002"),
        };

        if (!demoUsers.TryGetValue(request.Email, out var user) || user.Password != request.Password)
        {
            logger.LogWarning("❌ Failed login attempt for email: {Email}", request.Email);
            return Unauthorized(new { message = "البريد الإلكتروني أو كلمة المرور غير صحيحة." });
        }

        var token = GenerateJwtToken(user.Id, request.Email, user.Role);

        logger.LogInformation("✅ User {Email} logged in successfully | Role: {Role}", request.Email, user.Role);

        return Ok(new
        {
            Token     = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetExpiryMinutes()),
            User      = new { Id = user.Id, Email = request.Email, Role = user.Role }
        });
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/auth/me — [Authorize]
    // يُرجع بيانات المستخدم المصادق عليه من التوكن
    // ─────────────────────────────────────────────────────────────
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId    = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var userRole  = User.FindFirstValue(ClaimTypes.Role);

        logger.LogInformation("👤 Profile accessed by UserId: {UserId}", userId);

        return Ok(new
        {
            UserId = userId,
            Email  = userEmail,
            Role   = userRole,
            Message = "تم استخراج بياناتك من الـ JWT Token بنجاح!"
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Helper — توليد JWT Token
    // ─────────────────────────────────────────────────────────────
    private string GenerateJwtToken(string userId, string email, string role)
    {
        var secretKey = configuration["JwtSettings:SecretKey"]!;
        var issuer    = configuration["JwtSettings:Issuer"]!;
        var audience  = configuration["JwtSettings:Audience"]!;

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email,           email),
            new Claim(ClaimTypes.Role,            role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(GetExpiryMinutes()),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetExpiryMinutes() =>
        int.TryParse(configuration["JwtSettings:ExpiryMinutes"], out var m) ? m : 60;
}

public record LoginRequest(string Email, string Password);
