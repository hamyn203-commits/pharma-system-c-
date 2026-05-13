namespace AlNeda.Core.Models;

public class PharmacyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = "active";
    public bool HasAppAccount { get; set; }
    public bool IsAppAccountActive { get; set; }
    public DateTime? AppLastLoginAt { get; set; }
    public int AppOrdersCount { get; set; }
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

public class CreatePharmacyAppAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ResetPharmacyAppPasswordRequest
{
    public string Password { get; set; } = string.Empty;
}

public class SetPharmacyAppAccountStatusRequest
{
    public bool IsActive { get; set; }
}
