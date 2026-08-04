// Status badge shared by the admin subscription views, so a term renders the
// same on the dashboard summary and the full Subscriptions page.

// The API already reconciles the stored label against the clock, so a term that
// ran out is "Expired" here even if nothing has rewritten the row yet.
export function SubStatus({ row }) {
  if (row.status === "Expired") return <span className="badge badge-red">Expired</span>;

  const soon = row.daysRemaining <= 7;
  const isTrial = row.status === "Trialing";

  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
      <span
        className={"badge " + (isTrial ? "" : "badge-green")}
        style={isTrial ? { background: "#eef2ff", color: "#4338ca" } : undefined}
      >
        {isTrial ? "Trial" : "Active"}
      </span>
      <span style={{ fontSize: 12, color: soon ? "#dc2626" : "var(--muted)", fontWeight: soon ? 600 : 400 }}>
        {row.daysRemaining}d left
      </span>
    </span>
  );
}
