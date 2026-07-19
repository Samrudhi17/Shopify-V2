// Admin shell: sidebar (Dashboard / Vendors / Shops / Reports / Settings) + <Outlet/>.
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

const menu = [
  { to: "/admin", label: "Dashboard", icon: "▤", end: true },
  { to: "/admin/vendors", label: "Vendors", icon: "👥" },
  { to: "/admin/shops", label: "Shops", icon: "🏬" },
  { to: "/admin/reports", label: "Reports", icon: "📊" },
  { to: "/admin/settings", label: "Settings", icon: "⚙️" },
];

export default function AdminLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="shop-name">
          <span style={{ opacity: 0.7, fontWeight: 600, fontSize: 12, display: "block" }}>ADMIN</span>
          {user?.name || "Administrator"}
        </div>
        <nav className="side-nav">
          {menu.map((m) => (
            <NavLink
              key={m.to}
              to={m.to}
              end={m.end}
              className={({ isActive }) => "side-link" + (isActive ? " active" : "")}
            >
              <span style={{ width: 20, textAlign: "center" }}>{m.icon}</span> {m.label}
            </NavLink>
          ))}
        </nav>
        <button onClick={handleLogout} className="btn btn-outline" style={{ marginTop: 12 }}>Logout</button>
      </aside>
      <main className="side-content">
        <Outlet />
      </main>
    </div>
  );
}
