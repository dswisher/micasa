use crate::error::Result;
use crate::manifest::Manifest;
use crate::package_manager::PackageInfo;
use crate::platform;
use crate::version;

/// Executes the info command for a single package, collecting information from all available managers
pub fn execute_single(package_name: &str) -> Result<()> {
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

/// Executes the info command for all packages in the manifest
pub fn execute_manifest() -> Result<()> {
    let manifest = Manifest::read_default()?;

    if manifest.is_empty() {
        println!("Manifest is empty.");
        return Ok(());
    }

    let managers = platform::get_available_managers();

    if managers.is_empty() {
        println!("No package managers available on this system");
        return Ok(());
    }

    println!("Package information from manifest:\n");

    for entry in manifest.entries() {
        println!("Package: {}", entry.name);
        if let Some(version_spec) = &entry.version_spec {
            println!("  Required version: {}", version_spec);
        }

        let mut found_any = false;

        for manager in &managers {
            if let Ok(info) = manager.get_info(&entry.name) {
                found_any = true;
                print_package_info(&info, entry.version_spec.as_deref());
            }
        }

        if !found_any {
            println!("  Not found in any package manager");
        }

        println!();
    }

    Ok(())
}

/// Print package info with version requirement checking
fn print_package_info(info: &PackageInfo, version_spec: Option<&str>) {
    println!("  From {}:", info.source);

    if let Some(version) = &info.installed_version {
        let satisfies = version_spec
            .and_then(|spec| version::satisfies_requirement(version, Some(spec)).ok())
            .unwrap_or(true);

        let status = if satisfies { "✓" } else { "✗" };
        println!("    Installed: {} {}", version, status);
    } else {
        println!("    Installed: Not installed");
    }

    if let Some(available) = &info.available_version {
        let satisfies = version_spec
            .and_then(|spec| version::satisfies_requirement(available, Some(spec)).ok())
            .unwrap_or(true);

        let status = if satisfies { "✓" } else { "✗" };
        println!("    Available: {} {}", available, status);
    }

    if let Some(desc) = &info.description {
        // Truncate long descriptions
        let desc_short = if desc.len() > 100 {
            format!("{}...", &desc[..97])
        } else {
            desc.clone()
        };
        println!("    Description: {}", desc_short);
    }
}
