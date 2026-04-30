import type { Page } from "../types";

const NAV_ITEMS: { id: Page; label: string; icon: string }[] = [
  { id: "devices", label: "Devices", icon: "⌚" },
  { id: "backup", label: "Backup", icon: "💾" },
  { id: "sync", label: "Sync", icon: "🔄" },
  { id: "updates", label: "Updates", icon: "⬆" },
  { id: "music", label: "Music", icon: "🎵" },
  { id: "connectiq", label: "Connect IQ", icon: "🧩" },
];

interface SidebarProps {
  activePage: Page;
  onNavigate: (page: Page) => void;
  deviceCount: number;
}

export function Sidebar({ activePage, onNavigate, deviceCount }: SidebarProps) {
  return (
    <nav className="sidebar">
      <div className="sidebar-header">
        <span className="sidebar-title">Garmin Connect</span>
        <span className="sidebar-subtitle">Device Manager</span>
      </div>
      <ul className="sidebar-nav">
        {NAV_ITEMS.map((item) => (
          <li key={item.id}>
            <button
              className={`sidebar-item ${activePage === item.id ? "active" : ""}`}
              onClick={() => onNavigate(item.id)}
            >
              <span className="sidebar-icon">{item.icon}</span>
              <span className="sidebar-label">{item.label}</span>
              {item.id === "devices" && deviceCount > 0 && (
                <span className="sidebar-badge">{deviceCount}</span>
              )}
            </button>
          </li>
        ))}
      </ul>
    </nav>
  );
}
