// Admin: every subscription term any vendor has held, newest first.
// Read-only — billing is settled by Razorpay, not edited by hand here.
import { useEffect, useMemo, useState } from "react";
import api from "../../services/api";
import usePageTitle from "../../hooks/usePageTitle";
import { money, formatDate } from "./subscriptionFormat";
import { SubStatus } from "./subscriptionUi";

const FILTERS = ["All", "Active", "Trialing", "Expired"];

export default function AdminSubscriptions() {
  usePageTitle("Subscriptions");
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState("All");
  const [query, setQuery] = useState("");

  useEffect(() => {
    api.get("/admin/subscriptions")
      .then((r) => setRows(r.data))
      .catch(() => setRows([]))
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    return rows.filter((r) =>
      (filter === "All" || r.status === filter) &&
      (!q || r.vendorName.toLowerCase().includes(q)
          || (r.shopName || "").toLowerCase().includes(q)
          || (r.vendorEmail || "").toLowerCase().includes(q))
    );
  }, [rows, filter, query]);

  // Only paid terms count toward revenue; trials are ₹0 by definition.
  const revenue = useMemo(
    () => filtered.reduce((sum, r) => sum + Number(r.amount || 0), 0),
    [filtered]
  );

  return (
    <div>
      <div className="page-head">
        <div>
          <h1 style={{ fontSize: 28 }}>Subscriptions</h1>
          <div className="subtitle">
            {filtered.length} term{filtered.length === 1 ? "" : "s"} · {money(revenue)} collected
          </div>
        </div>
      </div>

      <div className="card" style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "center", marginBottom: 20 }}>
        <input
          className="input"
          style={{ maxWidth: 280 }}
          placeholder="Search vendor, shop or email…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        <div style={{ display: "flex", gap: 8 }}>
          {FILTERS.map((f) => (
            <button
              key={f}
              type="button"
              className={"btn " + (filter === f ? "btn-primary" : "btn-outline")}
              style={{ padding: "6px 14px", fontSize: 13 }}
              onClick={() => setFilter(f)}
            >
              {f}
            </button>
          ))}
        </div>
      </div>

      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead>
              <tr>
                <th>Vendor</th><th>Shop</th><th>Plan</th><th>Amount</th>
                <th>Started</th><th>Ends</th><th>Status</th><th>Payment</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((r) => (
                <tr key={r.subscriptionId}>
                  <td>
                    <div style={{ fontWeight: 600, color: "var(--text-h)" }}>{r.vendorName}</div>
                    <div style={{ fontSize: 12, color: "var(--muted)" }}>{r.vendorEmail}</div>
                  </td>
                  <td>{r.shopName || "—"}</td>
                  <td>{r.planName}</td>
                  <td style={{ fontWeight: r.isTrial ? 400 : 700 }}>
                    {r.isTrial ? <span style={{ color: "var(--muted)" }}>Free</span> : money(r.amount)}
                  </td>
                  <td>{formatDate(r.startsAt)}</td>
                  <td>{formatDate(r.endsAt)}</td>
                  <td><SubStatus row={r} /></td>
                  <td style={{ fontSize: 12, color: "var(--muted)" }}>
                    {r.paymentId
                      ? <span title={`Paid ${formatDate(r.paidAt)}`}>{r.paymentId}</span>
                      : "—"}
                  </td>
                </tr>
              ))}
              {!loading && filtered.length === 0 && (
                <tr><td colSpan={8} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>
                  {rows.length === 0
                    ? "No subscriptions yet. They appear here as soon as a vendor registers or pays."
                    : "No subscriptions match those filters."}
                </td></tr>
              )}
              {loading && (
                <tr><td colSpan={8} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>Loading…</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
