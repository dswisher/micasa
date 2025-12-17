use crate::error::{MicasaError, Result};
use crate::platform;

/// Executes the uninstall command
/// Finds which manager has the package installed and uninstalls from that manager
pub fn execute(package_name: &str) -> Result<()> {
    let managers = platform::get_available_managers();

    if managers.is_empty() {
        return Err(MicasaError::NoPackageManagers);
    }

    // Try to find which manager has this package installed
    let mut uninstalled = false;

    for manager in &managers {
        // Check if package is available in this manager
        if let Ok(info) = manager.get_info(package_name) {
            if info.installed_version.is_some() {
                println!(
                    "Uninstalling '{}' using {}...",
                    package_name,
                    manager.name()
                );
                manager.uninstall(package_name)?;
                println!("Successfully uninstalled '{}'", package_name);
                uninstalled = true;
            }
        }
    }

    if !uninstalled {
        println!("Package '{}' is not installed", package_name);
    }

    Ok(())
}
