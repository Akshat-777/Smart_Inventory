import { type FormEvent, useState } from "react";
import { Navigate } from "react-router-dom";
import { getErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";

export function LoginPage() {
  const { token, login, register } = useAuth();
  const [isRegistering, setIsRegistering] = useState(false);
  const [email, setEmail] = useState(
    import.meta.env.PROD ? "" : "admin@inventory.local"
  );
  const [password, setPassword] = useState(import.meta.env.PROD ? "" : "Admin123!");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (token) return <Navigate to="/" replace />;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      if (isRegistering) {
        await register(email, password);
      } else {
        await login(email, password);
      }
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-wrap">
      <div className="login-hero">
        <div className="login-hero-bg" aria-hidden />
        <div className="login-hero-inner">
          <h2>Stock clarity, without the spreadsheet chaos.</h2>
          <p>Monitor products, warehouses, and orders from one calm dashboard built for teams who ship.</p>
        </div>
      </div>
      <div className="login-panel">
        <form className="card login-card" onSubmit={onSubmit}>
          <h1>{isRegistering ? "Create an account" : "Welcome back"}</h1>
          <p className="muted">
            {isRegistering 
              ? "Sign up for a new inventory workspace." 
              : "Sign in to your inventory workspace."}
          </p>
          <label className="field">
            <span>Email</span>
            <input
              autoComplete="username"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              type="email"
              required
            />
          </label>
          <label className="field">
            <span>Password</span>
            <input
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              type="password"
              required
            />
          </label>
          {error ? <div className="alert error">{error}</div> : null}
          <button className="btn primary" type="submit" disabled={loading}>
            {loading ? (isRegistering ? "Creating account…" : "Signing in…") : (isRegistering ? "Sign up" : "Sign in")}
          </button>
          <div style={{ marginTop: "1rem", textAlign: "center" }}>
            <button
              type="button"
              className="btn link"
              onClick={() => {
                setIsRegistering(!isRegistering);
                setError(null);
              }}
            >
              {isRegistering ? "Already have an account? Sign in" : "Don't have an account? Sign up"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
