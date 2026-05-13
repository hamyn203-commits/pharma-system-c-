namespace AlNeda.Core.Models;

public class AppUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? PharmacyId { get; set; }
    public string? PharmacyName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "rep";
    public int? PharmacyId { get; set; }
}

public class UpdateUserRequest
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = "rep";
    public int? PharmacyId { get; set; }
    public bool IsActive { get; set; } = true;
}
