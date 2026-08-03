// Shop registration / profile. If the vendor already has a shop, show all its
// details + catalog URL + QR code. Otherwise show the registration form.
import { useEffect, useState } from "react";
import api from "../../services/api";
import { uploadFile } from "../../services/uploads";
import { useAuth } from "../../context/AuthContext";
import Anim from "../../components/Anim";
import {
  AADHAAR_FIELD, PAN_FIELD, PHONE_FIELD, SHOP_ACT_FIELD,
  onlyDigits, asPan, asShopActNo,
} from "../../constants/validation";

const initial = {
  shopName: "",
  aadhaarCardNo: "",
  pancardNo: "",
  shopActNo: "",
  address: "",
  phone: "",
  alternateNumber: "",
};

export default function Profile() {
  const { user } = useAuth();
  const [shop, setShop] = useState(null);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState(initial);
  const [certificate, setCertificate] = useState(null);
  const [logo, setLogo] = useState(null);
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [justCreated, setJustCreated] = useState(false);

  // `transform` normalizes as the user types (e.g. upper-case a PAN).
  const set = (k, transform) => (e) =>
    setForm({ ...form, [k]: transform ? transform(e.target.value) : e.target.value });

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
    setSubmitting(true);
    try {
      const shopActCertificateUrl = certificate ? await uploadFile(certificate, "certificates") : null;
      const logoUrl = logo ? await uploadFile(logo, "logos") : null;

      const { data } = await api.post("/shops", {
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
      setError(err?.response?.data?.message || err.message);
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
        <form onSubmit={handleSubmit}>
          <F label="Shop Name * (must be unique)" value={form.shopName} onChange={set("shopName")} required minLength={2} maxLength={150} />
          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <F label="Aadhaar Card *" value={form.aadhaarCardNo} onChange={set("aadhaarCardNo", onlyDigits)} required {...AADHAAR_FIELD} />
            <F label="PAN Card *" value={form.pancardNo} onChange={set("pancardNo", asPan)} required {...PAN_FIELD} />
          </div>
          <F label="Shop Act No *" value={form.shopActNo} onChange={set("shopActNo", asShopActNo)} required {...SHOP_ACT_FIELD} />
          <label className="field">
            <span>Shop Act Certificate * (PDF / Image)</span>
            <input className="input" type="file" accept=".pdf,image/*" onChange={(e) => setCertificate(e.target.files[0])} required />
          </label>
          <F label="Address *" value={form.address} onChange={set("address")} required minLength={5} maxLength={255} />
          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <F label="Phone *" value={form.phone} onChange={set("phone", onlyDigits)} required {...PHONE_FIELD} />
            <F label="Alternate Number" value={form.alternateNumber} onChange={set("alternateNumber", onlyDigits)} {...PHONE_FIELD} />
          </div>
          <label className="field">
            <span>Logo</span>
            <input className="input" type="file" accept="image/*" onChange={(e) => setLogo(e.target.files[0])} />
          </label>
          {error && <div className="alert alert-error">{error}</div>}
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? "Creating…" : "Create Shop"}
          </button>
        </form>
      </div>
    </div>
  );
}

function F({ label, ...props }) {
  return (
    <label className="field">
      <span>{label}</span>
      <input className="input" {...props} />
    </label>
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
