using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public class Inventory
{
    [Key]
    public int InventoryId { get; set; }

    public int VariantId { get; set; }
    [ForeignKey(nameof(VariantId))]
    public ProductVariant? Variant { get; set; }

    public int StockQty { get; set; }

    public int ReservedQty { get; set; }

    // Typically StockQty - ReservedQty.
    public int AvailableQty { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
