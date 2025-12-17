use crate::error::{MicasaError, Result};
use crate::platform;

/// Executes the install command
/// Tries each available package manager until one succeeds
pub fn execute(package_name: &str) -> Result<()> {
    let managers = platform::get_available_managers();

    if managers.is_empty() {
        return Err(MicasaError::NoPackageManagers);
    }

    // Try each manager until one succeeds
    let mut last_error = None;

    for manager in &managers {
        println!(
            "Trying to install '{}' using {}...",
            package_name,
            manager.name()
        );

        match manager.install(package_name) {
            Ok(()) => {
                println!(
                    "Successfully installed '{}' using {}",
                    package_name,
                    manager.name()
                );
                return Ok(());
            }
            Err(e) => {
                eprintln!("Failed with {}: {}", manager.name(), e);
                last_error = Some(e);
            }
        }
    }

    // If we get here, all managers failed
    Err(last_error.unwrap_or(MicasaError::NoPackageManagers))
}
