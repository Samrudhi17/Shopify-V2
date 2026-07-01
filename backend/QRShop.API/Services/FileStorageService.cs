namespace QRShop.API.Services;

public interface IFileStorageService
{
    // Saves an uploaded file under wwwroot/uploads/<subfolder> and returns a
    // public URL (e.g. http://localhost:5152/uploads/products/<guid>.jpg).
    Task<string> SaveAsync(IFormFile file, string subfolder);

    // Saves raw bytes (e.g. a generated QR PNG) and returns its public URL.
    Task<string> SaveBytesAsync(byte[] bytes, string subfolder, string extension);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _http;

    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };

    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    public FileStorageService(IWebHostEnvironment env, IHttpContextAccessor http)
    {
        _env = env;
        _http = http;
    }

    public async Task<string> SaveAsync(IFormFile file, string subfolder)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("Empty file.");
        if (file.Length > MaxBytes)
            throw new ArgumentException("File exceeds the 10 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException($"File type '{ext}' is not allowed.");

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

    private string BuildUrl(string subfolder, string fileName)
    {
        var request = _http.HttpContext!.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}/uploads/{subfolder}/{fileName}";
    }
}
