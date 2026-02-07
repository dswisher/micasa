// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
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


        public async Task<bool> DistributeFilesAsync(string dirPath, CancellationToken stoppingToken)
        {
            // TODO - implement this for real - this just handles one case to verify the unit test works

            // TODO - only try to create the target directories if they do not exist
            fileSystem.Directory.CreateDirectory($"{environment.GetHomeDirectory()}/.local/bin");
            fileSystem.Directory.CreateDirectory($"{environment.GetHomeDirectory()}/.local/share/man/man1");

            MoveFile($"{dirPath}/fd-v10.3.0-aarch64-unknown-linux-musl/fd", $"{environment.GetHomeDirectory()}/.local/bin/fd");
            MoveFile($"{dirPath}/fd-v10.3.0-aarch64-unknown-linux-musl/fd.1", $"{environment.GetHomeDirectory()}/.local/share/man/man1/fd.1");

            // TODO - does this method even need to be async?
            await Task.CompletedTask;

            return true;
        }


        private void MoveFile(string sourcePath, string targetPath)
        {
            logger.LogInformation("...moving file from '{SourcePath}' to '{TargetPath}'...", sourcePath, targetPath);

            fileSystem.File.Copy(sourcePath, targetPath, overwrite: true);
        }


        private void MoveDirectory(string sourcePath, string targetPath)
        {
            // TODO - implement this to handle cases like nvim
        }


        private void CreateSymLink(string sourcePath, string targetPath)
        {
            // Create a link from sourcePath to targetPath, for example, for nvim, create a link from "~/.local/bin/nvim" to "~/.local/opt/nvim/bin/nvim"
            fileSystem.File.CreateSymbolicLink(sourcePath, targetPath);
        }
    }
}
