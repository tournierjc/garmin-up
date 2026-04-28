import { useState } from "react";
import { invoke } from "@tauri-apps/api/core";
import type { DetectedDevice } from "../types";

interface BackupResult {
  files_copied: number;
  bytes_copied: number;
  backup_path: string;
  timestamp: string;
}

interface BackupPageProps {
  devices: DetectedDevice[];
}

export function BackupPage({ devices }: BackupPageProps) {
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<BackupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleBackup(unitId: string) {
    try {
      setRunning(true);
      setError(null);
      setResult(null);
      const res = await invoke<BackupResult>("backup_device", { unitId });
      setResult(res);
    } catch (e) {
      setError(String(e));
    } finally {
      setRunning(false);
    }
  }

  async function handleRestore(unitId: string) {
    try {
      setRunning(true);
      setError(null);
      setResult(null);
      const res = await invoke<BackupResult>("restore_device", { unitId });
      setResult(res);
    } catch (e) {
      setError(String(e));
    } finally {
      setRunning(false);
    }
  }

  if (devices.length === 0) {
    return (
      <div className="page">
        <div className="page-header">
          <h2>Backup &amp; Restore</h2>
        </div>
        <div className="placeholder-content">
          <span className="placeholder-icon">💾</span>
          <h3>No Devices Connected</h3>
          <p>Connect a Garmin device via USB to backup or restore data.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="page-header">
        <h2>Backup &amp; Restore</h2>
      </div>

      {devices.map((device) => (
        <div key={device.unit_id} className="backup-card">
          <div className="backup-card-header">
            <div>
              <h3>{device.description}</h3>
              <span className="mono text-secondary">{device.unit_id}</span>
            </div>
            <div className="backup-actions">
              <button
                className="btn btn-accent"
                onClick={() => handleBackup(device.unit_id)}
                disabled={running}
              >
                {running ? "Backing up…" : "Backup"}
              </button>
              <button
                className="btn"
                onClick={() => handleRestore(device.unit_id)}
                disabled={running}
              >
                {running ? "Restoring…" : "Restore"}
              </button>
            </div>
          </div>
        </div>
      ))}

      {result && (
        <div className="backup-result">
          <h4>✓ Operation Complete</h4>
          <div className="device-field">
            <span className="device-field-label">Files</span>
            <span className="device-field-value">{result.files_copied}</span>
          </div>
          <div className="device-field">
            <span className="device-field-label">Size</span>
            <span className="device-field-value mono">{formatBytes(result.bytes_copied)}</span>
          </div>
          <div className="device-field">
            <span className="device-field-label">Path</span>
            <span className="device-field-value mono">{result.backup_path}</span>
          </div>
          <div className="device-field">
            <span className="device-field-label">Timestamp</span>
            <span className="device-field-value">{result.timestamp}</span>
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

function formatBytes(bytes: number): string {
  if (bytes === 0) return "0 B";
  const units = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${units[i]}`;
}
