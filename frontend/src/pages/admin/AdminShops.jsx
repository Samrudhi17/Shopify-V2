// Admin: all shops with activate/deactivate (deactivating hides the catalog).
import { useEffect, useState } from "react";
import api from "../../services/api";

export default function AdminShops() {
  const [shops, setShops] = useState([]);

  const load = () => api.get("/admin/shops").then((r) => setShops(r.data)).catch(() => {});
  useEffect(() => { load(); }, []);

  async function toggle(s) {
    const next = s.status === "Active" ? "Inactive" : "Active";
    setShops((xs) => xs.map((x) => (x.shopId === s.shopId ? { ...x, status: next } : x)));
    try { await api.put(`/admin/shops/${s.shopId}/status`, { status: next }); }
    catch { load(); }
  }

  return (
    <div>
      <div className="page-head"><h1 style={{ fontSize: 28 }}>Shops</h1><span className="subtitle">{shops.length} total</span></div>
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Shop</th><th>Owner</th><th>Phone</th><th>Catalog</th><th>Status</th><th style={{ textAlign: "right" }}>Action</th></tr></thead>
            <tbody>
              {shops.map((s) => (
                <tr key={s.shopId}>
                  <td style={{ fontWeight: 600, color: "var(--text-h)" }}>{s.shopName}</td>
                  <td>{s.vendorName}</td><td>{s.phone}</td>
                  <td>{s.catalogUrl ? <a href={s.catalogUrl} target="_blank" rel="noreferrer">/{s.slug}</a> : "—"}</td>
                  <td><span className={"badge " + (s.status === "Active" ? "badge-green" : "badge-red")}>{s.status}</span></td>
                  <td style={{ textAlign: "right" }}>
                    <button className="btn btn-outline" style={{ padding: "6px 12px", fontSize: 13 }} onClick={() => toggle(s)}>
                      {s.status === "Active" ? "Deactivate" : "Activate"}
                    </button>
                  </td>
                </tr>
              ))}
              {shops.length === 0 && <tr><td colSpan={6} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>No shops yet.</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
