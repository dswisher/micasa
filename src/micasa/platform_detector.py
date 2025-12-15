import platform
from typing import Optional


class PlatformDetector:
    """Detects the operating system and version information."""

    def __init__(self):
        self._os_type = platform.system()
        self._distribution_id: Optional[str] = None
        self._version_id: Optional[str] = None
        self._load_linux_info()

    def _load_linux_info(self):
        """Load distribution information from /etc/os-release on Linux systems."""
        if self._os_type != "Linux":
            return

        try:
            with open("/etc/os-release", "r") as f:
                for line in f:
                    line = line.strip()
                    if line.startswith("ID="):
                        self._distribution_id = line.split("=", 1)[1].strip('"')
                    elif line.startswith("VERSION_ID="):
                        self._version_id = line.split("=", 1)[1].strip('"')
        except FileNotFoundError:
            pass

    def is_macos(self) -> bool:
        """Check if running on macOS.

        Returns:
            True if running on macOS
        """
        return self._os_type == "Darwin"

    def is_linux(self) -> bool:
        """Check if running on Linux.

        Returns:
            True if running on Linux
        """
        return self._os_type == "Linux"

    def is_ubuntu(self) -> bool:
        """Check if running on Ubuntu.

        Returns:
            True if running on Ubuntu
        """
        return self.is_linux() and self._distribution_id == "ubuntu"

    def is_amazonlinux(self) -> bool:
        """Check if running on Amazon Linux.

        Returns:
            True if running on Amazon Linux
        """
        return self.is_linux() and self._distribution_id == "amzn"

    def get_ubuntu_key(self) -> Optional[str]:
        """Get the Ubuntu version key for blueprint lookups.

        Returns:
            A key like 'ubuntu22' or 'ubuntu24', or None if not Ubuntu or version unknown
        """
        if not self.is_ubuntu() or not self._version_id:
            return None

        # Extract major version (e.g., "22.04" -> "22", "24.04" -> "24")
        major_version = self._version_id.split(".")[0]
        return f"ubuntu{major_version}"

    def get_amazonlinux_key(self) -> Optional[str]:
        """Get the Amazon Linux version key for blueprint lookups.

        Returns:
            A key like 'amazonlinux2023', or None if not Amazon Linux or version unknown
        """
        if not self.is_amazonlinux() or not self._version_id:
            return None

        # Amazon Linux 2023 has VERSION_ID="2023"
        return f"amazonlinux{self._version_id}"

    def get_os_type(self) -> str:
        """Get the operating system type.

        Returns:
            The OS type string (e.g., 'Darwin', 'Linux')
        """
        return self._os_type

    def get_distribution_id(self) -> Optional[str]:
        """Get the Linux distribution ID.

        Returns:
            The distribution ID (e.g., 'ubuntu'), or None if not Linux or not found
        """
        return self._distribution_id

    def get_version_id(self) -> Optional[str]:
        """Get the distribution version ID.

        Returns:
            The version ID (e.g., '22.04'), or None if not available
        """
        return self._version_id
