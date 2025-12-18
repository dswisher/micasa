# Notes for Claude

See README.md for details on the project.


## Code Structure Overview

The project is written in rust.
It is a wrapper around the platform-specific package managers (`brew`, `apt`, `yum`, `chocolatey`), which do all the real work.

Each package manager has a wrapper in `src/package_manager` that implements the `PackageManager` trait, defined in `src/package_manager/mod.rs`.

The code should compile without any warnings.

