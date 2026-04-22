import { useState } from "react";
import { invoke } from "@tauri-apps/api/core";
import type { DetectedDevice } from "../types";

interface FirmwareInfo {
  current_version: number;
  latest_version: number | null;
  latest_version_name: string | null;
  download_url: string | null;
  file_size: number | null;
  update_available: boolean;
  release_notes: string | null;
}

interface UpdatesPageProps {
  devices: DetectedDevice[];
}

export function UpdatesPage({ devices }: UpdatesPageProps) {
  const [checking, setChecking] = useState(false);
  const [installing, setInstalling] = useState(false);
  const [firmwareInfo, setFirmwareInfo] = useState<Record<string, FirmwareInfo>>({});
  const [error, setError] = useState<string | null>(null);

  async function handleCheckAll() {
    setChecking(true);
    setError(null);
    try {
      const results: Record<string, FirmwareInfo> = {};
      for (const device of devices) {
        const info = await invoke<FirmwareInfo>("check_firmware_update", { unitId: device.unit_id });
        results[device.unit_id] = info;
      }
      setFirmwareInfo(results);
    } catch (err) {
      setError(String(err));
    } finally {
      setChecking(false);
    }
  }

  async function handleInstall(unitId: string, downloadUrl: string) {
    setInstalling(true);
    setError(null);
    try {
      await invoke("install_firmware", { unitId, downloadUrl });
      const updated = { ...firmwareInfo };
      if (updated[unitId]) {
        updated[unitId].update_available = false;
      }
      setFirmwareInfo(updated);
    } catch (err) {
      setError(String(err));
    } finally {
      setInstalling(false);
    }
  }

  if (devices.length === 0) {
    return (
      <div className="page">
        <div className="page-header">
          <h2>Firmware &amp; Maps</h2>
        </div>
        <div className="placeholder-content">
          <span className="placeholder-icon">⬆</span>
          <h3>No Devices Connected</h3>
          <p>Connect a Garmin device via USB to check for firmware updates.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="page-header">
        <h2>Firmware &amp; Maps</h2>
        <button className="btn btn-accent" onClick={handleCheckAll} disabled={checking}>
          {checking ? "Checking…" : "Check for Updates"}
        </button>
      </div>

      {devices.map((device) => {
        const info = firmwareInfo[device.unit_id];
        return (
          <div key={device.unit_id} className="backup-card">
            <div className="backup-card-header">
              <div>
                <h3>{device.description}</h3>
                <span className="mono text-secondary">
                  v{(parseInt(device.software_version) / 100).toFixed(2)}
                  {info?.update_available && info.latest_version_name && (
                    <> → v{info.latest_version_name}</>
                  )}
                </span>
              </div>
              <div className="backup-actions">
                {info?.update_available && info.download_url ? (
                  <button
                    className="btn btn-accent"
                    onClick={() => handleInstall(device.unit_id, info.download_url!)}
                    disabled={installing}
                  >
                    {installing ? "Installing…" : "Install Update"}
                  </button>
                ) : info && !info.update_available ? (
                  <span className="text-secondary">Up to date</span>
                ) : null}
              </div>
            </div>
          </div>
        );
      })}

      {error && (
        <div className="backup-result error">
          <h4>✗ Error</h4>
          <p>{error}</p>
        </div>
      )}
    </div>
  );
}
