import json
import os
from typing import Optional

from micasa.executable_finder import ExecutableFinder
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

        Returns:
            The executable name, or None if not specified
        """
        version_check = self.data.get('version_check', {})
        return version_check.get('executable')

    def get_version_arg(self) -> str:
        """Get the argument to pass to the executable to get its version.

        Returns:
            The version argument (defaults to '--version')
        """
        version_check = self.data.get('version_check', {})
        return version_check.get('arg', '--version')

    def get_version_regex(self) -> Optional[str]:
        """Get the regex pattern to extract version from output.

        Returns:
            The regex pattern, or None if not specified
        """
        version_check = self.data.get('version_check', {})
        return version_check.get('regex')

    def check_installed_version(self) -> Optional[str]:
        """Check the version of the installed executable.

        Returns:
            The installed version string, or None if not found or unable to determine
        """
        executable_name = self.get_executable_name()
        if not executable_name:
            return None

        finder = ExecutableFinder(executable_name)
        executable_path = finder.find()
        if not executable_path:
            return None

        checker = VersionChecker(
            executable_path,
            self.get_version_arg(),
            self.get_version_regex()
        )
        return checker.get_version()

    def __str__(self):
        """Return string representation."""
        version = self.get_version()
        return f"Blueprint({self.name}, version={version})"
