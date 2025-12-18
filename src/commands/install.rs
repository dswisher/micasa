use crate::error::{MicasaError, Result};
use crate::manifest::{Manifest, ManifestEntry};
use crate::package_manager::PackageManager;
use crate::platform;
use crate::version;

/// Executes the install command for a single package
/// Tries each available package manager until one succeeds
pub fn execute_single(package_name: &str) -> Result<()> {
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
                println!("  Successfully installed {}\n", entry.name);
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
                            println!("  Successfully installed using {}", manager.name());
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
