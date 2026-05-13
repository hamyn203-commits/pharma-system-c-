using AlNeda.API.Authorization;
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
[Authorize(Policy = AuthPolicies.Staff)]
[Tags("Admin Orders")]
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
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? source)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Orders.Include(o => o.Pharmacy).Include(o => o.Items).ThenInclude(i => i.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(source))
            q = q.Where(o => o.Source == source);

        var dto = await q.OrderByDescending(o => o.CreatedAt).Select(o => ToDto(o)).ToListAsync();
        return Ok(dto);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var dto = await db.Orders.Include(o => o.Pharmacy).Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(x => x.Id == id)
            .Select(o => ToDto(o))
            .FirstOrDefaultAsync();
        if (dto == null) return NotFound(new ApiError { Message = "الطلب غير موجود", Code = "ORDER_NOT_FOUND" });
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (request.PharmacyId == 0)
            return BadRequest(new ApiError { Message = "اختر الصيدلية", Code = "VALIDATION_ERROR" });
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new ApiError { Message = "أضف منتجات على الأقل", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(request.PharmacyId);
        if (pharmacy == null) return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_LINKED" });

        foreach (var item in request.Items)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod == null)
                return BadRequest(new ApiError { Message = $"منتج ID {item.ProductId} غير موجود", Code = "PRODUCT_NOT_FOUND" });
            if (prod.Quantity < item.Quantity)
                return BadRequest(new ApiError { Message = $"الكمية المتوفرة من '{prod.Name}' هي {prod.Quantity} فقط", Code = "OUT_OF_STOCK" });
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
            Source = "admin",
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
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            });
        }

        pharmacy.Balance = order.BalanceAfter;
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "create", "Order", order.Id.ToString(),
            $"طلب #{order.OrderNumber} للصيدلية '{pharmacy.Name}' بقيمة {order.FinalTotal}");

        return Ok(await db.Orders.Include(o => o.Pharmacy).Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.Id == order.Id)
            .Select(o => ToDto(o))
            .FirstAsync());
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound(new ApiError { Message = "الطلب غير موجود", Code = "ORDER_NOT_FOUND" });

        var result = OrderWorkflow.validateTransition(order.Status, request.Status);
        if (result is not OrderWorkflow.TransitionOutcome.Allowed allowed)
            return BadRequest(new ApiError { Message = "لا يمكن تغيير الحالة إلى " + request.Status, Code = "INVALID_ORDER_STATUS" });

        var oldStatus = order.Status;
        order.Status = allowed.newStatus;
        order.LastStatusUpdate = DateTime.Now;
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = id,
            OldStatus = oldStatus,
            NewStatus = allowed.newStatus,
            Note = allowed.message,
            CreatedAt = DateTime.Now
        });

        if (allowed.newStatus == "reviewed")
        {
            var items = await db.OrderItems.Where(i => i.OrderId == id).ToListAsync();
            foreach (var item in items)
            {
                var prod = await db.Products.FindAsync(item.ProductId);
                if (prod != null) prod.Quantity = Math.Max(prod.Quantity - item.Quantity, 0);
            }
        }

        if (allowed.newStatus == "cancelled")
        {
            var stockWasDeducted = oldStatus is "reviewed" or "in_store" or "with_driver" or "on_the_way" or "postponed";
            if (stockWasDeducted)
            {
                var items = await db.OrderItems.Where(i => i.OrderId == id).ToListAsync();
                foreach (var item in items)
                {
                    var prod = await db.Products.FindAsync(item.ProductId);
                    if (prod != null) prod.Quantity += item.Quantity;
                }
            }

            var pharmacy = await db.Pharmacies.FindAsync(order.PharmacyId);
            if (pharmacy != null)
                pharmacy.Balance = OrderWorkflow.updateBalance(pharmacy.Balance, order.FinalTotal, "cancel");
        }

        await db.SaveChangesAsync();
        await _audit.LogAsync(User.Identity?.Name ?? "system", "status_change", "Order", id.ToString(),
            $"تغيير حالة الطلب #{order.OrderNumber} من {oldStatus} إلى {allowed.newStatus}");

        return Ok(await db.Orders.Include(o => o.Pharmacy).Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.Id == order.Id)
            .Select(o => ToDto(o))
            .FirstAsync());
    }

    private static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        PharmacyId = o.PharmacyId,
        PharmacyName = o.Pharmacy.Name,
        TotalAmount = o.TotalAmount,
        Discount = o.Discount,
        DiscountType = o.DiscountType,
        FinalTotal = o.FinalTotal,
        AmountPaid = o.AmountPaid,
        BalanceBefore = o.BalanceBefore,
        BalanceAfter = o.BalanceAfter,
        Status = o.Status,
        Source = o.Source,
        SourceOfferId = o.SourceOfferId,
        ClientNotes = o.ClientNotes,
        MobileCreatedAt = o.MobileCreatedAt,
        CancellationRequestedAt = o.CancellationRequestedAt,
        CancellationReason = o.CancellationReason,
        CreatedAt = o.CreatedAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product!.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList()
    };
}

public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
