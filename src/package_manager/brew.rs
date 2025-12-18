use std::process::Command;

use serde::Deserialize;

use crate::error::{MicasaError, Result};
use crate::package_manager::{PackageInfo, PackageManager};

/// Structs for deserializing Homebrew's JSON output
/// These correspond to the structure returned by `brew info --json=v2`
#[derive(Debug, Deserialize)]
struct BrewInfoResponse {
    formulae: Vec<BrewFormula>,
}

#[derive(Debug, Deserialize)]
struct BrewFormula {
    #[allow(dead_code)] // We use the canonical package_name instead for consistency
    name: String,
    #[serde(default)]
    desc: Option<String>,
    versions: BrewVersions,
    #[serde(default)]
    installed: Vec<BrewInstalled>,
}

#[derive(Debug, Deserialize)]
struct BrewVersions {
    stable: String,
}

#[derive(Debug, Deserialize)]
struct BrewInstalled {
    version: String,
}

/// Wrapper for Homebrew package manager (macOS)
pub struct BrewWrapper;

impl BrewWrapper {
    pub fn new() -> Self {
        Self
    }

    /// Resolve a package name to possible candidates on this platform
    /// Returns a vector with the canonical name first, then aliases
    /// This handles cases where package names differ across platforms
    fn resolve_package_candidates(&self, name: &str) -> Vec<String> {
        // Homebrew typically doesn't have package name aliases,
        // but we include this method for consistency
        // Add any brew-specific aliases here if needed
        vec![name.to_string()]
    }

    /// Helper to execute brew commands
    fn execute_brew(&self, args: &[&str]) -> Result<String> {
        let output = Command::new("brew")
            .args(args)
            .output()
            .map_err(|e| {
                MicasaError::CommandExecutionFailed(format!("Failed to execute brew: {}", e))
            })?;

        if !output.status.success() {
            let stderr = String::from_utf8_lossy(&output.stderr);
            return Err(MicasaError::CommandFailed {
                status: output.status,
                stderr: stderr.to_string(),
            });
        }

        Ok(String::from_utf8(output.stdout)?)
    }

    /// Parse JSON response from brew info command
    fn parse_info_response(&self, json_str: &str) -> Result<BrewFormula> {
        let response: BrewInfoResponse = serde_json::from_str(json_str)
            .map_err(|e| MicasaError::JsonParseError(format!("Failed to parse JSON: {}", e)))?;

        response
            .formulae
            .into_iter()
            .next()
            .ok_or_else(|| MicasaError::JsonParseError("No formulae found in response".to_string()))
    }
}

impl PackageManager for BrewWrapper {
    fn name(&self) -> &str {
        "brew"
    }

    fn is_available(&self) -> bool {
        Command::new("brew")
            .arg("--version")
            .output()
            .map(|output| output.status.success())
            .unwrap_or(false)
    }

    fn install(&self, package_name: &str) -> Result<()> {
        if !self.is_available() {
            return Err(MicasaError::PackageManagerNotAvailable(
                self.name().to_string(),
            ));
        }

        // Check if any candidate is already installed
        let candidates = self.resolve_package_candidates(package_name);
        for candidate in &candidates {
            if let Ok(json_output) = self.execute_brew(&["info", candidate, "--json=v2"]) {
                if let Ok(formula) = self.parse_info_response(&json_output) {
                    if !formula.installed.is_empty() {
                        let version = &formula.installed[0].version;
                        println!(
                            "{} is already installed (as {}, version {})",
                            package_name, candidate, version
                        );
                        return Ok(());
                    }
                }
            }
        }

        println!("Installing {} via brew...", package_name);

        // Install using the first candidate (canonical name)
        let install_target = &candidates[0];
        self.execute_brew(&["install", install_target])?;
        println!("Successfully installed {}", package_name);
        Ok(())
    }

    fn uninstall(&self, package_name: &str) -> Result<()> {
        if !self.is_available() {
            return Err(MicasaError::PackageManagerNotAvailable(
                self.name().to_string(),
            ));
        }

        // Find which candidate is actually installed
        let candidates = self.resolve_package_candidates(package_name);
        let mut installed_candidate = None;

        for candidate in &candidates {
            if let Ok(json_output) = self.execute_brew(&["info", candidate, "--json=v2"]) {
                if let Ok(formula) = self.parse_info_response(&json_output) {
                    if !formula.installed.is_empty() {
                        installed_candidate = Some(candidate.clone());
                        break;
                    }
                }
            }
        }

        let uninstall_target = match installed_candidate {
            Some(candidate) => {
                if candidate != package_name {
                    println!("Uninstalling {} (installed as {}) via brew...", package_name, candidate);
                } else {
                    println!("Uninstalling {} via brew...", package_name);
                }
                candidate
            }
            None => {
                return Err(MicasaError::CommandExecutionFailed(format!(
                    "Package {} is not installed",
                    package_name
                )));
            }
        };

        self.execute_brew(&["uninstall", &uninstall_target])?;
        println!("Successfully uninstalled {}", package_name);
        Ok(())
    }

    fn get_info(&self, package_name: &str) -> Result<PackageInfo> {
        if !self.is_available() {
            return Err(MicasaError::PackageManagerNotAvailable(
                self.name().to_string(),
            ));
        }

        // Try each candidate package name
        // First pass: look for installed packages (highest priority)
        // Second pass: look for available packages
        let candidates = self.resolve_package_candidates(package_name);
        let mut first_error = None;
        let mut available_info = None;

        for candidate in &candidates {
            match self.execute_brew(&["info", candidate, "--json=v2"]) {
                Ok(json_output) => {
                    if let Ok(formula) = self.parse_info_response(&json_output) {
                        let installed_version = formula.installed.first().map(|i| i.version.clone());
                        let available_version = Some(formula.versions.stable);

                        // If installed, return immediately (highest priority)
                        if installed_version.is_some() {
                            return Ok(PackageInfo {
                                name: package_name.to_string(), // Use canonical name
                                installed_version,
                                available_version,
                                description: formula.desc,
                                source: self.name().to_string(),
                            });
                        }

                        // Save available info as fallback
                        if available_info.is_none() {
                            available_info = Some(PackageInfo {
                                name: package_name.to_string(),
                                installed_version: None,
                                available_version,
                                description: formula.desc,
                                source: self.name().to_string(),
                            });
                        }
                    }
                }
                Err(e) => {
                    if first_error.is_none() {
                        first_error = Some(e);
                    }
                }
            }
        }

        // Return available info if we found any
        if let Some(info) = available_info {
            return Ok(info);
        }

        // If no candidate worked, return the first error
        Err(first_error.unwrap_or_else(|| {
            MicasaError::CommandExecutionFailed(format!(
                "No information found for package: {}",
                package_name
            ))
        }))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_name() {
        let brew = BrewWrapper::new();
        assert_eq!(brew.name(), "brew");
    }

    #[test]
    #[cfg(target_os = "macos")]
    fn test_is_available() {
        let brew = BrewWrapper::new();
        // This will only pass on macOS with brew installed
        // Comment out or skip if brew is not installed
        assert!(brew.is_available());
    }
}
