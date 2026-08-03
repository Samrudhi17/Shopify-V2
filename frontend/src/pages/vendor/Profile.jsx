// Shop registration / profile. If the vendor already has a shop, show all its
// details + catalog URL + QR code. Otherwise show the registration form.
import { useEffect, useMemo, useRef, useState } from "react";
import api from "../../services/api";
import { uploadFile } from "../../services/uploads";
import { useAuth } from "../../context/AuthContext";
import Anim from "../../components/Anim";
import Field from "../../components/Field";
import {
  AADHAAR_FIELD, ADDRESS_FIELD, PAN_FIELD, PHONE_FIELD, SHOP_ACT_FIELD, SHOP_NAME_FIELD,
  FILE_RULES, MESSAGES, formatBytes, onlyDigits, asPan, asShopActNo,
  validateField, validateFile, validateImageFile, validateForm,
} from "../../constants/validation";
import usePageTitle from "../../hooks/usePageTitle";

const initial = {
  shopName: "",
  aadhaarCardNo: "",
  pancardNo: "",
  shopActNo: "",
  address: "",
  phone: "",
  alternateNumber: "",
};

// Order matters: the first invalid field gets focus on a blocked submit.
const FIELDS = [
  "shopName", "aadhaarCardNo", "pancardNo", "shopActNo",
  "address", "phone", "alternateNumber",
];

export default function Profile() {
  usePageTitle("Profile");
  const { user } = useAuth();
  const [shop, setShop] = useState(null);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState(initial);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});
  const [certificate, setCertificate] = useState(null);
  const [certificateError, setCertificateError] = useState("");
  const [logo, setLogo] = useState(null);
  const [logoError, setLogoError] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [justCreated, setJustCreated] = useState(false);

  // Live shop-name availability: "idle" | "checking" | "available" | "taken".
  const [nameStatus, setNameStatus] = useState("idle");
  const nameCheckId = useRef(0);

  // `transform` normalizes as the user types (e.g. upper-case a PAN).
  const set = (k, transform) => (e) => {
    const value = transform ? transform(e.target.value) : e.target.value;
    setForm((f) => ({ ...f, [k]: value }));
    setErrors((prev) => ({ ...prev, [k]: validateField(k, value, { ...form, [k]: value }) }));
    if (k === "shopName") setNameStatus("idle");
  };

  const blur = (k) => () => {
    setTouched((t) => ({ ...t, [k]: true }));
    setErrors((prev) => ({ ...prev, [k]: validateField(k, form[k], form) }));
  };

  const errorFor = (k) => {
    if (!(touched[k] || form[k])) return "";
    if (k === "shopName" && !errors.shopName && nameStatus === "taken") return MESSAGES.shopNameTaken;
    return errors[k] || "";
  };

  // Ask the API whether the shop name is free, 500ms after typing stops.
  // Each request carries a sequence number so a slow earlier reply cannot
  // overwrite the result of a later one.
  useEffect(() => {
    const name = form.shopName.trim();
    if (validateField("shopName", name)) { setNameStatus("idle"); return; }

    const id = ++nameCheckId.current;
    setNameStatus("checking");
    const timer = setTimeout(() => {
      api.get(`/shops/name-available?name=${encodeURIComponent(name)}`)
        .then((r) => {
          if (id !== nameCheckId.current) return;
          setNameStatus(r.data.available ? "available" : "taken");
        })
        .catch(() => { if (id === nameCheckId.current) setNameStatus("idle"); });
    }, 500);

    return () => clearTimeout(timer);
  }, [form.shopName]);

  // Certificate is required; logo is optional but must be valid if chosen.
  function pickCertificate(e) {
    const file = e.target.files[0] || null;
    setCertificate(file);
    setCertificateError(file ? validateFile(file, FILE_RULES.certificate) : "");
  }

  async function pickLogo(e) {
    const file = e.target.files[0] || null;
    setLogo(file);
    setLogoError(file ? await validateImageFile(file, FILE_RULES.logo) : "");
  }

  const formIsValid = useMemo(
    () =>
      Object.keys(validateForm(form, FIELDS)).length === 0 &&
      nameStatus === "available" &&
      Boolean(certificate) && !certificateError && !logoError,
    [form, nameStatus, certificate, certificateError, logoError]
  );

  // Load the vendor's existing shop (if any).
  useEffect(() => {
    if (!user?.id) return;
    api.get(`/shops/vendor/${user.id}`)
      .then((r) => setShop(r.data))
      .catch(() => setShop(null))
      .finally(() => setLoading(false));
  }, [user]);

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
    if (!certificate) {
      setCertificateError("Please attach your Shop Act certificate.");
      return;
    }
    if (certificateError || logoError) return;

    setSubmitting(true);
    try {
      const shopActCertificateUrl = certificate ? await uploadFile(certificate, "certificates") : null;
      const logoUrl = logo ? await uploadFile(logo, "logos") : null;

      await api.post("/shops", {
        ...form,
        vendorId: user?.id,
        shopActCertificateUrl,
        logoUrl,
      });
      // Reload full details so the read view shows everything.
      const details = await api.get(`/shops/vendor/${user.id}`);
      setShop(details.data);
      setJustCreated(true);
    } catch (err) {
      const message = err?.response?.data?.message || err.message;
      // Another vendor can claim the name between the live check and this
      // submit, so surface the server's verdict on the field itself.
      if (err?.response?.status === 409 && /shop name/i.test(message || "")) {
        setNameStatus("taken");
        setTouched((t) => ({ ...t, shopName: true }));
        document.querySelector('[name="shopName"]')?.focus();
      }
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return <div>Loading…</div>;

  // ---------- Read view: shop already registered ----------
  if (shop) {
    return (
      <div style={{ maxWidth: 860 }}>
        {justCreated && (
          <div className="card" style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 20, background: "#f0fdf4", border: "1px solid #bbf7d0" }}>
            <Anim name="success" size={64} loop={false} style={{ margin: 0 }} />
            <div>
              <h3 style={{ color: "#15803d" }}>Shop created successfully!</h3>
              <p style={{ color: "#166534", fontSize: 14 }}>Your catalog URL and QR code are ready below.</p>
            </div>
          </div>
        )}
        {/* Banner header */}
        <div className="card" style={{ display: "flex", alignItems: "center", gap: 20, marginBottom: 20,
          background: "linear-gradient(120deg,#faf5ff,#eef2ff)", border: "1px solid #e9d5ff" }}>
          <div style={{ width: 72, height: 72, borderRadius: 16, background: "#fff", display: "grid", placeItems: "center",
            overflow: "hidden", boxShadow: "var(--shadow-sm)", flexShrink: 0 }}>
            {shop.logoUrl
              ? <img src={shop.logoUrl} alt="logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
              : <span style={{ fontSize: 28 }}>🏬</span>}
          </div>
          <div style={{ flex: 1 }}>
            <h1 style={{ fontSize: 26 }}>{shop.shopName}</h1>
            <div style={{ display: "flex", alignItems: "center", gap: 10, marginTop: 6 }}>
              <span className={"badge " + (shop.status === "Active" ? "badge-green" : "badge-red")}>{shop.status}</span>
              <span style={{ color: "var(--muted)", fontSize: 14 }}>{shop.shopType} Shop</span>
            </div>
            <a href={shop.catalogUrl} target="_blank" rel="noreferrer" style={{ fontSize: 14, display: "inline-block", marginTop: 8 }}>
              🔗 {shop.catalogUrl}
            </a>
          </div>
        </div>

        <div className="grid" style={{ gridTemplateColumns: "1.4fr 1fr", gap: 20 }}>
          <div className="card">
            <h3 style={{ marginBottom: 6 }}>Shop Details</h3>
            <Row label="Phone" value={shop.phone} />
            <Row label="Alternate No." value={shop.alternateNumber} />
            <Row label="Address" value={shop.address} />
            <Row label="Aadhaar" value={shop.aadhaarCardNo} />
            <Row label="PAN" value={shop.pancardNo} />
            <Row label="Shop Act No." value={shop.shopActNo} />
            {shop.shopActCertificateUrl && (
              <Row label="Certificate" value={<a href={shop.shopActCertificateUrl} target="_blank" rel="noreferrer">View file</a>} />
            )}
          </div>

          <div className="card" style={{ textAlign: "center", display: "flex", flexDirection: "column", alignItems: "center" }}>
            <h3 style={{ marginBottom: 14 }}>Catalog QR</h3>
            {shop.qrImagePath && (
              <img src={shop.qrImagePath} alt="QR code" width={190}
                style={{ border: "1px solid var(--border)", borderRadius: 12, padding: 8, background: "#fff" }} />
            )}
            <p style={{ marginTop: 12, fontSize: 13, color: "var(--muted)" }}>Scan to open the catalog</p>
            <button className="btn btn-outline" style={{ marginTop: 10 }}
              onClick={() => navigator.clipboard?.writeText(shop.catalogUrl)}>
              Copy catalog link
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ---------- Registration form: no shop yet ----------
  return (
    <div style={{ maxWidth: 620 }}>
      <h1 style={{ fontSize: 28, marginBottom: 4 }}>Shop Profile</h1>
      <p style={{ color: "var(--muted)", marginBottom: 24 }}>
        Register your shop to generate a catalog URL and QR code.
      </p>

      <div className="card">
        <form onSubmit={handleSubmit} noValidate>
          <Field
            label="Shop Name * (must be unique)" name="shopName" value={form.shopName}
            onChange={set("shopName")} onBlur={blur("shopName")}
            error={errorFor("shopName")}
            hint={
              nameStatus === "checking" ? "Checking availability…"
                : nameStatus === "available" ? "✓ Available" : null
            }
            {...SHOP_NAME_FIELD}
          />

          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <Field
              label="Aadhaar Card *" name="aadhaarCardNo" value={form.aadhaarCardNo}
              onChange={set("aadhaarCardNo", onlyDigits)} onBlur={blur("aadhaarCardNo")}
              error={errorFor("aadhaarCardNo")} {...AADHAAR_FIELD}
            />
            <Field
              label="PAN Card *" name="pancardNo" value={form.pancardNo}
              onChange={set("pancardNo", asPan)} onBlur={blur("pancardNo")}
              error={errorFor("pancardNo")} {...PAN_FIELD}
            />
          </div>

          <Field
            label="Shop Act No *" name="shopActNo" value={form.shopActNo}
            onChange={set("shopActNo", asShopActNo)} onBlur={blur("shopActNo")}
            error={errorFor("shopActNo")} {...SHOP_ACT_FIELD}
          />

          <Field
            label={`Shop Act Certificate * (PDF or image, max ${formatBytes(FILE_RULES.certificate.maxBytes)})`}
            type="file" accept={FILE_RULES.certificate.accept}
            onChange={pickCertificate}
            error={certificateError}
            hint={certificate && !certificateError ? `${certificate.name} · ${formatBytes(certificate.size)}` : null}
          />

          <Field
            label="Address *" name="address" value={form.address}
            onChange={set("address")} onBlur={blur("address")}
            error={errorFor("address")} {...ADDRESS_FIELD}
          />

          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <Field
              label="Phone *" name="phone" value={form.phone}
              onChange={set("phone", onlyDigits)} onBlur={blur("phone")}
              error={errorFor("phone")} {...PHONE_FIELD}
            />
            <Field
              label="Alternate Number" name="alternateNumber" value={form.alternateNumber}
              onChange={set("alternateNumber", onlyDigits)} onBlur={blur("alternateNumber")}
              error={errorFor("alternateNumber")} {...PHONE_FIELD}
            />
          </div>

          <Field
            label={`Logo (image, max ${formatBytes(FILE_RULES.logo.maxBytes)}, ${FILE_RULES.logo.minDimension}–${FILE_RULES.logo.maxDimension}px)`}
            type="file" accept={FILE_RULES.logo.accept}
            onChange={pickLogo}
            error={logoError}
            hint={logo && !logoError ? `${logo.name} · ${formatBytes(logo.size)}` : null}
          />

          {error && <div className="alert alert-error">{error}</div>}
          <button type="submit" className="btn btn-primary" disabled={submitting || !formIsValid}>
            {submitting ? "Creating…" : "Create Shop"}
          </button>
        </form>
      </div>
    </div>
  );
}

function Row({ label, value }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: "1px solid var(--border)", fontSize: 14 }}>
      <span style={{ color: "var(--muted)" }}>{label}</span>
      <span style={{ fontWeight: 600, color: "var(--text-h)", textAlign: "right" }}>{value || "—"}</span>
    </div>
  );
}
