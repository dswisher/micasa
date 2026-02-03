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
I want those tools to be installed, but each platform has a different way of getting them installed (`choco`, `apt-get`, `yum`, etc).
The purpose of this project is to help ease that pain: with one command, I want to install as many of the tools as possible, regardless of platform.

There are other (almost certainly better) tools to do this, but I haven't found one that is:
1. Low-friction to get installed
2. Works on Windows, MacOS, and Linux.


