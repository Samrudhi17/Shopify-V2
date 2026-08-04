// Formatting helpers shared by the admin subscription views. Plain module (no
// components) so React Fast Refresh keeps working in the files that import it.

// Amounts arrive as rupees already — the API converts from the paise it stores.
export function money(rupees) {
  const n = Number(rupees ?? 0);
  return "₹" + n.toLocaleString("en-IN", { maximumFractionDigits: 2 });
}

export function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}
