using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRShop.API.Models.Entities;

public static class PaymentStatus
{
    public const string Created = "Created";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
}

// One row per Razorpay order. Written when the order is created, then settled by
// whichever confirmation arrives first — the browser callback or the webhook.
public class PaymentTransaction
{
    [Key]
    public int PaymentTransactionId { get; set; }

    public int VendorId { get; set; }
    [ForeignKey(nameof(VendorId))]
    public Vendor? Vendor { get; set; }

    public int PlanId { get; set; }
    [ForeignKey(nameof(PlanId))]
    public Plan? Plan { get; set; }

    [Required, MaxLength(64)]
    public string RazorpayOrderId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? RazorpayPaymentId { get; set; }

    // Copied from the plan when the order is created, so a later price change
    // does not rewrite what this vendor was actually charged.
    public int AmountPaise { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = PaymentStatus.Created;

    // The subscription term this payment bought, once it is granted. Null until
    // the payment settles, and the marker that makes settling idempotent.
    public int? SubscriptionId { get; set; }
    [ForeignKey(nameof(SubscriptionId))]
    public Subscription? Subscription { get; set; }

    // Raw provider payload, kept for reconciling a disputed payment by hand.
    [Column(TypeName = "text")]
    public string? RawPayload { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SettledAt { get; set; }
}
