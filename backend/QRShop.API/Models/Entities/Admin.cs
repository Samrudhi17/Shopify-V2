using System.ComponentModel.DataAnnotations;

namespace QRShop.API.Models.Entities;

public class Admin
{
    [Key]
    public int AdminId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // Firebase Authentication UID (name/email/password live in Firebase).
    [MaxLength(128)]
    public string? FirebaseUid { get; set; }

    // Firebase handles the real auth; this is a hashed fallback/local password.
    [MaxLength(255)]
    public string? Password { get; set; }

    [MaxLength(50)]
    public string Role { get; set; } = "Admin";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
