import { DeviceList } from "../components/DeviceList";
import type { DetectedDevice } from "../types";

interface DevicesPageProps {
  devices: DetectedDevice[];
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}

export function DevicesPage({ devices, loading, error, onRefresh }: DevicesPageProps) {
  return (
    <div className="page">
      <div className="page-header">
        <h2>Devices</h2>
        <button className="btn btn-accent" onClick={onRefresh} disabled={loading}>
          {loading ? "Scanning…" : "Refresh"}
        </button>
      </div>
      <DeviceList devices={devices} loading={loading} error={error} onRefresh={onRefresh} />
    </div>
  );
}
