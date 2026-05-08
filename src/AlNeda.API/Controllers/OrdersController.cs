using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.DomainLogic;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public OrdersController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Orders.Include(o => o.Pharmacy).Include(o => o.Items).ThenInclude(i => i.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);
        var orders = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
        var dto = orders.Select(o => new OrderDto
        {
            Id = o.Id, OrderNumber = o.OrderNumber, PharmacyId = o.PharmacyId,
            PharmacyName = o.Pharmacy?.Name, TotalAmount = o.TotalAmount,
            Discount = o.Discount, DiscountType = o.DiscountType, FinalTotal = o.FinalTotal,
            AmountPaid = o.AmountPaid, BalanceBefore = o.BalanceBefore,
            BalanceAfter = o.BalanceAfter, Status = o.Status, CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                Id = i.Id, ProductId = i.ProductId, ProductName = i.Product?.Name,
                Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice
            }).ToList()
        }).ToList();
        return Ok(dto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var o = await db.Orders.Include(x => x.Pharmacy).Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.StatusHistory).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound(new { message = "الطلب غير موجود" });
        return Ok(MapOrder(o));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (request.PharmacyId == 0)
            return BadRequest(new { message = "اختر الصيدلية" });
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "أضف منتجات على الأقل" });

        await using var db = await _contextFactory.CreateDbContextAsync();

        var pharmacy = await db.Pharmacies.FindAsync(request.PharmacyId);
        if (pharmacy == null) return NotFound(new { message = "الصيدلية غير موجودة" });

        // Check stock
        foreach (var item in request.Items)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod == null)
                return BadRequest(new { message = $"منتج ID {item.ProductId} غير موجود" });
            if (prod.Quantity < item.Quantity)
                return BadRequest(new { message = $"الكمية المتوفرة من '{prod.Name}' هي {prod.Quantity} فقط" });
        }

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            PharmacyId = request.PharmacyId,
            TotalAmount = request.TotalAmount,
            Discount = request.Discount,
            DiscountType = request.DiscountType,
            FinalTotal = OrderWorkflow.computeFinalTotal(request.TotalAmount, request.Discount, request.DiscountType),
            Status = "pending",
            BalanceBefore = pharmacy.Balance,
            BalanceAfter = OrderWorkflow.updateBalance(pharmacy.Balance, OrderWorkflow.computeFinalTotal(request.TotalAmount, request.Discount, request.DiscountType), "order"),
            CreatedAt = DateTime.Now
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id, ProductId = item.ProductId,
                Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            });
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null) prod.Quantity -= item.Quantity;
        }

        pharmacy.Balance = order.BalanceAfter;
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "Order", order.Id.ToString(),
            $"طلب #{order.OrderNumber} للصيدلية '{pharmacy.Name}' بقيمة {order.FinalTotal}");

        return Ok(MapOrder(order));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var order = await db.Orders.Include(o => o.Items).Include(o => o.Pharmacy).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound(new { message = "الطلب غير موجود" });

        var result = OrderWorkflow.validateTransition(order.Status, request.Status);
        if (result is not OrderWorkflow.TransitionOutcome.Allowed allowed)
            return BadRequest(new { message = "لا يمكن تغيير الحالة إلى " + request.Status });

        var oldStatus = order.Status;
        order.Status = allowed.newStatus;
        order.LastStatusUpdate = DateTime.Now;
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = id, OldStatus = oldStatus, NewStatus = allowed.newStatus,
            Note = allowed.message, CreatedAt = DateTime.Now
        });

        if (allowed.newStatus == "cancelled" && oldStatus != "reviewed")
        {
            foreach (var item in order.Items)
            {
                var prod = await db.Products.FindAsync(item.ProductId);
                if (prod != null) prod.Quantity += item.Quantity;
            }
            if (order.Pharmacy != null)
                order.Pharmacy.Balance = OrderWorkflow.updateBalance(order.Pharmacy.Balance, order.FinalTotal, "cancel");
        }

        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "status_change", "Order", id.ToString(),
            $"تغيير حالة الطلب #{order.OrderNumber} من {oldStatus} إلى {allowed.newStatus}");

        return Ok(MapOrder(order));
    }

    private static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id, OrderNumber = o.OrderNumber, PharmacyId = o.PharmacyId,
        PharmacyName = o.Pharmacy?.Name, TotalAmount = o.TotalAmount,
        Discount = o.Discount, DiscountType = o.DiscountType, FinalTotal = o.FinalTotal,
        AmountPaid = o.AmountPaid, BalanceBefore = o.BalanceBefore,
        BalanceAfter = o.BalanceAfter, Status = o.Status, CreatedAt = o.CreatedAt,
        Items = o.Items?.Select(i => new OrderItemDto
        {
            Id = i.Id, ProductId = i.ProductId, ProductName = i.Product?.Name,
            Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice
        }).ToList() ?? []
    };
}

public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
