// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Micasa.Cli.Installers;

namespace Micasa.Cli.Models
{
    public class DriverFactoryResult
    {
        public Formula? Formula { get; set; }
        public InstallerDirective? InstallerDirective { get; set; }
        public IInstallationDriver? Driver { get; set; }
    }
}
