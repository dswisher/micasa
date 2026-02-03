// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models;
using Micasa.Cli.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Installers
{
    public class DriverFactory : IDriverFactory
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IFormulaReader formulaReader;
        private readonly IPlatformDecoder platformDecoder;
        private readonly IPlatformMatcher platformMatcher;
        private readonly ILogger<DriverFactory> logger;

        public DriverFactory(
            IServiceProvider serviceProvider,
            IFormulaReader formulaReader,
            IPlatformDecoder platformDecoder,
            IPlatformMatcher platformMatcher,
            ILogger<DriverFactory> logger)
        {
            this.serviceProvider = serviceProvider;
            this.formulaReader = formulaReader;
            this.platformDecoder = platformDecoder;
            this.platformMatcher = platformMatcher;
            this.logger = logger;
        }


        public async Task<DriverFactoryResult> GetDriverForFormulaAsync(string formulaName, CancellationToken stoppingToken)
        {
            // Create the result that we will populate and return
            var result = new DriverFactoryResult();

            // Read the formula
            result.Formula = await formulaReader.ReadFormulaAsync(formulaName, stoppingToken);

            if (result.Formula == null)
            {
                logger.LogError("Formula {FormulaName} not found.", formulaName);
                return result;
            }

            // We have the formula, find the platform
            var platform = platformDecoder.GetPlatformName();
            var bestMatch = platformMatcher.FindBestMatch(platform, result.Formula.Platforms.Keys);

            if (bestMatch == null)
            {
                logger.LogError("Could not find a matching platform for {Platform} in formula {FormulaName}. Known platforms: {KnownPlatforms}",
                    platform, formulaName, string.Join(", ", result.Formula.Platforms.Keys));
                return result;
            }

            result.InstallerDirective = result.Formula.Platforms[bestMatch];

            // Find the driver that corresponds to the best match
            result.Driver = serviceProvider.GetKeyedService<IInstallationDriver>(result.InstallerDirective.Tool);

            if (result.Driver == null)
            {
                logger.LogError("Could not find an installation driver for tool {Tool} required by formula {FormulaName} on platform {Platform}.",
                    result.InstallerDirective.Tool, formulaName, bestMatch);
            }

            return result;
        }
    }
}
