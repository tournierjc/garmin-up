use std::path::PathBuf;

use sysinfo::Disks;
use tracing::{debug, info};

use super::DetectedDevice;
use crate::xml::garmin_device;

pub fn scan_for_devices() -> Vec<DetectedDevice> {
    let mut devices = Vec::new();
    let disks = Disks::new_with_refreshed_list();

    for disk in disks.list() {
        let mount = disk.mount_point();
        let garmin_dir = mount.join("GARMIN");

        if !garmin_dir.is_dir() {
            continue;
        }

        debug!("Found GARMIN directory at {}", mount.display());

        let xml_path = garmin_dir.join("GarminDevice.xml");
        if !xml_path.is_file() {
            debug!("No GarminDevice.xml at {}", xml_path.display());
            continue;
        }

        match garmin_device::parse_file(&xml_path) {
            Ok(device) => {
                info!(
                    "Detected device: {} (ID: {}) at {}",
                    device.model.description, device.id, mount.display()
                );
                devices.push(DetectedDevice::from_mount(mount.to_path_buf(), device));
            }
            Err(e) => {
                tracing::warn!(
                    "Failed to parse GarminDevice.xml at {}: {}",
                    xml_path.display(),
                    e
                );
            }
        }
    }

    #[cfg(target_os = "linux")]
    scan_kio_mtp_devices(&mut devices);

    scan_garmin_data_dir(&mut devices);

    devices
}

#[cfg(target_os = "linux")]
fn scan_kio_mtp_devices(devices: &mut Vec<DetectedDevice>) {
    let device_names = match kio_ls("mtp:/") {
        Ok(names) => names,
        Err(err) => {
            debug!("No KIO MTP devices available: {err}");
            return;
        }
    };

    for device_name in device_names {
        let device_root = format!("mtp:/{device_name}/");
        let mut candidate_roots = vec![device_root.clone()];

        if let Ok(storage_roots) = kio_ls(&device_root) {
            candidate_roots.extend(
                storage_roots
                    .into_iter()
                    .map(|storage_root| format!("{device_root}{storage_root}/")),
            );
        }

        for candidate_root in candidate_roots {
            if try_add_kio_device(&candidate_root, devices) {
                break;
            }
        }
    }
}

#[cfg(target_os = "linux")]
fn try_add_kio_device(root_uri: &str, devices: &mut Vec<DetectedDevice>) -> bool {
    for garmin_dir_name in ["GARMIN", "Garmin"] {
        let xml_uri = format!("{root_uri}{garmin_dir_name}/GarminDevice.xml");

        let xml_content = match kio_cat(&xml_uri) {
            Ok(content) => content,
            Err(_) => continue,
        };

        match garmin_device::parse_str(&xml_content) {
            Ok(device) => {
                if devices.iter().any(|existing| existing.unit_id == device.id) {
                    debug!("Skipping duplicate KIO MTP device {}", device.id);
                    return true;
                }

                let mount_path = PathBuf::from(root_uri.trim_end_matches('/'));
                info!(
                    "Detected KIO MTP device: {} (ID: {}) at {}",
                    device.model.description,
                    device.id,
                    mount_path.display()
                );
                devices.push(DetectedDevice::from_mount(mount_path, device));
                return true;
            }
            Err(err) => {
                tracing::warn!("Failed to parse GarminDevice.xml via {xml_uri}: {err}");
            }
        }
    }

    false
}

#[cfg(target_os = "linux")]
fn kio_ls(uri: &str) -> Result<Vec<String>, String> {
    let output = run_kioclient(["ls", uri])?;
    Ok(parse_kioclient_ls_entries(&output))
}

#[cfg(target_os = "linux")]
fn kio_cat(uri: &str) -> Result<String, String> {
    run_kioclient(["cat", uri])
}

#[cfg(target_os = "linux")]
const KIOCLIENT_TIMEOUT: std::time::Duration = std::time::Duration::from_secs(5);

#[cfg(target_os = "linux")]
fn spawn_and_wait_with_timeout(
    program: &str,
    args: &[&str],
    timeout: std::time::Duration,
) -> std::io::Result<std::process::Output> {
    use std::io::Read;
    use std::process::Stdio;
    use std::sync::mpsc;
    use std::thread;

    let mut child = std::process::Command::new(program)
        .args(args)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()?;

    let mut stdout_pipe = child.stdout.take().expect("stdout was piped");
    let mut stderr_pipe = child.stderr.take().expect("stderr was piped");

    let (tx, rx) = mpsc::channel::<(Vec<u8>, Vec<u8>)>();
    thread::spawn(move || {
        let mut out = Vec::new();
        let mut err_buf = Vec::new();
        let _ = stdout_pipe.read_to_end(&mut out);
        let _ = stderr_pipe.read_to_end(&mut err_buf);
        let _ = tx.send((out, err_buf));
    });

    match rx.recv_timeout(timeout) {
        Ok((stdout, stderr)) => {
            let status = child.wait()?;
            Ok(std::process::Output { status, stdout, stderr })
        }
        Err(mpsc::RecvTimeoutError::Timeout) => {
            let _ = child.kill();
            let _ = child.wait();
            Err(std::io::Error::new(
                std::io::ErrorKind::TimedOut,
                format!("process timed out after {}s", timeout.as_secs()),
            ))
        }
        Err(mpsc::RecvTimeoutError::Disconnected) => {
            let _ = child.kill();
            let _ = child.wait();
            Err(std::io::Error::new(
                std::io::ErrorKind::Other,
                "output thread disconnected unexpectedly",
            ))
        }
    }
}

#[cfg(target_os = "linux")]
fn run_kioclient<const N: usize>(args: [&str; N]) -> Result<String, String> {
    let mut last_error = None;

    for program in ["kioclient5", "kioclient"] {
        let output = match spawn_and_wait_with_timeout(program, &args, KIOCLIENT_TIMEOUT) {
            Ok(output) => output,
            Err(err) if err.kind() == std::io::ErrorKind::NotFound => continue,
            Err(err) => {
                last_error = Some(format!("{program} failed: {err}"));
                continue;
            }
        };

        if output.status.success() {
            return String::from_utf8(output.stdout)
                .map_err(|err| format!("{program} produced non-UTF8 output: {err}"));
        }

        let stderr = String::from_utf8_lossy(&output.stderr);
        last_error = Some(if stderr.trim().is_empty() {
            format!("{program} {:?} failed with status {}", args, output.status)
        } else {
            format!("{program} {:?} failed: {}", args, stderr.trim())
        });
    }

    Err(last_error.unwrap_or_else(|| "kioclient is unavailable".to_string()))
}

#[cfg(target_os = "linux")]
fn parse_kioclient_ls_entries(output: &str) -> Vec<String> {
    output
        .lines()
        .map(str::trim)
        .filter(|line| !line.is_empty() && *line != ".")
        .map(ToOwned::to_owned)
        .collect()
}

fn scan_garmin_data_dir(_devices: &mut Vec<DetectedDevice>) {
    let data_dir = dirs_garmin_data();
    if !data_dir.is_dir() {
        return;
    }

    let devices_list_path = data_dir.join("CoreService").join("devices_list.xml");
    if devices_list_path.is_file() {
        debug!("Found Garmin Express data at {}", data_dir.display());
    }
}

fn dirs_garmin_data() -> PathBuf {
    if cfg!(target_os = "windows") {
        let appdata = std::env::var("PROGRAMDATA").unwrap_or_else(|_| "C:\\ProgramData".into());
        PathBuf::from(appdata).join("Garmin")
    } else {
        dirs_home().join("Documents").join("Garmin")
    }
}

fn dirs_home() -> PathBuf {
    std::env::var("HOME")
        .map(PathBuf::from)
        .unwrap_or_else(|_| PathBuf::from("/tmp"))
}

#[cfg(all(test, target_os = "linux"))]
mod tests {
    use super::{parse_kioclient_ls_entries, spawn_and_wait_with_timeout};
    use std::time::Duration;

    #[test]
    fn parses_kioclient_listing_output() {
        let output = ".\nfenix 6 Pro\n\nEdge 1040\n";
        let entries = parse_kioclient_ls_entries(output);
        assert_eq!(entries, vec!["fenix 6 Pro", "Edge 1040"]);
    }

    #[test]
    fn timeout_helper_succeeds_for_fast_command() {
        let output = spawn_and_wait_with_timeout("echo", &["hello"], Duration::from_secs(5))
            .expect("echo should succeed within timeout");
        assert!(output.status.success());
        let stdout = String::from_utf8(output.stdout).unwrap();
        assert_eq!(stdout.trim(), "hello");
    }

    #[test]
    fn timeout_helper_times_out_for_slow_command() {
        let err = spawn_and_wait_with_timeout("sleep", &["10"], Duration::from_millis(100))
            .expect_err("sleep 10 should be killed by the 100 ms timeout");
        assert_eq!(err.kind(), std::io::ErrorKind::TimedOut);
    }
}
