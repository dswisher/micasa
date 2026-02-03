// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Options;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Commands
{
    public class UninstallCommand
    {
        private readonly ILogger logger;

        public UninstallCommand(ILogger<UninstallCommand> logger)
        {
            this.logger = logger;
        }


        public async Task ExecuteAsync(UninstallOptions options, CancellationToken stoppingToken)
        {
            logger.LogWarning("Uninstall command is not yet implemented.");
            await Task.CompletedTask;
        }
    }
}
