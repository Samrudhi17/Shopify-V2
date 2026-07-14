// Login: username (email) + password. Signs in via Firebase, then loads the
// backend profile to determine role (Admin vs Vendor) and routes accordingly.
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { signInWithEmailAndPassword } from "firebase/auth";
import { auth, isFirebaseConfigured } from "../../services/firebase";
import { useAuth } from "../../context/AuthContext";
import api from "../../services/api";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const { login } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    if (!isFirebaseConfigured) {
      setError("Firebase is not configured yet. Add VITE_FIREBASE_* keys to frontend/.env.");
      return;
    }
    try {
      const cred = await signInWithEmailAndPassword(auth, email, password);
      const token = await cred.user.getIdToken();
      localStorage.setItem("token", token);

      // Backend resolves whether this Firebase UID is an Admin or a Vendor.
      const { data } = await api.get(`/auth/me?firebaseUid=${cred.user.uid}`);
      login(data, token);

      navigate(data.role === "Admin" ? "/admin" : "/vendor");
    } catch (err) {
      setError(err?.response?.data?.message || err.message || "Login failed");
    }
  }

  return (
    <div className="auth-wrap">
      <div className="auth-card">
        <div className="brand" style={{ marginBottom: 18 }}>
          <span className="brand-mark">▦</span> QR Digital Shop
        </div>
        <h2>Welcome back</h2>
        <p className="auth-sub">Log in to manage your shop.</p>

        <form onSubmit={handleSubmit}>
          <label className="field">
            <span>Username (email)</span>
            <input className="input" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@example.com" />
          </label>
          <label className="field">
            <span>Password</span>
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" />
          </label>
          {error && <div className="alert alert-error">{error}</div>}
          <button type="submit" className="btn btn-primary btn-block">Login</button>
        </form>

        <div className="auth-foot">
          <Link to="/forgot-password">Forgot password?</Link>
          <Link to="/register">Create an account</Link>
        </div>
      </div>
    </div>
  );
}
