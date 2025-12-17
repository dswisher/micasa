use clap::{Parser, Subcommand};

mod commands;
mod error;
mod package_manager;
mod platform;

#[derive(Parser)]
#[command(name = "micasa")]
#[command(about = "A CLI wrapper to provide consistent cross-platform package management.", long_about = None)]
struct Cli {
    #[command(subcommand)]
    command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    /// Install a package
    Install {
        /// Name of the package to install
        package: String,

        /// Optional source or path
        #[arg(short, long)]
        source: Option<String>,
    },

    /// Uninstall a package
    Uninstall {
        /// Name of the package to uninstall
        package: String,
    },

    /// Show information about a package
    Info {
        /// Name of the package to get info about
        package: String,
    },
}

fn main() {
    let cli = Cli::parse();

    let result = match &cli.command {
        Commands::Install { package, source: _ } => {
            // TODO: Handle source parameter later
            commands::install::execute(package)
        }

        Commands::Uninstall { package } => commands::uninstall::execute(package),

        Commands::Info { package } => commands::info::execute(package),
    };

    if let Err(e) = result {
        eprintln!("Error: {}", e);
        std::process::exit(1);
    }
}
