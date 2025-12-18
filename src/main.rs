use clap::{Parser, Subcommand};

mod commands;
mod error;
mod manifest;
mod package_manager;
mod platform;
mod version;

#[derive(Parser)]
#[command(name = "micasa")]
#[command(about = "A CLI wrapper to provide consistent cross-platform package management.", long_about = None)]
struct Cli {
    #[command(subcommand)]
    command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    /// Install a package or all packages from manifest
    Install {
        /// Name of the package to install (if not provided, installs from manifest)
        package: Option<String>,

        /// Optional source or path
        #[arg(short, long)]
        source: Option<String>,
    },

    /// Uninstall a package
    Uninstall {
        /// Name of the package to uninstall
        package: String,
    },

    /// Show information about a package or all packages from manifest
    Info {
        /// Name of the package to get info about (if not provided, uses manifest)
        package: Option<String>,
    },
}

fn main() {
    let cli = Cli::parse();

    let result = match &cli.command {
        Commands::Install { package, source: _ } => {
            // TODO: Handle source parameter later
            match package {
                Some(pkg) => commands::install::execute_single(pkg),
                None => commands::install::execute_manifest(),
            }
        }

        Commands::Uninstall { package } => commands::uninstall::execute(package),

        Commands::Info { package } => match package {
            Some(pkg) => commands::info::execute_single(pkg),
            None => commands::info::execute_manifest(),
        },
    };

    if let Err(e) = result {
        eprintln!("Error: {}", e);
        std::process::exit(1);
    }
}
