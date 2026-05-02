//! KDE `kioclient` bridge for MTP (`mtp:/…`) URIs — not visible to POSIX `fs`.
#![cfg(target_os = "linux")]

use std::io::Read;
use std::path::Path;
use std::process::Stdio;
use std::sync::mpsc;
use std::thread;
use std::time::Duration;

use url::Url;

/// Short timeouts for `ls` / `cat` metadata.
pub const KIO_FAST_TIMEOUT: Duration = Duration::from_secs(15);
/// Large map / firmware payloads over MTP can be slow.
pub const KIO_COPY_TIMEOUT: Duration = Duration::from_secs(900);

pub fn parse_ls_entries(output: &str) -> Vec<String> {
    output
        .lines()
        .map(str::trim)
        .filter(|line| !line.is_empty() && *line != ".")
        .map(ToOwned::to_owned)
        .collect()
}

fn spawn_and_wait_with_timeout(
    program: &str,
    args: &[&str],
    timeout: Duration,
) -> std::io::Result<std::process::Output> {
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

fn run_program(
    program: &str,
    args: &[&str],
    timeout: Duration,
) -> Result<std::process::Output, std::io::Error> {
    spawn_and_wait_with_timeout(program, args, timeout)
}

fn run_kioclient_first_success(args: &[&str], timeout: Duration) -> Result<Vec<u8>, String> {
    let mut last_error = None;
    for program in ["kioclient5", "kioclient"] {
        let output = match run_program(program, args, timeout) {
            Ok(output) => output,
            Err(err) if err.kind() == std::io::ErrorKind::NotFound => continue,
            Err(err) => {
                last_error = Some(format!("{program}: {err}"));
                continue;
            }
        };

        if output.status.success() {
            return Ok(output.stdout);
        }

        let stderr = String::from_utf8_lossy(&output.stderr);
        last_error = Some(if stderr.trim().is_empty() {
            format!(
                "{program} {:?} failed with status {}",
                args, output.status
            )
        } else {
            format!("{program} {:?} failed: {}", args, stderr.trim())
        });
    }

    Err(last_error.unwrap_or_else(|| "kioclient is unavailable".to_string()))
}

/// For detector unit tests (echo / sleep diagnostics).
#[cfg(test)]
pub fn run_program_for_tests(
    program: &str,
    args: &[&str],
    timeout: Duration,
) -> std::io::Result<std::process::Output> {
    spawn_and_wait_with_timeout(program, args, timeout)
}

/// List directory contents (file names).
pub fn ls(uri: &str, timeout: Duration) -> Result<Vec<String>, String> {
    let out = run_kioclient_first_success(&["ls", uri], timeout)?;
    Ok(parse_ls_entries(
        std::str::from_utf8(&out).map_err(|e| format!("non-UTF8 ls output: {e}"))?,
    ))
}

/// Read whole file as UTF-8 (`GarminDevice.xml`, etc.).
pub fn cat_utf8(uri: &str, timeout: Duration) -> Result<String, String> {
    let stdout = cat_bytes(uri, timeout)?;
    String::from_utf8(stdout).map_err(|e| format!("non-UTF8 file at {uri}: {e}"))
}

/// Raw bytes (updates, binaries).
pub fn cat_bytes(uri: &str, timeout: Duration) -> Result<Vec<u8>, String> {
    run_kioclient_first_success(&["cat", uri], timeout)
}

pub fn mkdir(uri: &str, timeout: Duration) -> Result<(), String> {
    run_kioclient_first_success(&["mkdir", uri], timeout)?;
    Ok(())
}

pub fn remove(uri: &str, timeout: Duration) -> Result<(), String> {
    run_kioclient_first_success(&["remove", uri], timeout)?;
    Ok(())
}

pub fn move_url(from: &str, to: &str, timeout: Duration) -> Result<(), String> {
    run_kioclient_first_success(&["move", "--overwrite", from, to], timeout)?;
    Ok(())
}

/// `file:///…` URL for a readable local absolute path (`kioclient copy` expects this).
pub fn file_url(abs: &Path) -> Result<String, String> {
    let canon = abs.canonicalize().map_err(|e| format!("canonicalize {:?}: {e}", abs))?;
    Url::from_file_path(&canon)
        .map(|u| u.to_string())
        .map_err(|()| format!("invalid file URL for {:?}", canon))
}

/// Copy **from local filesystem** onto an `mtp:/…` (or other) destination URL.
pub fn copy_overwrite(local_file: &Path, dest_remote_uri: &str, timeout: Duration) -> Result<(), String> {
    let fu = file_url(local_file)?;
    run_kioclient_first_success(
        &[
            "--overwrite",
            "--noninteractive",
            "copy",
            fu.as_str(),
            dest_remote_uri,
        ],
        timeout,
    )?;
    Ok(())
}

#[cfg(all(test, target_os = "linux"))]
mod tests_spawn {
    use super::{parse_ls_entries, run_program_for_tests};
    use std::time::Duration;

    #[test]
    fn parses_ls_output() {
        let output = ".\nfenix 6 Pro\n\nEdge 1040\n";
        let entries = parse_ls_entries(output);
        assert_eq!(entries, vec!["fenix 6 Pro", "Edge 1040"]);
    }

    #[test]
    fn timeout_fast_command() {
        let output = run_program_for_tests("echo", &["hello"], Duration::from_secs(5))
            .expect("echo should succeed");
        assert!(output.status.success());
        assert_eq!(
            String::from_utf8(output.stdout).unwrap().trim(),
            "hello"
        );
    }

    #[test]
    fn timeout_slow_command() {
        let err =
            run_program_for_tests("sleep", &["10"], Duration::from_millis(100)).expect_err("timeout");
        assert_eq!(err.kind(), std::io::ErrorKind::TimedOut);
    }
}
