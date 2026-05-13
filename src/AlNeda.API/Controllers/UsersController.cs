using AlNeda.API.Authorization;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public UsersController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var users = await db.Users.OrderBy(u => u.Username).ToListAsync();
        var dto = users.Select(u => new AppUserDto
        {
            Id = u.Id, Username = u.Username, Role = u.Role,
            IsActive = u.IsActive, CreatedAt = u.CreatedAt
        }).ToList();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "اسم المستخدم مطلوب" });
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "كلمة المرور مطلوبة" });

        await using var db = await _contextFactory.CreateDbContextAsync();

        if (await db.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = $"المستخدم '{request.Username}' موجود مسبقاً" });

        var (hash, salt) = AuthService.HashPassword(request.Password);
        var user = new User
        {
            Username = request.Username.Trim(),
            Password = hash,
            PasswordSalt = salt,
            Role = request.Role ?? "rep",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "User", user.Id.ToString(), $"تم إنشاء المستخدم '{user.Username}'");

        return Ok(new AppUserDto { Id = user.Id, Username = user.Username, Role = user.Role, IsActive = user.IsActive, CreatedAt = user.CreatedAt });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "المستخدم غير موجود" });

        user.Username = request.Username.Trim();
        user.Role = request.Role ?? "rep";
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var (hash, salt) = AuthService.HashPassword(request.Password);
            user.Password = hash;
            user.PasswordSalt = salt;
        }

        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "update", "User", id.ToString(), $"تم تحديث المستخدم '{user.Username}'");

        return Ok(new AppUserDto { Id = user.Id, Username = user.Username, Role = user.Role, IsActive = user.IsActive, CreatedAt = user.CreatedAt });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == 1) return BadRequest(new { message = "لا يمكن حذف مستخدم admin الافتراضي" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "المستخدم غير موجود" });

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "delete", "User", id.ToString(), $"تم حذف المستخدم '{user.Username}'");

        return Ok(new { message = "تم حذف المستخدم بنجاح" });
    }
}
