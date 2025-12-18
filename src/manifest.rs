use crate::error::{MicasaError, Result};
use std::env;
use std::fs;
use std::path::PathBuf;

/// Represents a single entry in the manifest file
#[derive(Debug, Clone, PartialEq)]
pub struct ManifestEntry {
    /// The package name
    pub name: String,
    /// Optional version specification (e.g., ">=0.11.5", "~=0.9.17")
    pub version_spec: Option<String>,
}

/// Represents the manifest file containing package specifications
#[derive(Debug, Clone)]
pub struct Manifest {
    /// List of package entries from the manifest
    entries: Vec<ManifestEntry>,
}

impl Manifest {
    /// Get the default manifest file path (~/.config/micasa/micasa.txt)
    /// Respects XDG_CONFIG_HOME environment variable
    pub fn get_default_path() -> Result<PathBuf> {
        let config_dir = if let Ok(xdg_home) = env::var("XDG_CONFIG_HOME") {
            PathBuf::from(xdg_home)
        } else {
            // Fall back to ~/.config
            let home_dir = env::var("HOME").map_err(|_| {
                MicasaError::ManifestNotFound(PathBuf::from(
                    "Unable to determine home directory",
                ))
            })?;
            PathBuf::from(home_dir).join(".config")
        };

        Ok(config_dir.join("micasa").join("micasa.txt"))
    }

    /// Read and parse the manifest from the default location
    pub fn read_default() -> Result<Self> {
        let path = Self::get_default_path()?;
        Self::read_from_path(&path)
    }

    /// Read and parse manifest from a specific path
    pub fn read_from_path(path: &PathBuf) -> Result<Self> {
        let content = fs::read_to_string(path)
            .map_err(|_| MicasaError::ManifestNotFound(path.clone()))?;

        let entries = Self::parse_content(&content)?;

        Ok(Manifest { entries })
    }

    /// Parse manifest content from a string
    fn parse_content(content: &str) -> Result<Vec<ManifestEntry>> {
        let mut entries = Vec::new();

        for (line_num, line) in content.lines().enumerate() {
            let trimmed = line.trim();

            // Skip blank lines and comments
            if trimmed.is_empty() || trimmed.starts_with('#') {
                continue;
            }

            // Parse the line
            let entry = if let Some((name, version)) = trimmed.split_once(':') {
                // Line contains a version spec
                ManifestEntry {
                    name: name.trim().to_string(),
                    version_spec: Some(version.trim().to_string()),
                }
            } else {
                // Line is just a package name
                ManifestEntry {
                    name: trimmed.to_string(),
                    version_spec: None,
                }
            };

            // Validate that package name is not empty
            if entry.name.is_empty() {
                return Err(MicasaError::ManifestParseError(format!(
                    "Empty package name on line {}",
                    line_num + 1
                )));
            }

            entries.push(entry);
        }

        Ok(entries)
    }

    /// Get all package entries
    pub fn entries(&self) -> &[ManifestEntry] {
        &self.entries
    }

    /// Check if manifest is empty
    pub fn is_empty(&self) -> bool {
        self.entries.is_empty()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_simple_package() {
        let content = "neovim";
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 1);
        assert_eq!(entries[0].name, "neovim");
        assert_eq!(entries[0].version_spec, None);
    }

    #[test]
    fn test_parse_package_with_version() {
        let content = "neovim: >=0.11.5";
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 1);
        assert_eq!(entries[0].name, "neovim");
        assert_eq!(entries[0].version_spec, Some(">=0.11.5".to_string()));
    }

    #[test]
    fn test_parse_multiple_packages() {
        let content = r#"
neovim: >=0.11.5
curl
eza
uv: ~=0.9.17
"#;
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 4);
        assert_eq!(entries[0].name, "neovim");
        assert_eq!(entries[0].version_spec, Some(">=0.11.5".to_string()));
        assert_eq!(entries[1].name, "curl");
        assert_eq!(entries[1].version_spec, None);
        assert_eq!(entries[2].name, "eza");
        assert_eq!(entries[3].name, "uv");
    }

    #[test]
    fn test_parse_with_comments() {
        let content = r#"
# This is a comment
neovim: >=0.11.5
# Another comment
curl
"#;
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 2);
        assert_eq!(entries[0].name, "neovim");
        assert_eq!(entries[1].name, "curl");
    }

    #[test]
    fn test_parse_with_blank_lines() {
        let content = r#"
neovim


curl
"#;
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 2);
    }

    #[test]
    fn test_parse_with_whitespace() {
        let content = "  neovim  :  >=0.11.5  ";
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 1);
        assert_eq!(entries[0].name, "neovim");
        assert_eq!(entries[0].version_spec, Some(">=0.11.5".to_string()));
    }

    #[test]
    fn test_parse_compatible_release_operator() {
        let content = "chezmoi: ~=2.68.1";
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 1);
        assert_eq!(entries[0].name, "chezmoi");
        assert_eq!(entries[0].version_spec, Some("~=2.68.1".to_string()));
    }

    #[test]
    fn test_parse_empty_content() {
        let content = "";
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 0);
    }

    #[test]
    fn test_parse_only_comments() {
        let content = r#"
# Comment 1
# Comment 2
"#;
        let entries = Manifest::parse_content(content).unwrap();

        assert_eq!(entries.len(), 0);
    }
}
