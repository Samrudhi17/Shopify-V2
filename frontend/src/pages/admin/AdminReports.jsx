// Admin Reports: active vs inactive shops analysis.
import { useEffect, useState } from "react";
import api from "../../services/api";

export default function AdminReports() {
  const [s, setS] = useState({ totalVendors: 0, totalShops: 0, activeShops: 0, inactiveShops: 0 });

  useEffect(() => { api.get("/admin/stats").then((r) => setS(r.data)).catch(() => {}); }, []);

  const total = s.totalShops || 1;
  const activePct = Math.round((s.activeShops / total) * 100);

  return (
    <div>
      <div className="page-head"><h1 style={{ fontSize: 28 }}>Reports & Analysis</h1></div>

      <div className="grid grid-4">
        <Stat ico="👥" tone="violet" label="Total Vendors" value={s.totalVendors} />
        <Stat ico="🏬" tone="blue" label="Total Shops" value={s.totalShops} />
        <Stat ico="✅" tone="green" label="Active Shops" value={s.activeShops} />
        <Stat ico="🚫" tone="red" label="Inactive Shops" value={s.inactiveShops} />
      </div>

      <div className="card" style={{ marginTop: 24 }}>
        <h3 style={{ marginBottom: 14 }}>Shop Activity</h3>
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: 14, marginBottom: 6 }}>
          <span>Active shops</span><strong>{activePct}%</strong>
        </div>
        <div style={{ height: 14, background: "#fee2e2", borderRadius: 999, overflow: "hidden" }}>
          <div style={{ width: `${activePct}%`, height: "100%", background: "#16a34a" }} />
        </div>
        <p style={{ color: "var(--muted)", fontSize: 13, marginTop: 10 }}>
          {s.activeShops} active · {s.inactiveShops} inactive of {s.totalShops} total shops
        </p>
      </div>
    </div>
  );
}

function Stat({ ico, tone, label, value }) {
  return (
    <div className="card stat-card">
      <div className={"stat-ico ico-" + tone}>{ico}</div>
      <div><div className="stat-label">{label}</div><div className="stat-value">{value}</div></div>
    </div>
  );
}
