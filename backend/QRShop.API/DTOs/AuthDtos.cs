namespace QRShop.API.DTOs;

// Sent from the React Register page after Firebase account creation.
public record RegisterRequest(
    string FirebaseUid,
    string Name,
    string Email,
    string Phone,
    string? AlternatePhone,
    string AadhaarCardNo,
    string Address);

public record UserProfileResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    string? ShopName);
