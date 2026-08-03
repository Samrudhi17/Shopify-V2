// Admin Settings: roles & permissions overview + admin accounts.
import { useEffect, useState } from "react";
import api from "../../services/api";
import usePageTitle from "../../hooks/usePageTitle";

const ROLES = [
  { role: "Admin", perms: ["Manage vendors", "Activate / deactivate shops", "View all reports", "System settings"] },
  { role: "Vendor", perms: ["Manage own shop & profile", "Add / edit / remove products", "Manage stock & categories", "View own reports"] },
];

export default function AdminSettings() {
  usePageTitle("Settings");
  const [admins, setAdmins] = useState([]);
  useEffect(() => { api.get("/admin/admins").then((r) => setAdmins(r.data)).catch(() => {}); }, []);

  return (
    <div>
      <div className="page-head"><h1 style={{ fontSize: 28 }}>System Settings</h1></div>

      <h2 style={{ fontSize: 18, margin: "8px 0 14px" }}>Roles & Permissions</h2>
      <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 20 }}>
        {ROLES.map((r) => (
          <div className="card" key={r.role}>
            <span className={"badge " + (r.role === "Admin" ? "badge-green" : "")} style={r.role === "Vendor" ? { background: "#eef2ff", color: "#4338ca" } : {}}>{r.role}</span>
            <ul style={{ marginTop: 12, paddingLeft: 18, color: "var(--text)", lineHeight: 1.9 }}>
              {r.perms.map((p) => <li key={p}>{p}</li>)}
            </ul>
          </div>
        ))}
      </div>

      <h2 style={{ fontSize: 18, margin: "28px 0 14px" }}>Administrators</h2>
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <table className="table">
          <thead><tr><th>Name</th><th>Email</th><th>Role</th></tr></thead>
          <tbody>
            {admins.map((a) => (
              <tr key={a.adminId}><td style={{ fontWeight: 600, color: "var(--text-h)" }}>{a.name}</td><td>{a.email}</td>
                <td><span className="badge badge-green">{a.role}</span></td></tr>
            ))}
            {admins.length === 0 && <tr><td colSpan={3} style={{ textAlign: "center", color: "var(--muted)", padding: 40 }}>No admins.</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  );
}
