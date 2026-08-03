// Public customer-facing catalog at /:shopSlug. No login, no cart — browse only.
// Shows shop info (logo, name, contact), products with search + filters.
// Fully responsive: desktop, tablet, mobile.
import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../../services/api";
import Anim from "../../components/Anim";
import ImageCarousel from "../../components/ImageCarousel";
import usePageTitle from "../../hooks/usePageTitle";

const ALL = "All";

export default function ShopCatalog() {
  const { shopSlug } = useParams();
  const [shop, setShop] = useState(null);
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [query, setQuery] = useState("");
  const [filters, setFilters] = useState({ category: ALL, type: ALL, size: ALL, color: ALL });

  // This is the page a customer lands on from a QR code, so the tab should read
  // as the shop's own storefront rather than ours.
  usePageTitle(shop?.shopName || "Shop");

  useEffect(() => {
    api.get(`/catalog/${shopSlug}`)
      .then((r) => { setShop(r.data.shop); setProducts(r.data.products || []); })
      .catch((e) => setError(e?.response?.data?.message || "Shop not found"))
      .finally(() => setLoading(false));
  }, [shopSlug]);

  const opts = useMemo(() => {
    const uniq = (k) => [ALL, ...new Set(products.map((p) => p[k]).filter(Boolean))];
    return { category: uniq("categoryName"), type: uniq("productType"), size: uniq("size"), color: uniq("color") };
  }, [products]);

  const filtered = products.filter((p) =>
    (filters.category === ALL || p.categoryName === filters.category) &&
    (filters.type === ALL || p.productType === filters.type) &&
    (filters.size === ALL || p.size === filters.size) &&
    (filters.color === ALL || p.color === filters.color) &&
    (p.productName.toLowerCase().includes(query.toLowerCase()) ||
     (p.brand || "").toLowerCase().includes(query.toLowerCase()))
  );

  const setF = (k) => (e) => setFilters({ ...filters, [k]: e.target.value });

  if (loading) return <div style={{ padding: 40, textAlign: "center" }}>Loading…</div>;
  if (error) return <div style={{ padding: 60, textAlign: "center", color: "var(--muted)" }}>{error}</div>;

  return (
    <div className="catalog">
      {/* Shop header */}
      <header className="catalog-header">
        <div className="catalog-header-inner">
          <div className="catalog-logo">
            {shop.logoUrl ? <img src={shop.logoUrl} alt="logo" /> : <span>🏬</span>}
          </div>
          <div className="catalog-shopinfo">
            <h1>{shop.shopName}</h1>
            <div className="catalog-contact">
              <a href={`tel:${shop.phone}`}>📞 {shop.phone}</a>
              {shop.alternateNumber && <a href={`tel:${shop.alternateNumber}`}>📱 {shop.alternateNumber}</a>}
              <span>📍 {shop.address}</span>
            </div>
          </div>
        </div>
      </header>

      <div className="catalog-body">
        {/* Search + filters */}
        <div className="card catalog-filters">
          <label className="catalog-search">
            <div className="catalog-label">Search</div>
            <input className="input" placeholder="Search products or brand…" value={query} onChange={(e) => setQuery(e.target.value)} />
          </label>
          <Filter label="Category" value={filters.category} onChange={setF("category")} options={opts.category} />
          <Filter label="Type" value={filters.type} onChange={setF("type")} options={opts.type} />
          <Filter label="Size" value={filters.size} onChange={setF("size")} options={opts.size} />
          <Filter label="Color" value={filters.color} onChange={setF("color")} options={opts.color} />
        </div>

        {/* Products */}
        {filtered.length === 0 ? (
          <div className="card" style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>
            <Anim name="empty" size={200} />
            <p style={{ marginTop: 8 }}>No products found.</p>
          </div>
        ) : (
          <div className="catalog-grid">
            {filtered.map((p) => (
              <div className="card catalog-card" key={p.productId}>
                <ImageCarousel
                  images={p.imageUrls?.length ? p.imageUrls : (p.imageUrl ? [p.imageUrl] : [])}
                  alt={p.productName}
                  aspect="4 / 3"
                  overlay={p.availableQty <= 0 ? <span className="catalog-oos">Out of stock</span> : null}
                />
                <div className="catalog-card-body">
                  <div className="catalog-badges">
                    {p.categoryName && <span className="badge badge-green">{p.categoryName}</span>}
                    {p.productType && <span className="badge" style={{ background: "#eef2ff", color: "#4338ca" }}>{p.productType}</span>}
                  </div>
                  <h3>{p.productName}</h3>
                  <p className="catalog-meta">{[p.brand, p.color, p.size && `Size ${p.size}`].filter(Boolean).join(" · ")}</p>
                  <div className="catalog-price">₹{p.basePrice}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <footer className="catalog-footer">
        {shop.shopName} · <a href={`tel:${shop.phone}`}>📞 {shop.phone}</a> · Powered by ScanStore
      </footer>
    </div>
  );
}

function Filter({ label, value, onChange, options }) {
  return (
    <label className="catalog-filter">
      <div className="catalog-label">{label}</div>
      <select className="input" value={value} onChange={onChange}>
        {options.map((o) => <option key={o} value={o}>{o}</option>)}
      </select>
    </label>
  );
}
