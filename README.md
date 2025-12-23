# micasa

This tool has two purposes:
1. To provide a consistent CLI interface to all the various package managers
2. To make it easy to set up your desired suite of tools on a new machine, based on a manifest


## Motivation

I work on multiple machines, whether they are local machines or hosts in the cloud.
For my own sanity, I want a (fairly) consistent environment on each machine.
This helps me be as productive as possible.

For application configuration (aka "dotfiles"), I use [chezmoi](https://www.chezmoi.io/), along with my [dotfiles repo](https://github.com/dswisher/dotfiles).
That way, my neovim, zsh and other configurations are in sync.

The other aspect are the tools that I use, like `neovim`, `uv`, `tig`, and others.
I want those tools to be installed, but each platform has a different way of getting them installed (`winget`, `apt-get`, `yum`, etc).
The purpose of this project is to help ease that pain: with one command, I want to install as many of the tools as possible, regardless of platform.

There are other (almost certainly better) tools to do this, but I haven't found one that is:
1. Low-friction to get installed
2. Works on Windows, MacOS, and Linux.


## Bootstrapping a Box

* [Windows](docs/bootstrap-windows.md)


## Roadmap

* Support for `snap`. This does not work in `docker`, so I have no way (at the moment) to test.


## Requirements

TBD


## Installation

TBD


## Commands overview

The commands are invoked as `micasa <command> <options>`. For example, `micasa info eza`.

* `info <package>` - provide details on the specified package, or if none is specified, all packages in the manifest
* `install <package>` - install the specified package, or all packages in the manifest
* `uninstall <package>` - uninstall the specified package (package name is required)


## Manifest

The manifest is a text file that contains one line per tool that should be installed.
Each line consists of a name, a colon, and a version specification.
The version specification roughly follows [PEP 440](https://peps.python.org/pep-0440/).
Here is a short example:

    neovim: 0.11.5
    uv: ~= 0.9.17
    chezmoi: ~= 2.68.1

If there is no preference for the version, the colon and version may be omitted.
In other words, this is a valid entry:

    eza

The file is typically found at `~/.config/micasa/micasa.txt`, but that may be changed by setting the `XDG_CONFIG_HOME` environment variable.


## Aliases

Some packages are known by different names by different package managers, or have various "flavors".

For example, on Ubuntu 24.04 with `apt-get`, the `fd` package is known as `fd-find`.
As another example, on Amazon Linux 2023 with `dnf`, the `curl` package could be installed from `curl` or `curl-minimal`.

To cope with this, the code for each package manager has a small list of aliases for packages.
Basically, alternate names that need to be checked/used.


## Development

This is written in rust, so a fairly recent rust toolchain should be all you need.


## Testing in Docker

TBD


## Similar Projects

* [app](https://github.com/hkdb/app) - Windows is on the roadmap, but not yet implemented. Written in go.


