use crate::package_manager::{apt::AptWrapper, brew::BrewWrapper, PackageManager};

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
            vec![Box::new(AptWrapper::new())]
        }
        OsType::Windows => {
            // Future: will return winget, scoop, chocolatey
            vec![]
        }
    }
}
