# Garmin Connect Desktop

A cross-platform Garmin Express replacement built with Rust + Tauri + React.

## Features

- **Device detection** — Auto-detects USB-connected Garmin devices via mount point scanning
- **Backup & restore** — Full incremental backup of device data to the local `Data/` folder structure
- **Garmin Connect sync** — Sign in and upload activities to Garmin Connect
- **Firmware updates** — Check for and install firmware updates via `.gcd` files
- **Map management** — View, install, and remove `.img` map files on device
- **ConnectIQ apps** — List and manage installed watch apps and faces

## Prerequisites

| Tool | Version |
|------|---------|
| Rust | 1.70+ |
| Node.js | 18+ |
| npm | 9+ |
| Tauri CLI | 2.x |

Install Tauri CLI:

```sh
cargo install tauri-cli --version "^2"
```

Install system dependencies on Linux (Debian/Ubuntu):

```sh
sudo apt install libwebkit2gtk-4.1-dev libgtk-3-dev libayatana-appindicator3-dev librsvg2-dev
```

## Development

```sh
npm install
npm run tauri dev
```

This starts the Vite dev server and the Tauri window with hot-reload.

## Production Build

```sh
npm install
npm run tauri build
```

The installer/binary is output to `src-tauri/target/release/bundle/`.

## Frontend Only (no Tauri window)

```sh
npm run dev     # Vite dev server at http://localhost:1420
npm run build   # TypeScript check + Vite production build to dist/
```

## Project Structure

```
garmin-connect/
├── src/                        # React + TypeScript frontend
│   ├── pages/                  # DevicesPage, BackupPage, SyncPage, UpdatesPage
│   ├── components/             # Sidebar, DeviceList, DeviceCard
│   ├── hooks/                  # useDevices (Tauri event + invoke)
│   └── types.ts                # Shared TypeScript types
└── src-tauri/                  # Rust backend
    └── src/
        ├── backup/             # Backup & restore manager
        ├── commands/           # Tauri command handlers
        ├── connect/            # Garmin Connect API (auth, client)
        ├── connectiq/          # ConnectIQ app management
        ├── device/             # Device detection & monitoring
        ├── firmware/           # Firmware update checker & installer
        ├── fit/                # FIT file scanning
        ├── maps/               # Map file management
        └── xml/                # GarminDevice.xml, data_store.xml parsers
```

## Platform Notes

**Linux** — Device detection uses sysinfo disk scanning. Ensure the device is mounted (auto-mount via udev or manually).

**Windows** — Device detection uses the same sysinfo Disks API. Garmin devices appear as removable drives.
