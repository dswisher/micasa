use crate::error::{MicasaError, Result};
use pep440_rs::{Version, VersionSpecifiers};
use std::str::FromStr;

/// Normalize a version string by stripping platform-specific suffixes
/// For example: "7.81.0-1ubuntu1.18" becomes "7.81.0"
fn normalize_version(version: &str) -> &str {
    version.split('-').next().unwrap_or(version)
}

/// Check if an available version satisfies a version specification
///
/// # Arguments
/// * `available_version` - The version string from the package manager
/// * `version_spec` - Optional PEP 440 version specifier (e.g., ">=0.11.5", "~=0.9.17")
///
/// # Returns
/// * `Ok(true)` if the version satisfies the requirement (or no requirement specified)
/// * `Ok(false)` if the version does not satisfy the requirement
/// * `Err` if version parsing fails
pub fn satisfies_requirement(
    available_version: &str,
    version_spec: Option<&str>,
) -> Result<bool> {
    // If no version spec, any version is acceptable
    let Some(spec_str) = version_spec else {
        return Ok(true);
    };

    // Normalize the available version (strip platform suffixes)
    let normalized_version = normalize_version(available_version);

    // Parse the available version
    let version = Version::from_str(normalized_version).map_err(|e| {
        MicasaError::InvalidVersionSpec(
            normalized_version.to_string(),
            format!("Failed to parse version: {}", e),
        )
    })?;

    // Parse the version specifier
    let specifiers = VersionSpecifiers::from_str(spec_str).map_err(|e| {
        MicasaError::InvalidVersionSpec(
            spec_str.to_string(),
            format!("Failed to parse version specifier: {}", e),
        )
    })?;

    // Check if version satisfies the specifier
    Ok(specifiers.contains(&version))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_normalize_version() {
        assert_eq!(normalize_version("7.81.0"), "7.81.0");
        assert_eq!(normalize_version("7.81.0-1ubuntu1.18"), "7.81.0");
        assert_eq!(normalize_version("0.11.5-2"), "0.11.5");
    }

    #[test]
    fn test_no_version_spec() {
        assert!(satisfies_requirement("1.0.0", None).unwrap());
        assert!(satisfies_requirement("0.11.5", None).unwrap());
    }

    #[test]
    fn test_exact_version() {
        assert!(satisfies_requirement("0.11.5", Some("==0.11.5")).unwrap());
        assert!(!satisfies_requirement("0.11.4", Some("==0.11.5")).unwrap());
    }

    #[test]
    fn test_minimum_version() {
        assert!(satisfies_requirement("0.11.5", Some(">=0.11.0")).unwrap());
        assert!(satisfies_requirement("0.12.0", Some(">=0.11.0")).unwrap());
        assert!(!satisfies_requirement("0.10.0", Some(">=0.11.0")).unwrap());
    }

    #[test]
    fn test_compatible_release() {
        // ~=0.9.17 allows >=0.9.17 and <0.10
        assert!(satisfies_requirement("0.9.17", Some("~=0.9.17")).unwrap());
        assert!(satisfies_requirement("0.9.20", Some("~=0.9.17")).unwrap());
        assert!(!satisfies_requirement("0.10.0", Some("~=0.9.17")).unwrap());
        assert!(!satisfies_requirement("0.9.16", Some("~=0.9.17")).unwrap());
    }

    #[test]
    fn test_ubuntu_version_format() {
        // Ubuntu versions with platform suffixes
        assert!(satisfies_requirement("7.81.0-1ubuntu1.18", Some(">=7.81.0")).unwrap());
        assert!(satisfies_requirement("0.11.5-2", Some(">=0.11.5")).unwrap());
    }

    #[test]
    fn test_version_range() {
        assert!(satisfies_requirement("1.5.0", Some(">=1.0.0,<2.0.0")).unwrap());
        assert!(!satisfies_requirement("2.0.0", Some(">=1.0.0,<2.0.0")).unwrap());
    }
}
