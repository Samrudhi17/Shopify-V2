// Admin: all vendors with activate/deactivate.
import { useEffect, useState } from "react";
import api from "../../services/api";

export default function AdminVendors() {
  const [vendors, setVendors] = useState([]);

  const load = () => api.get("/admin/vendors").then((r) => setVendors(r.data)).catch(() => {});
  useEffect(() => { load(); }, []);

  async function toggle(v) {
    const next = v.status === "Active" ? "Inactive" : "Active";
    setVendors((vs) => vs.map((x) => (x.vendorId === v.vendorId ? { ...x, status: next } : x)));
    try { await api.put(`/admin/vendors/${v.vendorId}/status`, { status: next }); }
    catch { load(); }
  }

  return (
    <div>
      <div className="page-head"><h1 style={{ fontSize: 28 }}>Vendors</h1><span className="subtitle">{vendors.length} total</span></div>
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Vendor</th><th>Email</th><th>Phone</th><th>Shop</th><th>Status</th><th style={{ textAlign: "right" }}>Action</th></tr></thead>
            <tbody>
              {vendors.map((v) => (
                <tr key={v.vendorId}>
                  <td style={{ fontWeight: 600, color: "var(--text-h)" }}>{v.name}</td>
                  <td>{v.email}</td><td>{v.phone}</td><td>{v.shopName || "—"}</td>
                  <td><span className={"badge " + (v.status === "Active" ? "badge-green" : "badge-red")}>{v.status}</span></td>
                  <td style={{ textAlign: "right" }}>
                    <button className="btn btn-outline" style={{ padding: "6px 12px", fontSize: 13 }} onClick={() => toggle(v)}>
                      {v.status === "Active" ? "Deactivate" : "Activate"}
                    </button>
                  </td>
                </tr>
              ))}
              {vendors.length === 0 && <tr><td colSpan={6} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>No vendors yet.</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
