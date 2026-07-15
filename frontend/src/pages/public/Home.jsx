// Landing page: hero, features (Services), About, Contact + Login/Register.
import { Link } from "react-router-dom";

const features = [
  { icon: "🔗", title: "Auto Catalog URL", text: "Register your shop and instantly get a shareable catalog link — domain/yourshop." },
  { icon: "📱", title: "Unique QR Code", text: "Every shop gets its own QR code customers scan to browse on their phones." },
  { icon: "📦", title: "Products & Inventory", text: "Manage categories, products, variants and live stock from one dashboard." },
  { icon: "🛒", title: "Mobile Catalog", text: "A fast, filterable storefront with search and cart — no e-commerce cost." },
];

export default function Home() {
  return (
    <div>
      <nav className="nav">
        <div className="brand">
          <span className="brand-mark">▦</span> QR Digital Shop
        </div>
        <div className="nav-links">
          <a href="#services">Services</a>
          <a href="#about">About</a>
          <a href="#contact">Contact</a>
          <Link to="/login" className="btn btn-ghost">Login</Link>
          <Link to="/register" className="btn btn-primary">Register</Link>
        </div>
      </nav>

      <header className="hero">
        <h1>Turn your shop into a <span className="grad">digital catalog</span></h1>
        <p>
          Local businesses can showcase products online without building a website.
          Register, add products, and share a QR code customers scan to shop.
        </p>
        <div className="hero-actions">
          <Link to="/register" className="btn btn-primary">Get started free</Link>
          <a href="#services" className="btn btn-outline">See how it works</a>
        </div>
      </header>

      <section id="services" className="section">
        <div className="container">
          <div style={{ textAlign: "center", marginBottom: 40 }}>
            <div className="section-tag">Services</div>
            <h2 style={{ marginTop: 8 }}>Everything to sell locally, online</h2>
          </div>
          <div className="grid grid-4">
            {features.map((f) => (
              <div className="card" key={f.title}>
                <div className="feature-icon">{f.icon}</div>
                <h3>{f.title}</h3>
                <p>{f.text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section id="about" className="section" style={{ background: "#fff", borderTop: "1px solid var(--border)", borderBottom: "1px solid var(--border)" }}>
        <div className="container" style={{ maxWidth: 760, textAlign: "center" }}>
          <div className="section-tag">About Us</div>
          <h2 style={{ margin: "8px 0 16px" }}>Digital transformation, made affordable</h2>
          <p style={{ color: "var(--muted)", fontSize: 17, lineHeight: 1.7 }}>
            The QR Based Digital Shop Catalog Platform helps local businesses create and
            manage online product catalogs without the cost and complexity of traditional
            e-commerce. Vendors register their shops, add products, manage inventory, and
            generate unique QR codes and catalog URLs — increasing product visibility and
            customer engagement with a simple, user-friendly solution.
          </p>
        </div>
      </section>

      <section id="contact" className="section">
        <div className="container" style={{ textAlign: "center" }}>
          <div className="section-tag">Contact Us</div>
          <h2 style={{ margin: "8px 0 16px" }}>Get in touch</h2>
          <p style={{ color: "var(--muted)" }}>📧 support@qrdigitalshop.example</p>
          <div style={{ marginTop: 24 }}>
            <Link to="/register" className="btn btn-primary">Create your shop</Link>
          </div>
        </div>
      </section>

      <footer className="footer">© {new Date().getFullYear()} QR Digital Shop · Built for local businesses</footer>
    </div>
  );
}
