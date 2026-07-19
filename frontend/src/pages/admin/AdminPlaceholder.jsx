// Lightweight placeholder for admin sub-pages not built out yet.
export default function AdminPlaceholder({ title, note }) {
  return (
    <div>
      <div className="page-head">
        <h1 style={{ fontSize: 28 }}>{title}</h1>
      </div>
      <div className="card" style={{ color: "var(--muted)" }}>
        {note || "This section is coming soon."}
      </div>
    </div>
  );
}
