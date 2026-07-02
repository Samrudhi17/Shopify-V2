namespace QRShop.API.DTOs;

// Sent from the vendor Add Product form.
public record CreateProductRequest(
    int VendorId,
    int? CategoryId,
    string ProductName,
    string? ProductType,
    string? Description,
    string? Brand,
    string? Color,
    string? Size,
    decimal BasePrice,
    int Quantity,
    List<string>? ImageUrls);

// Sent from the Edit Product form.
public record UpdateProductRequest(
    int? CategoryId,
    string ProductName,
    string? ProductType,
    string? Description,
    string? Brand,
    string? Color,
    string? Size,
    decimal BasePrice,
    int Quantity,
    List<string>? ImageUrls);

public record ProductResponse(
    int ProductId,
    string ProductName,
    string? ProductType,
    string? Brand,
    string? Description,
    decimal BasePrice,
    int? CategoryId,
    string? CategoryName,
    string? Color,
    string? Size,
    int StockQty,
    string? PrimaryImageUrl,
    string Status);
