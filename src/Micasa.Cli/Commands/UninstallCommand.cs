// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Drivers;
using Micasa.Cli.Options;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Commands
{
    public class UninstallCommand
    {
        private readonly IDriverFactory driverFactory;
        private readonly ILogger logger;

        public UninstallCommand(
            IDriverFactory driverFactory,
            ILogger<UninstallCommand> logger)
        {
            this.driverFactory = driverFactory;
            this.logger = logger;
        }


        public async Task ExecuteAsync(UninstallOptions options, CancellationToken stoppingToken)
        {
            // Read the formula and get the driver
            var driverResult = await driverFactory.GetDriverForFormulaAsync(options.FormulaName, stoppingToken);

            if (driverResult.Driver == null)
            {
                return;
            }

            await driverResult.Driver.UninstallAsync(driverResult.InstallerDirective!, stoppingToken);

            logger.LogInformation("Removal of {Formula} completed successfully.", options.FormulaName);
        }
    }
}
