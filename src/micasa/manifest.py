import os
from typing import Optional


class Package:
    """Represents a package with its version specification."""

    def __init__(self, name: str, version_spec: str):
        self.name = name
        self.version_spec = version_spec

    def __str__(self):
        """Return string representation."""
        return f"{self.name}: {self.version_spec}"


class Manifest:
    """Represents a micasa manifest file."""

    def __init__(self):
        self.packages: list[Package] = []
        self.raw_content = ""

    @classmethod
    def load(cls, path: Optional[str] = None) -> "Manifest":
        """Load manifest from file.

        Args:
            path: Path to manifest file. Defaults to ~/.config/micasa/micasa.txt

        Returns:
            the loaded manifest.

        Raises:
            SystemExit: If the manifest file does not exist.
        """
        if path is None:
            path = os.path.expanduser("~/.config/micasa/micasa.txt")

        manifest = cls()

        try:
            with open(path, 'r') as f:
                manifest.raw_content = f.read()
        except FileNotFoundError:
            print(f"Error: Manifest file not found at: {path}")
            print()
            print("Please create a manifest file at the default location or specify a path.")
            print("See the documentation for manifest file format.")
            raise SystemExit(1)

        # Parse the manifest content
        for line in manifest.raw_content.splitlines():
            line = line.strip()
            if not line or line.startswith('#'):
                continue

            if ':' in line:
                name, version_spec = line.split(':', 1)
                name = name.strip()
                version_spec = version_spec.strip()
            else:
                name = line
                version_spec = ""

            manifest.packages.append(Package(name, version_spec))

        return manifest

    def __str__(self):
        """Return string representation for debugging."""
        return f"Manifest(packages={len(self.packages)})\nRaw content:\n{self.raw_content}"
