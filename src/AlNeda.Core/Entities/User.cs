using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = "admin";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
