using AlNeda.API.Authorization;
using System.Security.Claims;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.DomainLogic;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/mobile")]
[Authorize(Policy = AuthPolicies.PharmacyApp)]
[Tags("Mobile")]
public class MobileController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;
    private readonly IConfiguration _configuration;

    public MobileController(
        IDbContextFactory<Data.AppDbContext> contextFactory,
        IAuditService audit,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _audit = audit;
        _configuration = configuration;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(PharmacyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Profile()
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId.Value);
        if (pharmacy == null) return PharmacyNotLinked();

        return Ok(new PharmacyDto
        {
            Id = pharmacy.Id,
            Name = pharmacy.Name,
            Address = pharmacy.Address,
            Phone = pharmacy.Phone,
            Balance = pharmacy.Balance,
            AccountStatus = pharmacy.AccountStatus,
            CreatedAt = pharmacy.CreatedAt
        });
    }

    [HttpGet("products")]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Products([FromQuery] string? search)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        var approvalCheck = await EnsurePharmacyApprovedAsync(db);
        if (approvalCheck != null) return approvalCheck;

        var showUnavailable = GetMobileSettings().ShowUnavailableProducts;
        var query = db.Products.Include(p => p.CategoryObj).Where(p => p.IsActive == 1);
        if (!showUnavailable)
            query = query.Where(p => p.Quantity > 0);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s) || (p.Barcode != null && p.Barcode.Contains(s)));
        }

        var products = await query.OrderBy(p => p.Name).Select(p => ProductDto(p)).ToListAsync();
        return Ok(products);
    }

    [HttpGet("products/{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Product(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        var approvalCheck = await EnsurePharmacyApprovedAsync(db);
        if (approvalCheck != null) return approvalCheck;

        var showUnavailable = GetMobileSettings().ShowUnavailableProducts;
        var product = await db.Products.Include(p => p.CategoryObj)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == 1 && (showUnavailable || p.Quantity > 0));
        if (product == null)
            return NotFound(Error("المنتج غير موجود", "PRODUCT_NOT_FOUND"));

        return Ok(ProductDto(product));
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Categories()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var categories = await db.Categories.Include(c => c.Products)
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                ColorCode = c.ColorCode,
                IsActive = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products.Count(p => p.IsActive == 1),
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("orders")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrder([FromBody] MobileCreateOrderRequest request)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();
        if (request.Items.Count == 0)
            return BadRequest(Error("أضف منتجات إلى الطلب", "VALIDATION_ERROR"));

        var settings = GetMobileSettings();
        if (!settings.AcceptMobileOrders)
            return StatusCode(StatusCodes.Status403Forbidden, Error("استقبال طلبات التطبيق متوقف حالياً", "FORBIDDEN"));

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId.Value);
        if (pharmacy == null) return PharmacyNotLinked();

        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new MobileCreateOrderItemRequest { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (requested.Any(i => i.Quantity <= 0 || i.Quantity > settings.MaxQuantityPerOrderItem))
            return BadRequest(Error("كمية غير صالحة في الطلب", "VALIDATION_ERROR"));

        var ids = requested.Select(i => i.ProductId).ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id) && p.IsActive == 1).ToDictionaryAsync(p => p.Id);
        foreach (var item in requested)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return NotFound(Error($"المنتج {item.ProductId} غير موجود", "PRODUCT_NOT_FOUND"));
            if (product.Quantity < item.Quantity)
                return BadRequest(Error($"الكمية المتاحة من '{product.Name}' هي {product.Quantity}", "OUT_OF_STOCK", new { item.ProductId, available = product.Quantity }));
        }

        var total = requested.Sum(i => products[i.ProductId].UnitPrice * i.Quantity);
        var now = DateTime.Now;
        MarketingOffer? sourceOffer = null;
        if (request.SourceOfferId.HasValue)
        {
            sourceOffer = await db.MarketingOffers.FindAsync(request.SourceOfferId.Value);
            if (sourceOffer == null || !MarketingOfferRules.IsActive(sourceOffer, now))
                return BadRequest(Error("العرض المرتبط بالطلب غير نشط", "OFFER_NOT_ACTIVE"));
            if (!await MarketingOfferRules.MatchesAudienceAsync(db, sourceOffer, pharmacy, now))
                return BadRequest(Error("العرض غير متاح لهذه الصيدلية", "OFFER_NOT_TARGETED"));
        }

        var order = new Order
        {
            OrderNumber = $"MOB-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            PharmacyId = pharmacy.Id,
            TotalAmount = total,
            Discount = 0,
            DiscountType = "value",
            FinalTotal = total,
            BalanceBefore = pharmacy.Balance,
            BalanceAfter = OrderWorkflow.updateBalance(pharmacy.Balance, total, "order"),
            Status = "pending",
            Source = "mobile",
            SourceOfferId = sourceOffer?.Id,
            ClientNotes = request.Notes ?? string.Empty,
            MobileCreatedAt = now,
            CreatedAt = now
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        foreach (var item in requested)
        {
            var product = products[item.ProductId];
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.UnitPrice,
                TotalPrice = product.UnitPrice * item.Quantity
            });
        }

        pharmacy.Balance = order.BalanceAfter;
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "mobile", "create", "Order", order.Id.ToString(),
            $"Mobile order #{order.OrderNumber} created for pharmacy '{pharmacy.Name}' with total {order.FinalTotal:N2}");

        return Ok(await LoadOrderDto(db, order.Id, pharmacy.Id));
    }

    [HttpGet("offers")]
    [ProducesResponseType(typeof(List<MarketingOfferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Offers()
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId.Value);
        if (pharmacy == null) return PharmacyNotLinked();

        var now = DateTime.Now;
        var candidates = await db.MarketingOffers
            .Where(o => o.Status == MarketingOfferRules.Published && o.StartsAt <= now && now < o.EndsAt)
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync();

        var result = new List<MarketingOfferDto>();
        foreach (var offer in candidates.Where(o => MarketingOfferRules.IsActive(o, now)))
        {
            if (await MarketingOfferRules.MatchesAudienceAsync(db, offer, pharmacy, now))
                result.Add(ToOfferDto(offer, now));
        }

        // Android clients should keep any local offers cache short (1-5 minutes) and invalidate on UpdatedAt/Version changes.
        Response.Headers.CacheControl = "private, max-age=300";
        return Ok(result);
    }

    [HttpPost("offers/{id:int}/events")]
    [EnableRateLimiting("offer-events")]
    [ProducesResponseType(typeof(OfferEventResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> OfferEvent(int id, [FromBody] OfferEventRequest request)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        var eventType = request.Type.Trim().ToLowerInvariant();
        if (eventType is not ("impression" or "click"))
            return BadRequest(Error("نوع الحدث غير صالح", "VALIDATION_ERROR"));

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId.Value);
        if (pharmacy == null) return PharmacyNotLinked();

        var now = DateTime.Now;
        var offer = await db.MarketingOffers.FindAsync(id);
        if (offer == null)
            return NotFound(Error("العرض غير موجود", "OFFER_NOT_FOUND"));

        var accepted = MarketingOfferRules.IsActive(offer, now)
            && await MarketingOfferRules.MatchesAudienceAsync(db, offer, pharmacy, now);

        var deviceKey = MarketingOfferRules.DeviceKey(request.DeviceId);
        var dateKey = MarketingOfferRules.EventDateKey(now);
        var usedFallback = MarketingOfferRules.UsesDeviceFallback(request.DeviceId);
        var counted = false;

        if (accepted && eventType == "impression")
        {
            counted = !await db.OfferEvents.AnyAsync(e =>
                e.MarketingOfferId == id
                && e.PharmacyId == pharmacy.Id
                && e.DeviceKey == deviceKey
                && e.EventDateKey == dateKey
                && e.EventType == "impression"
                && e.IsUniqueDailyImpression
                && !e.IsRejected);
        }
        else if (accepted && eventType == "click")
        {
            counted = true;
        }

        db.OfferEvents.Add(new OfferEvent
        {
            MarketingOfferId = id,
            PharmacyId = pharmacy.Id,
            DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : request.DeviceId.Trim(),
            DeviceKey = deviceKey,
            EventType = eventType,
            OccurredAt = now,
            EventDateKey = dateKey,
            IsUniqueDailyImpression = eventType == "impression" && counted,
            UsedDeviceFallback = usedFallback,
            IsRejected = !accepted,
            RejectionReason = accepted ? string.Empty : "offer_not_active_or_not_targeted"
        });
        await db.SaveChangesAsync();

        return Ok(new OfferEventResponse
        {
            Accepted = accepted,
            Counted = counted,
            UsedDeviceFallback = usedFallback,
            Message = accepted
                ? (counted ? "تم تسجيل الحدث" : "تم تجاهل التكرار اليومي")
                : "العرض غير نشط أو غير متاح لهذه الصيدلية"
        });
    }

    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Orders()
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        await using var db = await _contextFactory.CreateDbContextAsync();
        var orders = await db.Orders
            .Include(o => o.Pharmacy)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.PharmacyId == pharmacyId.Value)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => ToOrderDto(o))
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("orders/{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Order(int id)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        await using var db = await _contextFactory.CreateDbContextAsync();
        var dto = await LoadOrderDto(db, id, pharmacyId.Value);
        return dto == null ? NotFound(Error("الطلب غير موجود", "ORDER_NOT_FOUND")) : Ok(dto);
    }

    [HttpPost("orders/{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] MobileCancelOrderRequest request)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();
        if (!GetMobileSettings().AllowMobileCancellation)
            return StatusCode(StatusCodes.Status403Forbidden, Error("إلغاء الطلب من التطبيق غير متاح", "FORBIDDEN"));

        await using var db = await _contextFactory.CreateDbContextAsync();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.PharmacyId == pharmacyId.Value);
        if (order == null)
            return NotFound(Error("الطلب غير موجود", "ORDER_NOT_FOUND"));
        if (order.Status != "pending")
            return BadRequest(Error("يمكن إلغاء الطلبات في حالة pending فقط", "INVALID_ORDER_STATUS"));

        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId.Value);
        order.Status = "cancelled";
        order.CancellationRequestedAt = DateTime.Now;
        order.CancellationReason = request.Reason ?? string.Empty;
        order.LastStatusUpdate = DateTime.Now;
        if (pharmacy != null)
            pharmacy.Balance = OrderWorkflow.updateBalance(pharmacy.Balance, order.FinalTotal, "cancel");

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            OldStatus = "pending",
            NewStatus = "cancelled",
            Note = order.CancellationReason,
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "mobile", "cancel", "Order", order.Id.ToString(),
            $"Mobile cancellation for order #{order.OrderNumber}");

        return Ok(await LoadOrderDto(db, order.Id, pharmacyId.Value));
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments()
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        await using var db = await _contextFactory.CreateDbContextAsync();
        var payments = await db.Payments.Include(p => p.Pharmacy)
            .Where(p => p.PharmacyId == pharmacyId.Value)
            .OrderByDescending(p => p.Date)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                PharmacyId = p.PharmacyId,
                PharmacyName = p.Pharmacy.Name,
                OrderId = p.OrderId,
                Amount = p.Amount,
                PaymentStatus = p.PaymentStatus,
                PaymentType = p.PaymentType,
                AmountPaid = p.AmountPaid,
                RemainingAmount = p.RemainingAmount,
                PaymentNotes = p.PaymentNotes,
                Date = p.Date
            })
            .ToListAsync();
        return Ok(payments);
    }

    [HttpGet("returns")]
    public async Task<IActionResult> Returns()
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        await using var db = await _contextFactory.CreateDbContextAsync();
        var returns = await db.Returns.Include(r => r.Pharmacy).Include(r => r.Items).ThenInclude(i => i.Product)
            .Where(r => r.PharmacyId == pharmacyId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToReturnDto(r))
            .ToListAsync();
        return Ok(returns);
    }

    [HttpPost("returns")]
    public async Task<IActionResult> CreateReturn([FromBody] MobileCreateReturnRequest request)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();
        if (request.Items.Count == 0)
            return BadRequest(Error("أضف منتجات إلى المرتجع", "VALIDATION_ERROR"));

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId.Value);
        if (pharmacy == null) return PharmacyNotLinked();
        if (request.OrderId.HasValue && !await db.Orders.AnyAsync(o => o.Id == request.OrderId && o.PharmacyId == pharmacy.Id))
            return NotFound(Error("الطلب غير موجود", "ORDER_NOT_FOUND"));

        var ids = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return BadRequest(Error("كمية غير صالحة", "VALIDATION_ERROR"));
            if (!products.ContainsKey(item.ProductId))
                return NotFound(Error($"المنتج {item.ProductId} غير موجود", "PRODUCT_NOT_FOUND"));
        }

        var total = request.Items.Sum(i => products[i.ProductId].UnitPrice * i.Quantity);
        var ret = new Return
        {
            ReturnNumber = $"MRET-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            PharmacyId = pharmacy.Id,
            OrderId = request.OrderId,
            TotalAmount = total,
            Status = "pending",
            ReturnType = request.ReturnType,
            Reason = request.Reason,
            Notes = request.Notes,
            StockAdjusted = false,
            BalanceAdjusted = false,
            BalanceBefore = pharmacy.Balance,
            BalanceAfter = pharmacy.Balance,
            CreatedAt = DateTime.Now
        };
        db.Returns.Add(ret);
        await db.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            db.ReturnItems.Add(new ReturnItem
            {
                ReturnId = ret.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.UnitPrice
            });
        }
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "mobile", "create", "Return", ret.Id.ToString(),
            $"Mobile return #{ret.ReturnNumber} requested by pharmacy '{pharmacy.Name}'");

        return Ok(ToReturnDto(await db.Returns.Include(r => r.Pharmacy).Include(r => r.Items).ThenInclude(i => i.Product).FirstAsync(r => r.Id == ret.Id)));
    }

    [HttpGet("sync/status")]
    public IActionResult SyncStatus()
    {
        return Ok(new
        {
            status = "online",
            serverTime = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] MobileSyncRequest request)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null) return PharmacyNotLinked();

        await using var db = await _contextFactory.CreateDbContextAsync();
        var since = request.LastSyncTime;
        var showUnavailable = GetMobileSettings().ShowUnavailableProducts;

        var productQuery = db.Products.Include(p => p.CategoryObj).Where(p => p.IsActive == 1);
        if (!showUnavailable)
            productQuery = productQuery.Where(p => p.Quantity > 0);
        if (since.HasValue)
            productQuery = productQuery.Where(p => p.UpdatedAt >= since.Value || p.CreatedAt >= since.Value);

        var orderQuery = db.Orders
            .Include(o => o.Pharmacy)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.PharmacyId == pharmacyId.Value);
        if (since.HasValue)
            orderQuery = orderQuery.Where(o => o.CreatedAt >= since.Value || o.LastStatusUpdate >= since.Value);

        var paymentQuery = db.Payments.Include(p => p.Pharmacy).Where(p => p.PharmacyId == pharmacyId.Value);
        if (since.HasValue)
            paymentQuery = paymentQuery.Where(p => p.Date >= since.Value);

        return Ok(new MobileSyncResponse
        {
            Products = await productQuery.OrderBy(p => p.Name).Select(p => ProductDto(p)).ToListAsync(),
            Categories = await db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                ColorCode = c.ColorCode,
                IsActive = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                CreatedAt = c.CreatedAt
            }).ToListAsync(),
            Orders = await orderQuery.OrderByDescending(o => o.CreatedAt).Select(o => ToOrderDto(o)).ToListAsync(),
            Payments = await paymentQuery.OrderByDescending(p => p.Date).Select(p => new PaymentDto
            {
                Id = p.Id,
                PharmacyId = p.PharmacyId,
                PharmacyName = p.Pharmacy.Name,
                OrderId = p.OrderId,
                Amount = p.Amount,
                PaymentStatus = p.PaymentStatus,
                PaymentType = p.PaymentType,
                AmountPaid = p.AmountPaid,
                RemainingAmount = p.RemainingAmount,
                PaymentNotes = p.PaymentNotes,
                Date = p.Date
            }).ToListAsync(),
            ServerTime = DateTime.UtcNow
        });
    }

    private int? GetPharmacyId()
    {
        var value = User.FindFirst("pharmacyId")?.Value
            ?? User.FindFirst("PharmacyId")?.Value
            ?? User.FindFirst("pharmacy_id")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    private async Task<bool> IsPharmacyApprovedAsync(Data.AppDbContext db, int pharmacyId)
    {
        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId);
        return pharmacy != null && pharmacy.IsApproved;
    }

    private async Task<IActionResult?> EnsurePharmacyApprovedAsync(Data.AppDbContext db)
    {
        var pid = GetPharmacyId();
        if (pid == null) return PharmacyNotLinked();
        if (!await IsPharmacyApprovedAsync(db, pid.Value))
            return StatusCode(StatusCodes.Status403Forbidden,
                Error("حسابك في انتظار موافقة إدارة المخزن", "ACCOUNT_PENDING"));
        return null;
    }

    private MobileSettingsDto GetMobileSettings()
    {
        var section = _configuration.GetSection("MobileApp");
        return new MobileSettingsDto
        {
            AcceptMobileOrders = section.GetValue("AcceptMobileOrders", true),
            ApiUrl = section.GetValue<string>("ApiUrl") ?? string.Empty,
            NotificationPollingSeconds = section.GetValue("NotificationPollingSeconds", 30),
            AllowMobileCancellation = section.GetValue("AllowMobileCancellation", true),
            ShowUnavailableProducts = section.GetValue("ShowUnavailableProducts", false),
            MaxQuantityPerOrderItem = Math.Max(1, section.GetValue("MaxQuantityPerOrderItem", 100))
        };
    }

    private static ProductDto ProductDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Barcode = p.Barcode,
        Category = p.Category,
        CategoryId = p.CategoryId,
        CategoryName = p.CategoryObj != null ? p.CategoryObj.Name : null,
        Company = p.Company,
        Quantity = p.Quantity,
        UnitPrice = p.UnitPrice,
        ExpiryDate = p.ExpiryDate,
        ImagePath = p.ImagePath,
        ImageUrl = p.ImageUrl,
        Description = p.Description,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };

    private static OrderDto ToOrderDto(Order o) => new()
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

    private static MarketingOfferDto ToOfferDto(MarketingOffer offer, DateTime now) => new()
    {
        Id = offer.Id,
        Title = offer.Title,
        Subtitle = offer.Subtitle,
        Description = offer.Description,
        ImageUrl = offer.ImageUrl,
        OfferType = offer.OfferType,
        AudienceRule = offer.AudienceRule,
        StartsAt = offer.StartsAt,
        EndsAt = offer.EndsAt,
        Status = offer.Status,
        QuantityLimit = offer.QuantityLimit,
        RemainingQuantity = offer.RemainingQuantity,
        OldPrice = offer.OldPrice,
        NewPrice = offer.NewPrice,
        DiscountPercent = offer.DiscountPercent,
        UpdatedAt = offer.UpdatedAt,
        IsActive = MarketingOfferRules.IsActive(offer, now)
    };

    private static ReturnDto ToReturnDto(Return r) => new()
    {
        Id = r.Id,
        ReturnNumber = r.ReturnNumber,
        PharmacyId = r.PharmacyId,
        PharmacyName = r.Pharmacy.Name,
        OrderId = r.OrderId,
        TotalAmount = r.TotalAmount,
        Status = r.Status,
        ReturnType = r.ReturnType,
        Reason = r.Reason,
        Notes = r.Notes,
        StockAdjusted = r.StockAdjusted,
        BalanceAdjusted = r.BalanceAdjusted,
        BalanceBefore = r.BalanceBefore,
        BalanceAfter = r.BalanceAfter,
        CreatedAt = r.CreatedAt,
        Items = r.Items.Select(i => new ReturnItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product != null ? i.Product.Name : null,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.Quantity * i.UnitPrice
        }).ToList()
    };

    private static async Task<OrderDto?> LoadOrderDto(Data.AppDbContext db, int id, int pharmacyId)
    {
        return await db.Orders
            .Include(o => o.Pharmacy)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.Id == id && o.PharmacyId == pharmacyId)
            .Select(o => ToOrderDto(o))
            .FirstOrDefaultAsync();
    }

    private ObjectResult PharmacyNotLinked() =>
        StatusCode(StatusCodes.Status403Forbidden, Error("حساب الصيدلية غير مربوط بصيدلية", "PHARMACY_NOT_LINKED"));

    private static ApiError Error(string message, string code, object? details = null) => new()
    {
        Message = message,
        Code = code,
        Details = details ?? new { }
    };
}
