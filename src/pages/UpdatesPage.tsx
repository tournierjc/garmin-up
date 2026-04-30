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

interface MapUpdateSummary {
  product_group: string;
  display_name: string;
  version: string;
  part_number: string;
  update_type: number;
  can_auto_start_download: boolean;
}

interface MapUpdatesDebug {
  serial: string;
  map_updates: MapUpdateSummary[];
  purchasable_products: string[];
  auto_check_enabled: boolean | null;
  verbose_details_len?: number | null;
  verbose_details_sample?: [string, string][] | null;
}

interface UpdatesPageProps {
  devices: DetectedDevice[];
}

export function UpdatesPage({ devices }: UpdatesPageProps) {
  const [checking, setChecking] = useState(false);
  const [installingFirmwareUnit, setInstallingFirmwareUnit] = useState<string | null>(null);
  const [installingMapKey, setInstallingMapKey] = useState<string | null>(null);
  const [firmwareInfo, setFirmwareInfo] = useState<Record<string, FirmwareInfo>>({});
  const [mapUpdates, setMapUpdates] = useState<Record<string, MapUpdateSummary[]>>({});
  const [mapDebug, setMapDebug] = useState<Record<string, MapUpdatesDebug>>({});
  const [error, setError] = useState<string | null>(null);

  async function handleCheckAll() {
    setChecking(true);
    setError(null);
    try {
      const results: Record<string, FirmwareInfo> = {};
      const mapResults: Record<string, MapUpdateSummary[]> = {};
      const debugResults: Record<string, MapUpdatesDebug> = {};
      for (const device of devices) {
        const info = await invoke<FirmwareInfo>("check_firmware_update", { unitId: device.unit_id });
        results[device.unit_id] = info;

        const maps = await invoke<MapUpdateSummary[]>("check_map_updates", { unitId: device.unit_id });
        mapResults[device.unit_id] = maps;

        // Always fetch debug so we can understand empty-map cases (matches backend response).
        const dbg = await invoke<MapUpdatesDebug>("check_map_updates_debug", { unitId: device.unit_id });
        debugResults[device.unit_id] = dbg;
      }
      setFirmwareInfo(results);
      setMapUpdates(mapResults);
      setMapDebug(debugResults);
    } catch (err) {
      setError(String(err));
    } finally {
      setChecking(false);
    }
  }

  async function handleInstall(unitId: string, downloadUrl: string) {
    setInstallingFirmwareUnit(unitId);
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
      setInstallingFirmwareUnit(null);
    }
  }

  async function handleInstallMap(unitId: string, partNumber: string) {
    const key = `${unitId}:${partNumber}`;
    setInstallingMapKey(key);
    setError(null);
    try {
      await invoke<string[]>("download_and_install_map_update", { unitId, partNumber });
      const updated = { ...mapUpdates };
      updated[unitId] = (updated[unitId] || []).filter((u) => u.part_number !== partNumber);
      setMapUpdates(updated);
    } catch (err) {
      setError(String(err));
    } finally {
      setInstallingMapKey(null);
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
          <p>Connect a Garmin device via USB to check for firmware and map updates.</p>
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
        const maps = mapUpdates[device.unit_id];
        const dbg = mapDebug[device.unit_id];
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
                    disabled={installingFirmwareUnit !== null || installingMapKey !== null}
                  >
                    {installingFirmwareUnit === device.unit_id ? "Installing…" : "Install Update"}
                  </button>
                ) : info && !info.update_available ? (
                  <span className="text-secondary">Up to date</span>
                ) : null}
              </div>
            </div>

            <div style={{ marginTop: "0.75rem" }}>
              <div className="mono text-secondary" style={{ marginBottom: "0.35rem" }}>
                Map updates
              </div>
              {maps ? (
                maps.length > 0 ? (
                  <div style={{ display: "grid", gap: "0.5rem" }}>
                    {maps.map((m) => (
                      <div
                        key={m.part_number}
                        style={{
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "space-between",
                          gap: "0.75rem",
                        }}
                      >
                        <div style={{ minWidth: 0 }}>
                          <div style={{ fontWeight: 600, overflow: "hidden", textOverflow: "ellipsis" }}>
                            {m.display_name || m.product_group}
                          </div>
                          <div className="mono text-secondary" style={{ fontSize: "0.85rem" }}>
                            {m.part_number} · v{m.version}
                          </div>
                        </div>
                        <button
                          className="btn btn-accent"
                          onClick={() => handleInstallMap(device.unit_id, m.part_number)}
                          disabled={installingFirmwareUnit !== null || installingMapKey !== null}
                        >
                          {installingMapKey === `${device.unit_id}:${m.part_number}` ? "Installing…" : "Install"}
                        </button>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div style={{ display: "grid", gap: "0.35rem" }}>
                    <span className="text-secondary">No map updates</span>
                    {dbg && (
                      <details className="mono text-secondary" style={{ fontSize: "0.85rem" }}>
                        <summary>Debug details</summary>
                        <div style={{ marginTop: "0.35rem", display: "grid", gap: "0.25rem" }}>
                          <div>serial: {dbg.serial}</div>
                          <div>purchasable_products: {dbg.purchasable_products.length}</div>
                          <div>auto_check_enabled: {String(dbg.auto_check_enabled)}</div>
                          {dbg.verbose_details_len !== undefined && (
                            <div>verbose_details_len: {String(dbg.verbose_details_len)}</div>
                          )}
                          {dbg.verbose_details_sample && dbg.verbose_details_sample.length > 0 && (
                            <div style={{ whiteSpace: "pre-wrap" }}>
                              details_sample:
                              {"\n"}
                              {dbg.verbose_details_sample
                                .slice(0, 25)
                                .map(([k, v]) => `${k}=${v}`)
                                .join("\n")}
                            </div>
                          )}
                        </div>
                      </details>
                    )}
                  </div>
                )
              ) : (
                <span className="text-secondary">Not checked yet</span>
              )}
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
