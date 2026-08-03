// Vendor dashboard: Total Products / Categories / Stock cards, Add Product,
// and a "Recently added" list of the last few products.
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";

export default function VendorDashboard() {
  const { user } = useAuth();
  const [stats, setStats] = useState({ totalProducts: 0, totalCategories: 0, totalStock: 0 });
  const [recent, setRecent] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/vendor/stats`).then((r) => setStats(r.data)).catch(() => {});
    api.get(`/products`).then((r) => setRecent(r.data.slice(0, 6))).catch(() => {});
  }, [user]);

  return (
    <div>
      <div className="page-head">
        <div>
          <h1 style={{ fontSize: 28 }}>Shop Dashboard</h1>
          <div className="subtitle">{user?.shopName ? `Managing ${user.shopName}` : "Welcome back"}</div>
        </div>
        <button className="btn btn-primary" onClick={() => navigate("/vendor/products/new")}>+ Add Product</button>
      </div>

      <div className="grid grid-3">
        <Stat ico="📦" tone="violet" label="Total Products" value={stats.totalProducts} />
        <Stat ico="🗂️" tone="blue" label="Total Categories" value={stats.totalCategories} />
        <Stat ico="📊" tone="green" label="Total Stock" value={stats.totalStock} />
      </div>

      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", margin: "36px 0 16px" }}>
        <h2 style={{ fontSize: 20 }}>Recently added</h2>
        {recent.length > 0 && <a onClick={() => navigate("/vendor/products")} style={{ cursor: "pointer" }}>View all</a>}
      </div>

      {recent.length === 0 ? (
        <div className="card" style={{ color: "var(--muted)" }}>No products yet. Add your first product.</div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <table className="table">
            <thead>
              <tr><th>Product</th><th>Type</th><th>Category</th><th>Price</th><th>Stock</th></tr>
            </thead>
            <tbody>
              {recent.map((p) => (
                <tr key={p.productId} style={{ cursor: "pointer" }} onClick={() => navigate(`/vendor/products/${p.productId}/edit`)}>
                  <td style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    {p.primaryImageUrl
                      ? <img src={p.primaryImageUrl} alt="" style={{ width: 36, height: 36, borderRadius: 8, objectFit: "cover" }} />
                      : <div style={{ width: 36, height: 36, borderRadius: 8, background: "#f1f5f9" }} />}
                    <span style={{ fontWeight: 600, color: "var(--text-h)" }}>{p.productName}</span>
                  </td>
                  <td>{p.productType || "—"}</td>
                  <td>{p.categoryName || "—"}</td>
                  <td>₹{p.basePrice}</td>
                  <td>{p.stockQty}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
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
