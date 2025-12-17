use crate::error::Result;

/// Represents information about a package from a package manager
#[derive(Debug, Clone)]
pub struct PackageInfo {
    /// The package name
    pub name: String,
    /// Currently installed version (None if not installed)
    pub installed_version: Option<String>,
    /// Available version from the repository
    pub available_version: Option<String>,
    /// Package description
    pub description: Option<String>,
    /// Which package manager provided this info
    pub source: String,
}

/// Trait for interacting with platform-specific package managers
///
/// In Rust, traits are similar to C# interfaces, but with some key differences:
/// - Traits can have default implementations
/// - Traits can define associated types and constants
/// - Traits are implemented using `impl Trait for Type` syntax
pub trait PackageManager {
    /// Returns the name of this package manager (e.g., "brew", "apt")
    fn name(&self) -> &str;

    /// Checks if this package manager is available on the current system
    /// This should verify the executable exists and is functional
    fn is_available(&self) -> bool;

    /// Installs a package
    ///
    /// # Arguments
    /// * `package_name` - The name of the package to install
    ///
    /// # Returns
    /// * `Ok(())` if installation succeeded
    /// * `Err` if installation failed or package manager not available
    fn install(&self, package_name: &str) -> Result<()>;

    /// Uninstalls a package
    ///
    /// # Arguments
    /// * `package_name` - The name of the package to uninstall
    ///
    /// # Returns
    /// * `Ok(())` if uninstallation succeeded
    /// * `Err` if uninstallation failed or package not found
    fn uninstall(&self, package_name: &str) -> Result<()>;

    /// Gets information about a package
    ///
    /// # Arguments
    /// * `package_name` - The name of the package to query
    ///
    /// # Returns
    /// * `Ok(PackageInfo)` with package details
    /// * `Err` if package not found or query failed
    fn get_info(&self, package_name: &str) -> Result<PackageInfo>;
}

// Re-export wrapper implementations
pub mod brew;
pub mod apt;
