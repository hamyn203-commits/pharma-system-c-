using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

public class User : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(256)]
    public string PasswordSalt { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = "admin";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
