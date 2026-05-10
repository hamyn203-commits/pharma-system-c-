namespace AlNeda.Core.Models;

public class PaymentDto
{
    public int Id { get; set; }
    public int PharmacyId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentNotes { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class CreatePaymentRequest
{
    public int PharmacyId { get; set; }
    public int? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = "cash";
    public string PaymentNotes { get; set; } = string.Empty;
}
