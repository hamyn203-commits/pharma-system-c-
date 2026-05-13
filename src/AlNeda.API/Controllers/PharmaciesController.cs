using AlNeda.API.Authorization;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/pharmacies")]
[Authorize(Policy = AuthPolicies.Staff)]
public class PharmaciesController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public PharmaciesController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Pharmacies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.Phone.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(p => p.AccountStatus == status);
        }

        var rows = await q.OrderBy(p => p.Name)
            .Select(p => new
            {
                Pharmacy = p,
                AppUser = db.Users.Where(u => u.Role == "pharmacy" && u.PharmacyId == p.Id).OrderBy(u => u.Id).FirstOrDefault(),
                AppOrdersCount = db.Orders.Count(o => o.PharmacyId == p.Id && o.Source == "mobile")
            })
            .ToListAsync();

        return Ok(rows.Select(x => ToDto(x.Pharmacy, x.AppUser, x.AppOrdersCount)).ToList());
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var total = await db.Pharmacies.CountAsync();
        var pending = await db.Pharmacies.CountAsync(p => p.AccountStatus == "pending");
        var active = await db.Pharmacies.CountAsync(p => p.AccountStatus == "active");
        return Ok(new { total, pending, active });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_LINKED" });
        var appUser = await db.Users.Where(u => u.Role == "pharmacy" && u.PharmacyId == p.Id).OrderBy(u => u.Id).FirstOrDefaultAsync();
        var appOrdersCount = await db.Orders.CountAsync(o => o.PharmacyId == p.Id && o.Source == "mobile");
        return Ok(ToDto(p, appUser, appOrdersCount));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePharmacyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ApiError { Message = "اسم الصيدلية مطلوب", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();

        // Validate optional app account credentials
        var createAppAccount = !string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password);
        if (createAppAccount)
        {
            if (request.Password.Length < 8)
                return BadRequest(new ApiError { Message = "كلمة المرور يجب ألا تقل عن 8 أحرف", Code = "VALIDATION_ERROR" });
            if (await db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.Trim().ToLower()))
                return Conflict(new ApiError { Message = "اسم المستخدم موجود مسبقاً", Code = "USERNAME_EXISTS" });
        }

        var pharmacy = new Pharmacy
        {
            Name = request.Name.Trim(),
            Address = request.Address ?? string.Empty,
            Phone = request.Phone ?? string.Empty,
            OwnerName = request.OwnerName?.Trim(),
            Balance = request.Balance,
            AccountStatus = request.AccountStatus ?? "active",
            CreatedAt = DateTime.Now,
            ApprovedAt = string.Equals(request.AccountStatus, "active", StringComparison.OrdinalIgnoreCase) ? DateTime.Now : null
        };
        db.Pharmacies.Add(pharmacy);
        await db.SaveChangesAsync();

        // Auto-create app account when admin provides credentials
        User? appUser = null;
        if (createAppAccount)
        {
            var (hash, salt) = AuthService.HashPassword(request.Password!);
            appUser = new User
            {
                Username = request.Username!.Trim(),
                Password = hash,
                PasswordSalt = salt,
                Role = "pharmacy",
                PharmacyId = pharmacy.Id,
                IsActive = pharmacy.AccountStatus == "active",
                CreatedAt = DateTime.Now
            };
            db.Users.Add(appUser);
            await db.SaveChangesAsync();
        }

        var actor = User.Identity?.Name ?? "system";
        await _audit.LogAsync(actor, "create", "Pharmacy", pharmacy.Id.ToString(),
            $"تم إنشاء الصيدلية '{pharmacy.Name}'{(appUser != null ? " مع حساب تطبيق" : "")}");

        return Ok(ToDto(pharmacy, appUser, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePharmacyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ApiError { Message = "اسم الصيدلية مطلوب", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_LINKED" });

        p.Name = request.Name.Trim();
        p.Address = request.Address ?? string.Empty;
        p.Phone = request.Phone ?? string.Empty;
        p.OwnerName = request.OwnerName?.Trim();
        p.Balance = request.Balance;
        p.AccountStatus = request.AccountStatus ?? "active";
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "update", "Pharmacy", id.ToString(),
            $"تم تحديث الصيدلية '{p.Name}'");

        var appUser = await db.Users.Where(u => u.Role == "pharmacy" && u.PharmacyId == p.Id).OrderBy(u => u.Id).FirstOrDefaultAsync();
        var appOrdersCount = await db.Orders.CountAsync(o => o.PharmacyId == p.Id && o.Source == "mobile");
        return Ok(ToDto(p, appUser, appOrdersCount));
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] PharmacyStatusRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_LINKED" });

        p.AccountStatus = request.Status;
        if (request.Status == "active") p.ApprovedAt = DateTime.Now;
        if (request.Status == "blocked") p.BlockedAt = DateTime.Now;

        var appUser = await db.Users.FirstOrDefaultAsync(u => u.Role == "pharmacy" && u.PharmacyId == id);
        if (appUser != null)
        {
            appUser.IsActive = request.Status == "active";
        }

        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "update", "Pharmacy", id.ToString(),
            $"تم تغيير حالة الصيدلية '{p.Name}' إلى {request.Status}");

        return Ok(new { message = "تم تحديث الحالة بنجاح" });
    }

    [HttpPost("{id:int}/app-account")]
    public async Task<IActionResult> CreateAppAccount(int id, [FromBody] CreatePharmacyAppAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new ApiError { Message = "اسم المستخدم مطلوب", Code = "VALIDATION_ERROR" });
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiError { Message = "كلمة المرور مطلوبة", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(id);
        if (pharmacy == null)
            return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_LINKED" });
        if (await db.Users.AnyAsync(u => u.Username == request.Username.Trim()))
            return Conflict(new ApiError { Message = "اسم المستخدم موجود مسبقاً", Code = "VALIDATION_ERROR" });
        if (await db.Users.AnyAsync(u => u.Role == "pharmacy" && u.PharmacyId == id))
            return Conflict(new ApiError { Message = "الصيدلية لديها حساب تطبيق بالفعل", Code = "VALIDATION_ERROR" });

        var (hash, salt) = AuthService.HashPassword(request.Password);
        var user = new User
        {
            Username = request.Username.Trim(),
            Password = hash,
            PasswordSalt = salt,
            Role = "pharmacy",
            PharmacyId = id,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "create", "User", user.Id.ToString(),
            $"تم إنشاء حساب تطبيق للصيدلية '{pharmacy.Name}'");

        return Ok(new AppUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            PharmacyId = user.PharmacyId,
            PharmacyName = pharmacy.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id:int}/app-account/status")]
    public async Task<IActionResult> SetAppAccountStatus(int id, [FromBody] SetPharmacyAppAccountStatusRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(id);
        if (pharmacy == null)
            return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_LINKED" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Role == "pharmacy" && u.PharmacyId == id);
        if (user == null)
            return NotFound(new ApiError { Message = "حساب التطبيق غير موجود", Code = "PHARMACY_NOT_LINKED" });

        user.IsActive = request.IsActive;
        if (request.IsActive && pharmacy.AccountStatus != "active")
        {
            pharmacy.AccountStatus = "active";
            pharmacy.ApprovedAt = DateTime.Now;
            pharmacy.BlockedAt = null;
        }

        await db.SaveChangesAsync();
        await _audit.LogAsync(User.Identity?.Name ?? "system", "update", "User", user.Id.ToString(),
            $"تم {(request.IsActive ? "تفعيل" : "تعطيل")} حساب تطبيق الصيدلية");

        return Ok(new { user.Id, user.Username, user.IsActive, pharmacy.AccountStatus });
    }

    [HttpPut("{id:int}/app-account/password")]
    public async Task<IActionResult> ResetAppAccountPassword(int id, [FromBody] ResetPharmacyAppPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiError { Message = "كلمة المرور مطلوبة", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Role == "pharmacy" && u.PharmacyId == id);
        if (user == null)
            return NotFound(new ApiError { Message = "حساب التطبيق غير موجود", Code = "PHARMACY_NOT_LINKED" });

        var (hash, salt) = AuthService.HashPassword(request.Password);
        user.Password = hash;
        user.PasswordSalt = salt;
        await db.SaveChangesAsync();
        await _audit.LogAsync(User.Identity?.Name ?? "system", "password_reset", "User", user.Id.ToString(),
            "تم إعادة تعيين كلمة مرور حساب تطبيق الصيدلية");

        return Ok(new { message = "تم إعادة تعيين كلمة المرور" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_LINKED" });

        db.Pharmacies.Remove(p);
        await db.SaveChangesAsync();
        await _audit.LogAsync(User.Identity?.Name ?? "system", "delete", "Pharmacy", id.ToString(),
            $"تم حذف الصيدلية '{p.Name}'");

        return Ok(new { message = "تم حذف الصيدلية بنجاح" });
    }

    private static PharmacyDto ToDto(Pharmacy p, User? appUser, int appOrdersCount) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Address = p.Address,
        Phone = p.Phone,
        Balance = p.Balance,
        AccountStatus = p.AccountStatus,
        OwnerName = p.OwnerName,
        HasAppAccount = appUser != null,
        IsAppAccountActive = appUser?.IsActive ?? false,
        AppUsername = appUser?.Username,
        AppLastLoginAt = appUser?.LastLoginAt ?? p.LastLoginAt,
        AppOrdersCount = appOrdersCount,
        CreatedAt = p.CreatedAt
    };
}
