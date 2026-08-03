// Registration (vendor sign-up). Creates a Firebase account, then posts the
// full profile to the backend. The backend makes the FIRST registered user an
// Admin and every subsequent user a Vendor.
//
// Every field validates as you type and shows its own error underneath. The
// rules live in constants/validation.js and are mirrored by the API.
import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { createUserWithEmailAndPassword } from "firebase/auth";
import { auth, isFirebaseConfigured } from "../../services/firebase";
import api from "../../services/api";
import Anim from "../../components/Anim";
import Field from "../../components/Field";
import {
  AADHAAR_FIELD, ADDRESS_FIELD, EMAIL_FIELD, NAME_FIELD, PHONE_FIELD,
  asName, onlyDigits, validateField, validateForm,
} from "../../constants/validation";

const initial = {
  name: "",
  email: "",
  password: "",
  confirmPassword: "",
  phone: "",
  alternatePhone: "",
  aadhaarCardNo: "",
  address: "",
};

// Order matters: the first invalid field gets focus on a blocked submit.
const FIELDS = [
  "name", "email", "password", "confirmPassword",
  "phone", "alternatePhone", "aadhaarCardNo", "address",
];

export default function Register() {
  const [form, setForm] = useState(initial);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const navigate = useNavigate();

  // `transform` normalizes as the user types (e.g. strip non-digits from a phone).
  const set = (key, transform) => (e) => {
    const value = transform ? transform(e.target.value) : e.target.value;
    const next = { ...form, [key]: value };
    setForm(next);

    setErrors((prev) => {
      const updated = { ...prev, [key]: validateField(key, value, next) };
      // Changing the password re-checks the confirmation against it, but only
      // once the user has actually started typing there.
      if (key === "password" && next.confirmPassword) {
        updated.confirmPassword = validateField("confirmPassword", next.confirmPassword, next);
      }
      return updated;
    });
  };

  const blur = (key) => () => {
    setTouched((t) => ({ ...t, [key]: true }));
    setErrors((prev) => ({ ...prev, [key]: validateField(key, form[key], form) }));
  };

  // Show a field's error once it has been blurred or typed into.
  const errorFor = (key) => ((touched[key] || form[key]) ? errors[key] || "" : "");

  const formIsValid = useMemo(
    () => Object.keys(validateForm(form, FIELDS)).length === 0,
    [form]
  );

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");

    const found = validateForm(form, FIELDS);
    if (Object.keys(found).length > 0) {
      setErrors(found);
      setTouched(Object.fromEntries(FIELDS.map((f) => [f, true])));
      document.querySelector(`[name="${FIELDS.find((f) => found[f])}"]`)?.focus();
      return;
    }

    if (!isFirebaseConfigured) {
      setError("Firebase is not configured yet. Add VITE_FIREBASE_* keys to frontend/.env.");
      return;
    }

    setSubmitting(true);
    try {
      const cred = await createUserWithEmailAndPassword(auth, form.email, form.password);
      const firebaseUid = cred.user.uid;

      // confirmPassword is a client-side check only — the API never sees it.
      const { confirmPassword: _confirmPassword, ...profile } = form;

      // Backend decides Admin (first user) vs Vendor and persists to MySQL.
      await api.post("/auth/register", { ...profile, firebaseUid });
      navigate("/login");
    } catch (err) {
      setError(err?.response?.data?.message || err.message || "Registration failed");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-wrap">
      <div className="auth-card" style={{ maxWidth: 480 }}>
        <div className="brand" style={{ marginBottom: 6 }}>
          <span className="brand-mark">▦</span> QR Digital Shop
        </div>
        <Anim name="account" size={130} style={{ margin: "0 auto 4px" }} />
        <h2>Create your account</h2>
        <p className="auth-sub">Register as a vendor and set up your shop.</p>

        <form onSubmit={handleSubmit} noValidate>
          <Field
            label="Name *" name="name" value={form.name}
            onChange={set("name", asName)} onBlur={blur("name")}
            error={errorFor("name")} {...NAME_FIELD}
          />
          <Field
            label="Email *" name="email" value={form.email}
            onChange={set("email")} onBlur={blur("email")}
            error={errorFor("email")} {...EMAIL_FIELD}
          />
          <Field
            label="Password *" name="password" value={form.password}
            onChange={set("password")} onBlur={blur("password")}
            error={errorFor("password")}
            type={showPassword ? "text" : "password"}
            placeholder="At least 8 characters"
            reveal={showPassword} onToggleReveal={() => setShowPassword((s) => !s)}
          />
          <Field
            label="Confirm Password *" name="confirmPassword" value={form.confirmPassword}
            onChange={set("confirmPassword")} onBlur={blur("confirmPassword")}
            error={errorFor("confirmPassword")}
            type={showConfirm ? "text" : "password"}
            placeholder="Re-enter your password"
            reveal={showConfirm} onToggleReveal={() => setShowConfirm((s) => !s)}
          />

          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <Field
              label="Phone Number *" name="phone" value={form.phone}
              onChange={set("phone", onlyDigits)} onBlur={blur("phone")}
              error={errorFor("phone")} {...PHONE_FIELD}
            />
            <Field
              label="Alternate Phone" name="alternatePhone" value={form.alternatePhone}
              onChange={set("alternatePhone", onlyDigits)} onBlur={blur("alternatePhone")}
              error={errorFor("alternatePhone")} {...PHONE_FIELD}
            />
          </div>

          <Field
            label="Aadhaar Card *" name="aadhaarCardNo" value={form.aadhaarCardNo}
            onChange={set("aadhaarCardNo", onlyDigits)} onBlur={blur("aadhaarCardNo")}
            error={errorFor("aadhaarCardNo")} {...AADHAAR_FIELD}
          />
          <Field
            label="Address *" name="address" value={form.address}
            onChange={set("address")} onBlur={blur("address")}
            error={errorFor("address")} {...ADDRESS_FIELD}
          />

          {error && <div className="alert alert-error">{error}</div>}
          <button type="submit" className="btn btn-primary btn-block" disabled={submitting || !formIsValid}>
            {submitting ? "Creating account…" : "Register"}
          </button>
        </form>

        <div className="auth-foot" style={{ justifyContent: "center", gap: 6 }}>
          <span>Already have an account?</span> <Link to="/login">Login</Link>
        </div>
      </div>
    </div>
  );
}

