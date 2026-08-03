// Vendor shell: sidebar (shop name on top + menu) with a nested <Outlet/>.
import { useEffect, useState } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import api from "../../services/api";
import useSubscription from "../../hooks/useSubscription";

const menu = [
  { to: "/vendor", label: "Shop Dashboard", end: true },
  { to: "/vendor/profile", label: "Profile" },
  { to: "/vendor/categories", label: "Categories" },
  { to: "/vendor/products", label: "Products" },
  { to: "/vendor/inventory", label: "Inventory" },
  { to: "/vendor/qrcode", label: "QR Code" },
  { to: "/vendor/reports", label: "Reports" },
  { to: "/vendor/plans", label: "Plans & Billing" },
  { to: "/vendor/settings", label: "Settings" },
];

// Start warning with under a week left, and turn it urgent in the last 3 days.
const WARN_FROM_DAYS = 7;
const URGENT_FROM_DAYS = 3;

function SubscriptionBanner({ subscription }) {
  if (!subscription) return null;

  const { isActive, daysRemaining } = subscription;

  if (isActive && daysRemaining > WARN_FROM_DAYS) return null;

  const urgent = !isActive || daysRemaining <= URGENT_FROM_DAYS;
  const days = `${daysRemaining} day${daysRemaining === 1 ? "" : "s"}`;

  return (
    <div
      className={urgent ? "alert alert-error" : "alert"}
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
        ...(urgent ? {} : { background: "#fffbeb", color: "#b45309", border: "1px solid #fde68a" }),
      }}
    >
      <span>
        {isActive
          ? `Your ${subscription.planName} plan ends in ${days}.`
          : "Your subscription has ended — your shop's catalog and QR code are offline."}
      </span>
      <Link to="/vendor/plans" className="btn btn-primary" style={{ padding: "6px 14px", fontSize: 14 }}>
        {isActive ? "Renew now" : "Reactivate"}
      </Link>
    </div>
  );
}

export default function VendorLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [shop, setShop] = useState(null);
  const { subscription } = useSubscription();

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/shops/vendor/${user.id}`).then((r) => setShop(r.data)).catch(() => setShop(null));
  }, [user]);

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <div className="shell">
      <aside className="sidebar">
        <div style={{ display: "flex", alignItems: "center", gap: 10, padding: "6px 10px 16px", borderBottom: "1px solid #1e293b", marginBottom: 12 }}>
          <div style={{ width: 40, height: 40, borderRadius: 10, overflow: "hidden", background: "#1e293b", display: "grid", placeItems: "center", flexShrink: 0 }}>
            {shop?.logoUrl ? <img src={shop.logoUrl} alt="logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} /> : <span>🏬</span>}
          </div>
          <span style={{ color: "#fff", fontWeight: 700, fontSize: 15 }}>{shop?.shopName || user?.shopName || "My Shop"}</span>
        </div>
        <nav className="side-nav">
          {menu.map((m) => (
            <NavLink
              key={m.to}
              to={m.to}
              end={m.end}
              className={({ isActive }) => "side-link" + (isActive ? " active" : "")}
            >
              {m.label}
            </NavLink>
          ))}
        </nav>
        <button onClick={handleLogout} className="btn btn-outline" style={{ marginTop: 12 }}>Logout</button>
      </aside>
      <main className="side-content">
        <SubscriptionBanner subscription={subscription} />
        <Outlet />
      </main>
    </div>
  );
}
