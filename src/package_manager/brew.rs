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

        println!("Installing {} via brew...", package_name);
        self.execute_brew(&["install", package_name])?;
        println!("Successfully installed {}", package_name);
        Ok(())
    }

    fn uninstall(&self, package_name: &str) -> Result<()> {
        if !self.is_available() {
            return Err(MicasaError::PackageManagerNotAvailable(
                self.name().to_string(),
            ));
        }

        println!("Uninstalling {} via brew...", package_name);
        self.execute_brew(&["uninstall", package_name])?;
        println!("Successfully uninstalled {}", package_name);
        Ok(())
    }

    fn get_info(&self, package_name: &str) -> Result<PackageInfo> {
        if !self.is_available() {
            return Err(MicasaError::PackageManagerNotAvailable(
                self.name().to_string(),
            ));
        }

        // Get info as JSON
        let json_output = self.execute_brew(&["info", package_name, "--json=v2"])?;

        // Parse the JSON response
        let formula = self.parse_info_response(&json_output)?;

        // Extract version information
        let installed_version = formula.installed.first().map(|i| i.version.clone());
        let available_version = Some(formula.versions.stable);

        Ok(PackageInfo {
            name: formula.name,
            installed_version,
            available_version,
            description: formula.desc,
            source: self.name().to_string(),
        })
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
