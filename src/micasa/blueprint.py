import json
import os
from typing import Optional

from micasa.executable_finder import ExecutableFinder
from micasa.platform_detector import PlatformDetector
from micasa.version_checker import VersionChecker


class Blueprint:
    """Represents a package blueprint."""

    def __init__(self, name: str, data: dict):
        self.name = name
        self.data = data

    @classmethod
    def load(cls, package_name: str, blueprints_dir: Optional[str] = None) -> Optional["Blueprint"]:
        """Load a blueprint from the blueprints directory.

        Args:
            package_name: The name of the package
            blueprints_dir: The directory containing blueprints. Defaults to ./blueprints

        Returns:
            The loaded Blueprint, or None if not found
        """
        if blueprints_dir is None:
            blueprints_dir = "blueprints"

        blueprint_path = os.path.join(blueprints_dir, f"{package_name}.json")

        if not os.path.exists(blueprint_path):
            return None

        with open(blueprint_path, 'r') as f:
            data = json.load(f)

        return cls(package_name, data)

    def get_version(self) -> Optional[str]:
        """Get the version from the blueprint.

        Returns:
            The version string, or None if not available
        """
        return self.data.get('version')

    def get_executable_name(self) -> Optional[str]:
        """Get the name of the executable binary.

        Supports both simple string format and platform-specific object format.
        For platform-specific format, looks up the executable name for the current
        platform and falls back to 'default' if not found.

        Returns:
            The executable name, or None if not specified
        """
        version_check = self.data.get('version_check', {})
        executable = version_check.get('executable')

        if executable is None:
            return None

        # If it's a string, return it directly (simple format)
        if isinstance(executable, str):
            return executable

        # If it's a dict, resolve the platform-specific name (object format)
        if isinstance(executable, dict):
            detector = PlatformDetector()

            # Try platform-specific keys in order of specificity
            if detector.is_macos():
                if 'macos' in executable:
                    return executable['macos']
            elif detector.is_ubuntu():
                ubuntu_key = detector.get_ubuntu_key()
                if ubuntu_key and ubuntu_key in executable:
                    return executable[ubuntu_key]
            elif detector.is_debian():
                debian_key = detector.get_debian_key()
                if debian_key and debian_key in executable:
                    return executable[debian_key]
            elif detector.is_amazonlinux():
                amazonlinux_key = detector.get_amazonlinux_key()
                if amazonlinux_key and amazonlinux_key in executable:
                    return executable[amazonlinux_key]

            # Fall back to 'default' if no platform-specific key matched
            return executable.get('default')

        return None

    def get_version_arg(self) -> str:
        """Get the argument to pass to the executable to get its version.

        Returns:
            The version argument (defaults to '--version')
        """
        version_check = self.data.get('version_check', {})
        return version_check.get('arg', '--version')

    def get_version_regex(self) -> Optional[str]:
        """Get the regex pattern to extract version from output.

        Supports both simple string format and platform-specific object format.
        For platform-specific format, looks up the regex for the current
        platform and falls back to 'default' if not found.

        Returns:
            The regex pattern, or None if not specified
        """
        version_check = self.data.get('version_check', {})
        regex = version_check.get('regex')

        if regex is None:
            return None

        # If it's a string, return it directly (simple format)
        if isinstance(regex, str):
            return regex

        # If it's a dict, resolve the platform-specific regex (object format)
        if isinstance(regex, dict):
            detector = PlatformDetector()

            # Try platform-specific keys in order of specificity
            if detector.is_macos():
                if 'macos' in regex:
                    return regex['macos']
            elif detector.is_ubuntu():
                ubuntu_key = detector.get_ubuntu_key()
                if ubuntu_key and ubuntu_key in regex:
                    return regex[ubuntu_key]
            elif detector.is_debian():
                debian_key = detector.get_debian_key()
                if debian_key and debian_key in regex:
                    return regex[debian_key]
            elif detector.is_amazonlinux():
                amazonlinux_key = detector.get_amazonlinux_key()
                if amazonlinux_key and amazonlinux_key in regex:
                    return regex[amazonlinux_key]

            # Fall back to 'default' if no platform-specific key matched
            return regex.get('default')

        return None

    def check_installed_version(self) -> Optional[str]:
        """Check the version of the installed executable.

        Returns:
            The installed version string, or None if not found or unable to determine
        """
        import os

        executable_name = self.get_executable_name()
        if not executable_name:
            return None

        finder = ExecutableFinder(executable_name)
        executable_path = finder.find()
        if not executable_path:
            return None

        # Warn if executable was found outside PATH
        if finder.should_warn_about_path():
            exec_dir = os.path.dirname(executable_path)
            print(f"Warning: '{executable_name}' found at {executable_path}")
            print(f"         but is not in your PATH. Consider adding {exec_dir} to your PATH.")
            print()

        checker = VersionChecker(
            executable_path,
            self.get_version_arg(),
            self.get_version_regex()
        )
        return checker.get_version()

    def get_brew_name(self) -> Optional[str]:
        """Get the brew package name from the blueprint.

        Returns:
            The brew package name, or None if not specified
        """
        names = self.data.get('package_names', {})
        return names.get('brew')

    def get_apt_get_name(self, distro_key: str) -> Optional[str]:
        """Get the apt-get package name from the blueprint for a specific distribution version.

        Args:
            distro_key: The distribution version key (e.g., 'ubuntu22', 'ubuntu24', 'debian12')

        Returns:
            The apt-get package name, or None if not specified
        """
        names = self.data.get('package_names', {})
        apt_get_names = names.get('apt-get', {})
        if isinstance(apt_get_names, dict):
            return apt_get_names.get(distro_key)
        return None

    def get_dnf_name(self, amazonlinux_key: str) -> Optional[str]:
        """Get the dnf package name from the blueprint for a specific Amazon Linux version.

        Args:
            amazonlinux_key: The Amazon Linux version key (e.g., 'amazonlinux2023')

        Returns:
            The dnf package name, or None if not specified
        """
        names = self.data.get('package_names', {})
        dnf_names = names.get('dnf', {})
        if isinstance(dnf_names, dict):
            return dnf_names.get(amazonlinux_key)
        return None

    def get_curl_command(self) -> Optional[str]:
        """Get the curl installation command from the blueprint.

        Returns:
            The curl command string, or None if not specified
        """
        names = self.data.get('package_names', {})
        return names.get('curl')

    def __str__(self):
        """Return string representation."""
        version = self.get_version()
        return f"Blueprint({self.name}, version={version})"
