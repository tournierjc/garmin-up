import { useState, useEffect } from "react";
import { invoke } from "@tauri-apps/api/core";
import type { GarminSession, ActivityUploadResult } from "../types";

export function SyncPage() {
  const [loggedIn, setLoggedIn] = useState(false);
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [uploadResult, setUploadResult] = useState<ActivityUploadResult | null>(null);

  useEffect(() => {
    invoke<string | null>("connect_status").then((name) => {
      if (name) {
        setLoggedIn(true);
        setDisplayName(name);
      }
    });
  }, []);

  async function handleLogin(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const session = await invoke<GarminSession>("connect_login", { email, password });
      setLoggedIn(true);
      setDisplayName(session.display_name);
      setPassword("");
    } catch (err) {
      setError(String(err));
    } finally {
      setLoading(false);
    }
  }

  async function handleLogout() {
    await invoke("connect_logout");
    setLoggedIn(false);
    setDisplayName("");
  }

  async function handleUpload() {
    setError(null);
    setUploadResult(null);
    setLoading(true);
    try {
      const result = await invoke<ActivityUploadResult>("connect_upload_activity", {
        filePath: "/tmp/test.fit",
      });
      setUploadResult(result);
    } catch (err) {
      setError(String(err));
    } finally {
      setLoading(false);
    }
  }

  if (!loggedIn) {
    return (
      <div className="page">
        <div className="page-header">
          <h2>Garmin Connect</h2>
        </div>
        <div className="login-container">
          <form className="login-form" onSubmit={handleLogin}>
            <div className="form-field">
              <label className="form-label">Email</label>
              <input
                type="email"
                className="form-input"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
              />
            </div>
            <div className="form-field">
              <label className="form-label">Password</label>
              <input
                type="password"
                className="form-input"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>
            {error && <div className="form-error">{error}</div>}
            <button type="submit" className="btn btn-accent" disabled={loading} style={{ width: "100%" }}>
              {loading ? "Signing in…" : "Sign In"}
            </button>
          </form>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="page-header">
        <h2>Garmin Connect</h2>
        <div className="connect-user">
          <span className="text-secondary">{displayName}</span>
          <button className="btn" onClick={handleLogout}>Sign Out</button>
        </div>
      </div>

      <div className="sync-actions">
        <div className="backup-card">
          <div className="backup-card-header">
            <div>
              <h3>Upload Activity</h3>
              <span className="mono text-secondary">Sync FIT files to Garmin Connect</span>
            </div>
            <div className="backup-actions">
              <button className="btn btn-accent" onClick={handleUpload} disabled={loading}>
                {loading ? "Uploading…" : "Upload"}
              </button>
            </div>
          </div>
        </div>
      </div>

      {uploadResult && (
        <div className="backup-result">
          <h4>✓ Upload Complete</h4>
          <div className="device-field">
            <span className="device-field-label">Activity ID</span>
            <span className="device-field-value mono">{uploadResult.activity_id}</span>
          </div>
        </div>
      )}

      {error && (
        <div className="backup-result error">
          <h4>✗ Error</h4>
          <p>{error}</p>
        </div>
      )}
    </div>
  );
}
