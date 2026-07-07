using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public InventoryController(AppDbContext db) => _db = db;

    // GET /api/inventory?vendorId=1 — stock per product variant for the shop.
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int vendorId)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
        if (shop is null) return Ok(Array.Empty<object>());

        var rows = await _db.Inventories
            .Where(i => i.Variant!.Product!.ShopId == shop.ShopId)
            .Select(i => new
            {
                i.InventoryId,
                Sku = i.Variant!.Sku,
                ProductName = i.Variant!.Product!.ProductName,
                ProductType = i.Variant!.Product!.ProductType,
                i.Variant!.Color,
                i.Variant!.Size,
                i.StockQty,
                i.ReservedQty,
                i.AvailableQty,
            })
            .ToListAsync();

        return Ok(rows);
    }

    // PUT /api/inventory/5  { "stockQty": 20 } — update stock for a variant.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest body)
    {
        var inv = await _db.Inventories.FindAsync(id);
        if (inv is null) return NotFound(new { message = "Inventory row not found." });

        inv.StockQty = body.StockQty;
        inv.AvailableQty = body.StockQty - inv.ReservedQty;
        inv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { inv.InventoryId, inv.StockQty, inv.AvailableQty });
    }
}

public record UpdateStockRequest(int StockQty);
