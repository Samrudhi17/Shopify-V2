using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.DTOs;
using QRShop.API.Models.Entities;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _me;
    private readonly IRazorpayService _razorpay;
    private readonly ISubscriptionService _subscriptions;
    private readonly ILogger<SubscriptionsController> _log;

    public SubscriptionsController(
        AppDbContext db, ICurrentUser me, IRazorpayService razorpay,
        ISubscriptionService subscriptions, ILogger<SubscriptionsController> log)
    {
        _db = db;
        _me = me;
        _razorpay = razorpay;
        _subscriptions = subscriptions;
        _log = log;
    }

    // GET /api/subscriptions/plans — the pricing table. Public so the marketing
    // page can show prices before anyone signs up.
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> Plans()
    {
        var plans = await _db.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PlanResponse(
                p.Code,
                p.Name,
                p.AmountPaise,
                p.AmountPaise / 100m,
                p.DurationDays))
            .ToListAsync();

        return Ok(plans);
    }

    // GET /api/subscriptions/me — the signed-in vendor's current term, used for
    // the days-remaining banner and to decide whether to nag.
    [HttpGet("me")]
    public async Task<IActionResult> Mine()
    {
        var vendorId = await _me.GetVendorIdAsync();
        if (vendorId is null)
            return NotFound(new { message = "No vendor profile for this account." });

        var current = await _subscriptions.GetCurrentAsync(vendorId.Value);
        if (current is null)
            return Ok(new SubscriptionStatusResponse(null, null, null, null, null, false, 0));

        var now = DateTime.UtcNow;
        var isActive = current.IsCurrentlyValid(now);

        // Round up: with 6 hours left a vendor should read "1 day", not "0".
        var daysRemaining = isActive ? (int)Math.Ceiling((current.EndsAt - now).TotalDays) : 0;

        return Ok(new SubscriptionStatusResponse(
            current.Plan?.Code,
            current.Plan?.Name,
            current.StartsAt,
            current.EndsAt,
            // The stored label goes stale the moment a term runs out, so report
            // what the clock says rather than what the column says.
            isActive ? current.Status : SubscriptionStatus.Expired,
            isActive,
            daysRemaining));
    }

    // GET /api/subscriptions/history — every term and payment for this vendor,
    // so they can see what they were charged and when without asking.
    [HttpGet("history")]
    public async Task<IActionResult> History()
    {
        var vendorId = await _me.GetVendorIdAsync();
        if (vendorId is null)
            return NotFound(new { message = "No vendor profile for this account." });

        var now = DateTime.UtcNow;

        var terms = await _db.Subscriptions
            .Where(s => s.VendorId == vendorId)
            .OrderByDescending(s => s.EndsAt)
            .Select(s => new SubscriptionTermResponse(
                s.Plan!.Name,
                s.StartsAt,
                s.EndsAt,
                s.Status != SubscriptionStatus.Expired && s.EndsAt > now
                    ? s.Status
                    : SubscriptionStatus.Expired,
                s.StartsAt <= now && s.EndsAt > now))
            .ToListAsync();

        // Unpaid attempts are included on purpose: a vendor whose card failed
        // needs to see that it failed, not an empty list.
        var payments = await _db.PaymentTransactions
            .Where(t => t.VendorId == vendorId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PaymentHistoryResponse(
                t.CreatedAt,
                t.Plan!.Name,
                t.AmountPaise / 100m,
                t.Status,
                t.RazorpayOrderId,
                t.RazorpayPaymentId))
            .ToListAsync();

        return Ok(new BillingHistoryResponse(terms, payments));
    }

    // POST /api/subscriptions/order  { planCode: "yearly" }
    // Creates a Razorpay order for the plan and returns what Checkout needs.
    [HttpPost("order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        var vendorId = await _me.GetVendorIdAsync();
        if (vendorId is null)
            return NotFound(new { message = "No vendor profile for this account." });

        // The client sends only a plan code. The amount is read from the database
        // — accepting a price from the browser would let anyone buy a year for ₹1.
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Code == req.PlanCode && p.IsActive);
        if (plan is null)
            return BadRequest(new { message = "Unknown plan." });

        if (plan.Code == PlanCodes.Trial)
            return BadRequest(new { message = "The free trial cannot be purchased." });

        string orderId;
        try
        {
            orderId = await _razorpay.CreateOrderAsync(plan.AmountPaise, $"vendor-{vendorId}-{plan.Code}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            // The vendor gets a generic message — the provider's response can name
            // keys and account state — but the real reason has to reach the logs,
            // or a misconfigured server is an unexplained 502 with nothing to go on.
            _log.LogError(ex, "Razorpay order creation failed for vendor {VendorId}, plan {PlanCode}.",
                vendorId, plan.Code);
            return StatusCode(502, new { message = "Could not reach the payment provider. Please try again." });
        }

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            VendorId = vendorId.Value,
            PlanId = plan.PlanId,
            RazorpayOrderId = orderId,
            AmountPaise = plan.AmountPaise,
            Status = PaymentStatus.Created,
        });
        await _db.SaveChangesAsync();

        return Ok(new CreateOrderResponse(
            orderId, _razorpay.KeyId, plan.AmountPaise, "INR", plan.Name));
    }

    // POST /api/subscriptions/verify — called by the browser when Checkout
    // succeeds. The webhook is the authoritative path; this exists so the vendor
    // sees their new expiry immediately instead of waiting for the callback.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentRequest req)
    {
        var vendorId = await _me.GetVendorIdAsync();
        if (vendorId is null)
            return NotFound(new { message = "No vendor profile for this account." });

        if (!_razorpay.IsValidPaymentSignature(req.RazorpayOrderId, req.RazorpayPaymentId, req.RazorpaySignature))
            return BadRequest(new { message = "Payment verification failed." });

        // A valid signature proves Razorpay captured the payment, not that the
        // caller owns the order — check that separately before granting time.
        var owned = await _db.PaymentTransactions
            .AnyAsync(t => t.RazorpayOrderId == req.RazorpayOrderId && t.VendorId == vendorId.Value);
        if (!owned)
            return BadRequest(new { message = "Payment verification failed." });

        var result = await _subscriptions.SettlePaymentAsync(
            req.RazorpayOrderId, req.RazorpayPaymentId, rawPayload: null);

        if (result == SettlementResult.UnknownOrder)
            return BadRequest(new { message = "Payment verification failed." });

        var current = await _subscriptions.GetCurrentAsync(vendorId.Value);
        return Ok(new { message = "Payment successful.", endsAt = current?.EndsAt });
    }
}
