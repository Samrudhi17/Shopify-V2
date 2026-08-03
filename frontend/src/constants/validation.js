// Field format rules for every form in the app.
//
// These mirror backend/QRShop.API/Validation/ValidationPatterns.cs — keep the
// two files in sync when a rule changes. The API re-validates everything, so
// these exist to give immediate, readable feedback rather than to be trusted.
//
// The strings are HTML `pattern` values, which the browser anchors implicitly
// (it matches against ^(?:...)$), so no ^ or $ here.

export const PATTERNS = {
  // Two or more words, letters only (apostrophes/hyphens/dots allowed inside a
  // word) — so "Samrudhi Pawar" passes but "Samrudhi" alone does not.
  fullName: "[A-Za-z]+(?:['.\\-][A-Za-z]+)*(?:\\s+[A-Za-z]+(?:['.\\-][A-Za-z]+)*)+",

  // 12 digits, first digit 2-9 (UIDAI never issues numbers starting 0 or 1),
  // and not the same digit twelve times — rejects 000000000000, 111111111111,
  // 222222222222 and friends.
  aadhaar: "(?!([0-9])\\1{11})[2-9][0-9]{11}",

  pan: "[A-Z]{5}[0-9]{4}[A-Z]",

  // Indian mobile: 10 digits starting 6-9, and not the same digit ten times —
  // rejects 9999999999, 8888888888 and any number starting 0-5.
  phone: "(?!([0-9])\\1{9})[6-9][0-9]{9}",
  // Local part may not start/end with a dot or contain "..". Domain labels after
  // the first must be 2+ characters, which rejects typos like "gmail.c.om" while
  // still allowing short real domains ("x.com") and multi-level TLDs (".co.in").
  email: "[A-Za-z0-9_%+\\-]+(\\.[A-Za-z0-9_%+\\-]+)*@[A-Za-z0-9\\-]+(\\.[A-Za-z0-9\\-]{2,})*\\.[A-Za-z]{2,}",
  // Shop Act / Gumasta number. The exact format is set by each state and
  // municipal body, so this enforces structure rather than one state's layout:
  // 8-30 chars of A-Z/0-9 in groups separated by a single "/" or "-", starting
  // and ending alphanumeric, with at least 4 digits overall.
  // Accepts PUNE/1/2023/0012345, SA/2024/12345, 3564782356487645.
  // Rejects SA1, AB//123, ----, ABCDEFGH, trailing separators.
  shopActNo: "(?=.{8,30}$)(?=(?:[^0-9]*[0-9]){4,})[A-Z0-9]+(?:[/-][A-Z0-9]+)*",

  // 10-255 chars of address-ish characters, containing at least 5 letters so
  // "1234567890" or ",,,,,,,,,," can't pass as an address.
  address: "(?=(?:[^A-Za-z]*[A-Za-z]){5,})[A-Za-z0-9\\s,./#&()'\\-]{10,255}",

  // At least 8 characters with a letter and a digit.
  password: "(?=.*[A-Za-z])(?=.*[0-9]).{8,}",

  // Shop name: 2-150 chars containing at least 2 letters, so "123" or "&&&"
  // can't be registered as a shop name.
  shopName: "(?=(?:[^A-Za-z]*[A-Za-z]){2,})[A-Za-z0-9\\s&.'\\-]{2,150}",
};

export const MESSAGES = {
  fullName: "Enter your full name — first and last (e.g. Samrudhi Pawar).",
  aadhaar: "Enter a valid 12-digit Aadhaar number (it cannot start with 0 or 1, or repeat one digit).",
  pan: "PAN must be 5 letters, 4 digits, then 1 letter (e.g. ABCDE1234F).",
  phone: "Enter a valid 10-digit mobile number (it must start with 6-9 and cannot repeat one digit).",
  email: "Enter a valid email address (e.g. you@example.com).",
  shopActNo: "Enter the Shop Act number as printed on your licence — 8-30 characters, letters and digits separated by '/' or '-' (e.g. PUNE/1/2023/0012345).",
  address: "Enter a fuller address — at least 10 characters including a street or area name.",
  password: "Password must be at least 8 characters and include a letter and a number.",
  confirmPassword: "Passwords do not match.",
  shopName: "Shop name must be 2-150 characters and include at least two letters.",
  shopNameTaken: "That shop name is already taken. Try another.",
  required: "This field is required.",
};

// Input transforms, applied as the user types so the value can only ever be in
// a shape the pattern accepts.
export const onlyDigits = (v) => v.replace(/\D/g, "");

// Strips leading and trailing whitespace when the field loses focus, by
// re-firing the caller's own onChange with the trimmed value — so validation,
// transforms and state all run exactly as they do for a normal keystroke.
//
// On blur rather than on change: trimming every keystroke would swallow the
// space in "John Doe" the moment it was typed, making the field impossible to
// use. Passwords are left alone — a trailing space can be deliberate there, and
// silently removing it would lock someone out of an account they set up.
export function trimOnBlur({ onBlur, onChange, name }, type) {
  return (e) => {
    const raw = e.target.value;
    const trimmed = typeof raw === "string" ? raw.trim() : raw;

    if (type !== "password" && onChange && trimmed !== raw) {
      onChange({ ...e, target: { ...e.target, name: name ?? e.target.name, value: trimmed } });
    }

    onBlur?.(e);
  };
}

export const asPan = (v) => v.toUpperCase().replace(/[^A-Z0-9]/g, "");
// Licence numbers are printed in upper case; normalize so the pattern (A-Z only)
// doesn't punish someone typing lower case, and drop characters that can't appear.
export const asShopActNo = (v) => v.toUpperCase().replace(/[^A-Z0-9/-]/g, "");
// Names: letters plus the punctuation that legitimately appears in them. Collapse
// runs of spaces so "Samrudhi   Pawar" doesn't look like a typo mid-typing.
export const asName = (v) => v.replace(/[^A-Za-z'.\-\s]/g, "").replace(/\s{2,}/g, " ");

const anchored = (p) => new RegExp(`^(?:${p})$`);

// Validates one field and returns an error string, or "" when it is valid.
// `form` is the whole form object so rules can compare fields (confirmPassword).
export function validateField(name, value, form = {}) {
  const v = (value ?? "").trim();

  switch (name) {
    case "name":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.fullName).test(v) ? "" : MESSAGES.fullName;

    case "email":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.email).test(v) ? "" : MESSAGES.email;

    case "password":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.password).test(v) ? "" : MESSAGES.password;

    case "confirmPassword":
      if (!v) return MESSAGES.required;
      return v === (form.password ?? "").trim() ? "" : MESSAGES.confirmPassword;

    case "phone":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.phone).test(v) ? "" : MESSAGES.phone;

    // Optional: only checked once the user has typed something.
    case "alternatePhone":
    case "alternateNumber":
      if (!v) return "";
      return anchored(PATTERNS.phone).test(v) ? "" : MESSAGES.phone;

    case "shopName":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.shopName).test(v) ? "" : MESSAGES.shopName;

    case "pancardNo":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.pan).test(v) ? "" : MESSAGES.pan;

    case "shopActNo":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.shopActNo).test(v) ? "" : MESSAGES.shopActNo;

    case "aadhaarCardNo":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.aadhaar).test(v) ? "" : MESSAGES.aadhaar;

    case "address":
      if (!v) return MESSAGES.required;
      return anchored(PATTERNS.address).test(v) ? "" : MESSAGES.address;

    default:
      return "";
  }
}

// ---------------------------------------------------------------------------
// File uploads
// ---------------------------------------------------------------------------
// Mirrored by FileStorageService.cs, which re-checks every upload server-side.
export const FILE_RULES = {
  certificate: {
    label: "Shop Act certificate",
    accept: ".pdf,.jpg,.jpeg,.png,.webp",
    extensions: ["pdf", "jpg", "jpeg", "png", "webp"],
    maxBytes: 3 * 1024 * 1024,
  },
  logo: {
    label: "Logo",
    accept: ".jpg,.jpeg,.png,.webp",
    extensions: ["jpg", "jpeg", "png", "webp"],
    maxBytes: 500 * 1024,
    minDimension: 100,
    maxDimension: 3000,
  },
  product: {
    label: "Product image",
    accept: ".jpg,.jpeg,.png,.webp",
    extensions: ["jpg", "jpeg", "png", "webp"],
    maxBytes: 500 * 1024,
  },
};

export function formatBytes(bytes) {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(bytes % (1024 * 1024) ? 1 : 0)} MB`;
  return `${Math.round(bytes / 1024)} KB`;
}

// Size + type checks. Returns "" when the file is acceptable.
export function validateFile(file, rule) {
  if (!file) return "";
  const ext = (file.name.split(".").pop() || "").toLowerCase();
  if (!rule.extensions.includes(ext)) {
    return `${rule.label} must be a ${rule.extensions.join(", ")} file.`;
  }
  if (file.size === 0) return `${rule.label} appears to be empty.`;
  if (file.size > rule.maxBytes) {
    return `${rule.label} must be ${formatBytes(rule.maxBytes)} or smaller — this file is ${formatBytes(file.size)}.`;
  }
  return "";
}

// Reads an image's pixel dimensions without uploading it.
function readImageDimensions(file) {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const img = new Image();
    img.onload = () => { URL.revokeObjectURL(url); resolve({ width: img.naturalWidth, height: img.naturalHeight }); };
    img.onerror = () => { URL.revokeObjectURL(url); reject(new Error("unreadable")); };
    img.src = url;
  });
}

// Size + type + pixel dimensions. Async because the image has to be decoded.
export async function validateImageFile(file, rule) {
  const basic = validateFile(file, rule);
  if (basic || !file || !rule.minDimension) return basic;

  try {
    const { width, height } = await readImageDimensions(file);
    if (width < rule.minDimension || height < rule.minDimension) {
      return `${rule.label} must be at least ${rule.minDimension}×${rule.minDimension} pixels — this image is ${width}×${height}.`;
    }
    if (width > rule.maxDimension || height > rule.maxDimension) {
      return `${rule.label} must be no larger than ${rule.maxDimension}×${rule.maxDimension} pixels — this image is ${width}×${height}.`;
    }
  } catch {
    return `${rule.label} could not be read — it may be corrupt or not a real image.`;
  }
  return "";
}

// Runs validateField across a whole form; returns { field: message } for failures.
export function validateForm(form, fields) {
  const errors = {};
  for (const f of fields) {
    const msg = validateField(f, form[f], form);
    if (msg) errors[f] = msg;
  }
  return errors;
}

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

// Indian mobile numbers only: the pattern already enforces 10 digits starting
// 6-9, so the +91 is shown as a fixed prefix rather than something to type. It
// is display only — the value stays the bare 10 digits, which is what the API
// validates and the database stores.
export const PHONE_FIELD = {
  pattern: PATTERNS.phone,
  title: MESSAGES.phone,
  inputMode: "tel",
  maxLength: 10,
  placeholder: "98765 43210",
  prefix: "+91",
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
  maxLength: 30,
  placeholder: "e.g. PUNE/1/2023/0012345",
};

export const NAME_FIELD = {
  pattern: PATTERNS.fullName,
  title: MESSAGES.fullName,
  maxLength: 150,
  placeholder: "e.g. Samrudhi Pawar",
};

export const ADDRESS_FIELD = {
  pattern: PATTERNS.address,
  title: MESSAGES.address,
  maxLength: 255,
  placeholder: "House / street, area, city",
};

export const SHOP_NAME_FIELD = {
  pattern: PATTERNS.shopName,
  title: MESSAGES.shopName,
  maxLength: 150,
  placeholder: "e.g. Gokul Cloth Store",
};
