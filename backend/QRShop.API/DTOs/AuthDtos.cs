using System.ComponentModel.DataAnnotations;
using QRShop.API.Validation;

namespace QRShop.API.DTOs;

// Sent from the React Register page after Firebase account creation.
// Validation attributes must sit on the record's constructor PARAMETERS, not on
// the generated properties: MVC throws at request time if it finds validation
// metadata on a positional record's properties.
public record RegisterRequest(
    string FirebaseUid,

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150, ErrorMessage = "Name must be 150 characters or fewer.")]
    [RegularExpression(ValidationPatterns.FullName, ErrorMessage = ValidationPatterns.FullNameMessage)]
    string Name,

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(150, ErrorMessage = "Email must be 150 characters or fewer.")]
    [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailMessage)]
    string Email,

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string Phone,

    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    string? AlternatePhone,

    [Required(ErrorMessage = "Aadhaar number is required.")]
    [RegularExpression(ValidationPatterns.Aadhaar, ErrorMessage = ValidationPatterns.AadhaarMessage)]
    string AadhaarCardNo,

    [Required(ErrorMessage = "Address is required.")]
    [RegularExpression(ValidationPatterns.Address, ErrorMessage = ValidationPatterns.AddressMessage)]
    string Address);

public record UserProfileResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    string? ShopName);
