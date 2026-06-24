using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

// Sub-category under a Category (e.g. under "Men": Shirts, Jeans, T-Shirts).
public class ProductCategory
{
    [Key]
    public int ProductCategoryId { get; set; }

    public int CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Image { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
