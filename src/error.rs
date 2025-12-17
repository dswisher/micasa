use std::io;
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
}

/// Type alias for Result with our error type
/// This is idiomatic Rust - similar to defining a custom Result<T> in C#
pub type Result<T> = std::result::Result<T, MicasaError>;
