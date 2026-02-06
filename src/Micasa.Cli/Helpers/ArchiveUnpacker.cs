// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Helpers
{
    public class ArchiveUnpacker : IArchiveUnpacker
    {
        private readonly ICommandRunner commandRunner;
        private readonly ILogger<ArchiveUnpacker> logger;

        public ArchiveUnpacker(
            ICommandRunner commandRunner,
            ILogger<ArchiveUnpacker> logger)
        {
            this.commandRunner = commandRunner;
            this.logger = logger;
        }


        public async Task<bool> UnpackAsync(InstallerDirective directive, string archivePath, CancellationToken stoppingToken)
        {
            // Get the working directory that we'll use when unpacking
            var workDir = Path.GetDirectoryName(archivePath);

            if (string.IsNullOrEmpty(workDir))
            {
                throw new InvalidOperationException("Could not determine working directory for unpacking the archive.");
            }

            // Look at the file extension to determine how to unpack this archive
            var extension = GetArchiveExtension(archivePath);
            switch (extension)
            {
                case ".appimage":
                    return await UnpackAppImageAsync(directive, archivePath, workDir, stoppingToken);

                case ".tar.gz":
                    return await UnpackTarGzAsync(archivePath, workDir, stoppingToken);

                default:
                    throw new NotImplementedException($"Archive unpacking for files with extension '{extension}' is not implemented.");
            }
        }


        private async Task<bool> UnpackAppImageAsync(InstallerDirective directive, string archivePath, string workDir, CancellationToken stoppingToken)
        {
            // If fuse is not required, we can skip unpacking
            if (!directive.RequiresFuse.GetValueOrDefault())
            {
                return true;
            }

            // Make sure the file is executable
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                logger.LogDebug("...setting executable permissions on AppImage file {ArchivePath}...", archivePath);
                File.SetUnixFileMode(archivePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            // Run the command
            var unpackResult = await commandRunner.RunCommandAsync(archivePath, "--appimage-extract", workDir, stoppingToken);

            return commandRunner.VerifyExitCodeZero(unpackResult);
        }


        private async Task<bool> UnpackTarGzAsync(string archivePath, string workDir, CancellationToken stoppingToken)
        {
            // Run the command
            var unpackResult = await commandRunner.RunCommandAsync("tar", $"xvfz {archivePath}", workDir, stoppingToken);

            return commandRunner.VerifyExitCodeZero(unpackResult);
        }


        private static string GetArchiveExtension(string filePath)
        {
            var lowerPath = filePath.ToLowerInvariant();

            // Check for known compound extensions first
            if (lowerPath.EndsWith(".tar.gz"))
            {
                return ".tar.gz";
            }

            if (lowerPath.EndsWith(".tar.bz2"))
            {
                return ".tar.bz2";
            }

            if (lowerPath.EndsWith(".tar.xz"))
            {
                return ".tar.xz";
            }

            // Fall back to standard extension handling
            return Path.GetExtension(filePath).ToLowerInvariant();
        }
    }
}
