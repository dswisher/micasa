import platform
import subprocess

from micasa.blueprint import Blueprint
from micasa.executable_finder import ExecutableFinder
from micasa.manifest import Manifest


def run(args):
    """Run the install command."""
    manifest = Manifest.load()

    # Find the package in the manifest
    package = None
    for p in manifest.packages:
        if p.name == args.package:
            package = p
            break

    if not package:
        print(f"Error: Package '{args.package}' not found in manifest")
        return

    # Load the blueprint
    blueprint = Blueprint.load(args.package)
    if not blueprint:
        print(f"Error: No blueprint found for package '{args.package}'")
        return

    # Check if the package is already installed
    installed_version = blueprint.check_installed_version()
    if installed_version:
        print(f"Package '{args.package}' is already installed (version {installed_version})")
        return

    # Check if we're on macOS
    if platform.system() != "Darwin":
        print("Error: Installation is currently only supported on macOS")
        print(f"Detected platform: {platform.system()}")
        return

    # Check if brew is installed
    brew_finder = ExecutableFinder("brew")
    brew_path = brew_finder.find()
    if not brew_path:
        print("Error: 'brew' is not installed or not in PATH")
        print("Please install Homebrew from https://brew.sh/")
        return

    # Get the brew package name from the blueprint
    brew_name = blueprint.get_brew_name()
    if not brew_name:
        print(f"Error: No brew package name specified in blueprint for '{args.package}'")
        return

    # Install the package using brew
    print(f"Installing '{args.package}' using brew...")
    print(f"Running: brew install {brew_name}")
    print()

    try:
        subprocess.run(
            ["brew", "install", brew_name],
            check=True,
            capture_output=False
        )
        print()
        print(f"Successfully installed '{args.package}'")
    except subprocess.CalledProcessError as e:
        print()
        print(f"Error: Failed to install '{args.package}'")
        print(f"brew install exited with code {e.returncode}")
