use crate::error::{MicasaError, Result};
use crate::manifest::{Manifest, ManifestEntry};
use crate::package_manager::PackageManager;
use crate::platform;
use crate::version;

/// Executes the install command for a single package
/// Tries each available package manager until one succeeds
/// If a manifest exists with version constraints for this package, they will be enforced
pub fn execute_single(package_name: &str) -> Result<()> {
    let managers = platform::get_available_managers();

    if managers.is_empty() {
        return Err(MicasaError::NoPackageManagers);
    }

    // Try to read manifest to get version constraints (ignore error if manifest doesn't exist)
    let version_spec = Manifest::read_default()
        .ok()
        .and_then(|manifest| {
            manifest
                .entries()
                .iter()
                .find(|entry| entry.name == package_name)
                .map(|entry| entry.version_spec.clone())
        })
        .flatten();

    if let Some(ref spec) = version_spec {
        println!(
            "Found version constraint in manifest for '{}': {}",
            package_name, spec
        );
    }

    // Create a manifest entry for version checking
    let entry = ManifestEntry {
        name: package_name.to_string(),
        version_spec,
    };

    // Use the same logic as manifest installation for consistency
    install_package_with_version(&managers, &entry)
}

/// Executes the install command for all packages in the manifest
pub fn execute_manifest() -> Result<()> {
    let manifest = Manifest::read_default()?;

    if manifest.is_empty() {
        println!("Manifest is empty. Nothing to install.");
        return Ok(());
    }

    let managers = platform::get_available_managers();

    if managers.is_empty() {
        return Err(MicasaError::NoPackageManagers);
    }

    println!(
        "Installing {} packages from manifest...\n",
        manifest.entries().len()
    );

    let mut results: Vec<(String, bool, Option<String>)> = Vec::new();

    for entry in manifest.entries() {
        println!("Processing {}...", entry.name);

        match install_package_with_version(&managers, entry) {
            Ok(()) => {
                results.push((entry.name.clone(), true, None));
            }
            Err(e) => {
                results.push((entry.name.clone(), false, Some(e.to_string())));
                eprintln!("  Failed to install {}: {}\n", entry.name, e);
            }
        }
    }

    // Print summary
    print_install_summary(&results);

    // Return error if any failed
    let failed_count = results.iter().filter(|(_, success, _)| !success).count();
    if failed_count > 0 {
        Err(MicasaError::CommandExecutionFailed(format!(
            "{} package(s) failed to install",
            failed_count
        )))
    } else {
        Ok(())
    }
}

/// Install a package with version constraint checking
fn install_package_with_version(
    managers: &[Box<dyn PackageManager>],
    entry: &ManifestEntry,
) -> Result<()> {
    let mut last_error = None;
    let mut checked_managers = Vec::new();
    let mut found_in_any_manager = false;

    for manager in managers {
        // Skip managers that aren't available on this system
        if !manager.is_available() {
            continue;
        }

        println!("  Checking {}...", manager.name());

        // First, check if already installed with satisfactory version
        if let Ok(info) = manager.get_info(&entry.name) {
            found_in_any_manager = true;

            if let Some(installed_version) = &info.installed_version {
                if version::satisfies_requirement(installed_version, entry.version_spec.as_deref())?
                {
                    println!(
                        "  {} is already installed (version {})",
                        entry.name, installed_version
                    );
                    return Ok(());
                }
            }

            // Check if available version satisfies requirement
            if let Some(available_version) = &info.available_version {
                if version::satisfies_requirement(available_version, entry.version_spec.as_deref())?
                {
                    println!(
                        "  Available version {} satisfies requirement",
                        available_version
                    );

                    match manager.install(&entry.name) {
                        Ok(()) => {
                            // Get the installed version to report it
                            let installed_version = manager
                                .get_info(&entry.name)
                                .ok()
                                .and_then(|info| info.installed_version)
                                .unwrap_or_else(|| "unknown".to_string());

                            println!(
                                "  Successfully installed '{}' version '{}' using {}",
                                entry.name,
                                installed_version,
                                manager.name()
                            );
                            return Ok(());
                        }
                        Err(e) => {
                            eprintln!("  Installation failed with {}: {}", manager.name(), e);
                            last_error = Some(e);
                            checked_managers
                                .push((manager.name().to_string(), Some(available_version.clone())));
                        }
                    }
                } else {
                    println!(
                        "  Available version {} does not satisfy requirement {:?}",
                        available_version, entry.version_spec
                    );
                    checked_managers
                        .push((manager.name().to_string(), Some(available_version.clone())));
                }
            } else {
                println!("  Package found but no available version information");
                checked_managers.push((manager.name().to_string(), None));
            }
        } else {
            println!("  Package not found in {}", manager.name());
        }
    }

    // If we get here, no manager could satisfy the requirement
    if !found_in_any_manager {
        // Package doesn't exist in any package manager
        Err(MicasaError::PackageNotFound(entry.name.clone()))
    } else if let Some(version_spec) = &entry.version_spec {
        // Package exists but no version satisfies the constraint
        let _available_str = checked_managers
            .iter()
            .map(|(mgr, ver)| format!("{}: {}", mgr, ver.as_deref().unwrap_or("not found")))
            .collect::<Vec<_>>()
            .join(", ");

        Err(MicasaError::NoSatisfactoryVersion(
            entry.name.clone(),
            version_spec.clone(),
        ))
    } else {
        // Package exists but installation failed for other reasons
        Err(last_error.unwrap_or(MicasaError::PackageNotFound(entry.name.clone())))
    }
}

/// Print installation summary
fn print_install_summary(results: &[(String, bool, Option<String>)]) {
    println!("\n{}", "=".repeat(60));
    println!("Installation Summary");
    println!("{}", "=".repeat(60));

    let success_count = results.iter().filter(|(_, success, _)| *success).count();
    let total = results.len();

    println!("Successful: {}/{}", success_count, total);

    let failed: Vec<_> = results
        .iter()
        .filter(|(_, success, _)| !success)
        .collect();

    if !failed.is_empty() {
        println!("\nFailed packages:");
        for (name, _, error) in failed {
            println!(
                "  - {}: {}",
                name,
                error.as_deref().unwrap_or("Unknown error")
            );
        }
    }
}
