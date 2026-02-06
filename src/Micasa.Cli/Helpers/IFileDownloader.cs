// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Micasa.Cli.Helpers
{
    public interface IFileDownloader
    {
        Task DownloadFileAsync(string fileUrl, string destinationPath, CancellationToken stoppingToken);
    }
}
