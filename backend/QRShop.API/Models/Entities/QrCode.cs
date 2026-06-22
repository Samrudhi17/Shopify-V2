using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public class QrCode
{
    [Key]
    public int QrId { get; set; }

    public int ShopId { get; set; }
    [ForeignKey(nameof(ShopId))]
    public Shop? Shop { get; set; }

    [MaxLength(500)]
    public string CatalogUrl { get; set; } = string.Empty;

    // Path/URL to the generated QR image (Firebase Storage or local).
    [MaxLength(500)]
    public string? QrImagePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
