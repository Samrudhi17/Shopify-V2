using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.Filters;
using QRShop.API.Models.Entities;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _me;

    public InventoryController(AppDbContext db, ICurrentUser me)
    {
        _db = db;
        _me = me;
    }

    private async Task<Shop?> MyShopAsync()
    {
        var vendorId = await _me.GetVendorIdAsync();
        if (vendorId is null) return null;
        return await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
    }

    // GET /api/inventory — stock per product variant for the vendor's shop.
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var shop = await MyShopAsync();
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
    [RequiresActiveSubscription]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest body)
    {
        var shop = await MyShopAsync();
        if (shop is null) return NotFound(new { message = "Inventory row not found." });

        // Constrained to rows hanging off the caller's own shop.
        var inv = await _db.Inventories
            .FirstOrDefaultAsync(i => i.InventoryId == id && i.Variant!.Product!.ShopId == shop.ShopId);
        if (inv is null) return NotFound(new { message = "Inventory row not found." });

        inv.StockQty = body.StockQty;
        inv.AvailableQty = body.StockQty - inv.ReservedQty;
        inv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { inv.InventoryId, inv.StockQty, inv.AvailableQty });
    }
}

public record UpdateStockRequest(int StockQty);
