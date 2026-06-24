using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    public int ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(80)]
    public string? Sku { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public Inventory? Inventory { get; set; }
    public ICollection<StockHistory> StockHistory { get; set; } = new List<StockHistory>();
}
