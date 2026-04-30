import { useEffect, useMemo, useState } from "react";
import { open, confirm } from "@tauri-apps/plugin-dialog";
import { invoke } from "@tauri-apps/api/core";
import type { DetectedDevice } from "../types";

interface InstalledApp {
  file_name: string;
  path: string;
  size: number;
  app_type: "WatchApp" | "WatchFace" | "DataField" | "Widget" | "Unknown";
}

interface ConnectIQPageProps {
  devices: DetectedDevice[];
}

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  const kb = bytes / 1024;
  if (kb < 1024) return `${kb.toFixed(1)} KB`;
  const mb = kb / 1024;
  return `${mb.toFixed(1)} MB`;
}

export function ConnectIQPage({ devices }: ConnectIQPageProps) {
  const [selectedUnitId, setSelectedUnitId] = useState<string>(() => devices[0]?.unit_id ?? "");
  const [apps, setApps] = useState<InstalledApp[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [installing, setInstalling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedDevice = useMemo(
    () => devices.find((d) => d.unit_id === selectedUnitId) ?? null,
    [devices, selectedUnitId],
  );

  useEffect(() => {
    if (!selectedUnitId && devices[0]) setSelectedUnitId(devices[0].unit_id);
  }, [devices, selectedUnitId]);

  async function refresh() {
    if (!selectedDevice) return;
    setLoading(true);
    setError(null);
    try {
      const res = await invoke<InstalledApp[]>("list_iq_apps", { unitId: selectedDevice.unit_id });
      setApps(res);
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    setApps(null);
    if (selectedDevice) {
      refresh();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedUnitId]);

  async function installPrg() {
    if (!selectedDevice) return;
    setInstalling(true);
    setError(null);
    try {
      const picked = await open({
        multiple: true,
        directory: false,
        title: "Select Connect IQ .prg files",
        filters: [{ name: "Connect IQ Apps", extensions: ["prg", "PRG"] }],
      });

      const items = (Array.isArray(picked) ? picked : picked ? [picked] : []) as unknown[];
      const paths = items.map((p) => (typeof p === "string" ? p : (p as { path: string }).path));
      if (paths.length === 0) return;

      for (const fullPath of paths) {
        const fileName = fullPath.split(/[/\\\\]/).pop() || fullPath;
        await invoke("install_iq_app", {
          unitId: selectedDevice.unit_id,
          sourcePath: fullPath,
          fileName,
        });
      }

      await refresh();
    } catch (e) {
      setError(String(e));
    } finally {
      setInstalling(false);
    }
  }

  async function removeApp(app: InstalledApp) {
    if (!selectedDevice) return;
    setError(null);
    const ok = await confirm(
      `Remove ${app.file_name}?\n\nThis will also remove the matching SETTINGS (.set) and DATA (.dat) files if present.`,
      { title: "Remove Connect IQ app", kind: "warning" },
    );
    if (!ok) return;

    setInstalling(true);
    try {
      await invoke("remove_iq_app", { unitId: selectedDevice.unit_id, fileName: app.file_name });
      await refresh();
    } catch (e) {
      setError(String(e));
    } finally {
      setInstalling(false);
    }
  }

  if (devices.length === 0) {
    return (
      <div className="page">
        <div className="page-header">
          <h2>Connect IQ</h2>
        </div>
        <div className="placeholder-content">
          <span className="placeholder-icon">🧩</span>
          <h3>No Devices Connected</h3>
          <p>Connect a Garmin device via USB to manage Connect IQ apps and watch faces.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="page-header">
        <h2>Connect IQ</h2>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <button className="btn" onClick={refresh} disabled={loading || installing || !selectedDevice}>
            {loading ? "Refreshing…" : "Refresh"}
          </button>
          <button className="btn btn-accent" onClick={installPrg} disabled={installing || loading || !selectedDevice}>
            {installing ? "Working…" : "Install .prg"}
          </button>
        </div>
      </div>

      <div className="backup-card">
        <div className="backup-card-header">
          <div>
            <h3>Target device</h3>
            <span className="text-secondary">Apps are stored under Garmin/Apps on the device.</span>
          </div>
        </div>
        <div style={{ display: "grid", gap: "0.5rem" }}>
          <select
            value={selectedUnitId}
            onChange={(e) => setSelectedUnitId(e.target.value)}
            className="mono"
            style={{ padding: "0.5rem", borderRadius: 8 }}
          >
            {devices.map((d) => (
              <option key={d.unit_id} value={d.unit_id}>
                {d.description} ({d.unit_id})
              </option>
            ))}
          </select>
          {selectedDevice && (
            <div className="mono text-secondary" style={{ fontSize: "0.85rem" }}>
              mount: {selectedDevice.mount_path}
            </div>
          )}
        </div>
      </div>

      <div className="backup-card">
        <div className="backup-card-header">
          <div>
            <h3>Installed apps</h3>
            <span className="text-secondary">
              {apps ? `${apps.length} file(s)` : "Loading…"}
            </span>
          </div>
        </div>

        {apps && apps.length === 0 && <span className="text-secondary">No Connect IQ apps found.</span>}

        {apps && apps.length > 0 && (
          <div style={{ display: "grid", gap: "0.5rem" }}>
            {apps.map((a) => (
              <div
                key={a.file_name}
                style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.75rem" }}
              >
                <div style={{ minWidth: 0 }}>
                  <div style={{ fontWeight: 600 }}>{a.file_name}</div>
                  <div className="mono text-secondary" style={{ fontSize: "0.85rem" }}>
                    {formatBytes(a.size)} · {a.app_type}
                  </div>
                </div>
                <button className="btn" onClick={() => removeApp(a)} disabled={installing || loading}>
                  Remove
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {error && (
        <div className="backup-result error">
          <h4>✗ Error</h4>
          <p>{error}</p>
        </div>
      )}
    </div>
  );
}

