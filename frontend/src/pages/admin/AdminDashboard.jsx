// Admin dashboard: totals + shop list (read-only).
// Activating/deactivating a shop lives on the Shops page, not here.
import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import api from "../../services/api";

export default function AdminDashboard() {
  const { user } = useAuth();
  const [stats, setStats] = useState({ totalVendors: 0, totalShops: 0, activeShops: 0, inactiveShops: 0 });
  const [shops, setShops] = useState([]);

  useEffect(() => {
    api.get("/admin/stats").then((r) => setStats(r.data)).catch(() => {});
    api.get("/admin/shops").then((r) => setShops(r.data)).catch(() => {});
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
        <h2 style={{ fontSize: 20 }}>Shops</h2>
        <span className="subtitle">{shops.length} total</span>
      </div>

      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead>
              <tr>
                <th>Shop</th><th>Owner</th><th>Phone</th><th>Catalog</th><th>Status</th>
              </tr>
            </thead>
            <tbody>
              {shops.map((s) => (
                <tr key={s.shopId}>
                  <td>
                    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                      <Avatar name={s.shopName} />
                      <div>
                        <div style={{ fontWeight: 600, color: "var(--text-h)" }}>{s.shopName}</div>
                        <div style={{ fontSize: 12, color: "var(--muted)" }}>#{s.shopId}</div>
                      </div>
                    </div>
                  </td>
                  <td>{s.vendorName}</td>
                  <td>{s.phone}</td>
                  <td>
                    {s.catalogUrl
                      ? <a href={s.catalogUrl} target="_blank" rel="noreferrer">/{s.slug}</a>
                      : "—"}
                  </td>
                  <td>
                    <span className={"badge " + (s.status === "Active" ? "badge-green" : "badge-red")}>
                      {s.status}
                    </span>
                  </td>
                </tr>
              ))}
              {shops.length === 0 && (
                <tr>
                  <td colSpan={5} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>
                    No shops registered yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
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
