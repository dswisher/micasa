use crate::package_manager::{apt::AptWrapper, brew::BrewWrapper, dnf::DnfWrapper, PackageManager};
use std::fs;

/// Represents the operating system type
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum OsType {
    MacOS,
    Linux,
    Windows,
}

/// Detects the current platform
pub fn detect_os() -> OsType {
    if cfg!(target_os = "macos") {
        OsType::MacOS
    } else if cfg!(target_os = "linux") {
        OsType::Linux
    } else if cfg!(target_os = "windows") {
        OsType::Windows
    } else {
        panic!("Unsupported operating system")
    }
}

/// Detects the Linux distribution by reading /etc/os-release
/// Returns the ID field value (e.g., "ubuntu", "fedora", "amzn")
fn detect_linux_distro() -> Option<String> {
    let os_release = fs::read_to_string("/etc/os-release").ok()?;

    for line in os_release.lines() {
        if line.starts_with("ID=") {
            let id = line
                .strip_prefix("ID=")?
                .trim()
                .trim_matches('"')
                .trim_matches('\'');
            return Some(id.to_string());
        }
    }

    None
}

/// Returns available package managers for the current platform
/// Returns them in preferred order (first = most preferred)
///
/// In Rust, we return Box<dyn Trait> for trait objects (similar to C# interface references).
/// The 'static lifetime means the trait object doesn't borrow any data.
pub fn get_available_managers() -> Vec<Box<dyn PackageManager>> {
    let os = detect_os();

    match os {
        OsType::MacOS => {
            // For now, only brew is implemented
            vec![Box::new(BrewWrapper::new())]
        }
        OsType::Linux => {
            // Detect specific Linux distribution
            match detect_linux_distro().as_deref() {
                // DNF-based distributions
                Some("amzn") | Some("fedora") | Some("rhel") | Some("centos") | Some("rocky")
                | Some("almalinux") => {
                    vec![Box::new(DnfWrapper::new())]
                }
                // APT-based distributions
                Some("debian") | Some("ubuntu") => {
                    vec![Box::new(AptWrapper::new())]
                }
                // Unknown distro - return both and let availability check decide
                _ => {
                    vec![Box::new(AptWrapper::new()), Box::new(DnfWrapper::new())]
                }
            }
        }
        OsType::Windows => {
            // Future: will return winget, scoop, chocolatey
            vec![]
        }
    }
}
