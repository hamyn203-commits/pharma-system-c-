using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
