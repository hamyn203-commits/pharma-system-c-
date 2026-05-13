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
[Route("api/payments")]
[Authorize(Policy = AuthPolicies.Staff)]
public class PaymentsController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public PaymentsController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? pharmacyId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var query = db.Payments.Include(p => p.Pharmacy).AsQueryable();
        if (pharmacyId.HasValue)
        {
            query = query.Where(p => p.PharmacyId == pharmacyId.Value);
        }

        var payments = await query
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        if (request.PharmacyId <= 0)
        {
            return BadRequest(new { message = "اختر الصيدلية" });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "المبلغ يجب أن يكون أكبر من صفر" });
        }

        if (!Domain.PaymentType.isValid(request.PaymentType))
        {
            return BadRequest(new { message = "طريقة الدفع غير صالحة" });
        }

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(request.PharmacyId);
        if (pharmacy == null)
        {
            return NotFound(new { message = "الصيدلية غير موجودة" });
        }

        var oldBalance = pharmacy.Balance;
        var payment = new Payment
        {
            PharmacyId = request.PharmacyId,
            OrderId = request.OrderId,
            Amount = request.Amount,
            PaymentType = request.PaymentType,
            PaymentStatus = Domain.PaymentStatus.calculate(request.Amount, pharmacy.Balance, request.PaymentType),
            AmountPaid = request.Amount,
            RemainingAmount = Math.Max(pharmacy.Balance - request.Amount, 0),
            PaymentNotes = request.PaymentNotes ?? string.Empty,
            Date = DateTime.Now
        };

        db.Payments.Add(payment);
        pharmacy.Balance = OrderWorkflow.updateBalance(pharmacy.Balance, request.Amount, "payment");
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(
            username,
            "create",
            "Payment",
            payment.Id.ToString(),
            $"تم تسجيل تحصيل بقيمة {payment.Amount:N2} من الصيدلية '{pharmacy.Name}'");
        await _audit.LogBalanceChangeAsync(username, pharmacy.Id, pharmacy.Name, oldBalance, pharmacy.Balance, "payment");

        return Ok(new PaymentDto
        {
            Id = payment.Id,
            PharmacyId = payment.PharmacyId,
            PharmacyName = pharmacy.Name,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            PaymentStatus = payment.PaymentStatus,
            PaymentType = payment.PaymentType,
            AmountPaid = payment.AmountPaid,
            RemainingAmount = payment.RemainingAmount,
            PaymentNotes = payment.PaymentNotes,
            Date = payment.Date
        });
    }
}
