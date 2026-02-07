// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Drivers;
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
            // Read the formula and get the driver
            var driverResult = await driverFactory.GetDriverForFormulaAsync(options.FormulaName, stoppingToken);

            if (driverResult.Driver == null)
            {
                return;
            }

            // Do the install
            logger.LogInformation("Installing {Formula}...", options.FormulaName);

            await driverResult.Driver.InstallAsync(driverResult.InstallerDirective!, stoppingToken);

            logger.LogInformation("Installation of {Formula} completed successfully.", options.FormulaName);

            if (!string.IsNullOrEmpty(driverResult.InstallerDirective!.Executable))
            {
                logger.LogInformation("Executable: '{Executable}'", driverResult.InstallerDirective.Executable);
            }
        }
    }
}
