namespace QRShop.API.DTOs;

// One card in the public shop directory (the "browse shops" page).
// Deliberately excludes Aadhaar/PAN/Shop Act and the owner's identity — this is
// served unauthenticated, so it carries only what a customer needs.
public record PublicShopSummary(
    int ShopId,
    string ShopName,
    string Slug,
    string? LogoUrl,
    string Address,
    string Phone,
    string? ShopType,
    int ProductCount);

public record CatalogShop(
    string ShopName,
    string? LogoUrl,
    string Phone,
    string? AlternateNumber,
    string Address,
    string? CatalogUrl);

public record CatalogProduct(
    int ProductId,
    string ProductName,
    string? ProductType,
    string? Brand,
    string? Description,
    decimal BasePrice,
    string? CategoryName,
    string? Color,
    string? Size,
    int AvailableQty,
    string? ImageUrl,
    // Every image, primary first — the catalog card carousel scrolls through these.
    List<string> ImageUrls);
