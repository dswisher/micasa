// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Helpers
{
    public class FileDownloader : IFileDownloader
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<FileDownloader> logger;

        public FileDownloader(
            IHttpClientFactory httpClientFactory,
            ILogger<FileDownloader> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }


        public async Task DownloadFileAsync(string fileUrl, string destinationPath, CancellationToken stoppingToken)
        {
            logger.LogDebug("...downloading {Url} to {Path}...", fileUrl, destinationPath);

            using (var httpClient = httpClientFactory.CreateClient())
            using (var response = await httpClient.GetAsync(fileUrl, stoppingToken))
            {
                response.EnsureSuccessStatusCode();

                await using (var fileStream = System.IO.File.Create(destinationPath))
                {
                    await response.Content.CopyToAsync(fileStream, stoppingToken);
                }
            }
        }
    }
}
