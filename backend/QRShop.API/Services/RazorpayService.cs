using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QRShop.API.Services;

public interface IRazorpayService
{
    string KeyId { get; }

    // Creates an order at Razorpay and returns its id.
    Task<string> CreateOrderAsync(int amountPaise, string receipt, CancellationToken ct = default);

    // Checkout hands the browser these three values; the signature proves the
    // payment really came from Razorpay and was not typed in by the client.
    bool IsValidPaymentSignature(string orderId, string paymentId, string signature);

    // Webhooks are signed over the exact bytes of the request body.
    bool IsValidWebhookSignature(string rawBody, string signature);
}

public class RazorpayService : IRazorpayService
{
    private readonly HttpClient _http;
    private readonly string? _keyId;
    private readonly string? _keySecret;
    private readonly string? _webhookSecret;

    // Configuration is read but not enforced here. Throwing in the constructor
    // would take down every endpoint that merely *mentions* billing — including
    // the public pricing page — on an install where the keys are not filled in
    // yet. The paying paths below fail loudly instead.
    public RazorpayService(HttpClient http, IConfiguration config)
    {
        _http = http;

        _keyId = config["RAZORPAY_KEY_ID"];
        _keySecret = config["RAZORPAY_KEY_SECRET"];

        // Optional: an install without webhooks configured still works through
        // the browser callback. The webhook endpoint rejects everything until
        // this is set, rather than trusting unverified calls.
        _webhookSecret = config["RAZORPAY_WEBHOOK_SECRET"];

        _http.BaseAddress = new Uri("https://api.razorpay.com/v1/");

        if (!string.IsNullOrEmpty(_keyId) && !string.IsNullOrEmpty(_keySecret))
        {
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }
    }

    public string KeyId => _keyId ?? throw NotConfigured();

    private static InvalidOperationException NotConfigured() => new(
        "Razorpay is not configured. Set RAZORPAY_KEY_ID and RAZORPAY_KEY_SECRET in .env.");

    public async Task<string> CreateOrderAsync(int amountPaise, string receipt, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_keyId) || string.IsNullOrEmpty(_keySecret)) throw NotConfigured();

        var payload = new
        {
            amount = amountPaise,
            currency = "INR",
            receipt,
            // The order is only a quote until a payment is captured against it.
            payment_capture = 1,
        };

        using var response = await _http.PostAsJsonAsync("orders", payload, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Razorpay order creation failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Razorpay returned an order without an id.");
    }

    public bool IsValidPaymentSignature(string orderId, string paymentId, string signature) =>
        !string.IsNullOrEmpty(_keySecret) && IsValidHmac($"{orderId}|{paymentId}", signature, _keySecret);

    public bool IsValidWebhookSignature(string rawBody, string signature) =>
        !string.IsNullOrEmpty(_webhookSecret) && IsValidHmac(rawBody, signature, _webhookSecret);

    // Razorpay signs with HMAC-SHA256 and sends the digest as lowercase hex.
    private static bool IsValidHmac(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        byte[] provided;
        try
        {
            provided = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        // Fixed-time compare: a plain == would leak, through its timing, how much
        // of a guessed signature was correct, which is enough to forge one.
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
