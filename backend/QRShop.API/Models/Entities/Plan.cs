using System.ComponentModel.DataAnnotations;

namespace QRShop.API.Models.Entities;

// A purchasable subscription package. Rows are seeded by migration rather than
// created at runtime — prices are a business decision, not user input.
public class Plan
{
    [Key]
    public int PlanId { get; set; }

    // Stable identifier the client and the order endpoint refer to, so prices
    // can change without the frontend hard-coding a database id.
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Stored in paise, which is also the unit Razorpay's API takes. Rupees as a
    // decimal would invite rounding drift on prices like 1199/6.
    public int AmountPaise { get; set; }

    public int DurationDays { get; set; }

    // Hides a plan from the pricing page without deleting it, so subscriptions
    // that already reference it keep resolving.
    public bool IsActive { get; set; } = true;

    // Sort order on the pricing page.
    public int DisplayOrder { get; set; }
}
