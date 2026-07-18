// Vendor Settings: edit shop contact details + account info.
import { useEffect, useState } from "react";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";

export default function Settings() {
  const { user, logout } = useAuth();
  const [shop, setShop] = useState(null);
  const [form, setForm] = useState({ phone: "", alternateNumber: "", address: "" });
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/shops/vendor/${user.id}`).then((r) => {
      setShop(r.data);
      setForm({ phone: r.data.phone || "", alternateNumber: r.data.alternateNumber || "", address: r.data.address || "" });
    }).catch(() => {});
  }, [user]);

  const set = (k) => (e) => { setForm({ ...form, [k]: e.target.value }); setSaved(false); };

  async function save(e) {
    e.preventDefault();
    setSaving(true);
    try {
      await api.put(`/shops/${shop.shopId}`, form);
      setSaved(true);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{ maxWidth: 640 }}>
      <div className="page-head"><h1 style={{ fontSize: 28 }}>Settings</h1></div>

      <div className="card" style={{ marginBottom: 20 }}>
        <h3 style={{ marginBottom: 14 }}>Account</h3>
        <Row label="Name" value={user?.name} />
        <Row label="Email" value={user?.email} />
        <Row label="Role" value={user?.role} />
      </div>

      {shop && (
        <div className="card">
          <h3 style={{ marginBottom: 14 }}>Contact details (shown on your catalog)</h3>
          <form onSubmit={save}>
            <label className="field"><span>Phone *</span>
              <input className="input" value={form.phone} onChange={set("phone")} required /></label>
            <label className="field"><span>Alternate Number</span>
              <input className="input" value={form.alternateNumber} onChange={set("alternateNumber")} /></label>
            <label className="field"><span>Address *</span>
              <input className="input" value={form.address} onChange={set("address")} required /></label>
            {saved && <div className="alert alert-success">Saved.</div>}
            <button className="btn btn-primary" disabled={saving}>{saving ? "Saving…" : "Save changes"}</button>
          </form>
        </div>
      )}

      <div className="card" style={{ marginTop: 20 }}>
        <h3 style={{ marginBottom: 10 }}>Session</h3>
        <button className="btn btn-outline" onClick={logout}>Logout</button>
      </div>
    </div>
  );
}

function Row({ label, value }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: "1px solid var(--border)", fontSize: 14 }}>
      <span style={{ color: "var(--muted)" }}>{label}</span>
      <span style={{ fontWeight: 600, color: "var(--text-h)" }}>{value || "—"}</span>
    </div>
  );
}
