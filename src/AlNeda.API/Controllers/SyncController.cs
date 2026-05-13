using AlNeda.API.Authorization;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data.UnitOfWork;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize(Policy = AuthPolicies.Staff)]
public class SyncController : ControllerBase
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IAuditService _auditService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IUnitOfWorkFactory uowFactory,
        IAuditService auditService,
        ILogger<SyncController> logger)
    {
        _uowFactory = uowFactory;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request)
    {
        if (request.Products == null && request.Categories == null)
            return BadRequest(new { message = "بيانات المزامنة مطلوبة" });

        var username = User.Identity?.Name ?? "api";
        var response = new SyncResponse
        {
            Success = true,
            ServerTime = DateTime.UtcNow,
            Message = "تمت المزامنة بنجاح"
        };

        try
        {
            using var uow = _uowFactory.Create();
            var productRepo = uow.GetRepository<Product>();
            var categoryRepo = uow.GetRepository<Category>();

            await uow.BeginTransactionAsync();

            // ── Process incoming Products ──
            if (request.Products?.Count > 0)
            {
                foreach (var incoming in request.Products)
                {
                    var existing = incoming.RemoteId.HasValue
                        ? (await productRepo.GetAllAsync()).FirstOrDefault(p => p.RemoteId == incoming.RemoteId || p.Id == incoming.RemoteId)
                        : null;

                    if (existing != null)
                    {
                        // Track changes for audit
                        var changes = new List<(string, object?, object?)>();
                        if (existing.Name != incoming.Name) changes.Add(("Name", existing.Name, incoming.Name));
                        if (existing.UnitPrice != incoming.UnitPrice) changes.Add(("UnitPrice", existing.UnitPrice, incoming.UnitPrice));
                        if (existing.Quantity != incoming.Quantity) changes.Add(("Quantity", existing.Quantity, incoming.Quantity));

                        existing.Name = incoming.Name;
                        existing.Barcode = incoming.Barcode;
                        existing.Category = incoming.Category;
                        existing.Quantity = incoming.Quantity;
                        existing.UnitPrice = incoming.UnitPrice;
                        existing.ExpiryDate = incoming.ExpiryDate;
                        existing.RemoteId = incoming.RemoteId;
                        existing.IsSynced = true;

                        await productRepo.UpdateAsync(existing);

                        if (changes.Count > 0)
                        {
                            await _auditService.LogEntityUpdateAsync(username, "Product", existing.Id, changes.ToArray());
                        }
                    }
                    else
                    {
                        incoming.IsSynced = true;
                        await productRepo.AddAsync(incoming);
                        await _auditService.LogAsync(username, "create", "Product", incoming.Id.ToString(),
                            $"تم إضافة المنتج '{incoming.Name}' عبر المزامنة");
                    }
                }
            }

            // ── Process incoming Categories ──
            if (request.Categories?.Count > 0)
            {
                foreach (var incoming in request.Categories)
                {
                    var existing = incoming.RemoteId.HasValue
                        ? (await categoryRepo.GetAllAsync()).FirstOrDefault(c => c.RemoteId == incoming.RemoteId || c.Id == incoming.RemoteId)
                        : null;

                    if (existing != null)
                    {
                        existing.Name = incoming.Name;
                        existing.Description = incoming.Description;
                        existing.ColorCode = incoming.ColorCode;
                        existing.RemoteId = incoming.RemoteId;
                        existing.IsSynced = true;
                        await categoryRepo.UpdateAsync(existing);
                    }
                    else
                    {
                        incoming.IsSynced = true;
                        await categoryRepo.AddAsync(incoming);
                        await _auditService.LogAsync(username, "create", "Category", incoming.Id.ToString(),
                            $"تم إضافة التصنيف '{incoming.Name}' عبر المزامنة");
                    }
                }
            }

            await uow.SaveChangesAsync();
            await uow.CommitTransactionAsync();

            // ── Send back server's latest data ──
            var allProducts = (await productRepo.GetAllAsync()).ToList();
            response.NewOrUpdatedProducts = allProducts;
            response.NewOrUpdatedCategories = (await categoryRepo.GetAllAsync()).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            return StatusCode(500, new SyncResponse
            {
                Success = false,
                Message = $"فشلت المزامنة: {ex.Message}",
                ServerTime = DateTime.UtcNow
            });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "online",
            serverTime = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
