// Admin dashboard: totals, vendor list, shop activate/deactivate.
import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import api from "../../services/api";

export default function AdminDashboard() {
  const { user } = useAuth();
  const [stats, setStats] = useState({ totalVendors: 0, totalShops: 0, activeShops: 0, inactiveShops: 0 });
  const [vendors, setVendors] = useState([]);

  useEffect(() => {
    // Endpoints to be implemented on the backend.
    api.get("/admin/stats").then((r) => setStats(r.data)).catch(() => {});
    api.get("/admin/vendors").then((r) => setVendors(r.data)).catch(() => {});
  }, []);

  return (
    <div>
      <div className="page-head">
        <div>
          <h1 style={{ fontSize: 28 }}>Dashboard</h1>
          <div className="subtitle">Welcome back, {user?.name || "Admin"} 👋</div>
        </div>
      </div>

      <div className="grid grid-4">
        <Stat ico="👥" tone="violet" label="Total Vendors" value={stats.totalVendors} />
        <Stat ico="🏬" tone="blue" label="Total Shops" value={stats.totalShops} />
        <Stat ico="✅" tone="green" label="Active Shops" value={stats.activeShops} />
        <Stat ico="🚫" tone="red" label="Inactive Shops" value={stats.inactiveShops} />
      </div>

      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", margin: "36px 0 16px" }}>
        <h2 style={{ fontSize: 20 }}>Vendors</h2>
        <span className="subtitle">{vendors.length} total</span>
      </div>

      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <table className="table">
          <thead>
            <tr>
              <th>Vendor</th><th>Email</th><th>Phone</th><th>Status</th><th style={{ textAlign: "right" }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {vendors.map((v) => (
              <tr key={v.vendorId}>
                <td>
                  <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <Avatar name={v.name} />
                    <div>
                      <div style={{ fontWeight: 600, color: "var(--text-h)" }}>{v.name}</div>
                      <div style={{ fontSize: 12, color: "var(--muted)" }}>#{v.vendorId}</div>
                    </div>
                  </div>
                </td>
                <td>{v.email}</td>
                <td>{v.phone}</td>
                <td>
                  <span className={"badge " + (v.status === "Active" ? "badge-green" : "badge-red")}>
                    {v.status}
                  </span>
                </td>
                <td style={{ textAlign: "right" }}>
                  <button className="btn btn-outline" style={{ padding: "6px 12px", fontSize: 13 }}>View</button>
                </td>
              </tr>
            ))}
            {vendors.length === 0 && (
              <tr>
                <td colSpan={5} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>
                  No vendors registered yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Stat({ ico, tone, label, value }) {
  return (
    <div className="card stat-card">
      <div className={"stat-ico ico-" + tone}>{ico}</div>
      <div>
        <div className="stat-label">{label}</div>
        <div className="stat-value">{value}</div>
      </div>
    </div>
  );
}

function Avatar({ name }) {
  const initials = (name || "?").split(" ").map((w) => w[0]).slice(0, 2).join("").toUpperCase();
  return (
    <div style={{
      width: 36, height: 36, borderRadius: "50%", background: "#ede9fe", color: "#6d28d9",
      display: "grid", placeItems: "center", fontWeight: 700, fontSize: 13, flexShrink: 0,
    }}>
      {initials}
    </div>
  );
}
