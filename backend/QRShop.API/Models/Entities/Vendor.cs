using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public class Vendor
{
    [Key]
    public int VendorId { get; set; }

    // The admin (first user) who owns/oversees vendors.
    public int? AdminId { get; set; }
    [ForeignKey(nameof(AdminId))]
    public Admin? Admin { get; set; }

    // Firebase Authentication UID (name/email/password live in Firebase).
    [MaxLength(128)]
    public string? FirebaseUid { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Shop> Shops { get; set; } = new List<Shop>();
}
