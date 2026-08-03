using System.Net.Http.Headers;
using System.Text.Json;

namespace QRShop.API.Services;

public interface IAiDescriptionService
{
    Task<string> GenerateAsync(ProductFacts facts, CancellationToken ct = default);
}

// Only what the vendor actually typed into the form. Anything null is simply
// left out of the prompt rather than sent as an empty string.
public record ProductFacts(
    string ProductName,
    string? ProductType,
    string? Brand,
    string? Color,
    string? Size,
    decimal? BasePrice,
    string? Category);

// One POST to an OpenAI-compatible /chat/completions endpoint.
//
// Deliberately not an SDK: OpenAI, DeepSeek and most others accept this exact
// request shape, so switching provider is a change of AI_BASE_URL and AI_MODEL
// in .env — no new dependency and no code change.
public class AiDescriptionService : IAiDescriptionService
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _model;

    // Short, factual, and forbidden from inventing product claims. Invented
    // fabric or warranty details would go straight onto a live public catalog
    // that real customers buy from.
    private const string SystemPrompt =
        "You write short product descriptions for a small Indian retail shop's online catalog. " +
        "Use only the details given. Never invent fabric, material, origin, warranty, discounts, " +
        "sizing advice or care instructions that are not stated. " +
        "Write 2 to 3 plain sentences, no markdown, no bullet points, no headings, no emoji, " +
        "no price unless a price is given. Do not address the reader as 'you'. " +
        "Reply with the description text only.";

    public AiDescriptionService(HttpClient http, IConfiguration config)
    {
        _http = http;

        _apiKey = config["AI_API_KEY"];

        // Defaults to Gemini's OpenAI-compatible endpoint because it is the only
        // one of the three with a genuine free tier. Override both in .env to
        // point at DeepSeek or OpenAI instead.
        _model = config["AI_MODEL"] ?? "gemini-3.6-flash";

        var baseUrl = config["AI_BASE_URL"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/";
        if (!baseUrl.EndsWith('/')) baseUrl += "/";
        _http.BaseAddress = new Uri(baseUrl);

        // A model can stall; the vendor is staring at a spinner in a form.
        _http.Timeout = TimeSpan.FromSeconds(30);

        if (!string.IsNullOrEmpty(_apiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> GenerateAsync(ProductFacts facts, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("AI_API_KEY is not configured. Add it to .env.");

        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = Describe(facts) },
            },
            // ~3 sentences. Also the ceiling on what one click can cost.
            max_tokens = 160,
            temperature = 0.7,
        };

        using var response = await _http.PostAsJsonAsync("chat/completions", payload, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"AI request failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("The AI returned an empty description.");

        // Models like to wrap prose in quotes when told to reply with text only.
        return text.Trim().Trim('"').Trim();
    }

    private static string Describe(ProductFacts f)
    {
        var lines = new List<string> { $"Product name: {f.ProductName}" };

        if (!string.IsNullOrWhiteSpace(f.ProductType)) lines.Add($"Type: {f.ProductType}");
        if (!string.IsNullOrWhiteSpace(f.Category)) lines.Add($"Category: {f.Category}");
        if (!string.IsNullOrWhiteSpace(f.Brand)) lines.Add($"Brand: {f.Brand}");
        if (!string.IsNullOrWhiteSpace(f.Color)) lines.Add($"Colour: {f.Color}");
        if (!string.IsNullOrWhiteSpace(f.Size)) lines.Add($"Size: {f.Size}");
        if (f.BasePrice is > 0) lines.Add($"Price: Rs. {f.BasePrice}");

        return string.Join("\n", lines);
    }
}
