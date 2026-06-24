using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

// Top-level shop categories, e.g. Men / Women / Kids.
public class Category
{
    [Key]
    public int CategoryId { get; set; }

    public int ShopId { get; set; }
    [ForeignKey(nameof(ShopId))]
    public Shop? Shop { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}
