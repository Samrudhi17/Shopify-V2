// Vendor product list: grouped by product type, with filters
// (product type, category, size, color) and edit / remove actions.
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";
import Anim from "../../components/Anim";

const ALL = "All";

// Trim + title-case the product type so "Jeans ", "jeans", "JEANS" all group as "Jeans".
function titleCase(s) {
  return s.replace(/\s+/g, " ").trim().toLowerCase().replace(/\b\w/g, (c) => c.toUpperCase());
}
function normalize(p) {
  const t = (p.productType || "").trim();
  return { ...p, productType: t ? titleCase(t) : null };
}

export default function Products() {
  const { user } = useAuth();
  const [products, setProducts] = useState([]);
  const [filters, setFilters] = useState({ type: ALL, category: ALL, size: ALL, color: ALL });
  const navigate = useNavigate();

  const load = () => {
    if (!user?.id) return;
    api.get(`/products?vendorId=${user.id}`)
      .then((r) => setProducts(r.data.map(normalize)))
      .catch(() => {});
  };
  useEffect(load, [user]);

  // Build filter option lists from the data.
  const opts = useMemo(() => {
    const uniq = (key) => [ALL, ...new Set(products.map((p) => (p[key] || "").trim()).filter(Boolean))];
    return {
      type: uniq("productType"),
      category: uniq("categoryName"),
      size: uniq("size"),
      color: uniq("color"),
    };
  }, [products]);

  const tr = (v) => (v || "").trim();
  const filtered = products.filter((p) =>
    (filters.type === ALL || tr(p.productType) === filters.type) &&
    (filters.category === ALL || tr(p.categoryName) === filters.category) &&
    (filters.size === ALL || tr(p.size) === filters.size) &&
    (filters.color === ALL || tr(p.color) === filters.color)
  );

  // Group by product type (trimmed + case-insensitive so "Jeans" and "Jeans " merge).
  const groups = useMemo(() => {
    const g = {}; // lowerKey -> { label, items }
    for (const p of filtered) {
      const label = (p.productType || "Other").trim() || "Other";
      const key = label.toLowerCase();
      if (!g[key]) g[key] = { label, items: [] };
      g[key].items.push(p);
    }
    return Object.values(g).sort((a, b) => a.label.localeCompare(b.label));
  }, [filtered]);

  async function remove(id) {
    if (!window.confirm("Remove this product from the database?")) return;
    await api.delete(`/products/${id}`);
    load();
  }

  async function changeStock(product, delta) {
    const next = Math.max(0, (product.stockQty || 0) + delta);
    if (next === product.stockQty) return;
    // Optimistic update.
    setProducts((ps) => ps.map((p) => (p.productId === product.productId ? { ...p, stockQty: next } : p)));
    try {
      await api.patch(`/products/${product.productId}/stock`, { stockQty: next });
    } catch {
      setProducts((ps) => ps.map((p) => (p.productId === product.productId ? product : p)));
    }
  }

  const setF = (k) => (e) => setFilters({ ...filters, [k]: e.target.value });

  return (
    <div>
      <div className="page-head">
        <div>
          <h1 style={{ fontSize: 28 }}>Products</h1>
          <div className="subtitle">{products.length} product{products.length !== 1 ? "s" : ""}</div>
        </div>
        <button className="btn btn-primary" onClick={() => navigate("/vendor/products/new")}>+ Add Product</button>
      </div>

      {/* Filters */}
      <div className="card" style={{ display: "flex", gap: 12, flexWrap: "wrap", marginBottom: 20 }}>
        <Filter label="Product Type" value={filters.type} onChange={setF("type")} options={opts.type} />
        <Filter label="Category" value={filters.category} onChange={setF("category")} options={opts.category} />
        <Filter label="Size" value={filters.size} onChange={setF("size")} options={opts.size} />
        <Filter label="Color" value={filters.color} onChange={setF("color")} options={opts.color} />
      </div>

      {filtered.length === 0 ? (
        <div className="card" style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>
          <Anim name="empty" size={180} />
          <p style={{ marginTop: 8 }}>No products yet. Click <strong>Add Product</strong> to create one.</p>
        </div>
      ) : (
        groups.map(({ label, items }) => (
          <div key={label} style={{ marginBottom: 28 }}>
            <h2 style={{ fontSize: 18, marginBottom: 12 }}>{label} <span style={{ color: "var(--muted)", fontWeight: 400, fontSize: 14 }}>({items.length})</span></h2>
            <div className="grid grid-3">
              {items.map((p) => (
                <div className="card" key={p.productId} style={{ padding: 0, overflow: "hidden" }}>
                  {p.primaryImageUrl ? (
                    <img src={p.primaryImageUrl} alt={p.productName} style={{ width: "100%", height: 160, objectFit: "cover" }} />
                  ) : (
                    <div style={{ height: 160, background: "#f1f5f9", display: "grid", placeItems: "center", color: "#94a3b8" }}>No image</div>
                  )}
                  <div style={{ padding: 16 }}>
                    <div style={{ display: "flex", gap: 6, flexWrap: "wrap", marginBottom: 8 }}>
                      {p.categoryName && <span className="badge badge-green">{p.categoryName}</span>}
                      {p.color && <span className="badge" style={{ background: "#eef2ff", color: "#4338ca" }}>{p.color}</span>}
                    </div>
                    <h3>{p.productName}</h3>
                    <p style={{ color: "var(--muted)", fontSize: 13 }}>
                      {p.brand}{p.size ? ` · Size ${p.size}` : ""}
                    </p>
                    <div style={{ fontWeight: 800, fontSize: 20, margin: "8px 0" }}>₹{p.basePrice}</div>

                    {/* Stock management */}
                    <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", margin: "10px 0", padding: "8px 10px", background: "#f8fafc", borderRadius: 10 }}>
                      <span style={{ fontSize: 13, color: "var(--muted)" }}>Stock</span>
                      <div style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
                        <button className="qty-btn" disabled={p.stockQty === 0} onClick={() => changeStock(p, -1)}>−</button>
                        <span style={{ minWidth: 28, textAlign: "center", fontWeight: 700 }}>{p.stockQty}</span>
                        <button className="qty-btn" onClick={() => changeStock(p, +1)}>+</button>
                      </div>
                    </div>

                    <div style={{ display: "flex", gap: 8 }}>
                      <button className="btn btn-outline" style={{ padding: "6px 12px", fontSize: 13 }} onClick={() => navigate(`/vendor/products/${p.productId}/edit`)}>Edit</button>
                      <button className="btn btn-ghost" style={{ padding: "6px 12px", fontSize: 13, color: "#dc2626" }} onClick={() => remove(p.productId)}>Remove</button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </div>
  );
}

function Filter({ label, value, onChange, options }) {
  return (
    <label style={{ fontSize: 13 }}>
      <div style={{ color: "var(--muted)", marginBottom: 4, fontWeight: 600 }}>{label}</div>
      <select className="input" value={value} onChange={onChange} style={{ minWidth: 140 }}>
        {options.map((o) => <option key={o} value={o}>{o}</option>)}
      </select>
    </label>
  );
}
