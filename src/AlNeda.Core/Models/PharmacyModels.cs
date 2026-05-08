namespace AlNeda.Core.Models;

public class PharmacyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
}

public class CreatePharmacyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = "active";
}

public class UpdatePharmacyRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = "active";
}

public class PharmacyStatusRequest
{
    public string Status { get; set; } = "active";
}
