// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Installers;
using Micasa.Cli.Options;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Commands
{
    public class InfoCommand
    {
        private readonly IDriverFactory driverFactory;
        private readonly ILogger<InfoCommand> logger;

        public InfoCommand(
            IDriverFactory driverFactory,
            ILogger<InfoCommand> logger)
        {
            this.driverFactory = driverFactory;
            this.logger = logger;
        }


        public async Task ExecuteAsync(InfoOptions options, CancellationToken stoppingToken)
        {
            // Read the formula and get the installation driver
            var driverResult = await driverFactory.GetDriverForFormulaAsync(options.FormulaName, stoppingToken);
            if (driverResult.Driver == null)
            {
                return;
            }

            logger.LogInformation("Platform:          {Platform}", driverResult.Platform);

            // Gather the information from the driver
            var info = await driverResult.Driver.GetInfoAsync(driverResult.InstallerDirective!, stoppingToken);
            if (info == null)
            {
                return;
            }

            // Display the info
            logger.LogInformation("PackageId:         {PackageId}", info.PackageId);
            logger.LogInformation("Stable Version:    {StableVersion}", info.StableVersion);
            logger.LogInformation("Installed Version: {StableVersion}", info.InstalledVersion);
        }
    }
}
