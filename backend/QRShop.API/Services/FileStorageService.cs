namespace QRShop.API.Services;

public interface IFileStorageService
{
    // Saves an uploaded file under wwwroot/uploads/<subfolder> and returns a
    // ROOT-RELATIVE path (e.g. /uploads/products/<guid>.jpg).
    //
    // Relative on purpose: an absolute URL bakes the current host into the
    // database, so every row breaks when the site moves between localhost,
    // staging and the real domain. The browser resolves these against whatever
    // origin served the page, and nginx proxies /uploads/ to this API.
    Task<string> SaveAsync(IFormFile file, string subfolder);

    // Saves raw bytes (e.g. a generated QR PNG) and returns its relative path.
    Task<string> SaveBytesAsync(byte[] bytes, string subfolder, string extension);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    // Per-folder upload limits, mirrored by FILE_RULES in the client's
    // constants/validation.js. A certificate may be a scan or a PDF; a logo is
    // an image only and is kept small because it renders in a 56px card.
    private sealed record UploadRule(string Label, string[] Extensions, long MaxBytes);

    private static readonly Dictionary<string, UploadRule> Rules = new()
    {
        ["certificates"] = new("Shop Act certificate",
            new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" }, 3 * 1024 * 1024),
        ["logos"] = new("Logo",
            new[] { ".jpg", ".jpeg", ".png", ".webp" }, 500 * 1024),
        ["products"] = new("Product image",
            new[] { ".jpg", ".jpeg", ".png", ".webp" }, 500 * 1024),
        ["qrcodes"] = new("QR code",
            new[] { ".png" }, 2 * 1024 * 1024),
    };

    private static readonly UploadRule DefaultRule = new("File",
        new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" }, 500 * 1024);

    private static string Describe(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024)} MB" : $"{bytes / 1024} KB";

    public FileStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(IFormFile file, string subfolder)
    {
        var rule = Rules.TryGetValue(subfolder, out var r) ? r : DefaultRule;

        if (file is null || file.Length == 0)
            throw new ArgumentException($"{rule.Label} is empty.");

        if (file.Length > rule.MaxBytes)
            throw new ArgumentException(
                $"{rule.Label} must be {Describe(rule.MaxBytes)} or smaller — this file is {Describe(file.Length)}.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!rule.Extensions.Contains(ext))
            throw new ArgumentException(
                $"{rule.Label} must be a {string.Join(", ", rule.Extensions.Select(e => e.TrimStart('.')))} file.");

        // wwwroot may not exist by default on a Web API project.
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", subfolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        return BuildUrl(subfolder, fileName);
    }

    public async Task<string> SaveBytesAsync(byte[] bytes, string subfolder, string extension)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", subfolder);
        Directory.CreateDirectory(folder);

        var ext = extension.StartsWith('.') ? extension : "." + extension;
        var fileName = $"{Guid.NewGuid():N}{ext}";
        await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);

        return BuildUrl(subfolder, fileName);
    }

    // Root-relative, with no scheme or host — see the interface for why.
    private static string BuildUrl(string subfolder, string fileName) =>
        $"/uploads/{subfolder}/{fileName}";
}
