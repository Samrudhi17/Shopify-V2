using System.ComponentModel.DataAnnotations;
using QRShop.API.Validation;

namespace QRShop.API.DTOs;

// Sent from the React Register page after Firebase account creation.
// Attributes use the [property: ...] target so MVC's model validator sees them
// on the generated properties (a bare attribute would bind to the parameter).
public record RegisterRequest(
    string FirebaseUid,

    [property: Required(ErrorMessage = "Name is required.")]
    [property: StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be 2-150 characters.")]
    string Name,

    [property: Required(ErrorMessage = "Email is required.")]
    [property: StringLength(150, ErrorMessage = "Email must be 150 characters or fewer.")]
    [property: RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailMessage)]
    string Email,

    [property: Required(ErrorMessage = "Phone number is required.")]
    [property: RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string Phone,

    [property: RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string? AlternatePhone,

    [property: Required(ErrorMessage = "Aadhaar number is required.")]
    [property: RegularExpression(ValidationPatterns.Aadhaar, ErrorMessage = ValidationPatterns.AadhaarMessage)]
    string AadhaarCardNo,

    [property: Required(ErrorMessage = "Address is required.")]
    [property: StringLength(255, MinimumLength = 5, ErrorMessage = "Address must be 5-255 characters.")]
    string Address);

public record UserProfileResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    string? ShopName);
