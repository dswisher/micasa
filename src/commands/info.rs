use crate::error::Result;
use crate::package_manager::PackageInfo;
use crate::platform;

/// Executes the info command, collecting information from all available managers
pub fn execute(package_name: &str) -> Result<()> {
    let managers = platform::get_available_managers();

    if managers.is_empty() {
        println!("No package managers available on this system");
        return Ok(());
    }

    let mut found_any = false;
    let mut infos: Vec<PackageInfo> = Vec::new();

    // Collect info from all managers
    for manager in &managers {
        match manager.get_info(package_name) {
            Ok(info) => {
                found_any = true;
                infos.push(info);
            }
            Err(e) => {
                eprintln!("Could not get info from {}: {}", manager.name(), e);
            }
        }
    }

    if !found_any {
        println!(
            "Package '{}' not found in any package manager",
            package_name
        );
        return Ok(());
    }

    // Display collected information
    println!();
    for info in infos {
        println!("Package: {} (from {})", info.name, info.source);
        println!("{}", "=".repeat(60));
        if let Some(version) = info.installed_version {
            println!("  Installed: {}", version);
        } else {
            println!("  Installed: Not installed");
        }
        if let Some(available) = info.available_version {
            println!("  Available: {}", available);
        }
        if let Some(desc) = info.description {
            println!("  Description: {}", desc);
        }
    }

    Ok(())
}
