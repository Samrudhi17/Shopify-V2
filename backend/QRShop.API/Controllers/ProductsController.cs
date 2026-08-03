using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.Filters;
using QRShop.API.DTOs;
using QRShop.API.Models.Entities;
using QRShop.API.Services;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _me;
    private readonly IAiDescriptionService _ai;
    private readonly ILogger<ProductsController> _log;

    public ProductsController(
        AppDbContext db, ICurrentUser me, IAiDescriptionService ai, ILogger<ProductsController> log)
    {
        _db = db;
        _me = me;
        _ai = ai;
        _log = log;
    }

    // POST /api/products/generate-description — draft copy for the product form.
    //
    // The AI key lives here and never reaches the browser. Gated on an active
    // subscription because every click costs money.
    [RequiresActiveSubscription]
    [HttpPost("generate-description")]
    public async Task<IActionResult> GenerateDescription(
        [FromBody] GenerateDescriptionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ProductName))
            return BadRequest(new { message = "Enter a product name first." });

        // The category is picked from a dropdown, so resolve the name for the
        // prompt rather than trusting a name sent by the client.
        string? categoryName = null;
        if (req.CategoryId is > 0)
        {
            categoryName = await _db.Categories
                .Where(c => c.CategoryId == req.CategoryId)
                .Select(c => c.CategoryName)
                .FirstOrDefaultAsync(ct);
        }

        var facts = new ProductFacts(
            req.ProductName.Trim(),
            req.ProductType,
            req.Brand,
            req.Color,
            req.Size,
            req.BasePrice,
            categoryName);

        try
        {
            var description = await _ai.GenerateAsync(facts, ct);
            return Ok(new { description });
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            // The provider's response can carry billing or key detail that has no
            // business reaching a vendor's browser.
            _log.LogError(ex, "AI description generation failed.");
            return StatusCode(502, new { message = "Could not generate a description right now. Please try again." });
        }
    }

    // The signed-in vendor's shop, or null if they have not created one yet.
    private async Task<Shop?> MyShopAsync()
    {
        var vendorId = await _me.GetVendorIdAsync();
        if (vendorId is null) return null;
        return await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
    }

    // Product ids are sequential, so every by-id endpoint has to confirm the row
    // belongs to the caller's shop before touching it.
    private async Task<bool> OwnsProductAsync(int productId)
    {
        var shop = await MyShopAsync();
        if (shop is null) return false;
        return await _db.Products.AnyAsync(p => p.ProductId == productId && p.ShopId == shop.ShopId);
    }

    // GET /api/products — products for the signed-in vendor's shop (newest first).
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var shop = await MyShopAsync();
        if (shop is null) return Ok(Array.Empty<object>());

        var products = await _db.Products
            .Where(p => p.ShopId == shop.ShopId)
            .OrderByDescending(p => p.ProductId)
            .Select(Projection)
            .ToListAsync();

        return Ok(products);
    }

    // GET /api/products/5 — single product (for the edit form).
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        if (!await OwnsProductAsync(id)) return NotFound();

        var product = await _db.Products.Where(p => p.ProductId == id).Select(Projection).FirstOrDefaultAsync();
        if (product is null) return NotFound();
        return Ok(product);
    }

    // POST /api/products — create a product with a variant, images, and stock.
    [RequiresActiveSubscription]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest req)
    {
        var shop = await MyShopAsync();
        if (shop is null)
            return BadRequest(new { message = "Create your shop first (Profile) before adding products." });

        if (string.IsNullOrWhiteSpace(req.ProductName))
            return BadRequest(new { message = "Product name is required." });

        var product = new Product
        {
            ShopId = shop.ShopId,
            CategoryId = req.CategoryId,
            ProductName = req.ProductName.Trim(),
            ProductType = req.ProductType?.Trim(),
            Description = req.Description,
            Brand = req.Brand,
            BasePrice = req.BasePrice,
            Status = "Active",
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.ProductId,
            Color = req.Color,
            Size = req.Size,
            Sku = $"SKU-{product.ProductId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            Price = req.BasePrice,
        };
        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync();

        // Stock comes from the required Quantity field.
        _db.Inventories.Add(new Inventory
        {
            VariantId = variant.VariantId,
            StockQty = req.Quantity,
            ReservedQty = 0,
            AvailableQty = req.Quantity,
        });

        if (req.ImageUrls is { Count: > 0 })
            for (var i = 0; i < req.ImageUrls.Count; i++)
                _db.ProductImages.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = req.ImageUrls[i], IsPrimary = i == 0 });

        // Record the initial stock movement.
        _db.StockHistory.Add(new StockHistory { VariantId = variant.VariantId, MovementType = "IN", Quantity = req.Quantity, Remarks = "Initial stock" });

        await _db.SaveChangesAsync();

        var result = await _db.Products.Where(p => p.ProductId == product.ProductId).Select(Projection).FirstAsync();
        return result;
    }

    // PUT /api/products/5 — update product, its variant, and stock.
    [RequiresActiveSubscription]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest req)
    {
        if (!await OwnsProductAsync(id)) return NotFound(new { message = "Product not found." });

        var product = await _db.Products
            .Include(p => p.Variants).ThenInclude(v => v.Inventory)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id);
        if (product is null) return NotFound(new { message = "Product not found." });

        product.CategoryId = req.CategoryId;
        product.ProductName = req.ProductName.Trim();
        product.ProductType = req.ProductType?.Trim();
        product.Description = req.Description;
        product.Brand = req.Brand;
        product.BasePrice = req.BasePrice;

        var variant = product.Variants.FirstOrDefault();
        if (variant is null)
        {
            variant = new ProductVariant { ProductId = product.ProductId, Sku = $"SKU-{product.ProductId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}" };
            _db.ProductVariants.Add(variant);
        }
        variant.Color = req.Color;
        variant.Size = req.Size;
        variant.Price = req.BasePrice;

        variant.Inventory ??= new Inventory { Variant = variant };
        variant.Inventory.StockQty = req.Quantity;
        variant.Inventory.AvailableQty = req.Quantity - variant.Inventory.ReservedQty;
        variant.Inventory.UpdatedAt = DateTime.UtcNow;

        // Replace images if a new set was provided.
        if (req.ImageUrls is { Count: > 0 })
        {
            _db.ProductImages.RemoveRange(product.Images);
            for (var i = 0; i < req.ImageUrls.Count; i++)
                _db.ProductImages.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = req.ImageUrls[i], IsPrimary = i == 0 });
        }

        await _db.SaveChangesAsync();
        var result = await _db.Products.Where(p => p.ProductId == id).Select(Projection).FirstAsync();
        return Ok(result);
    }

    // PATCH /api/products/5/stock  { "stockQty": 9 } — adjust stock from the product page.
    [RequiresActiveSubscription]
    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest body)
    {
        if (!await OwnsProductAsync(id)) return NotFound(new { message = "Product/variant not found." });

        var variant = await _db.ProductVariants
            .Include(v => v.Inventory)
            .Where(v => v.ProductId == id)
            .FirstOrDefaultAsync();
        if (variant is null) return NotFound(new { message = "Product/variant not found." });

        var newStock = Math.Max(0, body.StockQty);
        variant.Inventory ??= new Inventory { Variant = variant };
        var delta = newStock - variant.Inventory.StockQty;
        variant.Inventory.StockQty = newStock;
        variant.Inventory.AvailableQty = newStock - variant.Inventory.ReservedQty;
        variant.Inventory.UpdatedAt = DateTime.UtcNow;

        if (delta != 0)
            _db.StockHistory.Add(new StockHistory
            {
                VariantId = variant.VariantId,
                MovementType = delta > 0 ? "IN" : "OUT",
                Quantity = Math.Abs(delta),
                Remarks = "Manual adjust",
            });

        await _db.SaveChangesAsync();
        return Ok(new { productId = id, stockQty = newStock });
    }

    // DELETE /api/products/5 — remove a product and its variant/inventory/images.
    [RequiresActiveSubscription]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await OwnsProductAsync(id)) return NotFound(new { message = "Product not found." });

        var product = await _db.Products
            .Include(p => p.Variants).ThenInclude(v => v.Inventory)
            .Include(p => p.Variants).ThenInclude(v => v.StockHistory)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id);
        if (product is null) return NotFound(new { message = "Product not found." });

        foreach (var v in product.Variants)
        {
            if (v.Inventory is not null) _db.Inventories.Remove(v.Inventory);
            _db.StockHistory.RemoveRange(v.StockHistory);
        }
        _db.ProductVariants.RemoveRange(product.Variants);
        _db.ProductImages.RemoveRange(product.Images);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = id });
    }

    // Projection used by all read endpoints (first variant + its stock).
    private static readonly Expression<Func<Product, ProductResponse>> Projection = p => new ProductResponse(
        p.ProductId,
        p.ProductName,
        p.ProductType,
        p.Brand,
        p.Description,
        p.BasePrice,
        p.CategoryId,
        p.Category != null ? p.Category.CategoryName : null,
        p.Variants.Select(v => v.Color).FirstOrDefault(),
        p.Variants.Select(v => v.Size).FirstOrDefault(),
        p.Variants.Select(v => v.Inventory != null ? v.Inventory.StockQty : 0).FirstOrDefault(),
        p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
        // Primary first, then the rest in upload order.
        p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.ImageId)
            .Select(i => i.ImageUrl).ToList(),
        p.Status);
}
