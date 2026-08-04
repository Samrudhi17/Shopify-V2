using Microsoft.EntityFrameworkCore;
using QRCoder;
using QRShop.API.Data;
using QRShop.API.Models.Entities;

namespace QRShop.API.Services;

public interface IQrCodeService
{
    // Renders the QR PNG for a URL and returns its stored relative path.
    Task<string> RenderAsync(string url);

    // Rewrites a shop's QR so it encodes the current PUBLIC_BASE_URL.
    Task<string> RefreshAsync(Shop shop);

    // Refreshes every shop whose stored catalog URL no longer matches the
    // current PUBLIC_BASE_URL. Returns how many were rewritten.
    Task<int> RefreshStaleAsync();
}

public class QrCodeService : IQrCodeService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IConfiguration _config;

    public QrCodeService(AppDbContext db, IFileStorageService storage, IConfiguration config)
    {
        _db = db;
        _storage = storage;
        _config = config;
    }

    public async Task<string> RenderAsync(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(20);
        return await _storage.SaveBytesAsync(png, "qrcodes", ".png");
    }

    public async Task<string> RefreshAsync(Shop shop)
    {
        var catalogUrl = PublicUrls.Catalog(_config, shop.Slug);
        var qrImagePath = await RenderAsync(catalogUrl);

        if (shop.QrCode is null)
            _db.QrCodes.Add(new QrCode { ShopId = shop.ShopId, CatalogUrl = catalogUrl, QrImagePath = qrImagePath });
        else
        {
            shop.QrCode.CatalogUrl = catalogUrl;
            shop.QrCode.QrImagePath = qrImagePath;
        }

        shop.CatalogUrl = catalogUrl;
        return qrImagePath;
    }

    // The target URL is drawn INTO the QR image, so config alone cannot fix an
    // existing code after the site moves. Running this on startup means a deploy
    // to a new address needs no extra authenticated call.
    public async Task<int> RefreshStaleAsync()
    {
        var shops = await _db.Shops.Include(s => s.QrCode).ToListAsync();
        var changed = 0;

        foreach (var shop in shops)
        {
            var expected = PublicUrls.Catalog(_config, shop.Slug);
            if (shop.QrCode is not null && shop.CatalogUrl == expected && shop.QrCode.CatalogUrl == expected)
                continue;

            await RefreshAsync(shop);
            changed++;
        }

        if (changed > 0) await _db.SaveChangesAsync();
        return changed;
    }
}
