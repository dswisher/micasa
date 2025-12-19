use std::process::Command;

use serde::Deserialize;

use crate::error::{MicasaError, Result};
use crate::package_manager::{PackageInfo, PackageManager};

/// Structure for npm list output
#[derive(Debug, Deserialize)]
struct NpmListOutput {
    #[serde(default)]
    dependencies: Option<std::collections::HashMap<String, NpmPackageInfo>>,
}

#[derive(Debug, Deserialize)]
struct NpmPackageInfo {
    version: String,
}

/// Structure for npm view output
#[derive(Debug, Deserialize)]
struct NpmViewOutput {
    version: String,
    #[serde(default)]
    description: Option<String>,
}

/// Wrapper for npm package manager
pub struct NpmWrapper;

impl NpmWrapper {
    pub fn new() -> Self {
        Self
    }

    /// Helper to execute npm commands
    fn execute_npm(&self, args: &[&str]) -> Result<String> {
        let output = Command::new("npm")
            .args(args)
            .output()
            .map_err(|e| {
                MicasaError::CommandExecutionFailed(format!("Failed to execute npm: {}", e))
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

    /// Helper to execute npm commands with sudo (for global install/uninstall)
    fn execute_npm_sudo(&self, args: &[&str]) -> Result<String> {
        let mut npm_args = vec!["npm"];
        npm_args.extend_from_slice(args);

        let output = Command::new("sudo")
            .args(&npm_args)
            .output()
            .map_err(|e| {
                MicasaError::CommandExecutionFailed(format!("Failed to execute npm with sudo: {}", e))
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

    /// Ensures npm is installed, installing Node.js if necessary
    fn ensure_npm_installed(&self) -> Result<()> {
        // Check if npm is already available
        if Command::new("npm").arg("--version").output().is_ok() {
            return Ok(());
        }

        println!("  npm is required, attempting to install Node.js...");

        // Try to install nodejs using the system package manager
        // On most systems, this will also install npm
        let install_commands = vec![
            vec!["apt-get", "install", "-y", "nodejs", "npm"],
            vec!["dnf", "install", "-y", "nodejs"],
            vec!["yum", "install", "-y", "nodejs"],
            vec!["brew", "install", "node"],
        ];

        for cmd in install_commands {
            let result = Command::new("sudo")
                .args(&cmd)
                .output();

            if let Ok(output) = result {
                if output.status.success() {
                    // Verify npm is now available
                    if Command::new("npm").arg("--version").output().is_ok() {
                        println!("  Successfully installed Node.js and npm");
                        return Ok(());
                    }
                }
            }
        }

        Err(MicasaError::CommandExecutionFailed(
            "Failed to install Node.js/npm. Please install Node.js manually to use npm package manager.".to_string()
        ))
    }

    /// Check if a package is installed globally
    fn is_installed(&self, package_name: &str) -> Result<Option<String>> {
        let output = self.execute_npm(&["list", "-g", "--json", "--depth=0"])?;

        let list: NpmListOutput = serde_json::from_str(&output)
            .map_err(|e| MicasaError::JsonParseError(format!("Failed to parse npm list output: {}", e)))?;

        Ok(list.dependencies
            .and_then(|deps| deps.get(package_name).map(|info| info.version.clone())))
    }

    /// Get package information from npm registry
    fn get_package_view(&self, package_name: &str) -> Result<NpmViewOutput> {
        let output = self.execute_npm(&["view", package_name, "--json"])?;

        serde_json::from_str(&output)
            .map_err(|e| MicasaError::JsonParseError(format!("Failed to parse npm view output: {}", e)))
    }
}

impl PackageManager for NpmWrapper {
    fn name(&self) -> &str {
        "npm"
    }

    fn is_available(&self) -> bool {
        // npm is always "available" - we'll try to install it when needed
        true
    }

    fn install(&self, package_name: &str) -> Result<()> {
        // Ensure npm is installed
        self.ensure_npm_installed()?;

        // Check if already installed
        if let Ok(Some(version)) = self.is_installed(package_name) {
            println!("{} is already installed (version {})", package_name, version);
            return Ok(());
        }

        println!("Installing {} via npm...", package_name);

        // Install globally with sudo
        self.execute_npm_sudo(&["install", "-g", package_name])?;
        println!("Successfully installed {}", package_name);
        Ok(())
    }

    fn uninstall(&self, package_name: &str) -> Result<()> {
        // Ensure npm is installed
        self.ensure_npm_installed()?;

        // Check if installed
        if self.is_installed(package_name)?.is_none() {
            return Err(MicasaError::CommandExecutionFailed(format!(
                "Package {} is not installed",
                package_name
            )));
        }

        println!("Uninstalling {} via npm...", package_name);
        self.execute_npm_sudo(&["uninstall", "-g", package_name])?;
        println!("Successfully uninstalled {}", package_name);
        Ok(())
    }

    fn get_info(&self, package_name: &str) -> Result<PackageInfo> {
        // Ensure npm is installed
        self.ensure_npm_installed()?;

        // Check if installed
        let installed_version = self.is_installed(package_name)?;

        // Get latest available version
        let view = self.get_package_view(package_name)?;

        Ok(PackageInfo {
            name: package_name.to_string(),
            installed_version,
            available_version: Some(view.version),
            description: view.description,
            source: self.name().to_string(),
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_name() {
        let npm = NpmWrapper::new();
        assert_eq!(npm.name(), "npm");
    }
}
