// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Micasa.Cli.Drivers;

namespace Micasa.Cli.Models
{
    public class DriverFactoryResult
    {
        public Formula? Formula { get; set; }
        public string? Platform { get; set; }
        public InstallerDirective? InstallerDirective { get; set; }
        public IDriver? Driver { get; set; }
    }
}
