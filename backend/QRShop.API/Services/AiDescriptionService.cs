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

// One POST to Gemini's generateContent endpoint.
//
// This talks to Gemini's own API rather than its OpenAI-compatibility layer:
// that layer silently ignores parameters it does not recognise, which would
// quietly drop the maxOutputTokens cap and leave a single click's cost
// unbounded. The native shape documents every field used here.
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
        _model = config["AI_MODEL"] ?? "gemini-flash-latest";

        var baseUrl = config["AI_BASE_URL"] ?? "https://generativelanguage.googleapis.com/v1beta/";
        if (!baseUrl.EndsWith('/')) baseUrl += "/";
        _http.BaseAddress = new Uri(baseUrl);

        // A model can stall; the vendor is staring at a spinner in a form.
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> GenerateAsync(ProductFacts facts, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("AI_API_KEY is not configured. Add it to .env.");

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = Describe(facts) } } },
            },
            generationConfig = new
            {
                // ~3 sentences. Also the ceiling on what one click can cost.
                maxOutputTokens = 160,
                temperature = 0.7,
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"models/{_model}:generateContent")
        {
            Content = JsonContent.Create(payload),
        };
        // Gemini takes the key in its own header, not as a bearer token.
        request.Headers.Add("x-goog-api-key", _apiKey);

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini request failed ({(int)response.StatusCode}): {body}");

        return ExtractText(body);
    }

    private static string ExtractText(string body)
    {
        using var doc = JsonDocument.Parse(body);

        // A candidate blocked by a safety filter, or cut off before it wrote
        // anything, comes back with no parts at all — so nothing here assumes
        // the happy shape.
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException($"Gemini returned no candidates: {body}");
        }

        var candidate = candidates[0];

        if (!candidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.GetArrayLength() == 0)
        {
            var reason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "unknown";
            throw new InvalidOperationException($"Gemini returned no text (finishReason: {reason}).");
        }

        var text = parts[0].TryGetProperty("text", out var t) ? t.GetString() : null;

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Gemini returned an empty description.");

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
