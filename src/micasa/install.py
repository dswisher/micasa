import subprocess
from typing import Optional

from micasa.blueprint import Blueprint
from micasa.executable_finder import ExecutableFinder
from micasa.manifest import Manifest
from micasa.platform_detector import PlatformDetector


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

    # Detect the platform
    detector = PlatformDetector()

    if detector.is_macos():
        _install_macos(args.package, blueprint)
    elif detector.is_ubuntu():
        distro_key = detector.get_ubuntu_key()
        _install_apt_get(args.package, blueprint, distro_key, "Ubuntu", detector.get_version_id())
    elif detector.is_debian():
        distro_key = detector.get_debian_key()
        _install_apt_get(args.package, blueprint, distro_key, "Debian", detector.get_version_id())
    elif detector.is_amazonlinux():
        _install_amazonlinux(args.package, blueprint, detector)
    else:
        print("Error: Installation is not supported on this platform")
        print(f"Detected OS: {detector.get_os_type()}")
        if detector.is_linux():
            print(f"Distribution: {detector.get_distribution_id()}")


def _install_macos(package_name: str, blueprint: Blueprint):
    """Install a package on macOS using brew.

    Args:
        package_name: The name of the package to install
        blueprint: The package blueprint

    Returns:
        None
    """
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
        print(f"Error: No brew package name specified in blueprint for '{package_name}'")
        return

    # Install the package using brew
    print(f"Installing '{package_name}' using brew...")
    print(f"Running: brew install {brew_name}")
    print()

    try:
        subprocess.run(
            ["brew", "install", brew_name],
            check=True,
            capture_output=False
        )
        print()
        print(f"Successfully installed '{package_name}'")
    except subprocess.CalledProcessError as e:
        print()
        print(f"Error: Failed to install '{package_name}'")
        print(f"brew install exited with code {e.returncode}")


def _install_apt_get(package_name: str, blueprint: Blueprint, distro_key: Optional[str], distro_name: str, version_id: Optional[str]):
    """Install a package using apt-get (for Ubuntu, Debian, etc.).

    Args:
        package_name: The name of the package to install
        blueprint: The package blueprint
        distro_key: The distribution version key (e.g., 'ubuntu22', 'debian12')
        distro_name: The distribution name for display (e.g., 'Ubuntu', 'Debian')
        version_id: The version ID for display (e.g., '22.04', '12')

    Returns:
        None
    """
    # Check if we have a distribution key
    if not distro_key:
        print(f"Error: Unable to determine {distro_name} version")
        print(f"Version ID: {version_id}")
        return

    # Check if apt-get is installed
    apt_get_finder = ExecutableFinder("apt-get")
    apt_get_path = apt_get_finder.find()
    if not apt_get_path:
        print("Error: 'apt-get' is not installed or not in PATH")
        return

    # Get the apt-get package name from the blueprint
    apt_get_name = blueprint.get_apt_get_name(distro_key)
    if not apt_get_name:
        # No apt-get package available, try curl fallback
        print(f"No apt-get package specified in blueprint for '{package_name}'")
        print(f"{distro_name} version: {distro_key}")
        print("Falling back to curl installation method...")
        print()
        _install_with_curl(package_name, blueprint)
        return

    # Install the package using apt-get
    print(f"Installing '{package_name}' using apt-get...")
    print(f"{distro_name} version: {version_id}")
    print(f"Running: sudo apt-get update && sudo apt-get install -y {apt_get_name}")
    print()

    try:
        # Update package lists first
        subprocess.run(
            ["sudo", "apt-get", "update"],
            check=True,
            capture_output=False
        )
        print()

        # Install the package
        subprocess.run(
            ["sudo", "apt-get", "install", "-y", apt_get_name],
            check=True,
            capture_output=False
        )
        print()
        print(f"Successfully installed '{package_name}'")
    except subprocess.CalledProcessError as e:
        print()
        print(f"Error: Failed to install '{package_name}'")
        print(f"apt-get exited with code {e.returncode}")


def _install_amazonlinux(package_name: str, blueprint: Blueprint, detector: PlatformDetector):
    """Install a package on Amazon Linux using dnf.

    Args:
        package_name: The name of the package to install
        blueprint: The package blueprint
        detector: The platform detector

    Returns:
        None
    """
    # Get the Amazon Linux version key
    amazonlinux_key = detector.get_amazonlinux_key()
    if not amazonlinux_key:
        print("Error: Unable to determine Amazon Linux version")
        print(f"Version ID: {detector.get_version_id()}")
        return

    # Check if dnf is installed
    dnf_finder = ExecutableFinder("dnf")
    dnf_path = dnf_finder.find()
    if not dnf_path:
        print("Error: 'dnf' is not installed or not in PATH")
        return

    # Get the dnf package name from the blueprint
    dnf_name = blueprint.get_dnf_name(amazonlinux_key)
    if not dnf_name:
        # No dnf package available, try curl fallback
        print(f"No dnf package specified in blueprint for '{package_name}'")
        print(f"Amazon Linux version: {amazonlinux_key}")
        print("Falling back to curl installation method...")
        print()
        _install_with_curl(package_name, blueprint)
        return

    # Install the package using dnf
    print(f"Installing '{package_name}' using dnf...")
    print(f"Amazon Linux version: {detector.get_version_id()}")
    print(f"Running: sudo dnf install -y {dnf_name}")
    print()

    try:
        # Install the package
        subprocess.run(
            ["sudo", "dnf", "install", "-y", dnf_name],
            check=True,
            capture_output=False
        )
        print()
        print(f"Successfully installed '{package_name}'")
    except subprocess.CalledProcessError as e:
        print()
        print(f"Error: Failed to install '{package_name}'")
        print(f"dnf exited with code {e.returncode}")


def _install_with_curl(package_name: str, blueprint: Blueprint):
    """Install a package using a curl command.

    Args:
        package_name: The name of the package to install
        blueprint: The package blueprint

    Returns:
        None
    """
    # Check if curl is installed
    curl_finder = ExecutableFinder("curl")
    curl_path = curl_finder.find()
    if not curl_path:
        print("Error: 'curl' is not installed or not in PATH")
        print("Please install curl first!")
        return

    # Get the curl command from the blueprint
    curl_command = blueprint.get_curl_command()
    if not curl_command:
        print(f"Error: No installation method available for '{package_name}'")
        return

    # Install the package using curl
    print(f"Installing '{package_name}' using curl...")
    print(f"Running: {curl_command}")
    print()

    try:
        subprocess.run(
            curl_command,
            shell=True,
            check=True,
            capture_output=False
        )
        print()
        print(f"Successfully installed '{package_name}'")
    except subprocess.CalledProcessError as e:
        print()
        print(f"Error: Failed to install '{package_name}'")
        print(f"curl command exited with code {e.returncode}")
