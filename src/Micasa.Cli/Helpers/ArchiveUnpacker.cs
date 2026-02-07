// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models;

namespace Micasa.Cli.Helpers
{
    public class ArchiveUnpacker : IArchiveUnpacker
    {
        private readonly ICommandRunner commandRunner;

        public ArchiveUnpacker(ICommandRunner commandRunner)
        {
            this.commandRunner = commandRunner;
        }


        public async Task UnpackAsync(InstallerDirective directive, string archivePath, CancellationToken stoppingToken)
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
                case ".tar.gz":
                    await UnpackTarGzAsync(archivePath, workDir, stoppingToken);
                    break;

                default:
                    throw new NotImplementedException($"Archive unpacking for files with extension '{extension}' is not implemented.");
            }
        }


        private async Task UnpackTarGzAsync(string archivePath, string workDir, CancellationToken stoppingToken)
        {
            // Run the command
            var unpackResult = await commandRunner.RunCommandAsync("tar", $"xvfz {archivePath}", workDir, stoppingToken);

            commandRunner.VerifyExitCodeZero(unpackResult);
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
