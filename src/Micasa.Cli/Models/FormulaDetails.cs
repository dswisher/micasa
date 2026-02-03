// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Micasa.Cli.Models
{
    public class FormulaDetails
    {
        public required string PackageId { get; set; }
        public string? StableVersion { get; set; }
        public string? InstalledVersion { get; set; }
    }
}
