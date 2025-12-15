import re
import subprocess
from typing import Optional


class VersionChecker:
    """Checks the version of an installed executable."""

    def __init__(self, executable_path: str, version_arg: str = "--version", version_regex: Optional[str] = None):
        self.executable_path = executable_path
        self.version_arg = version_arg
        self.version_regex = version_regex

    def get_version(self) -> Optional[str]:
        """Get the version of the executable.

        Returns:
            The version string, or None if unable to determine
        """
        try:
            result = subprocess.run(
                [self.executable_path, self.version_arg],
                capture_output=True,
                text=True,
                timeout=5
            )

            output = result.stdout + result.stderr

            if self.version_regex:
                match = re.search(self.version_regex, output)
                if match:
                    return match.group(1) if match.groups() else match.group(0)
                return None

            return output.strip()

        except (subprocess.TimeoutExpired, subprocess.SubprocessError, FileNotFoundError):
            return None
