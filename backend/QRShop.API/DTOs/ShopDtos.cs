using System.ComponentModel.DataAnnotations;
using QRShop.API.Validation;

namespace QRShop.API.DTOs;

// Sent from the vendor Profile (shop registration) form.
// The optional fields keep their nullable type: RegularExpression passes on null,
// so the format is only enforced when a value is actually supplied.
public record CreateShopRequest(
    int VendorId,

    [Required(ErrorMessage = "Shop name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Shop name must be 2-150 characters.")]
    string ShopName,

    [RegularExpression(ValidationPatterns.Aadhaar, ErrorMessage = ValidationPatterns.AadhaarMessage)]
    string? AadhaarCardNo,

    [RegularExpression(ValidationPatterns.Pan, ErrorMessage = ValidationPatterns.PanMessage)]
    string? PancardNo,

    [RegularExpression(ValidationPatterns.ShopActNo, ErrorMessage = ValidationPatterns.ShopActNoMessage)]
    string? ShopActNo,

    string? ShopActCertificateUrl,

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Address must be 5-255 characters.")]
    string Address,

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string Phone,

    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string? AlternateNumber,

    string? LogoUrl);

public record UpdateShopDetailsRequest(
    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string Phone,

    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string? AlternateNumber,

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Address must be 5-255 characters.")]
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
