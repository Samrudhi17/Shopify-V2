namespace QRShop.API.DTOs;

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
    string? ImageUrl);
