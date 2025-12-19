use crate::error::{MicasaError, Result};
use crate::package_manager::{PackageInfo, PackageManager};
use flate2::read::GzDecoder;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::env;
use std::fs::{self, File};
use std::os::unix::fs::PermissionsExt;
use std::path::{Path, PathBuf};
use std::process::Command;
use tar::Archive;

/// Represents a GitHub release asset
#[derive(Debug, Deserialize)]
struct GitHubAsset {
    name: String,
    browser_download_url: String,
}

/// Represents a GitHub release
#[derive(Debug, Deserialize)]
struct GitHubRelease {
    tag_name: String,
    assets: Vec<GitHubAsset>,
}

/// Information about a package's GitHub source
#[derive(Debug, Clone)]
struct PackageSource {
    /// GitHub repository (e.g., "sharkdp/fd")
    repo: String,
    /// The executable name(s) to look for after extraction
    executables: Vec<String>,
}

/// State tracking for installed packages
#[derive(Debug, Serialize, Deserialize, Default)]
struct InstallState {
    packages: HashMap<String, InstalledPackage>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct InstalledPackage {
    version: String,
    source: String,
    install_path: PathBuf,
    symlinks: Vec<PathBuf>,
}

/// GitHub package manager implementation
pub struct GitHubWrapper {
    registry: HashMap<String, PackageSource>,
    state_file: PathBuf,
    packages_dir: PathBuf,
    bin_dir: PathBuf,
}

impl GitHubWrapper {
    /// Creates a new GitHub package manager wrapper
    pub fn new() -> Self {
        let home_dir = env::var("HOME").expect("HOME environment variable not set");
        let home = PathBuf::from(home_dir);

        let packages_dir = home.join(".local").join("packages");
        let bin_dir = home.join(".local").join("bin");
        let state_file = home.join(".local").join("share").join("micasa").join("github-installed.json");

        let mut registry = HashMap::new();

        // Initialize registry with known packages
        registry.insert(
            "fd".to_string(),
            PackageSource {
                repo: "sharkdp/fd".to_string(),
                executables: vec!["fd".to_string()],
            },
        );

        registry.insert(
            "bat".to_string(),
            PackageSource {
                repo: "sharkdp/bat".to_string(),
                executables: vec!["bat".to_string()],
            },
        );

        registry.insert(
            "ripgrep".to_string(),
            PackageSource {
                repo: "BurntSushi/ripgrep".to_string(),
                executables: vec!["rg".to_string()],
            },
        );

        registry.insert(
            "eza".to_string(),
            PackageSource {
                repo: "eza-community/eza".to_string(),
                executables: vec!["eza".to_string()],
            },
        );

        registry.insert(
            "fzf".to_string(),
            PackageSource {
                repo: "junegunn/fzf".to_string(),
                executables: vec!["fzf".to_string()],
            },
        );

        registry.insert(
            "lazygit".to_string(),
            PackageSource {
                repo: "jesseduffield/lazygit".to_string(),
                executables: vec!["lazygit".to_string()],
            },
        );

        registry.insert(
            "chezmoi".to_string(),
            PackageSource {
                repo: "twpayne/chezmoi".to_string(),
                executables: vec!["chezmoi".to_string()],
            },
        );

        registry.insert(
            "neovim".to_string(),
            PackageSource {
                repo: "neovim/neovim".to_string(),
                executables: vec!["nvim".to_string()],
            },
        );

        registry.insert(
            "uv".to_string(),
            PackageSource {
                repo: "astral-sh/uv".to_string(),
                executables: vec!["uv".to_string(), "uvx".to_string()],
            },
        );

        Self {
            registry,
            state_file,
            packages_dir,
            bin_dir,
        }
    }

    /// Load installation state from disk
    fn load_state(&self) -> Result<InstallState> {
        if self.state_file.exists() {
            let content = fs::read_to_string(&self.state_file)?;
            serde_json::from_str(&content)
                .map_err(|e| MicasaError::JsonParseError(e.to_string()))
        } else {
            Ok(InstallState::default())
        }
    }

    /// Save installation state to disk
    fn save_state(&self, state: &InstallState) -> Result<()> {
        // Ensure parent directory exists
        if let Some(parent) = self.state_file.parent() {
            fs::create_dir_all(parent)?;
        }

        let content = serde_json::to_string_pretty(state)
            .map_err(|e| MicasaError::JsonParseError(e.to_string()))?;
        fs::write(&self.state_file, content)?;
        Ok(())
    }

    /// Get the latest release for a repository
    fn get_latest_release(&self, repo: &str) -> Result<GitHubRelease> {
        let url = format!("https://api.github.com/repos/{}/releases/latest", repo);

        let output = Command::new("curl")
            .args(["-s", "-L", "-H", "Accept: application/vnd.github+json", &url])
            .output()
            .map_err(|e| MicasaError::CommandExecutionFailed(format!("Failed to fetch release info: {}", e)))?;

        if !output.status.success() {
            return Err(MicasaError::CommandExecutionFailed(
                format!("Failed to fetch release for {}", repo)
            ));
        }

        let json_str = String::from_utf8(output.stdout)?;
        serde_json::from_str(&json_str)
            .map_err(|e| MicasaError::JsonParseError(format!("Failed to parse GitHub response: {}", e)))
    }

    /// Score an asset based on how well it matches our platform
    fn score_asset(&self, asset_name: &str) -> i32 {
        let os = std::env::consts::OS;
        let arch = std::env::consts::ARCH;

        let asset_lower = asset_name.to_lowercase();
        let mut score = 0;

        // OS matching
        match os {
            "linux" => {
                if asset_lower.contains("linux") {
                    score += 10;
                }
                // Prefer musl for better portability
                if asset_lower.contains("musl") {
                    score += 5;
                } else if asset_lower.contains("gnu") {
                    score += 3;
                }
            }
            "macos" => {
                if asset_lower.contains("darwin") || asset_lower.contains("macos") || asset_lower.contains("apple") {
                    score += 10;
                }
            }
            _ => {}
        }

        // Architecture matching
        match arch {
            "x86_64" => {
                if asset_lower.contains("x86_64") || asset_lower.contains("amd64") {
                    score += 10;
                }
            }
            "aarch64" => {
                if asset_lower.contains("aarch64") || asset_lower.contains("arm64") {
                    score += 10;
                }
            }
            _ => {}
        }

        // Prefer archives over other formats
        if asset_lower.ends_with(".tar.gz") || asset_lower.ends_with(".tgz") {
            score += 2;
        } else if asset_lower.ends_with(".zip") {
            score += 1;
        }

        // Avoid debug builds
        if asset_lower.contains("debug") {
            score -= 10;
        }

        score
    }

    /// Find the best matching asset for the current platform
    fn find_best_asset<'a>(&self, assets: &'a [GitHubAsset]) -> Option<&'a GitHubAsset> {
        assets
            .iter()
            .map(|asset| (asset, self.score_asset(&asset.name)))
            .filter(|(_, score)| *score > 0)
            .max_by_key(|(_, score)| *score)
            .map(|(asset, _)| asset)
    }

    /// Download a file from URL to destination
    fn download_file(&self, url: &str, dest: &Path) -> Result<()> {
        // Ensure parent directory exists
        if let Some(parent) = dest.parent() {
            fs::create_dir_all(parent)?;
        }

        let output = Command::new("curl")
            .args(["-L", "-o", dest.to_str().unwrap(), url])
            .output()
            .map_err(|e| MicasaError::CommandExecutionFailed(format!("Failed to download file: {}", e)))?;

        if !output.status.success() {
            return Err(MicasaError::CommandExecutionFailed(
                format!("Download failed: {}", String::from_utf8_lossy(&output.stderr))
            ));
        }

        Ok(())
    }

    /// Extract a tar.gz archive to destination, stripping the top-level directory
    fn extract_tar_gz(&self, archive_path: &Path, dest: &Path) -> Result<()> {
        // Ensure destination directory exists
        fs::create_dir_all(dest)?;

        // First pass: collect all paths to check for common prefix
        let file = File::open(archive_path)?;
        let decoder = GzDecoder::new(file);
        let mut archive = Archive::new(decoder);

        let mut paths = Vec::new();
        for entry in archive.entries()? {
            let entry = entry?;
            paths.push(entry.path()?.into_owned());
        }

        // Check if all entries have a common root directory
        // A common prefix only makes sense if files are nested (have more than one component)
        let mut common_prefix: Option<PathBuf> = None;
        let has_nested_paths = paths.iter().any(|p| p.components().count() > 1);

        if has_nested_paths {
            for path in &paths {
                if let Some(first_component) = path.components().next() {
                    let prefix = PathBuf::from(first_component.as_os_str());
                    match &common_prefix {
                        None => common_prefix = Some(prefix),
                        Some(existing) => {
                            if existing != &prefix {
                                common_prefix = None;
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Second pass: extract files with path transformations
        let file = File::open(archive_path)?;
        let decoder = GzDecoder::new(file);
        let mut archive = Archive::new(decoder);

        for entry in archive.entries()? {
            let mut entry = entry?;
            let path = entry.path()?.into_owned();

            // Strip common prefix if present
            let final_path = if let Some(ref prefix) = common_prefix {
                path.strip_prefix(prefix)
                    .map(|p| dest.join(p))
                    .unwrap_or_else(|_| dest.join(&path))
            } else {
                dest.join(&path)
            };

            // Create parent directories
            if let Some(parent) = final_path.parent() {
                fs::create_dir_all(parent)?;
            }

            // Extract the entry
            entry.unpack(&final_path).map_err(|e| {
                MicasaError::CommandExecutionFailed(format!(
                    "Failed to unpack '{}' to '{}': {}",
                    path.display(),
                    final_path.display(),
                    e
                ))
            })?;
        }

        Ok(())
    }

    /// Find executables in a directory
    fn find_executables(&self, dir: &Path, expected_names: &[String]) -> Result<Vec<PathBuf>> {
        let mut executables = Vec::new();

        // Try common locations first
        let search_paths = vec![
            dir.to_path_buf(),
            dir.join("bin"),
        ];

        for search_path in search_paths {
            if !search_path.exists() {
                continue;
            }

            for entry in fs::read_dir(&search_path)? {
                let entry = entry?;
                let path = entry.path();

                if !path.is_file() {
                    continue;
                }

                // Check if it's executable
                let metadata = fs::metadata(&path)?;
                let permissions = metadata.permissions();
                if permissions.mode() & 0o111 == 0 {
                    continue;
                }

                // Check if it matches expected names
                if let Some(name) = path.file_name().and_then(|n| n.to_str()) {
                    if expected_names.iter().any(|expected| name == expected) {
                        executables.push(path);
                    }
                }
            }
        }

        if executables.is_empty() {
            Err(MicasaError::CommandExecutionFailed(
                "No executables found in extracted package".to_string()
            ))
        } else {
            Ok(executables)
        }
    }

    /// Create symlinks for executables
    fn create_symlinks(&self, executables: &[PathBuf]) -> Result<Vec<PathBuf>> {
        fs::create_dir_all(&self.bin_dir)?;

        let mut symlinks = Vec::new();

        for exe in executables {
            if let Some(name) = exe.file_name() {
                let link = self.bin_dir.join(name);

                // Remove existing symlink if present
                if link.exists() {
                    fs::remove_file(&link)?;
                }

                // Create symlink
                std::os::unix::fs::symlink(exe, &link)?;
                symlinks.push(link);
            }
        }

        Ok(symlinks)
    }

    /// Parse version from tag name (strips 'v' prefix)
    fn parse_version(&self, tag_name: &str) -> String {
        tag_name.strip_prefix('v').unwrap_or(tag_name).to_string()
    }

    /// Ensures curl is installed, installing it if necessary
    fn ensure_curl_installed(&self) -> Result<()> {
        // Check if curl is already available
        if Command::new("curl").arg("--version").output().is_ok() {
            return Ok(());
        }

        println!("  curl is required for GitHub package manager, attempting to install...");

        // Try to install curl using the system package manager
        let install_commands = vec![
            vec!["apt-get", "install", "-y", "curl"],
            vec!["dnf", "install", "-y", "curl"],
            vec!["yum", "install", "-y", "curl"],
            vec!["brew", "install", "curl"],
        ];

        for cmd in install_commands {
            let result = Command::new("sudo")
                .args(&cmd)
                .output();

            if let Ok(output) = result {
                if output.status.success() {
                    // Verify curl is now available
                    if Command::new("curl").arg("--version").output().is_ok() {
                        println!("  Successfully installed curl");
                        return Ok(());
                    }
                }
            }
        }

        Err(MicasaError::CommandExecutionFailed(
            "Failed to install curl. Please install curl manually to use GitHub package manager.".to_string()
        ))
    }
}

impl PackageManager for GitHubWrapper {
    fn name(&self) -> &str {
        "github"
    }

    fn is_available(&self) -> bool {
        // GitHub package manager is always "available" as a fallback
        // If curl is missing, we'll try to install it when needed
        true
    }

    fn install(&self, package_name: &str) -> Result<()> {
        // Ensure curl is installed (required for downloading)
        self.ensure_curl_installed()?;

        // Check if package is in registry
        let source = self.registry.get(package_name)
            .ok_or_else(|| MicasaError::PackageNotFound(package_name.to_string()))?;

        println!("  Fetching release information from GitHub...");
        let release = self.get_latest_release(&source.repo)?;
        let version = self.parse_version(&release.tag_name);

        // Find best matching asset
        let asset = self.find_best_asset(&release.assets)
            .ok_or_else(|| MicasaError::CommandExecutionFailed(
                format!("No suitable release asset found for {} on this platform", package_name)
            ))?;

        println!("  Downloading {} version {} ({})", package_name, version, asset.name);

        // Create download directory
        let cache_dir = self.packages_dir.join(".cache");
        fs::create_dir_all(&cache_dir)?;
        let archive_path = cache_dir.join(&asset.name);

        // Download the asset
        self.download_file(&asset.browser_download_url, &archive_path)?;

        // Extract to versioned directory
        let install_dir = self.packages_dir.join(package_name).join(&version);
        println!("  Extracting to {}...", install_dir.display());
        self.extract_tar_gz(&archive_path, &install_dir)?;

        // Find executables
        let executables = self.find_executables(&install_dir, &source.executables)?;
        println!("  Found {} executable(s)", executables.len());

        // Create symlinks
        let symlinks = self.create_symlinks(&executables)?;
        println!("  Created {} symlink(s) in {}", symlinks.len(), self.bin_dir.display());

        // Update state
        let mut state = self.load_state()?;
        state.packages.insert(
            package_name.to_string(),
            InstalledPackage {
                version: version.clone(),
                source: "github".to_string(),
                install_path: install_dir,
                symlinks,
            },
        );
        self.save_state(&state)?;

        Ok(())
    }

    fn uninstall(&self, package_name: &str) -> Result<()> {
        let mut state = self.load_state()?;

        let package = state.packages.get(package_name)
            .ok_or_else(|| MicasaError::PackageNotFound(package_name.to_string()))?
            .clone();

        // Remove symlinks
        for symlink in &package.symlinks {
            if symlink.exists() {
                fs::remove_file(symlink)?;
            }
        }

        // Remove installation directory
        if package.install_path.exists() {
            fs::remove_dir_all(&package.install_path)?;
        }

        // Update state
        state.packages.remove(package_name);
        self.save_state(&state)?;

        println!("Successfully uninstalled {} (version {})", package_name, package.version);
        Ok(())
    }

    fn get_info(&self, package_name: &str) -> Result<PackageInfo> {
        // Ensure curl is installed (required for fetching release info)
        self.ensure_curl_installed()?;

        // Check if package is in registry
        let source = self.registry.get(package_name)
            .ok_or_else(|| MicasaError::PackageNotFound(package_name.to_string()))?;

        // Check if installed
        let state = self.load_state()?;
        let installed_version = state.packages.get(package_name)
            .map(|p| p.version.clone());

        // Get latest available version
        let release = self.get_latest_release(&source.repo)?;
        let available_version = Some(self.parse_version(&release.tag_name));

        Ok(PackageInfo {
            name: package_name.to_string(),
            installed_version,
            available_version,
            description: Some(format!("GitHub: {}", source.repo)),
            source: "github".to_string(),
        })
    }
}
