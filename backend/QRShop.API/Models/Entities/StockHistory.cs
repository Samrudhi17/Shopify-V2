using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public class StockHistory
{
    [Key]
    public int MovementId { get; set; }

    public int VariantId { get; set; }
    [ForeignKey(nameof(VariantId))]
    public ProductVariant? Variant { get; set; }

    // "IN" or "OUT"
    [Required, MaxLength(10)]
    public string MovementType { get; set; } = "IN";

    public int Quantity { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? Remarks { get; set; }
}
