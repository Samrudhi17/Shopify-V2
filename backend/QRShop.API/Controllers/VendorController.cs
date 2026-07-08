using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorController : ControllerBase
{
    private readonly AppDbContext _db;

    public VendorController(AppDbContext db) => _db = db;

    // GET /api/vendor/stats?vendorId=1 — cards for the vendor dashboard.
    [HttpGet("stats")]
    public async Task<IActionResult> Stats([FromQuery] int vendorId)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
        if (shop is null)
            return Ok(new { totalProducts = 0, totalCategories = 0, totalStock = 0 });

        var totalProducts = await _db.Products.CountAsync(p => p.ShopId == shop.ShopId);
        var totalCategories = await _db.Categories.CountAsync(c => c.ShopId == shop.ShopId && c.Status == "Active");
        var totalStock = await _db.Inventories
            .Where(i => i.Variant!.Product!.ShopId == shop.ShopId)
            .SumAsync(i => (int?)i.StockQty) ?? 0;

        return Ok(new { totalProducts, totalCategories, totalStock });
    }

    // GET /api/vendor/reports?vendorId=1 — analytics for the vendor Reports page.
    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery] int vendorId)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
        if (shop is null)
            return Ok(new { totalProducts = 0, totalStock = 0, stockValue = 0m, byCategory = Array.Empty<object>(), byType = Array.Empty<object>(), lowStock = Array.Empty<object>() });

        var products = await _db.Products
            .Where(p => p.ShopId == shop.ShopId)
            .Select(p => new
            {
                p.ProductName,
                p.BasePrice,
                Category = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                Type = p.ProductType ?? "Other",
                Stock = p.Variants.Select(v => v.Inventory != null ? v.Inventory.StockQty : 0).FirstOrDefault(),
            })
            .ToListAsync();

        var byCategory = products.GroupBy(p => p.Category)
            .Select(g => new { name = g.Key, count = g.Count(), stock = g.Sum(x => x.Stock) }).ToList();
        var byType = products.GroupBy(p => p.Type)
            .Select(g => new { name = g.Key, count = g.Count() }).ToList();
        var lowStock = products.Where(p => p.Stock < 5)
            .Select(p => new { p.ProductName, p.Stock }).ToList();

        return Ok(new
        {
            totalProducts = products.Count,
            totalStock = products.Sum(p => p.Stock),
            stockValue = products.Sum(p => p.BasePrice * p.Stock),
            byCategory,
            byType,
            lowStock,
        });
    }
}
