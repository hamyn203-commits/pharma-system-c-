using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public AuthController(
        AuthService authService,
        IConfiguration configuration,
        IDbContextFactory<Data.AppDbContext> contextFactory,
        IAuditService audit)
    {
        _authService = authService;
        _configuration = configuration;
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(Error("اسم المستخدم وكلمة المرور مطلوبان", "VALIDATION_ERROR"));

        var user = await _authService.LoginAsync(request.Username, request.Password);
        if (user == null)
            return Unauthorized(Error("اسم المستخدم أو كلمة المرور غير صحيحة", "AUTH_REQUIRED"));

        if (user.Role == "pharmacy" && user.PharmacyId == null)
            return StatusCode(StatusCodes.Status403Forbidden, Error("حساب الصيدلية غير مربوط بصيدلية", "PHARMACY_NOT_LINKED"));

        if (user.Role == "pharmacy")
        {
            var status = user.Pharmacy?.AccountStatus?.ToLowerInvariant();
            if (status == "pending" || !user.IsActive)
                return StatusCode(StatusCodes.Status403Forbidden, Error("حسابك في انتظار موافقة إدارة المخزن", "ACCOUNT_PENDING"));
            if (status == "blocked")
                return StatusCode(StatusCodes.Status403Forbidden, Error("تم إيقاف حساب الصيدلية من إدارة المخزن", "ACCOUNT_BLOCKED"));
        }
        else if (!user.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, Error("تم إيقاف الحساب", "ACCOUNT_DISABLED"));
        }

        var token = GenerateJwtToken(user);
        var response = new LoginResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            PharmacyId = user.PharmacyId,
            PharmacyName = user.Pharmacy?.Name,
            Token = token,
            User = new LoginUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                PharmacyId = user.PharmacyId,
                PharmacyName = user.Pharmacy?.Name
            }
        };

        return Ok(response);
    }

    [HttpPost("register")]
    [HttpPost("register/pharmacy")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(PharmacyRegisterResponseDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RegisterPharmacy([FromBody] PharmacyRegisterRequest request)
    {
        var pharmacyName = request.PharmacyName.Trim();
        var username = request.Username.Trim();
        var phone = request.Phone.Trim();

        if (string.IsNullOrWhiteSpace(pharmacyName))
            return BadRequest(Error("اسم الصيدلية مطلوب", "VALIDATION_ERROR"));
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(Error("اسم المستخدم مطلوب", "VALIDATION_ERROR"));
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(Error("كلمة المرور مطلوبة", "VALIDATION_ERROR"));
        if (request.Password.Length < 8)
            return BadRequest(Error("كلمة المرور يجب ألا تقل عن 8 أحرف", "VALIDATION_ERROR"));
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(Error("رقم الهاتف مطلوب", "VALIDATION_ERROR"));

        await using var db = await _contextFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            return Conflict(Error("اسم المستخدم موجود مسبقا", "USERNAME_EXISTS"));
        if (await db.Pharmacies.AnyAsync(p => p.Phone == phone))
            return Conflict(Error("رقم الهاتف مسجل لصيدلية أخرى", "PHONE_EXISTS"));

        var pharmacy = new Pharmacy
        {
            Name = pharmacyName,
            Phone = phone,
            Address = request.Address?.Trim() ?? string.Empty,
            AccountStatus = "pending",
            DeviceId = request.DeviceId?.Trim(),
            CreatedAt = DateTime.Now
        };

        var (hash, salt) = AuthService.HashPassword(request.Password);
        var user = new User
        {
            Username = username,
            Password = hash,
            PasswordSalt = salt,
            Role = "pharmacy",
            Pharmacy = pharmacy,
            IsActive = false,
            CreatedAt = DateTime.Now
        };

        db.Pharmacies.Add(pharmacy);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await _audit.LogAsync("mobile-registration", "register", "Pharmacy", pharmacy.Id.ToString(),
            $"طلب إنشاء حساب تطبيق جديد للصيدلية '{pharmacy.Name}' برقم '{pharmacy.Phone}'");

        return Accepted(new PharmacyRegisterResponseDto
        {
            PharmacyId = pharmacy.Id,
            UserId = user.Id,
            Username = user.Username,
            PharmacyName = pharmacy.Name,
            AccountStatus = pharmacy.AccountStatus,
            RequiresApproval = true,
            Message = "تم إنشاء الحساب بنجاح وهو الآن في انتظار موافقة إدارة المخزن"
        });
    }

    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> ValidateToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var pharmacyId = User.FindFirst("pharmacyId")?.Value;

        if (userId == null || !int.TryParse(userId, out var id))
            return Unauthorized(Error("الرمز غير صالح", "AUTH_REQUIRED"));


        await using var db = await _contextFactory.CreateDbContextAsync();
        var user = await db.Users.Include(u => u.Pharmacy).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null || !user.IsActive)
            return Unauthorized(Error("انتهت صلاحية الجلسة أو تم إيقاف الحساب", "AUTH_REQUIRED"));

        if (user.Role == "pharmacy" && user.Pharmacy?.AccountStatus != "active")
            return StatusCode(StatusCodes.Status403Forbidden, Error("حساب الصيدلية غير نشط حاليا", "ACCOUNT_DISABLED"));
        return Ok(new
        {
            valid = true,
            user = new { id = userId, username, role, pharmacyId }
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("ALNEDA_JWT_KEY") ?? jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT Key is missing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.PharmacyId.HasValue)
        {
            claims.Add(new Claim("pharmacyId", user.PharmacyId.Value.ToString()));
            claims.Add(new Claim("PharmacyId", user.PharmacyId.Value.ToString()));
        }

        var issuer = Environment.GetEnvironmentVariable("ALNEDA_JWT_ISSUER") ?? jwtSection["Issuer"];
        var audience = Environment.GetEnvironmentVariable("ALNEDA_JWT_AUDIENCE") ?? jwtSection["Audience"];
        var expireMinutes = int.TryParse(
            Environment.GetEnvironmentVariable("ALNEDA_JWT_EXPIRE_MINUTES") ?? jwtSection["ExpireMinutes"],
            out var min) ? min : 1440;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static ApiError Error(string message, string code) => new()
    {
        Message = message,
        Code = code
    };
}
