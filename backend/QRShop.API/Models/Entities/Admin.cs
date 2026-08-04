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

    // No password is stored here or anywhere else: Firebase holds the
    // credentials and the API only ever sees a signed ID token. A nullable
    // Password column survived from before that change and was dropped in
    // DropAdminPassword — nothing ever read or wrote it.

    [MaxLength(50)]
    public string Role { get; set; } = "Admin";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
