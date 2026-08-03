// Vendor Reports: stock/product analytics.
import { useEffect, useState } from "react";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";
import Anim from "../../components/Anim";
import usePageTitle from "../../hooks/usePageTitle";

export default function Reports() {
  usePageTitle("Reports");
  const { user } = useAuth();
  const [r, setR] = useState(null);

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/vendor/reports`).then((res) => setR(res.data)).catch(() => {});
  }, [user]);

  if (!r) return <div><h1 style={{ fontSize: 28 }}>Reports</h1><p style={{ color: "var(--muted)" }}>Loading…</p></div>;

  return (
    <div>
      <div className="page-head" style={{ alignItems: "center" }}>
        <div>
          <h1 style={{ fontSize: 28 }}>Reports & Analytics</h1>
          <div className="subtitle">An overview of your shop's inventory and catalog.</div>
        </div>
        <Anim name="analytics" size={110} style={{ margin: 0 }} />
      </div>

      <div className="grid grid-3">
        <Stat ico="📦" tone="violet" label="Total Products" value={r.totalProducts} />
        <Stat ico="📊" tone="blue" label="Total Stock" value={r.totalStock} />
        <Stat ico="💰" tone="green" label="Stock Value" value={`₹${Number(r.stockValue).toLocaleString("en-IN")}`} />
      </div>

      <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 20, marginTop: 24 }}>
        <div className="card">
          <h3 style={{ marginBottom: 12 }}>Stock by Category</h3>
          {r.byCategory.length === 0 ? <Empty /> : r.byCategory.map((c) => (
            <Bar key={c.name} label={c.name} value={c.stock} max={Math.max(...r.byCategory.map((x) => x.stock), 1)} note={`${c.count} product${c.count !== 1 ? "s" : ""}`} />
          ))}
        </div>
        <div className="card">
          <h3 style={{ marginBottom: 12 }}>Products by Type</h3>
          {r.byType.length === 0 ? <Empty /> : r.byType.map((t) => (
            <div key={t.name} style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: "1px solid var(--border)" }}>
              <span>{t.name}</span>
              <span style={{ fontWeight: 700 }}>{t.count}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="card" style={{ marginTop: 20 }}>
        <h3 style={{ marginBottom: 12 }}>⚠️ Low Stock (under 5)</h3>
        {r.lowStock.length === 0 ? <p style={{ color: "var(--muted)" }}>All products are well stocked.</p> : (
          <div style={{ display: "flex", flexWrap: "wrap", gap: 10 }}>
            {r.lowStock.map((p, i) => (
              <span key={i} className="badge badge-red" style={{ padding: "6px 12px" }}>{p.productName}: {p.stock}</span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

const Empty = () => <p style={{ color: "var(--muted)" }}>No data yet.</p>;

function Bar({ label, value, max, note }) {
  return (
    <div style={{ marginBottom: 12 }}>
      <div style={{ display: "flex", justifyContent: "space-between", fontSize: 14, marginBottom: 4 }}>
        <span>{label} <span style={{ color: "var(--muted)", fontSize: 12 }}>· {note}</span></span>
        <strong>{value}</strong>
      </div>
      <div style={{ height: 8, background: "#f1f5f9", borderRadius: 999 }}>
        <div style={{ width: `${(value / max) * 100}%`, height: "100%", background: "var(--brand)", borderRadius: 999 }} />
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
        <div className="stat-value" style={{ fontSize: 24 }}>{value}</div>
      </div>
    </div>
  );
}
