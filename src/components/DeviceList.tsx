import type { DetectedDevice } from "../types";
import { DeviceCard } from "./DeviceCard";

interface DeviceListProps {
  devices: DetectedDevice[];
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}

export function DeviceList({ devices, loading, error, onRefresh }: DeviceListProps) {
  if (loading && devices.length === 0) {
    return (
      <div className="device-list-status">
        <span className="spinner" />
        <p>Scanning for devices…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="device-list-status error">
        <p>Failed to detect devices: {error}</p>
        <button className="btn" onClick={onRefresh}>Retry</button>
      </div>
    );
  }

  if (devices.length === 0) {
    return (
      <div className="device-list-status">
        <span className="empty-icon">📡</span>
        <p>No Garmin devices detected</p>
        <p className="hint">Connect a Garmin device via USB</p>
        <button className="btn" onClick={onRefresh}>Scan Again</button>
      </div>
    );
  }

  return (
    <div className="device-list">
      {devices.map((device) => (
        <DeviceCard key={device.unit_id} device={device} />
      ))}
    </div>
  );
}
