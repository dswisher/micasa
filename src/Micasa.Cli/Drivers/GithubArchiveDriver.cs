// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Exceptions;
using Micasa.Cli.GitHub;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Drivers
{
    public class GithubArchiveDriver : IDriver
    {
        private readonly IGitHubReleaseInfoFetcher infoFetcher;
        private readonly IGitHubArchiveSeeker archiveSeeker;
        private readonly IFileDownloader fileDownloader;
        private readonly IArchiveUnpacker archiveUnpacker;
        private readonly IFileDistributor fileDistributor;
        private readonly ILogger<GithubArchiveDriver> logger;


        public GithubArchiveDriver(
            IGitHubReleaseInfoFetcher infoFetcher,
            IGitHubArchiveSeeker archiveSeeker,
            IFileDownloader fileDownloader,
            IArchiveUnpacker archiveUnpacker,
            IFileDistributor fileDistributor,
            ILogger<GithubArchiveDriver> logger)
        {
            this.infoFetcher = infoFetcher;
            this.archiveSeeker = archiveSeeker;
            this.fileDownloader = fileDownloader;
            this.archiveUnpacker = archiveUnpacker;
            this.fileDistributor = fileDistributor;
            this.logger = logger;
        }


        public Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }


        public async Task InstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            string? tempDir = null;

            try
            {
                // Fetch info on the latest release from GitHub
                if (string.IsNullOrEmpty(directive.RepoUrl))
                {
                    throw new MicasaException("GitHubArchiveDriver requires a RepoUrl in the installer directive.");
                }

                var releaseInfo = await infoFetcher.FetchLatestReleaseInfoAsync(directive.RepoUrl, stoppingToken);

                // Find the "best" archive asset from the ones available in this release
                var bestAsset = archiveSeeker.FindBestAsset(releaseInfo);

                // Download the archive to a temporary location, making sure the directory exists
                tempDir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
                var tempScriptPath = Path.Join(tempDir, bestAsset.Name);

                if (Directory.Exists(tempDir))
                {
                    throw new MicasaException($"Temporary directory {tempDir} already exists.");
                }

                Directory.CreateDirectory(tempDir);

                await fileDownloader.DownloadFileAsync(bestAsset.BrowserDownloadUrl, tempScriptPath, stoppingToken);

                // Unpack the archive
                await archiveUnpacker.UnpackAsync(directive, tempScriptPath, stoppingToken);

                // Move the unpacked files to the desired installation location
                fileDistributor.DistributeFiles(tempDir);
            }
            finally
            {
                if (tempDir != null)
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }


        public Task UninstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
