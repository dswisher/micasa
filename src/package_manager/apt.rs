use std::process::Command;

use crate::error::{MicasaError, Result};
use crate::package_manager::{PackageInfo, PackageManager};

/// Wrapper for apt package manager (Debian/Ubuntu)
pub struct AptWrapper;

impl AptWrapper {
    pub fn new() -> Self {
        Self
    }

    /// Helper to detect if sudo is available
    fn needs_sudo(&self) -> bool {
        // Check if sudo command is available
        Command::new("which")
            .arg("sudo")
            .output()
            .map(|output| output.status.success())
            .unwrap_or(false)
    }

    /// Helper to execute apt-get commands
    fn execute_apt_get(&self, args: &[&str], use_sudo: bool) -> Result<String> {
        let mut cmd = if use_sudo {
            let mut c = Command::new("sudo");
            c.arg("apt-get");
            c
        } else {
            Command::new("apt-get")
        };

        let output = cmd
            .args(args)
            .env("DEBIAN_FRONTEND", "noninteractive")
            .output()
            .map_err(|e| {
                MicasaError::CommandExecutionFailed(format!("Failed to execute apt-get: {}", e))
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

    /// Helper to execute apt-cache commands
    fn execute_apt_cache(&self, args: &[&str]) -> Result<String> {
        let output = Command::new("apt-cache")
            .args(args)
            .output()
            .map_err(|e| {
                MicasaError::CommandExecutionFailed(format!("Failed to execute apt-cache: {}", e))
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

    /// Parse apt-cache policy output to extract version information
    /// Returns (installed_version, available_version)
    fn parse_policy_output(&self, output: &str) -> (Option<String>, Option<String>) {
        let mut installed = None;
        let mut available = None;

        for line in output.lines() {
            let trimmed = line.trim();

            if trimmed.starts_with("Installed:") {
                if let Some(version) = trimmed.strip_prefix("Installed:").map(|s| s.trim()) {
                    if version != "(none)" {
                        installed = Some(version.to_string());
                    }
                }
            } else if trimmed.starts_with("Candidate:") {
                if let Some(version) = trimmed.strip_prefix("Candidate:").map(|s| s.trim()) {
                    if version != "(none)" {
                        available = Some(version.to_string());
                    }
                }
            }
        }

        (installed, available)
    }

    /// Parse apt-cache show output to extract description
    fn parse_show_output(&self, output: &str) -> Option<String> {
        let mut in_description = false;
        let mut description_lines = Vec::new();

        for line in output.lines() {
            if line.starts_with("Description:") || line.starts_with("Description-en:") {
                // Extract the first line of description
                if let Some(desc) = line.split(':').nth(1) {
                    let desc = desc.trim();
                    if !desc.is_empty() {
                        description_lines.push(desc.to_string());
                    }
                }
                in_description = true;
            } else if in_description {
                // Continuation lines start with space
                if line.starts_with(' ') || line.starts_with('\t') {
                    description_lines.push(line.trim().to_string());
                } else {
                    // End of description section
                    break;
                }
            }
        }

        if description_lines.is_empty() {
            None
        } else {
            // Return the first line or paragraph
            Some(description_lines.join(" "))
        }
    }
}

impl PackageManager for AptWrapper {
    fn name(&self) -> &str {
        "apt"
    }

    fn is_available(&self) -> bool {
        Command::new("apt-get")
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

        println!("Installing {} via apt...", package_name);

        // Try without sudo first
        let result = self.execute_apt_get(&["install", "-y", package_name], false);

        // If permission error and sudo is available, retry with sudo
        if let Err(MicasaError::CommandFailed { ref stderr, .. }) = result {
            if stderr.contains("permission") || stderr.contains("not permitted") || stderr.contains("lock file") {
                if self.needs_sudo() {
                    println!("Retrying with sudo...");
                    self.execute_apt_get(&["install", "-y", package_name], true)?;
                    println!("Successfully installed {}", package_name);
                    return Ok(());
                }
            }
        }

        result?;
        println!("Successfully installed {}", package_name);
        Ok(())
    }

    fn uninstall(&self, package_name: &str) -> Result<()> {
        if !self.is_available() {
            return Err(MicasaError::PackageManagerNotAvailable(
                self.name().to_string(),
            ));
        }

        println!("Uninstalling {} via apt...", package_name);

        // Try without sudo first
        let result = self.execute_apt_get(&["remove", "-y", package_name], false);

        // If permission error and sudo is available, retry with sudo
        if let Err(MicasaError::CommandFailed { ref stderr, .. }) = result {
            if stderr.contains("permission") || stderr.contains("not permitted") || stderr.contains("lock file") {
                if self.needs_sudo() {
                    println!("Retrying with sudo...");
                    self.execute_apt_get(&["remove", "-y", package_name], true)?;
                    println!("Successfully uninstalled {}", package_name);
                    return Ok(());
                }
            }
        }

        result?;
        println!("Successfully uninstalled {}", package_name);
        Ok(())
    }

    fn get_info(&self, package_name: &str) -> Result<PackageInfo> {
        if !self.is_available() {
            return Err(MicasaError::PackageManagerNotAvailable(
                self.name().to_string(),
            ));
        }

        // Get version information
        let policy_output = self.execute_apt_cache(&["policy", package_name])?;
        let (installed_version, available_version) = self.parse_policy_output(&policy_output);

        // Get description
        let show_output = self.execute_apt_cache(&["show", package_name])?;
        let description = self.parse_show_output(&show_output);

        Ok(PackageInfo {
            name: package_name.to_string(),
            installed_version,
            available_version,
            description,
            source: self.name().to_string(),
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_name() {
        let apt = AptWrapper::new();
        assert_eq!(apt.name(), "apt");
    }

    #[test]
    #[cfg(target_os = "linux")]
    fn test_is_available() {
        let apt = AptWrapper::new();
        // This will only pass on Linux with apt installed
        assert!(apt.is_available());
    }

    #[test]
    fn test_parse_policy_installed() {
        let apt = AptWrapper::new();
        let output = "curl:\n  Installed: 7.81.0-1ubuntu1.18\n  Candidate: 7.81.0-1ubuntu1.18";
        let (installed, available) = apt.parse_policy_output(output);

        assert_eq!(installed, Some("7.81.0-1ubuntu1.18".to_string()));
        assert_eq!(available, Some("7.81.0-1ubuntu1.18".to_string()));
    }

    #[test]
    fn test_parse_policy_not_installed() {
        let apt = AptWrapper::new();
        let output = "curl:\n  Installed: (none)\n  Candidate: 7.81.0-1ubuntu1.18";
        let (installed, available) = apt.parse_policy_output(output);

        assert_eq!(installed, None);
        assert_eq!(available, Some("7.81.0-1ubuntu1.18".to_string()));
    }

    #[test]
    fn test_parse_show_description() {
        let apt = AptWrapper::new();
        let output = "Package: curl\nDescription: command line tool for transferring data\nHomepage: https://curl.se";
        let description = apt.parse_show_output(output);

        assert_eq!(description, Some("command line tool for transferring data".to_string()));
    }

    #[test]
    fn test_parse_show_multiline() {
        let apt = AptWrapper::new();
        let output = "Package: curl\nDescription: command line tool\n  for transferring data with URL syntax\n  supporting various protocols\nHomepage: https://curl.se";
        let description = apt.parse_show_output(output);

        assert!(description.is_some());
        let desc = description.unwrap();
        assert!(desc.contains("command line tool"));
        assert!(desc.contains("for transferring data"));
    }
}
