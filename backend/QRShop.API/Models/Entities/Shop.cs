using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public class Shop
{
    [Key]
    public int ShopId { get; set; }

    public int VendorId { get; set; }
    [ForeignKey(nameof(VendorId))]
    public Vendor? Vendor { get; set; }

    [Required, MaxLength(150)]
    public string ShopName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ShopType { get; set; } = "Cloth";

    [Required, MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PancardNo { get; set; }

    [MaxLength(20)]
    public string? AadhaarCardNo { get; set; }

    [MaxLength(20)]
    public string? AlternateNumber { get; set; }

    [MaxLength(50)]
    public string? ShopActNo { get; set; }

    // PDF or image of the Shop Act certificate (stored on Firebase Storage).
    [MaxLength(500)]
    public string? ShopActCertificateUrl { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    // Public catalog URL: domain/shopname
    [MaxLength(500)]
    public string? CatalogUrl { get; set; }

    // URL-safe unique slug derived from ShopName, used in the catalog URL.
    [MaxLength(160)]
    public string? Slug { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public QrCode? QrCode { get; set; }
}
