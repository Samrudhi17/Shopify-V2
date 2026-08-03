using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    // GET /api/admin/stats — dashboard cards (updates as vendors/shops register).
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var totalVendors = await _db.Vendors.CountAsync();
        var totalShops = await _db.Shops.CountAsync();
        var activeShops = await _db.Shops.CountAsync(s => s.Status == "Active");
        return Ok(new { totalVendors, totalShops, activeShops, inactiveShops = totalShops - activeShops });
    }

    // GET /api/admin/vendors — vendor list with their shop.
    [HttpGet("vendors")]
    public async Task<IActionResult> Vendors()
    {
        var vendors = await _db.Vendors
            .OrderByDescending(v => v.VendorId)
            .Select(v => new
            {
                v.VendorId, v.Name, v.Email, v.Phone, v.Status,
                ShopName = v.Shops.Select(s => s.ShopName).FirstOrDefault(),
                ShopStatus = v.Shops.Select(s => s.Status).FirstOrDefault(),
            })
            .ToListAsync();
        return Ok(vendors);
    }

    // GET /api/admin/shops — all shops with owner name.
    [HttpGet("shops")]
    public async Task<IActionResult> Shops()
    {
        var shops = await _db.Shops
            .OrderByDescending(s => s.ShopId)
            .Select(s => new
            {
                s.ShopId, s.ShopName, s.Slug, s.Phone, s.Address, s.Status, s.CatalogUrl,
                VendorName = s.Vendor!.Name,
            })
            .ToListAsync();
        return Ok(shops);
    }

    // PUT /api/admin/shops/5/status  { "status": "Active" | "Inactive" }
    [HttpPut("shops/{id:int}/status")]
    public async Task<IActionResult> SetShopStatus(int id, [FromBody] AdminStatusRequest body)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop is null) return NotFound(new { message = "Shop not found." });
        shop.Status = body.Status;
        await _db.SaveChangesAsync();
        return Ok(new { shop.ShopId, shop.Status });
    }

    // Vendors are deliberately not deactivable: an admin controls access by
    // activating/deactivating the vendor's SHOP (above), which is what hides the
    // public catalog. There is intentionally no vendors/{id}/status endpoint.

    // GET /api/admin/admins — for the roles & permissions settings page.
    [HttpGet("admins")]
    public async Task<IActionResult> Admins()
    {
        var admins = await _db.Admins.Select(a => new { a.AdminId, a.Name, a.Email, a.Role }).ToListAsync();
        return Ok(admins);
    }
}

public record AdminStatusRequest(string Status);
