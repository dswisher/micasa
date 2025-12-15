# micasa

A Python-based tool to help install a suite of tools on a host/machine.


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


## Requirements

This tool requires a recent Python version (one that is not end-of-life).

The second requirement is a manifest of the packages that should be installed, typically in `~/.config/micasa/micasa.toml`.
More details about this, below.


## Installation

There is no installation, per se.
Rather, just clone this git repo, and run `./micasa.py` or `python3 micasa.py`, which should print out a brief help summary.


## Status: Listing Missing/Out of Date Packages

The status command, invoked as follows, lists any packages from the manifest that are missing or have available updates.

    ./micasa.py status


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

## Blueprints

Each package in the manifest needs to have a blueprint file in the tool.
The blueprint specifies things like how to check the version, where package sources are located, and how to install them.


## Development

If you wish to make changes to the code, you should install some additional dependencies:

    uv sync --extra dev


## Testing in Docker

    docker run --interactive --tty --rm amazonlinux:2023 /bin/bash

