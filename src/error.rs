use std::io;
use std::path::PathBuf;
use std::process::ExitStatus;
use thiserror::Error;

/// Custom error type for micasa operations
///
/// In Rust, we use Result<T, E> instead of exceptions. The `?` operator
/// propagates errors up the call stack (similar to C#'s throw, but explicit).
/// The thiserror crate generates boilerplate Display/Error implementations.
#[derive(Error, Debug)]
pub enum MicasaError {
    #[error("Package manager '{0}' is not available on this system")]
    PackageManagerNotAvailable(String),

    #[error("Failed to execute command: {0}")]
    CommandExecutionFailed(String),

    #[error("Command exited with status {status}: {stderr}")]
    CommandFailed { status: ExitStatus, stderr: String },

    #[error("No package managers available on this platform")]
    NoPackageManagers,

    #[error("Failed to parse JSON response: {0}")]
    JsonParseError(String),

    #[error("IO error: {0}")]
    Io(#[from] io::Error),

    #[error("UTF-8 conversion error: {0}")]
    Utf8(#[from] std::string::FromUtf8Error),

    #[error("Manifest file not found at {0}")]
    ManifestNotFound(PathBuf),

    #[error("Failed to parse manifest: {0}")]
    ManifestParseError(String),

    #[error("Invalid version specification '{0}': {1}")]
    InvalidVersionSpec(String, String),

    #[error("No package manager provides satisfactory version for '{0}'. Required: {1}")]
    NoSatisfactoryVersion(String, String),

    #[error("Package '{0}' not found in any available package manager")]
    PackageNotFound(String),
}

/// Type alias for Result with our error type
/// This is idiomatic Rust - similar to defining a custom Result<T> in C#
pub type Result<T> = std::result::Result<T, MicasaError>;
