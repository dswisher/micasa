from micasa.blueprint import Blueprint
from micasa.executable_finder import ExecutableFinder
from micasa.manifest import Manifest


def run(args):
    """Run the status command."""
    manifest = Manifest.load()

    # Filter packages if a specific package was requested
    packages_to_check = manifest.packages
    if args.package:
        packages_to_check = [p for p in manifest.packages if p.name == args.package]
        if not packages_to_check:
            print(f"Error: Package '{args.package}' not found in manifest")
            return

    print(f"Checking {len(packages_to_check)} package{'s' if len(packages_to_check) != 1 else ''}...")
    print()

    packages_shown = 0

    for package in packages_to_check:
        blueprint = Blueprint.load(package.name)

        if blueprint is None:
            print(f"WARNING: No blueprint found for package '{package.name}'")
            packages_shown += 1
            continue

        executable_name = blueprint.get_executable_name()
        if not executable_name:
            print(f"{package.name}:")
            manifest_version = package.version_spec if package.version_spec else "(any)"
            print(f"  Manifest version: {manifest_version}")
            print("  Installed version: Unable to check (no executable specified in blueprint)")
            print()
            packages_shown += 1
            continue

        finder = ExecutableFinder(executable_name)
        executable_path = finder.find()

        if not executable_path:
            print(f"{package.name}:")
            manifest_version = package.version_spec if package.version_spec else "(any)"
            print(f"  Manifest version: {manifest_version}")
            print(f"  Installed version: Not found (executable '{executable_name}' not in PATH)")
            print()
            packages_shown += 1
            continue

        installed_version = blueprint.check_installed_version()

        # Skip installed packages unless verbose mode is enabled
        if installed_version and not args.verbose:
            continue

        print(f"{package.name}:")
        manifest_version = package.version_spec if package.version_spec else "(any)"
        print(f"  Manifest version: {manifest_version}")

        if installed_version:
            print(f"  Installed version: {installed_version}")
        else:
            print(f"  Installed version: Unable to parse version ('{executable_name}' found at {executable_path})")

        print()
        packages_shown += 1

    if packages_shown == 0:
        print("All packages are installed and working correctly")
