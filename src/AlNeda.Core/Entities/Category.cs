using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

public class Category : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }           // وصف التصنيف
    public string? Icon { get; set; }                  // أيقونة (emoji أو مسار أيقونة)
    public string? ColorCode { get; set; }             // لون التصنيف (Hex)
    public bool IsActive { get; set; } = true;         // نشط / غير نشط
    public bool IsDeleted { get; set; } = false;       // Soft Delete
    public int? RemoteId { get; set; }
    public bool IsSynced { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;         // ترتيب العرض

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }           // آخر تحديث

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
