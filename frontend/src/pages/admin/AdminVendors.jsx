// Admin: read-only vendor list.
// Admins cannot deactivate a vendor — access is controlled by activating or
// deactivating that vendor's shop, on the Shops page.
import { useEffect, useState } from "react";
import api from "../../services/api";

export default function AdminVendors() {
  const [vendors, setVendors] = useState([]);

  useEffect(() => {
    api.get("/admin/vendors").then((r) => setVendors(r.data)).catch(() => {});
  }, []);

  return (
    <div>
      <div className="page-head"><h1 style={{ fontSize: 28 }}>Vendors</h1><span className="subtitle">{vendors.length} total</span></div>
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Vendor</th><th>Email</th><th>Phone</th><th>Shop</th><th>Shop Status</th></tr></thead>
            <tbody>
              {vendors.map((v) => (
                <tr key={v.vendorId}>
                  <td style={{ fontWeight: 600, color: "var(--text-h)" }}>{v.name}</td>
                  <td>{v.email}</td><td>{v.phone}</td><td>{v.shopName || "—"}</td>
                  <td>
                    {v.shopStatus
                      ? <span className={"badge " + (v.shopStatus === "Active" ? "badge-green" : "badge-red")}>{v.shopStatus}</span>
                      : <span style={{ color: "var(--muted)" }}>No shop</span>}
                  </td>
                </tr>
              ))}
              {vendors.length === 0 && <tr><td colSpan={5} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>No vendors yet.</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
