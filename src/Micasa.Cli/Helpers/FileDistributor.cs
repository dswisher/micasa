// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Helpers
{
    /// <summary>
    /// Look at a directory containing files for a package, and move (or copy) the files to the proper locations.
    /// </summary>
    /// <remarks>
    /// For example, for a package like "fd", it will move the "fd" binary to "~/.local/bin/fd", and if "fd.1"
    /// is present, it will move that to "~/.local/share/man/man1/fd.1". There are a bunch of cases to consider,
    /// so this will be a non-trivial class.
    /// </remarks>
    public class FileDistributor : IFileDistributor
    {
        private readonly IFileSystem fileSystem;
        private readonly IEnvironmentHelper environment;
        private readonly ILogger logger;

        public FileDistributor(
            IFileSystem fileSystem,
            IEnvironmentHelper environment,
            ILogger<FileDistributor> logger)
        {
            this.fileSystem = fileSystem;
            this.environment = environment;
            this.logger = logger;
        }


        public bool DistributeFiles(string dirPath)
        {
            var homeDir = environment.GetHomeDirectory();

            // Get subdirectories
            var subdirs = fileSystem.Directory.GetDirectories(dirPath);

            // Case 3: Whole tree (has bin/, lib/, or share/ subdirectories)
            foreach (var subdir in subdirs)
            {
                var subdirName = fileSystem.Path.GetFileName(subdir);
                var binSubdir = fileSystem.Path.Combine(subdir, "bin");
                var libSubdir = fileSystem.Path.Combine(subdir, "lib");
                var shareSubdir = fileSystem.Path.Combine(subdir, "share");

                var hasBinDir = fileSystem.Directory.Exists(binSubdir);
                var hasLibDir = fileSystem.Directory.Exists(libSubdir);
                var hasShareDir = fileSystem.Directory.Exists(shareSubdir);

                if (hasBinDir || hasLibDir || hasShareDir)
                {
                    // Determine the target name from the binary name if possible
                    var targetName = subdirName;
                    if (hasBinDir)
                    {
                        var binFiles = fileSystem.Directory.GetFiles(binSubdir);
                        if (binFiles.Length > 0)
                        {
                            var firstBinary = binFiles[0];
                            var binaryName = fileSystem.Path.GetFileName(firstBinary);
                            targetName = binaryName;
                        }
                    }

                    // Move the entire tree to ~/.local/opt/{targetName}
                    var optDir = fileSystem.Path.Combine(homeDir, ".local", "opt");
                    fileSystem.Directory.CreateDirectory(optDir);

                    var targetDir = fileSystem.Path.Combine(optDir, targetName);
                    MoveDirectory(subdir, targetDir);

                    // Create symlinks for binaries
                    if (hasBinDir)
                    {
                        var targetBinDir = fileSystem.Path.Combine(targetDir, "bin");
                        var binaries = fileSystem.Directory.GetFiles(targetBinDir);
                        var localBinDir = fileSystem.Path.Combine(homeDir, ".local", "bin");
                        fileSystem.Directory.CreateDirectory(localBinDir);

                        foreach (var binary in binaries)
                        {
                            var binaryName = fileSystem.Path.GetFileName(binary);
                            var linkPath = fileSystem.Path.Combine(localBinDir, binaryName);
                            var linkTarget = fileSystem.Path.Combine(targetDir, "bin", binaryName);

                            CreateSymLink(linkPath, linkTarget);
                        }
                    }

                    return true;
                }
            }

            // Case 2: Binary and optional man page in subdirectory
            foreach (var subdir in subdirs)
            {
                var files = fileSystem.Directory.GetFiles(subdir);

                // Look for executable file (heuristic: no extension, not a doc file)
                var binary = files.FirstOrDefault(f =>
                {
                    var name = fileSystem.Path.GetFileName(f);
                    var ext = fileSystem.Path.GetExtension(f);
                    return string.IsNullOrEmpty(ext) &&
                           !name.Contains("LICENSE") &&
                           !name.Contains("README") &&
                           !name.Contains("CHANGELOG");
                });

                if (binary != null)
                {
                    var binaryName = fileSystem.Path.GetFileName(binary);
                    var localBinDir = fileSystem.Path.Combine(homeDir, ".local", "bin");
                    fileSystem.Directory.CreateDirectory(localBinDir);

                    var targetBin = fileSystem.Path.Combine(localBinDir, binaryName);
                    MoveFile(binary, targetBin);

                    // Check for man page
                    var manPage = files.FirstOrDefault(f => fileSystem.Path.GetExtension(f) == ".1");
                    if (manPage != null)
                    {
                        var manName = fileSystem.Path.GetFileName(manPage);
                        var manDir = fileSystem.Path.Combine(homeDir, ".local", "share", "man", "man1");
                        fileSystem.Directory.CreateDirectory(manDir);

                        var targetMan = fileSystem.Path.Combine(manDir, manName);
                        MoveFile(manPage, targetMan);
                    }

                    return true;
                }
            }

            // Case 1: Binary in same directory as archive
            var dirFiles = fileSystem.Directory.GetFiles(dirPath);
            var simpleBinary = dirFiles.FirstOrDefault(f =>
            {
                var name = fileSystem.Path.GetFileName(f);
                var ext = fileSystem.Path.GetExtension(f);

                // Skip archives
                if (ext == ".gz" || ext == ".zip" || ext == ".tgz" || ext == ".tar")
                {
                    return false;
                }

                // Skip documentation files
                if (ext == ".md" || name.Contains("LICENSE") || name.Contains("README") || name.Contains("CHANGELOG"))
                {
                    return false;
                }

                return true;
            });

            if (simpleBinary != null)
            {
                var binaryName = fileSystem.Path.GetFileName(simpleBinary);
                var localBinDir = fileSystem.Path.Combine(homeDir, ".local", "bin");
                fileSystem.Directory.CreateDirectory(localBinDir);

                var targetBin = fileSystem.Path.Combine(localBinDir, binaryName);
                MoveFile(simpleBinary, targetBin);

                return true;
            }

            logger.LogWarning("No recognizable distribution pattern found in {DirPath}", dirPath);

            return false;
        }


        private void MoveFile(string sourcePath, string targetPath)
        {
            logger.LogInformation("...moving file from '{SourcePath}' to '{TargetPath}'...", sourcePath, targetPath);

            fileSystem.File.Copy(sourcePath, targetPath, overwrite: true);
        }


        private void MoveDirectory(string sourcePath, string targetPath)
        {
            logger.LogInformation("...moving directory from '{SourcePath}' to '{TargetPath}'...", sourcePath, targetPath);

            fileSystem.Directory.Move(sourcePath, targetPath);
        }


        private void CreateSymLink(string linkPath, string linkTarget)
        {
            logger.LogInformation("...creating symlink at '{LinkPath}' pointing to '{LinkTarget}'...", linkPath, linkTarget);

            fileSystem.File.CreateSymbolicLink(linkPath, linkTarget);
        }
    }
}
