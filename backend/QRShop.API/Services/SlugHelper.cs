using System.Text;
using System.Text.RegularExpressions;

namespace QRShop.API.Services;

public static class SlugHelper
{
    // "Trendy Threads!" -> "trendy-threads"
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var lower = input.Trim().ToLowerInvariant();
        var cleaned = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
        cleaned = Regex.Replace(cleaned, @"[\s-]+", "-").Trim('-');
        return cleaned;
    }
}
