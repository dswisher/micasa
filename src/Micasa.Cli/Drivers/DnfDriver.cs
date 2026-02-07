// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Drivers
{
    public class DnfDriver : IDriver
    {
        private readonly ICommandRunner commandRunner;
        private readonly ILogger logger;

        public DnfDriver(
            ICommandRunner commandRunner,
            ILogger<DnfDriver> logger)
        {
            this.commandRunner = commandRunner;
            this.logger = logger;
        }


        public Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new System.NotImplementedException();
        }


        public async Task InstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // TODO - check to make sure the package is not already installed

            // TODO - only apply sudo if we really need to
            var installResult = await commandRunner.RunCommandAsync("sudo", $"dnf install -y {directive.PackageId}", stoppingToken);

            commandRunner.VerifyExitCodeZero(installResult);
        }


        public Task UninstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
