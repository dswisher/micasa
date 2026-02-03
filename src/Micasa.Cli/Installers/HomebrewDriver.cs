// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models;
using Micasa.Cli.Parsers;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Installers
{
    public class HomebrewDriver : IInstallationDriver
    {
        private readonly ICommandRunner commandRunner;
        private readonly IHomebrewInfoParser infoParser;
        private readonly ILogger logger;

        public HomebrewDriver(
            ICommandRunner commandRunner,
            IHomebrewInfoParser infoParser,
            ILogger<HomebrewDriver> logger)
        {
            this.commandRunner = commandRunner;
            this.infoParser = infoParser;
            this.logger = logger;
        }


        public async Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // Ask homebrew for the status of the formula
            var statusResult = await commandRunner.RunCommandAsync("brew", $"info --json=v2 {directive.PackageId}", stoppingToken);

            if (string.IsNullOrEmpty(statusResult.StandardOutput))
            {
                logger.LogError("Homebrew info command returned no output for formula {Formula}.", directive.PackageId);
                return null;
            }

            // Parse the result
            var details = infoParser.Parse(statusResult.StandardOutput);

            return details;
        }
    }
}
