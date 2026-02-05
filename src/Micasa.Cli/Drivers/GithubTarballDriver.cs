// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models;

namespace Micasa.Cli.Drivers
{
    public class GithubTarballDriver : IDriver
    {
        public Task<FormulaDetails?> GetInfoAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new System.NotImplementedException();
        }


        public Task<bool> InstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            // TODO - implement install for github tarball
            // 1. Using the github repo URL, fetch the latest release info from GitHub API
            // 2. Download the tarball asset from the latest release
            // 3. Extract the tarball to a temporary location
            // 4. Move the extracted files to the desired installation location

            throw new System.NotImplementedException();
        }


        public Task<bool> UninstallAsync(InstallerDirective directive, CancellationToken stoppingToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
