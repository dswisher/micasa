// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Exceptions;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Drivers
{
    public class ShellScriptDriver : IDriver
    {
        private readonly IFileDownloader fileDownloader;
        private readonly ICommandRunner commandRunner;
        private readonly ILogger logger;

        public ShellScriptDriver(
            IFileDownloader fileDownloader,
            ICommandRunner commandRunner,
            ILogger<ShellScriptDriver> logger)
        {
            this.fileDownloader = fileDownloader;
            this.commandRunner = commandRunner;
            this.logger = logger;
        }


        public Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }


        public async Task InstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // Sanity check
            if (string.IsNullOrEmpty(directive.InstallerUrl))
            {
                throw new MicasaException("ShellScriptDriver requires an InstallerUrl in the installer directive.");
            }

            // Get a temporary path for the install script that we're about to download
            var tempDir = Path.GetTempPath();
            var tempScriptPath = Path.Combine(tempDir, $"{Path.GetRandomFileName()}.sh");

            try
            {
                // Download the install script
                await fileDownloader.DownloadFileAsync(directive.InstallerUrl, tempScriptPath, stoppingToken);

                // Run the install script, via the shell
                var installResult = await commandRunner.RunCommandAsync("sh", tempScriptPath, stoppingToken);

                commandRunner.VerifyExitCodeZero(installResult);
            }
            finally
            {
                // Clean up the temporary file when done
                if (File.Exists(tempScriptPath))
                {
                    logger.LogDebug("...deleting temporary script file {Path}...", tempScriptPath);
                    File.Delete(tempScriptPath);
                }
            }
        }


        public Task UninstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
