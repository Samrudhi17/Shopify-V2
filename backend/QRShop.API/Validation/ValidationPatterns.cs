namespace QRShop.API.Validation;

// Single source of truth for the field formats accepted by the API.
// The same rules are mirrored on the client in frontend/src/constants/validation.js —
// keep the two files in sync when a rule changes.
public static class ValidationPatterns
{
    // Aadhaar: exactly 12 digits. (UIDAI numbers never begin with 0 or 1; if you
    // want that stricter rule, change this to ^[2-9][0-9]{11}$.)
    public const string Aadhaar = @"^[0-9]{12}$";
    public const string AadhaarMessage = "Aadhaar number must be exactly 12 digits.";

    // PAN: five letters, four digits, one letter (e.g. ABCDE1234F). Uppercase only —
    // the client upper-cases the input before it is sent.
    public const string Pan = @"^[A-Z]{5}[0-9]{4}[A-Z]$";
    public const string PanMessage = "PAN must be 5 letters, 4 digits, then 1 letter (e.g. ABCDE1234F).";

    // Phone: exactly 10 digits. (For Indian mobiles only, tighten to ^[6-9][0-9]{9}$.)
    public const string Phone = @"^[0-9]{10}$";
    public const string PhoneMessage = "Phone number must be exactly 10 digits.";

    // Email: stricter than [EmailAddress], which accepts things like "a@b".
    // Requires a dotted domain with a 2+ character TLD.
    public const string Email = @"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)*\.[A-Za-z]{2,}$";
    public const string EmailMessage = "Enter a valid email address.";

    // Shop Act No: the format varies by state, so this only enforces a sane
    // shape — 5-50 characters of letters, digits, '/' or '-'.
    public const string ShopActNo = @"^[A-Za-z0-9/\-]{5,50}$";
    public const string ShopActNoMessage =
        "Shop Act number must be 5-50 characters using letters, digits, '/' or '-'.";
}
