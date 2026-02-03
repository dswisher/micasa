// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Helpers;
using Micasa.Cli.Options;
using Micasa.Cli.Serialization;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Commands
{
    public class InstallCommand
    {
        private readonly IFormulaReader formulaReader;
        private readonly IPlatformDecoder platformDecoder;
        private readonly IPlatformMatcher platformMatcher;
        private readonly ILogger logger;

        public InstallCommand(
            IFormulaReader formulaReader,
            IPlatformDecoder platformDecoder,
            IPlatformMatcher platformMatcher,
            ILogger<InstallCommand> logger)
        {
            this.formulaReader = formulaReader;
            this.platformDecoder = platformDecoder;
            this.platformMatcher = platformMatcher;
            this.logger = logger;
        }


        public async Task ExecuteAsync(InstallOptions options, CancellationToken stoppingToken)
        {
            // Read the formula
            var formula = await formulaReader.ReadFormulaAsync(options.FormulaName, stoppingToken);

            if (formula == null)
            {
                logger.LogError("Formula {FormulaName} not found.", options.FormulaName);
                return;
            }

            // We have the formula, find the platform
            var platform = platformDecoder.GetPlatformName();
            var bestMatch = platformMatcher.FindBestMatch(platform, formula.Platforms.Keys);

            if (bestMatch == null)
            {
                logger.LogError("Could not find a matching platform for {Platform} in formula {FormulaName}. Known platforms: {KnownPlatforms}",
                    platform, options.FormulaName, string.Join(", ", formula.Platforms.Keys));
                return;
            }

            var installerDirective = formula.Platforms[bestMatch];

            // Find the driver that corresponds to the best match
            // TODO - find the driver

            // Execute the installation steps
            // TODO - execute the installation steps

            logger.LogWarning("Install command is not yet implemented.");
        }
    }
}
