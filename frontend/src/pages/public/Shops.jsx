// Public shop directory: customers browse every Active shop and open its catalog.
// Unauthenticated — served by GET /api/catalog.
import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import api from "../../services/api";
import usePageTitle from "../../hooks/usePageTitle";

export default function Shops() {
  usePageTitle("Browse Shops");
  const [shops, setShops] = useState([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");

  useEffect(() => {
    api.get("/catalog")
      .then((r) => setShops(r.data))
      .catch(() => setShops([]))
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return shops;
    return shops.filter((s) =>
      s.shopName.toLowerCase().includes(q) || (s.address || "").toLowerCase().includes(q)
    );
  }, [shops, query]);

  return (
    <div>
      <nav className="nav">
        <Link to="/" className="brand" style={{ textDecoration: "none" }}>
          <span className="brand-mark">▦</span> ScanStore
        </Link>
        <div className="nav-links">
          <Link to="/shops">Shops</Link>
          <Link to="/#services">Services</Link>
          <Link to="/#about">About</Link>
          <Link to="/#contact">Contact</Link>
          <Link to="/login" className="btn btn-ghost">Login</Link>
          <Link to="/register" className="btn btn-primary">Register</Link>
        </div>
      </nav>

      <section className="section" style={{ paddingBottom: 32 }}>
        <div className="container" style={{ textAlign: "center" }}>
          <div className="section-tag">Browse Shops</div>
          <h2 style={{ margin: "8px 0 14px" }}>Discover local shops near you</h2>
          <p style={{ color: "var(--muted)", fontSize: 17, maxWidth: 620, margin: "0 auto" }}>
            Every shop below publishes a live catalog. Open one to browse its products,
            or scan its QR code in store.
          </p>

          <input
            className="input"
            style={{ maxWidth: 420, margin: "28px auto 0" }}
            placeholder="Search by shop name or area…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
      </section>

      <section className="section" style={{ paddingTop: 0 }}>
        <div className="container">
          {loading ? (
            <p style={{ textAlign: "center", color: "var(--muted)" }}>Loading shops…</p>
          ) : filtered.length === 0 ? (
            <div className="card" style={{ textAlign: "center", padding: 48, color: "var(--muted)" }}>
              {shops.length === 0
                ? "No shops have been published yet. Check back soon."
                : `No shops match “${query}”.`}
            </div>
          ) : (
            <>
              <div style={{ color: "var(--muted)", fontSize: 14, marginBottom: 16 }}>
                {filtered.length} shop{filtered.length !== 1 ? "s" : ""}
              </div>
              <div className="grid grid-3">
                {filtered.map((s) => (
                  <Link
                    key={s.shopId}
                    to={`/${s.slug}`}
                    className="card"
                    style={{ textDecoration: "none", color: "inherit", display: "block" }}
                  >
                    <div style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 14 }}>
                      <div style={{
                        width: 56, height: 56, borderRadius: 14, background: "#f5f3ff",
                        display: "grid", placeItems: "center", overflow: "hidden", flexShrink: 0,
                      }}>
                        {s.logoUrl
                          ? <img src={s.logoUrl} alt="" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                          : <span style={{ fontSize: 26 }}>🏬</span>}
                      </div>
                      <div style={{ minWidth: 0 }}>
                        <h3 style={{ marginBottom: 4 }}>{s.shopName}</h3>
                        <span className="badge badge-green">{s.productCount} product{s.productCount !== 1 ? "s" : ""}</span>
                      </div>
                    </div>
                    <p style={{ color: "var(--muted)", fontSize: 14, marginBottom: 6 }}>📍 {s.address}</p>
                    <p style={{ color: "var(--muted)", fontSize: 14 }}>📞 {s.phone}</p>
                    <div style={{ marginTop: 14, color: "var(--brand)", fontWeight: 600, fontSize: 14 }}>
                      View catalog →
                    </div>
                  </Link>
                ))}
              </div>
            </>
          )}
        </div>
      </section>

      <footer className="footer">© {new Date().getFullYear()} ScanStore · Built for local businesses</footer>
    </div>
  );
}
