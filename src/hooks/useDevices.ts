import { useState, useEffect, useCallback } from "react";
import { invoke } from "@tauri-apps/api/core";
import { listen } from "@tauri-apps/api/event";
import type { DetectedDevice } from "../types";

export function useDevices() {
  const [devices, setDevices] = useState<DetectedDevice[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await invoke<DetectedDevice[]>("refresh_devices");
      setDevices(result);
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    invoke<DetectedDevice[]>("list_devices")
      .then(setDevices)
      .catch((e) => setError(String(e)))
      .finally(() => setLoading(false));

    const unlisten = listen<DetectedDevice[]>("devices-changed", (event) => {
      setDevices(event.payload);
    });

    return () => {
      unlisten.then((fn) => fn());
    };
  }, []);

  return { devices, loading, error, refresh };
}
