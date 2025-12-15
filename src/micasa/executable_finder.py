import os
import shutil
from typing import Optional


class ExecutableFinder:
    """Finds executable binaries using various strategies."""

    def __init__(self, executable_name: str):
        self.executable_name = executable_name
        self.found_outside_path = False

    def find_in_path(self) -> Optional[str]:
        """Find the executable in the system PATH.

        Returns:
            The full path to the executable, or None if not found
        """
        return shutil.which(self.executable_name)

    def find_in_local_bin(self) -> Optional[str]:
        """Find the executable in ~/.local/bin.

        Returns:
            The full path to the executable, or None if not found
        """
        home = os.path.expanduser("~")
        local_bin = os.path.join(home, ".local", "bin", self.executable_name)

        if os.path.isfile(local_bin) and os.access(local_bin, os.X_OK):
            return local_bin

        return None

    def find(self) -> Optional[str]:
        """Find the executable using available strategies.

        This method tries different strategies in order:
        1. Search in system PATH
        2. Search in ~/.local/bin

        Returns:
            The full path to the executable, or None if not found
        """
        # First try PATH
        path = self.find_in_path()
        if path:
            self.found_outside_path = False
            return path

        # Then try ~/.local/bin
        path = self.find_in_local_bin()
        if path:
            self.found_outside_path = True
            return path

        return None

    def should_warn_about_path(self) -> bool:
        """Check if a warning should be issued about PATH.

        Returns:
            True if the executable was found outside PATH and a warning should be shown
        """
        return self.found_outside_path
