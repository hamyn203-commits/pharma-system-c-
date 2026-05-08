using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.DomainLogic;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/returns")]
[Authorize]
public class ReturnsController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public ReturnsController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var list = await db.Returns.Include(r => r.Pharmacy).Include(r => r.Items).ThenInclude(i => i.Product)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        var dto = list.Select(r => new ReturnDto
        {
            Id = r.Id, ReturnNumber = r.ReturnNumber, PharmacyId = r.PharmacyId,
            PharmacyName = r.Pharmacy?.Name, OrderId = r.OrderId, TotalAmount = r.TotalAmount,
            Status = r.Status, ReturnType = r.ReturnType, Reason = r.Reason,
            Notes = r.Notes, StockAdjusted = r.StockAdjusted, BalanceAdjusted = r.BalanceAdjusted,
            BalanceBefore = r.BalanceBefore, BalanceAfter = r.BalanceAfter, CreatedAt = r.CreatedAt,
            Items = r.Items.Select(i => new ReturnItemDto
            {
                Id = i.Id, ProductId = i.ProductId, ProductName = i.Product?.Name,
                Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalPrice = i.Quantity * i.UnitPrice
            }).ToList()
        }).ToList();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequest request)
    {
        if (request.PharmacyId == 0)
            return BadRequest(new { message = "اختر الصيدلية" });
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "أضف منتجات على الأقل" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(request.PharmacyId);
        if (pharmacy == null) return NotFound(new { message = "الصيدلية غير موجودة" });

        var ret = new Return
        {
            ReturnNumber = $"RET-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            PharmacyId = request.PharmacyId,
            OrderId = request.OrderId,
            TotalAmount = request.TotalAmount,
            Status = "completed",
            ReturnType = request.ReturnType,
            Reason = request.Reason,
            Notes = request.Notes,
            StockAdjusted = request.StockAdjusted,
            BalanceAdjusted = request.BalanceAdjusted,
            BalanceBefore = pharmacy.Balance,
            BalanceAfter = request.BalanceAdjusted
                ? OrderWorkflow.updateBalance(pharmacy.Balance, request.TotalAmount, "return")
                : pharmacy.Balance,
            CreatedAt = DateTime.Now
        };
        db.Returns.Add(ret);
        await db.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            db.ReturnItems.Add(new ReturnItem
            {
                ReturnId = ret.Id, ProductId = item.ProductId,
                Quantity = item.Quantity, UnitPrice = item.UnitPrice
            });
            if (request.StockAdjusted)
            {
                var prod = await db.Products.FindAsync(item.ProductId);
                if (prod != null) prod.Quantity += item.Quantity;
            }
        }

        if (request.BalanceAdjusted)
            pharmacy.Balance = ret.BalanceAfter;

        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "Return", ret.Id.ToString(),
            $"مرتجع #{ret.ReturnNumber} للصيدلية '{pharmacy.Name}' بقيمة {ret.TotalAmount}" +
            (request.StockAdjusted ? " (تم تعديل المخزون)" : "") +
            (request.BalanceAdjusted ? " (تم تعديل الرصيد)" : ""));

        if (request.StockAdjusted)
            foreach (var item in request.Items)
                await _audit.LogAsync(username, "stock_adjust", "Return", ret.Id.ToString(),
                    $"إضافة {item.Quantity} وحدة للمنتج ID {item.ProductId} (مرتجع)");

        return Ok(new ReturnDto
        {
            Id = ret.Id, ReturnNumber = ret.ReturnNumber, PharmacyId = ret.PharmacyId,
            PharmacyName = pharmacy.Name, OrderId = ret.OrderId, TotalAmount = ret.TotalAmount,
            Status = ret.Status, ReturnType = ret.ReturnType, Reason = ret.Reason,
            Notes = ret.Notes, StockAdjusted = ret.StockAdjusted,
            BalanceAdjusted = ret.BalanceAdjusted, BalanceBefore = ret.BalanceBefore,
            BalanceAfter = ret.BalanceAfter, CreatedAt = ret.CreatedAt
        });
    }
}
