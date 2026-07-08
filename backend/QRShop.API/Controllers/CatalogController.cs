using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.DTOs;

namespace QRShop.API.Controllers;

// Public, unauthenticated catalog for customers who scan the QR / open the URL.
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly AppDbContext _db;

    public CatalogController(AppDbContext db) => _db = db;

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
                p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()))
            .ToListAsync();

        var shopDto = new CatalogShop(
            shop.ShopName, shop.LogoUrl, shop.Phone, shop.AlternateNumber, shop.Address, shop.CatalogUrl);

        return Ok(new { shop = shopDto, products });
    }
}
