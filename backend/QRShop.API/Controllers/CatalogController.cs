using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.Services;
using QRShop.API.DTOs;

namespace QRShop.API.Controllers;

// Public, unauthenticated catalog for customers who scan the QR / open the URL.
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public CatalogController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // GET /api/catalog — public directory of Active shops, for the "Shops" page
    // on the marketing site. Inactive shops are hidden, matching the rule that
    // deactivating a shop takes its catalog offline.
    [HttpGet]
    public async Task<IActionResult> Shops()
    {
        var shops = await _db.Shops
            .Where(s => s.Status == "Active")
            .OrderBy(s => s.ShopName)
            .Select(s => new PublicShopSummary(
                s.ShopId,
                s.ShopName,
                s.Slug ?? "",
                s.LogoUrl,
                s.Address,
                s.Phone,
                s.ShopType,
                s.Products.Count(p => p.Status == "Active")))
            .ToListAsync();

        return Ok(shops);
    }

    // GET /api/catalog/mulchand
    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Slug == slug);
        if (shop is null)
            return NotFound(new { message = "Shop not found." });

        if (shop.Status != "Active")
            return StatusCode(403, new { message = "This shop is currently unavailable." });

        var products = await _db.Products
            .Where(p => p.ShopId == shop.ShopId && p.Status == "Active")
            .OrderByDescending(p => p.ProductId)
            .Select(p => new CatalogProduct(
                p.ProductId,
                p.ProductName,
                p.ProductType,
                p.Brand,
                p.Description,
                p.BasePrice,
                p.Category != null ? p.Category.CategoryName : null,
                p.Variants.Select(v => v.Color).FirstOrDefault(),
                p.Variants.Select(v => v.Size).FirstOrDefault(),
                p.Variants.Select(v => v.Inventory != null ? v.Inventory.AvailableQty : 0).FirstOrDefault(),
                p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
                // Primary first, then the rest in upload order — the card
                // carousel scrolls through these.
                p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.ImageId)
                    .Select(i => i.ImageUrl).ToList()))
            .ToListAsync();

        var shopDto = new CatalogShop(
            shop.ShopName, shop.LogoUrl, shop.Phone, shop.AlternateNumber, shop.Address,
            PublicUrls.Catalog(_config, shop.Slug));

        return Ok(new { shop = shopDto, products });
    }
}
