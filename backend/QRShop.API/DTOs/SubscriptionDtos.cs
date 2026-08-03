using System.ComponentModel.DataAnnotations;

namespace QRShop.API.DTOs;

// AmountPaise is what Checkout needs; Amount is the same figure in rupees so the
// pricing page does not have to divide.
public record PlanResponse(
    string Code,
    string Name,
    int AmountPaise,
    decimal Amount,
    int DurationDays);

public record SubscriptionStatusResponse(
    string? PlanCode,
    string? PlanName,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? Status,
    bool IsActive,
    int DaysRemaining);

// One past or present term.
public record SubscriptionTermResponse(
    string PlanName,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    bool IsCurrent);

// A payment attempt. Amount is in rupees for display; the order id is what
// Razorpay support asks for when a vendor queries a charge.
public record PaymentHistoryResponse(
    DateTime CreatedAt,
    string PlanName,
    decimal Amount,
    string Status,
    string OrderId,
    string? PaymentId);

public record BillingHistoryResponse(
    IReadOnlyList<SubscriptionTermResponse> Terms,
    IReadOnlyList<PaymentHistoryResponse> Payments);

// Deliberately only a plan code — no amount. The price is looked up server-side.
public record CreateOrderRequest(
    [Required(ErrorMessage = "A plan is required.")]
    string PlanCode);

public record CreateOrderResponse(
    string OrderId,
    string KeyId,
    int AmountPaise,
    string Currency,
    string PlanName);

public record VerifyPaymentRequest(
    [Required] string RazorpayOrderId,
    [Required] string RazorpayPaymentId,
    [Required] string RazorpaySignature);
