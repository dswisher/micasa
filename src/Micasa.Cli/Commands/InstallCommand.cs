// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Installers;
using Micasa.Cli.Options;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Commands
{
    public class InstallCommand
    {
        private readonly IDriverFactory driverFactory;
        private readonly ILogger logger;

        public InstallCommand(
            IDriverFactory driverFactory,
            ILogger<InstallCommand> logger)
        {
            this.driverFactory = driverFactory;
            this.logger = logger;
        }


        public async Task ExecuteAsync(InstallOptions options, CancellationToken stoppingToken)
        {
            // Read the formula and get the installation driver
            var driverResult = await driverFactory.GetDriverForFormulaAsync(options.FormulaName, stoppingToken);

            // // Read the formula
            // var formula = await formulaReader.ReadFormulaAsync(options.FormulaName, stoppingToken);
            //
            // if (formula == null)
            // {
            //     logger.LogError("Formula {FormulaName} not found.", options.FormulaName);
            //     return;
            // }
            //
            // // We have the formula, find the platform
            // var platform = platformDecoder.GetPlatformName();
            // var bestMatch = platformMatcher.FindBestMatch(platform, formula.Platforms.Keys);
            //
            // if (bestMatch == null)
            // {
            //     logger.LogError("Could not find a matching platform for {Platform} in formula {FormulaName}. Known platforms: {KnownPlatforms}",
            //         platform, options.FormulaName, string.Join(", ", formula.Platforms.Keys));
            //     return;
            // }
            //
            // var installerDirective = formula.Platforms[bestMatch];
            //
            // // Find the driver that corresponds to the best match
            // var driver = driverFactory.GetDriverForTool(installerDirective.Tool);
            //
            // if (driver == null)
            // {
            //     logger.LogError("Could not find an installation driver for tool {Tool} required by formula {FormulaName} on platform {Platform}.",
            //         installerDirective.Tool, options.FormulaName, bestMatch);
            //     return;
            // }

            // Execute the installation steps
            // TODO - execute the installation steps

            logger.LogWarning("Install command is not yet implemented.");
        }
    }
}
