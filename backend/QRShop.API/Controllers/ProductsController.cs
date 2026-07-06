using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.DTOs;
using QRShop.API.Models.Entities;

namespace QRShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db) => _db = db;

    // GET /api/products?vendorId=1 — products for the vendor's shop (newest first).
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int vendorId)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
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
        var product = await _db.Products.Where(p => p.ProductId == id).Select(Projection).FirstOrDefaultAsync();
        if (product is null) return NotFound();
        return Ok(product);
    }

    // POST /api/products — create a product with a variant, images, and stock.
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest req)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.VendorId == req.VendorId);
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
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest req)
    {
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
    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest body)
    {
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
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
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
        p.Status);
}
