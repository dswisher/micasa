// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models;

namespace Micasa.Cli.Helpers
{
    public interface ICommandRunner
    {
        Task<CommandRunnerResult> RunCommandAsync(string command, string arguments, CancellationToken stoppingToken);
        Task<CommandRunnerResult> RunCommandAsync(string command, string arguments, string workDir, CancellationToken stoppingToken);

        bool VerifyExitCodeZero(CommandRunnerResult statusResult);
    }
}
