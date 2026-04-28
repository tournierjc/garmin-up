import type { DetectedDevice } from "../types";

interface DeviceCardProps {
  device: DetectedDevice;
}

export function DeviceCard({ device }: DeviceCardProps) {
  const fitTypes = device.data_types.filter((dt) =>
    dt.name.startsWith("FIT")
  ).length;
  const mapTypes = device.data_types.filter(
    (dt) =>
      dt.name.includes("Map") ||
      dt.name.includes("DEM") ||
      dt.name.includes("Approach")
  ).length;

  return (
    <div className="device-card">
      <div className="device-card-header">
        <span className="device-icon">⌚</span>
        <div className="device-identity">
          <h3 className="device-name">{device.description}</h3>
          <span className="device-part-number">{device.part_number}</span>
        </div>
      </div>
      <div className="device-card-body">
        <div className="device-field">
          <span className="device-field-label">Unit ID</span>
          <span className="device-field-value mono">{device.unit_id}</span>
        </div>
        <div className="device-field">
          <span className="device-field-label">Firmware</span>
          <span className="device-field-value mono">v{device.software_version}</span>
        </div>
        <div className="device-field">
          <span className="device-field-label">Mount</span>
          <span className="device-field-value mono">{device.mount_path}</span>
        </div>
        <div className="device-stats">
          <div className="device-stat">
            <span className="device-stat-value">{device.data_types.length}</span>
            <span className="device-stat-label">Data Types</span>
          </div>
          <div className="device-stat">
            <span className="device-stat-value">{fitTypes}</span>
            <span className="device-stat-label">FIT</span>
          </div>
          <div className="device-stat">
            <span className="device-stat-value">{mapTypes}</span>
            <span className="device-stat-label">Maps</span>
          </div>
        </div>
      </div>
    </div>
  );
}
