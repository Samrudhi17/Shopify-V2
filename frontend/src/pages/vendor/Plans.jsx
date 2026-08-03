// Pricing page. Prices come from the API — the amount charged is always looked
// up server-side from the plan code, so nothing here can change what is billed.
import { useCallback, useEffect, useMemo, useState } from "react";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";
import { loadRazorpayCheckout } from "../../services/razorpay";
import useSubscription from "../../hooks/useSubscription";
import usePageTitle from "../../hooks/usePageTitle";

// 30 -> 1 month, 182 -> 6, 365 -> 12.
const monthsOf = (durationDays) => Math.max(1, Math.round(durationDays / 30));

// Dates come back as UTC; show them in the vendor's own timezone.
const fmtDate = (iso) =>
  iso
    ? new Date(iso).toLocaleDateString("en-IN", { day: "numeric", month: "short", year: "numeric" })
    : "—";

const fmtDateTime = (iso) =>
  iso
    ? new Date(iso).toLocaleString("en-IN", {
        day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit",
      })
    : "—";

function StatusBadge({ status }) {
  const tone =
    status === "Active" ? "badge-green"
    : status === "Trialing" ? "badge-green"
    : "badge-red";
  return <span className={`badge ${tone}`}>{status || "None"}</span>;
}

// The headline panel: what you are on, and until when.
function CurrentPlanCard({ subscription }) {
  if (!subscription) return null;

  const { planName, status, startsAt, endsAt, isActive, daysRemaining } = subscription;

  return (
    <div className="card" style={{ marginBottom: 24 }}>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 24, alignItems: "flex-start" }}>
        <div style={{ minWidth: 180 }}>
          <div style={{ color: "var(--muted)", fontSize: 13, marginBottom: 4 }}>Current plan</div>
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <span style={{ fontSize: 22, fontWeight: 700, color: "var(--text-h)" }}>
              {planName || "No plan"}
            </span>
            <StatusBadge status={status} />
          </div>
        </div>

        <div>
          <div style={{ color: "var(--muted)", fontSize: 13, marginBottom: 4 }}>Started</div>
          <div style={{ fontWeight: 600 }}>{fmtDate(startsAt)}</div>
        </div>

        <div>
          <div style={{ color: "var(--muted)", fontSize: 13, marginBottom: 4 }}>
            {isActive ? "Renews / expires on" : "Expired on"}
          </div>
          <div style={{ fontWeight: 600 }}>{fmtDate(endsAt)}</div>
        </div>

        <div>
          <div style={{ color: "var(--muted)", fontSize: 13, marginBottom: 4 }}>Time left</div>
          <div style={{ fontWeight: 600, color: isActive ? undefined : "#b91c1c" }}>
            {isActive ? `${daysRemaining} day${daysRemaining === 1 ? "" : "s"}` : "None"}
          </div>
        </div>
      </div>

      {!isActive && (
        <div className="alert alert-error" style={{ marginTop: 16, marginBottom: 0 }}>
          Your shop's catalog and QR code are offline until you choose a plan below.
        </div>
      )}
    </div>
  );
}

export default function Plans() {
  usePageTitle("Plans & Billing");
  const { user } = useAuth();
  const { subscription, refresh } = useSubscription();
  const [plans, setPlans] = useState([]);
  const [history, setHistory] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busyCode, setBusyCode] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const loadHistory = useCallback(() => {
    api.get("/subscriptions/history")
      .then((r) => setHistory(r.data))
      .catch(() => setHistory(null));
  }, []);

  useEffect(() => {
    api.get("/subscriptions/plans")
      .then((r) => setPlans(r.data))
      .catch(() => setError("Could not load plans. Please refresh."))
      .finally(() => setLoading(false));

    loadHistory();
  }, [loadHistory]);

  // Everything is priced against the monthly plan, so the savings shown are the
  // real ones even after a price change.
  const monthlyRate = useMemo(() => {
    const monthly = plans.find((p) => p.code === "monthly");
    return monthly ? monthly.amount : null;
  }, [plans]);

  const paidPlans = plans.filter((p) => p.code !== "trial");

  async function buy(plan) {
    setError("");
    setSuccess("");
    setBusyCode(plan.code);

    try {
      const Razorpay = await loadRazorpayCheckout();

      // The server prices the order; the client only names the plan.
      const { data: order } = await api.post("/subscriptions/order", { planCode: plan.code });

      const checkout = new Razorpay({
        key: order.keyId,
        order_id: order.orderId,
        amount: order.amountPaise,
        currency: order.currency,
        name: "ScanStore",
        description: `${order.planName} subscription`,
        prefill: { name: user?.name || "", email: user?.email || "" },
        theme: { color: "#7c3aed" },
        handler: async (response) => {
          try {
            await api.post("/subscriptions/verify", {
              razorpayOrderId: response.razorpay_order_id,
              razorpayPaymentId: response.razorpay_payment_id,
              razorpaySignature: response.razorpay_signature,
            });
            setSuccess("Payment successful. Your subscription has been extended.");
            refresh();
            loadHistory();
          } catch (err) {
            // The webhook still settles this, so the vendor is not out of pocket
            // — say so rather than implying the payment was lost.
            setError(
              err?.response?.data?.message ||
                "We could not confirm the payment here. If you were charged, it will apply shortly."
            );
          } finally {
            setBusyCode(null);
          }
        },
        modal: {
          // Dismissing the modal has to clear the spinner, or the button stays
          // stuck on "Opening…" forever.
          ondismiss: () => setBusyCode(null),
        },
      });

      checkout.on("payment.failed", (response) => {
        setError(response?.error?.description || "Payment failed. Please try again.");
        setBusyCode(null);
      });

      checkout.open();
    } catch (err) {
      setError(err?.response?.data?.message || err.message || "Could not start the payment.");
      setBusyCode(null);
    }
  }

  if (loading) return <div>Loading…</div>;

  return (
    <div>
      <div className="page-head">
        <div>
          <h1 style={{ fontSize: 28 }}>Plans &amp; Billing</h1>
          <div className="subtitle">Your subscription, renewals and payment history.</div>
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <CurrentPlanCard subscription={subscription} />

      <h2 style={{ fontSize: 20, marginBottom: 12 }}>
        {subscription?.isActive ? "Extend or change your plan" : "Choose a plan"}
      </h2>

      <div className="grid grid-3" style={{ alignItems: "stretch" }}>
        {paidPlans.map((plan) => {
          const months = monthsOf(plan.durationDays);
          const perMonth = Math.round(plan.amount / months);
          const discount = monthlyRate
            ? Math.round((1 - perMonth / monthlyRate) * 100)
            : 0;
          const isBest = plan.code === "yearly";

          return (
            <div
              key={plan.code}
              className="card"
              style={{
                display: "flex",
                flexDirection: "column",
                gap: 10,
                position: "relative",
                // The annual plan is the one worth pushing, so it gets the accent
                // border rather than just a badge.
                borderColor: isBest ? "var(--brand)" : undefined,
                borderWidth: isBest ? 2 : undefined,
              }}
            >
              {discount > 0 && (
                <span
                  className="badge badge-green"
                  style={{ position: "absolute", top: 14, right: 14 }}
                >
                  Save {discount}%
                </span>
              )}

              <h3 style={{ marginBottom: 0 }}>{plan.name}</h3>

              {/* The per-month figure is the headline: ₹167/mo reads far better
                  than ₹1,999, and the total sits underneath so nothing is hidden. */}
              <div>
                <span style={{ fontSize: 34, fontWeight: 800, color: "var(--text-h)" }}>
                  ₹{perMonth.toLocaleString("en-IN")}
                </span>
                <span style={{ color: "var(--muted)", fontSize: 15 }}> /month</span>
              </div>

              <div style={{ color: "var(--muted)", fontSize: 14 }}>
                ₹{plan.amount.toLocaleString("en-IN")} billed once
                {months > 1 ? ` for ${months} months` : " each month"}
              </div>

              <button
                className={isBest ? "btn btn-primary" : "btn btn-outline"}
                style={{ marginTop: "auto" }}
                onClick={() => buy(plan)}
                disabled={busyCode !== null}
              >
                {busyCode === plan.code ? "Opening…" : "Choose plan"}
              </button>
            </div>
          );
        })}
      </div>

      <p style={{ color: "var(--muted)", fontSize: 13, marginTop: 18 }}>
        Renewing early adds to the time you have left — you never lose paid days.
      </p>

      {history?.payments?.length > 0 && (
        <>
          <h2 style={{ fontSize: 20, margin: "32px 0 12px" }}>Payment history</h2>
          <div className="card" style={{ padding: 0, overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 620 }}>
              <thead>
                <tr style={{ textAlign: "left", color: "var(--muted)", fontSize: 13 }}>
                  <th style={cellStyle}>Date</th>
                  <th style={cellStyle}>Plan</th>
                  <th style={cellStyle}>Amount</th>
                  <th style={cellStyle}>Status</th>
                  <th style={cellStyle}>Reference</th>
                </tr>
              </thead>
              <tbody>
                {history.payments.map((p) => (
                  <tr key={p.orderId} style={{ borderTop: "1px solid var(--border)" }}>
                    <td style={cellStyle}>{fmtDateTime(p.createdAt)}</td>
                    <td style={cellStyle}>{p.planName}</td>
                    <td style={cellStyle}>₹{p.amount.toLocaleString("en-IN")}</td>
                    <td style={cellStyle}>
                      <span className={`badge ${p.status === "Paid" ? "badge-green" : "badge-red"}`}>
                        {p.status}
                      </span>
                    </td>
                    {/* The order id is what Razorpay support asks for. */}
                    <td style={{ ...cellStyle, fontFamily: "monospace", fontSize: 12 }}>
                      {p.paymentId || p.orderId}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {history?.terms?.length > 0 && (
        <>
          <h2 style={{ fontSize: 20, margin: "32px 0 12px" }}>Subscription history</h2>
          <div className="card" style={{ padding: 0, overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 520 }}>
              <thead>
                <tr style={{ textAlign: "left", color: "var(--muted)", fontSize: 13 }}>
                  <th style={cellStyle}>Plan</th>
                  <th style={cellStyle}>From</th>
                  <th style={cellStyle}>To</th>
                  <th style={cellStyle}>Status</th>
                </tr>
              </thead>
              <tbody>
                {history.terms.map((t) => (
                  <tr
                    key={`${t.planName}-${t.startsAt}`}
                    style={{ borderTop: "1px solid var(--border)" }}
                  >
                    <td style={cellStyle}>
                      {t.planName}
                      {t.isCurrent && (
                        <span style={{ color: "var(--muted)", fontSize: 12 }}> · current</span>
                      )}
                    </td>
                    <td style={cellStyle}>{fmtDate(t.startsAt)}</td>
                    <td style={cellStyle}>{fmtDate(t.endsAt)}</td>
                    <td style={cellStyle}>
                      <StatusBadge status={t.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}

const cellStyle = { padding: "12px 16px", whiteSpace: "nowrap" };
