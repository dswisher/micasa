import shutil
from typing import Optional


class ExecutableFinder:
    """Finds executable binaries using various strategies."""

    def __init__(self, executable_name: str):
        self.executable_name = executable_name

    def find_in_path(self) -> Optional[str]:
        """Find the executable in the system PATH.

        Returns:
            The full path to the executable, or None if not found
        """
        return shutil.which(self.executable_name)

    def find(self) -> Optional[str]:
        """Find the executable using available strategies.

        This method tries different strategies in order:
        1. Search in system PATH

        Returns:
            The full path to the executable, or None if not found
        """
        path = self.find_in_path()
        if path:
            return path

        return None
