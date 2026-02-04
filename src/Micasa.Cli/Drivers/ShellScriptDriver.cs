// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Drivers
{
    public class ShellScriptDriver : IDriver
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ICommandRunner commandRunner;
        private readonly ILogger logger;

        public ShellScriptDriver(
            IHttpClientFactory httpClientFactory,
            ICommandRunner commandRunner,
            ILogger<ShellScriptDriver> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.commandRunner = commandRunner;
            this.logger = logger;
        }


        public Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }


        public async Task<bool> InstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // Get a temporary path for the install script that we're about to download
            var tempDir = Path.GetTempPath();
            var tempScriptPath = Path.Combine(tempDir, $"{Path.GetRandomFileName()}.sh");

            try
            {
                // Download the install script
                logger.LogDebug("...downloading {Url} to {Path}...", directive.InstallerUrl, tempScriptPath);

                using (var httpClient = httpClientFactory.CreateClient())
                using (var response = await httpClient.GetAsync(directive.InstallerUrl, stoppingToken))
                {
                    response.EnsureSuccessStatusCode();

                    await using (var fileStream = File.Create(tempScriptPath))
                    {
                        await response.Content.CopyToAsync(fileStream, stoppingToken);
                    }
                }

                // Run the install script, via the shell
                var installResult = await commandRunner.RunCommandAsync("sh", tempScriptPath, stoppingToken);

                if (!commandRunner.VerifyExitCodeZero(installResult))
                {
                    return false;
                }

                // We did it!
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception when running downloading and installing shell script.");
                return false;
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


        public Task<bool> UninstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
