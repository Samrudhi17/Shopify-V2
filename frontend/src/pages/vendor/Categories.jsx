// Categories: Men / Women / Kids (seeded when the shop is created).
// Select which categories to show on the catalog, then click Save.
import { useEffect, useState } from "react";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";

const ICONS = { Men: "👔", Women: "👗", Kids: "🧒" };

export default function Categories() {
  const { user } = useAuth();
  const [categories, setCategories] = useState([]);
  const [selected, setSelected] = useState(() => new Set());
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/categories?vendorId=${user.id}`)
      .then((r) => {
        setCategories(r.data);
        setSelected(new Set(r.data.filter((c) => c.status === "Active").map((c) => c.categoryId)));
      })
      .catch(() => setCategories([]))
      .finally(() => setLoading(false));
  }, [user]);

  function toggle(id) {
    setSaved(false);
    setSelected((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  async function save() {
    setSaving(true);
    setSaved(false);
    try {
      await api.post("/categories/select", { vendorId: user.id, selectedIds: [...selected] });
      setSaved(true);
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <div>Loading…</div>;

  return (
    <div>
      <div className="page-head">
        <div>
          <h1 style={{ fontSize: 28 }}>Categories</h1>
          <div className="subtitle">Select the categories to show on your catalog, then save.</div>
        </div>
        {categories.length > 0 && (
          <button className="btn btn-primary" onClick={save} disabled={saving}>
            {saving ? "Saving…" : "Save selection"}
          </button>
        )}
      </div>

      {saved && <div className="alert alert-success">Categories saved.</div>}

      {categories.length === 0 ? (
        <div className="card" style={{ color: "var(--muted)" }}>
          Categories appear here after you create your shop in <strong>Profile</strong>.
        </div>
      ) : (
        <div className="grid grid-3">
          {categories.map((c) => {
            const isSel = selected.has(c.categoryId);
            return (
              <button
                key={c.categoryId}
                onClick={() => toggle(c.categoryId)}
                className="card"
                style={{
                  textAlign: "center",
                  cursor: "pointer",
                  border: isSel ? "2px solid var(--brand)" : "1px solid var(--border)",
                  background: isSel ? "#faf5ff" : "#fff",
                }}
              >
                <div style={{ fontSize: 40 }}>{ICONS[c.categoryName] || "🏷️"}</div>
                <h3 style={{ marginTop: 10 }}>{c.categoryName}</h3>
                <span className={"badge " + (isSel ? "badge-green" : "badge-red")} style={{ marginTop: 8 }}>
                  {isSel ? "Selected" : "Not selected"}
                </span>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
