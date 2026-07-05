using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QRShop.API.Data;
using QRShop.API.DTOs;
using QRShop.API.Models.Entities;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IConfiguration _config;

    public ShopsController(AppDbContext db, IFileStorageService storage, IConfiguration config)
    {
        _db = db;
        _storage = storage;
        _config = config;
    }

    // POST /api/shops — register a shop, generate catalog URL + QR code.
    [HttpPost]
    public async Task<ActionResult<ShopResponse>> Create(CreateShopRequest req)
    {
        var vendor = await _db.Vendors.FindAsync(req.VendorId);
        if (vendor is null)
            return BadRequest(new { message = "Vendor not found." });

        if (await _db.Shops.AnyAsync(s => s.VendorId == req.VendorId))
            return Conflict(new { message = "This vendor already has a shop." });

        if (string.IsNullOrWhiteSpace(req.ShopName))
            return BadRequest(new { message = "Shop name is required." });

        if (await _db.Shops.AnyAsync(s => s.ShopName == req.ShopName))
            return Conflict(new { message = "Shop name is already taken." });

        // Build a URL-safe unique slug.
        var baseSlug = SlugHelper.Slugify(req.ShopName);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "shop";
        var slug = baseSlug;
        var n = 1;
        while (await _db.Shops.AnyAsync(s => s.Slug == slug))
            slug = $"{baseSlug}-{++n}";

        var publicBase = _config["AppSettings:PublicBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        var catalogUrl = $"{publicBase}/{slug}";

        var shop = new Shop
        {
            VendorId = req.VendorId,
            ShopName = req.ShopName,
            ShopType = "Cloth",
            AadhaarCardNo = req.AadhaarCardNo,
            PancardNo = req.PancardNo,
            ShopActNo = req.ShopActNo,
            ShopActCertificateUrl = req.ShopActCertificateUrl,
            Address = req.Address,
            Phone = req.Phone,
            AlternateNumber = req.AlternateNumber,
            LogoUrl = req.LogoUrl,
            Slug = slug,
            CatalogUrl = catalogUrl,
            Status = "Active",
        };
        _db.Shops.Add(shop);
        await _db.SaveChangesAsync();

        // Generate the QR code image for the catalog URL and store it locally.
        var qrImagePath = await GenerateQrAsync(catalogUrl);

        _db.QrCodes.Add(new QrCode
        {
            ShopId = shop.ShopId,
            CatalogUrl = catalogUrl,
            QrImagePath = qrImagePath,
        });

        // Seed the default cloth-shop categories.
        foreach (var name in new[] { "Men", "Women", "Kids" })
            _db.Categories.Add(new Category { ShopId = shop.ShopId, CategoryName = name });

        await _db.SaveChangesAsync();

        return new ShopResponse(shop.ShopId, shop.ShopName, slug, catalogUrl, qrImagePath, shop.LogoUrl, shop.Status);
    }

    // PUT /api/shops/5 — update editable shop details (used by vendor Settings).
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateShopDetailsRequest req)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop is null) return NotFound(new { message = "Shop not found." });

        shop.Phone = req.Phone;
        shop.AlternateNumber = req.AlternateNumber;
        shop.Address = req.Address;
        await _db.SaveChangesAsync();
        return Ok(new { shop.ShopId, shop.Phone, shop.AlternateNumber, shop.Address });
    }

    // GET /api/shops/vendor/1 — full shop details for the vendor Profile page.
    [HttpGet("vendor/{vendorId:int}")]
    public async Task<ActionResult<ShopDetailsResponse>> GetByVendor(int vendorId)
    {
        var shop = await _db.Shops
            .Include(s => s.QrCode)
            .FirstOrDefaultAsync(s => s.VendorId == vendorId);

        if (shop is null)
            return NotFound(new { message = "No shop for this vendor yet." });

        return new ShopDetailsResponse(
            shop.ShopId, shop.ShopName, shop.ShopType, shop.Address, shop.Phone,
            shop.AlternateNumber, shop.AadhaarCardNo, shop.PancardNo, shop.ShopActNo,
            shop.ShopActCertificateUrl, shop.LogoUrl, shop.Slug ?? "", shop.CatalogUrl ?? "",
            shop.QrCode?.QrImagePath, shop.Status);
    }

    // GET /api/shops/qrcode?vendorId=1 — used by the vendor QR Code page.
    [HttpGet("qrcode")]
    public async Task<IActionResult> GetQr([FromQuery] int vendorId)
    {
        var shop = await _db.Shops
            .Include(s => s.QrCode)
            .FirstOrDefaultAsync(s => s.VendorId == vendorId);

        if (shop?.QrCode is null)
            return NotFound(new { message = "No shop/QR found for this vendor." });

        return Ok(new { catalogUrl = shop.QrCode.CatalogUrl, qrImagePath = shop.QrCode.QrImagePath });
    }

    private async Task<string> GenerateQrAsync(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(20);
        return await _storage.SaveBytesAsync(png, "qrcodes", ".png");
    }
}
