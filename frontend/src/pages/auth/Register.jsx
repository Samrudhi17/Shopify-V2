// Registration (vendor sign-up). Creates a Firebase account, then posts the
// full profile to the backend. The backend makes the FIRST registered user an
// Admin and every subsequent user a Vendor.
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { createUserWithEmailAndPassword } from "firebase/auth";
import { auth, isFirebaseConfigured } from "../../services/firebase";
import api from "../../services/api";
import Anim from "../../components/Anim";
import {
  AADHAAR_FIELD, EMAIL_FIELD, PHONE_FIELD, onlyDigits,
} from "../../constants/validation";

const initial = {
  name: "",
  email: "",
  password: "",
  phone: "",
  alternatePhone: "",
  aadhaarCardNo: "",
  address: "",
};

export default function Register() {
  const [form, setForm] = useState(initial);
  const [error, setError] = useState("");
  const navigate = useNavigate();

  // `transform` normalizes as the user types (e.g. strip non-digits from a phone).
  const set = (k, transform) => (e) =>
    setForm({ ...form, [k]: transform ? transform(e.target.value) : e.target.value });

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    if (!isFirebaseConfigured) {
      setError("Firebase is not configured yet. Add VITE_FIREBASE_* keys to frontend/.env.");
      return;
    }
    try {
      const cred = await createUserWithEmailAndPassword(auth, form.email, form.password);
      const firebaseUid = cred.user.uid;

      // Backend decides Admin (first user) vs Vendor and persists to SQL Server.
      await api.post("/auth/register", { ...form, firebaseUid });
      navigate("/login");
    } catch (err) {
      setError(err?.response?.data?.message || err.message || "Registration failed");
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

        <form onSubmit={handleSubmit}>
          <Field label="Name *" value={form.name} onChange={set("name")} required maxLength={150} />
          <Field label="Email *" value={form.email} onChange={set("email")} required {...EMAIL_FIELD} />
          <Field label="Password *" type="password" value={form.password} onChange={set("password")} required />
          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <Field label="Phone Number *" value={form.phone} onChange={set("phone", onlyDigits)} required {...PHONE_FIELD} />
            <Field label="Alternate Phone" value={form.alternatePhone} onChange={set("alternatePhone", onlyDigits)} {...PHONE_FIELD} />
          </div>
          <Field label="Aadhaar Card *" value={form.aadhaarCardNo} onChange={set("aadhaarCardNo", onlyDigits)} required {...AADHAAR_FIELD} />
          <Field label="Address *" value={form.address} onChange={set("address")} required minLength={5} maxLength={255} />
          {error && <div className="alert alert-error">{error}</div>}
          <button type="submit" className="btn btn-primary btn-block">Register</button>
        </form>

        <div className="auth-foot" style={{ justifyContent: "center", gap: 6 }}>
          <span>Already have an account?</span> <Link to="/login">Login</Link>
        </div>
      </div>
    </div>
  );
}

function Field({ label, type = "text", ...props }) {
  return (
    <label className="field">
      <span>{label}</span>
      <input className="input" type={type} {...props} />
    </label>
  );
}
