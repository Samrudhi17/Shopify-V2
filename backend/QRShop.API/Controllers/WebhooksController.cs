using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IRazorpayService _razorpay;
    private readonly ISubscriptionService _subscriptions;
    private readonly ILogger<WebhooksController> _log;

    public WebhooksController(
        IRazorpayService razorpay, ISubscriptionService subscriptions, ILogger<WebhooksController> log)
    {
        _razorpay = razorpay;
        _subscriptions = subscriptions;
        _log = log;
    }

    // POST /api/webhooks/razorpay
    //
    // The authoritative confirmation of a payment. The browser callback can be
    // lost — the customer closes the tab, the network drops — but Razorpay keeps
    // retrying this until it gets a 2xx, so a vendor who paid always ends up with
    // their subscription.
    //
    // Anonymous because Razorpay has no Firebase token; the HMAC below is what
    // authenticates the caller.
    [AllowAnonymous]
    [HttpPost("razorpay")]
    public async Task<IActionResult> Razorpay()
    {
        // Signed over the exact bytes sent, so the body has to be read raw. Any
        // deserialize-and-reserialize round trip changes the whitespace and key
        // order, and the HMAC would never match.
        string rawBody;
        using (var reader = new StreamReader(Request.Body))
            rawBody = await reader.ReadToEndAsync();

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        if (!_razorpay.IsValidWebhookSignature(rawBody, signature))
        {
            _log.LogWarning("Rejected a Razorpay webhook with an invalid signature.");
            return Unauthorized();
        }

        string? eventName;
        string? orderId;
        string? paymentId;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            eventName = root.GetProperty("event").GetString();

            var entity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
            orderId = entity.GetProperty("order_id").GetString();
            paymentId = entity.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Signature was valid, so this is a Razorpay event in a shape this
            // endpoint does not read (a refund, a settlement). Acknowledge it, or
            // Razorpay retries a payload that will never be understood.
            _log.LogInformation("Ignoring an unrecognised Razorpay webhook payload.");
            return Ok();
        }

        if (string.IsNullOrEmpty(orderId))
            return Ok();

        switch (eventName)
        {
            case "payment.captured":
                await _subscriptions.SettlePaymentAsync(orderId, paymentId, rawBody);
                break;

            case "payment.failed":
                await _subscriptions.MarkFailedAsync(orderId, rawBody);
                break;

            default:
                _log.LogInformation("Ignoring Razorpay event {Event}.", eventName);
                break;
        }

        // Always 2xx once the signature checks out. A non-2xx puts Razorpay into
        // a retry loop over something already handled.
        return Ok();
    }
}
