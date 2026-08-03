namespace QRShop.API.Validation;

// Single source of truth for the field formats accepted by the API.
// The same rules are mirrored on the client in frontend/src/constants/validation.js —
// keep the two files in sync when a rule changes.
public static class ValidationPatterns
{
    // Full name: two or more words, letters only (apostrophes/hyphens/dots may
    // appear inside a word) — "Samrudhi Pawar" passes, "Samrudhi" does not.
    public const string FullName = @"^[A-Za-z]+(?:['.\-][A-Za-z]+)*(?:\s+[A-Za-z]+(?:['.\-][A-Za-z]+)*)+$";
    public const string FullNameMessage = "Enter your full name — first and last (e.g. Samrudhi Pawar).";

    // Aadhaar: 12 digits, first digit 2-9 (UIDAI never issues numbers starting
    // 0 or 1), and not the same digit repeated twelve times — so 000000000000,
    // 111111111111 and 222222222222 are all rejected.
    public const string Aadhaar = @"^(?!([0-9])\1{11})[2-9][0-9]{11}$";
    public const string AadhaarMessage =
        "Enter a valid 12-digit Aadhaar number (it cannot start with 0 or 1, or repeat one digit).";

    // Address: 10-255 address-ish characters containing at least 5 letters, so
    // "1234567890" or punctuation runs cannot pass as an address.
    public const string Address = @"^(?=(?:[^A-Za-z]*[A-Za-z]){5,})[A-Za-z0-9\s,./#&()'\-]{10,255}$";
    public const string AddressMessage =
        "Enter a fuller address — at least 10 characters including a street or area name.";

    // PAN: five letters, four digits, one letter (e.g. ABCDE1234F). Uppercase only —
    // the client upper-cases the input before it is sent.
    public const string Pan = @"^[A-Z]{5}[0-9]{4}[A-Z]$";
    public const string PanMessage = "PAN must be 5 letters, 4 digits, then 1 letter (e.g. ABCDE1234F).";

    // Phone: Indian mobile — 10 digits starting 6-9, and not the same digit ten
    // times, so 9999999999 and anything starting 0-5 are rejected.
    public const string Phone = @"^(?!([0-9])\1{9})[6-9][0-9]{9}$";
    public const string PhoneMessage =
        "Enter a valid 10-digit mobile number (it must start with 6-9 and cannot repeat one digit).";

    // Email: stricter than [EmailAddress], which accepts things like "a@b".
    // The local part may not start/end with a dot or contain "..", and domain
    // labels after the first must be 2+ characters — so "gmail.c.om" is rejected
    // while "x.com" and "example.co.in" still pass.
    public const string Email =
        @"^[A-Za-z0-9_%+\-]+(\.[A-Za-z0-9_%+\-]+)*@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]{2,})*\.[A-Za-z]{2,}$";
    public const string EmailMessage = "Enter a valid email address.";

    // Shop Act / Gumasta number. Each state and municipal body sets its own
    // layout, so this enforces structure rather than one state's format:
    // 8-30 chars of A-Z/0-9 in groups separated by a single '/' or '-', starting
    // and ending alphanumeric, with at least 4 digits overall.
    // Accepts PUNE/1/2023/0012345, SA/2024/12345, 3564782356487645.
    // Rejects SA1, AB//123, ----, ABCDEFGH and trailing separators.
    // Case-insensitive here (the client upper-cases as you type, and the
    // controller normalizes before saving) so a lower-case API call is not
    // rejected for a purely cosmetic reason.
    public const string ShopActNo =
        @"^(?=.{8,30}$)(?=(?:[^0-9]*[0-9]){4,})[A-Za-z0-9]+(?:[/-][A-Za-z0-9]+)*$";
    public const string ShopActNoMessage =
        "Enter the Shop Act number as printed on your licence — 8-30 characters, letters and digits separated by '/' or '-' (e.g. PUNE/1/2023/0012345).";
}
