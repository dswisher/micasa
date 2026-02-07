// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models;
using Micasa.Cli.Parsers;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Drivers
{
    public class AptDriver : IDriver
    {
        private readonly ICommandRunner commandRunner;
        private readonly IAptInfoParser infoParser;
        private readonly ILogger logger;

        public AptDriver(
            ICommandRunner commandRunner,
            IAptInfoParser infoParser,
            ILogger<AptDriver> logger)
        {
            this.commandRunner = commandRunner;
            this.infoParser = infoParser;
            this.logger = logger;
        }


        public async Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // Ask the package manager for the status of the formula
            var statusResult = await commandRunner.RunCommandAsync("apt-cache", $"policy {directive.PackageId}", stoppingToken);

            commandRunner.VerifyExitCodeZero(statusResult);

            if (string.IsNullOrEmpty(statusResult.StandardOutput))
            {
                logger.LogError("'{Command}' command returned no output for formula {Formula}.",
                    statusResult.Command, directive.PackageId);

                return null;
            }

            // Parse the result and return it
            var details = infoParser.Parse(statusResult.StandardOutput);

            return details;
        }


        public async Task InstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // TODO - check to make sure the package is not already installed

            // TODO - only apply sudo if we really need to
            var installResult = await commandRunner.RunCommandAsync("sudo", $"apt-get install -y {directive.PackageId}", stoppingToken);

            commandRunner.VerifyExitCodeZero(installResult);
        }


        public async Task UninstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // TODO - check to make sure the package is installed first

            // TODO - only apply sudo if we really need to
            var uninstallResult = await commandRunner.RunCommandAsync("sudo", $"apt-get remove -y {directive.PackageId}", stoppingToken);

            commandRunner.VerifyExitCodeZero(uninstallResult);
        }
    }
}
