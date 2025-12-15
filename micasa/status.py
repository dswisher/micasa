from micasa.blueprint import Blueprint
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

        installed_version = blueprint.check_installed_version()

        print(f"{package.name}:")
        print(f"  Manifest version: {package.version_spec}")

        if installed_version:
            print(f"  Installed version: {installed_version}")
        else:
            executable_name = blueprint.get_executable_name()
            if executable_name:
                print(f"  Installed version: Not found (executable '{executable_name}' not in PATH)")
            else:
                print("  Installed version: Unable to check (no executable specified in blueprint)")

        print()
