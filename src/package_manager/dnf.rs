use std::process::Command;

use crate::error::{MicasaError, Result};
use crate::package_manager::{PackageInfo, PackageManager};

/// Wrapper for DNF package manager (RPM-based systems: Amazon Linux 2023, RHEL 8+, Fedora 22+)
pub struct DnfWrapper;

impl DnfWrapper {
    pub fn new() -> Self {
        Self
    }

    /// Helper to detect if sudo is available
    fn needs_sudo(&self) -> bool {
        // Check if sudo command is available by trying to run it
        Command::new("sudo")
            .arg("--version")
            .output()
            .map(|output| output.status.success())
            .unwrap_or(false)
    }

    /// Resolve a package name to possible candidates on this platform
    /// Returns a vector with the canonical name first, then aliases
    /// This handles cases where package names differ across platforms
    fn resolve_package_candidates(&self, name: &str) -> Vec<String> {
        match name {
            "curl" => vec!["curl".to_string(), "curl-minimal".to_string()],
            "fd" => vec!["fd-find".to_string()],
            // Add more as needed
            _ => vec![name.to_string()],
        }
    }

    /// Helper to execute dnf commands
    fn execute_dnf(&self, args: &[&str], use_sudo: bool) -> Result<String> {
        let mut cmd = if use_sudo {
            let mut c = Command::new("sudo");
            c.arg("dnf");
            c
        } else {
            Command::new("dnf")
        };

        let output = cmd
            .args(args)
            .output()
            .map_err(|e| {
                MicasaError::CommandExecutionFailed(format!("Failed to execute dnf: {}", e))
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

    /// Parse 'dnf info' output to extract package information
    /// Returns (installed_version, available_version, description)
    fn parse_info_output(&self, output: &str) -> (Option<String>, Option<String>, Option<String>) {
        #[derive(Debug, PartialEq)]
        enum Section {
            None,
            Available,
            Installed,
        }

        let mut section = Section::None;
        let mut available_version = None;
        let mut available_release = None;
        let mut installed_version = None;
        let mut installed_release = None;
        let mut description = None;
        let mut current_key = String::new();
        let mut description_lines = Vec::new();

        for line in output.lines() {
            let trimmed = line.trim();

            // Skip metadata lines
            if trimmed.starts_with("Last metadata") {
                continue;
            }

            // Section headers
            if trimmed == "Available Packages" {
                section = Section::Available;
                continue;
            } else if trimmed == "Installed Packages" {
                section = Section::Installed;
                continue;
            }

            // Check for continuation lines first (they start with whitespace)
            if current_key == "Description" && line.starts_with(' ') {
                // Continuation line for description
                // Strip leading whitespace and optional colon
                let continuation = if line.trim_start().starts_with(':') {
                    line.trim_start().strip_prefix(':').unwrap_or(line).trim()
                } else {
                    line.trim()
                };
                if !continuation.is_empty() {
                    description_lines.push(continuation.to_string());
                }
            }
            // Parse key-value pairs (key is at start of line, not indented)
            else if let Some(colon_pos) = line.find(':') {
                let key = line[..colon_pos].trim();
                let value = line[colon_pos + 1..].trim();

                // Save previous description if we were collecting it
                if current_key == "Description" && !description_lines.is_empty() {
                    if description.is_none() {
                        description = Some(description_lines.join(" "));
                    }
                    description_lines.clear();
                }

                current_key = key.to_string();

                match (&section, key) {
                    (Section::Available, "Version") => available_version = Some(value.to_string()),
                    (Section::Available, "Release") => available_release = Some(value.to_string()),
                    (Section::Installed, "Version") => installed_version = Some(value.to_string()),
                    (Section::Installed, "Release") => installed_release = Some(value.to_string()),
                    (_, "Description") => {
                        if !value.is_empty() {
                            description_lines.push(value.to_string());
                        }
                    }
                    _ => {}
                }
            }
        }

        // Finalize description
        if !description_lines.is_empty() && description.is_none() {
            description = Some(description_lines.join(" "));
        }

        // Combine version and release
        let installed = match (installed_version, installed_release) {
            (Some(v), Some(r)) => Some(format!("{}-{}", v, r)),
            (Some(v), None) => Some(v),
            _ => None,
        };

        let available = match (available_version, available_release) {
            (Some(v), Some(r)) => Some(format!("{}-{}", v, r)),
            (Some(v), None) => Some(v),
            _ => None,
        };

        (installed, available, description)
    }
}

impl PackageManager for DnfWrapper {
    fn name(&self) -> &str {
        "dnf"
    }

    fn is_available(&self) -> bool {
        Command::new("dnf")
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
            if let Ok(info_output) = self.execute_dnf(&["info", candidate], false) {
                let (installed_version, _, _) = self.parse_info_output(&info_output);
                if let Some(version) = installed_version {
                    println!(
                        "{} is already installed (as {}, version {})",
                        package_name, candidate, version
                    );
                    return Ok(());
                }
            }
        }

        println!("Installing {} via dnf...", package_name);

        // Install using the first candidate (canonical name)
        let install_target = &candidates[0];

        // Try without sudo first
        let result = self.execute_dnf(&["install", "-y", install_target], false);

        // If permission error and sudo is available, retry with sudo
        if let Err(MicasaError::CommandFailed { ref stderr, .. }) = result {
            if stderr.contains("permission")
                || stderr.contains("not permitted")
                || stderr.contains("Cannot open")
                || stderr.contains("This command has to be run with superuser privileges") {
                if self.needs_sudo() {
                    println!("Retrying with sudo...");
                    self.execute_dnf(&["install", "-y", install_target], true)?;
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

        // Find which candidate is actually installed
        let candidates = self.resolve_package_candidates(package_name);
        let mut installed_candidate = None;

        for candidate in &candidates {
            if let Ok(info_output) = self.execute_dnf(&["info", candidate], false) {
                let (installed_version, _, _) = self.parse_info_output(&info_output);
                if installed_version.is_some() {
                    installed_candidate = Some(candidate.clone());
                    break;
                }
            }
        }

        let uninstall_target = match installed_candidate {
            Some(candidate) => {
                if &candidate != package_name {
                    println!("Uninstalling {} (installed as {}) via dnf...", package_name, candidate);
                } else {
                    println!("Uninstalling {} via dnf...", package_name);
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

        // Try without sudo first
        let result = self.execute_dnf(&["remove", "-y", &uninstall_target], false);

        // If permission error and sudo is available, retry with sudo
        if let Err(MicasaError::CommandFailed { ref stderr, .. }) = result {
            if stderr.contains("permission")
                || stderr.contains("not permitted")
                || stderr.contains("Cannot open")
                || stderr.contains("This command has to be run with superuser privileges") {
                if self.needs_sudo() {
                    println!("Retrying with sudo...");
                    self.execute_dnf(&["remove", "-y", &uninstall_target], true)?;
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

        // Try each candidate package name
        // First pass: look for installed packages (highest priority)
        // Second pass: look for available packages
        let candidates = self.resolve_package_candidates(package_name);
        let mut first_error = None;
        let mut available_info = None;

        for candidate in &candidates {
            match self.execute_dnf(&["info", candidate], false) {
                Ok(info_output) => {
                    let (installed_version, available_version, description) =
                        self.parse_info_output(&info_output);

                    // If installed, return immediately (highest priority)
                    if installed_version.is_some() {
                        return Ok(PackageInfo {
                            name: package_name.to_string(), // Use canonical name
                            installed_version,
                            available_version,
                            description,
                            source: self.name().to_string(),
                        });
                    }

                    // Save available info as fallback
                    if available_version.is_some() && available_info.is_none() {
                        available_info = Some(PackageInfo {
                            name: package_name.to_string(),
                            installed_version: None,
                            available_version,
                            description,
                            source: self.name().to_string(),
                        });
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
        let dnf = DnfWrapper::new();
        assert_eq!(dnf.name(), "dnf");
    }

    #[test]
    #[cfg(target_os = "linux")]
    fn test_is_available() {
        let dnf = DnfWrapper::new();
        // This will only pass on Linux with dnf installed
        // On systems without dnf, this test will fail (which is expected)
        let _ = dnf.is_available();
    }

    #[test]
    fn test_parse_info_not_installed() {
        let dnf = DnfWrapper::new();
        let output = "Available Packages\nName         : curl\nVersion      : 7.76.1\nRelease      : 8.amzn2023.0.2\nDescription  : command line tool for transferring data";
        let (installed, available, description) = dnf.parse_info_output(output);

        assert_eq!(installed, None);
        assert_eq!(available, Some("7.76.1-8.amzn2023.0.2".to_string()));
        assert_eq!(description, Some("command line tool for transferring data".to_string()));
    }

    #[test]
    fn test_parse_info_installed() {
        let dnf = DnfWrapper::new();
        let output = "Available Packages\nName         : curl\nVersion      : 7.76.1\nRelease      : 8.amzn2023.0.2\nDescription  : newer version\n\nInstalled Packages\nName         : curl\nVersion      : 7.76.1\nRelease      : 7.amzn2023.0.1\nDescription  : command line tool";
        let (installed, available, description) = dnf.parse_info_output(output);

        assert_eq!(installed, Some("7.76.1-7.amzn2023.0.1".to_string()));
        assert_eq!(available, Some("7.76.1-8.amzn2023.0.2".to_string()));
        assert!(description.is_some());
    }

    #[test]
    fn test_parse_info_with_multiline_description() {
        let dnf = DnfWrapper::new();
        let output = "Available Packages\nName         : curl\nVersion      : 7.76.1\nRelease      : 8.amzn2023.0.2\nDescription  : command line tool\n             : for transferring data\n             : with URL syntax";
        let (installed, available, description) = dnf.parse_info_output(output);

        assert_eq!(installed, None);
        assert_eq!(available, Some("7.76.1-8.amzn2023.0.2".to_string()));
        assert_eq!(
            description,
            Some("command line tool for transferring data with URL syntax".to_string())
        );
    }

    #[test]
    fn test_parse_info_skip_metadata() {
        let dnf = DnfWrapper::new();
        let output = "Last metadata expiration check: 0:05:32 ago on Tue Dec 17 12:34:56 2024.\nAvailable Packages\nName         : curl\nVersion      : 7.76.1\nRelease      : 8.amzn2023.0.2\nDescription  : tool";
        let (installed, available, _) = dnf.parse_info_output(output);

        assert_eq!(installed, None);
        assert_eq!(available, Some("7.76.1-8.amzn2023.0.2".to_string()));
    }
}
