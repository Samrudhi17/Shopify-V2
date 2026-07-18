// Inventory: read-only view of stock per product variant.
// Stock is managed from the Products page.
import { useEffect, useState } from "react";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";

export default function Inventory() {
  const { user } = useAuth();
  const [rows, setRows] = useState([]);

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/inventory?vendorId=${user.id}`).then((r) => setRows(r.data)).catch(() => {});
  }, [user]);

  return (
    <div>
      <div className="page-head">
        <div>
          <h1 style={{ fontSize: 28 }}>Inventory</h1>
          <div className="subtitle">Stock overview. Manage stock from the Products page.</div>
        </div>
      </div>
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <table className="table">
          <thead>
            <tr>
              <th>Product</th><th>Type</th><th>Variant</th><th>SKU</th>
              <th>Stock</th><th>Reserved</th><th>Available</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.inventoryId}>
                <td style={{ fontWeight: 600, color: "var(--text-h)" }}>{r.productName}</td>
                <td>{r.productType || "—"}</td>
                <td>{[r.color, r.size].filter(Boolean).join(" / ") || "—"}</td>
                <td style={{ fontSize: 13, color: "var(--muted)" }}>{r.sku}</td>
                <td style={{ fontWeight: 700 }}>{r.stockQty}</td>
                <td>{r.reservedQty}</td>
                <td>
                  <span className={"badge " + (r.availableQty > 0 ? "badge-green" : "badge-red")}>
                    {r.availableQty > 0 ? `${r.availableQty} in stock` : "Out of stock"}
                  </span>
                </td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr><td colSpan={7} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>No inventory yet. Add a product to see stock here.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
