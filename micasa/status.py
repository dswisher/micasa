from micasa.blueprint import Blueprint
from micasa.executable_finder import ExecutableFinder
from micasa.manifest import Manifest


def run(args):
    """Run the status command."""
    manifest = Manifest.load()

    print(f"Checking {len(manifest.packages)} packages...")
    print()

    for package in manifest.packages:
        blueprint = Blueprint.load(package.name)

        if blueprint is None:
            print(f"WARNING: No blueprint found for package '{package.name}'")
            continue

        print(f"{package.name}:")
        manifest_version = package.version_spec if package.version_spec else "(any)"
        print(f"  Manifest version: {manifest_version}")

        executable_name = blueprint.get_executable_name()
        if not executable_name:
            print("  Installed version: Unable to check (no executable specified in blueprint)")
            print()
            continue

        finder = ExecutableFinder(executable_name)
        executable_path = finder.find()

        if not executable_path:
            print(f"  Installed version: Not found (executable '{executable_name}' not in PATH)")
            print()
            continue

        installed_version = blueprint.check_installed_version()

        if installed_version:
            print(f"  Installed version: {installed_version}")
        else:
            print(f"  Installed version: Unable to parse version ('{executable_name}' found at {executable_path})")

        print()
