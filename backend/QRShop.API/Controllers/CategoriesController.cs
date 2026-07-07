using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;

    // GET /api/categories?vendorId=1 — categories for the vendor's shop.
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int vendorId)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
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
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCategoryStatusRequest body)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is null)
            return NotFound(new { message = "Category not found." });

        cat.Status = body.Status;
        await _db.SaveChangesAsync();
        return Ok(new { cat.CategoryId, cat.Status });
    }

    // POST /api/categories/select  { vendorId, selectedIds:[1,2] }
    // Marks the selected categories Active and the rest Inactive, then saves.
    [HttpPost("select")]
    public async Task<IActionResult> SaveSelection([FromBody] SaveCategorySelectionRequest body)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == body.VendorId);
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
public record SaveCategorySelectionRequest(int VendorId, List<int>? SelectedIds);
