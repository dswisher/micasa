// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ILogger<GithubArchiveDriver> logger;


        public GithubArchiveDriver(
            IGitHubReleaseInfoFetcher infoFetcher,
            IGitHubArchiveSeeker archiveSeeker,
            IFileDownloader fileDownloader,
            IArchiveUnpacker archiveUnpacker,
            ILogger<GithubArchiveDriver> logger)
        {
            this.infoFetcher = infoFetcher;
            this.archiveSeeker = archiveSeeker;
            this.fileDownloader = fileDownloader;
            this.archiveUnpacker = archiveUnpacker;
            this.logger = logger;
        }


        public Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }


        public async Task<bool> InstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            try
            {
                // Fetch info on the latest release from GitHub
                if (string.IsNullOrEmpty(directive.RepoUrl))
                {
                    logger.LogError("GitHubArchiveDriver requires a RepoUrl in the installer directive.");
                    return false;
                }

                var releaseInfo = await infoFetcher.FetchLatestReleaseInfoAsync(directive.RepoUrl, stoppingToken);

                // Find the "best" archive asset from the ones available in this release
                var bestAsset = archiveSeeker.FindBestAsset(releaseInfo);

                // Download the archive to a temporary location, making sure the directory exists
                var tempDir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
                var tempScriptPath = Path.Join(tempDir, bestAsset.Name);

                if (Directory.Exists(tempDir))
                {
                    throw new Exception($"Temporary directory {tempDir} already exists.");
                }

                Directory.CreateDirectory(tempDir);

                await fileDownloader.DownloadFileAsync(bestAsset.BrowserDownloadUrl, tempScriptPath, stoppingToken);

                // Unpack the archive
                await archiveUnpacker.UnpackAsync(directive, tempScriptPath, stoppingToken);

                // Move the unpacked files to the desired installation location
                // TODO - move files to installation location
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception when running downloading and installing github archive.");
                return false;
            }
            finally
            {
                // TODO - clean up temporary files
            }

            // Success
            return true;
        }


        public Task<bool> UninstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
