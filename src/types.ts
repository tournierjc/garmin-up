export interface DetectedDevice {
  mount_path: string;
  unit_id: string;
  description: string;
  part_number: string;
  software_version: string;
  data_types: DataType[];
}

export interface DataType {
  name: string;
  files: DataFile[];
}

export interface DataFile {
  specification: { identifier: string };
  location: { path: string; base_name: string | null; file_extension: string };
  transfer_direction: "InputToUnit" | "OutputFromUnit" | "InputOutput";
}

export interface GarminSession {
  display_name: string;
}

export interface ActivityUploadResult {
  activity_id: number;
  detail_id: number;
}

export interface ConnectDeviceInfo {
  device_id: number | null;
  device_type: string | null;
  display_name: string | null;
  part_number: string | null;
  firmware_version: string | null;
}

export type Page = "devices" | "backup" | "sync" | "updates";
