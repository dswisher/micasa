# Notes for Claude

See README.md for details on the project.

Packages should NEVER be installed by Claude on the local machine. Installing in the docker test container is acceptable.


## Code Structure Overview

The project is written in rust.
It is a wrapper around the platform-specific package managers (`brew`, `apt`, `yum`, `chocolatey`), which do all the real work.

Each package manager has a wrapper in `src/package_manager` that implements the `PackageManager` trait, defined in `src/package_manager/mod.rs`.

The code should compile without any warnings, and a `make lint` should not show any warnings.

