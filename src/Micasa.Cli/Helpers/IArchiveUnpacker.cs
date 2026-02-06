// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Micasa.Cli.Helpers
{
    public interface IArchiveUnpacker
    {
        Task UnpackAsync(string tempScriptPath, CancellationToken stoppingToken);
    }
}
