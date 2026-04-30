import { useMemo, useState } from "react";
import { open } from "@tauri-apps/plugin-dialog";
import { invoke } from "@tauri-apps/api/core";
import type { DetectedDevice } from "../types";

interface MusicPageProps {
  devices: DetectedDevice[];
}

export function MusicPage({ devices }: MusicPageProps) {
  const [selectedUnitId, setSelectedUnitId] = useState<string>(() => devices[0]?.unit_id ?? "");
  const [installing, setInstalling] = useState(false);
  const [result, setResult] = useState<string[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectedDevice = useMemo(
    () => devices.find((d) => d.unit_id === selectedUnitId) ?? null,
    [devices, selectedUnitId],
  );

  async function pickAndInstall() {
    if (!selectedDevice) return;
    setInstalling(true);
    setError(null);
    setResult(null);
    try {
      const picked = await open({
        multiple: true,
        directory: false,
        title: "Select music files",
        filters: [
          { name: "Audio", extensions: ["mp3", "m4a", "aac", "wav", "flac", "ogg"] },
          { name: "All files", extensions: ["*"] },
        ],
      });

      const items = (Array.isArray(picked) ? picked : picked ? [picked] : []) as unknown[];
      const paths = items.map((p) =>
        typeof p === "string" ? p : (p as { path: string }).path,
      );

      if (paths.length === 0) return;

      const installed = await invoke<string[]>("install_music_files", {
        unitId: selectedDevice.unit_id,
        sourcePaths: paths,
      });
      setResult(installed);
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
          <h2>Music</h2>
        </div>
        <div className="placeholder-content">
          <span className="placeholder-icon">🎵</span>
          <h3>No Devices Connected</h3>
          <p>Connect a Garmin device via USB to install music.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="page-header">
        <h2>Music</h2>
        <button className="btn btn-accent" onClick={pickAndInstall} disabled={installing || !selectedDevice}>
          {installing ? "Installing…" : "Add Music"}
        </button>
      </div>

      <div className="backup-card">
        <div className="backup-card-header">
          <div>
            <h3>Target device</h3>
            <span className="text-secondary">Music is copied to GARMIN/MUSIC on the device.</span>
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

      {result && (
        <div className="backup-result success">
          <h4>✓ Installed</h4>
          <p>{result.length} file(s) copied.</p>
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

