using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QRShop.API.Data;
using QRShop.API.DTOs;
using QRShop.API.Filters;
using QRShop.API.Models.Entities;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShopsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IConfiguration _config;
    private readonly IQrCodeService _qr;
    private readonly ICurrentUser _me;

    public ShopsController(AppDbContext db, IFileStorageService storage, IConfiguration config, ICurrentUser me, IQrCodeService qr)
    {
        _db = db;
        _storage = storage;
        _config = config;
        _me = me;
        _qr = qr;
    }

    // GET /api/shops/name-available?name=Gokul — live check for the registration
    // form. Advisory only: Create re-checks, because another vendor can claim the
    // name between this call and the submit.
    //
    // Anonymous: the shop form is filled in before the vendor profile exists, and
    // this leaks nothing beyond whether a public shop name is taken.
    [AllowAnonymous]
    [HttpGet("name-available")]
    public async Task<IActionResult> NameAvailable([FromQuery] string name)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length < 2)
            return Ok(new { available = false, reason = "Shop name must be at least 2 characters." });

        var taken = await _db.Shops.AnyAsync(s => s.ShopName == trimmed);
        return Ok(new { available = !taken, reason = taken ? "That shop name is already taken." : null });
    }

    // POST /api/shops — register a shop, generate catalog URL + QR code.
    [RequiresActiveSubscription]
    [HttpPost]
    public async Task<ActionResult<ShopResponse>> Create(CreateShopRequest req)
    {
        // The owner is whoever holds the token, not whoever the body names.
        var vendor = await _me.GetVendorAsync();
        if (vendor is null)
            return BadRequest(new { message = "Vendor not found." });

        if (await _db.Shops.AnyAsync(s => s.VendorId == vendor.VendorId))
            return Conflict(new { message = "This vendor already has a shop." });

        if (string.IsNullOrWhiteSpace(req.ShopName))
            return BadRequest(new { message = "Shop name is required." });

        // Trim before comparing and storing so "Gokul " can't claim a second
        // listing next to "Gokul". The column collation is case-insensitive, so
        // this also blocks "gokul".
        var shopName = req.ShopName.Trim();

        if (await _db.Shops.AnyAsync(s => s.ShopName == shopName))
            return Conflict(new { message = "That shop name is already taken." });

        // Build a URL-safe unique slug.
        var baseSlug = SlugHelper.Slugify(shopName);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "shop";
        var slug = baseSlug;
        var n = 1;
        while (await _db.Shops.AnyAsync(s => s.Slug == slug))
            slug = $"{baseSlug}-{++n}";

        var catalogUrl = PublicUrls.Catalog(_config, slug);

        var shop = new Shop
        {
            VendorId = vendor.VendorId,
            ShopName = shopName,
            ShopType = "Cloth",
            AadhaarCardNo = req.AadhaarCardNo,
            PancardNo = req.PancardNo,
            // Licence numbers are printed upper case; store one canonical form.
            ShopActNo = req.ShopActNo?.Trim().ToUpperInvariant(),
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
        var qrImagePath = await _qr.RenderAsync(catalogUrl);

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
    [RequiresActiveSubscription]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateShopDetailsRequest req)
    {
        var vendorId = await _me.GetVendorIdAsync();
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.ShopId == id && s.VendorId == vendorId);
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
        // The id stays in the route because the client already builds URLs that
        // way, but a vendor may only ask for their own shop.
        if (vendorId != await _me.GetVendorIdAsync() && !await _me.IsAdminAsync())
            return NotFound(new { message = "No shop for this vendor yet." });

        var shop = await _db.Shops
            .Include(s => s.QrCode)
            .FirstOrDefaultAsync(s => s.VendorId == vendorId);

        if (shop is null)
            return NotFound(new { message = "No shop for this vendor yet." });

        return new ShopDetailsResponse(
            shop.ShopId, shop.ShopName, shop.ShopType, shop.Address, shop.Phone,
            shop.AlternateNumber, shop.AadhaarCardNo, shop.PancardNo, shop.ShopActNo,
            shop.ShopActCertificateUrl, shop.LogoUrl, shop.Slug ?? "",
            // Computed, not read from the column, so it follows PUBLIC_BASE_URL.
            PublicUrls.Catalog(_config, shop.Slug),
            shop.QrCode?.QrImagePath, shop.Status);
    }

    // GET /api/shops/qrcode — used by the vendor QR Code page.
    [HttpGet("qrcode")]
    public async Task<IActionResult> GetQr()
    {
        var vendorId = await _me.GetVendorIdAsync();
        var shop = await _db.Shops
            .Include(s => s.QrCode)
            .FirstOrDefaultAsync(s => s.VendorId == vendorId);

        if (shop?.QrCode is null)
            return NotFound(new { message = "No shop/QR found for this vendor." });

        return Ok(new
        {
            catalogUrl = PublicUrls.Catalog(_config, shop.Slug),
            qrImagePath = shop.QrCode.QrImagePath,
        });
    }

    // POST /api/shops/{id}/regenerate-qr — re-render the QR PNG so it encodes the
    // CURRENT PUBLIC_BASE_URL. The target URL is drawn inside the image, so
    // changing the domain (localhost -> yourdomain.com) leaves every existing QR
    // pointing at the old address until this runs.
    [AdminOnly]
    [HttpPost("{id:int}/regenerate-qr")]
    public async Task<IActionResult> RegenerateQr(int id)
    {
        var shop = await _db.Shops.Include(s => s.QrCode).FirstOrDefaultAsync(s => s.ShopId == id);
        if (shop is null) return NotFound(new { message = "Shop not found." });

        var qrImagePath = await _qr.RefreshAsync(shop);
        await _db.SaveChangesAsync();

        return Ok(new { shop.ShopId, shop.ShopName, catalogUrl = shop.CatalogUrl, qrImagePath });
    }

    // POST /api/shops/regenerate-all-qr — same, for every shop. Run this once
    // after deploying to a new domain.
    [AdminOnly]
    [HttpPost("regenerate-all-qr")]
    public async Task<IActionResult> RegenerateAllQr()
    {
        var regenerated = await _qr.RefreshStaleAsync();
        return Ok(new { regenerated });
    }

}
