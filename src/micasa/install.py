import subprocess
from typing import Optional

from micasa.blueprint import Blueprint
from micasa.executable_finder import ExecutableFinder
from micasa.manifest import Manifest
from micasa.platform_detector import PlatformDetector

# Track whether apt-get update has been run in this session
_apt_get_updated = False


def run(args):
    """Run the install command."""
    if args.package:
        # Install a specific package
        _install_single_package(args.package)
    else:
        # Install all packages
        _install_all_packages()


def _install_single_package(package_name: str):
    """Install a single package.

    Args:
        package_name: The name of the package to install

    Returns:
        None
    """
    manifest = Manifest.load()

    # Find the package in the manifest
    package = None
    for p in manifest.packages:
        if p.name == package_name:
            package = p
            break

    if not package:
        print(f"Error: Package '{package_name}' not found in manifest")
        return

    # Load the blueprint
    blueprint = Blueprint.load(package_name)
    if not blueprint:
        print(f"Error: No blueprint found for package '{package_name}'")
        return

    # Check if the package is already installed
    installed_version = blueprint.check_installed_version()
    if installed_version:
        print(f"Package '{package_name}' is already installed (version {installed_version})")
        return

    # Install the package
    _install_package_with_platform_detection(package_name, blueprint)


def _install_all_packages():
    """Install all packages from the manifest that are not yet installed.

    Returns:
        None
    """
    manifest = Manifest.load()

    print("Installing all packages from manifest...")
    print()

    # Identify packages that need to be installed
    packages_to_install = []
    for package in manifest.packages:
        blueprint = Blueprint.load(package.name)
        if not blueprint:
            print(f"Warning: No blueprint found for package '{package.name}', skipping")
            continue

        installed_version = blueprint.check_installed_version()
        if installed_version:
            print(f"Package '{package.name}' is already installed (version {installed_version}), skipping")
        else:
            packages_to_install.append(package.name)

    if not packages_to_install:
        print()
        print("All packages are already installed")
        return

    print()
    print(f"Found {len(packages_to_install)} package(s) to install: {', '.join(packages_to_install)}")
    print()

    # Check if curl is needed and not installed
    curl_needed = False
    curl_finder = ExecutableFinder("curl")
    curl_installed = curl_finder.find() is not None

    for package_name in packages_to_install:
        blueprint = Blueprint.load(package_name)
        if blueprint and blueprint.get_curl_command():
            curl_needed = True
            break

    # If curl is needed but not installed, try to install it first
    if curl_needed and not curl_installed:
        if "curl" in packages_to_install:
            print("Installing curl first (required for other installations)...")
            print()
            _install_single_package("curl")
            packages_to_install.remove("curl")
            print()
        else:
            print("Warning: Some packages require curl, but curl is not in the manifest")
            print("         Installation of those packages may fail")
            print()

    # Install remaining packages
    for package_name in packages_to_install:
        print(f"Installing '{package_name}'...")
        _install_single_package(package_name)
        print()

    print("Installation complete")


def _install_package_with_platform_detection(package_name: str, blueprint: Blueprint):
    """Install a package with platform detection.

    Args:
        package_name: The name of the package to install
        blueprint: The package blueprint
    """
    # Detect the platform
    detector = PlatformDetector()

    if detector.is_macos():
        _install_macos(package_name, blueprint)
    elif detector.is_ubuntu():
        distro_key = detector.get_ubuntu_key()
        _install_apt_get(package_name, blueprint, distro_key, "Ubuntu", detector.get_version_id())
    elif detector.is_debian():
        distro_key = detector.get_debian_key()
        _install_apt_get(package_name, blueprint, distro_key, "Debian", detector.get_version_id())
    elif detector.is_amazonlinux():
        _install_amazonlinux(package_name, blueprint, detector)
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
    global _apt_get_updated

    print(f"Installing '{package_name}' using apt-get...")
    print(f"{distro_name} version: {version_id}")

    # Build the command based on whether we need to update
    if _apt_get_updated:
        print(f"Running: sudo apt-get install -y {apt_get_name}")
    else:
        print(f"Running: sudo apt-get update && sudo apt-get install -y {apt_get_name}")
    print()

    try:
        # Update package lists first (only if not already done)
        if not _apt_get_updated:
            subprocess.run(
                ["sudo", "apt-get", "update"],
                check=True,
                capture_output=False
            )
            _apt_get_updated = True
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
    import os

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

    # Install the package using curl from the home directory
    # (some install scripts use relative paths like bin or .local/bin)
    home_dir = os.path.expanduser("~")
    print(f"Installing '{package_name}' using curl...")
    print(f"Running: {curl_command}")
    print(f"Working directory: {home_dir}")
    print()

    try:
        subprocess.run(
            curl_command,
            shell=True,
            check=True,
            capture_output=False,
            cwd=home_dir
        )
        print()
        print(f"Successfully installed '{package_name}'")
    except subprocess.CalledProcessError as e:
        print()
        print(f"Error: Failed to install '{package_name}'")
        print(f"curl command exited with code {e.returncode}")
