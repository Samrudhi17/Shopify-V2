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
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _me;

    public CategoriesController(AppDbContext db, ICurrentUser me)
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

    // GET /api/categories — categories for the signed-in vendor's shop.
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var shop = await MyShopAsync();
        if (shop is null)
            return Ok(Array.Empty<object>());

        var cats = await _db.Categories
            .Where(c => c.ShopId == shop.ShopId)
            .Select(c => new { c.CategoryId, c.CategoryName, c.Status })
            .ToListAsync();

        return Ok(cats);
    }

    // PUT /api/categories/5  { "status": "Active" | "Inactive" }
    // Used by the Categories page to select/deselect a single category.
    [RequiresActiveSubscription]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCategoryStatusRequest body)
    {
        var shop = await MyShopAsync();
        if (shop is null)
            return BadRequest(new { message = "No shop for this vendor." });

        // Scoped to the caller's shop, so another vendor's category id reads as a
        // 404 instead of being edited.
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && c.ShopId == shop.ShopId);
        if (cat is null)
            return NotFound(new { message = "Category not found." });

        cat.Status = body.Status;
        await _db.SaveChangesAsync();
        return Ok(new { cat.CategoryId, cat.Status });
    }

    // POST /api/categories/select  { selectedIds:[1,2] }
    // Marks the selected categories Active and the rest Inactive, then saves.
    [RequiresActiveSubscription]
    [HttpPost("select")]
    public async Task<IActionResult> SaveSelection([FromBody] SaveCategorySelectionRequest body)
    {
        var shop = await MyShopAsync();
        if (shop is null)
            return BadRequest(new { message = "No shop for this vendor." });

        var cats = await _db.Categories.Where(c => c.ShopId == shop.ShopId).ToListAsync();
        var selected = body.SelectedIds ?? new List<int>();
        foreach (var c in cats)
            c.Status = selected.Contains(c.CategoryId) ? "Active" : "Inactive";

        await _db.SaveChangesAsync();
        return Ok(cats.Select(c => new { c.CategoryId, c.Status }));
    }
}

public record UpdateCategoryStatusRequest(string Status);

// VendorId is still accepted so the existing client payload binds, but it is
// ignored — the shop is resolved from the bearer token.
public record SaveCategorySelectionRequest(int VendorId, List<int>? SelectedIds);
