using System.Security.Claims;
using AlNeda.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Middleware;

public class MobileAccountStatusMiddleware
{
    private readonly RequestDelegate _next;

    public MobileAccountStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        if (!context.Request.Path.StartsWithSegments("/api/mobile"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var role = context.User.FindFirst(ClaimTypes.Role)?.Value
            ?? context.User.FindFirst("role")?.Value;
        if (!string.Equals(role, "pharmacy", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var userIdValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdValue, out var userId))
        {
            await WriteForbiddenAsync(context, "جلسة غير صالحة، سجل الدخول مرة أخرى.", "AUTH_REQUIRED");
            return;
        }

        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users.Include(u => u.Pharmacy).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            await WriteForbiddenAsync(context, "جلسة غير صالحة، سجل الدخول مرة أخرى.", "AUTH_REQUIRED");
            return;
        }

        if (!user.IsActive)
        {
            var status = user.Pharmacy?.AccountStatus?.ToLowerInvariant();
            var isPending = status == "pending";
            await WriteForbiddenAsync(
                context,
                isPending
                    ? "جاري مراجعة حسابك من إدارة المخزن. سيتم فتح التطبيق بعد الاعتماد."
                    : "تم سحب صلاحية حساب الصيدلية من إدارة المخزن.",
                isPending ? "ACCOUNT_PENDING" : "ACCOUNT_BLOCKED");
            return;
        }

        if (user.Pharmacy == null)
        {
            await WriteForbiddenAsync(context, "حساب الصيدلية غير مربوط بصيدلية.", "PHARMACY_NOT_LINKED");
            return;
        }

        var accountStatus = user.Pharmacy.AccountStatus?.ToLowerInvariant();
        if (accountStatus == "pending")
        {
            await WriteForbiddenAsync(context, "جاري مراجعة حسابك من إدارة المخزن. سيتم فتح التطبيق بعد الاعتماد.", "ACCOUNT_PENDING");
            return;
        }

        if (accountStatus == "blocked" || accountStatus != "active")
        {
            await WriteForbiddenAsync(context, "تم سحب صلاحية حساب الصيدلية من إدارة المخزن.", "ACCOUNT_BLOCKED");
            return;
        }

        await _next(context);
    }

    private static async Task WriteForbiddenAsync(HttpContext context, string message, string code)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ApiError
        {
            Message = message,
            Code = code
        });
    }
}
