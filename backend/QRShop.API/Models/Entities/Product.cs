using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public class Product
{
    [Key]
    public int ProductId { get; set; }

    public int ShopId { get; set; }
    [ForeignKey(nameof(ShopId))]
    public Shop? Shop { get; set; }

    // Optional sub-category (Product_Categories). Not required in the simple flow.
    public int? ProductCategoryId { get; set; }
    [ForeignKey(nameof(ProductCategoryId))]
    public ProductCategory? ProductCategory { get; set; }

    // Direct link to a top-level category (Men / Women / Kids).
    public int? CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [Required, MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    // e.g. Saree, Jeans, Shirt, T-Shirt.
    [MaxLength(100)]
    public string? ProductType { get; set; }

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
