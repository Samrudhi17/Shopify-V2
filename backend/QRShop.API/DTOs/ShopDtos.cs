namespace QRShop.API.DTOs;

// Sent from the vendor Profile (shop registration) form.
public record CreateShopRequest(
    int VendorId,
    string ShopName,
    string? AadhaarCardNo,
    string? PancardNo,
    string? ShopActNo,
    string? ShopActCertificateUrl,
    string Address,
    string Phone,
    string? AlternateNumber,
    string? LogoUrl);

public record UpdateShopDetailsRequest(
    string Phone,
    string? AlternateNumber,
    string Address);

public record ShopResponse(
    int ShopId,
    string ShopName,
    string Slug,
    string CatalogUrl,
    string? QrImagePath,
    string? LogoUrl,
    string Status);

// Full shop details for the vendor Profile page (read view).
public record ShopDetailsResponse(
    int ShopId,
    string ShopName,
    string? ShopType,
    string Address,
    string Phone,
    string? AlternateNumber,
    string? AadhaarCardNo,
    string? PancardNo,
    string? ShopActNo,
    string? ShopActCertificateUrl,
    string? LogoUrl,
    string Slug,
    string CatalogUrl,
    string? QrImagePath,
    string Status);
