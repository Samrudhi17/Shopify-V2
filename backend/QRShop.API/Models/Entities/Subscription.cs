using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public static class SubscriptionStatus
{
    public const string Trialing = "Trialing";
    public const string Active = "Active";
    public const string Expired = "Expired";
}

// One row per term a vendor has held: the free trial, then each paid period.
// Rows are never rewritten on renewal, so the history stays auditable — the
// vendor's current access is whichever row has the latest EndsAt.
public class Subscription
{
    [Key]
    public int SubscriptionId { get; set; }

    public int VendorId { get; set; }
    [ForeignKey(nameof(VendorId))]
    public Vendor? Vendor { get; set; }

    public int PlanId { get; set; }
    [ForeignKey(nameof(PlanId))]
    public Plan? Plan { get; set; }

    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    public DateTime EndsAt { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = SubscriptionStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Status is a label for display; access is decided by the clock, so an
    // expired row cannot grant access just because a background job never ran.
    public bool IsCurrentlyValid(DateTime nowUtc) =>
        Status != SubscriptionStatus.Expired && EndsAt > nowUtc;
}
