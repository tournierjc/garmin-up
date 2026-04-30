import { useState } from "react";
import { Sidebar } from "./components/Sidebar";
import { DevicesPage } from "./pages/DevicesPage";
import { BackupPage } from "./pages/BackupPage";
import { SyncPage } from "./pages/SyncPage";
import { UpdatesPage } from "./pages/UpdatesPage";
import { MusicPage } from "./pages/MusicPage";
import { useDevices } from "./hooks/useDevices";
import type { Page } from "./types";
import "./App.css";

function App() {
  const [activePage, setActivePage] = useState<Page>("devices");
  const { devices, loading, error, refresh } = useDevices();

  return (
    <div className="app-layout">
      <Sidebar
        activePage={activePage}
        onNavigate={setActivePage}
        deviceCount={devices.length}
      />
      <main className="content">
        {activePage === "devices" && (
          <DevicesPage devices={devices} loading={loading} error={error} onRefresh={refresh} />
        )}
        {activePage === "backup" && <BackupPage devices={devices} />}
        {activePage === "sync" && <SyncPage />}
        {activePage === "updates" && <UpdatesPage devices={devices} />}
        {activePage === "music" && <MusicPage devices={devices} />}
      </main>
    </div>
  );
}

export default App;
