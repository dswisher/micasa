// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Micasa.Cli.Helpers
{
    public interface IFileDistributor
    {
        Task<bool> DistributeFilesAsync(string dirPath, CancellationToken stoppingToken);
    }
}
