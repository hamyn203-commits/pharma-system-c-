using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public PurchasesController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var list = await db.Purchases.Include(p => p.Supplier).Include(p => p.Items).ThenInclude(i => i.Product)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();
        var dto = list.Select(p => new PurchaseDto
        {
            Id = p.Id, InvoiceNumber = p.InvoiceNumber, SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.Name, TotalAmount = p.TotalAmount,
            AmountPaid = p.AmountPaid, RemainingAmount = p.RemainingAmount,
            Status = p.Status, Notes = p.Notes, CreatedAt = p.CreatedAt,
            Items = p.Items.Select(i => new PurchaseItemDto
            {
                Id = i.Id, ProductId = i.ProductId, ProductName = i.Product?.Name,
                Quantity = i.Quantity, UnitPrice = i.UnitCost, TotalPrice = i.Quantity * i.UnitCost
            }).ToList()
        }).ToList();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            return BadRequest(new { message = "رقم الفاتورة مطلوب" });
        if (request.SupplierId == 0)
            return BadRequest(new { message = "اختر المورد" });
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "أضف منتجات على الأقل" });

        await using var db = await _contextFactory.CreateDbContextAsync();

        var purchase = new Purchase
        {
            InvoiceNumber = request.InvoiceNumber.Trim(),
            SupplierId = request.SupplierId,
            TotalAmount = request.TotalAmount,
            AmountPaid = request.AmountPaid,
            RemainingAmount = request.TotalAmount - request.AmountPaid,
            Status = request.AmountPaid >= request.TotalAmount ? "paid" : "unpaid",
            Notes = request.Notes,
            CreatedAt = DateTime.Now
        };
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            db.PurchaseItems.Add(new PurchaseItem
            {
                PurchaseId = purchase.Id, ProductId = item.ProductId,
                Quantity = item.Quantity, UnitCost = item.UnitPrice
            });
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null) prod.Quantity += item.Quantity;
        }
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "Purchase", purchase.Id.ToString(),
            $"فاتورة مشتريات #{purchase.InvoiceNumber} بقيمة {purchase.TotalAmount}");

        return Ok(new PurchaseDto
        {
            Id = purchase.Id, InvoiceNumber = purchase.InvoiceNumber,
            SupplierId = purchase.SupplierId, TotalAmount = purchase.TotalAmount,
            AmountPaid = purchase.AmountPaid, RemainingAmount = purchase.RemainingAmount,
            Status = purchase.Status, Notes = purchase.Notes, CreatedAt = purchase.CreatedAt
        });
    }
}
