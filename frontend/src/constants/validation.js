// Field format rules for every form in the app.
//
// These mirror backend/QRShop.API/Validation/ValidationPatterns.cs — keep the
// two files in sync when a rule changes. The API re-validates everything, so
// these exist to give immediate, readable feedback rather than to be trusted.
//
// The strings are HTML `pattern` values, which the browser anchors implicitly
// (it matches against ^(?:...)$), so no ^ or $ here.

export const PATTERNS = {
  aadhaar: "[0-9]{12}",
  pan: "[A-Z]{5}[0-9]{4}[A-Z]",
  phone: "[0-9]{10}",
  email: "[^@\\s]+@[^@\\s.]+(\\.[^@\\s.]+)*\\.[A-Za-z]{2,}",
  shopActNo: "[A-Za-z0-9/\\-]{5,50}",
};

export const MESSAGES = {
  aadhaar: "Aadhaar number must be exactly 12 digits.",
  pan: "PAN must be 5 letters, 4 digits, then 1 letter (e.g. ABCDE1234F).",
  phone: "Phone number must be exactly 10 digits.",
  email: "Enter a valid email address (e.g. you@example.com).",
  shopActNo: "Shop Act number must be 5-50 characters using letters, digits, '/' or '-'.",
};

// Input transforms, applied as the user types so the value can only ever be in
// a shape the pattern accepts.
export const onlyDigits = (v) => v.replace(/\D/g, "");
export const asPan = (v) => v.toUpperCase().replace(/[^A-Z0-9]/g, "");
export const asShopActNo = (v) => v.replace(/[^A-Za-z0-9/-]/g, "");

// Prop bundles spread onto <input> so each field is declared once, not per form.
// `title` is what the browser shows when the pattern fails.
export const AADHAAR_FIELD = {
  pattern: PATTERNS.aadhaar,
  title: MESSAGES.aadhaar,
  inputMode: "numeric",
  maxLength: 12,
  placeholder: "12-digit number",
};

export const PAN_FIELD = {
  pattern: PATTERNS.pan,
  title: MESSAGES.pan,
  maxLength: 10,
  placeholder: "ABCDE1234F",
};

export const PHONE_FIELD = {
  pattern: PATTERNS.phone,
  title: MESSAGES.phone,
  inputMode: "tel",
  maxLength: 10,
  placeholder: "10-digit number",
};

export const EMAIL_FIELD = {
  type: "email",
  pattern: PATTERNS.email,
  title: MESSAGES.email,
  maxLength: 150,
  placeholder: "you@example.com",
};

export const SHOP_ACT_FIELD = {
  pattern: PATTERNS.shopActNo,
  title: MESSAGES.shopActNo,
  maxLength: 50,
  placeholder: "e.g. SA/2024/12345",
};
