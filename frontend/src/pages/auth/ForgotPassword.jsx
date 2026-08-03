// Sends a Firebase password-reset email.
import { useState } from "react";
import { Link } from "react-router-dom";
import { sendPasswordResetEmail } from "firebase/auth";
import { auth, isFirebaseConfigured } from "../../services/firebase";
import usePageTitle from "../../hooks/usePageTitle";

export default function ForgotPassword() {
  usePageTitle("Reset Password");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setMessage("");
    if (!isFirebaseConfigured) {
      setError("Firebase is not configured yet. Add VITE_FIREBASE_* keys to frontend/.env.");
      return;
    }
    try {
      await sendPasswordResetEmail(auth, email);
      setMessage("Password reset email sent. Check your inbox.");
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="auth-wrap">
      <div className="auth-card">
        <div className="brand" style={{ marginBottom: 18 }}>
          <span className="brand-mark">▦</span> ScanStore
        </div>
        <h2>Forgot password</h2>
        <p className="auth-sub">We'll email you a reset link.</p>

        <form onSubmit={handleSubmit}>
          <label className="field">
            <span>Email</span>
            <input className="input" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@example.com" />
          </label>
          {message && <div className="alert alert-success">{message}</div>}
          {error && <div className="alert alert-error">{error}</div>}
          <button type="submit" className="btn btn-primary btn-block">Send reset link</button>
        </form>

        <div className="auth-foot" style={{ justifyContent: "center" }}>
          <Link to="/login">Back to login</Link>
        </div>
      </div>
    </div>
  );
}
